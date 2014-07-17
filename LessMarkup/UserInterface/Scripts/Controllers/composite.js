define(['directives/composite'], function() {
    app.controller('composite', function($scope) {
        $scope.elements = $scope.viewData.Elements;

        $scope.executeAction = function(action) {
            $scope.sendCommand(action, {}, function() {}, function() {});
        }
    });
});