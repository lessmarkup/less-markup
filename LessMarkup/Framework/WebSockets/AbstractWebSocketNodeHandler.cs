/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.WebSockets;
using LessMarkup.Framework.NodeHandlers;

namespace LessMarkup.Framework.WebSockets
{
    public abstract class AbstractWebSocketNodeHandler : AbstractNodeHandler, ISocketConnectionParent
    {
        private WebSocketConnection _connection;

        private static readonly Dictionary<Type, List<AbstractWebSocketNodeHandler>> _handlersByType = new Dictionary<Type, List<AbstractWebSocketNodeHandler>>();
        private static readonly object _controllersSync = new object();

        public void Start(System.Web.Mvc.Controller controller)
        {
            controller.HttpContext.AcceptWebSocketRequest(WebSocketHandler);
        }

        public virtual Task OnOpen()
        {
            lock (_controllersSync)
            {
                List<AbstractWebSocketNodeHandler> handlers;
                if (!_handlersByType.TryGetValue(GetType(), out handlers))
                {
                    handlers = new List<AbstractWebSocketNodeHandler>();
                    _handlersByType[GetType()] = handlers;
                }

                handlers.Add(this);
            }

            return Task.FromResult(0);
        }

        public virtual Task OnClose()
        {
            lock (_controllersSync)
            {
                List<AbstractWebSocketNodeHandler> handlers;
                if (_handlersByType.TryGetValue(GetType(), out handlers))
                {
                    handlers.Remove(this);
                }
            }
            return Task.FromResult(0);
        }

        public void OnException(Exception exception)
        {
        }

        public void Disconnect()
        {
            _connection.Close();
        }

        private async Task WebSocketHandler(AspNetWebSocketContext context)
        {
            _connection = new WebSocketConnection(this, context.WebSocket);
            await _connection.Handle();
        }

        public static IEnumerable<T> FindHandler<T>(Func<T, bool> func) where T : AbstractWebSocketNodeHandler
        {
            lock (_controllersSync)
            {
                List<AbstractWebSocketNodeHandler> handlers;

                if (!_handlersByType.TryGetValue(typeof(T), out handlers))
                {
                    return new List<T>();
                }

                return handlers.Where(t => func((T)t)).Select(t => (T)t).ToList();
            }
        }

        public static IEnumerable<T> FindHandler<T>() where T : AbstractWebSocketNodeHandler
        {
            lock (_controllersSync)
            {
                List<AbstractWebSocketNodeHandler> handlers;

                if (!_handlersByType.TryGetValue(typeof(T), out handlers))
                {
                    return new List<T>();
                }

                return handlers.Select(t => (T)t).ToList();
            }
        }

        protected Task SendRequest(string method, byte[] binary, int offset, int count, object parameters = null)
        {
            return _connection.SendRequest(method, binary, offset, count, parameters);
        }

        protected Task SendRequest(string method, object data = null)
        {
            return _connection.SendRequest(method, data);
        }
    }
}
