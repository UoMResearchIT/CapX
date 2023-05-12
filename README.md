# CapX
This is a PPM tool written in .NET Blazor Server. This is being used as a stop-gap solution for the Capacity Planner system we currently use with the Head of RSE updating the Capacity Planner on a monthly basis from this tool instead.

Currently deployed to [balex.itservices.manchester.ac.uk](balex.itservices.manchester.ac.uk). Access is restricted to Manchester IPs so will need to be on the VPN.

## User Accounts
User accounts are configured locally for now and can be setup by any current user until role-based authorisation is added.

## Adding RSEs and Setting their Availability
You can add an RSE through the "People" page. CapX does not take into account bank holidays, closure days or annual leave, which means, deducting all these from the number of business days in a year (261) gives a total number of working days of 220 which is approximately 0.84 FTE. RSEs therefore should be set to a maximum availability of 0.84 FTE to allow them to take these days off and the scheduling remain correct.

## Assigning RSEs to Tasks
When assigning an RSE to a task, due to the above, they can only work to a maximum of 84% (see above). The capacity planner will show how much capacity they have spare out of the time they are available. If you want an RSE to work on a project "at 50%" i.e. 2.5 days per week then you will need to assign them to the task at 42% (half of 84%). This will mean that the duration of a fixed work task will be scheduled assuming that they only work 2.1 days per week with 0.4 days spent on leave or on a closure day. In practice, they may book more hours per week to a task initially and the task will show as being ahead of schedule, but as soon as they take leave, this buffer will be eaten up and bring it back in line with the schedule. This is preferrable than having to constantly adjust the schedule every time some takes a day off, or projects being shown as constantly behind.

## Baseline Activity
Some of the activities RSEs do does not fall under the category of a project either because it is a continuous duty or it is covered by the baseline. Examples may include people management and leading communities such as the R User Group. To take these into account in an RSE's load, we simply reduce their availability accordingly. This can be done by adding an entry into the "Availability Changes" page when editing a person. You can add an explanation for this reduction in the form which will be read out during the capacity export we send to GaDMO.

## Task Type and Billing
CapX offers three types of task "Fixed Units", "Fixed Duration" and "Fixed Work". The "Fixed Units" task still requires either duration or work to be fixed so it a sort of meta task type and will be rarely necessary to use. Instead, we will focus on the other two.

### Fixed Work Tasks
Where we know how many days effort we want to allocate to a task, such as a draw-down type arrangment, we can make that task a "Fixed Work" task. When scheduling this type of task, CapX takes the percentage of the people assigned ("Units"), assumes 7 hours per day and 5 days per week (for 1 person at 100%) and marches forward in time to compute the end date. This type of task allows us to bill using the day rate as the budget of the task is computed from the number of hours of effort so customers are paying "by the hour". This means that the customer is not billed for non-working days such as leave / closure or sickness. We can easily collect payment for this using a journal transfer for the exact amount.

### Fixed Duration Tasks
These types of tasks can be useful you want to precisely specify how long the engagement should last. If you assign a person at a particular percentage for that fixed period, CapX will then calculate how many hours effort (work) accumulate when that person works at the percentage given for the number of days duration. Duration in CapX is specified as the number of **calendar days** and hence **includes weekends and bank holidays**. CapX will then calculate its cost based on the number of hours effort which totals 261 working days in the year (for 1 person at 100%). This assumes that the RSE will be paid via PCM which means the customer gets billed for the RSE's salary even when they are on annual leave or during closure days when the RSE still gets paid. However, if RSEs are assigned up to a maximum of 84%, the work will only be calculated on 220 days per year instead. Furthermore, the day rate used to compute PCM costs is calculated based on a particular RSE's spine point and is not the same for every team member. To ensure that the budget is worked out correctly when someone is paid by PCM, v1.1+ offers the ability to specify a day rate for a particular RSE on a particular task. This can be inflated or decreased appropriately based on the RSE to ensure the budget for the task is calculated somewhere close to the actual amount being collected by the PCM.

For example, one week's work at 84% is 29.4 hours. Assuming the RSE is paid at the standard day rate this gives a cost of £9,172,80. But the PCM will cover 100% as it includes an allowance for holidays / closure / leave, which means it in calculated for 35 hours per week giving a cost of £10,920. So the rate we should use to calculate the correct cost for PCM billing would need to be 312 / .84 = £371.43 per day. This gives a cost for the 29.4 hours scheduled in CapX for the task as £10,920.04 which is close enough.

Of course, each financial year the spine points increase as presumably the PCM collection increases too. We can split long-running tasks at the financial year end and assign the same RSE again for the next financial year but at an increased day rate to ensure the budget remains correct. **Ultimately, if they are paid via PCM, we don't particularly need to track this in CapX so don't worry about it!**

### End Dates
Task end dates are included in the assignable duration. So if you have a fixed duration task that starts on Monday 2nd January 2023 and runs for 7 days duration, the end date will show as Sunday 8th January 2023 (technically 6 days later) not 7 days later. An artifact of this is that the charts will not be able to display tasks that are 1 day in duration since the start and end date are the same and they are considered to have a zero value and hence no width on the chart.

## Additional Funding
If a project that is already in the system gains an additional tranche of funding for a new phase and we have decided that it ought not to be treated as a separate project, we can extend the funding of an existing project by first updating the "Budget" field for the project with the new funding total and then adding a new Task in the task list to allow the scheduling of the extension.
