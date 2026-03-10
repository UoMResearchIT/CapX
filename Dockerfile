# Based on https://github.com/abmdev86/blazor-server-docker/tree/bb8e4fe2ce95863f9bfa257f4aa56217830b76a2

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
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

RUN dotnet restore "PPMTool/PPMTool.sln"

COPY .config .config
RUN dotnet tool restore

COPY PPMTool PPMTool
COPY .git .git

# Create the database by running migrations
# The CONNECTION_STRING env var is required by the DesignTimeDbContextFactory
ENV CONNECTION_STRING="Data Source=/src/PPMTool/PPMTool.db;Cache=Shared;Mode=ReadWriteCreate;"
RUN dotnet ef database update -p "PPMTool/PPMTool.csproj"

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
