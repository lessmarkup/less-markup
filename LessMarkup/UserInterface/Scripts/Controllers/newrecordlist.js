app.directive("bindCell", function($compile) {
    return {
        scope: {
            parameters: '=bindCell',
        },
        link: function (scope, element) {

            var innerScope = scope.$parent.$new();

            function updateValue() {
                element.contents().remove();
                innerScope.data = scope.parameters.scope(innerScope);
                var html = $compile(scope.parameters.template)(innerScope);
                element.append(html);
            }

            updateValue();
            scope.$watch("scope.parameters.scope(scope)", updateValue);
        }
    };
});

app.directive("cellShowOptions", function($compile) {

    return {
        link: function (scope, element) {

            if (!scope.hasOptionsBar) {
                return;
            }

            $(element).on("click", function() {
                var row = $(element).parent("tr");

                if (row.hasClass("options-row")) {
                    return true;
                }

                var table = row.closest("table");

                table.find(".options-row").removeClass("options-row");
                table.find(".options-panel").remove();
                table.find(".options-space").remove();

                var $scope = scope.getRecordListScope();

                var space = "<tr class=\"options-space\"><td colspan=\"" + ($scope.columns.length+1).toString() + "\"></td></tr>";

                var html = $compile(scope.optionsTemplate)($scope);

                row.before($(space));
                row.after($(space));

                row.after(html);
                row.addClass("options-row");

                if (!$scope.$$phase) {
                    $scope.$apply();
                }

                return true;
            });
        }
    };
});

