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
Documentation of features and how to use them is available in the Wiki associated with this repository.

## Building from Source
The software can be cloned with the usual `git clone` command. However, depending on the version checked out, it may contain submodules which can be initialised as part of the initial clone or as a separate step after the fact with `git submodule update --init --recursive`.

## Running with Docker
CapX can be run from a Docker container. To build and run CapX with Docker, build the image with:

```bash
read -s GITHUB_TOKEN
<type your github token with package read permission>
export GITHUB_TOKEN
docker build --secret id=github_token,env=GITHUB_TOKEN -t capx . 
```

Once built, run a container from the image, map to the exposed ports and keep the database in a Docker volume so it persists past the lifetime of the run command:

```bash
docker run -p <your_port>:80 -v capx_state:/app/state capx
```

You can then access the app via a web browser at `localhost:<your_port>`.

To wipe the volume and start from the initial state, use
```bash
docker volume rm capx_state
```
once the container has been brought down.

## Running with Docker Compose

To run with Docker Compose, provide your GitHub token as an environment variable:

```bash
read -s GITHUB_TOKEN
<type your github token with package read permission>
export GITHUB_TOKEN
```
and then build and bring up the container:
```bash
docker compose up --build
```
You can then access the app via a web browser at `http://localhost:3000`. You can change the port by setting the environment variable CAPX_PORT, either in a .env file or in the environment.

Use Ctrl-C to bring the container down. The database state will be maintained in a docker volume. To wipe the volume and start from the initial state, use
```bash
docker volume rm capx_state
```
once the container has been brought down.

Since the image is being built each time you run "docker compose up --build", any changes to source files will be picked up and included.

### Known Issues
1. CapX will run slowly in Firefox while the ad blocker is enabled. Disabling the ad blocker resolves this issue.
2. Use of the Bitwarden browser plugin has been know to slow down the response of the interactive graphs. See [Issue 302](https://github.com/UoMResearchIT/CapX/issues/302) for details.
