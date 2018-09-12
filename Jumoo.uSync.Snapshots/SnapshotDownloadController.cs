using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Umbraco.Core.IO;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

namespace Jumoo.uSync.Snapshots
{
    [PluginController("uSync")]

    public class SnapshotDownloadController : UmbracoAuthorizedApiController
    {
        private readonly SnapshotManager manager; 
        public SnapshotDownloadController()
        {
            var root = IOHelper.MapPath("~/uSync/Snapshots");
            manager = new SnapshotManager(root);
        }

        [HttpGet]
        public string GetZipFile(string name)
        {
            var fullPath = manager.ZipSnapshot(name);
            return "/" + fullPath.Substring(IOHelper.MapPath("~/").Length).Replace("\\", "/");
        }

        [HttpGet]
        public ZipFileInfo GetAll()
        {
            var fullPath = manager.ZipAll();
            return new ZipFileInfo
            {
                Path = "/" + fullPath.Substring(IOHelper.MapPath("~/").Length).Replace("\\", "/")
            };
        }

        [HttpPost]
        public async Task<HttpResponseMessage> UploadFile()
        {
            if (!Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(System.Net.HttpStatusCode.UnsupportedMediaType);

            var response = Request.CreateResponse(System.Net.HttpStatusCode.OK);

            var uploadFolder = IOHelper.MapPath("~/App_Data/uSync/snapshot/upload");
            System.IO.Directory.CreateDirectory(uploadFolder);

            var provider = new CustomMultipartFormDataStreamProvider(uploadFolder);
            var result = await Request.Content.ReadAsMultipartAsync(provider);
            var filename = result.FileData.First().LocalFileName;

            // unzip this into the snapshots folder. 
            var name = manager.UnZipFolder(filename);
            response.Content = new StringContent(name);
            return response;
        }

        public class ZipFileInfo
        {
            public string Path { get; set; }
        }
    }

    public class CustomMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        public CustomMultipartFormDataStreamProvider(string path) : base(path) { }

        public override string GetLocalFileName(HttpContentHeaders headers)
        {
            return headers.ContentDisposition.FileName.Replace("\"", string.Empty);
        }
    }
}
