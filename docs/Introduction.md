# Introduction

WitSync is a simple command line tool that can copy TFS Work Item data from a source to a target project. It works in phases transferring: 

 - Global Lists
 - Areas
 - Iterations
 - Work Items

in this order.

WitSync is designed to be idempotent, that is, it can be run multiple times and getting the same result. The tool will pull the work items returned from a Work Item Query on the source TFS Project, compare to matching work items on the target TFS Project and update the latters.
Command line is less user friendly to start, but easy to automate.

The tool is controlled via a mapping file and you can force options on the command line.

Sample executions:
```Batchfile
WitSync.exe --workitems -c http://localhost:8080/tfs/DefaultCollection -p SourceProject -d http://localhost:8080/tfs/DefaultCollection -q DestProject -m MyMappingFile.yml –v

WitSync.exe -m MyMappingFile.yml
```

You can project only a data subset from the source to the target. This is very useful in scenario where TFS is used at different organization and they want to sync only a couple of work item types; it was indeed the first application for WitSync.

The tool sync two projects that may belong to the same or different Collections, in the same or another TFS Instance. I have been successful syncing data across continents between TFS instances living in different Active Directory domains.  

## Prerequisites

Both TFS environments should be prepared before using WitSync and starting synchronizing data.

### Source and Target

Create a Work Item Query on the source TFS Project and put path to the query in the mapping file. The columns returned are not important, as the mapping file specifies the fields to sync. You can skip this step and write the [WIQL](http://msdn.microsoft.com/en-us/library/bb130306.aspx) query in the mapping. 

Create a Work Item Query on the target TFS Project and put path to the query in the mapping file. This query can optimize the process as WitSync tries to match the source work items with the one returned by this query. The columns returned are not important, as the mapping file specifies the fields to sync. As above, you can directly put here the WIQL code instead of the Query path.

### Target Work Item types

WitSync matches source and target workitems looking at their IDs. You have two modes to record the match.

 - Specifying an Integer field, from the target Work Item type, to host the ID of corresponding source work item.
 - Using a local _Index_ file to record matching history.

In this latter case, using an Index file, make sure to properly backup it, otherwise the tool will re-create new workitems instead of updating the existing  ones.

### Permissions

You need an account in the source domain, say **TFS_WitSyncReader** that can execute the source WIQuery and read the work items and another account in the target domain, say **TFS_WitSyncWriter**. It must have permission to execute the target WIQuery and to add and update Work Items in the mapped Areas and Iterations.
The Globallist, Area and Iteration stages require Project Administrator permissions for **TFS_WitSyncWriter**.

You can use a single account when target and source belongs to the same AD domain, or you can use shadow accounts, or store credential in the Windows credential store; finally, you can feed credentials explicitly in the mapping file or the command line.


## Command line Syntax

Syntax is
```Batchfile
WitSync.exe <action> -c <source_collection_url> -p <source_project_name> -d <destination_collection_url> -q <destination_project_name> [-m <path_to_mapping_file>] [-v[erbose]] [-t[est]] [_advanced_options_]
```
The supported values for action are `SyncWorkItems` and `SyncAreasAndIterations`. This gives flexibility as Area/Iteration can be different in the two projects and you may not want syncronyze it.

`Verbose` option prints detailed output.

`Test` option tries action but does not save any change to target. Use of index file is specified in the mapping.

More details can be found in [Command line options](docs/CommandLineOptions.md).


## Return values

Successful run returns `0`, any other number means error.

The output will contain messages explaining what went wrong. To save the output in a file, use the `logFile` option.

To save the list of changed objects in a file, use the `changeLogFile` option. This is useful to create reports or make a complex workflow composed by multiple scripts and tools.
Only successful saves write records in the Change Log.  


## Mapping file

The mapping file use the [YAML](http://www.yaml.org/) format to configure the WitSync pipeline. You can configure just a small subset of information, as WitSync assumes you want an hi-fidelity copy of the source.

More details can be found in [Mapping file](docs/Mapping.md).


## Build and Redistribute

WitSync is written in C# using Visual Studio 2013. It depends on TFS assemblies that come with Team Explorer 2013.

The build is customized, so that a NuGet package is produced in the `distr` folder. This allows distributing WitSync using [Chocolatey](https://chocolatey.org/).

```
cinst WitSync -prerelease -source \\fileserver\yourLocalChocolateyFeed
```
