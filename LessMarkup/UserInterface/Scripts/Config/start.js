/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

define(['require', 'app', 'controllers/main'], function (require, app, main) {
    require(['domready!'], function(document) {
        angular.bootstrap(document, ['application']);
    });
});