<!--
SPDX-FileCopyrightText: 2026 University of Manchester

SPDX-License-Identifier: apache-2.0
-->

# Capacity eXtended (CapX)
This tool initially started as a basic project and portfolio management (PPM) tool. Its first feature was capacity management, but it has since been extended to incorporate a much larger, more complex data model useful for an increased number of operational management activities. Written in .NET Blazor (Server) with a SQLite database (other providers are supported), it is used for managing many aspects of the service delivery of the digital research technical professional departments and the development of its staff.

> [!IMPORTANT]
> The [dRTP Community of Operational Management, Peer Advice and Shared Support (dRTP COMPASS)](https://uomresearchit.github.io/DRTP-Op-Man-CoP-Website/) is responsible for its strategic direction. We’d love to hear if you are using this tool. If you would like to be part of dRTP-OMCoP then please let us know!

## User Accounts and Access
The app supports integration with CAS / Shibboleth as well as Azure AD / Entra with access to restricted parts of the app managed within the app using a Role-Based Access Control (RBAC) database table. Super-users are able to manage user roles and access via the "Manage Access" page.

If the app is run in the "Local" solution configuration then third-party authentication is disabled. Users can then select any name from the dropdown list in the header bar to log in as that person and role. This mode is intended for debugging or demos.

## API Access
Any user of the web application can gain access to the API endpoints. Note that their success in using the endpoints is dictated by their role in the web app RBAC database table. To access the API, users need to generate an API key from the "Developer Settings" in the web app under the "Developer Settings". The successful generation of an API key in the web app depends on a suitable secret (minimum 32 characters) being injected into the `Jwt:SecretKey` configuration parameter for the web application. This secret parameter can be injected via an environment variable named `API_KEY_SECRET`, or during development if using Visual Studio, this can be done by simply opening "Manage User Secrets" for the project and adding `"Jwt:SecretKey" : "some-32-char-long-value"` to the .NET secrets manager.

## Deployment
Deployment scripts, as well as reverse proxy configurations and sample environment variable files, are provided in the `deployment` directory. These scripts can be used with cron to long-poll GitHub for changes to the relevant branch, kicking off a re-build of the container as appropriate. The development deploy script additionally copies the database from the production VM prior to applying migrations to ensure the development version is tested on real data. The production database is also backed-up as part of the deployment process in case of failure. Any time a DB file is to be copied, all the `PPMTool.db*` files are copied since WAL is enabled. `sqlite3 PPMTool.db VACUUM;` is used to flush the WAL journals before any manipulation of the DB takes place.

> [!WARNING]
> The app will fail to start if it detects that required variables are not set. Exception details will be written to the log files in the `Logs` folder as well as `syslog` or `docker log`.

### Database Backups
Database backups can be scheduled using the relevant script in the `deployment` directory.

## Documentation and User Guides
Documentation of some of the features and how to use them is available in the Wiki associated with this repository. This is admittedly not kept up-to-date.

## Building and Running from Source
The software can be cloned with the usual `git clone` command. However, depending on the version checked out, it may contain submodules which can be initialised as part of the initial clone or as a separate step after the fact with `git submodule update --init --recursive`.

> [!TIP]
> By default, cloning the repository will checkout the `dev` branch. If you simply want to deploy a particular release then you can checkout the relevant tagged commit using `git checkout <tagname>` e.g. `git checkout Release_v1.14.0` then follow the instructions below to spin-up the container with Docker Compose below.

### Database Connection
The database connection string needs to be specified in a `CONNECTION_STRING` environment variable. See the `.env.sample` file for example connection strings. Note that this is also required at "design-time" when running EF Core tools to update the database. The connection string for `LEAVEBOOKINGS_CONNECTION_STRING=` is only relevant to The University of Manchester. If using CapX elsewhere, this can simply a be a dummy string as long as it is not blank. During development, User Secrets can be used to override the blank values specified in the `appsettings.json`.

### Seeding the Database
The app will run migrations every time it starts. If the connection string permits it, it will therefore create a blank database on first run. Furthermore, a single super user will be added to allow you to login. A blank database limits the ability to test new features or to demo the software without first adding records to the blank tables through the UI which takes time. To faciltate better testing, developers can set the `SEED_DUMMY_DATA` environment variable to "TRUE" (case insensitive) to have the software populate all the empty tables with dummy data on start-up.

> [!WARNING] 
> This feature overwrites all data in the tables as soon as the app starts!

### Database Migrations
As CapX supports multiple DB providers, the migrations required to set up the database and its tables are specific to the provider. Each supported provider has its own `.Migrations` project containing the relevant migrations for ensuring the DB is aligned to the models in the code. To add a new migration for a particular provider, set the `DB_PROVIDER` variable in the environment appropriately then run the following command from the root directory to invoke the EF Core Tools. For example, to add a migration for SQLite, set `DB_PROVIDER=sqlite` (typically using User Secrets if developing with Visual Studio or VS Code):

```
dotnet ef migrations add NameOfMigrationHere --context PPMToolContext --project PPMTool.Migrations.Sqlite/PPMTool.Migrations.Sqlite.csproj --startup-project PPMTool/PPMTool.csproj
```

The database can be updated manually with the following command but the web app calls `context.Database.Migrate();` anyway so the database will be created/updated as soon as the app runs making manual update rarely necessary.

```
dotnet ef database update --context PPMToolContext --project PPMTool.Migrations.Sqlite/PPMTool.Migrations.Sqlite.csproj --startup-project PPMTool/PPMTool.csproj
```

> [!WARNING] 
> If you are adding a migration for one provider due to a DB schema change, it is expected that you would add migrations for all the others that replicate the same change. Otherwise different DB providers will end up with DB schemas that differ from each other!

### Solution/Build and Launch Configurations
There are two _solution_ configurations: `Local` and `Release` that combine project-level _build_ configurations.

- `Local` is to be used for development on your own machine as it bypasses third-party CAS authentication integrations and instead allows the developer to "sign-in" with any user in the database for testing purposes.    
- `Release` is designed to be used on test and production servers and integrates with third party CAS authentication providers. They also include additional logging and crash reporting integration with Sentry that are not included in the `Local` configuration.

> [!TIP]
> The `ASPNETCORE_ENVIRONMENT` variable is used to specify whether the app should run in production or development mode. This is a runtime variable and has nothing to do with the compile-time _build_ configuration.

### Running with Docker Compose
Docker Compose runs a single container with a single volume containing the database.

#### Environment Variables
The application and the container requires serveral environment variables to be set in order to run correctly.
To set this up, create a `.env` file in the repository root with the following required variables:

> [!TIP]
> You do not have to create a `.env` file in the root. You can name it whatever you want and put it wherever you want on the system. However, you must then pass the path to this file with the `--env-file` flag whenever you call a docker compose command.

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core runtime environment. Valid values: `Development` or `Production`. This determines which `appsettings.*.json` file is loaded at runtime. |
| `BUILD_CONFIGURATION` | .NET build configuration (`-c` flag). Valid values: `Local`, `Debug`, or `Release`. See [Solution/Build and Launch Configurations](#solutionbuild-and-launch-configurations) for details. |
| `CONNECTION_STRING` | SQLite connection string, e.g. `Data Source=state/PPMTool.db` |
| `LEAVEBOOKINGS_CONNECTION_STRING` | Connection string for the leave bookings database (The University of Manchester only) |
| `API_KEY_SECRET` | Secret for API key generation (minimum 16 characters). Use `openssl rand -hex 16` to generate a strong key. |
| `CAPX_HTTP_PORT` | Port for the web application (e.g. `3000`) |
| `CAPX_API_PORT` | Port for the API (e.g. `3001`) |
| `CAPX_STATE_DIR` | Where the SQLite DB lives outside the container (if using this DB provider) |
| `SEED_DUMMY_DATA` | Set to `TRUE` to seed dummy data on startup |
| `SUPERUSER_NAME` | Name of the superuser (required if seeding) |
| `SUPERUSER_USERNAME` | Username of the superuser (required if seeding) |
| `SUPERUSER_EMAIL` | Email of the superuser (required if seeding) |
| `DB_PROVIDER` | Which DB provider should be used. Supports sqlite, sqlserver, postgresql values. |
| `DP_KEY_PATH` | Optional path to where on disk the data protection keys should be stored. |

The following variables need only be set when not using the "Local" solution configuration:

| Variable | Description |
|----------|-------------|
| `SENTRY_DSN` | The URL to which crash reports from the Sentry library are sent if using this crash monitoring solution |
| `MAIL_FROM_ADDRESS` | Email address from which notifications are sent |
| `MAIL_SMTP_SERVER` | URL of the SMTP server to send email requests to |
| `CAS_PROTOCOL` | The protocol that CAS/Shib should use (either 2 or 3) |
| `CAS_BASE_URL` | The base URL of the CAS/Shib authentication endpoint |
| `ENTRA_INSTANCE` | Base URL of the Entra ID authentication service |
| `ENTRA_DOMAIN` | The primary domain of the tenant |
| `ENTRA_TENANT_ID` | ID of the tenant |
| `ENTRA_CLIENT_ID` | ID of the registered app |
| `ENTRA_CALLBACK_PATH` | Local path where Entra redirects the browser after successful authentication |
| `AUTH_TYPE` | Whether to use "CAS" or "AzureAd" as the authentication provider |
| `AUTH_HOST_URL` | The URL of the site (the service URL used by some auth providers) |

An example `.env` file is provided in the source code as `.env.sample`.

#### Building and Running
Build and bring up the container:

```bash
docker compose up --build
```

or if you have named your `.env` file something custom and placed it in a custom location:

```bash
docker compose --env-file /path/to/env/file up --build
```

You can then access:
- The web application at `http://localhost:3000` (or your configured `CAPX_HTTP_PORT`)
- The API at `http://localhost:3000/api` (or your configured `CAPX_HTTP_PORT`)
- The swagger interface at `http://localhost:3000/swagger` (or your configured `CAPX_HTTP_PORT`)

Use Ctrl-C to bring the container down. The database state is maintained in a Docker volume. To wipe the volume and start from the initial state, use:

```bash
docker volume rm capx_state
```

Since the image is rebuilt each time you run `docker compose up --build`, any changes to source files will be picked up and included.

## Tests
There is a test project in the solution for testing the web app and the API. To run the tests, the web application needs to be running on the expected port and the database needs to be accessible. There needs to be a valid API key in the database for the API tests to use otherwise the setup fixture will complain that it cannot run the tests.

### Running Locally with Visual Studio
If running locally in Visual Studio, select the "Local" build configuration and then "Run without Debugging". Open the Test Explorer feature of Visual Studio and click "Run Tests" and the current set of tests will run against the application that are currently running on localhost and HTTPS.

### Running from CLI with Docker
TBC

### Running with GitHub Actions
TBC

## Known Issues
1. CapX will run slowly in Firefox while the ad blocker is enabled. Disabling the ad blocker resolves this issue.
2. Use of the Bitwarden browser plugin has been know to slow down the response of the interactive graphs. See [Issue 302](https://github.com/UoMResearchIT/CapX/issues/302) for details.
3. CapX does not work properly in Safari on macOS when run from Docker. See [Issue 570](https://github.com/UoMResearchIT/CapX/issues/570) for details.
4. Use of GitInfo library means that the app must have access to the `.git` directory at compile-time. If checked out as a submodule this wont' be the case so this is not supported. See [Issue 1255](https://github.com/UoMResearchIT/CapX/issues/1255) for details.

---

## CapX @ The University of Manchester RSE Department
The production version of CapX is currently deployed to [balex.itservices.manchester.ac.uk](https://balex.itservices.manchester.ac.uk) built from the `release` branch in the repo. This machine is on the UoM private network so users will need to be on the VPN to access.

There is a development version of CapX deployed to [balextest.itservices.manchester.ac.uk](https://balextest.itservices.manchester.ac.uk) which is a build of the `dev` branch and showcases new features but might not be entirely stable. This is also on the private network.

### API
CapX also offers an API integrated into the web application accessed via [https://balex.itservices.manchester.ac.uk/api](https://balex.itservices.manchester.ac.uk/api) and [https://balextest.itservices.manchester.ac.uk/api](https://balextest.itservices.manchester.ac.uk/api) in production and pre-production respectively. Endpoints require an API key to be supplied in the request header which can be generated in the developer settings part of the main web application. Swagger is also enabled for exploring the API at `...manchester.ac.uk/swagger`.

### Authentication
Authentication is provided by Entra production and pre-production systems for production and test instances respectively. Users will need a UoM PPAD account to access the test system.

### Deployment
CapX makes use of automated deployment. As the VMs are on the University private network, they are not visible to GitHub so we cannot simply use a GitHub action to auto-deploy. Instead, the build/test VMs run cron jobs which long-poll the repository every 10 minutes, using `git fetch` and `git status` to determine programmtically whether the source code on the VM is behind the remote on the `dev` and `release` branches. If it is, it will pull the latest source code for the branch, authenticating with GitHub using an SSH key, and then build the docker containers. The scripts used a provided in the `deployment` directory along with reverse proxy configuration for Nginx. Documentation on how to use the config files to setup the reverse proxy can be found on the [old MDS Wiki](https://github.com/UoMResearchIT/MDS-Essentials/wiki/.NET-Web-Hosting-on-Ubuntu#deployment-of-net-blazor-app-on-ubuntu-with-nginx).

### Database Backups
The databases are backed up (including flushing of the WAL journals) as part of the deployment automation pipeline. However, there is also an hourly cronjob that flushes and backs up the DB to a different directory. This mechanism stores 72 backups, deleting the oldest when this file count is exceeded.

### Leave Booking database
The University of Manchester instance of the CapX API also connects the [institutional leave booking system](https://holiday.its.manchester.ac.uk/) database. This connection string needs to be specified properly for this connection to work.


