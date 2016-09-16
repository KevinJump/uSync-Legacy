using Chauffeur.Host;
using Jumoo.uSync.BackOffice;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.Chauffeur
{
    public class uSyncCommand
    {
        public uSyncCommand()
        {

            group = "default";
            force = false;
            changes = true;
            errors = true;
        }

        public string folder;
        public string name; 

        public string group;
        public bool force;

        public bool changes;
        public bool errors;
        public bool verbose;
    }


    public class uSyncCommandHelper
    {
        private readonly IFileSystem _fileSystem;
        private readonly IChauffeurSettings _settings;
        private readonly uSyncBackOfficeContext _uSyncContext;
        private readonly TextWriter Out;

        public uSyncCommandHelper(
            TextWriter writer,
            IFileSystem fileSystem, IChauffeurSettings settings, uSyncBackOfficeContext uSync)
        {
            Out = writer; 
            _fileSystem = fileSystem;
            _settings = settings;
            _uSyncContext = uSync;
        }

        public async Task<uSyncCommand> ParseArgs(string[] args)
        {
            uSyncCommand command = new uSyncCommand();

            string siteDir;
            if (!_settings.TryGetSiteRootDirectory(out siteDir))
                return null;

            var uSyncSettingFolder = _uSyncContext.Configuration.Settings.Folder
                .Replace("~/", "").Replace('/', '\\');

            command.folder = _fileSystem.Path.Combine(siteDir, uSyncSettingFolder);

            if (args.Any())
            {
                int posistion = 0;
                foreach (var arg in args)
                {
                    if (arg.Trim().StartsWith("-"))
                    {
                        // is a command.. 
                        if (arg.IndexOf('=') > 0)
                        {
                            var cmd = arg.Substring(1, arg.IndexOf('=') - 1);
                            var val = arg.Substring(arg.IndexOf('=') + 1);

                            // await Out.WriteLineAsync(string.Format("Command: [{0}] Value: [{1}]", cmd, val));

                            switch (cmd.ToLower())
                            {
                                case "folder":
                                    command.folder=val;
                                    break;
                                case "group":
                                    command.group = val;
                                    break;
                                case "force":
                                    command.force = GetBool(val, false);
                                    break;
                                case "changes":
                                    command.changes = true;
                                    break;
                                case "errors":
                                    command.errors = true;
                                    break;
                                case "silent":
                                    command.changes = false;
                                    command.errors = false;
                                    break;
                                case "verbose":
                                    command.verbose = true;
                                    break;
                                case "name":
                                    command.name = val;
                                    break;
                                default:
                                    await Out.WriteLineAsync("Unknown uSync Command :(");
                                    break;
                            }
                        }
                    }
                    else
                    {
                        posistion++;
                        if (posistion == 1)
                            command.folder = _fileSystem.Path.Combine(siteDir, arg.Trim(new char[] { '\\', ' ' }));

                        if (posistion == 2)
                            command.name = arg.Trim();
                    }
                }
            }

            return command;
        }

        private bool GetBool(string val, bool defaultValue)
        {
            var boolValue = defaultValue;
            bool.TryParse(val, out boolValue);
            return boolValue;
        }

    }
}
