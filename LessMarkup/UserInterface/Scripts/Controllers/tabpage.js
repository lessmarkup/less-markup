/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

define([], function() {
    var controllerFunction = function ($scope) {
        var pageToScope = {};

        $scope.getPageScope = function (page) {
            return pageToScope[page.uniqueId];
        }

        function initializePageScope(scope, page) {
            scope.sendCommand = function (action, data, success, failure, path) {
                if (!path) {
                    path = page.path;
                }
                return $scope.sendCommand(action, data, success, failure, path);
            }

            scope.toolbarButtons = [];
            scope.path = page.path;
        }

        function loadPages() {
            $scope.pages = $scope.viewData.pages;
            $scope.activePage = $scope.pages.length > 0 ? $scope.pages[0] : null;

            for (var i = 0; i < $scope.pages.length; i++) {
                var page = $scope.pages[i];
                var pageScope = $scope.$new();
                initializePageScope(pageScope, page);
                pageToScope[page.uniqueId] = pageScope;
                pageScope.viewData = page.viewData;
            }

            if (!$scope.$$phase) {
                $scope.$apply();
            }
        }

        if ($scope.viewData.requires.length > 0) {
            require($scope.viewData.requires, loadPages);
        } else {
            loadPages();
        }

    }

    app.controller("tabpage", controllerFunction);

    return controllerFunction;
});
