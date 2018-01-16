using Jumoo.uSync.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Core.Helpers
{
    /// <summary>
    ///  helper class to work out actual changes based on the contents of the xml files.
    /// </summary>
    public class uSyncChangeTracker
    {
        // if the node name is in this list it doesn't get added to the path
        private static Dictionary<string, string> nodePaths = new Dictionary<string, string>
        {
            { "Info", "Core" },
            { "GenericProperties", "Property" }
        };

        // when the node is called something here, we lookup the value in the pair
        // and use that instead (so we can get names for properties)
        private static Dictionary<string, ChangeKeyPair> nodeKeys = new Dictionary<string, ChangeKeyPair>()
        {
            { "GenericProperty", new ChangeKeyPair("Key", ChangeValueType.Element) },
            { "PreValue", new ChangeKeyPair("Alias", ChangeValueType.Attribute) },
            { "Value", new ChangeKeyPair("LanguageCultureAlias", ChangeValueType.Attribute) },
            { "Composition", new ChangeKeyPair("Key", ChangeValueType.Attribute) },
            { "MediaType", new ChangeKeyPair("Key", ChangeValueType.Attribute) }

        };

        private static Dictionary<string, ChangeKeyPair> nodeNames = new Dictionary<string, ChangeKeyPair>()
        {
            { "GenericProperty", new ChangeKeyPair("Name", ChangeValueType.Element) },
            { "PreValue", new ChangeKeyPair("Alias", ChangeValueType.Attribute) },
            { "Value", new ChangeKeyPair("LanguageCultureAlias", ChangeValueType.Attribute) },
        };

        // nodes where we match them on the internal values of the elements. 
        private static List<string> nodesByVal = new List<string>()
        {
            { "Template" }, {"Tab"}
        };

        /// <summary>
        ///  attributes we ignore (becuase they are internal ids we don't care about)
        /// </summary>
        private static List<string> ignoreAttribs = new List<string>()
        {
            { "LanguageId" }, { "Id" }
        }; 

        /// <summary>
        ///  gets the changes between to xml files, will recurse down a tree and 
        ///  note any changes in attribute, value or elements. 
        /// </summary>
        public static List<uSyncChange> GetChanges(XElement newSourceNode, XElement oldTargetNode, string path)
        {
            var targetNode = newSourceNode.GetLocalizeduSyncElement();
            var sourceNode = oldTargetNode.GetLocalizeduSyncElement();

            List<uSyncChange> changes = new List<uSyncChange>();
            if (targetNode == null || sourceNode == null)
                return changes;
            
            if (targetNode.Name.LocalName != sourceNode.Name.LocalName)
            {
                changes.Add(new uSyncChange
                {
                    Path = path,
                    Name = GetElementName(targetNode.Name.LocalName, targetNode),
                    Change = ChangeDetailType.Update,
                    NewVal = targetNode.Name.LocalName,
                    OldVal = sourceNode.Name.LocalName, 
                    ValueType = ChangeValueType.Element                  
                });
            }


            // check
            if (targetNode.HasAttributes)
            {
                foreach (var newAttrib in targetNode.Attributes())
                {
                    if (!ignoreAttribs.Contains(newAttrib.Name.LocalName))
                    {
                        var oldAttrib = sourceNode.Attribute(newAttrib.Name);
                        if (oldAttrib == null)
                        {
                            changes.Add(new uSyncChange
                            {
                                Path = path,
                                Name = newAttrib.Name.LocalName,
                                Change = ChangeDetailType.Delete,
                                ValueType = ChangeValueType.Attribute,
                                OldVal = "attribute"
                            });
                        }
                        else
                        {
                            if (newAttrib.Value != oldAttrib.Value)
                            {
                                changes.Add(new uSyncChange
                                {
                                    Path = path,
                                    Name = newAttrib.Name.LocalName,
                                    Change = ChangeDetailType.Update,
                                    NewVal = newAttrib.Value,
                                    OldVal = oldAttrib.Value,
                                    ValueType = ChangeValueType.Attribute
                                });
                            }
                        }
                    }
                }
            }

            // new attributes
            if (sourceNode.HasAttributes)
            {
                foreach (var oldAttrib in sourceNode.Attributes())
                {
                    if (!ignoreAttribs.Contains(oldAttrib.Name.LocalName))
                    {
                        if (targetNode.Attribute(oldAttrib.Name) == null)
                        {
                            changes.Add(new uSyncChange
                            {
                                Path = path,
                                Name = oldAttrib.Name.LocalName,
                                Change = ChangeDetailType.Create,
                                OldVal = "attribute"
                            });
                        }
                    }
                }
            }

            // 3: recurse
            if (targetNode.HasElements)
            {
                foreach (var newChild in targetNode.Elements())
                {
                    var key = GetElementKey(newChild);
                    var name = GetElementName(key.Key, newChild);
                    var oldChild = GetElement(key, name, newChild, sourceNode);

                    if (oldChild == null)
                    {
                        // missing
                        changes.Add(new uSyncChange
                        {
                            Path = path,
                            Name = name,
                            Change = ChangeDetailType.Create,
                            ValueType = ChangeValueType.Element                            
                        });
                    }
                    else
                    {
                        // get the path... 
                        var childPath = "";
                        if (!nodePaths.ContainsKey(newChild.Name.LocalName))
                        {
                            childPath = name;
                            if (nodeNames.ContainsKey(newChild.Name.LocalName))
                            {
                                // get the property name from the nodes in this 
                                var changePair = nodeNames[newChild.Name.LocalName];
                                switch(changePair.Type)
                                {
                                    case ChangeValueType.Element:
                                        var nameNode = newChild.Element(changePair.Name);
                                        if (nameNode != null)
                                            childPath = nameNode.Value;
                                        break;
                                    case ChangeValueType.Attribute:
                                        childPath = newChild.Attribute(changePair.Name).ValueOrDefault(childPath);
                                        break;
                                }
                            }
                        }
                        else
                        {
                            childPath = nodePaths[newChild.Name.LocalName];
                        }

                        changes.AddRange(GetChanges(newChild, oldChild, string.Format("{0}.{1}", path, childPath)));
                    }
                }
            }
            else
            {
                // compare element value
                if (targetNode.Value != sourceNode.Value)
                {
                    var index = path.LastIndexOf('.');
                    if (index == -1)
                        index = path.Length;

                    changes.Add(new uSyncChange
                    {
                        Path = path.Substring(0, index),
                        Name = GetElementName(targetNode.Name.LocalName, targetNode),
                        Change = ChangeDetailType.Update,
                        NewVal = targetNode.Value,
                        OldVal = sourceNode.Value,
                        ValueType = ChangeValueType.Element                        
                    });
                }
            }

            // new elements
            if (sourceNode.HasElements)
            {
                foreach (var oldElement in sourceNode.Elements())
                {
                    var key = GetElementKey(oldElement);
                    var name = GetElementName(key.Key, oldElement);
                    var newElement = GetElement(key, name, oldElement, targetNode);

                    if (newElement == null )
                    { 
                        changes.Add(new uSyncChange
                        {
                            Path = path,
                            Name = name,
                            Change = ChangeDetailType.Delete,
                            ValueType = ChangeValueType.Element, 
                            OldVal = name
                        });
                    }
                }
            }
            return changes;
        }

        private static ChangeDetailKey GetElementKey(XElement node)
        {
            ChangeDetailKey key = new ChangeDetailKey()
            {
                Key = node.Name.LocalName,
                Value = node.Name.LocalName
            };

            // for many things we use a value below the element as the key for the element.
            if (nodeKeys.ContainsKey(node.Name.LocalName))
            {
                var keyPair = nodeKeys[node.Name.LocalName];
                key.Key = keyPair.Name;
                key.Value = node.Name.LocalName;
                key.Type = keyPair.Type;

                switch (keyPair.Type)
                {
                    case ChangeValueType.Element:
                        var keyNode = node.Element(key.Key);
                        if (keyNode != null)
                            key.Value = keyNode.Value;
                        break;
                    case ChangeValueType.Attribute:
                        key.Value = node.Attribute(key.Key).ValueOrDefault(key.Value);
                        break;
                }
            }

            if (nodesByVal.Contains(node.Name.LocalName))
            {
                key.Value = node.Value;
            }

            return key;
        }

        private static string GetElementName(string key, XElement node)
        {
            if (nodeNames.ContainsKey(node.Name.LocalName))
            {
                var keypair = nodeNames[node.Name.LocalName];
                switch(keypair.Type)
                {
                    case ChangeValueType.Element:
                        var nameNode = node.Element(keypair.Name);
                        if (nameNode != null)
                            return nameNode.Value;
                        break;
                    case ChangeValueType.Attribute:
                        return node.Attribute(keypair.Name).ValueOrDefault(key);
                }
            }

            if (nodesByVal.Contains(node.Name.LocalName))
                return node.Value;

            if (key == node.Name.LocalName)
                return key;

            return key;
        }

        private static XElement GetElement(ChangeDetailKey key, string name, XElement node, XElement target)
        {
            if (key.Value != node.Name.LocalName)
            {
                switch(key.Type)
                {
                    case ChangeValueType.Element:
                        return target.Elements()
                            .Where(x => x.Element(key.Key) != null && x.Element(key.Key).Value == key.Value)
                            .FirstOrDefault();
                    case ChangeValueType.Attribute:
                        return target.Elements()
                            .Where(x => x.Attribute(key.Key) != null && x.Attribute(key.Key).Value == key.Value)
                            .FirstOrDefault();
                    case ChangeValueType.Node:
                        return target.Elements()
                            .Where(x => x.Name.LocalName == key.Key && x.Value == key.Value)
                            .FirstOrDefault();
                    default:
                        return target.Elements()
                            .Where(x => x.Name.LocalName == key.Key && x.Value == key.Value)
                            .FirstOrDefault();

                }
            }
            else
            {
                return target.Element(name);
            }
        }

        public static IEnumerable<uSyncChange> NewItem(string name)
        {
            return new List<uSyncChange>
            {
                new uSyncChange
                {
                    Name = name,
                    Change = ChangeDetailType.Create,
                    ValueType = ChangeValueType.Node
                }
            };
        }

        public static IEnumerable<uSyncChange> ChangeError(string name)
        {
            return new List<uSyncChange>
            {
                new uSyncChange
                {
                    Name = name,
                    Change = ChangeDetailType.Error,
                    ValueType = ChangeValueType.Node
                }
            };

        }

        public class ChangeDetailKey
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public ChangeValueType Type { get; set; }
        }

    }


    public class uSyncChange
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public ChangeDetailType Change { get; set; }
        public string OldVal { get; set; }
        public string NewVal { get; set; }
        public ChangeValueType ValueType { get; set; }
    }

    public enum ChangeDetailType
    {
        Create,
        Update,
        Delete,
        Error
    }

    public enum ChangeValueType
    {
        Node, Element, Attribute, Value
    }

    public class ChangeKeyPair
    {
        public ChangeKeyPair()
        { }

        public ChangeKeyPair(string n, ChangeValueType t)
        {
            Name = n; Type = t;
        }

        public string Name { get; set; }
        public ChangeValueType Type { get; set; }
    }
}
