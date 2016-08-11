using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Jumoo.uSync.Core.Interfaces;
using Umbraco.Core.Services;
using Umbraco.Core.Models;
using Jumoo.uSync.Core;
using System.Xml.Linq;

using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Serializers;
using Jumoo.uSync.Core.Extensions;

namespace Jumoo.uSync.Content.UrlRedirect
{
    public class RedirectSerializer : ISyncSerializer<IContent>
    {
        string _itemType = "UrlRedirect";
        IRedirectUrlService _redirectService;
        IContentService _contentService;

        public RedirectSerializer() 
        {
            _redirectService = ApplicationContext.Current.Services.RedirectUrlService;
            _contentService = ApplicationContext.Current.Services.ContentService;
        }

        public SyncAttempt<IContent> DeSerialize(XElement node, bool forceUpdate)
        {
            if (node.Name.LocalName != _itemType && node.Name.LocalName != "EntityFolder")
                throw new ArgumentException("XML not valid for type: " + _itemType);

            var key = node.Attribute("contentGuid").ValueOrDefault(Guid.Empty);
            var name = node.Attribute("name").ValueOrDefault(key.ToString());
            if (key != Guid.Empty)
            {
                var contentRedirects = _redirectService.GetContentRedirectUrls(key);

                foreach(var redirect in node.Elements("redirect"))
                {
                    var url = redirect.Attribute("url").ValueOrDefault(string.Empty);
                    if (!string.IsNullOrEmpty(url))
                    {
                        if (!contentRedirects.Any(x => x.Url == url))
                        {
                            _redirectService.Register(url, key);
                        }
                    }
                }
            }

            return SyncAttempt<IContent>.Succeed(name, ChangeType.Import);
        }

        public bool IsUpdate(XElement node)
        {
            throw new NotImplementedException();
        }

        public SyncAttempt<XElement> Serialize(IContent item)
        {
            var redirects = _redirectService.GetContentRedirectUrls(item.Key);

            var node = new XElement(_itemType,
                new XAttribute("contentGuid", item.Key),
                new XAttribute("name", item.Name));

            foreach(var redirect in redirects)
            {
                var r = new XElement("redirect");
                r.Add(new XAttribute("url", redirect.Url));
                node.Add(r);
            }
            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IRedirectUrl), ChangeType.Export);
        }
                
    }
}
