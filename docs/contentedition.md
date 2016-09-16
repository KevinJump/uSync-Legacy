# uSync Content Edition

By default uSync concentrates on the developer and setting elements of Umbraco,
but **uSync.ContentEdition** expands that to also include Content and Media 

At its hart uSync.ContentEdition is really just two new uSync Handlers, that get added to the 
core uSync.BackOffice tool. Once installed these two Handlers all called just like all the 
other handlers but they manage content and media items. 

# Content 
Content is stored in the ```uSync/data/content``` folder, and is a file based representation
of the content stored inside Umbraco.

# Media 
By default uSync stores the media settings, in the ```uSync/data/media``` as of later versions (3.2+)
it does not also store a copy of the media items. Since Umbraco 7.4 it has become much easier to 
just copy your media folder between installations, and let uSync just deal with the media config. 


## Mappers
The biggest trick to moving media and content between umbraco installations is mapping the internal
ids between installations. By default Umbraco uses Interger based Ids when linking items. The value 
of these ids can change between umbraco instances - so when content is moved they need to be mapped.

For the most part we can now do this by converting them to the umbraco Key (GUID) values, and making
sure we set thesse keys the same on all instances - the inclusion of these Key values in Umbraco
(Increasingly since v7.3) has made that element of mapping much more successfull. 

the second part of mapping is finding the Ids to map. Content is often made up of many diffrent
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
* Dropdwon Multiple
* CheckBoxList

* Umbraco Grid.
* Grid RTE
* Grid Media Picker

We also include third party mappers

**Thirdpart Mappers**

* Archeype
* Nested Content
* Vorto
* DocTypeGridEditor
* LeBlender
* MayFly.MrNPicker
* CTH. Extended Media Picker

#### Roll your own mappers
You can also write your own Content Mappers, you need to impliment the
[IContentMapper](https://github.com/KevinJump/uSync/blob/Dev-v7_4/Jumoo.uSync.Core/Mappers/IContentMapper.cs) interface 
