/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using LessMarkup.Engine.Logging;

namespace LessMarkup.Engine.Email
{
    public class Pop3Client
    {
        enum StateMachine
        {
            WaitForHandshake,
            WaitUserOk,
            WaitPasswordOk,
            WaitMessageNumbers,
            WaitMessage,
            WaitMessageDeleteOk,
            WaitQuit,
            Quit,
        }

        private Stream _stream;
        private TcpClient _client;
        private readonly BufferBuilder _bufferBuilder = new BufferBuilder();
        private StateMachine _state;
        private int _messageSizeLimit;

        private readonly Encoding _originalEncoding = Encoding.GetEncoding(1252);

        private string _answerHeader;
        private readonly List<string> _answerBody = new List<string>();
        private bool _waitMultilineAnswer;

        private Func<Pop3Message, bool> _messageHandler;

        private readonly List<Tuple<int, int>> _pendingMessages = new List<Tuple<int, int>>(); 

        public void DownloadMessages(string server, bool useSsl, string user, string password, int messageSizeLimit, Func<Pop3Message, bool> messageHandler)
        {
            _messageSizeLimit = messageSizeLimit;
            _messageHandler = messageHandler;

            int port = 110;
            int pos = server.IndexOf(':');
            if (pos > 0)
            {
                port = int.Parse(server.Substring(pos + 1));
                server = server.Remove(pos).Trim();
            }

            using (_client = new TcpClient())
            {
                _client.NoDelay = true;
                this.LogDebug("Connecting to server " + server + ", port " + port);
                _client.Connect(server, port);

                _state = StateMachine.WaitForHandshake;

                SslStream sslStream = null;

                if (useSsl)
                {
                    sslStream = new SslStream(_client.GetStream());
                    _stream = sslStream;
                }
                else
                {
                    _stream = _client.GetStream();
                }

                using (_stream)
                {
                    if (sslStream != null)
                    {
                        sslStream.AuthenticateAsClient(server);
                    }

                    while (_client.Connected)
                    {
                        if (_state == StateMachine.Quit)
                        {
                            break;
                        }

                        if (_client.Available > 0)
                        {
                            this.LogDebug("Received " + _client.Available + " bytes");
                            _bufferBuilder.Append(_stream, _client.Available);
                        }

                        HandleReceivedData();

                        if (_answerHeader == null)
                        {
                            Thread.Sleep(100);
                            continue;
                        }

                        if (!_answerHeader.StartsWith("+OK"))
                        {
                            this.LogDebug("Received negative answer: " + _answerHeader);
                            this.LogDebug("Terminating connection");
                            break;
                        }

                        this.LogDebug("Received positive answer: " + _answerHeader);

                        switch (_state)
                        {
                            case StateMachine.WaitForHandshake:
                                this.LogDebug("Sending USER command");
                                SendCommand("USER " + user, false);
                                _state = StateMachine.WaitUserOk;
                                break;
                            case StateMachine.WaitUserOk:
                                this.LogDebug("Sending PASS command");
                                SendCommand("PASS " + password, false);
                                _state = StateMachine.WaitPasswordOk;
                                break;
                            case StateMachine.WaitPasswordOk:
                                this.LogDebug("Sending LIST command");
                                SendCommand("LIST", true);
                                _state = StateMachine.WaitMessageNumbers;
                                break;
                            case StateMachine.WaitMessageNumbers:
                                ParseMessageNumbers();
                                RequestNextPendingMessage();
                                break;
                            case StateMachine.WaitMessage:
                                ReadNextMessage();
                                break;
                            case StateMachine.WaitMessageDeleteOk:
                                RequestNextPendingMessage();
                                break;
                            case StateMachine.WaitQuit:
                                _state = StateMachine.Quit;
                                break;
                        }

                        _answerBody.Clear();
                        _answerHeader = null;
                    }
                }

                _client.Close();
            }
        }

        private void RequestNextPendingMessage()
        {
            if (!_pendingMessages.Any())
            {
                this.LogDebug("No pending messages available, sending QUIT command");
                SendCommand("QUIT", false);
                _state = StateMachine.WaitQuit;
                return;
            }

            var messageNumber = _pendingMessages[0].Item1;
            var messageSize = _pendingMessages[0].Item2;

            if (messageSize > _messageSizeLimit)
            {
                this.LogDebug("Sending HEAD " + messageNumber + " 2 command");
                SendCommand("HEAD " + messageNumber + " 2", true);
            }
            else
            {
                this.LogDebug("Sending RETR " + messageNumber + " command");
                SendCommand("RETR " + messageNumber, true);
            }

            _state = StateMachine.WaitMessage;
        }

        private static string ExtractEmail(string text)
        {
            for (;;)
            {
                int first = text.IndexOf('"');
                if (first < 0)
                {
                    break;
                }

                int second = text.IndexOf('"', first + 1);
                if (second <= first)
                {
                    break;
                }

                text = text.Remove(first, second + 1 - first);
            }

            text = text.Trim();

            var last = text.LastIndexOfAny(new[] {' ', '\t'});
            if (last >= 0)
            {
                text = text.Remove(0, last + 1);
            }

            if (text.StartsWith("<") && text.EndsWith(">"))
            {
                text = text.Substring(1, text.Length - 2);
            }

            return text.Trim().ToLower();
        }

        private static int GetXDigit(byte b)
        {
            if (b >= '0' && b <= '9')
            {
                return (byte)(b - '0');
            }
            if (b >= 'a' && b <= 'f')
            {
                return (byte) (b - 'a' + 10);
            }
            if (b >= 'A' && b <= 'F')
            {
                return (byte)(b - 'A' + 10);
            }
            return -1;
        }

        private string ExtractSubject(string text, string charset)
        {
            if (!string.IsNullOrWhiteSpace(charset))
            {
                try
                {
                    var targetEncoding = Encoding.GetEncoding(charset);
                    if (targetEncoding.CodePage != _originalEncoding.CodePage)
                    {
                        text = targetEncoding.GetString(_originalEncoding.GetBytes(text));
                    }
                }
// ReSharper disable EmptyGeneralCatchClause
                catch (Exception)
// ReSharper restore EmptyGeneralCatchClause
                {
                    
                }
            }

            for (;;)
            {
                var startBlock = text.IndexOf("=?", StringComparison.Ordinal);
                if (startBlock < 0)
                {
                    break;
                }

                var endBlock = text.IndexOf("?=", startBlock, StringComparison.Ordinal);

                if (endBlock < startBlock)
                {
                    break;
                }

                var block = text.Substring(startBlock + 2, endBlock - startBlock - 2).Split(new []{'?'});

                text = text.Remove(startBlock, endBlock - startBlock + 2);

                if (block.Length != 3 || block[1].Length != 1)
                {
                    continue;
                }

                var blockType = Char.ToLower(block[1][0]);

                if (blockType != 'q' && blockType != 'b')
                {
                    continue;
                }

                Encoding encoding;

                try
                {
                    encoding = Encoding.GetEncoding(block[0]);
                }
                catch
                {
                    continue;
                }

                byte[] blockBytes;
                int blockLength;

                if (blockType == 'b')
                {
                    blockBytes = _originalEncoding.GetBytes(block[2]);
                    blockLength = blockBytes.Length;
                }
                else
                {
                    var source = _originalEncoding.GetBytes(block[2]);
                    blockBytes = new byte[source.Length];
                    blockLength = 0;
                    for (int i = 0; i < source.Length;)
                    {
                        if (source[i] == '_')
                        {
                            blockBytes[blockLength++] = 32;
                            i++;
                            continue;
                        }
                        if (source[i] != '=' || i + 2 >= source.Length)
                        {
                            blockBytes[blockLength++] = source[i++];
                            continue;
                        }
                        var ch1 = GetXDigit(source[i + 1]);
                        var ch2 = GetXDigit(source[i + 2]);
                        if (ch1 == -1 || ch2 == -1)
                        {
                            blockBytes[blockLength++] = source[i++];
                            continue;
                        }

                        blockBytes[blockLength++] = (byte)((ch1 << 4) | ch2);
                    }
                }

                var decodedBlock = encoding.GetString(blockBytes, 0, blockLength);

                text = text.Insert(startBlock, decodedBlock);
            }

            return text;
        }

        // Date: Sun, 9 Jun 2002 23:06:48 +0400
        // or
        // Date: Sun, 9 Jun 2002 23:06:48 +0400 (ZoneName)

