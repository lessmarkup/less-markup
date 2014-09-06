/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

function DialogController($http, $scope, $timeout, lazyLoad, $sce) {

    $scope.definition = $scope.viewData.definition;
    $scope.object = $scope.viewData.object;
    $scope.submitError = "";
    $scope.submitSuccess = "";
    $scope.applyCaption = $scope.viewData.applyCaption;
    $scope.changesApplied = false;

    $scope.openForm = function() {
        $scope.changesApplied = false;
        $scope.submitError = "";
        $scope.submitSuccess = "";
        if (!$scope.$$phase) {
            $scope.$apply();
        }
    }

    var successFunction = function(changedObject, success, fail) {
        if (!$scope.$$phase) {
            $scope.$apply();
        }
        $scope.sendCommand("Save", {
            "changedObject": changedObject
        }, function(data) {
            $scope.hasChanges = false;
            $scope.changesApplied = true;
            $scope.submitSuccess = data;
            success();
        }, function(message) {
            fail(message);
        });
    }

    InputFormController($scope, null, $scope.definition, $scope.object, successFunction, $scope.getTypeahead, $sce);

    $scope.hasChanges = false;

    $scope.$watch("object", function() {
        $scope.hasChanges = true;
        $scope.resetAlerts();
        $scope.submitError = "";
        $scope.submitSuccess = "";
    }, true);

    $timeout(function() {
        $scope.hasChanges = false;
    });
}

app.controller("dialog", DialogController);
