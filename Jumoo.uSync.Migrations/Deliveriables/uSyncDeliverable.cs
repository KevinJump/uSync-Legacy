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
            string invokedVerb = string.Empty;
            object invokedInstance = null;

            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, options,
                (verb, subOptions) =>
                    {
                        invokedVerb = verb;
                        invokedInstance = subOptions;
                    }))
            {
                // didn't work..
                return DeliverableResponse.Continue;
            }

            switch (invokedVerb)
            {
                case "list":
                    break;
                case "import":
                    ImportCommand importer = new ImportCommand(In, Out);
                    await importer.Process((ImportOptions)invokedInstance);
                    break;
                case "export":
                    ExportCommand exporter = new ExportCommand(In, Out);
                    await exporter.Process((ExportOptions)invokedInstance);
                    break;
                /* snapshots will be in own deliveriable. 
                case "create-snapshot":
                    break;
                case "import-snapshot":
                    break;
                case "run-migration":
                    break;
                */
            }
            return DeliverableResponse.Continue;
        }

        public async Task Directions()
        {
            await Out.WriteLineAsync("usync action <commands>");
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("actions:");
            await Out.WriteLineAsync("\t list [type]");
            await Out.WriteLineAsync("\t import -f filename [-force] [-folder]");
            await Out.WriteLineAsync("\t export -t type -n name -f filename");

            await Out.WriteLineAsync("\n\t uSync will look for files in:");
            await Out.WriteLineAsync("\t\t uSync\\data");
            await Out.WriteLineAsync("\t\t and subfolders of any sepcified folder");

            await Out.WriteLineAsync("\n\t\t example : usync import -f macro/test.config");
            await Out.WriteLineAsync("\n\t\t\t will search ~/usytnc/data/macro/test.config");
            await Out.WriteLineAsync("\n\t\t\t\t and any sub folders of ~/uSync/data/macro/");
        }
    }
}