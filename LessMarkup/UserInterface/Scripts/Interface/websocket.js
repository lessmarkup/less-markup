/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

function uintToString(uintArray) {
    return decodeURIComponent(escape(String.fromCharCode.apply(null, uintArray)));
}

function isWebSocketAvailable() {
    try {
        var websocket = new WebSocket("ws:websocket.org");
        websocket.close('');
    } catch (e) {
        return true;
    }
    return false;
}

function WebSocketConnection(url) {

    var connection = this;

    connection.onOpen = function () { }

    connection.onClose = function () { }

    connection.onError = function () { }

    this.open = function () {
        try {
            connection.webSocket = new WebSocket(url);
            connection.webSocket.binaryType = "arraybuffer";
            connection.webSocket.onmessage = function (event) {
                if (typeof (event.data) == "string") {
                    var pos = event.data.indexOf(';');
                    if (pos <= 0) {
                        return;
                    }
                    var methodName = event.data.substr(0, pos).toLowerCase();
                    pos++;
                    var parameters = pos < event.data.length ? JSON.parse(event.data.substr(pos)) : null;
                    for (var param in connection) {
                        if (param.toLowerCase() == methodName) {
                            methodName = param;
                            break;
                        }
                    }
                    var func = connection[methodName];
                    if (typeof (func) == "function") {
                        func(parameters);
                    }
                } else {
                    // here we work with binary data
                    var byteArray = new Uint8Array(event.data);
                    var methodNameLength = new Uint32Array(byteArray.buffer.slice(0, 4))[0];
                    var valuesLength = new Uint32Array(byteArray.buffer.slice(4, 8))[0];
                    var methodName = uintToString(new Uint8Array(byteArray.buffer.slice(8, methodNameLength + 8))).toLowerCase();
                    var parametersText = "";
                    var parameters = null;
                    if (valuesLength > 0) {
                        parametersText = uintToString(new Uint8Array(byteArray.buffer.slice(8 + methodNameLength, 8 + methodNameLength + valuesLength)));
                        parameters = JSON.parse(parametersText);
                    }
                    for (var param in connection) {
                        if (param.toLowerCase() == methodName) {
                            methodName = param;
                            break;
                        }
                    }
                    var func = connection[methodName];
                    if (typeof (func) == "function") {
                        func(byteArray, 8 + methodNameLength + valuesLength, byteArray.length - 8 - methodNameLength - valuesLength, parameters);
                    }
                }
            }

            connection.webSocket.onopen = function (event) {
                connection.onOpen(event);
            }

            connection.webSocket.onerror = function (event) {
                connection.onError();
            }

            connection.webSocket.onclose = function () {
                connection.onClose();
            }

        } catch (exception) {
            connection.onError();
        }
    }

    connection.sendRequest = function (methodName, parameters) {
        var parametersText = "";
        if (parameters != null) {
            parametersText = JSON.stringify(parameters);
        }
        var fullText = methodName + ";" + parametersText;
        connection.webSocket.send(fullText);
    }

    this.close = function () {
        this.webSocket.close();
    }
}
