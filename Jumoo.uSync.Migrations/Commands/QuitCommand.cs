using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.Migrations.Commands
{
    public class QuitCommand : Command
    {
        public QuitCommand(TextReader reader, TextWriter writer)
            : base(reader, writer)
        { }

        public override string Name()
        {
            return "quit";
        }

        public override async Task<Response> Run(string[] args)
        {
            await _writer.WriteLineAsync("Bye");
            return await Task.FromResult(Response.Shutdown); 
        }
    }
}