app.controller('newrecordlist', function ($scope, inputForm, $sce) {

    var recordIdField = $scope.viewData.recordId;

    $scope.showOptions = false;
    $scope.columns = $scope.viewData.columns;
    $scope.links = $scope.viewData.links;
    $scope.currentPage = 0;
    $scope.showPagination = false;
    $scope.paginationItems = 7;
    $scope.rows = [];
    $scope.pageLoaded = false;
    $scope.actions = [];
    $scope.optionsTemplate = $scope.viewData.optionsTemplate;
    $scope.toolbarButtons = [];

    $scope.getSafeValue = function(html) {
        return $sce.trustAsHtml(html);
    }

    $scope.getRecordListScope = function() {
        return $scope;
    }

    var createNewRecord;

    function createNewRecord(recordId) {
        var record = { loaded: false }
        for (var i = 0; i < $scope.columns.length; i++) {
            record[$scope.columns[i].name] = "";
        }
        record[recordIdField] = recordId;
        return record;
    }

    function createRecordCopy(record) {
        record.loaded = true;
        return record;
    }

    var records = [];
    var currentRecordId = null;
    var currentRecord = null;
    var filter = "";

    for (var i = 0; i < $scope.columns.length; i++) {
        var column = $scope.columns[i];
        column.colSpan = 1;
        if (!column.scope || !column.scope.length) {
            column.scope = function(obj) {
                return obj.row;
            }
        } else {
            column.scope = new Function("obj", "with(obj) { return " + column.scope + "; }");
        }
    }

    $scope.onDataReceived = function(scope, data) {
    }

    for (var i = 0; i < $scope.viewData.actions.length; i++) {

        var action = $scope.viewData.actions[i];

        var visibleFunction;

        if (action.visible && action.visible.length > 0) {
            visibleFunction = new Function("obj", "with(obj) { return " + action.visible + "; }");
        } else {
            visibleFunction = function () { return true; };
        }

        switch (action.type) {
            case "Record":
            case "RecordCreate":
            case "RecordInitializeCreate":
                $scope.actions.push({
                    name: action.name,
                    text: action.text,
                    visible: visibleFunction,
                    type: action.type,
                    parameter: action.parameter,
                });
                break;
            case "Create":
                $scope.toolbarButtons.push({
                    name: action.name,
                    text: action.text,
                    visible: visibleFunction,
                    type: action.parameter,
                });
                break;
        }
    }

    $scope.hasOptionsBar = $scope.columns.length > 0 && ($scope.actions.length > 0 || $scope.links.length > 0);

    if ($scope.hasOptionsBar) {
        $scope.columns[0].colSpan = 2;
    }

    $scope.onToolbarButtonClick = function(action) {
        inputForm.editObject(null, action.type, function (object, success, failure) {
            $scope.sendAction(action.name, {
                newObject: object,
                filter: filter
            }, function (data) {
                handleActionResult(data, -1, success, failure);
            }, function (message) {
                failure(message);
            });
        }, $scope.getTypeahead);
    }

    function resetRecords(recordIds) {

        records = [];

        for (var i = 0; i < recordIds.length; i++) {
            var recordId = recordIds[i];
            var record = createNewRecord(recordId);
            records.push(record);
        }
    }

    $scope.getRecordId = function(record) {
        return record[recordIdField];
    }

    function hideOptions() {
        $scope.showOptions = false;
    }

    $scope.onClickOptions = function(record) {

        if (!$scope.hasOptionsBar) {
            return;
        }

        var recordId = record[recordIdField];

        if (!currentRecord || currentRecordId != recordId) {
            $scope.showOptions = true;
        } else {
            $scope.showOptions = !$scope.showOptions;
        }

        currentRecordId = recordId;
        currentRecord = record;

        if (!$scope.$$phase) {
            $scope.$apply();
        }
    }

    $scope.isActionVisible = function(action) {
        if (currentRecord == null || !$scope.showOptions) {
            return false;
        }
        return action.visible(currentRecord);
    }

    $scope.sortColumn = function (column) {

        if (!column.sortable) {
            return;
        }

        var current = column.sort;

        for (var i = 0; i < $scope.columns.length; i++) {
            var column = $scope.columns[i];
            column.sort = "";
        }

        if (!current || current == "up") {
            current = "down";
        } else {
            current = "up";
        }

        column.sort = current;

        var unloadedRecords = 0;

        for (var i = 0; i < records.length; i++) {
            var record = records[i];
            if (!record.loaded) {
                unloadedRecords++;
            }
        }

        hideOptions();

        if (unloadedRecords != 0) {
            $scope.sendAction("Sort", { column: column.name, direction: current }, function (data) {
                resetRecords(data.recordIds);
                $scope.showPage(1);
            }, function(message) {
                inputForm.message(message, "Error");
            });
            return;
        }

        var field = column.name;

        records.sort(function(a, b) {
            var af = a[field];
            var bf = b[field];
            if (af == bf) {
                return 0;
            }
            if (af < bf) {
                return -1;
            }
            return 1;
        });

        $scope.showPage(1);
    }

    function editCurrentRecord(index) {

        inputForm.editObject(records[index], $scope.viewData.type, function (object, success, failure) {
            $scope.sendAction("ModifyRecord", {
                modifiedObject: object,
                filter: filter
            }, function (data) {
                // success
                handleActionResult(data, index, success, failure);
            }, function (message) {
                // failed
                failure(message);
            });
        }, $scope.getTypeahead);
    }

    function handleActionResult(data, index, success, failure) {

        $scope.onDataReceived($scope, data);

        if (data.redirect && data.redirect.length) {
            if (success) {
                success();
            }
            $scope.navigateToView(data.redirect);
            return;
        }

        if (data.message && data.message.length) {
            if (failure) {
                failure(data.message);
            } else {
                inputForm.message(data.message, "Information");
                if (success) {
                    success();
                }
            }
            return;
        }

        if (data.hasOwnProperty("removed")) {
            hideOptions();
            records.splice(index, 1);
            $scope.loadVisibleRecords();
            return;
        }

        if (!data.hasOwnProperty("record")) {
            return;
        }

        var isNew = data.hasOwnProperty("isNew") && data.isNew;
        var newIndex = data.hasOwnProperty("index") ? data.index : -1;

        var record = createRecordCopy(data.record);

        if (!isNew && index >= 0 && (newIndex == index || newIndex == -1)) {
            records[index] = record;
            $scope.loadVisibleRecords();
        } else {
            hideOptions();
            if (!isNew) {
                records.splice(index, 1);
            }
            if (newIndex >= 0) {
                records.splice(data.index, 0, record);
            } else {
                newIndex = records.length;
                records.push(record);
            }
            var totalItems = records.length;
            var itemsPerPage = $scope.viewData.recordsPerPage;
            var pageCount = ((totalItems + itemsPerPage - 1) / itemsPerPage) | 0;
            if (pageCount <= 1) {
                $scope.loadVisibleRecords();
            } else {
                var page = (newIndex / itemsPerPage) + 1;
                $scope.showPage(page);
            }
        }

        if (!$scope.$$phase) {
            $scope.$apply();
        }

        if (success) {
            success();
        }
    }

    $scope.executeAction = function (action) {

        if (currentRecordId == null) {
            return;
        }

        var index;

        for (index = 0; index < records.length; index++) {
            var itemId = records[index][recordIdField];
            if (itemId == currentRecordId) {
                break;
            }
        }

        if (index >= records.length) {
            return;
        }

        if (action.name == "ModifyRecord") {
            editCurrentRecord(index);
            return;
        }

        var actionData = {
            recordId: currentRecordId,
            filter: filter
        };

        function sendAction(success, failure) {
            $scope.sendAction(action.name, actionData, function (data) {
                handleActionResult(data, index, success, failure);
            }, failure);
        }

        if (action.type == "RecordInitializeCreate") {
            $scope.sendAction(action.name, actionData, function(data) {
                if (data.message && data.message.length > 0) {
                    inputForm.message(data.message, "Information");
                } else {
                    inputForm.editObject(data.record, action.parameter, function (object, success, failure) {
                        actionData.newObject = object;
                        sendAction(success, failure);
                    }, $scope.getTypeahead);
                }
            }, function (message) {
                inputForm.message(message, "Error");
            });
        } else if (action.type == "RecordCreate") {
            var record = null;
            if (action.parameter === $scope.viewData.type) {
                record = currentRecord;
            }
            inputForm.editObject(record, action.parameter, function (object, success, failure) {
                actionData.newObject = object;
                sendAction(success, failure);
            }, $scope.getTypeahead);
        } else {
            sendAction(null, function (message) {
                inputForm.message(message, "Error");
            });
        }

    }

    function extractLink(text, row) {
        var link = text;
        var offset = 0;
        while(true) {
            var pos = link.indexOf("{", offset);
            if (pos < 0) {
                break;
            }
            var end = link.indexOf("}", pos);
            if (end < 0) {
                break;
            }

            var parameter = link.substring(pos + 1, end);

            if (!row.hasOwnProperty(parameter)) {
                offset = pos + 1;
                continue;
            }

            parameter = row[parameter];
            link = link.substring(0, pos) + parameter + link.substring(end + 1);
            offset += parameter.length;
        }
        return link;
    }

    $scope.navigateToLink = function(link) {
        $scope.navigateToView($scope.path + "/" + extractLink(link.url, currentRecord));
    }

    $scope.linkUrl = function (link) {
        if (currentRecord == null) {
            return "";
        }
        return $scope.path + "/" + extractLink(link.url, currentRecord);
    }

    $scope.getColumnLink = function(column, row) {
        return $scope.path + "/" + extractLink(column.url, row);
    }

    for (var i = 0; i < $scope.columns.length; i++) {
        var column = $scope.columns[i];
        if (!column.template || column.template.length == 0) {
            var value = "record." + column.name;
            if (column.allowUnsafe) {
                value = "getSafeValue(" + value + ")";
            }

            if (column.url && column.url.length > 0) {
                column.template = "<a href=\"{{getColumnLink(column, row)}}\" ng-click=\"navigateToView(getColumnLink(column, row))\" ng-bind=\"" + value + "\"></a>";
            } else {
                column.template = "<span ng-bind=\"" + value + "\"></span>";
            }
        }
    }

    resetRecords($scope.viewData.recordIds);

    for (var i = 0; i < $scope.viewData.records.length; i++) {
        var source = $scope.viewData.records[i];
        var target = records[i];
        for (var property in source) {
            target[property] = source[property];
        }

        target.loaded = true;
    }

    $scope.$watch("currentPage", function () {
        if (!$scope.pageLoaded) {
            return;
        }
        $scope.loadVisibleRecords();
    });

    $scope.showPage = function(page) {
        $scope.totalItems = records.length;
        var itemsPerPage = $scope.viewData.recordsPerPage;
        var pageCount = (($scope.totalItems + itemsPerPage - 1) / itemsPerPage) | 0;

        if (page < 1) {
            page = 1;
        } else if (page > pageCount) {
            page = pageCount > 0 ? pageCount : 1;
        }

        $scope.currentPage = page;
        $scope.pageOffset = (page - 1) * itemsPerPage;
        $scope.pageSize = itemsPerPage;
        $scope.itemsPerPage = itemsPerPage;

        if (pageCount == 0) {
            $scope.pageSize = 0;
        } else if ($scope.pageOffset + $scope.pageSize > $scope.totalItems) {
            $scope.pageSize = $scope.totalItems - $scope.pageOffset;
        }

        $scope.rows = [];

        currentRecordId = null;
        currentRecord = null;
        $scope.showOptions = false;

        var firstEmptyRecord = -1;
        var lastEmptyRecord = -1;
        for (var i = 0; i < $scope.pageSize; i++) {
            var record = records[i + $scope.pageOffset];

            $scope.rows.push(record);

            record.isOdd = (i % 2) != 0;

            if (!record.loaded) {
                if (firstEmptyRecord == -1) {
                    firstEmptyRecord = i;
                }
                lastEmptyRecord = i;
            }
        }

        if (firstEmptyRecord != -1) {
            var recordIds = [];
            for (var i = firstEmptyRecord; i <= lastEmptyRecord; i++) {
                var recordId = records[i + $scope.pageOffset][recordIdField];
                recordIds.push(recordId);
            }

            $scope.sendAction("Fetch", {
                ids: recordIds
            }, function (data) {
                $scope.onDataReceived($scope, data);
                $scope.updateRecords(data.records);
                if (!$scope.$$phase) {
                    $scope.$apply();
                }
            });
        }

        $scope.showPagination = $scope.pageSize < $scope.totalItems;

        if (!$scope.$$phase) {
            $scope.$apply();
        }

        $scope.pageLoaded = true;
    }

    $scope.updateRecords = function (updated) {
        for (var i = 0; i < updated.length; i++) {
            var source = updated[i];
            var recordId = source[recordIdField];
            var target = createRecordCopy(source);

            for (var j = 0; j < records.length; j++) {
                var currentRecord = records[j];
                var currentRecordId = currentRecord[recordIdField];
                if (currentRecordId != recordId) {
                    continue;
                }

                records[j] = target;

                for (var k = 0; k < $scope.rows.length; k++) {
                    var row = $scope.rows[k];
                    if (row[recordIdField] == recordId) {
                        $scope.rows[k] = target;
                        target.isOdd = (k % 2) != 0;
                        break;
                    }
                }

                break;
            }
        }
    }

    $scope.loadVisibleRecords = function () {
        $scope.showPage($scope.currentPage);
    }

    if ($scope.viewData.extensionScript && $scope.viewData.extensionScript.length) {
        require([$scope.viewData.extensionScript], function(extensionScript) {
            extensionScript($scope);
            $scope.onDataReceived($scope, $scope.viewData);
            $scope.showPage(1);
        });
    } else {
        $scope.onDataReceived($scope, $scope.viewData);
        $scope.showPage(1);
    }
});
