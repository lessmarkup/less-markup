/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

getApplication().directive("scrollspySide", function($timeout) {
    return {
        link: function (scope, element) {
            $timeout(function () {

                var headerHeight = getHeaderHeight();

                element = $(element);

                element.affix({
                    offset: {
                        top: headerHeight
                    }
                });

                var body = $("body");

                body.scrollspy({
                    target: ".scrollspy",
                    offset: headerHeight + 80
                });
            });
        }
    }
});

getApplication().directive("scrollspyTop", function($timeout) {
    return {
        scope: true,
        link: function (scope, element) {
            $timeout(function() {
                var headerHeight = getHeaderHeight();
                var children = $(element).detach().insertAfter("#navbar-menu").addClass("scrollspy");

                $("body").scrollspy({
                    target: ".scrollspy",
                    offset: headerHeight + 80
                });

                //$("body").scrollspy("refresh");

                scope.$on("onNodeLoaded", function() {
                    children.remove();
                });
            });
        }
    }
});

getApplication().controller("flatpage", function($scope, $rootScope) {
    $scope.flat = $scope.viewData.Flat;
    $scope.tree = $scope.viewData.Tree;
    $scope.position = $scope.viewData.Position;

    var pageToScope = {};

    $scope.getPageScope = function(page) {
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

    for (var i = 0; i < $scope.flat.length; i++) {
        var page = $scope.flat[i];
        var pageScope = $scope.$new();
        initializePageScope(pageScope, page);
        pageToScope[page.UniqueId] = pageScope;
        pageScope.viewData = page.ViewData;
    }

    $rootScope.scrollToPage = function (anchor) {
        var position = $("#" + anchor).offset().top;
        var headerHeight = getHeaderHeight();
        $("body").scrollTop(position - headerHeight - 10);
    }
});
