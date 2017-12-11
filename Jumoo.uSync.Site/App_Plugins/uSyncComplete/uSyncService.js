(function () {

    'use strict';

    function uSyncService($http) {

        var serviceRoot = Umbraco.Sys.ServerVariables.uSync.uSyncService;

        var service = {
            getSettings : getSettings
        };

        return service;

        //////////////////////

        function getSettings() {
            return $http.get(serviceRoot + "GetSettings");
        }
    }

    angular.module('umbraco.services')
        .factory('uSyncService', uSyncService);

})();