/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

define([
    'lib/nggrid/ng-grid'
], function () {

    app.ensureModule('ngGrid');

    app.controller('recordlist', function ($scope, inputForm, commandHandler) {

        $scope.data = $scope.viewData;
        $scope.selectedItems = [];
        $scope.filter = "";
        $scope.records = [];
        $scope.pageRecords = [];
        $scope.changeNumber = 0;
        $scope.currentPage = 0;
        $scope.showPagination = false;
        $scope.paginationItems = 7;
        $scope.totalItems = 0;
        $scope.pageOffset = 0;
        $scope.pageSize = 0;
        $scope.pageLoaded = false;
        $scope.cellLinks = $scope.viewData.CellLinks;
        $scope.recordListOptions = {
            data: 'pageRecords',
            // ReSharper disable once InconsistentNaming
            // plugins: [new ngGridFlexibleHeightPlugin()],
            columnDefs: $scope.data.Columns,
            selectedItems: $scope.selectedItems,
            enableColumnResize: $scope.data.ColumnsResizable
        };

        for (var column in $scope.recordListOptions.columnDefs) {
            if (column.cellTemplate && column.cellTemplate.length > 0) {
                column.cellTemplate = "<div class='ngCellText'>" + column.cellTemplate + "</div>";
            }
        }

        for (var recordAction in $scope.viewData.Actions) {

            $scope.toolbarButtons.push({
                Id: recordAction.Id,
                Text: recordAction.Text
            });

            commandHandler.subscribe(recordAction.Id, function (sender, handle) {

                if (!handle) {
                    return $scope.selectedItems.length > 0;
                }

                var ids = [];

                for (var i = 0; i < $scope.selectedItems.length; i++) {
                    ids.push($scope.selectedItems[i][$scope.data.RecordId]);
                }

                $scope.sendAction("HandleRecordsAction", {
                    ids: ids,
                    action: recordAction.Id
                }, function () {
                    if ($scope.data.RefreshOnAllActions) {
                        $scope.loadVisibleRecords();
                    }
                }, function (message) {
                    inputForm.message(message, "Error");
                });

                return true;
            });
        }

        $scope.editRecord = function (currentObject) {

            var index;
            var currentId = currentObject[$scope.data.RecordId];
            for (index = 0; index < $scope.records.length; index++) {
                var itemId = $scope.records[index][$scope.data.RecordId];
                if (itemId == currentId) {
                    break;
                }
            }
            if (index >= $scope.records.length) {
                return;
            }
            inputForm.editObject($scope, currentObject, $scope.data.Type, function (object, success, failure) {
                $scope.sendAction("ModifyRecord", {
                    objectToModify: object,
                    filter: $scope.filter
                }, function (data) {
                    // success
                    if ($scope.data.RefreshOnAllActions) {
                        $scope.loadVisibleRecords();
                    } else {
                        data.Record.loaded = true;
                        if (data.Index == index) {
                            $scope.records[index] = data.Record;
                        } else {
                            $scope.records.splice(index, 1);
                            if (data.Index >= 0) {
                                $scope.records.splice(data.Index, 0, data.Record);
                            }
                        }
                        $scope.loadVisibleRecords();
                    }
                    success();

                }, function (message) {
                    // failed
                    failure(message);
                });
            }, $scope.getTypeahead);
        }

        $scope.addRecord = function () {
            inputForm.editObject($scope, null, $scope.data.Type, function (object, success, failure) {

                $scope.sendAction("AddRecord", {
                    objectToAdd: object,
                    filter: $scope.filter
                }, function (data) {
                    if ($scope.data.RefreshOnAllActions) {
                        // todo: reset all records
                        $scope.loadVisibleRecords();
                    } else {
                        data.Record.loaded = true;
                        $scope.records.splice(data.Index, 0, data.Record);
                        $scope.loadVisibleRecords();
                    }
                    success();
                }, function (message) {
                    failure(message);
                });
            }, $scope.getTypeahead);
        }

        $scope.deleteRecords = function (records) {

            var ids = [];

            for (var i = records.length - 1; i >= 0; i--) {
                var record = records[i];
                ids.push(record[$scope.data.RecordId]);
            }

            inputForm.question("Do you want do delete selected objects?", "Delete Objects", function (success, fail) {

                $scope.sendAction("RemoveRecords", {
                    recordIds: ids
                }, function () {
                    if ($scope.data.RefreshOnAllActions) {
                        // todo: reset all records
                        $scope.loadVisibleRecords();
                    } else {
                        for (var i = 0; i < ids.length; i++) {
                            var id = ids[i];
                            for (var j = 0; j < $scope.records.length; j++) {
                                var record = $scope.records[j];
                                var recordId = record[$scope.data.RecordId];
                                if (recordId == id) {
                                    $scope.records.splice(j, 1);
                                    break;
                                }
                            }
                        }
                        $scope.loadVisibleRecords();
                    }
                    success();
                }, function (message) {
                    fail(message);
                });
            });
        }

        $scope.getRowLink = function (index, row) {
            var recordId = row.entity[$scope.data.RecordId];
            var link = $scope.cellLinks[index];
            return $scope.path + "/" + recordId.toString() + "/" + link.Link;
        }

        for (var i = 0; i < $scope.cellLinks.length; i++) {
            var link = $scope.cellLinks[i];
            $scope.recordListOptions.columnDefs.splice(i, 0, {
                displayName: link.Text,
                cellTemplate: "<div class='ngCellText'><a href='{{getRowLink(" + i.toString() + ", row)}}' ng-click='navigateToView(getRowLink(" + i.toString() + ", row))'>" + link.Text + "</a></div>"
            });
        }

        for (var i = 0; i < $scope.viewData.CellCommands.length; i++) {
            var command = $scope.viewData.CellCommands[i];
            if (typeof (command.VisibleCondition) != "undefined" && command.VisibleCondition != null) {
                command.VisibleFunction = new Function("obj", "with(obj) { return " + command.VisibleCondition + "; }");
            } else {
                command.VisibleFunction = null;
            }
            $scope.recordListOptions.columnDefs.splice(i, 0, {
                displayName: command.Text,
                cellTemplate: "<div class='ngCellText grid-cell-command'><span ng-if='showCellCommand(row, " + i.toString() + ")' ng-click='executeCellCommand(row, " + '"' + command.Command + '"' + ")'>" + command.Text + "</span></div>"
            });
        }

        $scope.showCellCommand = function (row, index) {
            var record = row.entity;
            var command = $scope.viewData.CellCommands[index];
            var visibleFunction = command.VisibleFunction;
            if (visibleFunction == null) {
                return true;
            }
            return visibleFunction(record);
        }

        $scope.executeCellCommand = function (row, commandId) {
            var recordId = row.entity[$scope.data.RecordId];

            var index;
            for (index = 0; index < $scope.records.length; index++) {
                var itemId = $scope.records[index][$scope.data.RecordId];
                if (itemId == recordId) {
                    break;
                }
            }
            if (index >= $scope.records.length) {
                return;
            }

            $scope.sendAction("CellCommand", {
                recordId: recordId,
                commandId: commandId
            }, function (data) {
                if (data.Message && data.Message.length > 0) {
                    inputForm.message(data.Message, "Information");
                } else if (data.Index && data.Record) {
                    data.Record.loaded = true;
                    if (data.Index == index) {
                        $scope.records[index] = data.Record;
                    } else {
                        $scope.records.splice(index, 1);
                        if (data.Index >= 0) {
                            $scope.records.splice(data.Index, 0, data.Record);
                        }
                    }
                    $scope.loadVisibleRecords();
                }
            }, function (message) {
                inputForm.message(message, "Error");
            });
        }

        if ($scope.data.Editable) {

            $scope.showRowProperties = function (row) {
                var record = row.entity;
                $scope.recordListOptions.selectAll(false);
                $scope.editRecord(record);
            }

            $scope.recordListOptions.columnDefs.splice(0, 0, {
                cellTemplate: "<div class='ngCellText grid-cell-edit' ng-click='showRowProperties(row)'><span></span></div>",
                width: 30
            });

            if (!$scope.viewData.DeleteOnly) {
                $scope.toolbarButtons.push({
                    Id: "recordlist-add",
                    Text: "Add"
                });

                commandHandler.subscribe('recordlist-add', function (sender, invoke) {
                    if (!invoke) {
                        return true;
                    }
                    $scope.addRecord();
                    return true;
                });

                $scope.toolbarButtons.push({
                    Id: "recordlist-remove",
                    Text: "Remove"
                });

                commandHandler.subscribe("recordlist-remove", function (sender, invoke) {
                    if ($scope.selectedItems.length == 0) {
                        return false;
                    }
                    if (!invoke) {
                        return true;
                    }
                    $scope.deleteRecords($scope.selectedItems);
                    $scope.recordListOptions.selectAll(false);
                    return true;
                });
            }
        }

        function createNewRecord(recordId) {
            var record = { loaded: false };
            for (var i = 0; i < $scope.data.Columns.length; i++) {
                record[$scope.data.Columns[i].field] = "";
            }
            record[$scope.data.RecordId] = recordId;
            return record;
        }

        for (var i = 0; i < $scope.data.RecordIds.length; i++) {
            var record = createNewRecord($scope.data.RecordIds[i]);
            $scope.records.push(record);
        }

        $scope.$watch("currentPage", function () {

            if (!$scope.pageLoaded) {
                return;
            }
            $scope.loadVisibleRecords();
        });

        $scope.showPage = function (page) {

            $scope.totalItems = $scope.records.length;
            var itemsPerPage = 20;
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

            $scope.pageRecords = [];

            var firstEmptyRecord = -1;
            var lastEmptyRecord = -1;
            for (var i = 0; i < $scope.pageSize; i++) {
                var record = $scope.records[i + $scope.pageOffset];
                $scope.pageRecords.push(record);
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
                    var recordId = $scope.records[i + $scope.pageOffset][$scope.data.RecordId];
                    recordIds.push(recordId);
                }

                $scope.sendAction("Fetch", {
                    ids: recordIds
                }, function (data) {
                    $scope.updateRecords(data.Records);
                });
            }

            $scope.showPagination = $scope.pageSize < $scope.totalItems;

            if (!$scope.$$phase) {
                $scope.$apply();
            }

            $scope.pageLoaded = true;
        }

        $scope.updateRecords = function (records) {
            var visibleChanged = false;
            for (var i = 0; i < records.length; i++) {
                var record = records[i];
                record.loaded = true;
                var recordId = record[$scope.data.RecordId];
                for (var j = 0; j < $scope.records.length; j++) {
                    var currentRecord = $scope.records[j];
                    var currentRecordId = currentRecord[$scope.data.RecordId];
                    if (currentRecordId != recordId) {
                        continue;
                    }
                    $scope.records[j] = record;
                    if (j >= $scope.pageOffset && j < $scope.pageSize + $scope.pageOffset) {
                        $scope.pageRecords[j - $scope.pageOffset] = record;
                        visibleChanged = true;
                    }
                    break;
                }
            }
            if (visibleChanged) {
                $scope.pageRecords = angular.copy($scope.pageRecords);
                if (!$scope.$$phase) {
                    $scope.$apply();
                }
            }
        }

        $scope.loadVisibleRecords = function () {
            $scope.showPage($scope.currentPage);
        }

        $scope.showPage(1);
    });
});
