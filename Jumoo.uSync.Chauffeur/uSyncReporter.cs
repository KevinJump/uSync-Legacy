using Jumoo.uSync.BackOffice;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.Chauffeur
{
    public class uSyncReporter
    {
        private readonly TextWriter Out;
        public uSyncReporter(TextWriter writer)
        {
            Out = writer;
        }

        public async Task Report(IEnumerable<uSyncAction> actions, uSyncCommand command)
        {
            await Out.WriteLineAsync(string.Format("Reporting : {0} items processed", actions.Count()));

            if (command.errors)
            {
                var errors = actions.Where(x => !x.Success).ToList();

                if (errors.Any())
                {
                    foreach (var error in errors)
                    {
                        await Out.WriteLineAsync(string.Format("error: {0} {1} {2}", error.Name, error.Change, error.Message));
                    }
                }
                else
                {
                    await Out.WriteLineAsync("No Errors");
                }
            }


            if (command.changes)
            {
                var changes = actions.Where(x => x.Change > Core.ChangeType.NoChange).ToList();

                if (changes.Any())
                {
                    foreach (var change in changes)
                    {
                        await Out.WriteLineAsync(string.Format("Change: {0} {1} {2} {3}",
                            change.ItemType, change.Name, change.Change, change.Message));
                    }
                }
                else
                {
                    await Out.WriteLineAsync("No Changes");
                }
            }
        }

    }
}
