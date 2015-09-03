using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Chauffeur;
using System.IO;
using Jumoo.uSync.Core;
using System.Threading.Tasks;
using Jumoo.uSync.BackOffice;

namespace Jumoo.uSync.Migrations.Deliveriables
{
    [DeliverableName("migration")]
    [DeliverableAlias("cs")]
    public class MigrationDeliverable : Deliverable, IProvideDirections
    {
        public MigrationDeliverable(TextReader reader, TextWriter writer)
            :base (reader, writer)
        {
            uSyncCoreContext.Instance.Init();
        }

        public async Task Directions()
        {
            await Out.WriteLineAsync("migration <actions> <name>");
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("Creates and manages migrations sets of your files.");
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("migration create [name]");
            // await Out.WriteLineAsync("migration import [name]");
            await Out.WriteLineAsync("migration import-all");
        }

        public override async Task<DeliverableResponse> Run(string command, string[] args)
        {
            if (args.Length > 0 )
            {
                var option = args[0].ToLower();

                switch(option)
                {
                    case "list":
                        await ListSnapshots();
                        break;
                    case "create":
                        await CreateMigration(args[1]);
                        break;
                    case "import":
                        await Out.WriteLineAsync("Not implimented - you should use import-all to ensure all changes are imported");
                        break;
                    case "import-all":
                        await ImportAll();
                        break;
                    default:
                        await Out.WriteLineAsync("Unreconised command " + args[0]);
                        break;
                }
            }
            return DeliverableResponse.Continue;
        }

        public async Task CreateMigration(string name)
        {
            uSyncBackOfficeContext.Instance.Init();

            await Out.WriteLineAsync("Creating ChangeSet: [" + name + "]");
            var snapshotManager = new MigrationManager("~/usync/migrations/");
            var info = snapshotManager.CreateMigration(name);

            if (info.FileCount == 0)
            {
                await Out.WriteLineAsync("Migration contains no changes, no folder created");
            }
            else
            {
                await Out.WriteLineAsync("Migration Created " + info.FileCount + " changes");
            }
            
        }

        public async Task ImportAll()
        {
            uSyncBackOfficeContext.Instance.Init();

            await Out.WriteLineAsync("merging all Migrations");
            var snapshotManager = new MigrationManager("~/usync/migrations/");
            var actions = snapshotManager.ApplyMigrations();

            if (actions.Any() && actions.Any(x => x.Change > ChangeType.NoChange))
            {
                await Out.WriteLineAsync(
                    string.Format("Snapshots Imported: {0} items, {1} changes ", actions.Count(), actions.Count(x => x.Change > ChangeType.NoChange)));

                foreach (var action in actions.Where(x => x.Change > ChangeType.NoChange))
                {
                    await Out.WriteLineAsync(string.Format("Action: {0,-30} {1,10}", action.Name, action.Change));
                }
            }
            else
            {
                await Out.WriteLineAsync(string.Format("items {0} processed, no changes made", actions.Count()));
            }
        }

        public async Task ListSnapshots()
        {
            uSyncBackOfficeContext.Instance.Init();

            var snapshotManager = new MigrationManager("~/usync/migrations/");
            var snaps = snapshotManager.ListMigrations();
            if (snaps.Any())
            {
                await Out.WriteLineAsync("Found " + snaps.Count() + " migrations\n=================");
                foreach (var snap in snaps)
                {
                    await Out.WriteLineAsync(string.Format("{0,-30} {1} [{2} file{3}]", snap.Name, snap.Time, snap.FileCount, snap.FileCount > 1 ? "s" : ""));
                }
            }
            else
            {
                await Out.WriteLineAsync("there are no migration - use migration create [name] to create a change");
            }
        }
    }
}