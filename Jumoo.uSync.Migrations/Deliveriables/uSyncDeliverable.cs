using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Chauffeur;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

using Jumoo.uSync.Core;
using System.Reflection;

namespace Jumoo.uSync.Migrations.Deliveriables
{
    [DeliverableName("usync")]
    [DeliverableAlias("us")]
    public class uSyncDeliverable : Deliverable, IProvideDirections
    {
        public uSyncDeliverable(TextReader reader, TextWriter writer)
            : base(reader, writer)
        {
            uSyncCoreContext.Instance.Init();
        }

        public override async Task<DeliverableResponse> Run(string command, string[] args)
        {
            var operation = args[0];

            switch (operation.ToLower())
            {
                case "list":
                    break;
                case "import":
                    await Import(args.Skip(1).ToArray());
                    break;
                case "export":
                    break;
                case "create-snapshot":
                    break;
                case "import-snapshot":
                    break;
                case "run-migration":
                    break;
            }
            return DeliverableResponse.Continue;
        }

        public async Task Directions()
        {
            await Out.WriteLineAsync("usync action <commands>");
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("actions:");
            await Out.WriteLineAsync("\t list [type]");
            await Out.WriteLineAsync("\t import filename [-all]");
            await Out.WriteLineAsync("\t export type name filename");
            await Out.WriteLineAsync("\t create-snapshot name");
            await Out.WriteLineAsync("\t import-snapshot name");
            await Out.WriteLineAsync("\t run-migration");

            await Out.WriteLineAsync("\n\t uSync will look for files in:");
            await Out.WriteLineAsync("\t\t uSync\\data");
            await Out.WriteLineAsync("\t\t and subfolders of any sepcified folder");

            await Out.WriteLineAsync("\n\t\t example : usync import macro/test.config");
            await Out.WriteLineAsync("\n\t\t\t will search ~/usytnc/data/macro/test.config");
            await Out.WriteLineAsync("\n\t\t\t\t and any sub folders of ~/uSync/data/macro/");


        }

        private async Task Import(string[] args)
        {

            await Out.WriteLineAsync("Importing : " + args[0]);

            if (!args.Any()) {
                await Out.WriteLineAsync("please provide a filename to import");
                return;
            }


            if (args.Length > 1)
            {
                switch (args[1].ToLower())
                {
                    case "-folder":
                        // folder import
                        await ImportFolders(args[0]);
                        break;
                }
            }
            else
            {
                var file = FindFile(args[0]);
                await ImportFile(file);
            }

        }

        private async Task ImportFolders(string path)
        {
            var fullPath = FindFolder(path);
            if (String.IsNullOrEmpty(fullPath))
                return;
             
            await Out.WriteLineAsync("Importing Folder: " + fullPath);

            if (Directory.Exists(fullPath))
            {
                foreach(var file in Directory.GetFiles(fullPath, "*.config"))
                {
                    await ImportFile(file);
                }

                foreach(var folder in Directory.GetDirectories(fullPath))
                {
                    await ImportFolders(folder);
                }
            }
        }


        private async Task ImportFile(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                await Out.WriteLineAsync("cannot find the file");
                return;
            }

            XElement node = XElement.Load(file);
            if (node == null)
            {
                await Out.WriteLineAsync("Failed to load file");
                return;
            }

            var type = node.Name.LocalName.ToLower();
            switch (type)
            {
                case "datatype":
                    await ImportDataType(node);
                    break;
                case "documenttype":
                    await ImportDocType(node);
                    break;
                case "macro":
                    await ImportMacro(node);
                    break;
                case "dictionaryitem":
                    await ImportDictionaryItem(node);
                    break;
                case "language":
                    await ImportLanguage(node);
                    break;
                case "mediatype":
                    await ImportMediaType(node);
                    break;
                case "membertype":
                    await ImportMemberType(node);
                    break;
                default:
                    await Out.WriteLineAsync("Didn't reconise file type " + type);
                    break;
            }

        }


        private async Task ImportDataType(XElement node)
        {
            var attempt = uSyncCoreContext.Instance.DataTypeSerializer.DeSerialize(node, true);
            if (attempt.Success)
            {
                await Out.WriteLineAsync("Imported datatype: " + attempt.Name);
            }
            else
            {
                var error = attempt.Message;
                if (attempt.Exception != null)
                    error = error + " " + attempt.Exception.ToString();
                await Out.WriteLineAsync(string.Format("Failed to import: {0}", error));
            }
        }

