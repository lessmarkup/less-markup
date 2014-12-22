/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

app.directive("bindCompiledHtml", function ($compile, lazyLoad) {
    return {
        template: '<div></div>',
        scope: {
            parameter: '=bindCompiledHtml',
        },
        link: function (scope, element) {

            var scopeFunction = scope.parameter.scope;
            if (typeof scopeFunction == "string") {
                var s = scope;
                while (s.$parent) {
                    s = s.$parent;
                    if (s[scopeFunction]) {
                        scopeFunction = s[scopeFunction];
                        break;
                    }
                }
            }

            var applyFunction = function (value) {
                element.contents().remove();
                if (value) {
                    lazyLoad.loadModules();
                    element.append($compile(value)(scopeFunction(scope.parameter.context)));
                }
            };
            scopeFunction(scope.parameter.context)[scope.parameter.name] = applyFunction;
            if (scope.parameter.html && scope.parameter.html != null && scope.parameter.html.length > 0) {
                applyFunction(scope.parameter.html);
            }
        }
    };
});
