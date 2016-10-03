# uSync Project

uSync is actually made up of a number of projects, each one doing it's own element of the 
wider syncing process.

## uSync.Core
Core does all the serialization/deserizliation of elements to and from Umbraco. 

Within the Core all the main mapping, and comparisons are performed - making sure that 
items exported from umbraco can go into another installation and have things like
links and content node pointers maintained. 

## uSync.BackOffice

BackOffice is what most people think of as uSync. the BackOffice element handles the 
files on the disk, starts the imports and exports, passing each file to the Core 

back office also manages the save and delete events - to capture changes as they are
made inside umbraco. 

## uSync.ContentEdition

ContentEdtion is the addition of two new Mappers to uSync.BackOffice. one for Content 
and one for Media. 

Content Edition also imports uSync.ContentMappers which has additional Content Mappers
that help with the translation of internal IDs between umbraco sites.

## uSync.Snapshots 

Snapshots is a slightly diffrent way of working with uSync, instead of having uSync save
all the changes all the time. Snapshots lets you create single point in time changesets 
of what is happing inside Umbraco.  

## uSync.Chauffeur

uSync.Chauffeur is a set of commands that can be added to Chauffeur, so you can run
uSync commands from the command line. 