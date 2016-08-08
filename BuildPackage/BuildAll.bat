@ECHO. 
@ECHO Build All uSync Packages.
@ECHO. 


Call nuget.exe restore ..\uSync.7.3.sln
Call BuildOne.bat "jumoo.usync.core/Core.Package.build.xml"
Call BuildOne.bat "jumoo.usync.BackOffice/BackOffice.Package.build.xml"
Call BuildOne.bat "jumoo.uSync.Content/Content.Package.build.xml" 
Call BuildOne.bat "jumoo.uSync.ContentMappers/ContentMappers.Package.build.xml" 
@rem Call BuildOne.bat "jumoo.uSync.Snapshots/Snapshots.Package.Build.xml"

