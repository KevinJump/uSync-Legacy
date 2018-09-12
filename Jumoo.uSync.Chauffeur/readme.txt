     _____                    _____ _           ___ ___             
 _ _|   __|_ _ ___ ___       |     | |_ ___ _ _|  _|  _|___ _ _ ___ 
| | |__   | | |   |  _| for  |   --|   | .'| | |  _|  _| -_| | |  _|
|___|_____|_  |_|_|___|      |_____|_|_|__,|___|_| |_| |___|___|_|  
--------  |___|  ----------------------------------------------------                                               

Thanks for installing uSync for Chauffeur, the CLI way to use uSync

See the chauffeur documentation for how to use Chauffeur
	https://aaronpowell.github.io/Chauffeur/

For uSync goodness start with: 
	
	umbraco > help uSync 

From Generic commands: 
	
	umbraco > uSync Import

	to run a full uSync import from the uSync/data folder, 
	just like you clicked the button in teh back office.

to specific elements : 
	
	umbraco > uSync domain export .\uSync\myDomains 

	to export your domain settings to the .\uSync\myDomains folder

+-----------------------------------------------------------------+
| if you are using uSync for Chauffer in you CD/CI setup remember |
|                                                                 |
|           Turn of import in the uSync config file !             |
|              (or everything will happen twice)                  |
|                                                                 |
+-----------------------------------------------------------------+