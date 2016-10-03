using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.BackOffice.Helpers
{
    /// <summary>
    ///  action tracker, lets you track deletes
    /// </summary>
    public class ActionTracker
    {
        private static List<SyncAction> _actions;
        private static string _actionFile;
        private static object _saveLock = new object();

        public ActionTracker(string folder)
        {
            _actionFile = Path.Combine(folder,"uSyncActions.config");

            LoadActions();
        }

        private void LoadActions()
        {
            _actions = new List<SyncAction>();

            if (File.Exists(_actionFile))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<SyncAction>));
                    using (FileStream fs = new FileStream(_actionFile, FileMode.Open))
                    {
                        _actions = (List<SyncAction>)serializer.Deserialize(fs);
                    }
                }
                catch
                {
                    // format fail on load.
                    _actions = new List<SyncAction>();
                }
            }
        }

        public void SaveActions()
        {
            lock(_saveLock)
            {
                if (File.Exists(_actionFile))
                    File.Delete(_actionFile);
                XmlSerializer serializer = new XmlSerializer(typeof(List<SyncAction>));
                using (StreamWriter sw = new StreamWriter(_actionFile))
                {
                    serializer.Serialize(sw, _actions);
                }
            }
        }

        /// <summary>
        ///  Add an action to uSync action log, for later retrevial
        /// </summary>
        /// <param name="actionType">type of action (delete, rename)</param>
        /// <param name="key">Guid of item (if it has one)</param>
        /// <param name="keyNameValue">value of element used as key (alias or name)</param>
        /// <param name="type">type of object</param>
        public void AddAction(SyncActionType actionType, Guid key, string keyNameValue, Type type)
        {
            var existing = _actions
                .Where(x => x.TypeName == type.ToString() 
                    && x.Key == key 
                    && x.Name == keyNameValue
                    && x.Action == actionType).Any();

            if (!existing)
            {
                _actions.Add(new SyncAction()
                {
                    Action = actionType,
                    Key = key,
                    Name = keyNameValue,
                    TypeName = type.ToString()
                });

                SaveActions();
            }
        }

        public void AddAction(SyncActionType actionType, string keyNameValue, Type type)
        {
            var existing = _actions.Where(x => x.Name == keyNameValue && x.TypeName == type.ToString() && x.Action == actionType).Any();

            if (!existing)
            {
                _actions.Add(new SyncAction()
                {
                    Action = actionType,
                    Key = Guid.Empty,
                    Name = keyNameValue,
                    TypeName = type.ToString()
                });

                SaveActions();
            }
        }


        public bool RemoveActions(string keyNameValue, string type)
        {
            bool changes = false;
            var actionsToRemove = _actions.Where(x => x.TypeName == type && x.Name == keyNameValue).ToList();

            if (actionsToRemove.Any())
            {
                foreach (var a in actionsToRemove)
                {
                    _actions.Remove(a);
                    changes = true;
                }
            }

            if (changes)
                SaveActions();

            return changes;

        }
        public void RemoveActions(string keyNameValue, Type type)
        {
            var typeName = type.ToString();
            RemoveActions(keyNameValue, typeName);
        }

        public IEnumerable<SyncAction> GetActions(Type type)
        {
            LogHelper.Debug<uSyncAction>("Getting Actions: for type {0} from {1} actions, found {2}",
                ()=> type.ToString(), 
                ()=> _actions.Count, 
                ()=> _actions.Count(x => x.TypeName == type.ToString())
                );
            return _actions.Where(x => x.TypeName == type.ToString());
        }

        public IEnumerable<SyncAction> GetActions(SyncActionType actionType)
        {
            return _actions.Where(x => x.Action == actionType);
        }

        public IEnumerable<SyncAction> GetAllActions()
        {
            return _actions;
        }
    }

    public class SyncAction
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
        public Guid Key { get; set; }
        public SyncActionType Action {get; set; }
    }

    public enum SyncActionType
    {
        Delete,
        Rename,
        Obsolete, 
    }
}
