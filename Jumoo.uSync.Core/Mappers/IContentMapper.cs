using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Core.Mappers
{
    public interface IContentMapper
    {
        string GetExportValue(int dataTypeDefinitionId, string value);
        string GetImportValue(int dataTypeDefinitionId, string content);
    }

    public interface IContentMapper2 : IContentMapper
    {
        string[] PropertyEditorAliases { get; }
    }
}
