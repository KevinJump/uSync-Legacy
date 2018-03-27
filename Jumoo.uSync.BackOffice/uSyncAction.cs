using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.EntityBase;
using Umbraco.Core.Models;

namespace Jumoo.uSync.BackOffice
{

    public struct uSyncAction
    {
        public bool Success { get; set; }
        public Type ItemType { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public ChangeType Change { get; set; }
        public string FileName { get; private set; }
        public string Name { get; set; }
        public bool RequiresPostProcessing { get; set; }
        public IEnumerable<uSyncChange> Details { get; set; }


        internal uSyncAction(bool success, string name, Type type, ChangeType change, string message, Exception ex, string filename, bool postProcess = false) : this()
        {
            Success = success;
            Name = name;
            ItemType = type;
            Message = message;
            Change = change;
            Exception = ex;
            FileName = filename;
            RequiresPostProcessing = postProcess; 
        }

        public static uSyncAction SetAction(
            bool success,
            string name,
            Type type = null,
            ChangeType change = ChangeType.NoChange,
            string message = null,
            Exception ex = null,
            string filename = null)
        {
            return new uSyncAction(success, name, type, change, message, ex, filename);
        }

        public static uSyncAction Fail(string name,
            Type type,
            ChangeType change = ChangeType.Fail,
            string message = null,
            Exception ex = null,
            string filename = null)
        {
            return new uSyncAction(false, name, type, change, message, null, string.Empty);
        }


        public static uSyncAction Fail(string name, Type type, string message)
        {
            return new uSyncAction(false, name, type, ChangeType.Fail, message, null, string.Empty);
        }


        public static uSyncAction Fail(string name, Type type, ChangeType change, string message)
        {
            return new uSyncAction(false, name, type, change, message, null, string.Empty);
        }

        public static uSyncAction Fail(string name, Type type, string message, string file)
        {
            return new uSyncAction(false, name, type, ChangeType.Fail, message, null, file);
        }

        public static uSyncAction Fail(string name, Type type, Exception ex)
        {
            return new uSyncAction(false, name, type, ChangeType.Fail, string.Empty, ex, string.Empty);
        }

        public static uSyncAction Fail(string name, Type type, ChangeType change, Exception ex)
        {
            return new uSyncAction(false, name, type, change, string.Empty, ex, string.Empty);
        }

        public static uSyncAction Fail(string name, Type type, Exception ex, string file)
        {
            return new uSyncAction(false, name, type, ChangeType.Fail, string.Empty, ex, file);
        }
    }

    public struct uSyncActionHelper<T>
    {
        public static uSyncAction SetAction(SyncAttempt<T> attempt, string filename, bool requirePostProcessing = true)
        {
            return new uSyncAction(attempt.Success, attempt.Name, attempt.ItemType, attempt.Change, attempt.Message, attempt.Exception, filename, requirePostProcessing);
        }

        public static uSyncAction ReportAction(bool willUpdate, string name)
        {
            return new uSyncAction(true, name, typeof(T), 
                willUpdate ? ChangeType.Update : ChangeType.NoChange,
                string.Empty, null, string.Empty);
        }

        public static uSyncAction ReportAction(bool willUpdate, string name, string message)
        {
            return new uSyncAction(true, name, typeof(T), 
                willUpdate ? ChangeType.Update : ChangeType.NoChange, 
                message, null, string.Empty);
        }
    }


    public class uSyncActionLogger
    {
        public static void SaveActionLog(string name, IEnumerable<uSyncAction> actions)
        {
            try {
                // creates an action log (xml file) of the actions...
                var folderPath = IOHelper.MapPath(Path.Combine(SystemDirectories.Data, "temp", "uSync"));


                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = string.Format("{0}_{1}.config", DateTime.Now.ToString("yyyyMMdd_HHmmss"), name);

                string filePath = Path.Combine(folderPath, fileName);

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                LogHelper.Info<uSyncActionLogger>("Saving uSync Report: {0}", () => filePath);

                XElement logFile = new XElement("uSync",
                    new XAttribute("Name", string.IsNullOrEmpty(name) ? "" : name),
                    new XAttribute("DateTime", DateTime.Now.ToString("U")));

                XElement logNode = new XElement("Actions");

                foreach (var action in actions)
                {
                    var actionNode = new XElement("Action",
                                            new XAttribute("Change", action.Change.ToString()),
                                            new XAttribute("Success", action.Success),
                                            new XAttribute("Message", string.IsNullOrEmpty(action.Message) ? "" : action.Message),
                                            new XAttribute("Name", string.IsNullOrEmpty(action.Name) ? "" : action.Name));

                    if (action.ItemType != null)
                    {
                        string type = action.ItemType.ToString();
                        type = type.Substring(type.LastIndexOf('.') + 1);
                        actionNode.Add(new XAttribute("Type", string.IsNullOrEmpty(type) ? "unknown" : type));
                    }

                    if (action.Exception != null)
                        actionNode.Add(new XElement("Exception", action.Exception.ToString()));


                    logNode.Add(actionNode);
                }

                logFile.Add(logNode);
                logFile.Save(filePath);

                // keep the last 20 no more.
                CleanLogFolder(20);
            }
            catch(Exception ex)
            {
                LogHelper.Warn<uSyncActionLogger>("Failed to save action log: {0}", () => ex.ToString());
            }
        }

