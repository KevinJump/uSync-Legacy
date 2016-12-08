using Jumoo.uSync.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Jumoo.uSync.BackOffice.Handlers.Deploy
{
    public class MacroDeployHandler : BaseDepoyHandler<IMacroService, IMacro>, ISyncHandler, IPickySyncHandler
    {
        IMacroService _macroService; 

        public MacroDeployHandler()
        {
            _macroService = ApplicationContext.Current.Services.MacroService;
            _baseSerializer = uSyncCoreContext.Instance.MacroSerializer;
            SyncFolder = Constants.Packaging.MacroNodeName;
        }

        public string Name
        {
            get
            {
                return "Deploy:MacroHandler";
            }
        }

        public int Priority
        {
            get
            {
                return uSyncConstants.Priority.Macros + 500;
            }
        }

        public override IEnumerable<IMacro> GetAllExportItems()
        {
            return _macroService.GetAll();
        }

        public override string GetFileName(IMacro item)
        {
            return item.Alias;
        }

        public override ChangeType DeleteItem(uSyncDeployNode node, bool force)
        {
            /*
            var item = _macroService.GetById(node.Key);
            if (item != null)
            {
                _macroService.Delete(item);
                return ChangeType.Delete;
            }
            */
            return ChangeType.NoChange;
        }

        public void RegisterEvents()
        {
            MacroService.Saved += base.Service_Saved;
            MacroService.Deleted += base.Service_Deleted;
        }
    }
}
