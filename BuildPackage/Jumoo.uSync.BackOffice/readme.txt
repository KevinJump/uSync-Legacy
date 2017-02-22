            ____                   
      _   _/ ___| _   _ _ __   ___ 
     | | | \___ \| | | | '_ \ / __|
     | |_| |___) | |_| | | | | (__ 
      \__,_|____/ \__, |_| |_|\___|
   ---------------|___/ ----- 3.3 ----
 
 uSync for Umbraco 7.4+ 
 
 Upgrading from v2.x
 --------------------
 The default data folder has moved from /usync to /usync/data. You
 can move your files to the new folder or let uSync generate a clean
 export (recommended). After an upgrade you delete all the other
 folders in the /usync folder. 

 Recommended: Perform a new export on upgrades
 ----------------------------------------------
 The files that uSync exports are genrally compatible between versions, 
 but updates usally bring enhanced functionality and perfomance improvements
 that reliy on addtions to the export files. 

 It is recommended that after an update to usync you perform a clean export.
 You can either delete hte usnyc/data folder, or perform an export via the
 dashboad.

 Deployment targets
 -------------------
 This uSync package includes deployment targets so the usync folder is 
 included during anywebsite publish. You do not need to include the 
 usync folder in your solution to have the files make up part of a 
 deployment. 
 
 ---------------
 Documentation : http://usync.readthedocs.io/
 Source code   : https://github.com/KevinJump/uSync


 
