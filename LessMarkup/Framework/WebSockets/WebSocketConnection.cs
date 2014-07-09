/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LessMarkup.Framework.WebSockets
{
    public class WebSocketConnection
    {
        private readonly ISocketConnectionParent _instance;
        private WebSocket _socket;
        private readonly CancellationTokenSource _cancellationToken;
        private readonly object _sendLock = new object();
        private readonly List<Task> _sendTasks = new List<Task>();

        public WebSocketConnection(ISocketConnectionParent instance, WebSocket socket)
        {
            _instance = instance;
            _socket = socket;
            _cancellationToken = new CancellationTokenSource();
        }

        public async Task Handle()
        {
            MemoryStream readStream = null;

            await _instance.OnOpen();

            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    var buffer = new Byte[1024];
                    var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken.Token);

                    if (_cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (!result.EndOfMessage)
                    {
                        if (readStream == null)
                        {
                            readStream = new MemoryStream();
                        }

                        readStream.Write(buffer, 0, result.Count);

                        continue;
                    }

                    byte[] resultBuffer;
                    int resultLength;

                    if (readStream != null)
                    {
                        readStream.Write(buffer, 0, result.Count);
                        resultBuffer = readStream.ToArray();
                        resultLength = resultBuffer.Length;
                        readStream.Dispose();
                        readStream = null;
                    }
                    else
                    {
                        resultBuffer = buffer;
                        resultLength = result.Count;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        break;
                    }

                    await HandleRequest(resultBuffer, resultLength, result.MessageType == WebSocketMessageType.Binary);

                    if (_socket == null || _socket.State != WebSocketState.Open)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                LogException(e);
            }

            if (readStream != null)
            {
                readStream.Dispose();
            }

            _socket = null;

            await _instance.OnClose();
        }

        public void Close()
        {
            _cancellationToken.Cancel();
        }

        private async Task HandleRequest(byte[] data, int length, bool isBinary)
        {
            if (!isBinary)
            {
                var text = Encoding.UTF8.GetString(data, 0, length);
                int pos = text.IndexOf(";", StringComparison.Ordinal);
                if (pos <= 0)
                {
                    throw new ArgumentOutOfRangeException("data");
                }
                var methodName = text.Substring(0, pos);
                pos += 1;
                var values = pos < text.Length ? JsonConvert.DeserializeObject<Dictionary<string, object>>(text.Substring(pos)).ToDictionary(k => k.Key.ToLower(), k => k.Value) : null;

                var method = _instance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(m => string.Compare(m.Name, methodName, StringComparison.OrdinalIgnoreCase) == 0 && m.ReturnType == typeof(Task));

                var methodParameters = method.GetParameters();
                var methodValues = new object[methodParameters.Length];

                if (values != null)
                {
                    for (var i = 0; i < methodParameters.Length; i++)
                    {
                        object parameterValue;
                        if (!values.TryGetValue(methodParameters[i].Name.ToLower(), out parameterValue) || parameterValue == null)
                        {
                            continue;
                        }
                        parameterValue = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(parameterValue), methodParameters[i].ParameterType);
                        methodValues[i] = parameterValue;
                    }
                }

                var result = (Task)method.Invoke(_instance, methodValues);

                await result;
            }
            else
            {
                var methodNameLength = BitConverter.ToInt32(data, 0);
                var valuesLength = BitConverter.ToInt32(data, 4);
                var methodName = Encoding.UTF8.GetString(data, 8, methodNameLength);
                var valuesText = valuesLength > 0 ? Encoding.UTF8.GetString(data, 8 + methodNameLength, valuesLength) : null;
                var values = valuesText != null ? JsonConvert.DeserializeObject<Dictionary<string, object>>(valuesText).ToDictionary(k => k.Key.ToLower(), k => k.Value) : null;

                var method = _instance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(m => string.Compare(m.Name, methodName, StringComparison.OrdinalIgnoreCase) == 0 && m.ReturnType == typeof(Task));

                var methodParameters = method.GetParameters();

                if (methodParameters.Length < 3 || methodParameters[0].ParameterType != typeof(byte[]) || methodParameters[1].ParameterType != typeof(int) || methodParameters[2].ParameterType != typeof(int))
                {
                    throw new ArgumentOutOfRangeException("data");
                }

                var methodValues = new object[methodParameters.Length];
                methodValues[0] = data;
                methodValues[1] = methodNameLength + 8 + valuesLength;
                methodValues[2] = length - methodNameLength - 8 - valuesLength;

                if (methodParameters.Length > 3 && values != null)
                {
                    for (int i = 3; i < methodParameters.Length; i++)
                    {
                        object parameterValue;
                        if (!values.TryGetValue(methodParameters[i].Name.ToLower(), out parameterValue) || parameterValue == null)
                        {
                            continue;
                        }
                        parameterValue = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(parameterValue), methodParameters[i].ParameterType);
                        methodValues[i] = parameterValue;
                    }
                }

                var result = (Task)method.Invoke(_instance, methodValues);

                await result;
            }
        }

        private async Task SendRequest(WebSocket socket, byte[] fullData, WebSocketMessageType messageType)
        {
            var rest = fullData.Length;
            var offset = 0;

            while (rest > 0)
            {
                var bufferSize = rest;
                bool endOfMessage = true;
                if (bufferSize > 1024)
                {
                    bufferSize = 1024;
                    endOfMessage = false;
                }

                var array = new ArraySegment<byte>(fullData, offset, bufferSize);

                await socket.SendAsync(array, messageType, endOfMessage, CancellationToken.None);

                rest -= bufferSize;
                offset += bufferSize;
            }
        }

        private async Task SendRequest(byte[] fullData, WebSocketMessageType messageType)
        {
            var socket = _socket;

            if (socket == null)
            {
                // We don't throw here an exception as it is possible for the machine to send image before it realizes the connection is closed
                return;
            }

            List<Task> waitTasks;
            Task ourTask;

            // We should wait until all send previous tasks will be completed

            lock (_sendLock)
            {
                waitTasks = _sendTasks.Count > 0 ? _sendTasks.ToList() : null;
                ourTask = new Task(() => SendRequest(socket, fullData, messageType), _cancellationToken.Token, TaskCreationOptions.None);
                _sendTasks.Add(ourTask);
            }

            if (waitTasks != null)
            {
                await Task.WhenAll(waitTasks);
            }

            ourTask.Start();

            await ourTask;

            lock (_sendLock)
            {
                _sendTasks.Remove(ourTask);
            }

            ourTask.Dispose();
        }

        public async Task SendRequest(string method, byte[] binary, int offset, int count, object parameters = null)
        {
            var methodData = Encoding.UTF8.GetBytes(method);
            var parametersData = parameters != null ? Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(parameters)) : null;
            var methodLength = BitConverter.GetBytes(methodData.Length);
            var parametersLength = BitConverter.GetBytes(parametersData != null ? parametersData.Length : 0);
            var fullData = new byte[count + 8 + methodData.Length + (parametersData != null ? parametersData.Length : 0)];
            Buffer.BlockCopy(methodLength, 0, fullData, 0, 4);
            Buffer.BlockCopy(parametersLength, 0, fullData, 4, 4);
            Buffer.BlockCopy(methodData, 0, fullData, 8, methodData.Length);
            if (parametersData != null)
            {
                Buffer.BlockCopy(parametersData, 0, fullData, 8 + methodData.Length, parametersData.Length);
            }
            Buffer.BlockCopy(binary, offset, fullData, methodData.Length + 8 + (parametersData != null ? parametersData.Length : 0), count);
            await SendRequest(fullData, WebSocketMessageType.Binary);
        }

        public async Task SendRequest(string method, object data = null)
        {
            var fullData = Encoding.UTF8.GetBytes(method + ";" + (data != null ? JsonConvert.SerializeObject(data) : ""));
            await SendRequest(fullData, WebSocketMessageType.Text);
        }

        private void LogException(Exception e)
        {
            _instance.OnException(e);
        }
    }
}
