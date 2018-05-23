## Handlers

Handlers are the things that do all the hard work in uSync. A Handler manages the import,
export and saving of items to and from Umbraco. 

By default, there are handlers for all the things in Umbraco:

```xml
<Handlers Group="default" EnableMissing="true">
    <HandlerConfig Name="uSync: DataTypeHandler" Enabled="true" />
    <HandlerConfig Name="uSync: TemplateHandler" Enabled="true" />
    <HandlerConfig Name="uSync: ContentTypeHandler" Enabled="true" />
    <HandlerConfig Name="uSync: MediaTypeHandler" Enabled="true" />
    <HandlerConfig Name="uSync: LanguageHandler" Enabled="true" />
    <HandlerConfig Name="uSync: DictionaryHandler" Enabled="true" />
    <HandlerConfig Name="uSync: MacroHandler" Enabled="true" />
    <HandlerConfig Name="uSync: DataTypeMappingHandler" Enabled="true" />
    <HandlerConfig Name="uSync: MemberTypeHandler" Enabled="false" />
</Handlers>
```

> Note: Handlers are enabled by default so if a handler
exists on disk but isn't in the config it will be enabled by default

### Enabling Handlers
Turning a handler on or off is as simple as setting ```Enabled="true"``` or ```Enabled="false"``` in the config.

### Enabling for actions
In uSync 3.2.2. you can now enable or disable handlers for each of the actions they
perform.

By default, handlers manage **import**, **export** and **save** events for each type of items.

You can now turn each of these on/off individually using the ```Actions=""``` attribute.

```Actions="All"``` is the default, meaning a handler does all three things.

Possible settings are: any combination of ```All,Import,Export,Events``` 

for example, if you want to have the DictionaryHandler only manage exports

```xml
<HandlerConfig Name="uSync: DictionaryHandler" Enabled="true" Actions="Export"/>
```

## Handler Groups
Handlers can also be grouped. with the Group="" setting. 

> Note: at this time uSync only processes the ```Default``` group - this setting is for future enhancements

## Custom Handler Actions
Some handlers have custom actions - that allow you to change some of the behavior of how the handler works: for these handlers you can control the values through custom key values :

So for example the content handler has a number of options: 

```xml
<HandlerConfig Name="uSync: ContentHandler" Enabled="true">
  <Setting Key="useshortidnames" Value="true" />
  <Setting Key="ignore" Value="home\blog\example" />
  <Setting Key="levelpath" Value="true" />
  <Setting Key="deleteactions" Value="true" />
  <Setting Key="rulesonexport" Value="true" />
  <Setting Key="include" Value="home\blog,home\products\unicorn" />
</HandlerConfig>
```

## Content / Media Handler Options

The Content and Media Handlers have a number of extra settings : 

**useshortidnames**
By default uSync will store the content or media in folders based on the name of the content or media item, for deep trees or long names you can hit an issue with 'MAX_FILE_PATH'. by setting useshortidnames then usync will store content and / or media based on the id, giving a shorter name.  

**ignore / include paths** 
using the ignore or include settings you can selectively sync content

**delete actions**
by default when you delete content, uSync doesn't create a delete action - that is it doesn't put the delete in the action file - action files are used to replay delete and renames on target sites - if you turn this on then content will also get content deleted on other servers when you delete it on you site. 

**rules on export**
by default the ignore and include rules are only processed duing import - for export everything is still written to disk - if you don't want the items to also be written to disk you should turn this setting on. 
