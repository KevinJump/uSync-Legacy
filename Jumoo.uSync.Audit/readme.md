Umbraco Developer Audit
-

Audit logs for all the back office developery stuff - Answeing the age old question *"just who did change that property on that doctype the other day?"* 

Tracks changes between what has just been saved and a cached version of what was there before. 

Handlers can listen for the changed event and then do things with the changes. so far there we have :

* Disk Audit - saves ever change to disk in a big folder structure
* DB Audit - puts the changes in some database tables and lets you look at them via a dashboard
* Slack Audit - posts the changes to a slack channel where everyone can see everything you are doing ever! 

*Audit uses uSync core for the serialization and comparison of changes but it doesn't require that you be using usync to work - this is just an auditing thing, so doesn't get involved with all the syncing stuff.*