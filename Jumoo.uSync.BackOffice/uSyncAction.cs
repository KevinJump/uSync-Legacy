using Jumoo.uSync.Core;
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

namespace Jumoo.uSync.BackOffice
{

    public struct uSyncAction
    {
        public bool Success { get; set; }
        public Type ItemType { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public ChangeType Change { get; set; }
        public string FileName { get; }
        public string Name { get; set; }

        internal uSyncAction(bool success, string name, Type type, ChangeType change, string message, Exception ex, string filename)
        {
            Success = success;
            Name = name;
            ItemType = type;
            Message = message;
            Change = change;
            Exception = ex;
            FileName = filename;
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
       
        public static uSyncAction SetAction(SyncAttempt<T> attempt, string filename)
        {
            Type itemType = null;
            string name = "unknown";
            if (attempt.Item != null)
                attempt.Item.GetType();

            name = Path.GetFileName(filename);

            return new uSyncAction(attempt.Success, name, itemType, attempt.Change, attempt.Message, attempt.Exception, filename);
        }

        public static uSyncAction SetAction(string name, SyncAttempt<T> attempt, string filename)
        {
            Type itemType = null;
            if (attempt.Item != null)
                attempt.Item.GetType();

            return new uSyncAction(attempt.Success, name, itemType, attempt.Change, attempt.Message, attempt.Exception, filename);
        }
    }


    public class uSyncActionLogger
    {
        public static void SaveActionLog(string name, List<uSyncAction> actions)
        {
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
                new XAttribute("Name", name), 
                new XAttribute("DateTime", DateTime.Now.ToString("g")));

            XElement logNode = new XElement("Actions");
            
            foreach (var action in actions)
            {
                var actionNode = new XElement("Action",
                                        new XAttribute("Change", action.Change.ToString()),
                                        new XAttribute("Success", action.Success),
                                        new XAttribute("Message", action.Message),
                                        new XAttribute("Name", action.Name));

                if (action.ItemType != null)
                {
                    string type = action.ItemType.ToString();
                    type = type.Substring(type.LastIndexOf('.'));
                    actionNode.Add(new XAttribute("Type", type));
                }

                if (action.Exception != null)
                    actionNode.Add(new XElement("Exception", action.Exception.ToString()));
                

                logNode.Add(actionNode);
            }

            logFile.Add(logNode);
            logFile.Save(filePath);
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
}