        private async Task ImportDocType(XElement node)
        {
            var attempt = uSyncCoreContext.Instance.ContentTypeSerializer.DeSerialize(node, true);
            if (attempt.Success)
            {
                await Out.WriteLineAsync("Imported datatype: " + attempt.Name);
            }
            else
            {
                var error = attempt.Message;
                if (attempt.Exception != null)
                    error = error + " " + attempt.Exception.ToString();
                await Out.WriteLineAsync(string.Format("Failed to import: {0}", error));
            }
        }

        private async Task ImportMacro(XElement node)
        {
            var attempt = uSyncCoreContext.Instance.MacroSerializer.DeSerialize(node, true);
            if (attempt.Success)
            {
                await Out.WriteLineAsync("Imported datatype: " + attempt.Name);
            }
            else
            {
                var error = attempt.Message;
                if (attempt.Exception != null)
                    error = error + " " + attempt.Exception.ToString();
                await Out.WriteLineAsync(string.Format("Failed to import: {0}", error));
            }
        }
        private async Task ImportDictionaryItem(XElement node)
        {
            var attempt = uSyncCoreContext.Instance.DictionarySerializer.DeSerialize(node, true);
            if (attempt.Success)
            {
                await Out.WriteLineAsync("Imported datatype: " + attempt.Name);
            }
            else
            {
                var error = attempt.Message;
                if (attempt.Exception != null)
                    error = error + " " + attempt.Exception.ToString();
                await Out.WriteLineAsync(string.Format("Failed to import: {0}", error));
            }
        }
        private async Task ImportLanguage(XElement node)
        {
            var attempt = uSyncCoreContext.Instance.LanguageSerializer.DeSerialize(node, true);
            if (attempt.Success)
            {
                await Out.WriteLineAsync("Imported datatype: " + attempt.Name);
            }
            else
            {
                var error = attempt.Message;
                if (attempt.Exception != null)
                    error = error + " " + attempt.Exception.ToString();
                await Out.WriteLineAsync(string.Format("Failed to import: {0}", error));
            }
        }
        private async Task ImportMediaType(XElement node)
        {
            var attempt = uSyncCoreContext.Instance.MediaTypeSerializer.DeSerialize(node, true);
            if (attempt.Success)
            {
                await Out.WriteLineAsync("Imported datatype: " + attempt.Name);
            }
            else
            {
                var error = attempt.Message;
                if (attempt.Exception != null)
                    error = error + " " + attempt.Exception.ToString();
                await Out.WriteLineAsync(string.Format("Failed to import: {0}", error));
            }
        }
        private async Task ImportMemberType(XElement node)
        {
            var attempt = uSyncCoreContext.Instance.MemberTypeSerializer.DeSerialize(node, true);
            if (attempt.Success)
            {
                await Out.WriteLineAsync("Imported datatype: " + attempt.Name);
            }
            else
            {
                var error = attempt.Message;
                if (attempt.Exception != null)
                    error = error + " " + attempt.Exception.ToString();
                await Out.WriteLineAsync(string.Format("Failed to import: {0}", error));
            }
        }

        private string FindFolder(string folder)
        {
            if (Directory.Exists(folder))
                return folder;

            var siteRoot = Path.Combine(
                new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName, "..");

            // try different paths...
            var path = Path.Combine(siteRoot, "usync", "data", folder);

            if (Directory.Exists(path))
                return path;

            // do a folder search?
            // return FindFile(Path.Combine(siteRoot, "usync", "data", Path.GetDirectoryName(file)), Path.GetFileName(file));

            return string.Empty;
        }

        private string FindFile(string file)
        {
            if (File.Exists(file))
                return file;

            var siteRoot = Path.Combine(
                new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName, "..");

            // try different paths...
            var path = Path.Combine(siteRoot, "usync", "data", file);

            if (File.Exists(path))
                return path;

            // do a folder search?
            // return FindFile(Path.Combine(siteRoot, "usync", "data", Path.GetDirectoryName(file)), Path.GetFileName(file));

            return string.Empty;
        }

        private string FindFile(string folder, string name)
        {
            var fileName = Path.Combine(folder, name);
            if (File.Exists(fileName))
                return fileName;

            foreach (var child in Directory.GetDirectories(folder))
            {
                var f = FindFile(child, name);
                if (!string.IsNullOrEmpty(f))
                    return f;
            }

            return string.Empty;
        }


    }
}