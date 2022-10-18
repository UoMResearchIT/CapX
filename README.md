# RSE-PPM-Tool
A PPM tool written in .NET Blazor Server. Ideally this will be a stop-gap solution for the Capacity Planner system we currently use with the Head of RSE updating the Capacity Planner on a monthly basis from this tool instead.

Currently deployed to [balex.itservices.manchester.ac.uk](balex.itservices.manchester.ac.uk). Access is restricted to Manchester IPs so will need to be on the VPN.

## User Accounts
User accounts are configured locally for now and can be setup by any current user until role-based authorisation is added.

## Adding RSEs and Availability
You can add an RSE through the "People" page. CapX does not take into account holidays, closure days or annual leave, which means, deducting all these from the number of business days in a year (261) gives a total number of working days of 220 which is approximately 0.84 FTE. RSEs therefore should either only be set to a maximum availability of 0.84 FTE or we assign people 16% of their time to an internal project called "non-working time" and make them available 1.0 FTE. Not decided which yet.

## Fixed Work vs Fixed Duration
Drawn-down type tasks, where we know how many days we want to allocate to a task, it can be a "Fixed Work" task. This allows us to bill using the day rate. We assume 220 chargeable days per year in this case as the remainder of the working days can be taken as annual leave or are closure days. Naturally, the customer is not billed for these non-working days.

However, if your task ought to run for a particular length of time and an RSE assigned at a particular percentage, then you can use a "Fixed Duration" task. CapX will calculate its cost based on 261 working days in the year. This assumes that the RSE will be paid via PCM which means the customer gets billed for the RSE's salary even when they are on sick leave, annual leave or during closure days when the RSE still gets paid. v1.1+ offers the ability to specify a day rate for a particular RSE on a particular task. This can be modified appropriately to ensure the budget for the task is calculated somewhere close to the actual amount being collected by the PCM.
