using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Chauffeur;
using Chauffeur.Host;
using System.IO;
using System.IO.Abstractions;
using Jumoo.uSync.BackOffice;
using System.Diagnostics;

namespace Jumoo.uSync.Chauffeur
{
    [DeliverableName("usync")]
    [DeliverableAlias("us")]
    public class uSyncDeliverable : Deliverable, IProvideDirections
    {
        private readonly IFileSystem _fileSystem;
        private readonly IChauffeurSettings _settings;
        private readonly uSyncBackOfficeContext _uSyncContext;
        private readonly uSyncCommandHelper _commandHelper;
        private readonly uSyncReporter _reporter;

        public uSyncDeliverable(
            TextReader reader,
            TextWriter writer,
            IFileSystem fileSystem,
            IChauffeurSettings settings)
            : base(reader, writer)
        {
            _fileSystem = fileSystem;
            _settings = settings;

            uSync.BackOffice.uSyncBackOfficeContext.Instance.Init();
            _uSyncContext = uSync.BackOffice.uSyncBackOfficeContext.Instance;

            _commandHelper = new uSyncCommandHelper(writer, fileSystem, settings, _uSyncContext);
            _reporter = new uSyncReporter(writer);
        }

        public override async Task<DeliverableResponse> Run(string command, string[] args)
        {
            if (!args.Any())
            {
                await Out.WriteLineAsync("No help");
                return DeliverableResponse.Continue;
            }


            await Out.WriteLineAsync("uSync for Chauffeur\n");
            //
            // Command Line. 
            //
            // Chauffeur usync [operation] [params] 
            var operation = args[0];

            switch(operation.ToLower())
            {
                case "import":
                    await Import(args.Skip(1).ToArray());
                    break;
                case "export":
                    await Export(args.Skip(1).ToArray());
                    break;
                case "report":
                    await Report(args.Skip(1).ToArray());
                    break;
                case "content-type":
                    await HandlerAction("uSync: ContentTypeHandler", args.Skip(1).ToArray());
                    break;
                case "data-type":
                    await HandlerAction("uSync: DataTypeHandler", args.Skip(1).ToArray());
                    break;
                case "media-type":
                    await HandlerAction("uSync: MediaTypeHandler", args.Skip(1).ToArray());
                    break;
                case "template":
                    await HandlerAction("uSync: TemplateHandler", args.Skip(1).ToArray());
                    break;
                case "language":
                    await HandlerAction("uSync: LanguageHandler", args.Skip(1).ToArray());
                    break;
                case "dictionary":
                    await HandlerAction("uSync: DictionaryHandler", args.Skip(1).ToArray());
                    break;
                case "macro":
                    await HandlerAction("uSync: MacroHandler", args.Skip(1).ToArray());
                    break;
                case "member-type":
                    await HandlerAction("uSync: MemberTypeHandler", args.Skip(1).ToArray());
                    break;
                case "content":
                    await HandlerAction("uSync: ContentHandler", args.Skip(1).ToArray());
                    break;
                case "content-template":
                    await HandlerAction("uSync: ContentTemplateHandler", args.Skip(1).ToArray());
                    break;
                case "media":
                    await HandlerAction("uSync: MediaHandler", args.Skip(1).ToArray());
                    break;
                case "domain":
                    await HandlerAction("uSync: Domain", args.Skip(1).ToArray());
                    break;
                default:
                    await Out.WriteLineAsync(string.Format("The command: {0} is not supported", command));
                    break;                  
            }

            return DeliverableResponse.Continue;
        }

        private async Task Import(string[] args)
        {
            Stopwatch sw = Stopwatch.StartNew();
            await Out.WriteLineAsync(" uSync Import ");
            await Out.WriteLineAsync("==============");

            
            var cmd = await _commandHelper.ParseArgs(args);
            await Out.WriteLineAsync(string.Format("Group  : {0}", cmd.group));
            await Out.WriteLineAsync(string.Format("Folder : {0}", cmd.folder));
            await Out.WriteLineAsync(string.Format("Force  : {0}", cmd.force));

            var export = _uSyncContext.Import(cmd.group, cmd.folder, cmd.force);

            await _reporter.Report(export, cmd);

            sw.Stop();
            await Out.WriteLineAsync(
                string.Format("Import {0} items Complete {1:n0}ms", export.Count(), sw.ElapsedMilliseconds));

            // _uSyncContext.Import("default", uSyncFolder, false);
        }

        private async Task Export(string[] args)
        {
            Stopwatch sw = Stopwatch.StartNew();
            await Out.WriteLineAsync(" uSync Export ");
            await Out.WriteLineAsync("==============");

            var cmd = await _commandHelper.ParseArgs(args);
            await Out.WriteLineAsync(string.Format("Group  : {0}", cmd.group));
            await Out.WriteLineAsync(string.Format("Folder : {0}", cmd.folder));

            var export = _uSyncContext.Export(cmd.group, cmd.folder);

            await _reporter.Report(export, cmd);

            sw.Stop();
            await Out.WriteLineAsync(
                string.Format("Export {0} items Complete {1:n0}ms", export.Count(), sw.ElapsedMilliseconds));
        }

