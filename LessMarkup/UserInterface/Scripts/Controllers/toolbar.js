/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

define([], function() {
    app.controller('toolbar', function ($scope, commandHandler) {
        $scope.hasCommand = function (command) {
            return commandHandler.isSubscribed(command);
        }
        $scope.invokeCommand = function (command) {
            commandHandler.invoke(command, this);
        }
        $scope.isEnabled = function (command) {
            return commandHandler.isEnabled(command, this);
        }
        //$scope.elements = toolbarConfiguration.Elements;
    });
});

