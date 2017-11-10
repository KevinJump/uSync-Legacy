            ____                   
      _   _/ ___| _   _ _ __   ___ 
     | | | \___ \| | | | '_ \ / __|
     | |_| |___) | |_| | | | | (__ 
      \__,_|____/ \__, |_| |_|\___|
   ---------------|___/ ----- 4.x ----
 
 uSync for Umbraco 7.4+ 
 
 Upgrading from v2.x
 --------------------
 The default data folder has moved from /usync to /usync/data. You
 can move your files to the new folder or let uSync generate a clean
 export (recommended). After an upgrade you delete all the other
 folders in the /usync folder. 

 Recommended: Perform a new export on upgrades
 ----------------------------------------------
 The files that uSync exports are generally compatible between versions, 
 but updates usually bring enhanced functionality and performance improvements
 that rely on additions to the export files. 

 It is recommended that after an update to usync you perform a clean export.
 You can either delete the usync/data folder, or perform an export via the
 dashboard.

 Deployment targets
 -------------------
 This uSync package includes deployment targets so the usync folder is 
 included during any website publish. You do not need to include the 
 usync folder in your solution to have the files make up part of a 
 deployment. 
 
 ---------------
 Documentation : http://usync.readthedocs.io/
 Source code   : https://github.com/KevinJump/uSync
