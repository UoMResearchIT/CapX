# CapX
This is a PPM tool written in .NET Blazor Server. This is being used as a stop-gap solution for the capacity planning system the ITS Governance and Delivery management Office (GaDMO) currently use which is of limited use to us day to day. Instead, CapX has an export facility which allows its data to be output in a format GaDMO can read into their system.

CapX is currently deployed to [balex.itservices.manchester.ac.uk](balex.itservices.manchester.ac.uk). This is a 10.99 private IP so users will need to be on the VPN to access.
We also have a test instance of the app to use as a staging area. This is found at 10.99.96.160 [balextest.itservices.manchester.ac.uk](balextest.itservices.manchester.ac.uk).

## User Accounts and Access
As of v1.6, the app is integrated with UoM CAS with local access to restricted parts of the app managed within the app using a Role-Based Access Control database table. Super-users are able to manage user roles via the "Manage Access" page.

## Documentation and User Guides
All documentation is now available in the Wiki associated with this repository rather than the Readme as before.

## Running with Docker
To run CapX with Docker,
```
cd CapX/PPMTool
docker compose up
```
