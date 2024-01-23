# Based on https://github.com/abmdev86/blazor-server-docker/tree/bb8e4fe2ce95863f9bfa257f4aa56217830b76a2

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["PPMTool.csproj", "."]
RUN dotnet restore "PPMTool.csproj"
COPY . .
RUN dotnet build "PPMTool.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "PPMTool.csproj" -c Debug -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PPMTool.dll"]
