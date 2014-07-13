/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

define(['app'], function(app) {
    var controllerFunction = function ($scope) {
        var pageToScope = {};

        $scope.getPageScope = function (page) {
            return pageToScope[page.UniqueId];
        }

        function initializePageScope(scope, page) {
            scope.sendAction = function (action, data, success, failure, path) {
                if (!path) {
                    path = page.Path;
                }
                return $scope.sendAction(action, data, success, failure, path);
            }

            scope.sendCommand = function (command, data, success, failure, path) {
                if (!path) {
                    path = page.Path;
                }
                return $scope.sendCommand(action, data, success, failure, path);
            }

            scope.sendActionAsync = function (action, data, success, failure, path) {
                if (!path) {
                    path = page.Path;
                }
                return $scope.sendActionAsync(action, data, success, failure, path);
            }

            scope.sendCommandAsync = function (command, data, success, failure, path) {
                if (!path) {
                    path = page.Path;
                }
                return $scope.sendCommandAsync(action, data, success, failure, path);
            }

            scope.toolbarButtons = [];
            scope.path = page.path;
        }

        function loadPages() {
            $scope.pages = $scope.viewData.Pages;
            $scope.activePage = $scope.pages.length > 0 ? $scope.pages[0] : null;

            for (var i = 0; i < $scope.pages.length; i++) {
                var page = $scope.pages[i];
                var pageScope = $scope.$new();
                initializePageScope(pageScope, page);
                pageToScope[page.UniqueId] = pageScope;
                pageScope.viewData = page.ViewData;
            }

            if (!$scope.$$phase) {
                $scope.$apply();
            }
        }

        if ($scope.viewData.Requires.length > 0) {
            require($scope.viewData.Requires, loadPages);
        } else {
            loadPages();
        }

    }

    app.controller("tabpage", controllerFunction);

    return controllerFunction;
});