        private async Task Report(string[] args)
        {
            Stopwatch sw = Stopwatch.StartNew();
            await Out.WriteLineAsync(" uSync Report ");
            await Out.WriteLineAsync("==============");

            var cmd = await _commandHelper.ParseArgs(args);
            await Out.WriteLineAsync(string.Format("Group  : {0}", cmd.group));
            await Out.WriteLineAsync(string.Format("Folder : {0}", cmd.folder));

            var export = _uSyncContext.Report(cmd.group, cmd.folder);

            await _reporter.Report(export, cmd);

            sw.Stop();
            await Out.WriteLineAsync(
                string.Format("Reported on {0} items Complete {1:n0}ms", export.Count(), sw.ElapsedMilliseconds));
        }

        private async Task HandlerAction(string handlerName, string[] args)
        {
            var handler = _uSyncContext.Handlers.Where(x => x.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (handler == null)
            {
                await Out.WriteLineAsync(string.Format("Cannot Find Handler: {0}", handlerName));
                return;
            }

            await Out.WriteLineAsync(string.Format("Loaded Handler {0}", handlerName));

            if (!args.Any())
            {
                await Out.WriteLineAsync("You haven't told me what to do, try import, export or report");
                return;
            }

            

            var action = args[0];
            var actionArgs = await _commandHelper.ParseArgs(args.Skip(1).ToArray());

            List<uSyncAction> results = new List<uSyncAction>();

            switch (action.ToLower())
            {
                case "import":
                    await Out.WriteLineAsync("Importing.... ");
                    actionArgs.folder = Path.Combine(actionArgs.folder, handler.SyncFolder);
                    results.AddRange(handler.ImportAll(actionArgs.folder, actionArgs.force));

                    var secondpass = results.Where(x => x.RequiresPostProcessing);
                    if (secondpass.Any() && handler is ISyncPostImportHandler)
                    {
                        await Out.WriteLineAsync("Processing Second Pass");
                        ((ISyncPostImportHandler)handler).ProcessPostImport(actionArgs.folder, secondpass);
                    }
                    break;
                case "export":
                    await Out.WriteLineAsync("Exporting.... ");
                    results.AddRange(handler.ExportAll(actionArgs.folder));
                    break;
                case "report":
                    await Out.WriteLineAsync("Running Change Report.... ");
                    actionArgs.folder = Path.Combine(actionArgs.folder, handler.SyncFolder);
                    results.AddRange(handler.Report(actionArgs.folder));
                    break;
                default:
                    await Out.WriteLineAsync(string.Format("Sorry i don't know how to {0}", action));
                    break;
            }

            await _reporter.Report(results, actionArgs);

            return;
        }

        public async Task Directions()
        {
            await Out.WriteLineAsync("usync");

            await Out.WriteLineAsync("\tuSync all the things from the command line");
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("\tusync <action|operation> <folder> [options]");
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("General actions:");
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("\t import <folder> [options]");
            await Out.WriteLineAsync("\t export <folder> [options]");
            await Out.WriteLineAsync("\t report <folder> [options]");
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("Specific Operations");
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("\t content-type       <action> [options]");
            await Out.WriteLineAsync("\t data-type          <action> [options]");
            await Out.WriteLineAsync("\t media-type         <action> [options]");
            await Out.WriteLineAsync("\t member-type        <action> [options]");
            await Out.WriteLineAsync("\t template           <action> [options]");
            await Out.WriteLineAsync("\t language           <action> [options]");
            await Out.WriteLineAsync("\t dictionary         <action> [options]");
            await Out.WriteLineAsync("\t macro              <action> [options]");
            await Out.WriteLineAsync("\t content            <action> [options]      (umbraco content)");
            await Out.WriteLineAsync("\t media              <action> [options]      (media settings and files)");
            await Out.WriteLineAsync("\t content-template   <action> [options]      (content blueprints)");
            await Out.WriteLineAsync("\t domain             <actiom> [options]      (domain / host settings)" );
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("Options:");
            await Out.WriteLineAsync("\tall commands use the same basic options");
            await Out.WriteLineAsync("");
            await Out.WriteLineAsync("\tfolder              path to the folder where usync lives (default from config)");
            await Out.WriteLineAsync("\tforce=<true|false>  do a force import (so import when no changes)");
            await Out.WriteLineAsync("\tgroup=<group>       name of the handler group to use (default = default)");
            await Out.WriteLineAsync("");


        }

    }

}
