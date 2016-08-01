angular.module('umbraco.resources').factory('uSyncDashboardService',
    function ($q, $http) {

        var serviceRoot = 'backoffice/uSync/uSyncApi/';

        return {
            getSettings: function () {
                return $http.get(serviceRoot + 'GetSettings');
            },

            importer: function (force) {
                return $http.get(serviceRoot + 'Import/?force=' + force);
            },

            exporter: function () {
                return $http.get(serviceRoot + 'Export');
            },

            reporter: function() {
                return $http.get(serviceRoot + 'Report');
            },
            updateSettings: function (mode) {
                return $http.get(serviceRoot + 'UpdateSyncMode/?mode=' + mode);
            },
            getHistory: function () {
                return $http.get(serviceRoot + 'GetHistory');
            },
            clearHistory: function () {
                return $http.get(serviceRoot + "ClearHistory");
            },
            getuSyncActions: function () {
                return $http.get(serviceRoot + "GetActions");
            },
            removeuSyncAction: function (name, type) {
                return $http.get(serviceRoot + "RemoveAction/?name=" + name + "&type=" + type);
            }
        }
    });