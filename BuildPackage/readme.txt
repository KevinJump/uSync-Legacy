Build all the uSync Packages
----------------------------

Each Package has it's own folder with build file, umbraco package.xml and nuspec file.

The build files are all very similar; most of the packages, have a dll, some app_plugin files and config

The Umbraco package for BackOffice also contains the Core Dlls, and the Umbraco Package for Content includes the Mappers DLL.

When versions are updated, make sure you also increase the depencendies in the nuspec files.

