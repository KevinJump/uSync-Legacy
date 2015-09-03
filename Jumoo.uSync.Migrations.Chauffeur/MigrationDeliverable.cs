using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Chauffeur;
using System.IO;
using Jumoo.uSync.BackOffice;
using Jumoo.uSync.Core;

namespace Jumoo.uSync.Migrations.Chauffeur
{
    [DeliverableName("migration")]
    [DeliverableAlias("m")]
    public class MigrationDeliverable : Deliverable, IProvideDirections
    {
        public MigrationDeliverable(TextReader reader, TextWriter writer)
            : base(reader ,writer)
        { }

        public override async Task<DeliverableResponse> Run(string command, string[] args)
        {
            if (args.Length > 0)
            {
                var action = args[0].ToLower();

                switch(action)
                {
                    case "list":
                        await ListMigrations();
                        break;
                    case "create":
                        await Create(args[1]);
                        break;
                    case "import-all":
                        await ImportAll();
                        break;
                    default:
                        await Out.WriteLineAsync("unknown command");
                        break;
                }
            }

            return DeliverableResponse.Continue;
        }

        public async Task Create(string name)
        {
            uSyncBackOfficeContext.Instance.Init();

            if (string.IsNullOrEmpty(name))
                name = DateTime.Now.ToString("ddMMyyyy_HHmmss");

            await Out.WriteLineAsync("Creating new migration: " + name);

            var migrationManager = new MigrationManager("~/usync/migrations/");
            var actions = migrationManager.CreateMigration(name);

            if (actions.FileCount > 0)
            {
                await Out.WriteLineAsync("Migration Created with " + actions.FileCount + " files");
            }
            else
            {
                await Out.WriteLineAsync("Migration contained no changes and has not being saved");
            }
        }

        public async Task ImportAll()
        {
            uSyncBackOfficeContext.Instance.Init();

            await Out.WriteLineAsync("Importing all migration changes from disk");
            var migrationManager = new MigrationManager("~/usync/migrations/");

            var actions = migrationManager.ApplyMigrations();
            if (actions.Any(x => x.Change > ChangeType.NoChange))
            {
                await Out.WriteLineAsync(
                    string.Format("Migrations Imported {0} items, {1} changes",
                        actions.Count(), actions.Count(x => x.Change > ChangeType.NoChange
                    )));

                foreach(var action in actions.Where(x => x.Change > ChangeType.NoChange))
                {
                    await Out.WriteLineAsync(
                        string.Format("{0,-30}, {1,10} {2}", action.Name, action.Change, action.Message));
                }
            }
            else
            {
                await Out.WriteLineAsync(
                    string.Format("{0} items processed, but no changes where made", actions.Count()));
            }

        }

        public async Task ListMigrations()
        {
            uSyncBackOfficeContext.Instance.Init();

            var migrationManager = new MigrationManager("~/usync/migrations");
            var migrations = migrationManager.ListMigrations();

            if (migrations.Any()) 
            {
                await Out.WriteLineAsync(string.Format("Found {0} migrations\n================", migrations.Count()));

                foreach(var migration in migrations)
                {
                    await Out.WriteLineAsync(
                        string.Format("{0,-30} {1} [{2} file{3}]",
                        migration.Name, migration.Time, migration.FileCount, migration.FileCount > 1 ? "s" : ""));
                }
            }
            else
            {
                await Out.WriteLineAsync("there are no migrations, use Migration create to create one");
            }
        }



        public async Task Directions()
        {
            await Out.WriteLineAsync("migration <action> [name]");
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("creates and managed migrations for you umbraco site");
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("migration create name");
            await Out.WriteLineAsync("\t creates a migration, with all changes since last migration");
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("migration import-all");
            await Out.WriteLineAsync("\t imports all changes in migrations");
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("migration list");
            await Out.WriteLineAsync("\t lists migrations currently on the disk");
        }
    }
}
