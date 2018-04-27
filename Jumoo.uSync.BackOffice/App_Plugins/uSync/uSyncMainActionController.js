(function () {

    'use strict';

    function mainActionController($scope) {
        var vm = this;

        vm.state = function () {
            return $scope.reporting ? "busy" : "init";
        }

        vm.importGroup = {
            defaultButton: {
                labelKey: "usync_import",
                handler: importItems
            },
            subButtons: [
                {
                    labelKey: "usync_importFull",
                    handler: importAll
                },
                {
                    labelKey: "usync_report",
                    handler: report
                }
            ]
        };

        vm.exportGroup = {
            defaultButton: {
                labelKey: "usync_export",
                handler: exportItems
            },
            subButtons: [
                {
                    labelKey: "usync_exportFull",
                    handler: exportAll
                }
            ]
        };

        ////////////
        function importItems() {
            $scope.import(false);
        }

        function importAll() {
            $scope.import(true);
        }

        function exportItems() {
            $scope.export(false);
        }

        function exportAll() {
            $scope.export(true);
        }

        function report() {
            $scope.report();
        }
    }

    angular.module('umbraco')
        .controller('uSync.MainAction.Controller', mainActionController);
})();