using Jumoo.uSync.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace Jumoo.uSync.Migrations.Chauffeur
{
    /// <summary>
    ///  handles single imports to and from file locations...
    ///  really just wrapps the back office stuff...
    /// </summary>
    public class uSyncImporter
    {
        TextReader In;
        TextWriter Out;

        string SyncRoot;

        public uSyncImporter(TextReader reader, TextWriter writer)
        {
            In = reader;
            Out = writer;

            SyncRoot = Path.Combine(
                    new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName, "../uSync/Export/");

            uSyncCoreContext.Instance.Init();
        }

        public async Task<bool> Import(string[] args)
        {
            if (args == null || args.Length == 0)
                return false;

            var file = Path.Combine(SyncRoot, args[0]);

            var force = false;

            if (args.Length > 1 && args[1].ToLower() == "force")
                force = true;

            if (!File.Exists(file))
            {
                await Out.WriteLineAsync("Cannot find file");
                return false;
            }

            XElement node = XElement.Load(file);
            if (node == null)
            {
                await Out.WriteLineAsync("Failed to load XML");
                return false;
            }

            var type = node.Name.LocalName.ToLower();
            switch (type)
            {
                case "datatype":
                    await ImportDataType(node, force);
                    break;
                case "documenttype":
                    await ImportContentType(node, force);
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
                    await Out.WriteLineAsync("Didn't recognise type of item in the file " + type);
                    break;
            }

            return true;
        }


        public async Task ImportDataType(XElement node, bool force)
        {
            await Out.WriteAsync("Importing ");
            var attempt = uSyncCoreContext.Instance.DataTypeSerializer.DeSerialize(node, force);

            if (attempt.Success)
            {
                await Out.WriteLineAsync(attempt.Name + " complete");
            }
            else
            {
                var error = attempt.Exception != null ? attempt.Exception.ToString() : attempt.Message;
                await Out.WriteLineAsync("failed " + error);
            }
        }

        public async Task ImportContentType(XElement node, bool force)
        {
            await Out.WriteAsync("Importing ");
            var attempt = uSyncCoreContext.Instance.ContentTypeSerializer.Deserialize(node, force, true);

            if (attempt.Success)
            {
                await Out.WriteLineAsync(attempt.Name + " complete");
            }
            else
            {
                var error = attempt.Exception != null ? attempt.Exception.ToString() : attempt.Message;
                await Out.WriteLineAsync("failed " + error);
            }
        }

        public async Task ImportMacro(XElement node, bool force)
        {
            await Out.WriteAsync("Importing ");
            var attempt = uSyncCoreContext.Instance.MacroSerializer.DeSerialize(node, force);

            if (attempt.Success)
            {
                await Out.WriteLineAsync(attempt.Name + " complete");
            }
            else
            {
                var error = attempt.Exception != null ? attempt.Exception.ToString() : attempt.Message;
                await Out.WriteLineAsync("failed " + error);
            }
        }

        public async Task ImportDictionaryItem(XElement node, bool force)
        {
            await Out.WriteAsync("Importing ");
            var attempt = uSyncCoreContext.Instance.DictionarySerializer.DeSerialize(node, force);

            if (attempt.Success)
            {
                await Out.WriteLineAsync(attempt.Name + " complete");
            }
            else
            {
                var error = attempt.Exception != null ? attempt.Exception.ToString() : attempt.Message;
                await Out.WriteLineAsync("failed " + error);
            }
        }

        public async Task ImportLanguage(XElement node, bool force)
        {
            await Out.WriteAsync("Importing ");
            var attempt = uSyncCoreContext.Instance.LanguageSerializer.DeSerialize(node, force);

            if (attempt.Success)
            {
                await Out.WriteLineAsync(attempt.Name + " complete");
            }
            else
            {
                var error = attempt.Exception != null ? attempt.Exception.ToString() : attempt.Message;
                await Out.WriteLineAsync("failed " + error);
            }
        }

        public async Task ImportMediaType(XElement node, bool force)
        {
            await Out.WriteAsync("Importing ");
            var attempt = uSyncCoreContext.Instance.MediaTypeSerializer.Deserialize(node, force, true);

            if (attempt.Success)
            {
                await Out.WriteLineAsync(attempt.Name + " complete");
            }
            else
            {
                var error = attempt.Exception != null ? attempt.Exception.ToString() : attempt.Message;
                await Out.WriteLineAsync("failed " + error);
            }
        }


        public async Task ImportMemberType(XElement node, bool force)
        {
            await Out.WriteAsync("Importing ");
            var attempt = uSyncCoreContext.Instance.MemberTypeSerializer.Deserialize(node, force, true);

            if (attempt.Success)
            {
                await Out.WriteLineAsync(attempt.Name + " complete");
            }
            else
            {
                var error = attempt.Exception != null ? attempt.Exception.ToString() : attempt.Message;
                await Out.WriteLineAsync("failed " + error);
            }
        }

    }
}
