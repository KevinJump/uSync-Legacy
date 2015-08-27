using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.Migrations.Commands
{
    public class Command
    {
        protected readonly TextReader _reader;
        protected readonly TextWriter _writer;

        public Command(TextReader reader, TextWriter writer)
        {
            _reader = reader;
            _writer = writer;
        }

        public virtual string Name()
        {
            return "unknown";
        }

        virtual public async Task<Response> Run(string[] args)
        {
            return await Task.FromResult(Response.Continue);
        }

    }

    public enum Response
    {
        Shutdown,
        Continue,
        FinishedWithError
    }
}