        private readonly static List<string> _months = new List<string> {"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};

        public static DateTime ExtractDateTime(string text)
        {
            var dateParts = text.Split(new[] {' '});
            if (dateParts.Length != 6 && dateParts.Length != 7)
            {
                return DateTime.UtcNow;
            }

            int day, year, timeZone;

            int month = _months.IndexOf(dateParts[2])+1;

            if (month < 1 || !int.TryParse(dateParts[1], out day) || !int.TryParse(dateParts[3], out year))
            {
                return DateTime.UtcNow;
            }

            var timeZoneText = dateParts[5];

            if (timeZoneText.Length == 0 || (timeZoneText[0] != '+' && timeZoneText[0] != '-') ||
                !int.TryParse(timeZoneText.Substring(1), out timeZone))
            {
                return DateTime.UtcNow;
            }

            var timeZoneHours = timeZone / 100;
            var timeZoneMinutes = timeZone % 100;

            if (timeZoneText[0] == '-')
            {
                timeZoneHours = -timeZoneHours;
                timeZoneMinutes = -timeZoneMinutes;
            }

            int hour, min, sec;

            var timeParts = dateParts[4].Split(new[] {':'});
            if (timeParts.Length != 3 || !int.TryParse(timeParts[0], out hour) || !int.TryParse(timeParts[1], out min) ||
                !int.TryParse(timeParts[2], out sec))
            {
                return DateTime.UtcNow;
            }

            if (year < 2000 || year > 3000 || month < 1 || month > 12 || day < 1 || day > 31 || hour < 0 || hour > 23 ||
                min < 0 || min >= 60 || sec < 0 || sec >= 60 || timeZoneMinutes >= 60 || timeZoneHours > 12 || timeZoneHours < -12)
            {
                return DateTime.UtcNow;
            }

            return new DateTime(year, month, day, hour, min, sec, DateTimeKind.Utc).AddHours(-timeZoneHours).AddMinutes(-timeZoneMinutes);
        }

        private void ReadNextMessage()
        {
            var message = new Pop3Message
            {
                Attachments = new List<MessageAttachment>()
            };

            var messageNumber = _pendingMessages[0].Item1;
            var messageSize = _pendingMessages[0].Item2;
            _pendingMessages.RemoveAt(0);

            this.LogDebug("Parsing message number " + messageNumber);

            var contentType = "text/plain";
            var contentCharset = "us-ascii";
            var contentTransferEncoding = "";
            var hasReceived = false;
            int pos;
            int lineNumber;
            for (lineNumber = 0; lineNumber < _answerBody.Count;)
            {
                var headerLine = _answerBody[lineNumber++];

                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    break;
                }

                while (lineNumber < _answerBody.Count && !string.IsNullOrWhiteSpace(_answerBody[lineNumber]) && Char.IsWhiteSpace(_answerBody[lineNumber][0]))
                {
                    headerLine += " " + _answerBody[lineNumber++].Trim();
                }

                pos = headerLine.IndexOf(':');
                if (pos <= 0)
                {
                    continue;
                }

                var headerKey = headerLine.Substring(0, pos).Trim().ToLower();
                var headerValue = headerLine.Substring(pos + 1).Trim();

                this.LogDebug("Header key: '" + headerKey + "', value '" + headerValue + "'");

                switch (headerKey)
                {
                    case "from":
                        message.From = headerValue;
                        break;
                    case "subject":
                        message.Subject = headerValue;
                        this.LogDebug("Subject set to '" + message.Subject + "'");
                        break;
                    case "received":
                        if (hasReceived)
                        {
                            break;
                        }
                        hasReceived = true;
                        pos = headerValue.LastIndexOf(';');
                        if (pos > 0)
                        {
                            headerValue = headerValue.Remove(0, pos + 1).Trim();
                        }
                        message.Received = ExtractDateTime(headerValue);
                        this.LogDebug("Received date set to '" + message.Received.ToShortDateString() + " " + message.Received.ToShortTimeString() + "'");
                        break;
                    case "date":
                        message.Created = ExtractDateTime(headerValue);
                        this.LogDebug("Created date set to '" + message.Created.ToShortDateString() + " " + message.Created.ToShortTimeString() + "'");
                        break;
                    case "content-type":
                        contentType = headerValue;
                        break;
                    case "content-transfer-encoding":
                        contentTransferEncoding = headerValue;
                        break;
                }
            }

            var multipartBoundary = "";

            pos = contentType.IndexOf(';');
            if (pos > 0)
            {
                var parameters = contentType.Substring(pos + 1);
                contentType = contentType.Substring(0, pos).Trim();

                foreach (var parameter in parameters.Split(new[] {';'}))
                {
                    pos = parameter.IndexOf('=');
                    if (pos <= 0)
                    {
                        continue;
                    }
                    var parameterName = parameter.Substring(0, pos).Trim().ToLower();
                    var parameterValue = parameter.Substring(pos + 1).Trim();

                    this.LogDebug("Content Type parameter key: '" + parameterName + "', value '" + parameterValue + "'");

                    if (parameterValue.Length >= 2 && parameterValue.StartsWith("\"") && parameterValue.EndsWith("\""))
                    {
                        parameterValue = parameterValue.Substring(1, parameterValue.Length - 2).Trim();
                    }
                    switch (parameterName)
                    {
                        case "boundary":
                            multipartBoundary = parameterValue;
                            break;
                        case "charset":
                            contentCharset = parameterValue;
                            break;
                    }
                }
            }

            message.LogDebug("Message content type: " + contentType);

            message.FromEmail = ExtractEmail(message.From);
            message.Subject = ExtractSubject(message.Subject, contentCharset);

            if (messageSize > _messageSizeLimit)
            {
                this.LogDebug("Message size exceeds specified limit: size " + messageSize + ", limit " + _messageSizeLimit);
                message.ParseError = string.Format("Message size exceeds limit of {0} bytes", _messageSizeLimit);
            }
            else
            {
                contentType = contentType.ToLower();
                switch (contentType)
                {
                    case "text/html":
                        this.LogDebug("Processing html message");
                        ProcessHtmlMessage(message, _answerBody, lineNumber, _answerBody.Count-lineNumber, contentCharset, contentTransferEncoding);
                        break;
                    case "text/plain":
                        this.LogDebug("Processing plaintext message");
                        ProcessPlainTextMessage(message, _answerBody, lineNumber, _answerBody.Count-lineNumber, contentCharset, contentTransferEncoding);
                        break;
                    default:
                        if (contentType.StartsWith("multipart/") && !string.IsNullOrWhiteSpace(multipartBoundary))
                        {
                            this.LogDebug("Processing multipart message");
                            ProcessMultipartMessage(message, lineNumber, _answerBody.Count-lineNumber, multipartBoundary, contentCharset);
                            break;
                        }
                        this.LogDebug("Cannot handle specified message type");
                        message.ParseError = "Unknown message content type";
                        break;
                }
            }

            if (_messageHandler.Invoke(message))
            {
                this.LogDebug("Successfully handled the message, sending DELE " + messageNumber + " command");
                SendCommand("DELE " + messageNumber, false);
                _state = StateMachine.WaitMessageDeleteOk;
            }
            else
            {
                this.LogDebug("Failed to handle the message, requesting next message");
                RequestNextPendingMessage();
            }
        }

