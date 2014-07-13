define(['app'], function (app) {

    app.provider('invokeQueue', [
        '$controllerProvider', '$provide', '$compileProvider', '$filterProvider', function($controllerProvider, $provide, $compileProvider, $filterProvider) {
            var providers = {
                $controllerProvider: $controllerProvider,
                $compileProvider: $compileProvider,
                $filterProvider: $filterProvider,
                $provide: $provide
            }

            this.$get = [
                function () {
                    function runModuleInvokeQueue(module) {
                        if (!module.hasOwnProperty("_invokeQueue")) {
                            return;
                        }
                        var invokeQueue = module._invokeQueue;
                        for (var i = 0, ii = invokeQueue.length; i < ii; i++) {
                            var invokeArgs = invokeQueue[i];

                            if (providers.hasOwnProperty(invokeArgs[0])) {
                                var provider = providers[invokeArgs[0]];
                                provider[invokeArgs[1]].apply(provider, invokeArgs[2]);
                            }
                        }
                    }
                    return {
                        runInvokeQueue: function() {
                            runModuleInvokeQueue(app);
                            angular.forEach(app.requires, function (module) {
                                runModuleInvokeQueue(module);
                            });
                        }
                    }
                }
            ];
        }
    ]);

    app.directive("bindCompiledHtml", function ($compile, invokeQueue) {
        return {
            template: '<div></div>',
            scope: {
                parameter: '=bindCompiledHtml',
            },
            link: function (scope, element) {
                var applyFunction = function (value) {
                    element.contents().remove();
                    if (value) {
                        invokeQueue.runInvokeQueue();
                        element.append($compile(value)(scope.parameter.scope(scope.parameter.context)));
                    }
                };
                scope.parameter.scope(scope.parameter.context)[scope.parameter.name] = applyFunction;
                if (scope.parameter.html && scope.parameter.html != null && scope.parameter.html.length > 0) {
                    applyFunction(scope.parameter.html);
                }
            }
        };
    });
});
