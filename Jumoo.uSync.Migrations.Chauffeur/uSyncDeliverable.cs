using Chauffeur;
using Jumoo.uSync.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.Migrations.Chauffeur
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
            if (args.Length == 0) {
                await Out.WriteLineAsync("Unknown command");
                return DeliverableResponse.FinishedWithError;
            }

            string action = args[0].ToLower();
            bool result = false;

            switch (action)
            {
                case "list":
                    break;
                case "import":
                    var importer = new uSyncImporter(In, Out);
                    result = await importer.Import(args.Skip(1).ToArray());
                    break;
                case "export":
                    var exporter = new uSyncExporter(In, Out);
                    result = await exporter.Export(args.Skip(1).ToArray());
                    break;
            }

            return DeliverableResponse.Continue;
        }


        public async Task Directions()
        {
            await Out.WriteLineAsync("usync <action> <options...>");
        }

    }
}
