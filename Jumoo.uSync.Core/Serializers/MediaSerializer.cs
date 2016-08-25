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

        public override string SerializerType { get { return uSyncConstants.Serailization.Media; } }

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
            else if (item.Trashed)
            {
                item.ChangeTrashedState(false);
            }

            if (item == null)
                return SyncAttempt<IMedia>.Fail(node.NameFromNode(), ChangeType.ImportFail, "Cannot find or create media item");

            if (item.Key != guid)
                item.Key = guid;

            if (item.Name != name)
                item.Name = name;

            if (item.ParentId != parentId)
                item.ParentId = parentId;

            /*
                * properties are set in second pass, for speed we don't do it here.
            */

            _mediaService.Save(item);

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
            if (uSyncCoreContext.Instance.Configuration.Settings.ContentMatch.Equals("mismatch", StringComparison.OrdinalIgnoreCase))
                return IsDiffrent(node);
            else
                return IsNewer(node);
        }

        /// <summary>
        ///  the contentedition way, we only update if content in the node is newer
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool IsNewer(XElement node)
        {
            LogHelper.Debug<MediaSerializer>("Using IsNewer Checker");
            var key = node.Attribute("guid").ValueOrDefault(Guid.Empty);
            if (key == Guid.Empty)
                return true;

            var item = _mediaService.GetById(key);
            if (item == null)
                return true;

            DateTime updateTime = node.Attribute("updated").ValueOrDefault(DateTime.Now).ToUniversalTime();

            if ((updateTime - item.UpdateDate.ToUniversalTime()).TotalSeconds > 1)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// are the node and content diffrent, this is the standard uSync way of doing comparisons. 
        /// </summary>
        private bool IsDiffrent(XElement node)
        {
            LogHelper.Debug<MediaSerializer>("Using IsDiffrent Checker");
            var key = node.Attribute("guid").ValueOrDefault(Guid.Empty);
            if (key == Guid.Empty)
                return true;

            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var item = _mediaService.GetById(key);
            if (item == null)
                return true;

            var attempt = Serialize(item);
            if (!attempt.Success)
                return true;

            var itemHash = attempt.Item.GetSyncHash();

            return (nodeHash.Equals(itemHash));
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
