using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Helpers;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Core.Serializers
{
    public class DomainSerializer : SyncBaseSerializer<IDomain>, ISyncChangeDetail
    {
        private readonly IDomainService domainService;

        public override string SerializerType => uSyncConstants.Serailization.Domain;

        public DomainSerializer() 
            : base("Domain")
        {
            domainService = ApplicationContext.Current.Services.DomainService;
        }

        public DomainSerializer(string itemType) : base(itemType) { }

        internal override SyncAttempt<IDomain> DeserializeCore(XElement node)
        {
            var domainName = node.Element("DomainName").ValueOrDefault("");
            if (string.IsNullOrEmpty(domainName))
                return SyncAttempt<IDomain>.Fail(node.NameFromNode(), ChangeType.Import, "Missing Domain");

            var languageId = node.Element("LanguageId").ValueOrDefault(-1);
            var rootKeyNode = node.Element("RootContent");

            IContent contentNode = null;
            if (rootKeyNode != null)
            {
                var rootKey = rootKeyNode.Attribute("Key").ValueOrDefault(Guid.Empty);
                contentNode = ApplicationContext.Current.Services.ContentService.GetById(rootKey); 
            }

            var domains = domainService.GetAll(true);

            var domain = default(IDomain);
            if (domains.Any(x => x.DomainName == domainName))
            {
                domain = domains.FirstOrDefault(x => x.DomainName == domainName);
            }

            if (domain == default(IDomain))
            {
                domain = new UmbracoDomain(domainName);
            }

            if (languageId > -1 && domain.LanguageId != languageId)
                domain.LanguageId = languageId;

            if (contentNode != null)
                domain.RootContentId = contentNode.Id;

            domainService.Save(domain);

            return SyncAttempt<IDomain>.Succeed(domain.DomainName, domain, ChangeType.Import);

        }

        internal override SyncAttempt<XElement> SerializeCore(IDomain item)
        {
            var node = new XElement("Domain");

            node.Add(new XElement("DomainName", item.DomainName));
            node.Add(new XElement("IsWildcard", item.IsWildcard));
            node.Add(new XElement("LanguageId", item.LanguageId));
            
            if (item.RootContentId != null)
            {
                var rootNode = ApplicationContext.Current.Services.ContentService.GetById(item.RootContentId.Value);
                if (rootNode != null)
                {
                    var rootContentNode = new XElement("RootContent", rootNode.Name);
                    rootContentNode.Add(new XAttribute("Key", rootNode.Key));
                    node.Add(rootContentNode);
                }
                
            }

            return SyncAttempt<XElement>.SucceedIf(
                node != null, item.DomainName, node, typeof(IDomain), ChangeType.Export);

        }

        public override bool IsUpdate(XElement node)
        {
            if (node.IsArchiveFile())
                return false;

            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var name = node.Element("DomainName").ValueOrDefault(string.Empty);
            if (string.IsNullOrEmpty(name))
                return true;

            var item = domainService.GetByName(name);
            if (item == null)
                return true;

            var attempt = Serialize(item);
            if (!attempt.Success)
                return true;

            var itemHash = attempt.Item.GetSyncHash();

            return (!nodeHash.Equals(itemHash));
                
        }


        public IEnumerable<uSyncChange> GetChanges(XElement node)
        {
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return null;

            var name = node.Element("DomainName").ValueOrDefault(string.Empty);
            if (string.IsNullOrEmpty(name))
                return null;

            var item = domainService.GetByName(name);
            if (item == null)
                return null;

            var attempt = Serialize(item);
            if (attempt.Success)
            {
                return uSyncChangeTracker.GetChanges(node, attempt.Item, "");
            }
            else
            {
                return uSyncChangeTracker.ChangeError(name);
            }
        }
    }
}
