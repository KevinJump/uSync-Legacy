using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using CommandLine;

namespace Jumoo.uSync.Migrations.Deliveriables
{
    public class Options
    {
        [VerbOption("import", HelpText = "Import files/folders"]
        public ImportOptions ImportVerb { get; set; }

        [VerbOption("export")]
        public ExportOptions ExportVerb { get; set; }
    }

    public class ImportOptions
    {
        [Option(Required = true)]
        public string FileName { get; set; }

        [Option('f', DefaultValue = false)]
        public bool Force { get; set; }

        [Option('d')]
        public bool Folder { get; set; }

    }

    public class ExportOptions
    {
        public string itemType { get; set; }
        public string itemKey { get; set; }
        public string fileName { get; set; }
    }
}