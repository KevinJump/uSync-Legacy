using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Interfaces;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core;
using Jumoo.uSync.Core.Helpers;
using System.Diagnostics;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Core.Serializers
{
    public abstract class DataTypeSyncBaseSerializer : ISyncContainerSerializerTwoPass<IDataTypeDefinition>
    {
        internal readonly string _itemType;
        internal IDataTypeService _dataTypeService;
        internal IEntityService _entityService;

        public int Priority { get { return uSyncConstants.Serailization.DefaultPriority; } }
        public string SerializerType { get { return uSyncConstants.Serailization.DataType; } }

        internal List<IDataTypeDefinition> _dtdCache;

        public DataTypeSyncBaseSerializer(string itemType)
        {
            _itemType = itemType;
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
            _entityService = ApplicationContext.Current.Services.EntityService;

            // this warms up the cache.
            var sw = Stopwatch.StartNew();
            _dtdCache = _dataTypeService.GetAllDataTypeDefinitions().ToList();
            sw.Stop();
            LogHelper.Debug<Events>("Warming up Datatypes ({0}ms)", () => sw.ElapsedMilliseconds);
        }

        public SyncAttempt<XElement> Serialize(IDataTypeDefinition item)
        {
            return SerializeCore(item);
        }

        public SyncAttempt<IDataTypeDefinition> Deserialize(XElement node, bool forceUpdate, bool onePass)
        {
            var attempt = DeSerialize(node, forceUpdate);

            if (onePass && attempt.Success)
            {
                return DesearlizeSecondPass(attempt.Item, node);
            }

            return attempt;
        }

        public SyncAttempt<IDataTypeDefinition> DeSerialize(XElement node, bool forceUpdate = false)
        {
            if (node.IsArchiveFile())
                return SyncAttempt<IDataTypeDefinition>.Succeed(node.Attribute("Name").ValueOrDefault("old_file"), ChangeType.Removed);


            if (node.Name.LocalName != _itemType && node.Name.LocalName != "EntityFolder")
                throw new ArgumentException("XML not valid for type: " + _itemType);

            if (forceUpdate || IsUpdate(node))
            {
                return DeserializeCore(node);
            }

            return SyncAttempt<IDataTypeDefinition>.Succeed(node.NameFromNode(), null, ChangeType.NoChange);
        }
       

        public SyncAttempt<IDataTypeDefinition> DesearlizeSecondPass(IDataTypeDefinition item, XElement node)
        {
            return DesearlizeSecondPassCore(item, node);
        }

        virtual public bool IsUpdate(XElement node)
        {
            if (node.IsArchiveFile())
                return false;

            return true;
        }

        abstract internal SyncAttempt<XElement> SerializeCore(IDataTypeDefinition item);
        abstract internal SyncAttempt<IDataTypeDefinition> DeserializeCore(XElement node);
        abstract internal SyncAttempt<IDataTypeDefinition> DesearlizeSecondPassCore(IDataTypeDefinition item, XElement node);

        public SyncAttempt<IDataTypeDefinition> DeserializeContainer(XElement node)
        {
            return SyncAttempt<IDataTypeDefinition>.Succeed(node.Name.LocalName, null, ChangeType.NoChange);
        }

        public SyncAttempt<XElement> SerializeContainer(EntityContainer item)
        {
            return uSyncContainerHelper.SerializeContainer(item);
        }
    }
}
