using Jumoo.uSync.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Jumoo.uSync.Migrations.Deliveriables
{
    public class ImportCommand
    {
        private TextReader In;
        private TextWriter Out;

        public ImportCommand(TextReader reader, TextWriter writer)
        {
            Out = writer;
            In = reader;
        }

        public async Task Import(ImportOptions options)
        {
            if (options.Folder)
            {
                await ImportFolders(options.FileName, options.Force);
            }
            else
            {
                var filePath = FindFile(options.FileName);
                await ImportFile(filePath, options.Force);
            }
        }

        private async Task ImportFolders(string path, bool force)
        {
            var fullPath = FindFolder(path);
            if (String.IsNullOrEmpty(fullPath))
                return;

            await Out.WriteLineAsync("Importing Folder: " + fullPath);

            if (Directory.Exists(fullPath))
            {
                foreach (var file in Directory.GetFiles(fullPath, "*.config"))
                {
                    await ImportFile(file, force);
                }

                foreach (var folder in Directory.GetDirectories(fullPath))
                {
                    await ImportFolders(folder, force);
                }
            }
        }


        private async Task ImportFile(string file, bool force)
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
                    await ImportDataType(node, force);
                    break;
                case "documenttype":
                    await ImportDocType(node, force);
                    break;
                case "macro":
                    await ImportMacro(node, force);
                    break;
                case "dictionaryitem":
                    await ImportDictionaryItem(node, force);
                    break;
                case "language":
                    await ImportLanguage(node, force);
                    break;
                case "mediatype":
                    await ImportMediaType(node, force);
                    break;
                case "membertype":
                    await ImportMemberType(node, force);
                    break;
                default:
                    await Out.WriteLineAsync("Didn't reconise file type " + type);
                    break;
            }

        }


        private async Task ImportDataType(XElement node, bool force)
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

        private async Task ImportDocType(XElement node, bool force)
        {
            var attempt = uSyncCoreContext.Instance.ContentTypeSerializer.DeSerialize(node, force);
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

        private async Task ImportMacro(XElement node, bool force)
        {
            var attempt = uSyncCoreContext.Instance.MacroSerializer.DeSerialize(node, force);
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
        private async Task ImportDictionaryItem(XElement node, bool force)
        {
            var attempt = uSyncCoreContext.Instance.DictionarySerializer.DeSerialize(node, force);
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
        private async Task ImportLanguage(XElement node, bool force)
        {
            var attempt = uSyncCoreContext.Instance.LanguageSerializer.DeSerialize(node, force);
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
        private async Task ImportMediaType(XElement node, bool force)
        {
            var attempt = uSyncCoreContext.Instance.MediaTypeSerializer.DeSerialize(node, force);
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
        private async Task ImportMemberType(XElement node, bool force)
        {
            var attempt = uSyncCoreContext.Instance.MemberTypeSerializer.DeSerialize(node, force);
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