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
    $scope.userLoggedIn = initialData.UserLoggedIn;
    $scope.userNotVerified = initialData.UserNotVerified;
    $scope.userName = initialData.UserName;
    $scope.userLoginError = "";
    $scope.userLoginProgress = false;
    $scope.alerts = [];
    $scope.hasLogin = initialData.HasLogin;
    $scope.hasSearch = initialData.HasSearch;
    $scope.showConfiguration = initialData.ShowConfiguration;
    $scope.configurationPath = initialData.ConfigurationPath;
    $scope.rootPath = initialData.RootPath;
    $scope.rootTitle = initialData.RootTitle;
    $scope.navigationTree = initialData.NavigationTree;
    $scope.hasNavigationTree = $scope.navigationTree != null && $scope.navigationTree.length > 0;
    $scope.topMenu = initialData.TopMenu;
    $scope.profilePath = initialData.ProfilePath;
    $scope.forgotPasswordPath = initialData.ForgotPasswordPath;
    $scope.languages = initialData.Languages;
    $scope.getViewScope = function () { return $scope; }
    $scope.showXsMenu = false;
    $scope.notifications = initialData.Notifications;
    $scope.recaptchaPublicKey = initialData.RecaptchaPublicKey;
    $scope.lastActivity = new Date().getDate() / 1000;
    $scope.title = $scope.rootTitle;
    $scope.loadingNewPage = true;
    $scope.searchText = "";
    $scope.searchResults = [];
    $scope.smiles = initialData.Smiles;
    $scope.smilesBase = initialData.SmilesBase;
    var searchTimeout = null;
    var pageProperties = {};

    $scope.selectedLanguage = null;
    if ($scope.languages != null) {
        for (var i = 0; i < $scope.languages.length; i++) {
            if ($scope.languages[i].Selected) {
                $scope.selectedLanguage = $scope.languages[i];
                break;
            }
        }
    }

    var smilesStr = null;
    var smiles = {};

    $scope.getFriendlyHtml = function (text) {

        if (text == null || text.length == 0) {
            return text;
        }

        function getSmileUrl(code) {
            if (!code.length || !initialData.SmilesBase) {
                return "";
            }
            return "<img src=\"" + initialData.SmilesBase + smiles[code] + "\"/>";
        }

        if (smilesStr == null) {
            smilesStr = "";
            for (var i = 0; i < initialData.Smiles.length; i++) {
                var smile = initialData.Smiles[i];
                smiles[smile.Code] = smile.Id;
                if (smilesStr.length > 0) {
                    smilesStr += "|";
                }
                for (var j = 0; j < smile.Code.length; j++) {
                    switch (smile.Code[j]) {
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
                    smilesStr += smile.Code[j];
                }
            }
        }

        var smilesExpr = new RegExp(smilesStr, "g");

        if (smilesStr.length > 0) {
            text = text.replace(smilesExpr, getSmileUrl);
        }

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
        $scope.sendCommand("SearchText", {
            text: $scope.searchText
        }, function (data) {
            if (data != null && data.hasOwnProperty("Results")) {
                $scope.searchResults = data.Results;
                for (var i = 0; i < $scope.searchResults.length; i++) {
                    var result = $scope.searchResults[i];
                    result.Text = result.Text.replace(new RegExp(searchText, "gim"), "<span class=\"highlight\">$&</span>");
                    result.Text = $sce.trustAsHtml(result.Text);
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

    if ($scope.hasNavigationTree) {
        for (var i = 0; i < $scope.navigationTree.length; i++) {
            var item = $scope.navigationTree[i];
            item.style = 'margin-left:' + (item.Level).toString() + 'em;';
        }
    }

    $scope.onUserActivity = function () {
        $scope.lastActivity = new Date().getDate() / 1000;
        $scope.$broadcast("UserActivity", {});
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

    if ($scope.notifications.length) {

        $scope.gotoNotification = function(notification) {
            notification.Version = notification.NewVersion;
            $scope.navigateToView(notification.Path);
        }

        $scope.notificationClass = function(notification) {
            if (notification.Count > 0) {
                return "active-notification";
            }
            return "";
        }

        for (var i = 0; i < $scope.notifications.length; i++) {
            $scope.notifications[i].NewVersion = $scope.notifications[i].Version;
        }

        var timeoutCancel = null;
        var lastDelay = 0;

        function cancelUpdates() {
            if (timeoutCancel != null) {
                $timeout.cancel(timeoutCancel);
                timeoutCancel = null;
            }
        }

        function subscribeForUpdates() {
            cancelUpdates();
            lastDelay = $scope.getDynamicDelay();
            if (lastDelay > 0) {
                timeoutCancel = $timeout(getUpdates, lastDelay * 1000);
            }
        }

        $scope.$on('UserActivity', function() {
            var delay = $scope.getDynamicDelay();
            if (delay != lastDelay) {
                cancelUpdates();
                $timeout(getUpdates, 1000);
            }
        });

        function getUpdates() {
            timeoutCancel = null;

            var data = {};

            var current = [];

            for (var i = 0; i < $scope.notifications.length; i++) {
                var notification = $scope.notifications[i];
                current.push({
                    Id: notification.Id,
                    Version: notification.Version,
                });
            }

            data.notifications = current;

            $scope.sendCommand("GetNotifications", data, function(data) {
                subscribeForUpdates();
                for (var i = 0; i < data.notifications.length; i++) {
                    var source = data.notifications[i];
                    for (var j = 0; j < $scope.notifications.length; j++) {
                        var target = $scope.notifications[j];
                        if (source.Id == target.Id) {
                            target.Count = source.Count;
                            target.NewVersion = source.Version;
                            break;
                        }
                    }
                }
                if (!$scope.$$phase) {
                    $scope.$apply();
                }
            }, function(message) {
                subscribeForUpdates();
            });
        }

        subscribeForUpdates();
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
        $http.post("", {
            "-command-": "Logout"
        }).success(function (data) {
            if (!data.Success) {
                $scope.showError(data.Message);
                return;
            }

            $scope.showConfiguration = false;
            $scope.userLoggedIn = false;
            $scope.userNotVerified = false;
            $scope.userName = "";
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
        $scope.sendCommand("GetRegisterObject", {}, function (data) {
            var registerObject = data.RegisterObject;
            var modelId = data.ModelId;
            inputForm.editObject($scope, registerObject, modelId, function (object, success, failure) {
                $scope.sendCommand("Register", { user: object }, function (data) {
                    $scope.userLoggedIn = true;
                    $scope.userNotVerified = data.UserNotVerified;
                    $scope.userName = data.UserName;
                    $scope.showConfiguration = data.ShowConfiguration;
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
        inputForm.editObject($scope, null, $scope.viewData.LoginModelId, function (object, success, failure) {
            $scope.doLogin(null, object, success, failure);
        });
    }

    $scope.doLogin = function (administratorKey, object, success, failure) {
        $scope.userLoginError = "";
        $scope.onUserActivity();

        var userEmail;
        var userPassword;

        if (object) {
            userEmail = object.Email;
            userPassword = object.Password;
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

        function addLoginError(status) {
            if (status > 0) {
                $scope.userLoginError = "Request failed, unknown communication error";
            } else {
                $scope.userLoginError = "Request failed, status: " + status.toString();
            }
            $scope.userLoginProgress = false;
            if (failure) {
                failure($scope.userLoginError);
            }
        }

        var stage1Data = {
            "-command-": "LoginStage1",
            user: userEmail
        };

        if (administratorKey) {
            stage1Data.administratorKey = administratorKey;
        }

        $http.post("", stage1Data).success(function (data) {
            if (!data.Success) {
                $scope.userLoggedIn = false;
                $scope.userLoginProgress = false;
                $scope.userLoginError = data.Message;
                if (failure) {
                    failure($scope.userLoginError);
                }
                return;
            }

            require(['lib/sha512'], function() {
                var pass1 = CryptoJS.SHA512(data.Data.Pass1 + userPassword);
                var pass2 = CryptoJS.SHA512(data.Data.Pass2 + pass1);

                var stage2Data = {
                    "-command-": "LoginStage2",
                    user: userEmail,
                    hash: data.Data.Pass2 + ';' + pass2,
                    remember: $scope.loginUserRemember
                }

                if (administratorKey) {
                    stage2Data.administratorKey = administratorKey;
                }

                $http.post("", stage2Data).success(function (data) {
                    $scope.userLoginProgress = false;
                    if (!data.Success) {
                        $scope.userLoginError = data.Message;
                        $scope.userLoggedIn = false;
                        if (failure) {
                            failure($scope.userLoginError);
                        }
                        return;
                    }
                    $scope.userLoggedIn = true;
                    $scope.userNotVerified = data.Data.UserNotVerified;
                    $scope.userName = data.Data.UserName;
                    $scope.showConfiguration = data.Data.ShowConfiguration;
                    $scope.loginUserPassword = "";
                    $scope.loginUserEmail = "";
                    $scope.loginUserRemember = false;
                    $scope.staticNodes = {};

                    var path = data.Data.Path;

                    if (!path || path.length == 0) {
                        path = $scope.path;
                    }

                    if (success) {
                        success();
                    }

                    $scope.navigateToView(path);

                }).error(function (data, status) {
                    addLoginError(status);
                });
            });
        }).error(function (data, status) {
            addLoginError(status);
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

    $scope.sendAction = function (action, data, success, failure, path) {
        if (data == null) {
            data = {};
        }
        data["-action-"] = action;
        return $scope.sendCommand("Action", data, success, failure, path);
    }

    $scope.sendCommand = function (command, data, success, failure, path) {
        if (data == null) {
            data = {};
        }
        data["-command-"] = command;
        if (!path) {
            data["-path-"] = $scope.path;
        } else {
            data["-path-"] = path;
        }
        $scope.alerts = [];
        $http.post("", data).success(function (data) {
            validateLoggedIn(data.UserLoggedIn);
            $scope.userNotVerified = data.UserNotVerified;
            if (!data.Success) {
                if (failure) {
                    failure(data.Message);
                } else {
                    $scope.showError(data.Message);
                }
                return;
            }
            if (success) {
                try {
                    success(data.Data);
                } catch (e) {
                    failure(e.toString());
                }
            } else {
                $scope.showMessage("Command successful");
            }
        }).error(function (data, status) {
            var message = status > 0 ? "Request failed, error " + status.toString() : "Request failed, unknown communication error";
            if (failure) {
                failure(message);
            } else {
                $scope.showError(message);
            }
        });
    }

    function validateLoggedIn(userLoggedIn) {
        if ($scope.userLoggedIn && !userLoggedIn) {
            $scope.userLoggedIn = false;
            $scope.userName = "";
            $scope.showConfiguration = false;
        }
    }

    $scope.getTypeahead = function (field, searchText) {
        return $scope.sendCommandAsync("Typeahead", { property: field.Property, searchText: searchText }, function (data) {
            return data.Records;
        });
    }

    $scope.sendActionAsync = function (action, data, success, failure, path) {
        if (data == null) {
            data = {};
        }
        data["-action-"] = action;
        return $scope.sendCommandAsync("Action", data, success, failure, path);
    }

    $scope.sendCommandAsync = function (command, data, success, failure, path) {
        if (data == null) {
            data = {};
        }
        data["-command-"] = command;
        if (!path) {
            data["-path-"] = $scope.path;
        } else {
            data["-path-"] = path;
        }
        $scope.alerts = [];
        return $http.post("", data).then(function (result) {
            data = result.data;
            validateLoggedIn(data.UserLoggedIn);
            $scope.userNotVerified = data.UserNotVerified;
            if (!data.Success) {
                if (failure) {
                    failure(data.Message);
                } else {
                    $scope.showError(data.Message);
                }
                return null;
            }
            if (success) {
                try {
                    return success(data.Data);
                } catch (e) {
                    failure(e.toString());
                }
            } else {
                $scope.showMessage("Command successful");
            }
            return null;
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
        $scope.title = data.Title;

        updatePageHistory();

        var template;
        if (data.Template != null && data.Template.length > 0) {
            $scope.templates[data.TemplateId] = data.Template;
            template = data.Template;
        } else {
            template = $scope.templates[data.TemplateId];
        }

        if (data.IsStatic) {
            $scope.staticNodes[url] = data;
        }

        commandHandler.reset();

        if (initialData.UseGoogleAnalytics) {
            ga('send', 'pageview', {
                page: '/' + url
            });
        }

        var finishNodeLoaded = function() {
            $scope.toolbarButtons = data.ToolbarButtons;
            $scope.viewData = data.ViewData;
            $scope.breadcrumbs = data.Breadcrumbs;
            $scope.title = data.Title;
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

        if (data.Require && data.Require != null && data.Require.length > 0) {
            require(data.Require, function() {
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

        $http.post("", {
            "-command-": "View",
            "-cached-": cachedItems,
            "-path-": url
        }).success(function (data) {
            validateLoggedIn(data.UserLoggedIn);
            $scope.userNotVerified = data.UserNotVerified;
            if (!data.Success) {
                $scope.loadingNewPage = false;
                $scope.showError(data.Message);
                return;
            }
            onNodeLoaded(data.Data, url);
        }).error(function (data, status) {
            $scope.loadingNewPage = false;
            $scope.showError(status > 0 ? "Request failed, error " + status.toString() : "Request failed, unknown communication error");
        });

        return false;
    };

    if (initialData.NodeLoadError && initialData.NodeLoadError.length > 0) {
        $scope.showError(initialData.NodeLoadError);
    }

    lazyLoad.initialize();

    onNodeLoaded(initialData.ViewData, initialData.Path);
});
