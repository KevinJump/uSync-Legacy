using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Models.EntityBase;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Core.Helpers
{
    /// <summary>
    ///  walks up and down the content/Media tree, getting you a path, for your id and t'oherway 
    /// </summary>
    public class uSyncTreeWalker
    {
        private readonly IEntityService _entityService;
        private readonly UmbracoObjectTypes _type;

        public uSyncTreeWalker(UmbracoObjectTypes type)
        {
            _entityService = ApplicationContext.Current.Services.EntityService;
            _type = type;
        }

        public string GetPathFromId(int id, UmbracoObjectTypes type)
        {
            if (_entityService.Exists(id))
            {
                var items = _entityService.GetAll(type, id);
                if (items != null && items.Any())
                {
                    var item = items.FirstOrDefault();
                    if (item != null && !item.Trashed)
                    {
                        return GetPath(item);
                    }
                }
            }

            return string.Empty;
        }

        public string GetPathFromKey(Guid key, UmbracoObjectTypes objectType)
        {
            var items = _entityService.GetAll(objectType, new[] { key });
            if (items != null && items.Any())
            {
                var item = items.FirstOrDefault();
                if (item != null && !item.Trashed)
                {
                    return GetPath(item);
                }
            }
               

            return string.Empty;
        }

        private string GetPath(IUmbracoEntity item)
        {
            var path = item.Name;
            if (item.ParentId != -1)
            {
                var parent = _entityService.GetParent(item.Id);
                if (parent != null)
                {
                    path = GetPath(parent) + @"\" + path;
                }
            }

            return path;
        }


        public int GetIdFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return -1;

            var pathBits = path.Split('\\');
            var pathRoot = pathBits[0];

            var root =
                _entityService.GetRootEntities(_type)
                    .Where(x => x.Name == pathRoot).FirstOrDefault();

            if (root != null)
            {
                if (pathBits.Length == 1)
                {
                    // we are here...
                    return root.Id;
                }
                else
                {
                    return GetFromPath(root.Id, pathBits, 2);
                }
            }

            return -1;
        }

        private int GetFromPath(int parentId, string[] pathBits, int level)
        {
            var item = _entityService.GetChildren(parentId)
                            .Where(x => x.Name == pathBits[level - 1])
                            .FirstOrDefault();

            if (item == null)
                return -1;

            if (pathBits.Length == level)
                return item.Id;

            if (pathBits.Length > level)
                return GetFromPath(item.Id, pathBits, level + 1);

            return -1;
        }

    }
}
