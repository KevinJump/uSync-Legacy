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

            switch(operation.ToLower())
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
            await Out.WriteLineAsync("\t\t [more to come]");

            await Out.WriteLineAsync("\n\t\t example : usync import macro/test.config");

        }

        private async Task Import(string[] args)
        {

            await Out.WriteLineAsync("Importing : " + args[0]);

            if (!args.Any()) {
                await Out.WriteLineAsync("please provide a filename to import");
                return;
            }

            var file = FindFile(args[0]);
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
                    break;
                case "documenttype":
                    break;
                case "macro":
                    var attempt = uSyncCoreContext.Instance.MacroSerializer.DeSerialize(node, true);
                    if (attempt.Success)
                    {
                        await Out.WriteLineAsync(string.Format("Imported {0} ", attempt.Name));
                    }
                    else
                    {
                        await Out.WriteLineAsync(
                            string.Format("failed to import {0} : {1}", attempt.Name, attempt.Message));
                    }
                    break;
                default:
                    await Out.WriteLineAsync("Didn't reconise file type " + type);
                    break;
            }

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

            return string.Empty;
        }

    }
}