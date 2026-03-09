# Capacity eXtended (CapX)
This is tool initially started as a basic project and portfolio management (PPM) tool. Its first feature was capacity management, but it has since been extended to incorporate a much larger, more complex data model useful for an increased number of operational management activities. Written in .NET Blazor (Server) with a SQLite database, it is used for managing many aspects of the service delivery of the RSE department and the development of its staff.

The production version of CapX is currently deployed to [balex.itservices.manchester.ac.uk](https://balex.itservices.manchester.ac.uk) built from the `release` branch in the repo. This is a 10.99 private IP so users will need to be on the VPN to access.

There is a development version of CapX deployed to [balextest.itservices.manchester.ac.uk](https://balextest.itservices.manchester.ac.uk) which is a build of the `dev` branch and showcases new features but might not be entirely stable. This is also on the private network.

CapX also offers an API integrated into the web application accessed via [https://balex.itservices.manchester.ac.uk/api](https://balex.itservices.manchester.ac.uk/api) and [https://balextest.itservices.manchester.ac.uk/api](https://balextest.itservices.manchester.ac.uk/api) in production and pre-production respectively. Endpoints require an API key to be supplied in the request header which can be generated in the developer settings part of the main web application.

## User Accounts and Access
The app is integrated with UoM CAS/Shibboleth as well as Azure AD / Entra with access to restricted parts of the app managed within the app using a Role-Based Access Control (RBAC) database table. Super-users are able to manage user roles and access via the "Manage Access" page.

The production version of CapX uses the production (DS) CAS/Shib/Entra and users with a standard UoM user account can authenticate. The development version of CapX authenticates using the pre-production (PPAD) CAS/Shib/Entra instances; users will need a UoM PPAD account to use the development version. If the app is run in the "Local" solution configuration then thrid-party authentication is disabled and instead, users can select any name from a dropdown list to log in as any user from the header bar -- this mode is intended for debugging or demos.

## API Access
Any user of the web application can gain access to the API endpoints. Note that their success in using the endpoints is dictated by their role in the web app RBAC database table. To access the API, users need to generate an API key from the "Developer Settings" in the web app under the "Developer Settings". The successful generation of an API key in the web app depends on a suitable secret (minimum 32 characters) being injected into the `Jwt:SecretKey` configuration parameter for the web application. This secret parameter can be injected via an environment variable named `API_KEY_SECRET`, or during development if using Visual Studio, this can be done by simply opening "Manage User Secrets" for the project and adding `"Jwt:SecretKey" : "some-32-char-long-value"` to the .NET secrets manager.

## Automated Deployment
CapX makes use of automated deployment. As the VMs are on the University private network, they are not visible to GitHub so we cannot simply use a GitHub action to auto-deploy. Instead, the build/test VMs run cron jobs which long-poll the repository every 10 minutes, using `git fetch` and `git status` to determine programmtically whether the source code on the VM is behind the remote on the `dev` and `release` branches. If it is, it will pull the latest source code for the branch, authenticating with GitHub using an SSH key, and then build the software, apply database migrations and restart the web services. The development build script additionally copies the database from the production VM prior to applying migrations to ensure the development version is tested on real data. The production database is also backed-up as part of the deployment process in case of failure. Any time a DB file is to be copied, all the `PPMTool.db*` files are copied since WAL is enabled. `sqlite3 PPMTool.db VACUUM;` is used to flush the WAL journals before any manipulation of the DB takes place. All the scripts live on the build/test machine with the production VM just acting as a deployment target.

Deployment scripts can be found in the `deployment` folder in the repo. Documentation on how to use the config files to setup the reverse proxy can be found on the [old MDS Wiki](https://github.com/UoMResearchIT/MDS-Essentials/wiki/.NET-Web-Hosting-on-Ubuntu#deployment-of-net-blazor-app-on-ubuntu-with-nginx). Reqruied environment variables can be supplied in the `variables.var` file supplied wih the the source code when deploying. The `systemd` service that runs the kestrel server will then read this file to set local environment variables which are then read in by the .NET app builder when the app starts.

> [!WARNING]
> The app will fail to start if it detects that required variables are not set. Exception details will be written to the log files in the `Logs` folder as well as `syslog`.

## Database Backups
The databases are backed up (including flushing of the WAL journals) as part of the deployment automation pipeline. However, there is also an hourly cronjob that flushes and backs up the DB to a different directory. This mechanism stores 72 backups, deleting the oldest when this file count is exceeded.

## Documentation and User Guides
Documentation of features and how to use them is available in the Wiki associated with this repository. This is admittedly not kept up-to-date.

## Building and Running from Source
The software can be cloned with the usual `git clone` command. However, depending on the version checked out, it may contain submodules which can be initialised as part of the initial clone or as a separate step after the fact with `git submodule update --init --recursive`. If using Visual Studio 2022, developers will need to run `Update-Database` from the package manager console to create the DB and run the migrations before running the solution.

### Database Connection
The database connection string needs to be specified in a `CONNECTION_STRING` environment variable. During development in Visual Studio, See the `deployment/variables.env` file for example connection strings. Note that this is also required at "design-time" when running EF Core tools to update the database. The CapX API also connects the [leave booking system](https://holiday.its.manchester.ac.uk/) database. The connection string for this connection also needs to be specified in the same way in a variable called `LEAVEBOOKINGS_CONNECTION_STRING=`. During development, User Secrets can be used to override the blank value in the `appsettings.json`.

### Seeding the Database
The default database produced when first running EF Core's `dotnet ef database update` command (or `Update-Database` from within the Visual Studio Package Manager Console) runs the migrations available in the source code checked out. This produces an empty database. When the app starts, a single super user will be added to allow you to login. In addition, based on the migration data available in the source code, the timesheet activities and tasks in use at UoM at the time the feature was added are also there as well as the initial version of the RSE competency framework. Every other table is blank. This limits the ability to test new features or to demo the software without first adding records to the blank tables through the UI which takes time. To faciltate better testing, developers can set the `SEED_DUMMY_DATA` environment variable to "TRUE" (case insensitive) to have the software populate all the empty tables with dummy data on start-up.

> [!WARNING] 
> This feature overwrites all data in the tables as soon as the app starts!

### Solution/Build and Launch Configurations
The Visual Studio solution no-longer has _launch_ configurations since the web app and the API are now integrated into one application. However, there are three _solution_ configurations: `Local`, `Debug` and `Release` that combine project-level _build_ configurations of the same name.
- `Local` is to be used for development on your own machine as it bypasses third-party CAS authentication integrations and instead allows the developer to "sign-in" with any user in the database for testing purposes.    
- `Release` is designed to be used on test and production servers and integrates with third party CAS authentication providers. They also include additional logging and crash reporting integration with Sentry that are not included in the `Local` configuration.    
- `Debug` solution configuration is basically the same as `Local` but with a slightly different logging level making it less verbose.

### Running with Docker Compose
Docker Compose runs a single container with a single volume containing the database.

#### Environment Variables
The application and the container requires serveral environment variables to be set in order to run correctly.
To set this up, create a `.env` file in the repository root with the following required variables:

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core runtime environment. Valid values: `Development` or `Production`. This determines which `appsettings.*.json` file is loaded at runtime. |
| `BUILD_CONFIGURATION` | .NET build configuration (`-c` flag). Valid values: `Local`, `Debug`, or `Release`. See [Solution/Build and Launch Configurations](#solutionbuild-and-launch-configurations) for details. |
| `CONNECTION_STRING` | SQLite connection string, e.g. `Data Source=state/PPMTool.db` |
| `LEAVEBOOKINGS_CONNECTION_STRING` | Connection string for the leave bookings database |
| `API_KEY_SECRET` | Secret for API key generation (minimum 16 characters). Use `openssl rand -hex 16` to generate a strong key. |
| `CAPX_HTTP_PORT` | Port for the web application (e.g. `3000`) |
| `CAPX_API_PORT` | Port for the API (e.g. `3001`) |
| `SEED_DUMMY_DATA` | Set to `TRUE` to seed dummy data on startup |
| `SUPERUSER_NAME` | Name of the superuser (required if seeding) |
| `SUPERUSER_USERNAME` | Username of the superuser (required if seeding) |
| `SUPERUSER_EMAIL` | Email of the superuser (required if seeding) |

The following variables need only be set when not using the "Local" solution configuration:

| Variable | Description |
|----------|-------------|
| `SENTRY_DSN` | The URL to which crash reports from the Sentry library are sent |
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

You can then access:
- The web application at `http://localhost:3000` (or your configured `CAPX_HTTP_PORT`)
- The API at `http://localhost:3000/api` (or your configured `CAPX_HTTP_PORT`)

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
3. CapX does not work properly in Safari on macOS when run from Docker.
