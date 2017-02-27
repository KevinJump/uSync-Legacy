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
