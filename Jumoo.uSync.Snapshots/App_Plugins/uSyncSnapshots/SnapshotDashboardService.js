angular.module('umbraco.resources').factory('uSyncSnapshotDashboardService',
    function ($q, $http) {
		
        var serviceRoot = 'backoffice/uSync/SnapshotService/';
        var downloadService = 'backoffice/uSync/SnapshotDownload/';

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
		  },

		  zipFile: function (name) {
		      return $http.get(downloadService + "GetZipFile/?name=" + name);
		  },

		  zipAll: function () {
		      return $http.get(downloadService + "GetAll/");
		  },

		  fileUpload: function (file) {

		      return $http({
		          method: 'POST',
		          url: downloadService + "uploadFile",
		          headers: { 'Content-Type': undefined },
		          transformRequest: function (data) {
		              var formData = new FormData();
		              formData.append("file", file);
		              return formData;
		          },
		          data: file
		      });
		  }



        }
    });