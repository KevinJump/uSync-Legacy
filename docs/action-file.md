# uSync Action File

The usync action file 

```bash
uSyncActions.config
```

Is placed in the root of the uSync folder when ever a delete (or certain type of rename)
is detected by uSync.

The action file is used on imports to make sure we delete things that are no longer
needed on the umbraco site.

# Full Exports 

When you perform a full export everything is cleaned from the disk, and recreated as
an export - prior to v3.2.3 exports would wipe the uSyncActions.config file as part
of this process, but we found this could cause problems as the target systems then
didn't know about any deletes that might have happened previously. 

in v3.2.3 we have introduced an option to keep or delete the action file on a full
export.

It is now recommended that you keep the action file on a full export, as this will
at least make sure that all downstream installs of umbraco will at least know of 
any deletes. 

You will only want to delete the action file once you are sure that all possible 
orphaned items have been removed from all of your umbraco instanced.  
