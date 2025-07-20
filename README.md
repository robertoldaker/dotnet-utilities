# dotnet-utilities
utilitiy programs for developing and deploying .NET apps

**AutoRestartDotNetServices**

This will automatically restart any dotnet services (websites) if it finds a new patch of the dotnet runtime installed. To use it run it from
the root folder where websites are installed. The first time it is run it will simply record the current version of dotnet and 
store it in a filed called DotNetInfo.json. When it is run again it will see if there has been a new install of the dotnet runtime and
if so look for any websites that use the dotnet runtime that has been upgraded and then look for a service for the website and 
restart it if the service is currently running.

This utility program overcomes issues that can arise if a website is not restarted after a dotnet patch has been applied.

Typically it will be run in a cronjob an hour or so after any unattended upgrades have been applied (where any dotnet patches will be applied)

This is an example ...
```
# Monitor dotnet installs and restart services if an update detected
# unattended upgrades runs 6.30ish so scheduled for 8 to ensure its finished
# only redirect stdout so that stderr gets emailed
0 8 * * *  cd websites && AutoRestartDotNetServices > /dev/null


