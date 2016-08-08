            ____                   
      _   _/ ___| _   _ _ __   ___ 
     | | | \___ \| | | | '_ \ / __|
     | |_| |___) | |_| | | | | (__ 
      \__,_|____/ \__, |_| |_|\___|
 -----------------|___/ ----- 3.1 -----

 uSync for Umbraco 7.4+ 
 
 Welcome ~/uSync/Data
 --------------------
 We've moved where the uSync files are stored, They are now saved to /uSync/Data by default, if you are upgrading
 you can move you're old uSync files into this folder, although we recommend you let uSync do a new export and use
 that. the files are compatible, but the new versions are slightly more comprehensive. 
 
 What's new 
 ----------
 In short Everything, but you probibly won't notice. 
 
 We've updated uSync to use all the lovely updates in Umbraco 7.3 and we've seperated out the serialization logic
 from the disk bits, this means their is a whole set of things you can call from code if you want, but if you just
 want to use uSync as normal that's fine.
 
 The dashboard (in the developer), has been given an update too, you can now run update reports to see what is
 going to actually change before you apply any updates. 
 
 Target Framework
 ----------------
 if you've upgraded you're version of umbraco, it's likely that the target framework is still set to 4.5, to
 remove anywarnings, you should pop this up to at least 4.5.1 in you're projects properties.
 
 
 
 