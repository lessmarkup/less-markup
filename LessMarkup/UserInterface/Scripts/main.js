/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

require.config({
    //baseUrl: "/scripts",

    paths: {
        'domready': 'lib/require/domready',
        'app': 'config/app',
        'angular': 'lib/angular/angular',
        'bootstrap': 'lib/bootstrap/bootstrap',
        'bootstrap-ui': 'lib/bootstrap/ui-bootstrap',
        'jquery': 'lib/jquery/jquery-2.1.1',
        'jquery-ui': 'lib/jquery/jquery-ui-1.10.4',
    },

    shim: {
        'angular': ['jquery'],
        'jquery-ui': ['jquery'],
        'bootstrap': ['jquery', 'jquery-ui'],
        'bootstrap-ui': ['angular', 'bootstrap'],
        'lib/spinner/spin': ['jquery-ui'],
        'lib/spinner/angular-spinner': ['angular'],
        'lib/tinymce/tinymce-angular': ['angular', 'lib/tinymce/config']
    },

    deps: [
        'config/start',
        'lib/spinner/spin',
        'lib/spinner/angular-spinner'
    ]
});
