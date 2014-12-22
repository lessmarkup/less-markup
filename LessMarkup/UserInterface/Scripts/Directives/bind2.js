/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

app.directive("bind2", function ($compile, lazyLoad) {
    return {
        scope: {
            func: '=bind2'
        },
        template: '',
        link: function (scope, element) {
            scope.func(function(text, s) {
                element.contents().remove();
                if (text) {
                    lazyLoad.loadModules();
                    element.append($compile(text)(s));
                }
            });
        }
    };
});
