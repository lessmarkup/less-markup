/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

app.provider('commandHandler', function() {
    var handlers = {};
    this.$get = [
        function() {
            return {
                subscribe: function(command, handler) {
                    var commandHandlers;
                    if (!handlers.hasOwnProperty(command)) {
                        commandHandlers = [];
                        handlers[command] = commandHandlers;
                    } else {
                        commandHandlers = handlers[command];
                    }
                    commandHandlers.push(handler);
                },
                unsubscribe: function(command, handler) {
                    if (!handlers.hasOwnProperty(command)) {
                        return;
                    }
                    var commandHandlers = handlers[command];
                    var index = commandHandlers.indexOf(handler);
                    commandHandlers.slice(index, 1);
                },
                invoke: function(command, sender) {
                    if (!handlers.hasOwnProperty(command)) {
                        return;
                    }
                    var commandHandlers = handlers[command];
                    for (var i = 0; i < commandHandlers.length; i++) {
                        commandHandlers[i](sender, true);
                    }
                },
                isSubscribed: function(command) {
                    return handlers.hasOwnProperty(command);
                },
                isEnabled: function(command, sender) {
                    if (!handlers.hasOwnProperty(command)) {
                        return false;
                    }
                    var commandHandlers = handlers[command];
                    for (var i = 0; i < commandHandlers.length; i++) {
                        if (commandHandlers[i](sender, false)) {
                            return true;
                        }
                    }
                    return false;
                },
                reset: function() {
                    handlers = {};
                }
            };
        }
    ];
});
