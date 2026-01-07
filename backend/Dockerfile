# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for layer caching (excluding tests)
COPY backend/NameMatch.Domain/NameMatch.Domain.csproj ./NameMatch.Domain/
COPY backend/NameMatch.Application/NameMatch.Application.csproj ./NameMatch.Application/
COPY backend/NameMatch.Infrastructure/NameMatch.Infrastructure.csproj ./NameMatch.Infrastructure/
COPY backend/NameMatch.Api/NameMatch.Api.csproj ./NameMatch.Api/

# Restore dependencies for API project (includes transitive dependencies)
RUN dotnet restore NameMatch.Api/NameMatch.Api.csproj

# Copy source code (excluding tests via .dockerignore)
COPY backend/NameMatch.Domain/ ./NameMatch.Domain/
COPY backend/NameMatch.Application/ ./NameMatch.Application/
COPY backend/NameMatch.Infrastructure/ ./NameMatch.Infrastructure/
COPY backend/NameMatch.Api/ ./NameMatch.Api/

# Copy seed data
COPY data/ssa-names/processed-names.json ./data/processed-names.json

# Build and publish release
RUN dotnet publish NameMatch.Api/NameMatch.Api.csproj \
    -c Release \
    -o /app/publish

# ============================================
# Stage 2: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN adduser --disabled-password --gecos '' --uid 1000 appuser

# Copy published application
COPY --from=build /app/publish .

# Copy seed data for database initialization
COPY --from=build /src/data/processed-names.json /app/data/processed-names.json

# Set permissions
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Expose port (Azure Container Apps expects 8080)
EXPOSE 8080

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "NameMatch.Api.dll"]
