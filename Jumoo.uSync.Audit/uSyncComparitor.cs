using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Helpers;
using Jumoo.uSync.Core.Interfaces;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Models.EntityBase;
using Umbraco.Core.Models.Membership;
using Umbraco.Web;

namespace Jumoo.uSync.Audit
{
    public class uSyncChangesEventArgs : EventArgs
    {
        public uSyncChangeGroup Changes { get; set; }
    }

    public class uSyncAudit
    {
        public static event TypedEventHandler<uSyncAudit, uSyncChangesEventArgs> Changed;

        public static void FireChangedEvent(uSyncChangesEventArgs changes)
        {
            if (Changed != null)
            {
                Changed.Invoke(new uSyncAudit(), changes);
            }
        }

    }

    public class uSyncComparitor<TItem> where TItem : IEntity
    {

        private readonly ISyncSerializer<TItem> _serializer;
        private readonly string _rootFolder;
        private readonly string _folder;
        private readonly IUser _user;

        private readonly string _typeName;

        public uSyncComparitor(ISyncSerializer<TItem> serializer)
        {
            _serializer = serializer;
            _typeName = typeof(TItem).FullName;
            _typeName = _typeName.Substring(_typeName.LastIndexOf('.') + 1);

            _rootFolder = Umbraco.Core.IO.IOHelper.MapPath("~/uSync/Audit/");
            _folder = Path.Combine(_rootFolder, _typeName);

            _user = UmbracoContext.Current.Security.CurrentUser;
        }

        public void ProcessChanges(IEnumerable<TItem> items)
        {
            var changes = GetChanges(items);
            var files = SaveUpdates(items);

            foreach(var file in files)
            {
                if (!string.IsNullOrEmpty(file.Value))
                {
                    var change = changes.ItemChanges.FirstOrDefault(x => x.Key == file.Key);
                    if (change != null)
                    {
                        var relativePath = file.Value.Substring(_rootFolder.Length);
                        change.Source = relativePath;
                    }
                }
            }

            uSyncAudit.FireChangedEvent(new uSyncChangesEventArgs
            {
                Changes = changes
            });
        }


        public void ProcessDelete(IEnumerable<TItem> items)
        {
            uSyncChangeGroup changes = new uSyncChangeGroup(_user.Id, _user.Name);

            foreach(var item in items)
            {
                var deleteChange = new uSyncItemChanges();

                deleteChange.Name = GetNiceName(item);
                deleteChange.Changes.Add(new uSyncChange
                {
                    Change = ChangeDetailType.Delete,
                    Name = GetNiceName(item),
                });

                changes.ItemChanges.Add(deleteChange);
            }
        }

        public uSyncChangeGroup GetChanges(IEnumerable<TItem> items)
        { 
            uSyncChangeGroup changes = new uSyncChangeGroup(_user.Id, _user.Name);
            changes.ItemType = _typeName;

            foreach(var item in items)
            {
                changes.ItemChanges.Add(this.GetUpdates(item));
            }

            return changes;
        }

        public IDictionary<Guid, string> SaveUpdates(IEnumerable<TItem> items)
        {
            Dictionary<Guid, string> files = new Dictionary<Guid, string>();

            foreach(var item in items)
            {
                files.Add(item.Key, SaveUpdate(item));
            }

            return files;
        }

        public uSyncItemChanges GetUpdates(TItem item)
        {
            var itemChanges = new uSyncItemChanges();
            itemChanges.Key = item.Key;
            itemChanges.Name = GetNiceName(item);

            var path = Path.Combine(_folder, item.Key + ".config");
            if (System.IO.File.Exists(path))
            {
                XElement existing = XElement.Load(path);
                if (existing != null)
                {
                    if (_serializer.IsUpdate(existing))
                    {
                        if (_serializer is ISyncChangeDetail)
                        {
                            var changes = ((ISyncChangeDetail)_serializer).GetChanges(existing);

                            // we do things backwards so we just flip them 
                            foreach (var change in changes)
                            {
                                var tmp = change.NewVal;
                                change.NewVal = change.OldVal;
                                change.OldVal = tmp;

                                if (change.Change == ChangeDetailType.Delete)
                                {
                                    change.Change = ChangeDetailType.Create;
                                }
                                else if (change.Change == ChangeDetailType.Create)
                                {
                                    change.Change = ChangeDetailType.Delete;
                                }
                            }

                            itemChanges.Changes.AddRange(changes);
                        }
                    }
                }
            }
            else
            {
                var newItem = new uSyncChange()
                {
                    Change = ChangeDetailType.Create,
                    Name = GetNiceName(item),
                    NewVal = "(Creation)",
                    OldVal = ""
                };

                itemChanges.Changes.Add(newItem);
            }

            return itemChanges;
        }

        private string GetNiceName(TItem item)
        {
            if (item is IUmbracoEntity)
                return ((IUmbracoEntity)item).Name;

            if (item is ITemplate)
                return ((ITemplate)item).Name;

            if (item is IDictionaryItem)
                return ((IDictionaryItem)item).ItemKey;

            if (item is ILanguage)
                return ((ILanguage)item).CultureName;

            if (item is IMacro)
                return ((IMacro)item).Name;

            return item.Id.ToString();
        }

        public string SaveUpdate(TItem item)
        {
            var attempt = _serializer.Serialize(item);
            if (attempt.Success)
            {
                var path = Path.Combine(_folder, item.Key + ".config");
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);

                Directory.CreateDirectory(Path.GetDirectoryName(path));

                var uniquePath = Path.Combine(_folder, item.Key + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".config");

                attempt.Item.Save(path);
                attempt.Item.Save(uniquePath);

                return uniquePath;
            }

            return string.Empty;
        }
    }
}
