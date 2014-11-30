/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

(function() {
    var scrollSpyId = "";
    var scrollSetManually = false;

    function onScrollChanged() {
        if (scrollSetManually) {
            return;
        }

        var headerHeight = getHeaderHeight();

        var windowHeight = $(window).height();

        var scrollPosition = $(window).scrollTop();

        var selectedId = "";

        var visibleElements = [];

        $('.spyelement').each(function (i, el) {
            var position = $(el).offset().top - headerHeight - 10 - scrollPosition;

            if (visibleElements.length > 0) {
                var previous = visibleElements[visibleElements.length - 1];
                previous.height = position - previous.position;
            }

            visibleElements.push({
                position: position,
                id: $(el).attr("id"),
                height: windowHeight - headerHeight - 10 - position
            });
        });

        if (visibleElements.length == 0) {
            return;
        }

        if (scrollPosition == $("html")[0].scrollHeight - $(window).height()) {
            selectedId = visibleElements[visibleElements.length - 1].id;
        } else {
            var maxVisibleHeight = 0;
            var maxVisibleElement = null;
            var topVisibleElement = null;

            angular.forEach(visibleElements, function (element) {

                if (element.position >= windowHeight) {
                    return;
                }

                var visibleHeight = element.height;
                if (element.position < 0) {
                    visibleHeight += element.position;
                }
                if (element.position + element.height > windowHeight) {
                    visibleHeight -= element.position + element.height - windowHeight;
                }

                if (visibleHeight > maxVisibleHeight) {
                    maxVisibleElement = element;
                    maxVisibleHeight = visibleHeight;
                }

                if (topVisibleElement == null && visibleHeight > 20) {
                    topVisibleElement = element;
                }
            });

            if (maxVisibleHeight >= windowHeight / 2) {
                selectedId = maxVisibleElement.id;
            } else if (topVisibleElement != null) {
                selectedId = topVisibleElement.id;
            }
        }

        if (selectedId == scrollSpyId) {
            return;
        }

        //console.log("checkScrollChanged");

        setScrollSpyId(selectedId);
    }

    function setScrollSpyId(id) {
        scrollSpyId = id;

        var selectedRef = null;

        $('.spyref').each(function (i, el) {
            var anchor = $(el).data('anchor');
            if (!anchor || anchor.length == 0) {
                return;
            }
            if (anchor == scrollSpyId) {
                selectedRef = $(el);
            }
            $(el).removeClass('active');
        });

        if (selectedRef == null) {
            return;
        }

        selectedRef.addClass('active');

        selectedRef.parents('.spyref').each(function (i, el) {
            $(el).addClass('active');
        });
    }

    function enableScrollSpy() {
        $(window).on('scroll', onScrollChanged);
        $(window).on('resize', onScrollChanged);
        scrollSpyId = "";
        onScrollChanged();
    }

    function disableScrollSpy() {
        $(window).off('scroll', onScrollChanged);
        $(window).off('resize', onScrollChanged);
        scrollSpyId = "";
    }

    app.directive("scrollspySide", function ($timeout) {
        return {
            link: function (scope) {
                $timeout(function () {
                    enableScrollSpy();

                    scope.$on("onNodeLoaded", function () {
                        disableScrollSpy();
                    });
                });
            }
        }
    });

    app.directive("scrollspyTop", function ($timeout) {
        return {
            scope: true,
            link: function (scope, element) {
                $timeout(function () {

                    var navbarMenu = $("#navbar-menu");

                    if (!navbarMenu.is(":visible")) {
                        navbarMenu = $("#navbar-menu-small");
                    }

                    var children = $(element).detach().insertAfter(navbarMenu).addClass("scrollspy");

                    enableScrollSpy();

                    scope.$on("onNodeLoaded", function () {
                        children.remove();
                        disableScrollSpy();
                    });
                });
            }
        }
    });

    app.controller("flatpage", function ($scope, $rootScope) {
        var pageToScope = {};

        $scope.getPageScope = function (page) {
            return pageToScope[page.uniqueId];
        }

        function initializePageScope(scope, page) {
            scope.sendCommand = function (action, data, success, failure, path) {
                if (!path) {
                    path = scope.Path;
                }
                data.flatNodeId = scope.nodeId;
                return $scope.sendCommand(action, data, success, failure, path);
            }

            scope.navigateToView = function(path) {
                return $scope.navigateToView(path);
            }

            scope.toolbarButtons = [];
            scope.path = page.path;
            scope.nodeId = page.nodeId;

            scope.getFullPath = function(path) {
                return scope.path + "/" + path;
            }
        }

        $rootScope.scrollToPage = function (anchor, exact) {

            $scope.hideXsMenu();

            var position = $("#" + anchor).offset().top;
            var headerHeight = getHeaderHeight();

            if (!exact) {
                position -= headerHeight;
                position -= 10;
            }

            setScrollSpyId(anchor);

            scrollSetManually = true;

            $("html,body").stop().animate({ scrollTop: position }, 1000, "swing", function () {
                scrollSetManually = false;
            });
        }

        function initializePages() {
            $scope.flat = $scope.viewData.flat;
            $scope.tree = $scope.viewData.tree;
            $scope.position = $scope.viewData.position;

            for (var i = 0; i < $scope.flat.length; i++) {
                var page = $scope.flat[i];
                var pageScope = $scope.$new();
                initializePageScope(pageScope, page);
                pageToScope[page.uniqueId] = pageScope;
                pageScope.viewData = page.viewData;
            }

            if (!$scope.$$phase) {
                $scope.$apply();
            }
        }

        if ($scope.viewData.scripts.length > 0) {
            require($scope.viewData.scripts, initializePages);
        } else {
            initializePages();
        }

    });
})();
