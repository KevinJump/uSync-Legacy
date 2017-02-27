# uSync Content Edition

By default, uSync concentrates on the developer and setting elements of Umbraco,
but **uSync.ContentEdition** expands that to also include Content and Media

```bash
PM>Install-Package uSync.ContentEdition
``` 

At its heart uSync.ContentEdition is really just two new uSync Handlers, that get added to the 
core uSync.BackOffice tool. Once installed these two Handlers are called just like all the 
other handlers but they manage content and media items. 

# Content 
Content is stored in the ```uSync/data/content``` folder, and is a file based representation
of the content stored inside Umbraco.

# Media 
By default, uSync stores the media settings, in the ```uSync/data/media```. As of later versions (3.2+)
it does not store a copy of the media items anymore. Since Umbraco 7.4 it has become much easier to 
just copy your media folder between installations, and let uSync only deal with the media config. 


## Mappers
The biggest trick to moving media and content between Umbraco installations is mapping the internal
IDs between installations. By default, Umbraco uses integer based IDs when linking items. The value 
of these IDs can change between Umbraco instances - so when content is moved they need to be re-mapped.

For the most part we can now do this by converting them to the Umbraco Key (GUID) values, and making
sure, we set these keys the same on all instances - the inclusion of these Key values in Umbraco
(Increasingly since v7.3) has made that element of mapping much more successful. 

the second part of mapping is finding the Ids to map. Content is often made up of many different
properties all with their own special data types that store the content in their own special way.
For each of these types uSync needs to know 1) that they store IDs and 2) the format they do it in.

Content Mappers handle this for uSync.ContentEdition. and out of the box uSync can map several types:

**Core Mappers**

* Content Picker
* Rich Text Editor
* MultiNode Tree Picker
* Related Links
* Multiple Media Picker
* Media Picker

* Umbraco Radio Button List
* Dropdown List 
* Dropdown ListPublishing keys
* Dropdown List Multiple Publishing keys
* Dropdown Multiple
* CheckBoxList

* Umbraco Grid.
* Grid RTE
* Grid Media Picker

We also include third party mappers

**Third-party Mappers**

* Archetype
* Nested Content
* Vorto
* DocTypeGridEditor
* LeBlender
* MayFly.MrNPicker
* CTH. Extended Media Picker

#### Roll your own mappers
You can also write your own Content Mappers, you need to implement the
[IContentMapper](https://github.com/KevinJump/uSync/blob/Dev-v7_4/Jumoo.uSync.Core/Mappers/IContentMapper.cs) interface 
