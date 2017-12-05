using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.ContentMappers
{
    public class StackedContentMapper : InnerContentMapper
    {
        public override string[] PropertyEditorAliases => new[] { "Our.Umbraco.StackedContent" };        
    }
}
