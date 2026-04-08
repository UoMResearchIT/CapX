# Based on https://github.com/abmdev86/blazor-server-docker/tree/bb8e4fe2ce95863f9bfa257f4aa56217830b76a2

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIG=Local
WORKDIR /src
COPY nuget.config nuget.config

COPY PPMTool/PPMTool.sln PPMTool/PPMTool.sln
COPY PPMTool/PPMTool.csproj PPMTool/PPMTool.csproj
COPY PPMTool.Tests/PPMTool.Tests.csproj PPMTool.Tests/PPMTool.Tests.csproj
COPY PPMTool.Data/PPMTool.Data.csproj PPMTool.Data/PPMTool.Data.csproj
COPY PPMTool.Migrations.Sqlite/PPMTool.Migrations.Sqlite.csproj PPMTool.Migrations.Sqlite/PPMTool.Migrations.Sqlite.csproj
COPY PPMTool.Migrations.SqlServer/PPMTool.Migrations.SqlServer.csproj PPMTool.Migrations.SqlServer/PPMTool.Migrations.SqlServer.csproj
COPY PPMTool.Migrations.PostgreSql/PPMTool.Migrations.PostgreSql.csproj PPMTool.Migrations.PostgreSql/PPMTool.Migrations.PostgreSql.csproj

# Restore packages
RUN dotnet nuget locals all --clear \
 && dotnet restore "PPMTool/PPMTool.sln" -p:Configuration=${BUILD_CONFIG}

# Copy full sources (inc. git for GitInfo library)
COPY PPMTool PPMTool
COPY PPMTool.Data PPMTool.Data
COPY PPMTool.Migrations.Sqlite PPMTool.Migrations.Sqlite
COPY PPMTool.Migrations.SqlServer PPMTool.Migrations.SqlServer
COPY PPMTool.Migrations.PostgreSql PPMTool.Migrations.PostgreSql
COPY .git .git

# Second restore needed for .NET 10 EF tools but don't know why
RUN dotnet restore "PPMTool/PPMTool.csproj" -p:Configuration=${BUILD_CONFIG}

# Build app
RUN dotnet build "PPMTool/PPMTool.csproj" -c ${BUILD_CONFIG} --no-restore

# Publish
FROM build AS publish
ARG BUILD_CONFIG=Local

# Publish only the main projects (not the test projects) to avoid assembly conflicts
RUN dotnet publish -c ${BUILD_CONFIG} -o /app/publish -f net10.0 "PPMTool/PPMTool.csproj"

# Runtime state directory volume mount
RUN mkdir -p /app/publish/state

# App expects DB at /app/PPMTool.db so create symlink
RUN ln -s state/PPMTool.db /app/publish/PPMTool.db

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .