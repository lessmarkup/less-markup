/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

getApplication().directive("affix", function() {
    return {
        link: function(scope, element) {
            $(element).affix({
                offset: {
                    top: $("#header").height() + 10
                }
            });
        }
    }
});

getApplication().controller("flatpage", function($scope) {
    $scope.flat = $scope.viewData.Flat;
    $scope.tree = $scope.viewData.Tree;

    $scope.getPageScope = function(page) {
        if (!page.scope) {
            page.scope = $scope.$new();
        }
        return page.scope;
    }

    $scope.$on('$viewContentLoaded', function() {
        for (var i = 0; i < $scope.flat.length; i++) {
            var page = $scope.flat[i];

            var childScope = $scope.$new();
            childScope.viewData = page.ViewData;

        }
    });
});