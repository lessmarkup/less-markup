/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

var scrollSpyId = "";
var scrollSetManually = false;

function onScrollChanged() {
    if (scrollSetManually) {
        scrollSetManually = false;
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

    if (scrollPosition == windowHeight) {
        selectedId = visibleElements[visibleElements.length - 1].id;
    } else {
        var maxVisibleHeight = 0;
        var maxVisibleElement = null;
        var topVisibleElement = null;

        angular.forEach(visibleElements, function(element) {

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
                var children = $(element).detach().insertAfter("#navbar-menu").addClass("scrollspy");

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

    $rootScope.scrollToPage = function (anchor) {
        var position = $("#" + anchor).offset().top;
        var headerHeight = getHeaderHeight();
        $(window).scrollTop(position - headerHeight - 10);
        setScrollSpyId(anchor);
        scrollSetManually = true;
    }

    function initializePages() {
        $scope.flat = $scope.viewData.Flat;
        $scope.tree = $scope.viewData.Tree;
        $scope.position = $scope.viewData.Position;

        for (var i = 0; i < $scope.flat.length; i++) {
            var page = $scope.flat[i];
            var pageScope = $scope.$new();
            initializePageScope(pageScope, page);
            pageToScope[page.UniqueId] = pageScope;
            pageScope.viewData = page.ViewData;
        }

        if (!$scope.$$phase) {
            $scope.$apply();
        }
    }

    if ($scope.viewData.Scripts.length > 0) {
        require($scope.viewData.Scripts, initializePages);
    } else {
        initializePages();
    }

});
