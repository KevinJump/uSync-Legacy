using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
            {  "Info", "Core" },
            { "GenericProperties", "Property" }
        };

        // when the node is called something here, we lookup the value in the pair
        // and use that instead (so we can get names for properties)
        private static Dictionary<string, string> nodeKeys = new Dictionary<string, string>()
        {
            { "GenericProperty", "Key" }
        };

        private static Dictionary<string, string> nodeNames = new Dictionary<string, string>()
        {
            { "GenericProperty", "Name" }
        };

        /// <summary>
        ///  gets the changes between to xml files, will recurse down a tree and 
        ///  note any changes in attribute, value or elements. 
        /// </summary>
        public static List<uSyncChange> GetChanges(XElement source, XElement target, string path)
        {
            List<uSyncChange> changes = new List<uSyncChange>();
            if (source.Name.LocalName != target.Name.LocalName)
            {
                changes.Add(new uSyncChange
                {
                    Path = path,
                    Name = source.Name.LocalName,
                    Change = ChangeDetailType.Update,
                    NewVal = source.Name.LocalName,
                    OldVal = target.Name.LocalName, 
                    ValueType = ChangeValueType.Element                  
                });
            }


            // check
            if (source.HasAttributes)
            {
                foreach (var sourceAttrib in source.Attributes())
                {
                    var targetAttrib = target.Attribute(sourceAttrib.Name);
                    if (targetAttrib == null)
                    {
                        changes.Add(new uSyncChange
                        {
                            Path = path,
                            Name = sourceAttrib.Name.LocalName,
                            Change = ChangeDetailType.Delete,
                            ValueType = ChangeValueType.Attribute
                            
                        });
                    }
                    else
                    {
                        if (sourceAttrib.Value != targetAttrib.Value)
                        {
                            changes.Add(new uSyncChange
                            {
                                Path = path,
                                Name = sourceAttrib.Name.LocalName,
                                Change = ChangeDetailType.Update,
                                OldVal = sourceAttrib.Value,
                                NewVal = targetAttrib.Value,
                                ValueType = ChangeValueType.Attribute
                            });
                        }
                    }
                }
            }

            // new attributes
            if (target.HasAttributes)
            {
                foreach (var targetAttrib in target.Attributes())
                {
                    if (source.Attribute(targetAttrib.Name) == null)
                    {
                        changes.Add(new uSyncChange
                        {
                            Path = path,
                            Name = targetAttrib.Name.LocalName,
                            Change = ChangeDetailType.Create
                        });
                    }
                }
            }

            // 3: recurse
            if (source.HasElements)
            {
                foreach (var sourceChild in source.Elements())
                {
                    var key = GetElementKey(sourceChild);
                    var name = GetElementName(key.Key, sourceChild);
                    var targetChild = GetElement(key, name, sourceChild, target);

                    if (targetChild == null)
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
                        if (!nodePaths.ContainsKey(sourceChild.Name.LocalName))
                        {
                            childPath = name;
                            if (nodeNames.ContainsKey(sourceChild.Name.LocalName))
                            {
                                // get the property name from the nodes in this 
                                var nameNode = sourceChild.Element(nodeNames[sourceChild.Name.LocalName]);
                                if (nameNode != null)
                                    childPath = nameNode.Value;
                            }
                        }
                        else
                        {
                            childPath = nodePaths[sourceChild.Name.LocalName];
                        }

                        changes.AddRange(GetChanges(sourceChild, targetChild, string.Format("{0}.{1}", path, childPath)));
                    }
                }
            }
            else
            {
                // compare element value
                if (source.Value != target.Value)
                {
                    changes.Add(new uSyncChange
                    {
                        Path = path,
                        Name = source.Name.LocalName,
                        Change = ChangeDetailType.Update,
                        NewVal = source.Value,
                        OldVal = target.Value,
                        ValueType = ChangeValueType.Element                        
                    });
                }
            }

            // new elements
            if (target.HasElements)
            {
                foreach (var targetElement in target.Elements())
                {
                    var key = GetElementKey(targetElement);
                    var name = GetElementName(key.Key, targetElement);
                    var sourceElement = GetElement(key, name, targetElement, source);

                    if (sourceElement == null )
                    { 
                        changes.Add(new uSyncChange
                        {
                            Path = path,
                            Name = name,
                            Change = ChangeDetailType.Delete,
                            ValueType = ChangeValueType.Element
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
                key.Key = nodeKeys[node.Name.LocalName];
                key.Value = node.Name.LocalName;
                var keyNode = node.Element(key.Key);
                if (keyNode != null)
                {
                    key.Value = keyNode.Value;
                }
            }

            return key;
        }

        private static string GetElementName(string key, XElement node)
        {
            if (key == node.Name.LocalName)
                return key;

            if (nodeNames.ContainsKey(node.Name.LocalName))
            {
                var nameNode = node.Element(nodeNames[node.Name.LocalName]);
                if (nameNode != null)
                {
                    return nameNode.Value;
                }
            }

            return key;
        }

        private static XElement GetElement(ChangeDetailKey key, string name, XElement node, XElement target)
        {
            if (key.Value != node.Name.LocalName)
            {
                return target.Elements()
                    .FirstOrDefault(x => x.Element(key.Key) != null && x.Element(key.Key).Value == key.Value);
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
}
