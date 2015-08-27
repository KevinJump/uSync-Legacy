//
//     All of this is a very simplified version of Chauffeur 
//     all credit to Aaron Powell
//     https://github.com/aaronpowell/Chauffeur
// 
//     When 7.3 is settled a bit, and Chauffeur works, we will
//     build uSync command on top of Chauffeur as it does this
//     in a much better and flexible way,
// 
//     this is a scaffold so we can test our ideas around the
//     command line, and see what does and doesn't work
//

namespace uSync
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Jumoo.uSync.Migrations.Commands;

    public class UmbracoHost
    {
        private readonly TextReader _reader;
        private readonly TextWriter _writer;

        public UmbracoHost(TextReader reader, TextWriter writer)
        {
            _reader = reader;
            _writer = writer;

            CommandLoader.Instance.Init();
        }

        public async Task<Response> Run()
        {
            var result = Response.Continue;

            while (result != Response.Shutdown)
            {
                var command = await Prompt();
                result = await Process(command);
            }

            return result;
        }

        private async Task<string> Prompt()
        {
            await _writer.WriteAsync("usync> ");
            return await _reader.ReadLineAsync();
        }

        public async Task<Response> Run(string[] args)
        {
            return await Process(string.Join(" ", args));
        }

        public async Task<Response> Process(string command)
        { 

            if (string.IsNullOrEmpty(command))
                return Response.Continue;

            var args = command.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            var what = args[0].ToLower();
            args = args.Skip(1).ToArray();

            try
            {
                var commandItem = CommandLoader.Instance.GetCommandItem(what);
                if (commandItem != null)
                    return await commandItem.Run(args);
                else
                    return Response.Continue;
            }
            catch( Exception ex)
            {
                _writer.WriteLine("Error:");
                _writer.WriteLine(ex.ToString());
                return Response.FinishedWithError;
            }
        }

    }


}
