/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

function HtmlPageController($scope) {
    $scope.htmlBody = function(apply) {
        apply($scope.viewData.body, $scope);
    }

    if ($scope.viewData.code) {
        new Function("$scope", $scope.viewData.code)($scope);
    }
}

app.controller("htmlpage", HtmlPageController);
