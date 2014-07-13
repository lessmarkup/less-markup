/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

define(['app', 'providers/invokequeue'], function (app) {

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
