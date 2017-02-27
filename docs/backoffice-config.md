# uSyncBackOffice.Config

The ```uSyncBackOffice.Config``` file controls the basic behaviour of uSync


## Core Settings

### Folder ```<Folder>~/uSync/data</Folder>```

Defines where uSync should store its files

### Import ```<Import>true</Import>```  

Turns import at startup on or off. If this is ```false``` uSync will not import when
the site starts

### ExportAtStartup ```<ExportAtStartup>false</ExportAtStartup>```

Tells uSync to run an export when the site starts, usually ```false```. 

### ExportOnSave ```<ExportOnSave>true</ExportOnSave>```

Export when you save (or delete) an item within Umbraco. 

With this set to ```true``` - everything on the disk will automatically stay in sync with 
the database.

### Watch For Changes ```<WatchForFileChanges>false</WatchForFileChanges>```

Have uSync watch the disk for any changes in files in the uSync folder.

If you copy or save new .config files into the uSync folder - then uSync will 
run an import against the folder, to get any new changes.    

### Archiving
```xml 
  <ArchiveVersions>false</ArchiveVersions>
  <ArchiveFolder>~/uSync/Archive/</ArchiveFolder>
  <MaxArchiveVersionCount>0</MaxArchiveVersionCount>
```
Usually when you make a change, uSync saves that change over the top of any existing
ones. This is fine as long as you are using source control to manage your site, but if you
want you can get uSync to create a backup of the existing files before it saves.

### Don't Throw Errors ```<DontThrowErrors>false</DontThrowErrors>```
Sometimes things go wrong and when they do uSync usually notices them, but 
occasionally something can go really wrong and uSync throws an error and you
get a YSOD. 

This can be really bad on a live site, because on startup uSync will throw a YSOD
and your site will not load.

To avoid this, you can have uSync catch that last exception, and not throw the YSOD.
This means you get your site, but it also means you won't see if uSync has gone wrong
unless you go looking in the logs.  

## Example Config 

```xml
<?xml version="1.0" encoding="utf-8" ?> 
<uSyncBackOfficeSettings>
  
  <!-- uSync 3.0. Settings file, -->
  
  <!-- uSync folder -->
  <Folder>~/uSync/data/</Folder>

  <!-- run import at startup -->
  <Import>true</Import> 
  
  <!-- export everything to disk at startup -->  
  <ExportAtStartup>false</ExportAtStartup>
    
  <!-- when a user saves something, write it to disk -->  
  <ExportOnSave>true</ExportOnSave> 

  <!-- watch the usync folder, and if something changes, import it-->  
  <WatchForFileChanges>false</WatchForFileChanges> 
  
  <!-- create an archive, when an item is a save, 
      if you're using source control, you probably don't want this -->
  <ArchiveVersions>false</ArchiveVersions>
  <ArchiveFolder>~/uSync/Archive/</ArchiveFolder>
  <MaxArchiveVersionCount>0</MaxArchiveVersionCount>

  <!-- Backups, create backups before doing the import, this will
        help, when rollback is implemented -->
  <BackupFolder>~/uSync/Backup/</BackupFolder>
  
  <!-- for a live site - you want don't throw errors = true, 
    then the site won't be affected should usync do something bad -->
  <DontThrowErrors>false</DontThrowErrors>
  
  <!-- turn individual elements on or off 
    if a handler is registered but not listed then it is by 
    default on - you have to add it to the list, to turn it off.
  -->
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

  <!-- 
    Handler groups: allow you to specify groups you want to import / export 
    
    Good if you have multiple targets, and you don't want to do everything 
    all the time. 
    
    These groups arn't avalible via the dashboard (yet) but can be called 
    via the API
  -->
  <!-- 
  <Handlers Group="snapshot" EnableMissing="False"> 
    <HandlerConfig Name="uSync: DataTypeHandler" Enabled="true" />
    <HandlerConfig Name="uSync: TemplateHandler" Enabled="true" />
    <HandlerConfig Name="uSync: ContentTypeHandler" Enabled="true" />
  </Handlers>
  -->
</uSyncBackOfficeSettings>
```