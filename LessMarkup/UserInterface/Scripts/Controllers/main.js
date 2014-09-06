/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

function scopeProperty(scope, name) {
    for (;;) {
        if (scope.hasOwnProperty("name")) {
            return scope.name;
        }
        if (!scope.hasOwnProperty("$parent")) {
            return undefined;
        }
        scope = scope.$parent;
    }
}

app.controller('main', function ($scope, $http, commandHandler, inputForm, $location, $browser, $timeout, lazyLoad, $sce) {
    var initialData = window.viewInitialData;
    window.viewInitialData = null;

    $scope.toolbarButtons = [];
    $scope.templates = {};
    $scope.title = "";
    $scope.breadcrumbs = [];
    $scope.viewData = null;
    $scope.staticNodes = {};
    $scope.path = "";
    $scope.loginUserEmail = "";
    $scope.loginUserPassword = "";
    $scope.loginUserRemember = false;
    $scope.loggedIn = initialData.loggedIn;
    $scope.userNotVerified = initialData.userNotVerified || false;
    $scope.userName = initialData.userName || "";
    $scope.userLoginError = "";
    $scope.userLoginProgress = false;
    $scope.alerts = [];
    $scope.hasLogin = initialData.hasLogin || false;
    $scope.hasSearch = initialData.hasSearch || false;
    $scope.showConfiguration = initialData.showConfiguration || false;
    $scope.configurationPath = initialData.configurationPath || "";
    $scope.rootPath = initialData.rootPath || "/";
    $scope.rootTitle = initialData.rootTitle || "Home";
    $scope.navigationTree = initialData.navigationTree || [];
    $scope.topMenu = initialData.topMenu || [];
    $scope.profilePath = initialData.profilePath || "";
    $scope.forgotPasswordPath = initialData.forgotPasswordPath || "";
    $scope.languages = initialData.languages || [];
    $scope.getViewScope = function () { return $scope; }
    $scope.showXsMenu = false;
    $scope.notifications = initialData.notifications || [];
    $scope.recaptchaPublicKey = initialData.recaptchaPublicKey || "";
    $scope.lastActivity = new Date().getDate() / 1000;
    $scope.title = $scope.rootTitle;
    $scope.loadingNewPage = true;
    $scope.searchText = "";
    $scope.searchResults = [];
    $scope.maximumFileSize = initialData.maximumFileSize || 1024;
    $scope.smiles = initialData.smiles || [];
    $scope.smilesBase = initialData.smilesBase || "";
    $scope.versionId = initialData.versionId || null;
    $scope.updateProperties = {};

    var searchTimeout = null;
    var pageProperties = {};

    $scope.selectedLanguage = null;

    for (var i = 0; i < $scope.languages.length; i++) {
        if ($scope.languages[i].selected) {
            $scope.selectedLanguage = $scope.languages[i];
            break;
        }
    }

    var smiles = {};
    var smilesStr = "";

    $scope.getSmileUrl = function(code) {
        if (!code.length || !$scope.smilesBase) {
            return "";
        }
        return "<img alt=\"" + code + "\" src=\"" + $scope.smilesBase + smiles[code] + "\" title=\"" + code + "\" />";
    }

    for (var i = 0; i < $scope.smiles.length; i++) {
        var smile = $scope.smiles[i];
        smiles[smile.code] = smile.id;
        if (smilesStr.length > 0) {
            smilesStr += "|";
        }
        for (var j = 0; j < smile.code.length; j++) {
            switch (smile.code[j]) {
                case '(':
                case ')':
                case '[':
                case ']':
                case '-':
                case '?':
                case '|':
                    smilesStr += '\\';
                    break;
            }
            smilesStr += smile.code[j];
        }
    }

    if (smilesStr.length > 0) {
        $scope.smilesExpr = new RegExp(smilesStr, "g");
    } else {
        $scope.smilesExpr = null;
    }

    $scope.smilesToImg = function(text) {
        if ($scope.smilesExpr != null) {
            text = text.replace(/([^<>]*)(<[^<>]*>)/gi, function (match, left, tag) {
                if (!left || left.length == 0) {
                    return match;
                }
                left = left.replace($scope.smilesExpr, $scope.getSmileUrl);
                return tag ? left + tag : left;
            });
        }
        return text;
    }

    $scope.getFriendlyHtml = function (text) {

        if (text == null || text.length == 0) {
            return text;
        }

        text = $scope.smilesToImg(text);

        return Autolinker.link(text, { truncate: 30 });
    }

    $scope.getScope = function () { return $scope; }

    $scope.clearSearch = function() {
        $scope.searchResults = [];
        $scope.searchText = "";
    }

    $scope.$watch("searchText", function () {
        if (searchTimeout != null) {
            $timeout.cancel(searchTimeout);
        }
        searchTimeout = $timeout($scope.search, 500);
    });

    $scope.search = function () {
        searchTimeout = null;
        var searchText = $scope.searchText.trim();
        if (searchText.length == 0) {
            $scope.searchResults = [];
            return;
        }
        $scope.sendCommand("searchText", {
            text: $scope.searchText
        }, function (data) {
            if (data != null && data.hasOwnProperty("results")) {
                $scope.searchResults = data.results;
                for (var i = 0; i < $scope.searchResults.length; i++) {
                    var result = $scope.searchResults[i];
                    result.text = result.text.replace(new RegExp(searchText, "gim"), "<span class=\"highlight\">$&</span>");
                    result.text = $sce.trustAsHtml(result.text);
                }
            } else {
                $scope.searchResults = [];
            }

            if (!$scope.$$phase) {
                $scope.$apply();
            }
        });
    }

    function resetPageProperties(currentLink) {
        pageProperties = {};

        if (!currentLink) {
            currentLink = window.location.href;
        }

        var queryPos = currentLink.indexOf('?');

        if (queryPos > 0) {
            var query = currentLink.substring(queryPos + 1, currentLink.length);
            var parameters = query.split('&');
            for (var i = 0; i < parameters.length; i++) {
                if (parameters[i].length == 0) {
                    continue;
                }
                var t = parameters[i].split('=');
                var name = t[0];
                var value = t.length > 0 ? t[1] : '';
                pageProperties[name] = value;
            }
        }
    }

    resetPageProperties();

    function updateNavigationTree() {
        for (var i = 0; i < $scope.navigationTree.length; i++) {
            var item = $scope.navigationTree[i];
            item.style = 'margin-left:' + (item.level).toString() + 'em;';
        }
    }

    updateNavigationTree();

    $scope.onUserActivity = function () {
        $scope.lastActivity = new Date().getDate() / 1000;
    }

    $scope.getDynamicDelay = function() {
        var activityDelayMin = (new Date().getDate() / 1000 - $scope.lastActivity) / 60;

        if (activityDelayMin < 2) {
            return 30;
        }

        if (activityDelayMin < 5) {
            return 60;
        }

        if (activityDelayMin < 10) {
            return 60 * 2;
        }

        return -1;
    }

    $scope.getPageProperty = function(name, defaultValue) {
        if (pageProperties.hasOwnProperty(name)) {
            return pageProperties[name];
        }
        return defaultValue;
    }

    function updatePageHistory() {
        var query = "";

        for (var property in pageProperties) {

            var value = pageProperties[property];

            if (value == null || value.length == 0) {
                continue;
            }

            if (query.length > 0) {
                query += "&";
            }
            query += property + "=" + value;
        }

        var newFullPath = window.location.protocol + "//" + window.location.host + $scope.path;

        if (query.length > 0) {
            newFullPath += "?" + query;
        }

        if (window.location.href != newFullPath) {
            history.pushState(newFullPath, $scope.title, newFullPath);
        }
    }

    $scope.setPageProperty = function (name, value) {

        if ($scope.getPageProperty(name, null) == value) {
            return;
        }

        pageProperties[name] = value;

        updatePageHistory();
    }

    $scope.gotoNotification = function (notification) {
        $scope.navigateToView(notification.path);
    }

    $scope.notificationClass = function (notification) {
        if (notification.count > 0) {
            return "active-notification";
        }
        return "";
    }

    var timeoutCancel = null;

    function cancelUpdates() {
        if (timeoutCancel != null) {
            $timeout.cancel(timeoutCancel);
            timeoutCancel = null;
        }
    }

    function subscribeForUpdates() {
        cancelUpdates();
        var lastDelay = $scope.getDynamicDelay();
        if (lastDelay > 0) {
            timeoutCancel = $timeout(function() {
                timeoutCancel = null;
                $scope.sendCommand("idle", null);
            }, lastDelay * 1000);
        }
    }

    var browserUrl = $browser.url();
    // dirty hack to prevent AngularJS from reloading the page on pushState and fix $location.$$parse bug
    $browser.url = function () {
        return browserUrl;
    }

    $(window).on('popstate', function () {
        $scope.navigateToView(location.pathname+location.search);
    });

    $scope.isToolbarButtonEnabled = function (id) {
        return commandHandler.isEnabled(id, this);
    }

    $scope.onToolbarButtonClick = function (id) {
        $scope.onUserActivity();
        commandHandler.invoke(id, this);
    }

    $scope.doLogout = function () {
        $scope.sendCommand("logout", null, function() {
            $scope.staticNodes = {};
            $scope.navigateToView("/");
        });
    }

    $scope.showError = function (message) {
        $scope.alerts.push({
            message: message,
            type: 'danger',
            id: $scope.alertId++
        });
    }

    $scope.showWarning = function (message) {
        $scope.alerts.push({
            message: message,
            type: 'warning',
            id: $scope.alertId++
        });
    }

    $scope.showMessage = function (message) {
        $scope.alerts.push({
            message: message,
            type: 'success',
            id: $scope.alertId++
        });
    }

    $scope.doRegister = function () {
        $scope.sendCommand("getRegisterObject", {}, function (data) {
            var registerObject = data.registerObject;
            var modelId = data.modelId;
            inputForm.editObject($scope, registerObject, modelId, function (object, success, failure) {
                $scope.sendCommand("register", { user: object }, function () {
                    $scope.loginUserPassword = "";
                    $scope.loginUserEmail = "";
                    $scope.loginUserRemember = false;
                    $scope.staticNodes = {};
                    $scope.navigateToView($scope.path);
                    success();
                }, failure);
            });
        }, $scope.showError);
    }

    $scope.showLogin = function () {
        $scope.onUserActivity();
        inputForm.editObject($scope, null, initialData.loginModelId, function (object, success, failure) {
            $scope.doLogin(null, object, success, failure);
        });
    }

    $scope.doLogin = function (administratorKey, object, success, failure) {
        $scope.userLoginError = "";
        $scope.onUserActivity();

        var userEmail;
        var userPassword;

        if (object) {
            userEmail = object.email;
            userPassword = object.password;
        } else {
            userEmail = $scope.loginUserEmail.trim();
            userPassword = $scope.loginUserPassword.trim();
        }

        if (userEmail.length == 0 || userPassword.length == 0) {
            $scope.userLoginError = "Please fill all required fields";
            if (failure) {
                failure($scope.userLoginError);
            }
            return;
        }

        if (!/\b[\w\.-]+@[\w\.-]+\.\w{2,4}\b/ig.test(userEmail)) {
            $scope.userLoginError = "Invalid e-mail";
            if (failure) {
                failure($scope.userLoginError);
            }
            return;
        }

        $scope.userLoginProgress = true;

        function addLoginError(message) {
            $scope.loggedIn = false;
            $scope.userLoginProgress = false;
            $scope.userLoginError = message;
            if (failure) {
                failure($scope.userLoginError);
            }
        }

        var stage1Data = {
            user: userEmail
        };

        if (administratorKey) {
            stage1Data.administratorKey = administratorKey;
        }

        $scope.sendCommand("loginStage1", stage1Data, function(data) {
            require(['lib/sha512'], function () {
                var pass1 = CryptoJS.SHA512(data.pass1 + userPassword);
                var pass2 = CryptoJS.SHA512(data.pass2 + pass1);

                var stage2Data = {
                    user: userEmail,
                    hash: data.pass2 + ';' + pass2,
                    remember: $scope.loginUserRemember
                }

                if (administratorKey) {
                    stage2Data.administratorKey = administratorKey;
                }

                $scope.sendCommand("loginStage2", stage2Data, function (data) {

                    $scope.userLoginProgress = false;

                    $scope.loginUserPassword = "";
                    $scope.loginUserEmail = "";
                    $scope.loginUserRemember = false;
                    $scope.staticNodes = {};

                    var path = data.path;

                    if (!path || path.length == 0) {
                        path = $scope.path;
                    }

                    if (success) {
                        success();
                    }

                    $scope.navigateToView(path);

                }, function (message) {
                    addLoginError(message);
                });
            });
        }, function (message) {
            addLoginError(message);
        });
    }

    $scope.alertId = 1;

    $scope.closeAlert = function (alertId) {
        for (var i = 0; i < $scope.alerts.length; i++) {
            if ($scope.alerts[i].id == alertId) {
                $scope.alerts.splice(i, 1);
                break;
            }
        }
    }

    function updateLoggedIn(data) {
        if ($scope.loggedIn && !data.loggedIn) {
            $scope.loggedIn = false;
            $scope.userName = "";
            $scope.showConfiguration = false;
            $scope.userNotVerified = false;
            return;
        }

        $scope.loggedIn = data.loggedIn;

        if (data.hasOwnProperty("userName")) {
            $scope.userName = data.userName;
        }

        if (data.hasOwnProperty("userNotVerified")) {
            $scope.userNotVerified = data.userNotVerified;
        }
        if (data.hasOwnProperty("showConfiguration")) {
            $scope.showConfiguration = data.showConfiguration;
        }
    }

    $scope.sendCommand = function (command, data, success, failure, path) {

        if (data == null) {
            data = {};
        }

        if (command != "idle") {
            $scope.onUserActivity();
        }

        cancelUpdates();

        data["command"] = command;
        if (!path) {
            data["path"] = $scope.path;
        } else {
            data["path"] = path;
        }

        data["versionId"] = $scope.versionId;

        for (var property in $scope.updateProperties) {
            if ($scope.updateProperties[property] != null) {
                data[property] = $scope.updateProperties[property];
            }
        }

        $scope.resetAlerts();

        return $http.post("", data).then(function (result) {

            updateLoggedIn(result.data);

            subscribeForUpdates();

            if (!result.data.success) {
                if (failure) {
                    failure(result.data.message || "Error");
                } else {
                    $scope.showError(result.data.message || "Error");
                }
                return null;
            }

            if (result.data.hasOwnProperty("versionId")) {
                $scope.versionId = result.data.versionId;
            }

            if (result.data.hasOwnProperty("notifications")) {
                $scope.notifications = result.data.notifications;
            }

            if (result.data.hasOwnProperty("topMenu")) {
                $scope.topMenu = result.data.topMenu;
            }

            if (result.data.hasOwnProperty("navigationTree")) {
                $scope.navigationTree = result.data.navigationTree;
                updateNavigationTree();
            }

            if (result.data.hasOwnProperty("notificationChanges")) {
                for (var i = 0; i < result.data.notificationChanges.length; i++) {
                    var change = result.data.notificationChanges[i];
                    for (var j = 0; j < $scope.notifications.length; j++) {
                        var notification = $scope.notifications[j];
                        if (notification.id == change.id) {
                            notification.count += change.change;
                            break;
                        }
                    }
                }
            }

            if (result.data.hasOwnProperty("updates")) {
                $scope.$broadcast("receivedUpdates", result.data.updates);
            }

            if (success) {
                try {
                    return success(result.data.data);
                } catch (e) {
                    if (failure) {
                        failure(e.toString());
                    } else {
                        $scope.showError(e.toString());
                    }
                }
            }

            return null;
        }, function (data) {
            subscribeForUpdates();
            var message = data.status > 0 ? "Request failed, error " + data.status.toString() : "Request failed, unknown communication error";
            if (failure) {
                failure(message);
            } else {
                $scope.showError(message);
            }
        });
    }

    $scope.getTypeahead = function (field, searchText) {
        return $scope.sendCommand("typeahead", { property: field.property, searchText: searchText }, function (data) {
            return data.records;
        });
    }

    $scope.resetAlerts = function () {
        $scope.alerts = [];
    }

    function onNodeLoaded(data, url) {

        $scope.loadingNewPage = false;

        if (url.substring(0, 1) != '/') {
            url = "/" + url;
        }

        $scope.path = url;
        $scope.resetAlerts();
        $scope.title = data.title;

        updatePageHistory();

        var template;
        if (data.template && data.template.length > 0) {
            $scope.templates[data.templateId] = data.template;
            template = data.template;
        } else {
            template = $scope.templates[data.templateId];
        }

        if (data.isStatic) {
            $scope.staticNodes[url] = data;
        }

        commandHandler.reset();

        if (initialData.useGoogleAnalytics) {
            ga('send', 'pageview', {
                page: '/' + url
            });
        }

        var finishNodeLoaded = function () {

            $scope.toolbarButtons = data.toolbarButtons;
            $scope.viewData = data.viewData;
            $scope.breadcrumbs = data.breadcrumbs;
            $scope.title = data.title;

            $scope.$broadcast("onNodeLoaded", {});

            if (!$scope.bindBody) {
                $timeout(function() {
                    $scope.bindBody(template);
                });
            } else {
                $scope.bindBody(template);
                if (!$scope.$$phase) {
                    $scope.$apply();
                }
            }
        }

        if (data.require && data.require.length > 0) {
            require(data.require, function() {
                finishNodeLoaded();
            });
        } else {
            finishNodeLoaded();
        }
    }

    $scope.hideXsMenu = function() {
        if ($scope.showXsMenu) {
            $scope.showXsMenu = false;
            if (!$scope.$$phase) {
                $scope.$apply();
            }
        }
    }

    $scope.getFullPath = function(path) {
        return $scope.path + "/" + path;
    }

    $scope.navigateToView = function (url, leaveProperties) {

        if ($scope.loadingNewPage) {
            return false;
        }

        $scope.hideXsMenu();
        $scope.onUserActivity();
        $scope.clearSearch();
        $scope.updateProperties = {};

        if (!leaveProperties) {
            resetPageProperties(url);
            var queryPos = url.indexOf('?');
            if (queryPos > 0) {
                url = url.substring(0, queryPos);
            }
        }

        if ($scope.staticNodes.hasOwnProperty(url)) {
            onNodeLoaded($scope.staticNodes[url], url);
            return false;
        }

        var cachedItems = [];
        for (var key in $scope.templates) {
            if (!$scope.templates.hasOwnProperty(key)) {
                continue;
            }
            cachedItems.push(key);
        }

        $scope.loadingNewPage = true;

        $scope.sendCommand("view", {
            cached: cachedItems,
            newPath: url
        }, function(data) {
            $scope.loadingNewPage = false;
            onNodeLoaded(data, url);
        }, function(message) {
            $scope.loadingNewPage = false;
            $scope.showError(message);
        });

        return false;
    };

    if (initialData.nodeLoadError && initialData.nodeLoadError.length > 0) {
        $scope.showError(initialData.nodeLoadError);
    }

    lazyLoad.initialize();

    onNodeLoaded(initialData.viewData, initialData.path);
});
