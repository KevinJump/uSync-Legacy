using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Jumoo.uSync.Core.Helpers;
using Slack.Webhooks;
using Umbraco.Core;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Audit.EventHandlers
{
    /// <summary>
    ///  tells slack when changes are made
    /// </summary>
    public class SlackChangeNotifier : ISyncAuditHandler
    {
        private string webHook;
        private string channelName;

        public SlackChangeNotifier(ApplicationContext appContext)
        {
        }

        public bool Activate()
        {
            webHook = ConfigurationManager.AppSettings["Audit.Slack.WebHook"];
            channelName = ConfigurationManager.AppSettings["Audit.Slack.Channel"];

            if (string.IsNullOrWhiteSpace(webHook))
                return false; 

            uSyncAudit.Changed += uSyncAudit_Changed;
            return true;
        }


        private void uSyncAudit_Changed(uSyncAudit sender, uSyncChangesEventArgs e)
        {
            LogHelper.Info<SlackChangeNotifier>("Slack Change");

            try
            {
                var itemName = "";
                var firstChange = e.Changes.ItemChanges.FirstOrDefault();
                if (firstChange!= null)
                {
                    itemName = firstChange.Name;
                }

                var slackMessage = new SlackMessage
                {
                    Text = $"{e.Changes.UserName} just changed *{itemName}* _({e.Changes.ItemType})_ ",
                    Attachments = new List<SlackAttachment>()
                };

                if (!string.IsNullOrWhiteSpace(channelName))
                    slackMessage.Channel = channelName;

                slackMessage.Attachments.AddRange(ProcessCreates(e.Changes.ItemChanges));
                slackMessage.Attachments.AddRange(ProcessUpdates(e.Changes.ItemChanges));
                slackMessage.Attachments.AddRange(ProcessDeletes(e.Changes.ItemChanges));

                if (slackMessage.Attachments.Any())
                    SendSlackNotification(slackMessage);
            }
            catch(Exception ex)
            {
                LogHelper.Error<SlackChangeNotifier>("Error sending slack notification", ex);
            }
        }

        private IEnumerable<SlackAttachment> ProcessCreates(IEnumerable<uSyncItemChanges> changes)
        {
            var attachments = new List<SlackAttachment>();

            foreach (var changeSet in changes)
            {
                var creates = changeSet.Changes.Where(x => x.Change == ChangeDetailType.Create);
                if (creates.Any())
                {
                    var attachment = new SlackAttachment
                    {
                        Fallback = "Created",
                        Text = $"Created:",
                        Color = "#43A047",
                        Fields = new List<SlackField>()
                    };


                    foreach (var change in creates)
                    {
                        var name = GetChangeDisplayName(change);

                        var slackField = new SlackField
                        {
                            Title = name,
                            Value = change.NewVal
                        };

                        attachment.Fields.Add(slackField);
                    }

                    attachments.Add(attachment);
                }
            }
            return attachments;
        }

        private IEnumerable<SlackAttachment> ProcessUpdates(IEnumerable<uSyncItemChanges> changes)
        {
            var attachments = new List<SlackAttachment>();

            foreach (var changeSet in changes)
            {
                var updates = changeSet.Changes.Where(x => x.Change == ChangeDetailType.Update);
                if (updates.Any())
                {
                    var attachment = new SlackAttachment
                    {
                        Fallback = "Updated",
                        Text = $"Updated:",
                        Color = "#039BE5",
                        Fields = new List<SlackField>()
                    };


                    foreach (var change in updates)
                    {
                        var name = GetChangeDisplayName(change);

                        var oldValue = new SlackField
                        {
                            Title = name,
                            Value = GetValueOrBlank(change.OldVal),
                            Short = true
                        };

                        var newValue = new SlackField
                        {
                            Title = "New Value",
                            Value = GetValueOrBlank(change.NewVal),
                            Short = true
                        };

                        attachment.Fields.Add(oldValue);
                        attachment.Fields.Add(newValue);
                    }

                    attachments.Add(attachment);
                }
            }
            return attachments;

        }

        private IEnumerable<SlackAttachment> ProcessDeletes(IEnumerable<uSyncItemChanges> changes)
        {
            var attachments = new List<SlackAttachment>();

            foreach (var changeSet in changes)
            {
                var deletes = changeSet.Changes.Where(x => x.Change == ChangeDetailType.Delete);
                if (deletes.Any())
                {
                    var attachment = new SlackAttachment
                    {
                        Fallback = "Deleted",
                        Text = $"Deleted:",
                        Color = "#E53935",
                        Fields = new List<SlackField>()
                    };


                    foreach (var change in deletes)
                    {
                        var name = GetChangeDisplayName(change);

                        var oldValue = new SlackField
                        {
                            Title = name,
                            Value = change.NewVal + " " + change.OldVal,
                        };
                   
                        attachment.Fields.Add(oldValue);
                    }

                    attachments.Add(attachment);
                }
            }
            return attachments;

        }




        private string GetChangeDisplayName(uSyncChange change)
        {
            if (change.Path != null)
            {
                return string.Format("{0} [{1}]",
                    change.Path.Substring(change.Path.LastIndexOf('.') + 1),
                    change.Name).Replace("Core", "");
            }
            return change.Name;
        }

        private string GetValueOrBlank(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "(blank)";

            return value;
        }

        private void SendSlackNotification(SlackMessage message)
        {
            var slackClient = new SlackClient(webHook);
            slackClient.Post(message);

        }

    }
}
