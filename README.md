# CapX
This is a PPM tool written in .NET Blazor Server. This is being used as a stop-gap solution for the capacity planning system the ITS Governance and Delivery management Office (GaDMO) currently use which is of limited use to us day to day. Instead, CapX has an export facility which allows its data to be output in a format GaDMO can read into their system.

The production version of CapX is currently deployed to [balex.itservices.manchester.ac.uk](balex.itservices.manchester.ac.uk). This is a 10.99 private IP so users will need to be on the VPN to access.
There is a development version of CapX deployed to [balextest.itservices.manchester.ac.uk](balextest.itservices.manchester.ac.uk) which is a build of the `dev` branch and show cases new features but might not be entirely stable.

## User Accounts and Access
As of v1.6, the app is integrated with UoM CAS with local access to restricted parts of the app managed within the app using a Role-Based Access Control database table. Super-users are able to manage user roles via the "Manage Access" page.
The production version of CapX uses the DS CAS and users with a standard UoM user account can authenticate. The development version of CapX authenticates using the PPAD CAS instance; users will need a UoM PPAD account to use the development version.

## Automated Deployment
CapX makes use of automated deployment. As the VMs are on the University private network, they are not visible to GitHub so we cannot simply use a GitHub action to auto-deploy. Instead, the VMs run a cron job which long-polls the repository every 10 minutes, using `git fetch` and `git status` to determine programmtically whether the source code on the VM is behind the remote. If it is, it will pull the latest source code for the `release` branch (production VM) or `dev` branch (development VM), authenticating with GitHub using an SSH key, and then build the software, apply database migrations and restart the web services. The development build script additionally copies the database from the production VM prior to applying migrations to ensure the development version is tested on real data. The production database is also backed-up as part of the deployment process in case of failure. Deployment scripts can be found in the `deployment` folder in the repo.

## Documentation and User Guides
All documentation is now available in the Wiki associated with this repository rather than the Readme as before.

## Running with Docker
CapX can be run from a Docker container. To run CapX with Docker, build the image with:

```bash
docker build --build-arg GITHUB_USERNAME=<your_github_username> --build-arg GITHUB_PASSWORD=<your_access_token_with_package_read_scope> -t capx . 
```

Once built, run a container from the image and map to the exposed ports:

```bash
docker run -p <your_port>:80 -p <your_ssl_port>:443 capx
```

You can then access the app via a web browser at `localhost:<your_port>`.

### Known Issues
1. CapX will run slowly in Firefox while the ad blocker is enabled. Disabling the ad blocker resolves this issue.
2. Use of the Bitwarden browser plugin has been know to slow down the response of the interactive graphs. See [Issue 302](https://github.com/UoMResearchIT/CapX/issues/302) for details.
