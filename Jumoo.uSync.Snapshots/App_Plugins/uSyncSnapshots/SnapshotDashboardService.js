angular.module('umbraco.resources').factory('uSyncSnapshotDashboardService',
    function ($q, $http) {
		
	   var serviceRoot = 'backoffice/uSync/SnapshotService/';

        return {
            getSnapshots: function () {
                return $http.get(serviceRoot + 'GetSnapshots');
            },
			
		  createSnapshot: function (name) {
			  return $http.get(serviceRoot + 'CreateSnapshot/?name=' + name);
		  },
		  
		  getSettings: function() {
			  return $http.get(serviceRoot + 'GetSnapshotSettings');
		  },
		  
		  applyAll: function() {
			  return $http.get(serviceRoot + 'ApplyAll');
		  },
		  
		  reportAll: function () {
			  return $http.get(serviceRoot + 'ReportAll');
		  },
		  
		  apply: function(name) {
			  return $http.get(serviceRoot + 'Apply/?snapshotName=' + name);
		  },

		  report: function(name) {
			  return $http.get(serviceRoot + 'Report/?snapshotName=' + name);
		  },
		  
		  delete: function(name) {
			  return $http.get(serviceRoot + 'Delete/?snapshotName=' + name);
		  }
        }
    });