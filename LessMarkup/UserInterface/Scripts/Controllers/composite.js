define(['directives/composite'], function() {
    app.controller('composite', function($scope) {
        $scope.elements = $scope.viewData.elements;

        $scope.executeAction = function(action) {
            $scope.sendCommand(action, {}, function() {}, function() {});
        }
    });
});