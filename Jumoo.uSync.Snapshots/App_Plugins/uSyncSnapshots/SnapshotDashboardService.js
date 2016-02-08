angular.module('umbraco.resources').factory('uSyncSnapshotDashboardService',
    function ($q, $http) {

        return {
            getSnapshots: function () {
                return $http.get('backoffice/uSync/SnapshotService/GetSnapshots');
            },
			
		  createSnapshot: function (name) {
			  return $http.post(
				'backoffice/uSync/SnapshotService/PostCreateSnapshot', name
			  );
  
			  
		  }
        }
    });