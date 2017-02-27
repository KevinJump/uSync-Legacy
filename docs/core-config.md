# uSync.Core

uSync is split into two elements Core and BackOffice. Core actually does
the serialization/de-serialization of items to and from Umbraco. 

The Core doesn't do anything on the disk or with events - it has been 
developed this way so that the Core can be used for other ways of Syncing
things, for example in your own code or via web services or the command line.

## uSyncCore.Config

The uSyncCore.Config file controls the settings for the serialization/de-serialization of
things from Umbraco.

## Media

Almost everything uSync.Core does is in memory, except for when you are syncing Media items 
then uSync can export those values to disk.

Since Umbraco 7.4 the way the ID of the media folder was created has moved from the database to 
disk (at startup Umbraco now just looks for the highest number folder and carries on from there.)

Because of this it is now recommended that you don't get uSync to copy your media folder - rather
you just copy the actual media folder between sites. **uSync will still sync the media settings,
and you will need have uSync keep these settings in sync.** but you don't want uSync also 
creating copies of all your media and giving you a massive disk headache.

If you do want uSync to manage the media outside of the media folder use the following settings.     

```xml
<MoveMedia>false</MoveMedia>
<MediaStorageFolder>~/uSync/MediaFiles/</MediaStorageFolder>
```

## Mappings
Inside Umbraco a lot of information is stored against the item ID which is an Int value, that can
change between installations. 

When you move configuration or items between Umbraco installations, we need to map these ID values, so they still make sense on the other side. The mappings section of the config sets up these mappings inside datatypes/doctypes. 

An example of this is the MultiNodeTreePicker (MNTP) 

When you select a root node for your MNTP in Umbraco the value of the content node is stored as an Integer 
inside Umbraco.

The following config tells uSyncCore how to map this value.  

```xml
    <uSyncValueMapperSettings>
      <DataTypeId>Umbraco.MultiNodeTreePicker</DataTypeId>
      <MappingType>content,media</MappingType>
      <ValueStorageType>json</ValueStorageType>
      <ValueAlias>startNode</ValueAlias>
      <PropertyName>id</PropertyName>
    </uSyncValueMapperSettings>
```

## Content Mappings
Content Mappings are just like mapping except they work inside Content and Media items. 

The storage of IDs inside Media and Content is way more common, and happens every time you put a link 
in your content, or pick an image or a node. 

The Content mappers handle these mappings, and uSync comes with all the mappings needed for the common
datatypes. 

uSync Content Edition - also includes mappings for many third-party packages including: 

* Archetype
* Vorto
* DocTypeGrid
* NestedContent
* LeBelnder

These mappings work on standard content, in nested content and in the grid.

Content Mappers are reasonably simple to implement if you know the structure of the content inside 
Umbraco. and the best way to see how they are implemented is to look at the code for
[Jumoo.uSync.ContentMappers](https://github.com/KevinJump/uSync/tree/Dev-v7_4/Jumoo.uSync.ContentMappers)  
  
