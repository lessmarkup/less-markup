define([], function() {
    app.directive("genericElement", function ($compile) {
        return {
            template: "<div></div>",
            scope: { element: '=genericElement' },
            link: function (scope, element) {
                var parameterId = 0;
                function getElementContents(element) {
                    switch (element.Type) {
                        case "Text":
                            parameterId++;
                            scope["parameter" + parameterId.toString()] = element.Text;
                            return "<span ng-bind='parameter" + parameterId.toString() + "'></span>";
                        case "Button":
                            parameterId++;
                            scope["parameter" + parameterId.toString()] = element.Text;
                            return "<button style='btn btn-default' ng-click='executeAction(" + element.Action + ")' ng-bind='parameter"+ parameterId.toString() + "'></button>";
                        case "Container":
                            var ret = "<div>";
                            angular.forEach(element.Elements, function(element) {
                                ret += getElementContents(element);
                            });
                            ret += "</div>";
                            return ret;
                    }
                }

                element.contents().remove();
                var contents = getElementContents(scope.element);
                element.append($compile(contents)(scope));
            }
        }
    });
});