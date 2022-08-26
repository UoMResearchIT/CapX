# RSE-PPM-Tool
A PPM tool written in .NET Blazor Server. Ideally this will be a stop-gap solution for the Capacity Planner system we currently use with the Head of RSE updating the Capacity Planner on a monthly basis from this tool instead.

Currently deployed to [balex.itservices.manchester.ac.uk](balex.itservices.manchester.ac.uk). Access is restricted to Manchester IPs so will need to be on the VPN.

## User Accounts
User accounts are configured locally for now and can be setup by any current user until role-based authorisation is added.

## Adding RSEs and Availability
You can add an RSE through the "People" page. CapX does not take into account holidays, closure days or annual leave, which means, deducting all these from the number of business days in a year (261) gives a total number of working days of 220 which is approximately 0.84 FTE. RSEs therefore should either only be set to a maximum availability of 0.84 FTE or we assign people 16% of their time to an internal project called "non-working time" and make them available 1.0 FTE. Not decided which yet.
