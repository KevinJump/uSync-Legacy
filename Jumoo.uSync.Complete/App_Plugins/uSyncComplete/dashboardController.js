(function () {

    'use strict';

    function dashboardController($scope, notificationsService, uSyncService) {

        var vm = this;

        vm.loaded = false;
        vm.settings = {};       


        Init();

        ///////////////////

        function GetSettings() {

            uSyncService.GetSettings()
                .then(function (result) {
                    vm.settings = result.data;
                    vm.loaded = true;
                }, function (error) {
                    notificationsService.error('Failed', error.data.ExceptionMessage);
                });
        }

        function LoadTabs() {

        }

        ///////////////////

        function Init() {
            GetSettings();
        }
    }

    angular.module('umbraco')
        .controller('usyncCompleteDashboardController', dashboardController);

})();