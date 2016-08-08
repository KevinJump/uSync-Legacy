Edit \BuildPackage\Package.build.xml and change BasePackageVersion to the version you wish to create (must be in the format 1.2.3.4) 

The last digit is the build number and not used for the product version.

Execute \BuildPackage\build.bat

Check \BuildPackage\Package to find both the Umbraco and NuGet package files ready to roll

If you ever need to change the Umbraco.Core dependency (currently v7.4.0), you will need to update the version in both \BuildPackage\Package.build.xml & \BuildPackage\package.nuspec