angular.module('umbraco').controller('uSyncSnapshotDashboardController',
    function ($scope, $http, uSyncSnapshotDashboardService) {

        $scope.loading = true;
		
	   GetSnapshots();

        function GetSnapshots() {

            uSyncSnapshotDashboardService.getSnapshots()
            .then(function (response) {
                $scope.snapshots = response.data;
                $scope.loading = false; 
            });
        }
		
	$scope.CreateSnapshot = function()
	{
		alert ($scope.snapshotName);
          uSyncSnapshotDashboardService.createSnapshot($scope.snapshotName)
            .then(function (response) {
			alert(response);
            });
		
	}

 });