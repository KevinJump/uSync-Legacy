# README #

### Purpose ###
Map datatype for Umbraco V7 

### Why? ###
Wish to give your content editors easy Maps to set real world locations. 
 
### Usage ###
1. Install Terratype framework package via Nuget
   https://www.nuget.org/packages/Terratype/

2. Install the Map Providers you would like to use
   https://www.nuget.org/packages/Terratype.GoogleMapsV3
   
3. Create a new data type based off this the newly added Terratype property Editor. You may need to obtain any API Keys that are necessary 

4. Add this new data type to a document type

5. Create new content based off this document type

### Usage ###

https://github.com/Joniff/Terratype/blob/master/docs/manual.pdf


### Render ###

@using Terratype;

@Html.Terratype(Options, Map, @<text>Label</text>)

 
### Log ###

**1.0.4**

	Fixed error with Null types in assemblies
	Fixed error with map height for IE in Umbraco backend only
	Added content editable Labels to maps


**1.0.3**

	Error checking for providers fixed


**1.0.2**

	Removed reliance on terratype map provider having to be same version as terratype

	
**1.0.1**

	Removed hardcoded /umbraco/ references


**1.0.0**

	Complete rewrite based from AngularGoogleMaps.

### Source code ###

Download the source code, it should work for Visual Studio 2013 & 2015. If you set **Terratype.TestSite** as your **Set as Startup project** this should execute the test Umbraco website, where you can test maps under different scenarios. Once running, surf to http://localhost:60389/umbraco and at the login type **admin** for user and **password** for password.

