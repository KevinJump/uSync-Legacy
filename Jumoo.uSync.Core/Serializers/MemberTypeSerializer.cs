using Jumoo.uSync.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Jumoo.uSync.Core.Helpers;

namespace Jumoo.uSync.Core.Serializers
{
    public class MemberTypeSerializer : ContentTypeBaseSerializer<IMemberType>, ISyncChangeDetail
    {
        public override string SerializerType { get { return uSyncConstants.Serailization.MemberType; } }

        public MemberTypeSerializer() :
            base("MemberType", UmbracoObjectTypes.MemberType) { }

        public MemberTypeSerializer(string type) 
            : base(type, UmbracoObjectTypes.MemberType) { }

        internal override SyncAttempt<IMemberType> DeserializeCore(XElement node)
        {
            if (node == null || node.Element("Info") == null || node.Element("Info").Element("Alias") == null)
                throw new ArgumentException("Invalid xml");

            var info = node.Element("Info");

            IMemberType item = null;

            var key = info.Element("Key").ValueOrDefault(Guid.Empty);
            if (key != Guid.Empty)
            {
                item = _memberTypeService.Get(key);
            }

            var name = info.Element("Name").ValueOrDefault(string.Empty);
            var alias = info.Element("Alias").ValueOrDefault(string.Empty);

            var parentId = -1;
            var parentAlias = info.Element("Master").ValueOrDefault(string.Empty);
            if (parentAlias != null)
            {
                var parent = _memberTypeService.Get(parentAlias);
                if (parent != null)
                    parentId = parent.Id;
            }

            if (item == null)
            {
                // we need to to an alias lookup of this one, because after an
                // upgrade it can have a blank guid turned into a real one...
                // 
                LogHelper.Debug<MemberTypeSerializer>("Finding Membertype by alias: {0}", ()=> alias);
                item = _memberTypeService.Get(alias);
            }


            if (item == null)
            {
                LogHelper.Debug<MemberTypeSerializer>("Creating new membertype {0}", ()=> alias);
                item = new MemberType(parentId)
                {
                    Alias= alias,
                    Name = name
                };
            }

            if (item.Key != key)
                item.Key = key;

            DeserializeBase(item, info);

            DeserializeTabSortOrder(item, node);

            var msg = DeserializeProperties(item, node);

            CleanUpTabs(item, node);

            _memberTypeService.Save(item);

            return SyncAttempt<IMemberType>.Succeed(item.Name, item, ChangeType.Import, msg);
        }

        internal override SyncAttempt<XElement> SerializeCore(IMemberType item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            // most of the things are ContentTypeBase
            var info = SerializeInfo(item);
            var tabs = SerializeTabs(item);
            var properties = SerializeProperties(item);
            var structure = SerializeStructure(item);

            var node = new XElement("MemberType",
                                info,
                                structure,
                                properties,
                                tabs);

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IMemberType), ChangeType.Export);
        }

        public override bool IsUpdate(XElement node)
        {
            if (node.IsArchiveFile())
                return false;

            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var aliasNode = node.Element("Info").Element("Alias");
            if (aliasNode == null)
                return true;

            var item = _memberTypeService.Get(aliasNode.Value);
            if (item == null)
                return true;

            var attempt = Serialize(item);
            if (!attempt.Success)
                return true;

            var itemHash = attempt.Item.GetSyncHash();
               
            return (!nodeHash.Equals(itemHash));

        }

        #region ISyncChangeDetail : Support for detailed change reports
        public IEnumerable<uSyncChange> GetChanges(XElement node)
        {
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return null;

            var aliasNode = node.Element("Info").Element("Alias");
            if (aliasNode == null)
                return null;

            var item = _memberTypeService.Get(aliasNode.Value);
            if (item == null)
            {
                return uSyncChangeTracker.NewItem(aliasNode.Value);
            }

            var attempt = Serialize(item);
            if (attempt.Success)
            {
                return uSyncChangeTracker.GetChanges(node, attempt.Item, "");
            }
            else
            {
                return uSyncChangeTracker.ChangeError(aliasNode.Value);
            }
        }
        #endregion
    }
}    