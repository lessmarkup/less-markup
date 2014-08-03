define([], function () {

    app.directive("bindCell", function($compile) {
        return {
            //template: '<div></div>',
            scope: {
                parameter: '=bindCell',
            },
            link: function (scope, element) {
                element.contents().remove();
                scope.record = scope.parameter.record;
                var html = $compile(scope.parameter.template)(scope);
                element.append(html);
            }
        };
    });

    app.directive("cellShowOptions", function($compile) {

        return {
            link: function (scope, element) {
                $(element).on("click", function() {
                    var row = $(element).parent("tr");

                    if (row.hasClass("options-row")) {
                        return;
                    }

                    var table = row.closest("table");

                    table.find(".options-row").removeClass("options-row");
                    table.find(".options-panel").remove();
                    table.find(".options-space").remove();

                    var $scope = scope.getRecordListScope();

                    var space = "<tr class=\"options-space\"><td colspan=\"" + $scope.columns.length.toString() + "\"></td></tr>";

                    var html = $compile(scope.optionsTemplate)($scope);

                    row.before($(space));
                    row.after($(space));

                    row.after(html);
                    row.addClass("options-row");

                    if (!$scope.$$phase) {
                        $scope.$apply();
                    }
                });
            }
        };
    });

    app.controller('newrecordlist', function ($scope, inputForm, commandHandler, $compile) {

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

        $scope.getRecordListScope = function() {
            return $scope;
        }

        var records = [];
        var currentRecordId = null;
        var currentRecord = null;
        var filter = "";

        for (var i in $scope.viewData.actions) {

            var action = $scope.viewData.actions[i];

            if (action.name == "AddRecord") {
                $scope.toolbarButtons.push({
                    Id: "recordlist-add",
                    Text: action.text
                });

                commandHandler.subscribe('recordlist-add', function (sender, invoke) {
                    if (!invoke) {
                        return true;
                    }
                    addRecord();
                    return true;
                });
                continue;
            }

            $scope.actions.push(action);

            if (action.visible && action.visible.length > 0) {
                action.visibleFunction = new Function("obj", "with(obj) { return " + action.visible + "; }");
            } else {
                action.visibleFunction = function () { return true; };
            }
        }

        function resetRecords(recordIds) {

            records = [];

            for (var i in recordIds) {
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
            currentRecord = null;
            currentRecordId = null;
        }

        $scope.onShowOptions = function(record) {

            var recordId = record[recordIdField];

            if (!$scope.showOptions) {
                currentRecord = null;
                currentRecordId = null;
            }

            if (currentRecordId == null) {
                currentRecordId = recordId;
                currentRecord = record;
                $scope.showOptions = true;
            } else if (currentRecordId == recordId) {
                currentRecordId = null;
                currentRecord = null;
                $scope.showOptions = false;
            } else {
                currentRecordId = recordId;
                currentRecord = record;
                $scope.showOptions = true;
            }

            if (!$scope.$$phase) {
                $scope.$apply();
            }
        }

        $scope.isActionVisible = function(action) {
            if (currentRecord == null) {
                return false;
            }
            return action.visibleFunction(currentRecord);
        }

        $scope.sortColumn = function (column) {

            if (!column.sortable) {
                return;
            }

            var current = column.sort;

            for (var i in $scope.columns) {
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

            for (var i in records) {
                var record = records[i];
                if (!record.loaded) {
                    unloadedRecords++;
                }
            }

            hideOptions();

            if (unloadedRecords == 0) {

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

            } else {
                $scope.sendAction("Sort", { column: column.name, direction: current }, function (data) {

                    var recordsById = {};

                    for (var i in records) {
                        var record = records[i];
                        recordsById[record[recordIdField]] = record;
                    }

                    var newRecords = [];

                    for (var i in data.recordIds) {
                        var recordId = data.recordIds[i];
                        newRecords.push(recordsById[recordId]);
                    }

                    records = newRecords;

                    $scope.showPage(1);
                });
            }
        }

        function addRecord() {
            inputForm.editObject(null, $scope.viewData.type, function (object, success, failure) {
                $scope.sendAction("AddRecord", {
                    objectToAdd: object,
                    filter: filter
                }, function (data) {
                    hideOptions();
                    data.record.loaded = true;
                    records.splice(data.index, 0, data.record);
                    $scope.loadVisibleRecords();
                    success();
                }, function (message) {
                    failure(message);
                });
            }, $scope.getTypeahead);
        }

        function editCurrentRecord(index) {

            inputForm.editObject(records[index], $scope.viewData.type, function (object, success, failure) {

                $scope.sendAction("ModifyRecord", {
                    objectToModify: object,
                    filter: filter
                }, function (data) {
                    // success
                    data.record.loaded = true;
                    if (data.index == index) {
                        records[index] = data.record;
                    } else {
                        hideOptions();
                        records.splice(index, 1);
                        if (data.index >= 0) {
                            records.splice(data.index, 0, data.record);
                        }
                    }
                    $scope.loadVisibleRecords();
                    success();

                }, function (message) {
                    // failed
                    failure(message);
                });
            }, $scope.getTypeahead);
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

            $scope.sendAction(action.name, { recordId: currentRecordId, filter: filter }, function (data) {

                if (data.message && data.message.length > 0) {
                    inputForm.message(data.message, "Information");
                } else if (data.hasOwnProperty("removed")) {
                    hideOptions();
                    records.splice(index, 1);
                    $scope.loadVisibleRecords();
                } else if (data.hasOwnProperty("index") && data.hasOwnProperty("record")) {
                    data.record.loaded = true;
                    if (data.index == index) {
                        records[index] = data.record;
                    } else {
                        hideOptions();
                        records.splice(index, 1);
                        if (data.index >= 0) {
                            records.splice(data.index, 0, data.record);
                        }
                    }
                    $scope.loadVisibleRecords();
                }
            }, function (message) {
                inputForm.message(message, "Error");
            });
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

        function createNewRecord(recordId) {
            var record = { loaded: false };
            for (var i in $scope.columns) {
                record[$scope.columns[i].name] = "";
            }
            record[recordIdField] = recordId;
            return record;
        }

        $scope.getColumnLink = function(column, row) {
            return $scope.path + "/" + extractLink(column.url, row);
        }

        for (var i in $scope.columns) {
            var column = $scope.columns[i];
            if (!column.template || column.template.length == 0) {
                if (column.url && column.url.length > 0) {
                    column.template = "<a href=\"{{getColumnLink(column)}}\" ng-click=\"navigateToView(getColumnLink(column))\" ng-bind=\"record." + column.name + "\"></a>";
                } else {
                    column.template = "<span ng-bind=\"record." + column.name + "\"></span>";
                }
            }
        }

        resetRecords($scope.viewData.recordIds);

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
            for (var i in updated) {
                var record = updated[i];
                record.loaded = true;
                var recordId = record[recordIdField];
                for (var j in records) {
                    var currentRecord = records[j];
                    var currentRecordId = currentRecord[recordIdField];
                    if (currentRecordId != recordId) {
                        continue;
                    }
                    records[j] = record;

                    for (var k in $scope.rows) {
                        var row = $scope.rows[k];
                        if (row[recordIdField] == recordId) {
                            $scope.rows[k] = record;
                            record.isOdd = (k % 2) != 0;
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

        $scope.showPage(1);
    });
});