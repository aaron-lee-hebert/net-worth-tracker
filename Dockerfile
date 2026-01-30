# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Directory.Build.props Directory.Packages.props ./
COPY src/NetWorthTracker.Core/NetWorthTracker.Core.csproj src/NetWorthTracker.Core/
COPY src/NetWorthTracker.Application/NetWorthTracker.Application.csproj src/NetWorthTracker.Application/
COPY src/NetWorthTracker.Infrastructure/NetWorthTracker.Infrastructure.csproj src/NetWorthTracker.Infrastructure/
COPY src/NetWorthTracker.Web/NetWorthTracker.Web.csproj src/NetWorthTracker.Web/

# Restore dependencies
RUN dotnet restore src/NetWorthTracker.Web/NetWorthTracker.Web.csproj

# Copy source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/NetWorthTracker.Web/NetWorthTracker.Web.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install ICU libraries for proper globalization support (currency symbols, etc.)
RUN apt-get update && apt-get install -y --no-install-recommends \
    locales \
    && rm -rf /var/lib/apt/lists/* \
    && sed -i '/en_US.UTF-8/s/^# //g' /etc/locale.gen \
    && locale-gen

# Create data directory for SQLite database
RUN mkdir -p /app/data

# Copy published application
COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV LANG=en_US.UTF-8
ENV LC_ALL=en_US.UTF-8

EXPOSE 8080

ENTRYPOINT ["dotnet", "NetWorthTracker.Web.dll"]
