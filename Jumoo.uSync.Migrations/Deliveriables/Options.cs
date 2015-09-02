using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using CommandLine;

namespace Jumoo.uSync.Migrations.Deliveriables
{
    public class Options
    {
        [VerbOption("list")]
        public ListOptions ListVerb { get; set; }

        [VerbOption("import", HelpText = "Import files/folders")]
        public ImportOptions ImportVerb { get; set; }

        [VerbOption("export")]
        public ExportOptions ExportVerb { get; set; }
    }

    public class ListOptions
    {
        public UmbracoType Type { get; set; }
    }

    public class ImportOptions
    {
        [Option('f', "file", Required = true)]
        public string FileName { get; set; }

        [Option("force", DefaultValue = false)]
        public bool Force { get; set; }

        [Option('d')]
        public bool Folder { get; set; }

    }

    public class ExportOptions
    {
        [Option('t', "type")]
        public UmbracoType Type { get; set; }

        [Option('n', "name")]
        public string itemKey { get; set; }

        [Option('f', "file")]
        public string fileName { get; set; }
    }

    public enum UmbracoType
    {
        DataType,
        ContentType,
        MediaType,
        Language,
        DictionaryItem,
        Template,
        Macro
    }
}