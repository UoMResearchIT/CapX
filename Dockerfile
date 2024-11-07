# Based on https://github.com/abmdev86/blazor-server-docker/tree/bb8e4fe2ce95863f9bfa257f4aa56217830b76a2

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY nuget.config nuget.config

ARG GITHUB_TOKEN

RUN dotnet nuget update source UoMResearchITGitHub --username "xxx-unused" --password "$GITHUB_TOKEN" --store-password-in-clear-text

COPY Blazor-ApexCharts Blazor-ApexCharts
COPY PPMTool/PPMTool.csproj PPMTool/PPMTool.csproj
COPY PPMTool/PPMTool.sln PPMTool/PPMTool.sln

RUN dotnet restore "PPMTool/PPMTool.sln"

COPY .config .config
RUN dotnet tool restore

COPY PPMTool PPMTool
RUN dotnet ef database update -p "PPMTool/PPMTool.csproj"

FROM build AS publish
RUN dotnet publish -c Local -o /app/publish -f net6.0 "PPMTool/PPMTool.sln"
RUN mkdir /app/publish/state
RUN cp PPMTool/PPMTool.db /app/publish/state
VOLUME /app/publish/state
RUN ln -s state/PPMTool.db /app/publish/PPMTool.db

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PPMTool.dll"]
