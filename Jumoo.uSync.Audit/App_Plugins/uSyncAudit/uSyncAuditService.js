(function () {

    'use strict';

    function uSyncAuditService($http) {

        var serviceRoot = "backoffice/usync/uSyncAuditApi/";

        var service = {
            getChanges: getChanges,
            getItems: getItems
        };

        return service;

        ////////////////

        function getChanges(pageNo) {
            return $http.get(serviceRoot + "GetChanges?page=" + pageNo);
        }

        function getItems(id) {
            return $http.get(serviceRoot + "GetItems/" + id);
        }
    }

    angular.module('umbraco.services')
        .factory('uSyncAuditService', uSyncAuditService);

})();