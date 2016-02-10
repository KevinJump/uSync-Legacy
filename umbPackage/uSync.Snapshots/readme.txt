          ____                   
    _   _/ ___| _   _ _ __   ___ 
   | | | \___ \| | | | '_ \ / __|
   | |_| |___) | |_| | | | | (__ 
    \__,_|____/ \__, |_| |_|\___|
  --------------|___/ -- Snapshots --

  uSync.Snapshots for Umbraco 7.4+
  
  Snapshots lets you choose when to track changes to your Umbraco site
    
 Playing nicely with uSync
 -------------------------
  uSync.Snapshots works alongside uSync(backoffice & content), so you can
  run both of them side by side. uSync puts its changes in /usync/data 
  while snapshots uses /usync/snapshots
  
  However if you are using snapshots, then you probibly don't need normal
  usync tracking changes and saving them to disk, you should set all the 
  basic settings of uSyncBackoffice to false, you can change these
  settings in usyncbackoffice.config.
  
  Something like this will turn 'normal' usync off.
  
	<Import>false</Import> 
	<ExportAtStartup>false</ExportAtStartup>
	<ExportOnSave>false</ExportOnSave> 

  Happy snapshotting. 
  