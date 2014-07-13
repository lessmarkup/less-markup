/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

define([
    'app',
    'controllers/inputform',
    'lib/codemirror/codemirror',
    'lib/codemirror/ui-codemirror',
    'lib/tinymce/tinymce',
    'lib/tinymce/config',
    'lib/tinymce/tinymce-angular'
], function (app, inputform) {

    var controllerFunction = function($http, $scope, $timeout) {

        $scope.definition = $scope.viewData.Definition;
        $scope.object = $scope.viewData.Object;
        $scope.submitError = "";
        $scope.submitSuccess = "";
        $scope.applyCaption = $scope.viewData.ApplyCaption;
        $scope.changesApplied = false;

        $scope.openForm = function() {
            $scope.changesApplied = false;
            $scope.submitError = "";
            $scope.submitSuccess = "";
            if (!$scope.$$phase) {
                $scope.$apply();
            }
        }

        inputform($scope, null, $scope.definition, $scope.object, function(changedObject, success, fail) {
            if (!$scope.$$phase) {
                $scope.$apply();
            }
            $scope.sendAction("Save", {
                "changedObject": changedObject
            }, function(data) {
                $scope.hasChanges = false;
                $scope.changesApplied = true;
                $scope.submitSuccess = data;
                success();
            }, function(message) {
                fail(message);
            });
        });

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

    app.controller("dialog", controllerFunction);

    return controllerFunction;
});
