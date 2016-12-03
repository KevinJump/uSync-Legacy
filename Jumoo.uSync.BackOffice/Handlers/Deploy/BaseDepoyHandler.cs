using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Helpers;
using Jumoo.uSync.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Events;
using Umbraco.Core.Models.EntityBase;

namespace Jumoo.uSync.BackOffice.Handlers.Deploy
{
    abstract public class BaseDepoyHandler<TService, TItem> where TItem : IEntity
    {
        internal ISyncSerializer<TItem> _baseSerializer;
        internal bool RequiresPostProcessing = false;
        internal bool TwoPassImport = false; 
        public string SyncFolder { get; set; }

        #region Importing 
        public IEnumerable<uSyncAction> ImportAll(string folder, bool force)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            var items = GetImportItems(folder);
            var tree = MakeTree(items, Guid.Empty);

            Dictionary<XElement, TItem> updates = new Dictionary<XElement, TItem>();
            foreach(var branch in tree)
            {
                actions.AddRange(ImportTree(branch, force, updates));
            }

            foreach(var update in updates)
            {
                ImportSecondPass(update.Value, update.Key);
            }

            return actions; 
        }

        /// <summary>
        /// load the import items from disk
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        internal IEnumerable<uSyncDeployNode> GetImportItems(string folder)
        {
            List<uSyncDeployNode> items = new List<uSyncDeployNode>();

            var mappedFolder = Umbraco.Core.IO.IOHelper.MapPath(folder);

            if (Directory.Exists(mappedFolder))
            {
                foreach(var item in Directory.GetFiles(mappedFolder, "*.config"))
                {
                    XElement node = XElement.Load(item);
                    if (node != null && node.Name.LocalName != "uSyncArchive")
                    {
                        items.Add(new uSyncDeployNode()
                        {
                            Key = GetKey(node),
                            Master = GetMaster(node),
                            Node = node 
                        });
                    }
                }
            }
            return items;
        }

        internal IEnumerable<uSyncDeployTreeNode> MakeTree(IEnumerable<uSyncDeployNode> items, Guid masterKey)
        {
            List<uSyncDeployTreeNode> branch = new List<uSyncDeployTreeNode>();

            var nodes = items.Where(x => x.Master == masterKey);
            foreach (var node in nodes)
            {
                var leaf = new uSyncDeployTreeNode()
                {
                    Node = node
                };

                leaf.Children.AddRange(MakeTree(items, node.Key));
            }

            return branch;
        }

        private IEnumerable<uSyncAction> ImportTree(uSyncDeployTreeNode tree, bool force, IDictionary<XElement, TItem> updates)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            var result = Import(tree.Node, force);
            if (result.Success && result.Item != null && TwoPassImport)
            {
                updates.Add(tree.Node.Node, result.Item);
            }
            actions.Add(
                uSyncActionHelper<TItem>.SetAction(result, tree.Node.Node.NameFromNode(), RequiresPostProcessing));

            foreach(var branch in tree.Children)
            {
                actions.AddRange(ImportTree(branch, force, updates));
            }

            return actions; 
        }

        virtual public SyncAttempt<TItem> Import(uSyncDeployNode node, bool force)
        {
            return Import(node.Node, force);
        }

        private SyncAttempt<TItem> Import(XElement node, bool force)
        {
            return _baseSerializer.DeSerialize(node, force);
        }

        public IEnumerable<uSyncAction> ProcessPostImport(string filepath, IEnumerable<uSyncAction> actions)
        {
            List<uSyncAction> postActions = new List<uSyncAction>();

            if (actions.Any())
            {
                var items = actions.Where(x => x.ItemType == typeof(TItem));
                foreach(var item in items)
                {
                    XElement node = XElement.Load(item.FileName);
                    if (node != null)
                    {
                        var attempt = Import(node, false);
                        if (attempt.Success&& TwoPassImport) 
                        {
                            ImportSecondPass(attempt.Item, node);
                        }
                    }
                }
            }

            return postActions;            
        }

        virtual public void ImportSecondPass(TItem item, XElement node)
        {
            if (_baseSerializer is ISyncSerializerTwoPass<TItem>)
            {
                ((ISyncSerializerTwoPass<TItem>)_baseSerializer).DesearlizeSecondPass(item, node);
            }
        }
        #endregion

        #region Reporting
        public IEnumerable<uSyncAction> Report(string folder)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            var items = GetImportItems(folder);
            foreach(var item in items)
            {
                actions.Add(Report(item));
            }

            return actions; 
        }

        public uSyncAction Report(uSyncDeployNode item)
        {
            var update = _baseSerializer.IsUpdate(item.Node);
            var action = uSyncActionHelper<TItem>.ReportAction(update, item.Node.NameFromNode());
            if (action.Change > ChangeType.NoChange)
                action.Details = ((ISyncChangeDetail)_baseSerializer).GetChanges(item.Node);

            return action;
        }
        #endregion

        #region Export
        abstract public IEnumerable<TItem> GetAllExportItems();

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            var items = GetAllExportItems();
            foreach(var item in items)
            {
                actions.Add(ExportToDisk(item, folder));
            }

            return actions; 
        }

        public virtual uSyncAction ExportToDisk(TItem item, string folder)
        {
            if (item == null)
                return uSyncAction.Fail(Path.GetFileName(folder), typeof(TItem), "Item not set");

            var filename = item.Key.ToString() + ".config";

            var attempt = _baseSerializer.Serialize(item);
            if (attempt.Success)
                DeployIOHelper.SaveNode(attempt.Item, folder, filename);

            return uSyncActionHelper<XElement>.SetAction(attempt, filename);
        }
        #endregion

        #region Event Handling

        internal void Service_Saved(TService sender, SaveEventArgs<TItem> e)
        { 
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.SavedEntities)
            {
                var action = ExportToDisk(item, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
            }
        }

        internal void Service_Deleted(TService sender, DeleteEventArgs<TItem> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach(var item in e.DeletedEntities)
            {
                DeployIOHelper.DeleteNode(item.Key, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
            }
        }

        #endregion

        virtual internal Guid GetKey(XElement node)
        {
            return node.KeyOrDefault();
        }

        virtual internal Guid GetMaster(XElement node)
        {
            if (node.Element("Info") != null
                && node.Element("Info").Element("Master") != null
                && node.Element("Info").Element("Master").Attribute("Key") != null)
                return node.Element("Info").Element("Master").Attribute("Key").ValueOrDefault(Guid.Empty);

            return Guid.Empty;
        }
    }
}
