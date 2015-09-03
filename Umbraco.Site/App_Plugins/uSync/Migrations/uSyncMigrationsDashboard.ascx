<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="uSyncMigrationsDashboard.ascx.cs" Inherits="Jumoo.uSync.Migrations.uSyncMigrationsDashboard" %>
<script type="text/javascript">
    $(document.forms[0]).submit(function () {
        document.getElementById("progressnote").innerHTML
            = "Doing stuff... <small>can take a little while</small>";

        document.getElementById("usyncinprogress").style.visibility = "visible";
        document.getElementById("usyncupdated").style.display = "none";
    });
</script>

<div class="row">
    <div class="span12">
        <h2>Snapshots</h2>
        <p>
            Snapshots are place in time version of you're umbraco install. each snapshot captures changes since the 
            previous one, you can then play these back on you're umbraco site, to make only the changes to need to 
            make.
        </p>
    </div>
</div>

<div class="row">
    <div class="span6">
        <form class="form-horizontal">
          <div class="control-group">
            <asp:Label AssociatedControlID="txtSnapshotName" Text="Name" runat="server" CssClass="control-label"></asp:Label>
            <div class="controls">
                <asp:TextBox ID="txtSnapshotName" runat="server"></asp:TextBox>
            </div>
          </div>
          <div class="control-group">
            <asp:Button ID="btnSnapshot" runat="server" OnClick="btnSnapshot_Click" Text="Create Snapshot" CssClass="btn btn-info" />
          </div>
        </form>
    </div>
    <div class="span6">
        <h4>Apply</h4>
        You can combine all the snapshots below, and apply their changes to this umbraco installation: 
        <asp:Button ID="btnApplySnapshot" runat="server" Text="Apply Snapshots" CssClass="btn btn-default" OnClick="btnApplySnapshot_Click" />
    </div>
</div>

<div class="row">
    <div class="span12">
        <asp:Label ID="lbStatus" runat="server"></asp:Label>
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


    <asp:Repeater ID="snapshotList" runat="server">
        <HeaderTemplate>
            <table class="table table-condensed">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>No of Items</th>
                        <th>Time</th>
                    </tr>
                </thead>
        </HeaderTemplate>
        <ItemTemplate>
            <tr>
                <td><%# DataBinder.Eval(Container.DataItem, "Name") %></td>
                <td><%# DataBinder.Eval(Container.DataItem, "FileCount") %></td>
                <td><%# DataBinder.Eval(Container.DataItem, "Time") %></td>
            </tr>
        </ItemTemplate>
        <FooterTemplate>
            </table>
        </FooterTemplate>
    </asp:Repeater>