        private static void DecorateAsHtml(string text, StringBuilder builder)
        {
            builder.Append("<p>");
            foreach (var c in text)
            {
                switch (c)
                {
                    case '<':
                        builder.Append("&lt;");
                        break;
                    case '>':
                        builder.Append("&gt;");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }
            builder.AppendLine("</p>");
        }

        private List<string> DecodeQuotedPrintable(List<string> source, int from, int count)
        {
            var ret = new List<string>();

            int to = from + count;
            var buf = new byte[1000];
            for (int i = from; i < to;)
            {
                var line = _originalEncoding.GetBytes(source[i++]);
                var bp = 0;
                for (int j = 0; j < line.Length;)
                {
                    if (bp >= buf.Length - 3)
                    {
                        ret.Add(_originalEncoding.GetString(buf, 0, bp));
                        bp = 0;
                    }

                    var ch = line[j++];
                    if (ch != '=')
                    {
                        buf[bp++] = ch;
                        continue;
                    }
                    if (j == line.Length)
                    {
                        if (i < to)
                        {
                            line = _originalEncoding.GetBytes(source[i++]);
                            j = 0;
                            continue;
                        }
                        buf[bp++] = ch;
                        break;
                    }
                    if (j + 1 >= line.Length)
                    {
                        buf[bp++] = ch;
                        continue;
                    }
                    var ch1 = GetXDigit(line[j]);
                    var ch2 = GetXDigit(line[j + 1]);
                    if (ch1 < 0 || ch2 < 0)
                    {
                        buf[bp++] = ch;
                        continue;
                    }
                    j += 2;
                    buf[bp++] = (byte)((ch1 << 4) + ch2);
                }
                ret.Add(_originalEncoding.GetString(buf, 0, bp));
            }

            return ret;
        }

        private void ProcessPlainTextMessage(Pop3Message message, List<string> lines, int startLine, int lineCount, string charset, string contentTransferEncoding)
        {
            if (contentTransferEncoding == "quoted-printable")
            {
                lines = DecodeQuotedPrintable(lines, startLine, lineCount);
                startLine = 0;
                lineCount = lines.Count;
            }

            Encoding destinationEncoding;

            try
            {
                destinationEncoding = Encoding.GetEncoding(charset);
            }
            catch (Exception)
            {
                destinationEncoding = _originalEncoding;
            }

            var sb = new StringBuilder();

            var endLine = startLine + lineCount;
            for (int i = startLine; i < endLine; i++)
            {
                var text = lines[i];

                if (destinationEncoding.CodePage != _originalEncoding.CodePage)
                {
                    text = destinationEncoding.GetString(_originalEncoding.GetBytes(text));
                }

                DecorateAsHtml(text, sb);
            }

            message.HtmlBody = sb.ToString();
        }

        private void ProcessHtmlMessage(Pop3Message message, List<string> lines, int startLine, int lineCount, string charset, string contentTransferEncoding)
        {
            if (contentTransferEncoding == "quoted-printable")
            {
                lines = DecodeQuotedPrintable(lines, startLine, lineCount);
                startLine = 0;
                lineCount = lines.Count;
            }

            Encoding destinationEncoding;

            try
            {
                destinationEncoding = Encoding.GetEncoding(charset);
            }
            catch (Exception)
            {
                destinationEncoding = _originalEncoding;
            }

            var sb = new StringBuilder();
            var endLine = startLine + lineCount;

            var tagStack = new List<string>();

            for (int i = startLine; i < endLine; i++)
            {
                var text = lines[i];

                if (destinationEncoding.CodePage != _originalEncoding.CodePage)
                {
                    text = destinationEncoding.GetString(_originalEncoding.GetBytes(text));
                }

                for (int p = 0; p < text.Length;)
                {
                    var c = text[p];
                    if (c != '<')
                    {
                        sb.Append(c);
                        p++;
                        continue;
                    }
                    p++;
                    var tagBodyStart = p;
                    var isClosing = p < text.Length && text[p] == '/';
                    if (isClosing)
                    {
                        p++;
                    }
                    var endTagName = false;
                    int tagNameFrom = p;
                    int tagNameTo = p;
                    for (; p < text.Length; p++)
                    {
                        c = text[p];

                        if (c == '>' || !Char.IsLetterOrDigit(c))
                        {
                            if (!endTagName)
                            {
                                endTagName = true;
                                tagNameTo = p;
                            }

                            if (c == '>')
                            {
                                break;
                            }
                        }
                    }

                    if (p < text.Length && text[p] == '>')
                    {
                        p++;
                        var tagName = text.Substring(tagNameFrom, tagNameTo - tagNameFrom).ToLower();
                        switch (tagName)
                        {
                            case "p":
                            case "div":
                            case "span":
                            case "b":
                            case "i":
                            case "u":
                            case "blockquote":
                            case "ul":
                            case "ol":
                            case "li":
                            case "table":
                            case "tr":
                            case "td":
                            case "header":
                            case "hr":
                                break;
                            case "h1":
                            case "h2":
                            case "h3":
                            case "h4":
                            case "h5":
                            case "h6":
                                tagName = "h3";
                                break;
                            default:
                                continue;
                        }

                        if (isClosing)
                        {
                            if (tagStack.Contains(tagName))
                            {
                                tagStack.Remove(tagName);
                            }
                            sb.Append("</" + tagName + ">");
                        }
                        else
                        {
                            if (tagName == "blockquote")
                            {
                                i = endLine;
                                break;
                            }
                            if (tagName == "div")
                            {
                                var tagBody = text.Substring(tagBodyStart, p - tagBodyStart - 1);
                                if (tagBody.Contains("gmail_quote"))
                                {
                                    i = endLine;
                                    break;
                                }
                            }

                            if (tagName == "p")
                            {
                                tagStack.Remove(tagName);
                            }

                            tagStack.Add(tagName);

                            sb.Append("<" + tagName + ">");
                        }
                    }
                }
            }

            for (int i = tagStack.Count - 1; i >= 0; i--)
            {
                sb.AppendLine("</" + tagStack[i] + ">");
            }

            message.HtmlBody = sb.ToString();
        }

        private void ProcessMultipartMessage(Pop3Message message, int startLine, int lineCount, string boundary, string charset)
        {
            var boundaryLine = "--" + boundary;
            var boundaryEndLine = "--" + boundary + "--";

            var endLine = startLine + lineCount;

            int? previousLine = null;

            for (int lineNumber = startLine; lineNumber < endLine; lineNumber++)
            {
                var line = _answerBody[lineNumber];

                if (line != boundaryLine && line != boundaryEndLine)
                {
                    continue;
                }

                if (!previousLine.HasValue)
                {
                    if (line == boundaryEndLine)
                    {
                        break;
                    }

                    previousLine = lineNumber;
                    continue;
                }

                ProcessSinglePart(message, previousLine.Value+1, lineNumber-previousLine.Value-1, charset);

                previousLine = lineNumber;

                if (line == boundaryEndLine)
                {
                    break;
                }
            }
        }

        private void ProcessSinglePart(Pop3Message message, int startLine, int lineCount, string charset)
        {
            this.LogDebug("Processing a part of multipart message");

            var contentType = "text/plain";
            var contentTransferEncoding = "base64";
            var contentBoundary = "";
            var fileName = "";

            int endLine = startLine + lineCount;
            int pos;
            int lineNumber;
            for (lineNumber = startLine; lineNumber < endLine;)
            {
                var line = _answerBody[lineNumber++];
                if (string.IsNullOrWhiteSpace(line))
                {
                    break;
                }

                while (lineNumber < _answerBody.Count && !string.IsNullOrWhiteSpace(_answerBody[lineNumber]) && Char.IsWhiteSpace(_answerBody[lineNumber][0]))
                {
                    line += " " + _answerBody[lineNumber++].Trim();
                }

                pos = line.IndexOf(':');
                if (pos <= 0)
                {
                    continue;
                }

                var headerKey = line.Substring(0, pos).Trim().ToLower();
                var headerValue = line.Substring(pos + 1).Trim();

                this.LogDebug("Header key: '" + headerKey + "', value '" + headerValue + "'");

                switch (headerKey)
                {
                    case "content-type":
                        contentType = headerValue;
                        break;
                    case "content-transfer-encoding":
                        contentTransferEncoding = headerValue;
                        break;
                }
            }

            pos = contentType.IndexOf(';');
            if (pos > 0)
            {
                var parameters = contentType.Substring(pos + 1);
                contentType = contentType.Substring(0, pos).Trim();

                foreach (var parameter in parameters.Split(new[] { ';' }))
                {
                    pos = parameter.IndexOf('=');
                    if (pos <= 0)
                    {
                        continue;
                    }
                    var parameterName = parameter.Substring(0, pos).Trim().ToLower();
                    var parameterValue = parameter.Substring(pos + 1).Trim();
                    if (parameterValue.Length >= 2 && parameterValue.StartsWith("\"") && parameterValue.EndsWith("\""))
                    {
                        parameterValue = parameterValue.Substring(1, parameterValue.Length - 2).Trim();
                    }
                    this.LogDebug("Content Type parameter key: '" + parameterName + "', value '" + parameterValue + "'");
                    switch (parameterName)
                    {
                        case "boundary":
                            contentBoundary = parameterValue;
                            break;
                        case "charset":
                            charset = parameterValue;
                            break;
                        case "name":
                            fileName = parameterValue;
                            break;
                    }
                }
            }

            contentType = contentType.ToLower();

            this.LogDebug("Content Type: " + contentType);

            switch (contentType)
            {
                case "text/html":
                    ProcessHtmlMessage(message, _answerBody, lineNumber, endLine - lineNumber, charset, contentTransferEncoding);
                    break;
                case "text/plain":
                    ProcessPlainTextMessage(message, _answerBody, lineNumber, endLine - lineNumber, charset, contentTransferEncoding);
                    break;
                default:
                    if (contentType.StartsWith("multipart/") && !string.IsNullOrWhiteSpace(contentBoundary))
                    {
                        ProcessMultipartMessage(message, lineNumber, endLine - lineNumber, contentBoundary, charset);
                        break;
                    }
                    if (contentType.StartsWith("image/") ||
                        contentType.StartsWith("text/") ||
                        contentType.StartsWith("application/") ||
                        contentType.StartsWith("video/") ||
                        contentType.StartsWith("audio/"))
                    {
                        ProcessAttachment(message, lineNumber, endLine - lineNumber, fileName, contentType, contentTransferEncoding);
                        break;
                    }
                    this.LogDebug("Unknown multipart item content type: " + contentType);
                    break;
            }
        }

        private void ProcessAttachment(Pop3Message message, int startLine, int lineCount,
            string fileName, string contentType, string contentEncoding)
        {
            if (contentEncoding.ToLower() != "base64")
            {
                return;
            }

            var sb = new StringBuilder();

            var endLine = startLine + lineCount;
            for (int i = startLine; i < endLine; i++)
            {
                sb.AppendLine(_answerBody[i]);
            }

            var attachment = new MessageAttachment
            {
                Body = Convert.FromBase64String(sb.ToString()),
                FileName = fileName,
                ContentType = contentType
            };

            message.Attachments.Add(attachment);
        }

        private string ReadNextLine(ref int readPos)
        {
            var length = _bufferBuilder.Length-1;
            var buffer = _bufferBuilder.Data;

            for (int pos = readPos; pos < length; pos++)
            {
                var ch1 = buffer[pos];
                var ch2 = buffer[pos + 1];

                if (pos == readPos && ch1 == 0)
                {
                    readPos++;
                    continue;
                }

                if ((ch1 == '\r' && ch2 == '\n') || (ch1 == '\n' && ch2 == '\r'))
                {
                    var ret = Encoding.GetEncoding(1252).GetString(buffer, readPos, pos-readPos);
                    readPos = pos + 2;
                    return ret;
                }
            }

            return null;
        }

        private void HandleReceivedData()
        {
            if (_answerHeader != null)
            {
                // Answer is not yet processed
                return;
            }

            int readPos = 0;

            var headerLine = ReadNextLine(ref readPos);

            if (string.IsNullOrWhiteSpace(headerLine))
            {
                return;
            }

            if (!headerLine.StartsWith("+OK") && !headerLine.StartsWith("-ERR"))
            {
                _state = StateMachine.Quit;
                return;
            }

            if (!_waitMultilineAnswer)
            {
                _answerHeader = headerLine;
                _bufferBuilder.Remove(readPos);
                return;
            }

            _answerBody.Clear();

            for (;;)
            {
                var nextLine = ReadNextLine(ref readPos);
                if (nextLine == null)
                {
                    _answerBody.Clear();
                    return;
                }

                if (!nextLine.StartsWith("."))
                {
                    _answerBody.Add(nextLine);
                    continue;
                }

                if (nextLine == ".")
                {
                    break;
                }

                _answerBody.Add(nextLine.Substring(1));
            }

            _answerHeader = headerLine;
            _bufferBuilder.Remove(readPos);
        }

        private void ParseMessageNumbers()
        {
            _pendingMessages.Clear();
            foreach (var messageLine in _answerBody)
            {
                var lineParts = messageLine.Split(new[] {' '});
                if (lineParts.Length != 2)
                {
                    continue;
                }
                int number, size;
                if (!int.TryParse(lineParts[0], out number) || !int.TryParse(lineParts[1], out size))
                {
                    continue;
                }
                this.LogDebug("Message number " + number + ", size " + size + " bytes");
                _pendingMessages.Add(Tuple.Create(number, size));
            }
        }

        private void SendCommand(string command, bool waitMultilineAnswer)
        {
            var bytes = Encoding.ASCII.GetBytes(command + "\r\n");
            _stream.Write(bytes, 0, bytes.Length);
            _waitMultilineAnswer = waitMultilineAnswer;
        }
    }
}
