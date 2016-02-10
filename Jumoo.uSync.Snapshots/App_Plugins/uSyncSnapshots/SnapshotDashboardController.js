angular.module('umbraco').controller('uSyncSnapshotDashboardController',
    function ($scope, $http, uSyncSnapshotDashboardService) {

	   $scope.minDate = new Date('2000/1/1');
	
        $scope.loading = true;
	   $scope.working = false;

	   $scope.reporting = false;
	   $scope.reported = false;
	   
	   LoadSettings();
	   GetSnapshots();
	   
	   function LoadSettings() {
		   uSyncSnapshotDashboardService.getSettings()
		   .then(function(response) {
			   $scope.settings = response.data; 
			   $scope.settings.Mode = "combined" ;
		   });
	   }

        function GetSnapshots() {

            uSyncSnapshotDashboardService.getSnapshots()
            .then(function (response) {
                $scope.snapshots = response.data;
                $scope.loading = false; 
            });
        }
		
	$scope.CreateSnapshot = function()
	{
		$scope.reporting = false;
		$scope.reported = false;
		$scope.noChanges = false;
		$scope.working = true;
		
          uSyncSnapshotDashboardService.createSnapshot($scope.snapshotName)
            .then(function (response) {
			$scope.working = false; 
			
			if (response.data.FileCount > 0) 
			{
				$scope.snapshots.push(response.data);
			}
			else 
			{
				$scope.noChanges = true;
			}
            });
		
	}
	
	$scope.refresh = function(hide)
	{
		$scope.noChanges = false;
		$scope.loading = hide; 
		GetSnapshots();
	}
	
	$scope.apply = function(name) 
	{
		var result = confirm('Applying snapshots out of sync can be dangerous\n\n' + 
		'You should consider using "Apply All" to make sure the combined changes are written to disk.' + 
		'Only the changes that are different from whats on the the site will be applied\n\n' +
		'Do you really want to apply a single snapshot?');

		if (result == true)
		{
			$scope.reporting = true;
			$scope.reported = false;
			$scope.reportName = "Snapshot: " + name + " "; 

			  uSyncSnapshotDashboardService.apply(name)
				.then(function (response) {
				$scope.changes = response.data; 
				$scope.reporting = false;
				$scope.reported = true;
				$scope.refresh(false);
			   });


		}
		
	}

	$scope.report = function(name) 
	{
		alert("Running snapshots out of sequence isn't recommended. " +
		 'You can end up with old values overwriting newer ones\n\n' + 
		 'when applying snapshots consider using "Apply All"');
		 
		$scope.reporting = true;
		$scope.reported = false;
		$scope.reportName = "Snapshot report: " + name + " "; 

		uSyncSnapshotDashboardService.report(name)
		.then(function (response) {
			$scope.changes = response.data; 
			$scope.reporting = false;
			$scope.reported = true;
            });
	}
	
	$scope.delete = function(name) 
	{
		var result = confirm('Are you sure you want to delete this snapshot?\n');
		if (result == true)
		{
			uSyncSnapshotDashboardService.delete(name)
			.then(function(response) {
				$scope.refresh(false);
			});
		}
		 
	}
	
	$scope.applyAll = function() 
	{
		$scope.reporting = true;
		$scope.reported = false;
		$scope.reportName = "All Snapshots "; 

          uSyncSnapshotDashboardService.applyAll()
            .then(function (response) {
			$scope.changes = response.data; 
			$scope.reporting = false;
			$scope.reported = true;
			$scope.refresh(false);
           });
		   
	}
	
	$scope.reportAll = function() 
	{
		  $scope.reporting = true;
		  $scope.reported = false;
		  $scope.reportName = "All Snapshots Report"; 
		  
	       uSyncSnapshotDashboardService.reportAll()
            .then(function (response) {
			$scope.changes = response.data; 
			$scope.reporting = false;
			$scope.reported = true;
            });
	}
	
	$scope.getChangeCount = function()
	{
		var count = 0;
	
		angular.forEach($scope.changes, function(val, key) {
			if (val.Change > 0) 
			{
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
	
	$scope.getChangeName = function(changeValue)
	{
		return $scope.changeVals[changeValue];
	}
	
	$scope.getTypeName = function(changeType)
	{
		var umbType = changeType.substring(0, changeType.indexOf(','));
		return umbType.substring(umbType.lastIndexOf('.')+1);
	}
	
	$scope.isDateSet = function(dateValue)
	{
		return (new Date(dateValue) > $scope.minDate);
	}
	   
	$scope.showNoChange = false;
	
	$scope.showChange = function(changeValue)
	{
		if ($scope.showNoChange || changeValue > 0) {
			return true;
		}
		return false;
	}
 });