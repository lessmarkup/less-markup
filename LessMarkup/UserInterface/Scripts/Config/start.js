define(['require', 'app', 'controllers/main'], function(require, app, main) {
    require(['domready!'], function(document) {
        angular.bootstrap(document, ['application']);
    });
});