using System;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core.Models;

using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Helpers;


namespace Jumoo.uSync.Core.Serializers
{
    public class MediaSerializer : ContentBaseSerializer<IMedia>
    {

        private uSyncMediaFileMover _mover;

        public MediaSerializer(string mediaFolder) : base(string.Empty)
        {
            _mover = new uSyncMediaFileMover(mediaFolder);
        }

        internal override SyncAttempt<IMedia> DeserializeCore(XElement node, int parentId, bool forceUpdate)
        {
            bool newItem = false;

            var nodeGuid = node.Attribute("guid");
            if (nodeGuid == null)
                return SyncAttempt<IMedia>.Fail(node.NameFromNode(), ChangeType.Import, "No guid, in xml");

            Guid sourceGuid = new Guid(nodeGuid.Value);
            Guid targetGuid = uSyncIdMapper.GetTargetGuid(sourceGuid);

            var name = node.Attribute("nodeName").Value;
            string mediaTypeAlias = node.Attribute("nodeTypeAlias").Value;

            var update = node.Attribute("updated").ValueOrDefault(DateTime.Now);

            var item = _mediaService.GetById(targetGuid);
            if (item == null || item.Trashed)
            {
                newItem = true;
                item = _mediaService.CreateMedia(name, parentId, mediaTypeAlias);
            }
            else
            {
                if (!forceUpdate)
                {
                    if (DateTime.Compare(update, item.UpdateDate.ToLocalTime()) < 0)
                        return SyncAttempt<IMedia>.Succeed(item.Name, item, ChangeType.NoChange);
                }
            }

            if (item != null)
            {
                if (item.Name != name)
                    item.Name = name;

                if (item.ParentId != parentId)
                    item.ParentId = parentId;

                _mover.ImportFile(sourceGuid, item);

                _mediaService.Save(item);
            }

            if (newItem)
                uSyncIdMapper.AddPair(sourceGuid, item.Key);

            return SyncAttempt<IMedia>.Succeed(item.Name, item, ChangeType.Import);

        }

        internal override SyncAttempt<XElement> SerializeCore(IMedia item)
        {
            var mediaTypeAlias = item.ContentType.Alias;

            var attempt = base.SerializeBase(item, mediaTypeAlias);
            if (!attempt.Success)
                return attempt;

            var node = attempt.Item;

            node.Add(new XAttribute("parentGUID", item.Level > 1 ? item.Parent().Key : new Guid("")));
            node.Add(new XAttribute("nodeTypeAlias", item.ContentType.Alias));
            node.Add(new XAttribute("path", item.Path));

            foreach (var file in item.Properties.Where(p => p.Alias == "umbracoFile"))
            {
                if (file == null || file.Value == null)
                {
                    // we don't have an associated file
                }
                else
                {
                    _mover.ExportMediaFile(file.Value.ToString(), uSyncIdMapper.GetSourceGuid(item.Key));
                }

            }

            return SyncAttempt<XElement>.Succeed(item.Name, node, ChangeType.Export);

        }
    }
}
