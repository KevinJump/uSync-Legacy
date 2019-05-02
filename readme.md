# uSync 

![Build Status](https://jumoo.visualstudio.com/_apis/public/build/definitions/e5bc8d11-6d47-4620-9e6e-dd8199b2843e/6/badge)

**Documentation : http://usync.readthedocs.io/**

uSync is a synchronization tool for the Umbraco CMS. It serializes the database config and data
within an Umbraco site, and reads and writes it to disk as a collection of XML files. 

uSync can be used as part of your source control, continuous deployment or site synchronisation plans. 

Out of the box, uSync reads and writes all the database elements to and from the `usync/data` folder.

It will save:

* Document Types
* DataTypes
* MediaTypes
* Templates
* Macros
* Languages
* Dictionary Items
* MemberTypes

You can use **uSync.ContentEdition** to manage content and media if you also want to write them to disk.

## Compatibility

| Umbraco Version | uSync Version |
| --------------- |:-------------:|
| >= 8.x.x.       | [uSync8](https://github.com/KevinJump/uSync8) |
| >= 7.6.x        | 4.x.x         |
| <= 7.5.x        | 3.x.x         |


## The basics workings of uSync

The main elements of uSync are the Serializers and Handlers:

### Serializers
Serializers manage the transition between Umbraco and the XML that uSync uses,
they control how the configuration is written in and out, manage things like 
internal IDs so your settings can move between Umbraco installations. 

Serializers do the heavy lifting of uSync, and live in the **uSync.Core** package, 
you can use this package to programmatically import and export data to Umbraco. 

### Handlers
Handlers manage the storing of the XML and passing to the serializers. By default
this means reading and writing the XML to disk from the uSync folder. Handlers 
are the entry point for imports and exports, and they capture the save and delete
events inside of Umbraco so that things are saved to disk when you make changes via
the back office. 

You can add your own handlers by implementing the `ISyncHandler` interface.

### Mappers 
Mappers help with the content and media serialization process, they 
allow uSync to know how to find and map internal IDs from within properties on your 
content.

Within Umbraco when you use links, and things like content pickers store the internal
ID to link the property to the correct content. Between Umbraco installations these
IDs can change so uSync needs to find them and map them to something more global (often GUIDs).

Mappers allow uSync to do this. as of v3.1 **uSync.ContentEdition** includes mappers for: 

* Built in editors *(RTE, Multi-Node Tree Picker, Content Picker, etc)*
* The Grid
* Archetype
* Nested Content
* Vorto
* LeBlender
* DocTypeGridEditor

You can roll your own mappers, by implementing the `IContentMapper` interface and putting 
settings in `uSyncCore.Config`.

# Packages

there are a number of uSync packages, that make up the uSync suite, most of the time
you don't need to worry about them, but they can be used in different ways to give you
more control over how your data is handled.

## uSync (BackOffice)

This is the main uSync package, it reads and writes the Umbraco elements to disk. (uSync/data folder), 
It contains the Handlers and the main dashboard for uSync. 

## uSync.Core

Core contains the serializers controlling the access to the Umbraco system. Core allows you 
to pass and consume the XML representations of your Umbraco site, it doesn't write anything to disk.

## uSync.ContentEdition

Content Edition is a set of additional handlers, to control Content and Media. 

## uSync.Snapshots

Snapshots is a different approach to saving the Umbraco settings. The standard uSync saves all changes
as they are made into the standard uSync folder. 

Snapshots allows you to capture a series of changes into a single snapshot, which can then be moved around
either as part of a wider collection or as a single change. 