        internal static int ClearHistory()
        {
            // deletes the files in the history folder.
            var folderPath = IOHelper.MapPath(Path.Combine(SystemDirectories.Data, "temp", "uSync"));

            if (!Directory.Exists(folderPath))
                return 0;

            int count = 0;
            foreach (var file in Directory.GetFiles(folderPath, "*.config"))
            {
                if (System.IO.File.Exists(file))
                {
                    System.IO.File.Delete(file);
                    count++;
                }

            }

            return count; 

        }

        public static IEnumerable<uSyncHistory> GetActionHistory(bool loadHistory)
        {
            var history = new List<uSyncHistory>();

            var folderPath = IOHelper.MapPath(Path.Combine(SystemDirectories.Data, "temp", "uSync"));

            if (!Directory.Exists(folderPath))
                return history;

            var dir = new DirectoryInfo(folderPath);

            foreach(var file in dir.GetFiles("*.config"))
            {
                history.Add(LoadHistoryData(file));
            }

            history.Reverse();

            return history;
        }

        public static uSyncHistory LoadHistoryData(FileInfo file)
        {
            var info = new uSyncHistory();


            info.name = Path.GetFileNameWithoutExtension(file.Name);
            info.path = file.FullName;

            XElement data = XElement.Load(file.FullName);
            if (data != null)
            {
                info.type = data.Attribute("Name").ValueOrDefault("");
                info.date = data.Attribute("DateTime").ValueOrDefault("");

                info.actions = new List<uSyncAction>(); 

                var actions = data.Element("Actions");
                if (actions != null && actions.HasElements)
                {
                    foreach(var action in actions.Elements("Action"))
                    {
                        var name = action.Attribute("Name").ValueOrDefault("");
                        var type = action.Attribute("Type").ValueOrDefault("");
                        var message = action.Attribute("Message").ValueOrDefault("");
                        var success = action.Attribute("Success").ValueOrDefault(true);
                        var change = action.Attribute("Change").ValueOrDefault("NoChange");
                        var changeType = (ChangeType)Enum.Parse(typeof(ChangeType), change, true);

                        var umbType = Type.GetType(string.Format("Umbraco.Core.Models.{0},Umbraco.Core", type));
                        if (umbType == null)
                            umbType = typeof(Umbraco.Core.Models.EntityBase.IEntity);

                        info.actions.Add(uSyncAction.SetAction(
                            success, 
                            name, 
                            umbType, 
                            changeType,
                            message));
                    }
                }
            }

            return info; 
        }

        /// <summary>
        ///  makes sure the log folder doesn't become masssssssive!
        /// </summary>
        /// <param name="count"></param>
        private static void CleanLogFolder(int count)
        {
            // creates an action log (xml file) of the actions...
            var folderPath = IOHelper.MapPath(Path.Combine(SystemDirectories.Data, "temp", "uSync"));

            if (Directory.Exists(folderPath))
            {
                DirectoryInfo dir = new DirectoryInfo(folderPath);
                FileInfo[] filelist = dir.GetFiles("*.config");
                var files = filelist.OrderByDescending(file => file.CreationTime);
                foreach(var file in files.Skip(count))
                {
                    file.Delete();
                }
            }
        }

        public static void LogActions(List<uSyncAction> actions)
        {
            LogHelper.Info<uSyncAction>("### uSync.BackOffice Processed {0} items with {1} changes ###",
                () => actions.Count(), () => actions.Where(x => x.Change > ChangeType.NoChange).Count());

            foreach (var action in actions.Where(x => x.Change > ChangeType.NoChange))
            {
                var itemType = action.ItemType != null ? action.ItemType.ToString() : "";
                LogHelper.Info<uSyncAction>("Action: {0} {1} {2} {3} {4}",
                    () => action.Change, ()=> action.Name, () => itemType, () => action.Message, () => action.FileName);
            }
        }

    }

    public class uSyncHistory
    {
        public string name { get; set; }
        public string path { get; set; }
        public string type { get; set; }
        public string date { get; set; }

        public List<uSyncAction> actions { get; set; }
    }
}
