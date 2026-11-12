# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so the layer is cached while only sources change
COPY Directory.Build.props Payroll.sln ./
COPY src/Payroll.Domain/Payroll.Domain.csproj src/Payroll.Domain/
COPY src/Payroll.Infrastructure/Payroll.Infrastructure.csproj src/Payroll.Infrastructure/
COPY src/Payroll.Web/Payroll.Web.csproj src/Payroll.Web/
COPY tests/Payroll.Tests/Payroll.Tests.csproj tests/Payroll.Tests/
RUN dotnet restore Payroll.sln

COPY . .
RUN dotnet test tests/Payroll.Tests/Payroll.Tests.csproj -c Release --no-restore
RUN dotnet publish src/Payroll.Web/Payroll.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Payroll="Data Source=/app/App_Data/payroll.db"

COPY --from=build /app/publish .
RUN mkdir -p /app/App_Data && chown -R app:app /app/App_Data
USER app

VOLUME ["/app/App_Data"]
EXPOSE 8080
ENTRYPOINT ["dotnet", "Payroll.Web.dll"]
