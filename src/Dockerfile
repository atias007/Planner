#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# ---------------------------- Base Image -------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# ---------------------------- Build Image -------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["DatabaseMigrations/DatabaseMigrations.csproj", "DatabaseMigrations/"]
COPY ["DatabaseMigrations.Factory/DatabaseMigrations.Factory.csproj", "DatabaseMigrations.Factory/"]
COPY ["DbUp/dbup-core/dbup-core.csproj", "DbUp/dbup-core/"]
COPY ["DbUp/dbup-mysql/dbup-mysql.csproj", "DbUp/dbup-mysql/"]
COPY ["DbUp/dbup-postgresql/dbup-postgresql.csproj", "DbUp/dbup-postgresql/"]
COPY ["DbUp/dbup-sqlserver/dbup-sqlserver.csproj", "DbUp/dbup-sqlserver/"]
COPY ["Hooks/Planar.Hooks.csproj", "Hooks/"]
COPY ["Jobs/CommonJob/CommonJob.csproj", "Jobs/CommonJob/"]
COPY ["Jobs/PlanarJob/PlanarJob.csproj", "Jobs/PlanarJob/"]
COPY ["Jobs/ProcessJob/ProcessJob.csproj", "Jobs/ProcessJob/"]
COPY ["Jobs/SqlJob/SqlJob.csproj", "Jobs/SqlJob/"]
COPY ["Jobs/SqlTableReportJob/SqlTableReportJob.csproj", "Jobs/SqlTableReportJob/"]
COPY ["Jobs/RestJob/RestJob.csproj", "Jobs/RestJob/"]
COPY ["Jobs/PowerShellJob/PowerShellJob.csproj", "Jobs/PowerShellJob/"]
COPY ["Nager.Date/Nager.Date.csproj", "Nager.Date/"]
COPY ["Planar/Planar.csproj", "Planar/"]
COPY ["Planar.API.Common/Planar.API.Common.csproj", "Planar.API.Common/"]
COPY ["Planar.CLI/Planar.CLI.csproj", "Planar.CLI/"]
COPY ["Planar.Common/Planar.Common.csproj", "Planar.Common/"]
COPY ["Planar.Service/Planar.Service.csproj", "Planar.Service/"]

RUN dotnet restore "Planar/Planar.csproj"
RUN dotnet restore "Planar.CLI/Planar.CLI.csproj"
COPY . .
WORKDIR "/src/Planar"
RUN dotnet build "Planar.csproj" -c Release -o /app/build
WORKDIR "/src/Planar.CLI"
RUN dotnet build "Planar.CLI.csproj" -c Release -o /app/build
WORKDIR "/src/Planar"

FROM build AS publish
RUN dotnet publish "../Planar.CLI/Planar.CLI.csproj" -c Release -o /app/publish /p:UseAppHost=false
RUN dotnet publish "Planar.csproj" -c Release -o /app/publish /p:UseAppHost=false
COPY ["Planar.CLI/planar-cli", "/app/publish"]

# ---------------------------- Final Image -------------------------------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# ---------------------------- Health Check -------------------------------------------
HEALTHCHECK --interval=5m --timeout=3s --start-period=30s CMD curl --fail http://localhost/service/healthcheck || exit

# ---------------------------- Entry Point -------------------------------------------
ENV PATH="/app:${PATH}"
ENTRYPOINT ["dotnet", "Planar.dll"]