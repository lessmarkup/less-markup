/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

var scrollChanged = false;
var timerId;
var scrollSpyId = "";
var scrollSetManually = false;

function onScrollChanged() {
    if (scrollSetManually) {
        scrollSetManually = false;
        return;
    }
    scrollChanged = true;
    console.log("onScrollChanged");
}

function getScrollSpyId() {
    return scrollSpyId;
}

function setScrollSpyId(id) {
    console.log("setScrollSpy");
    scrollSpyId = id;
    scrollChanged = false;

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

function checkScrollChanged() {
    if (!scrollChanged) {
        return;
    }

    scrollChanged = false;

    var headerHeight = getHeaderHeight();

    var windowHeight = $(window).height();

    var validRange = (windowHeight - headerHeight - 10) / 2;

    var scrollPosition = $(window).scrollTop();

    var minDistance = -validRange;
    var minId = "";

    var reserveId = "";

    $('.spyelement').each(function(i, el) {
        var position = $(el).offset().top - headerHeight + 20 - scrollPosition;

        if (position > validRange) {
            reserveId = $(el).attr('id');
            return;
        }

        if (minDistance < 0) {
            if (position >= 0 || position > -windowHeight) {
                minDistance = position;
                minId = $(el).attr('id');
            } else {
                reserveId = $(el).attr('id');
            }
            return;
        }

        if (position < 0) {
            return;
        }

        if (position < minDistance) {
            minDistance = position;
            minId = $(el).attr('id');
        }
    });

    if (minId.length == 0) {
        minId = reserveId;
    }

    if (minId == scrollSpyId) {
        return;
    }

    console.log("checkScrollChanged");

    setScrollSpyId(minId);
}

function enableScrollSpy() {
    $(window).on('scroll', onScrollChanged);
    timerId = setInterval(checkScrollChanged, 100);
    scrollSpyId = "";
    scrollChanged = true;
}

function disableScrollSpy() {
    $(window).off('scroll', onScrollChanged);
    clearInterval(timerId);
    scrollSpyId = "";
    scrollChanged = true;
}

getApplication().directive("scrollspySide", function($timeout) {
    return {
        link: function (scope, element) {
            $timeout(function () {

                enableScrollSpy();



                scope.$on("onNodeLoaded", function() {
                    disableScrollSpy();
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
                var children = $(element).detach().insertAfter("#navbar-menu").addClass("scrollspy");

                enableScrollSpy();

                scope.$on("onNodeLoaded", function() {
                    children.remove();
                    disableScrollSpy();
                });
            });
        }
    }
});

getApplication().controller("flatpage", function($scope, $rootScope, $timeout) {
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
        $(window).scrollTop(position - headerHeight - 10);
        setScrollSpyId(anchor);
        scrollSetManually = true;
    }
});
