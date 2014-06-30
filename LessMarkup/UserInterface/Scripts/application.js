/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

angular.module('application', ['ui.bootstrap', 'ui.tinymce', 'angularSpinner', 'ngGrid', 'ui.codemirror']);
function getApplication() {
    return angular.module('application');
}

function applyHeaderHeight() {
    $(".header-placeholder").height($(".navbar-inner").height());
}

getApplication().directive('a', function () {
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

getApplication().directive("fileread", [function () {
    return {
        scope: {
            fileread: "="
        },
        link: function (scope, element) {
            element.bind("change", function (changeEvent) {
                var reader = new FileReader();
                reader.onload = function (loadEvent) {
                    scope.$apply(function () {
                        scope.fileread = loadEvent.target.result;
                    });
                }
                reader.readAsDataURL(changeEvent.target.files[0]);
            });
        }
    }
}]);

applyHeaderHeight();

$(window).resize(function () {
    applyHeaderHeight();
});

function getHub() {
    return $.connection.recordListHub;
}

