using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Core.Helpers
{
    /// <summary>
    ///  Maps internal IDs inside things like preValues into 
    ///  things that can transend installations 
    ///     (usally names, aliases or paths)
    /// 
    ///  these mappings are usally stored in the config file
    ///  and given a guid. 
    /// 
    ///  the ValueMapper then trys to translate that value
    ///  to and from something generic.
    /// 
    /// 
    /// </summary>
    public class uSyncValueMapper
    {

        // TODO: Mapping needs fully testing 

        private readonly uSyncValueMapperSettings _settings;
        private XElement _node;

        public uSyncValueMapper(XElement node, uSyncValueMapperSettings settings)
        {
            _settings = settings;
            _node = node;
        }

        public string ValueAlias
        {
            get { return _settings.ValueAlias;  }
        }

        #region ToGeneric (going out from installation)
        public bool MapToGeneric(string value, int mapId)
        {
            bool isMapped = false;

            var ids = GetValueMatchSubstring(value);
            
            foreach (Match match in Regex.Matches(ids, _settings.IdRegex))
            {
                string mappingType = _settings.MappingType.ToLower();
                string id = match.Value;
                string mappedValue = string.Empty;

                foreach (var type in mappingType.Split(','))
                {
                    var destinationType = type;
                    switch (type)
                    {
                        case "content":
                            mappedValue = ContentToGeneric(id);
                            break;
                        case "media":
                            mappedValue = MediaToGeneric(id);
                            break;
                        case "tab":
                            mappedValue = TabToGeneric(id);
                            break;
                        case "mediatype":
                            mappedValue = MediaTypeToGeneric(id);
                            break;
                        case "doctype":
                            mappedValue = ContentTypeToGeneric(id);
                            break;
                    }

                    if (!string.IsNullOrEmpty(mappedValue))
                    {
                        AddToNode(id, mappedValue, destinationType, mapId);
                        isMapped = true;
                        break;
                    }
                }
            }

            return isMapped;
        }

        private string GetValueMatchSubstring(string value)
        {

            switch(_settings.ValueStorageType.ToLower())
            {
                case "json":
                    LogHelper.Debug<uSyncValueMapper>("Mapping Alias: {1} Value: {0}", () => value, ()=> _settings.ValueAlias);
                    if (!string.IsNullOrEmpty(_settings.PropertyName) && IsJson(value))
                    {
                        JObject jObject = JObject.Parse(value);

                        if (jObject != null )
                        {
                            var propertyValue = jObject.SelectToken(_settings.PropertyName);
                            if (propertyValue != null)
                                return propertyValue.ToString(Newtonsoft.Json.Formatting.None);
                        }
                        else
                        {
                            LogHelper.Warn<uSyncValueMapper>("Mapping JSON Couldn't parse : {0}", ()=> value);
                        }
                    }
                    break;
                case "number":
                    break;
                case "text":
                    if (_settings.PropertySplitter != '\0' && _settings.PropertyPosistion > 0)
                    {
                        if (value.Contains(_settings.PropertySplitter))
                        {
                            var props = value.Split(_settings.PropertySplitter);
                            if (props.Count() >= _settings.PropertyPosistion)
                            {
                                return props[_settings.PropertyPosistion - 1];
                            }
                        }

                    }
                    break;
            }

            return value;
        }

        private bool IsJson(string val)
        {
            val = val.Trim();
            return (val.StartsWith("{") && val.EndsWith("}"))
                || (val.StartsWith("[") && val.EndsWith("]"));
        }

        private void AddToNode(string id, string value, string type, int mapId)
        {
            XElement nodes = _node.Element("Nodes");
            if (nodes == null)
            {
                nodes = new XElement("Nodes");
                _node.Add(nodes);
            }

            var mappedNode = new XElement("Node",
                    new XAttribute("Id", id),
                    new XAttribute("Value", value),
                    new XAttribute("Type", type),
                    new XAttribute("MapGuid", mapId.ToString()));

            nodes.Add(mappedNode);
        }

        /// <summary>
        ///  generic mappers - really i want to farm these off - we could
        /// have a mapper interface, and if you impliment it for a type
        /// then you could just call that to map the type.
        /// </summary>
         

        private string ContentToGeneric(string id)
        {
            uSyncTreeWalker walker = new uSyncTreeWalker(UmbracoObjectTypes.Document);

            int contentId;
            if (int.TryParse(id, out contentId))
                return walker.GetPathFromId(contentId, UmbracoObjectTypes.Document);

            return string.Empty;
        }

        private string MediaToGeneric(string id)
        {
            uSyncTreeWalker walker = new uSyncTreeWalker(UmbracoObjectTypes.Media);

            int contentId;
            if (int.TryParse(id, out contentId))
                return walker.GetPathFromId(contentId, UmbracoObjectTypes.Media);

            return string.Empty;
        }

        private string MediaTypeToGeneric(string id)
        {
            int typeId;
            if (int.TryParse(id, out typeId))
            {
                var item = ApplicationContext.Current.Services.ContentTypeService.GetMediaType(id);
                if (item != null)
                    return item.Key.ToString();
            }
            return string.Empty;
        }

        private string ContentTypeToGeneric(string id)
        {
            int typeId;

            if (int.TryParse(id, out typeId))
            {
                var item = ApplicationContext.Current.Services.ContentTypeService.GetContentType(typeId);
                if (item != null)
                    return item.Key.ToString();
            }

            return string.Empty;
        }

        private string TabToGeneric(string id)
        {
            int tabId;
            if (int.TryParse(id, out tabId))
            {
                foreach (var contentType in ApplicationContext.Current.Services.ContentTypeService.GetAllContentTypes())
                {
                    var tab = contentType.PropertyGroups.Where(x => x.Id == tabId).FirstOrDefault();
                    if (tab != null)
                        return contentType.Key.ToString() + "|" + tab.Name;
                }
            }
            return string.Empty;
        }

        #endregion

        #region ToId (coming in)

        public string MapToId(XElement valueNode, PreValue preVal)
        {
            // existing values are appended with zzusync
            // and as they are replace that is removed
            // it stops us double mapping

            // at the end we remove the zzusync
            Regex regx = new Regex(@"\d+");
            var value = regx.Replace(valueNode.Value, "$0:zzusync");

            var mapGuid = valueNode.Attribute("MapGuid");
            if (mapGuid == null)
                return value;

            var mappedNodes = _node.Element("Nodes").Descendants()
                .Where(x => x.Attribute("MapGuid").Value == mapGuid.Value)
                .ToList();

            foreach (var mapNode in mappedNodes)
            {
                var type = mapNode.Attribute("Type").Value;
                var val = mapNode.Attribute("Value").Value;
                var id = mapNode.Attribute("Id").Value + ":zzusync";


                var localId = GetMappedId(val, type); // now returns empty string if it's can't map the id.
                if (localId.IsNullOrWhiteSpace()) // if it is empty then we get the value from the existing pre-value (so no mapping)
                {
                    LogHelper.Debug<uSyncValueMapper>("Mapping LocalId back to value in umbraco");
                    localId = GetValueMatchSubstring(preVal.Value);
                }

                // replace any instances of id with the value in localId
                value = value.Replace(id, localId);
            }

            return value.Replace(":zzusync", "");
        }

        public string GetMappedId(string value, string type)
        {
            switch(type)
            {
                case "content":
                    return ContentToId(value);
                case "media":
                    return MediaToId(value);
                case "tab":
                    return TabToId(value);
                case "mediatype":
                    return MediaTypeToId(value);
                case "doctype":
                    return ContentTypeToId(value);
            }

            return string.Empty;
        }

        private string MediaToId(string value)
        {
            var walker = new uSyncTreeWalker(UmbracoObjectTypes.Media);
            var mappedId = walker.GetIdFromPath(value);
            return (mappedId != -1) ? mappedId.ToString() : string.Empty;
        }

        private string ContentToId(string value)
        {
            var walker = new uSyncTreeWalker(UmbracoObjectTypes.Document);
            var mappedId = walker.GetIdFromPath(value);
            return (mappedId != -1) ? mappedId.ToString() : string.Empty;
        }

        private string MediaTypeToId(string value)
        {

            var itemKey = Guid.Empty;
            if (Guid.TryParse(value, out itemKey))
            {
                var item = ApplicationContext.Current.Services.ContentTypeService.GetMediaType(itemKey);
                if (item != null)
                    return item.Id.ToString();
            }
            return string.Empty;
        }

        private string ContentTypeToId(string value)
        {
            var itemKey = Guid.Empty;
            if (Guid.TryParse(value, out itemKey))
            {
                var item = ApplicationContext.Current.Services.ContentTypeService.GetContentType(itemKey);
                if (item != null)
                    return item.Id.ToString();
            }
            return string.Empty;
        }

        private string TabToId(string value)
        {
            if (value.Contains("|") && value.Split('|').Count() == 2)
            {
                var nameTabPair = value.Split('|');

                var itemKey = Guid.Empty;
                if (Guid.TryParse(nameTabPair[0], out itemKey))
                {
                    var item = ApplicationContext.Current.Services.ContentTypeService.GetContentType(itemKey);
                    if (item != null)
                    {
                        var tab = item.PropertyGroups.Where(x => x.Name == nameTabPair[1]).FirstOrDefault();
                        if (tab != null)
                            return tab.Id.ToString();
                    }
                }
            }
            return string.Empty;
        }
        #endregion
    }
}
