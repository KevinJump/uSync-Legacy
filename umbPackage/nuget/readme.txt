            ____                   
      _   _/ ___| _   _ _ __   ___ 
     | | | \___ \| | | | '_ \ / __|
     | |_| |___) | |_| | | | | (__ 
      \__,_|____/ \__, |_| |_|\___|
 -----------------|___/ ----- 3.0 -----

 uSync for Umbraco 7.3+ 
 
 Welcome ~/uSync/Data
 --------------------
 We've changed the location of the uSync files! They are now saved to /uSync/Data by default; if you are upgrading
 you can move your old uSync files into this folder, although we recommend you let uSync do a new export and use
 that instead. The files are compatible, but the new versions are slightly more comprehensive. 
 
 What's new 
 ----------
 In short: Everything, but you probably won't notice. 
 
 We've updated uSync to use all the lovely updates in Umbraco 7.3 and we've seperated out the serialization logic
 from the disk bits, which means there is a whole set of things you can call from code if you want to - but if you just
 want to use uSync as normal that's fine.
 
 The uSync dashboard (in the Developer section), has been given an update too: you can now run update reports to see what is
 actually going to change before you apply any updates. 
 
 Target Framework
 ----------------
 If you've upgraded Umbraco, it's likely that the target framework is still set to 4.5. To
 remove any uSync warnings or errors you should pop this up to at least 4.5.1 in your project properties.
 
 
 
 
