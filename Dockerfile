# Based on https://github.com/abmdev86/blazor-server-docker/tree/bb8e4fe2ce95863f9bfa257f4aa56217830b76a2

OK this now seems to work. Is this OK? FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIG=Local
WORKDIR /src
COPY nuget.config nuget.config

COPY PPMTool/PPMTool.csproj PPMTool/PPMTool.csproj
COPY PPMTool/PPMTool.sln PPMTool/PPMTool.sln
COPY PPMTool.Tests/PPMTool.Tests.csproj PPMTool.Tests/PPMTool.Tests.csproj

# Initial restore
RUN dotnet nuget locals all --clear \
 && dotnet restore "PPMTool/PPMTool.sln" -p:Configuration=${BUILD_CONFIG}

# Tool restore
COPY .config .config
RUN dotnet tool restore

# Copy full sources
COPY PPMTool PPMTool
COPY .git .git

# Second restore needed for .NET 10 EF tools but don't know why
RUN dotnet restore "PPMTool/PPMTool.csproj" -p:Configuration=${BUILD_CONFIG}

# Build here, EF will reuse it
RUN dotnet build "PPMTool/PPMTool.csproj" -c ${BUILD_CONFIG} --no-restore

# Create the database by running migrations
# The following are required at design time
ENV CONNECTION_STRING="Data Source=/src/PPMTool/PPMTool.db;Cache=Shared;Mode=ReadWriteCreate;" \
    SUPERUSER_NAME="Captain Marvel" \
    SUPERUSER_USERNAME=c123456m \
    SUPERUSER_EMAIL=captain.marvel@manchester.ac.uk

RUN dotnet ef database update -p "PPMTool/PPMTool.csproj" --configuration ${BUILD_CONFIG} --no-build

FROM build AS publish
ARG BUILD_CONFIG=Local
# Publish only the main projects (not the test projects) to avoid assembly conflicts
RUN dotnet publish -c ${BUILD_CONFIG} -o /app/publish -f net10.0 "PPMTool/PPMTool.csproj"
RUN mkdir /app/publish/state
RUN cp PPMTool/PPMTool.db /app/publish/state
VOLUME /app/publish/state
RUN ln -s state/PPMTool.db /app/publish/PPMTool.db
# Copy migration data files needed for runtime seeding (SEED_DUMMY_DATA=TRUE)
RUN mkdir -p /app/publish/Migrations && cp -r PPMTool/Migrations/Data /app/publish/Migrations/

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .