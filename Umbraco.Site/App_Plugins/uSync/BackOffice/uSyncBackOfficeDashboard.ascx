<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="uSyncBackOfficeDashboard.ascx.cs" Inherits="Jumoo.uSync.BackOffice.UI.uSyncBackOfficeDashboard" %>
<style>
    .setting-checkbox input {
        float: left;
        margin-left: 20px;
        margin-right: 20px;
    }
</style>

<div id="usyncBackOfficeDashboard">
    <div class="propertypane">
        <div class="row">
            <div class="span12">
                <h3>uSync.BackOffice</h3>
                <p>
                    uSync Backoffice, syncs all the bits of umbraco in settings and developer that
                    are in the database, everything will appear in <strong><asp:Label ID="uSyncFolder2" runat="server"></asp:Label></strong>
                </p>
            </div>
        </div>
        <div class="row">
            <div class="span6">
                <h3>Import</h3>
                <p>
                    Run a full uSync Import, this will take everything in the <asp:Label ID="usyncFolder" runat="server"></asp:Label> folder, and
                    import it into this version of uSync.
                </p>
                <asp:Button runat="server" ID="btnFullImport" text="Import" CssClass="btn btn-success" OnClick="btnFullImport_Click" />
            </div>

            <div class="span6">
                <h3>Export</h3>
                <p>
                    Run a full uSync Export, this will wipe the <asp:Label ID="usyncFolder1" runat="server"></asp:Label> folder, and
                    write out everything in you're umbraco install to disk
                </p>
                <asp:Button runat="server" ID="btnFullExport" text="Export" CssClass="btn btn-warning" OnClick="btnFullExport_Click" />
            </div>
        </div>
        <div class="row">
            <div class="span12">
                <p>
                    <asp:Label ID="uSyncResults" runat="server"></asp:Label>
                </p>
                <asp:Repeater ID="uSyncStatus" runat="server">
                    <HeaderTemplate>
                        <table class="table table-condensed">
                            <thead>
                                <tr>
                                    <th>Name</th>
                                    <th>Change</th>
                                    <th>Result</th>
                                    <th>Message</th>
                                </tr>
                            </thead>
                    </HeaderTemplate>
                    <ItemTemplate>
                            <tr>
                                <td><%# DataBinder.Eval(Container.DataItem, "Name") %></td>
                                <td><%# DataBinder.Eval(Container.DataItem, "ItemType") %></td>
                                <td><%# DataBinder.Eval(Container.DataItem, "Change") %></td>
                                <td><%# DataBinder.Eval(Container.DataItem, "Success") %></td>
                                <td><%# DataBinder.Eval(Container.DataItem, "Message") %></td>
                            </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </div>
        </div>

        <div class="row">
            <div class="span12">
                 <hr />
                <h3>uSync.TechnicalBits</h3>
            </div>
        </div>

        <div class="row">
            <div class="span6">
                <h4>Settings</h4>
                <ul class="unstyled">
                    <li><asp:CheckBox ID="chkImport" runat="server" Text="Import on startup" CssClass="setting-checkbox" /></li>
                    <li><asp:CheckBox ID="chkExport" runat="server" Text="Export on startup" CssClass="setting-checkbox" /></li>
                    <li><asp:CheckBox ID="chkEvents" runat="server" Text="Write on saves" CssClass="setting-checkbox" /></li>
                    <li><asp:CheckBox ID="chkFiles" runat="server" Text="Watch folder for changes, and import" CssClass="setting-checkbox" /></li>
                </ul>

                <asp:Button ID="btnSaveSettings" runat="server" CssClass="btn btn-default" Text="Save Settings" OnClick="btnSaveSettings_Click" />
                <p></p>
            </div>

            <div class="span6">
                <h4>uSync Info</h4>
                <small>uSync.BackOffice: <asp:Label ID="uSyncVersionNumber" runat="server"></asp:Label></small> <br />
                <small>uSync.Core: <asp:Label ID="uSyncCoreVersion" runat="server"></asp:Label></small>
            </div>
        </div>
        <div class="row">

            <div class="span6">
                <h4>Other Settings
                    <small>from uSyncBackOffice.config</small>
                </h4>

                <asp:BulletedList runat="server" ID="uSyncOtherSettings" CssClass="unstyled">

                </asp:BulletedList>
            </div>
            <div class="span6">
                <h4>Registred Handlers. (<asp:Label ID="uSyncHandlerCount" runat="server"></asp:Label>)</h4>
                <p><small>The handlers do the hard work, of importing and exporting data, they can be turned off in the 
                    config file
                </small></p>
                <asp:BulletedList runat="server" ID="uSyncHandlers" CssClass="unstyled"></asp:BulletedList>
            </div>
        </div>



    </div>
</div>