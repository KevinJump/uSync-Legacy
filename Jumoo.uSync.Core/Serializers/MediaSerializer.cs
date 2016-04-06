using System;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core.Models;

using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Helpers;

using Jumoo.uSync.Core.Interfaces;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Core.Serializers
{
    public class MediaSerializer : ContentBaseSerializer<IMedia>
    {
        public MediaSerializer() : base(string.Empty)
        { }

        internal override SyncAttempt<IMedia> DeserializeCore(XElement node, int parentId, bool forceUpdate)
        {
            var nodeGuid = node.Attribute("guid");
            if (nodeGuid == null)
                return SyncAttempt<IMedia>.Fail(node.NameFromNode(), ChangeType.Import, "No guid, in xml");

            Guid guid = new Guid(nodeGuid.Value);

            var name = node.Attribute("nodeName").Value;
            string mediaTypeAlias = node.Attribute("nodeTypeAlias").Value;

            var update = node.Attribute("updated").ValueOrDefault(DateTime.Now);

            var item = _mediaService.GetById(guid);
            if (item == null)
            {
                item = _mediaService.CreateMedia(name, parentId, mediaTypeAlias);
            }
            else
            {
                if (item.Trashed)
                {
                    item.ChangeTrashedState(false);
                }

                if (!forceUpdate)
                {
                    if (DateTime.Compare(update, item.UpdateDate.ToLocalTime()) < 0)
                        return SyncAttempt<IMedia>.Succeed(item.Name, item, ChangeType.NoChange);
                }
            }

            if (item != null)
            {
                if (item.Key != guid)
                    item.Key = guid;

                if (item.Name != name)
                    item.Name = name;

                if (item.ParentId != parentId)
                    item.ParentId = parentId;

                /*
                 * properties are set in second pass, for speed we don't do it here.
                 */
                /*
                var properties = node.Elements().Where(x => x.Attribute("isDoc") == null);
                foreach (var property in properties)
                {
                    var propertyTypeAlias = property.Name.LocalName;
                    if (item.HasProperty(propertyTypeAlias))
                    {
                        var propType = item.Properties[propertyTypeAlias].PropertyType;
                        var newValue = GetImportIds(propType, GetImportXml(property));

                        LogHelper.Debug<Events>("#### Setting property: [{0}] to {1}", () => propertyTypeAlias, () => newValue);
                        item.SetValue(propertyTypeAlias, newValue);
                    }
                }
                */

                _mediaService.Save(item);
            }

            return SyncAttempt<IMedia>.Succeed(item.Name, item,ChangeType.Import);

        }

        internal override SyncAttempt<XElement> SerializeCore(IMedia item)
        {
            var mediaTypeAlias = item.ContentType.Alias;

            var attempt = base.SerializeBase(item, mediaTypeAlias);
            if (!attempt.Success)
                return attempt;

            var node = attempt.Item;

            node.Add(new XAttribute("parentGUID", item.Level > 1 ? item.Parent().Key : new Guid("00000000-0000-0000-0000-000000000000")));
            node.Add(new XAttribute("nodeTypeAlias", item.ContentType.Alias));
            node.Add(new XAttribute("path", item.Path));

            // if we are not moving the media then we just write the umbracoFile settings as we see them
            //
            if (!uSyncCoreContext.Instance.Configuration.Settings.MoveMedia)
            {
                // this is done in SerializeBase ? 
            }

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IMedia), ChangeType.Export);
        }

        public override bool IsUpdate(XElement node)
        {
            var key = node.Attribute("guid").ValueOrDefault(Guid.Empty);
            if (key == Guid.Empty)
                return true;

            var item = _mediaService.GetById(key);
            if (item == null)
                return true;

            DateTime updateTime = node.Attribute("updated").ValueOrDefault(DateTime.Now);
            if (DateTime.Compare(updateTime, item.UpdateDate.ToLocalTime()) <= 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public override SyncAttempt<IMedia> DesearlizeSecondPass(IMedia item, XElement node)
        {
            base.DeserializeMappedIds(item, node);
            PublishOrSave(item, true, true);

            return SyncAttempt<IMedia>.Succeed(item.Name, ChangeType.Import);
        }

        public override void PublishOrSave(IMedia item, bool published, bool raiseEvents)
        {
            _mediaService.Save(item, raiseEvents: raiseEvents);
        }
    }
}
