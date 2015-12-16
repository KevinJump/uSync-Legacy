<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="uSyncBackOfficeDashboard.ascx.cs" Inherits="Jumoo.uSync.BackOffice.UI.uSyncBackOfficeDashboard" %>
<style>
    .usync-dash h3, .usync-dash h4 {
        border-bottom: 1px solid #eee;
        padding-bottom: 0.2em;
    }
    .usync-info
    {
        margin: 0 5px;
    }

    .goodwill-licence-banner { font-size: 0.8em; background-color: #f1f1f1; color: #222;}
    .goodwill-licence-banner a { font-weight: bold; color: #aaa; }
</style>
<script type="text/javascript">
    $(document.forms[0]).submit(function () {
        document.getElementById("progressnote").innerHTML
            = "Doing stuff... <small>can take a little while</small>";

        document.getElementById("usyncinprogress").style.visibility = "visible";
        document.getElementById("usyncupdated").style.display = "none";
    });
</script>

<div id="usyncBackOfficeDashboard" class="usync-dash">
    <div class="propertypane">
        <div class="row">
            <div class="span12">
                <h3>uSync.BackOffice
                    <small>
                        [BackOffice: <asp:Label ID="uSyncVersionNumber" runat="server"></asp:Label>]
                        [Core: <asp:Label ID="uSyncCoreVersion" runat="server"></asp:Label>]
                    </small>
                </h3>               
            </div>
        </div>
        <div class="row">            
            <div class="span6">
                <div class="usync-info">
                    <h4>Import / Export</h4>
                    <p>
                        Perform import and exports - these actions will be processed against sync files in the 
                        <asp:Label ID="usyncFolder" runat="server"></asp:Label> folder.
                    </p>
                    <p>
                        <asp:Button runat="server" ID="btnReport" text="Report" CssClass="btn btn-info" OnClick="btnReport_Click"/>
                        <small class="muted">What will change</small>
                    </p>
                    <p>
                        <asp:Button runat="server" ID="btnSyncImport" text="Import" CssClass="btn btn-success" OnClick="btnSyncImport_Click"/>
                        <small class="muted">Import only the changes</small>
                    </p>
                    <hr />
                    <p>
                        <asp:Button runat="server" ID="btnFullImport" text="Full Import" CssClass="btn btn-mini btn-warning" OnClick="btnFullImport_Click" />
                        <small class="muted">Import everything from folder</small>
                    </p>
                    <p>
                        <asp:Button runat="server" ID="btnFullExport" text="Export" CssClass="btn  btn-mini btn-inverse" OnClick="btnFullExport_Click" />
                        <small class="muted">Export current settings to disk</small>
                    </p>
                </div>
            </div>
            <div class="span6">
                <div class="usync-info">
                    <h4>Setup</h4>
                    <p>Choose how uSync behaves on your site</p>
                    <label class="radio">
                        <asp:RadioButton ID="rbAutoSync" runat="server" CssClass="" GroupName="uSyncMode"/> 
                        Auto Sync <small>Run import at startup, Save changes to disk when they occur</small>
                    </label>
                    <label class="radio">
                        <asp:RadioButton ID="rbTarget" runat="server" CssClass="" GroupName="uSyncMode"/> 
                        Sync Target <small>Will run import at startup only</small>
                    </label>
                    <label class="radio">
                        <asp:RadioButton ID="rbManual" runat="server" CssClass="" GroupName="uSyncMode"/> 
                        Manual Sync <small>No automatic imports or saves.</small>
                    </label>
                    <label class="radio">
                        <asp:RadioButton ID="rbOther" runat="server" GroupName="uSyncMode" Enabled="false"/>
                        Other <small>Some other weird setup - see your usyncBackOffice.config</small>
                    </label>
                    <div class="pull-right">
                        <asp:Button ID="btnSaveSettings" runat="server" CssClass="btn btn-default" Text="Save Settings" OnClick="btnSaveSettings_Click" />
                    </div>
                </div>
            </div>
        </div>
        <div class="row">
            <div class="span12">
                <hr />
            </div>
        </div>
            
        <div id="usyncupdated">
            <div class="row">
                <div class="span12">
                    <blockquote>
                        <asp:PlaceHolder ID="uSyncResultPlaceHolder" runat="server" Visible="false">
                            <h3><asp:Label ID="resultHeader" runat="server">Results</asp:Label></h3>
                            <asp:Label ID="resultStatus" runat="server"></asp:Label>
                        </asp:PlaceHolder>
                    </blockquote>
                </div>
                <div class="span12">
                    <asp:Repeater ID="uSyncStatus" runat="server">
                        <HeaderTemplate>
                            <table class="table table-condensed">
                                <thead>
                                    <tr>
                                        <th></th>
                                        <th>Name</th>
                                        <th>Type</th>
                                        <th>Change</th>
                                        <th>Message</th>
                                    </tr>
                                </thead>
                                <tbody>
                        </HeaderTemplate>
                        <ItemTemplate>
                                <tr>
                                    <th><%# ResultIcon( Eval("Success") ) %></th>
                                    <td><%# DataBinder.Eval(Container.DataItem, "Name") %></td>
                                    <td><%# TypeString( Eval("ItemType") ) %></td>
                                    <td><%# DataBinder.Eval(Container.DataItem, "Change") %></td>
                                    <td><%# DataBinder.Eval(Container.DataItem, "Message") %></td>
                                </tr>
                        </ItemTemplate>
                        <FooterTemplate>
                                </tbody>
                            </table>
                        </FooterTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </div>



        <div id="usyncinprogress" style="visibility:hidden;">
            <div class="row">
                <div class="span12">
                    <h3 id="progressnote"></h3>
                    <div class="progress progress-striped active">
                        <div class="bar" style="width: 100%;"></div>
                    </div>
                </div>
            </div>
        </div>

        <asp:Panel runat="server" Visible="false" ID="goodwillLicence">
            <div class="row">
                <div class="span12">
                    <!-- yes removing this code removes the banner - but how much nicer would it be to support usync :) -->
                    <div class="goodwill-licence-banner alert alert-warning" >
                        <i class="icon-info"></i> uSync is unlicenced - 
                            It doesn't stop anything from working, but you can make yourself feel better with a <a href="http://jumoo.uk/usync/licence/" target="_blank">goodwill licence</a>
                    </div>
                </div>
            </div>
        </asp:Panel>

        <asp:Panel ID ="panelTech" Visible="true" runat="server">
        <div class="row">
            <div class="span12">
                <h3>uSync.TechnicalBits</h3>

                <h4>Registred Handlers. (<asp:Label ID="uSyncHandlerCount" runat="server"></asp:Label>)</h4>
                <p><small>The handlers do the hard work, of importing and exporting data, they can be turned off in the 
                    config file
                </small></p>
                <asp:BulletedList runat="server" ID="uSyncHandlers"></asp:BulletedList>
            </div>
        </div>
        </asp:Panel>
    </div>
</div>

