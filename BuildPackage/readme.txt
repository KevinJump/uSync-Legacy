Build all the uSync Packages
----------------------------

Each package has its own folder with build file, Umbraco package.xml and nuspec file.

The build files are all very similar; most of the packages, have a DLL, some App_Plugin files and config.

The Umbraco package for BackOffice also contains the Core DLLs, and the Umbraco Package for Content includes the Mappers DLL.

When versions are updated, make sure you also increase the dependencies in the nuspec files.
