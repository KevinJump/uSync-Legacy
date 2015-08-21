<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="uSyncMigrationsDashboard.ascx.cs" Inherits="Jumoo.uSync.Migrations.uSyncMigrationsDashboard" %>

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

<form class="form-horizontal">
  <div class="control-group">
    <asp:Label AssociatedControlID="txtSnapshotName" Text="Name" runat="server" CssClass="control-label"></asp:Label>
    <div class="controls">
        <asp:TextBox ID="txtSnapshotName" runat="server"></asp:TextBox>
    </div>
  </div>
  <div class="control-group">
    <asp:Button ID="btnSnapshot" runat="server" OnClick="btnSnapshot_Click" Text="Create Snapshot" CssClass="btn btn-default" />
  </div>

   <asp:BulletedList ID="snapshotList" runat="server"></asp:BulletedList>
</form>