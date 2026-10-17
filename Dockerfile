# ==============================================================================
# Production Dockerfile for Modern Enterprise ERP (.NET 10 Web API)
# Multi-stage, secure non-root execution, minimal attack surface
# ==============================================================================

# --- Stage 1: Build & Restore ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project definitions for optimized caching
COPY ERP.slnx ./
COPY src/ERP.Common.Domain/ERP.Common.Domain.csproj src/ERP.Common.Domain/
COPY src/ERP.Common.Application/ERP.Common.Application.csproj src/ERP.Common.Application/
COPY src/ERP.Common.Infrastructure/ERP.Common.Infrastructure.csproj src/ERP.Common.Infrastructure/
COPY src/ERP.Web.Api/ERP.Web.Api.csproj src/ERP.Web.Api/
COPY tests/ERP.UnitTests/ERP.UnitTests.csproj tests/ERP.UnitTests/

# Restore dependencies
RUN dotnet restore src/ERP.Web.Api/ERP.Web.Api.csproj

# Copy complete source code
COPY src/ src/
COPY tests/ tests/

# Build Web API in Release mode
WORKDIR /src/src/ERP.Web.Api
RUN dotnet build ERP.Web.Api.csproj -c Release --no-restore

# --- Stage 2: Publish ---
FROM build AS publish
RUN dotnet publish ERP.Web.Api.csproj -c Release -o /app/publish /p:UseAppHost=false --no-build

# --- Stage 3: Runtime & Production Hardening ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for container health check probe
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Create secure, non-privileged system user and group
RUN groupadd -g 10001 erpgroup && \
    useradd -u 10001 -g erpgroup -s /bin/false -m appuser

# Copy published application artifacts
COPY --from=publish /app/publish .

# Set file permissions for unprivileged user
RUN chown -R appuser:erpgroup /app

# Switch to non-root user
USER appuser

# Configure environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

# Health check probe against live/ready endpoint
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "ERP.Web.Api.dll"]
