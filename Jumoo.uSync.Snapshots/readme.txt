        ____                     ____                        _           _       
  _   _/ ___| _   _ _ __   ___  / ___| _ __   __ _ _ __  ___| |__   ___ | |_ ___ 
 | | | \___ \| | | | '_ \ / __| \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __/ __|
 | |_| |___) | |_| | | | | (__ _ ___) | | | | (_| | |_) \__ \ | | | (_) | |_\__ \
  \__,_|____/ \__, |_| |_|\___(_)____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|___/
              |___/                               |_|                            

  uSync Snapshots, v1.0.0.740

  uSync Snapshots works with uSync to allow you to create moment in time changesets
  of your umbraco installations.  
  
  normal usync will track changes you make to your umbraco installation and write
  any of those changes to disk, it also has the option to automatically take any
  changes that are on the disk and push them into umbraco. This is great when you
  are developing something, but some people find it a little scary, especially
  when they then start pushing things to live sites. 
  
  Snapshots, allows you to have more control over when uSync does stuff. 
  
  With snapshots you get a new tab [uSync Snapshots] that lets you create and 
  apply snapshots to your uSync Install.
  
  
  The Tab
  ========
                                                              _________________
  Get Started  Examine Management  Xml Data Integrity Report | uSync Snapshots |
  -----------------------------------------------------------                   --
  
  The uSync Snapshot tab lives in the developer section of your umbraco install,
  probibly right at the end after uSync (because you need to install that too)
  
  The settings for uSync.Snapshot are quite simple, 
  
  Current Snapshots (located in ~/uSync/Snapshots)
  ------------------------------------------------
  
  Here you will see a list of snapshots on the disk, Snapshot folders have a 
  very spacific format (date)_(name) (e.g 20160212_093622_SnapshotName) this 
  is so the snapshots can retain an order while on the disk, and so we can 
  work out when they where created. 
  
  Local vs Remote - when you create a snapshot on a umbraco install it will show
  up as 'Local', when you copy a snapshot to an umbraco site it will be marked as
  'Remote' (this is maked the 'uSyncSnapshotAudit' table inside your umbraco site)
  
  Applying snapshots
  ------------------
  
  As a genral rule it makes more sense to apply all snapshots to an umbraco 
  installation then it does to do them one by one. 
  
  As snapshots only contain the changes since the last time they where ran 
  if you apply them individually then you can end up with all sorts of out of 
  sync things happening to your site. 
  
  When you apply all snapshots, the following happens. 
  
  All the snapshots in the snapshot folder (~/uSync/snapshots) are combined in
  to one folder in turn. This has the effect of building one uptodate snapshot
  that contains only the latest version of any one item.
  
  This master snapshot is then applied to the site. changes are reported at the
  bottom of the page
  
  Change Reports
  --------------
  When you run a report or apply a snapshot, details of the changes are listed 
  at the bottom of the screen - each change also comes with a list of what 
  should change when its applied. 
  
  the Change detail report is currently a bit experimental, while it does show
  you what will change, its not always 100% accruate in how the change will be
  made -
  
  it might forexample say a property is going to be deleted and then recreated
  when in fact uSync run will rename the property (this is less destructive).
  
  
  
  
  