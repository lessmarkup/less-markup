/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

window.app = angular.module('application', ['ui.bootstrap', 'angularSpinner', 'ui.scrollfix']);

window.app.ensureModule = function(name) {
    if (window.app.requires.indexOf(name) >= 0) {
        return;
    }

    window.app.requires.push(name);
}

window.app.directive('a', function () {
    return {
        restrict: 'E',
        link: function (scope, elem, attrs) {
            if (attrs.ngClick || attrs.href === '' || attrs.href === '#') {
                elem.on('click', function (e) {
                    e.preventDefault();
                });
            }
        }
    };
});

$(window).resize(function () {
    applyHeaderHeight();
});

window.getHeaderHeight = function() {
    return $("#header").height();
}

window.applyHeaderHeight = function() {
    $(".header-placeholder").height(getHeaderHeight());
}

applyHeaderHeight();
