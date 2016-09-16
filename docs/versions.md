# uSync Versions

uSync uses quite a lot of the underling Api of Umbraco it is subseptible to changes
that otherwise make little diffrence to the user, but it does mean there are diffrent Versions
of uSync depending on what version of Umbraco you are using:  

| Umbraco Version    | uSync Version         
|--------------------|----------------------
| Umbraco 7.4+       | uSync 3.x.x.740      
| Umbraco 7.3.x      | uSync 3.0.x
| Umbraco 7.0- 7.2.x | uSync 2.5.x
| Umbrac0 6.2.5+     | uSync 2.2.6
| Umbraco 6.2 - 6.24 | uSync 2.2.5
| Umbraco 6.1.x      | uSync 1.6
| Umbraco 6.0x       | *No working version*
| Umbraco 4.10+      | uSync 1.3.3 

You should install the latest version for your version of Umbraco.

# Upgrades 
When you Upgrade Umbraco it is also recommended that you upgrade uSync. 

once you have upgraded Umbraco and uSync you should remove your uSync 
folder and let uSync create a new sync. 

While there is compatability between versions, the matching and change 
detection is much more reliable and quicker when the uSync files are 
from the latest versions of uSync. 