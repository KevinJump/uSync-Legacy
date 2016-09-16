# Getting Started

At the basic level, uSync 'just works' out of the box. 


## First Run
The first time you run your umbraco site after installing uSync. uSync will
perform a export of your settings from the site. 

**uSync will export:** 

- Document Types
- Data Types
- Media Types
- Templates
- Languages
- Dictionary Items

the export will be put in the ```uSync/data``` folder within your umbraco site.

## Export On Save / Delete
Once usync is running it will listen for events related to saving or deleting 
of items within umbraco. 

Every time you save and item uSync will write out an export file for the item
in the uSync/data folder. 

When you delete an item - a entry is made into the ```uSyncActions.config``` file
that tracks deletes. this file is used during an import, to process the delete
on other umbraco instances.

## Import On Startup
by default uSync will run an import everytime your umbraco site starts (or the
application pool refreshes). 

During an import uSync looks at the files in teh uSync/data folder and compares
them with the items within the umbraco site. 

If there are any diffrences then uSync applies the values from disk to Umbraco, 
so this way things are kept in sync. 

## uSync Dashboard

The uSync dashboard gives you a place to see what uSync is doing, run manual
imports or exports and check settings. 

![uSync Dashboard](dashboard.png)

The dashboard is located in the developer section of Umbraco. 
