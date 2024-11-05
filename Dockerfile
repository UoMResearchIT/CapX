# Based on https://github.com/abmdev86/blazor-server-docker/tree/bb8e4fe2ce95863f9bfa257f4aa56217830b76a2

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .

ARG GITHUB_USERNAME
ARG GITHUB_PASSWORD

RUN dotnet nuget update source UoMResearchITGitHub --username "$GITHUB_USERNAME" --password "$GITHUB_PASSWORD" --store-password-in-clear-text

RUN dotnet restore "PPMTool/PPMTool.sln"
RUN dotnet tool restore
RUN dotnet ef database update -p "PPMTool/PPMTool.csproj"

FROM build AS publish
RUN dotnet publish -c Local -o /app/publish -f net6.0 "PPMTool/PPMTool.sln"
RUN cp PPMTool/PPMTool.db /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PPMTool.dll"]
