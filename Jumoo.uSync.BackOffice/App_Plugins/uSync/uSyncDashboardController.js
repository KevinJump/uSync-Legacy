angular.module('umbraco').controller('uSyncDashboardController',
    function ($scope, $http, uSyncDashboardService) {

        $scope.snapshotsUrl = "/app_plugins/usync/test.html";

        $scope.loading = true;
        $scope.uSyncMode = 'other';

        $scope.reporting = false;
        $scope.reported = false;
        $scope.showSettings = false;
        $scope.showTechnical = false;
        $scope.showActions = false;
        $scope.deleteActionFile = false;

        LoadSettings();

        function LoadSettings() {
            uSyncDashboardService.getSettings()
            .then(function (response) {
                $scope.settings = response.data;

                $scope.getuSyncMode();
                $scope.loading = false;

                $scope.loadHistory();
            });
        }

        $scope.loadHistory = function () {
            uSyncDashboardService.getHistory()
            .then(function (response) {
                $scope.history = response.data;
            });
        };

        $scope.loadActions = function () {
            $scope.reported = false;

            uSyncDashboardService.getuSyncActions()
            .then(function (response) {
                $scope.uSyncActions = response.data;
                if ($scope.uSyncActions != 0) {
                    $scope.showActions = true;
                }
            });
        }

        $scope.removeAction = function (name, type) {
            var c = confirm("be careful if you delete an action, you can't get it back");
            if (c) {
                uSyncDashboardService.removeuSyncAction(name, type)
                .then(function (response) {
                    $scope.uSyncActionResponse = response.data;
                    $scope.loadActions();
                });
            }

        }

        $scope.clearHistory = function () {
            var c = confirm('are you sure ? if you delete the history, there is no way of getting it back');

            if (c) {
                uSyncDashboardService.clearHistory()
                .then(function (response) {
                    $scope.historyCleared = response.data;
                    $scope.loadHistory();
                });
            }
        };

        $scope.updateSettings = function () {
            $scope.clearError();

            uSyncDashboardService.updateSettings($scope.uSyncMode)
            .then(function (response) {
                alert('settings updated, you will need to restart your site to see the changes');
            }, function (error) {
                $scope.working = false;
                $scope.setError(error.data);
            })

        }

        $scope.toggleSettings = function () {
            $scope.showSettings = !$scope.showSettings;
        }

        $scope.toggleTechnical = function () {
            $scope.showTechnical = !$scope.showTechnical;
        }

        $scope.getuSyncMode = function () {
            if ($scope.settings.settings.Import) {
                if ($scope.settings.settings.ExportOnSave) {
                    $scope.uSyncMode = 'auto'
                }
                else {
                    $scope.uSyncMode = 'target';
                }
            }
            else {
                if (!$scope.settings.settings.ExportAtStartup) {
                    if ($scope.settings.settings.ExportOnSave) {
                        $scope.uSyncMode = 'source';
                    }
                    else {
                        $scope.uSyncMode = 'manual';
                    }
                }
                else {
                    $scope.uSyncMode = 'other';
                }
            }
        }

        /* Back Office functions */
        $scope.import = function (force) {
            $scope.clearError();

            $scope.reporting = true;
            $scope.reported = false;
            $scope.showActions = false;

            $scope.reportName = "Import";

            uSyncDashboardService.importer(force)
            .then(function (response) {
                $scope.changes = response.data;
                $scope.reporting = false;
                $scope.reported = true;

                $scope.loadHistory();
            }, function (error) {
                $scope.working = false;
                $scope.setError(error.data);
            });
        }

        $scope.report = function () {
            $scope.clearError();

            $scope.reporting = true;
            $scope.reported = false;
            $scope.showActions = false;

            $scope.reportName = "Report";

            uSyncDashboardService.reporter()
            .then(function (response) {
                $scope.changes = response.data;
                $scope.reporting = false;
                $scope.reported = true;
            }, function (error) {
                $scope.working = false;
                $scope.setError(error.data);
            });
        }

        $scope.export = function () {
            $scope.clearError();

            $scope.reporting = true;
            $scope.reported = false;
            $scope.showActions = false;

            $scope.reportName = "Export";

            $scope.loadHistory();

            uSyncDashboardService.exporter($scope.deleteActionFile)
            .then(function (response) {
                $scope.changes = response.data;
                $scope.reporting = false;
                $scope.reported = true;
            }, function (error) {
                $scope.working = false;
                $scope.setError(error.data);
            });
        }


        /* results display */
        $scope.getChangeCount = function (changes) {
            var count = 0;

            angular.forEach(changes, function (val, key) {
                if (val.Change > 0) {
                    count++;
                }
            });

            return count;
        }

        $scope.changeVals = [
            'NoChange',
            'Import',
            'Export',
            'Update',
            'Delete',
            'WillChange',
            'Information',
            'Rolledback',
           '',
           '',
           '',
            'Fail',
            'ImportFail',
            'Mismatch'
        ];

        $scope.getChangeName = function (changeValue) {
            if (changeValue < 0) {
                return "archived item";
            }
            return $scope.changeVals[changeValue];
        }

        $scope.getTypeName = function (changeType) {
            var umbType = changeType.substring(0, changeType.indexOf(','));
            return umbType.substring(umbType.lastIndexOf('.') + 1);
        }

        $scope.isDateSet = function (dateValue) {
            return (new Date(dateValue) > $scope.minDate);
        }

        $scope.detailValues = ["Node", "Element", "Attribute", "Value"];
        $scope.detailTypes = ["Create", "Update", "Delete", "Error"];

        $scope.getDetailValueType = function (valueType) {
            return $scope.detailValues[valueType];
        }

        $scope.getDetailChange = function (changeType) {
            return $scope.detailTypes[changeType];
        }

        $scope.getDetailValue = function (val) {
            if (val == "") {
                return "(blank)";
            }
            else if (val == null) {
                return "-";
            }
            return val;
        }

        $scope.showDetail = function (change) {
            if (change.showDetail == true) {
                change.showDetail = false;
            }
            else {
                change.showDetail = true;
            }
        }


        $scope.showNoChange = false;

        $scope.showChange = function (changeValue) {
            if ($scope.showNoChange || changeValue > 0) {
                return true;
            }
            return false;
        }

        $scope.setError = function (error) {
            $scope.errorMsg = error.Message + ' - ' + error.ExceptionMessage;
            $scope.isInError = true;
        }

        $scope.clearError = function () {
            $scope.errorMsg = "";
            $scope.isInError = false;
        }

        $scope.visibleHandler = -1;

        $scope.toggleGroup = function (index) {
            if ($scope.visibleHandler == index) {
                $scope.visibleHandler = -1;
            }
            else {
                $scope.visibleHandler = index;
            }
        }

        $scope.showToggle = function (index) {
            return $scope.visibleHandler == index;
        }

    });