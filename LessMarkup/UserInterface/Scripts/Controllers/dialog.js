define(['app', 'controllers/inputform'], function (app, inputform) {

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

        var ret = inputform($scope, null, $scope.definition, $scope.object, function(changedObject, success, fail) {
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

        return ret;
    }

    app.controller("dialog", controllerFunction);

    return controllerFunction;
});
