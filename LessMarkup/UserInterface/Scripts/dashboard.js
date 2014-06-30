/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

getApplication().controller('dashboard', function($scope, $http, $sce) {
    $scope.items = window.dashboardInitialData;

    for (var i = 0; i < $scope.items.length; i++) {
        $scope.items[i].isActive = i == 0;
    }

    $scope.caption = $scope.items[0].Text;
    $scope.contents = '';

    $scope.selectItem = function(item) {
        for (var i = 0; i < $scope.items.length; i++) {
            $scope.items[i].isActive = false;
        }

        $scope.contents = '';
        item.isActive = true;
        $scope.caption = item.Text;
        $scope.$apply();

        $http.post("", {
            command: "ShowPage",
            pageId: item.Id
        }).success(function(data) {
            $scope.contents = $sce.trustAsHtml(data);
            $scope.$apply();
        });
    }
});
