# NameMatch Infrastructure Guide

This document explains the infrastructure setup for NameMatch and how to deploy and test it.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Local Development](#local-development)
- [Azure Infrastructure](#azure-infrastructure)
- [CI/CD Pipelines](#cicd-pipelines)
- [Deployment Guide](#deployment-guide)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Azure (per environment)                      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────────┐                      ┌───────────────────┐    │
│  │  Container Apps  │                      │    PostgreSQL     │    │
│  │   Environment    │                      │  Flexible Server  │    │
│  └────────┬─────────┘                      └───────────────────┘    │
│           │                                          ▲              │
│  ┌────────┴────────┐                                 │              │
│  │                 │                                 │              │
│  ▼                 ▼                                 │              │
│ ┌─────────┐   ┌─────────┐                           │              │
│ │ Backend │   │Frontend │                           │              │
│ │  (API)  │──▶│ (nginx) │                           │              │
│ └────┬────┘   └─────────┘                           │              │
│      │                                               │              │
│      └───────────────────────────────────────────────┘              │
│                                                                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────┐ │
│  │     ACR     │  │  Key Vault  │  │ Log Analytics│  │App Insights│ │
│  │  (images)   │  │  (secrets)  │  │   (logs)    │  │(monitoring)│ │
│  └─────────────┘  └─────────────┘  └─────────────┘  └────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

### Components

| Component | Technology | Purpose |
|-----------|------------|---------|
| Backend | .NET 8 API in Container Apps | REST API, authentication, business logic |
| Frontend | Vue 3 + nginx in Container Apps | SPA served via nginx with API proxy |
| Database | PostgreSQL Flexible Server | Data persistence |
| Registry | Azure Container Registry | Docker image storage |
| Secrets | Azure Key Vault | Secure storage for passwords and keys |
| Monitoring | Application Insights + Log Analytics | Logging, metrics, tracing |

### Environments

| Environment | Trigger | URL Pattern | Scaling |
|-------------|---------|-------------|---------|
| Dev | Push to `develop` | `namematch-dev-*.azurecontainerapps.io` | 0-3 replicas |
| Prod | GitHub Release | `namematch-prod-*.azurecontainerapps.io` | 2-10 replicas |

---

## Local Development

### Prerequisites

- Docker Desktop
- .NET 8 SDK (for non-Docker development)
- Node.js 22+ (for non-Docker development)

### Option 1: Full Docker Stack (Recommended)

Run the entire stack in containers:

```bash
# Start all services (PostgreSQL, backend, frontend)
docker-compose up --build

# Access the app
# Frontend: http://localhost:5173
# Backend:  http://localhost:5001
# API docs: http://localhost:5001/swagger
```

### Option 2: Docker with Hot Reload

For development with live code reloading:

```bash
# Start with hot reload (uses docker-compose.override.yml automatically)
docker-compose up

# This enables:
# - Backend: dotnet watch with live reload
# - Frontend: Vite dev server with HMR
```

### Option 3: Hybrid (Docker DB + Local Apps)

Run only PostgreSQL in Docker:

```bash
# Start just the database
docker-compose up postgres

# In another terminal - run backend
cd backend
dotnet run --project NameMatch.Api

# In another terminal - run frontend
cd frontend
npm install
npm run dev
```

### Stopping Services

```bash
# Stop all containers
docker-compose down

# Stop and remove volumes (resets database)
docker-compose down -v
```

---

## Azure Infrastructure

### Resource Naming Convention

All resources follow the pattern: `namematch-{environment}-{resource}`

| Resource | Dev Name | Prod Name |
|----------|----------|-----------|
| Resource Group | `namematch-dev-rg` | `namematch-prod-rg` |
| Container Registry | `namematchdevacr` | `namematchprodacr` |
| Key Vault | `namematch-dev-kv` | `namematch-prod-kv` |
| PostgreSQL | `namematch-dev-pg` | `namematch-prod-pg` |
| Backend App | `namematch-dev-api` | `namematch-prod-api` |
| Frontend App | `namematch-dev-web` | `namematch-prod-web` |

### Bicep Modules

```
infra/bicep/
├── main.bicep                    # Orchestrator (subscription scope)
└── modules/
    ├── log-analytics.bicep       # Log Analytics workspace
    ├── app-insights.bicep        # Application Insights
    ├── container-registry.bicep  # Azure Container Registry
    ├── key-vault.bicep           # Key Vault with secrets
    ├── postgresql.bicep          # PostgreSQL Flexible Server
    ├── container-apps-environment.bicep  # Container Apps env
    ├── container-app-backend.bicep       # Backend container app
    ├── container-app-frontend.bicep      # Frontend container app
    └── acr-role-assignment.bicep         # ACR pull permissions
```

### Cost Estimates

| Environment | Monthly Cost | Notes |
|-------------|--------------|-------|
| Dev | ~$25 | Scales to zero when idle |
| Prod | ~$175 | Min 2 replicas always running |

---

## CI/CD Pipelines

### Workflow Overview

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   ci.yml    │     │ cd-dev.yml  │     │ cd-prod.yml │
│             │     │             │     │             │
│ Tests on    │     │ Deploy on   │     │ Deploy on   │
│ PR/push     │     │ develop     │     │ release     │
└─────────────┘     └─────────────┘     └─────────────┘
                           │                   │
                           ▼                   ▼
                    ┌─────────────┐     ┌─────────────┐
                    │  Dev Env    │────▶│  Prod Env   │
                    │             │     │ (promoted)  │
                    └─────────────┘     └─────────────┘
```

### CI Workflow (`ci.yml`)

**Triggers:** Push to `main`/`develop`, Pull Requests to `main`

**Jobs:**
1. **backend-test** - .NET build and xUnit tests with PostgreSQL
2. **frontend-test** - TypeScript check, Vitest unit tests, production build
3. **e2e-test** - Playwright tests (PR only)
4. **docker-build** - Verify Docker images build successfully

### CD Dev Workflow (`cd-dev.yml`)

**Trigger:** Push to `develop` branch

**Steps:**
1. Build Docker images with commit SHA tag
2. Push to dev ACR
3. Deploy to dev Container Apps
4. Verify health endpoints

### CD Prod Workflow (`cd-prod.yml`)

**Trigger:** GitHub Release published

**Steps:**
1. Promote images from dev ACR to prod ACR
2. Deploy to prod Container Apps
3. Verify health endpoints

### Infrastructure Workflow (`infra.yml`)

**Trigger:** Manual (workflow_dispatch)

**Inputs:**
- `environment`: dev or prod
- `action`: plan (preview) or deploy

---

## Deployment Guide

### Step 1: Create Azure Service Principal

```bash
# Login to Azure
az login

# Create service principal with Contributor role
az ad sp create-for-rbac \
  --name "namematch-github-actions" \
  --role Contributor \
  --scopes /subscriptions/{subscription-id} \
  --sdk-auth

# Note the output - you'll need:
# - clientId (AZURE_CLIENT_ID)
# - tenantId (AZURE_TENANT_ID)
# - subscriptionId (AZURE_SUBSCRIPTION_ID)
```

### Step 2: Configure GitHub Secrets

Go to **Repository → Settings → Secrets and variables → Actions**

**Repository Secrets:**
| Secret | Value |
|--------|-------|
| `AZURE_CLIENT_ID` | Service principal client ID |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |

**Environment Secrets** (create `dev` and `prod` environments):
| Secret | Value |
|--------|-------|
| `POSTGRES_ADMIN_PASSWORD` | Strong password for PostgreSQL |
| `JWT_KEY` | Random string, minimum 32 characters |

Generate secure values:
```bash
# Generate PostgreSQL password
openssl rand -base64 24

# Generate JWT key
openssl rand -base64 48
```

### Step 3: Deploy Infrastructure

1. Go to **Actions → Infrastructure Deployment**
2. Click **Run workflow**
3. Select:
   - Environment: `dev`
   - Action: `plan` (first time, to preview)
4. Review the output
5. Run again with Action: `deploy`

### Step 4: First Application Deployment

After infrastructure is deployed:

```bash
# Push to develop to trigger deployment
git checkout develop
git merge infra
git push origin develop
```

Or manually trigger the CD Dev workflow.

### Step 5: Deploy to Production

1. Create a GitHub Release with a version tag (e.g., `v1.0.0`)
2. The CD Prod workflow will automatically:
   - Promote images from dev to prod
   - Deploy to production

---

## Testing

### Local Testing

#### Test Docker Build

```bash
# Build images without running
docker-compose build

# Verify images were created
docker images | grep namematch
```

#### Test Full Stack

```bash
# Start the stack
docker-compose up -d

# Wait for services to be healthy
docker-compose ps

# Test backend health
curl http://localhost:5001/health

# Test frontend
curl http://localhost:5173

# View logs
docker-compose logs -f backend
docker-compose logs -f frontend
```

#### Run Backend Tests

```bash
cd backend
dotnet test
```

#### Run Frontend Tests

```bash
cd frontend
npm run test:run      # Unit tests
npm run e2e           # E2E tests (requires backend running)
```

### Testing Infrastructure (Without Deploying)

#### Validate Bicep Templates

```bash
# Check for syntax errors
az bicep build --file infra/bicep/main.bicep --stdout > /dev/null
echo "Bicep is valid!"

# Preview what would be created (what-if)
az deployment sub what-if \
  --location eastus \
  --template-file infra/bicep/main.bicep \
  --parameters environment=dev \
  --parameters postgresAdminPassword="TestPassword123!" \
  --parameters jwtKey="TestKeyThatIsAtLeast32CharactersLong!"
```

#### Use the Deployment Script

```bash
# Preview dev deployment
./infra/scripts/deploy.sh -e dev -a plan

# Preview prod deployment
./infra/scripts/deploy.sh -e prod -a plan
```

### Testing Deployed Environment

After deployment, test the live environment:

```bash
# Get the URLs from Azure
BACKEND_URL=$(az containerapp show -n namematch-dev-api -g namematch-dev-rg --query properties.configuration.ingress.fqdn -o tsv)
FRONTEND_URL=$(az containerapp show -n namematch-dev-web -g namematch-dev-rg --query properties.configuration.ingress.fqdn -o tsv)

# Test backend health
curl https://$BACKEND_URL/health

# Test API endpoint
curl https://$BACKEND_URL/api/health

# Open frontend in browser
open https://$FRONTEND_URL
```

---

## Troubleshooting

### Docker Issues

**Container won't start:**
```bash
# Check logs
docker-compose logs backend
docker-compose logs frontend

# Rebuild from scratch
docker-compose down -v
docker-compose build --no-cache
docker-compose up
```

**Database connection failed:**
```bash
# Ensure PostgreSQL is healthy
docker-compose ps postgres

# Check PostgreSQL logs
docker-compose logs postgres

# Connect directly to verify
docker-compose exec postgres psql -U postgres -d namematch
```

### Azure Deployment Issues

**Infrastructure deployment failed:**
```bash
# Check deployment status
az deployment sub show \
  --name namematch-dev-{run-number} \
  --query properties.error

# View detailed logs in Azure Portal:
# Subscriptions → Deployments → Select deployment → Operation details
```

**Container App not starting:**
```bash
# Check container logs
az containerapp logs show \
  -n namematch-dev-api \
  -g namematch-dev-rg \
  --follow

# Check revision status
az containerapp revision list \
  -n namematch-dev-api \
  -g namematch-dev-rg \
  --output table
```

**Key Vault access denied:**
```bash
# Verify managed identity has access
az role assignment list \
  --scope /subscriptions/{sub}/resourceGroups/namematch-dev-rg/providers/Microsoft.KeyVault/vaults/namematch-dev-kv \
  --output table
```

### GitHub Actions Issues

**Workflow fails with "Azure Login failed":**
- Verify `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` are set correctly
- Ensure federated credentials are configured for the service principal

**Image push to ACR fails:**
- Verify the Container Registry exists
- Check that the managed identity has `AcrPush` role

### Database Migration Issues

**EF Core migrations not applied:**
```bash
# Connect to the running backend container
docker-compose exec backend sh

# Apply migrations manually
dotnet ef database update --project NameMatch.Infrastructure --startup-project NameMatch.Api
```

---

## Quick Reference

### Useful Commands

```bash
# Local Development
docker-compose up                    # Start all services
docker-compose down -v               # Stop and reset
docker-compose logs -f backend       # View backend logs

# Azure CLI
az login                             # Login to Azure
az containerapp logs show -n namematch-dev-api -g namematch-dev-rg --follow
az deployment sub list --query "[?contains(name,'namematch')]"

# Bicep
az bicep build --file infra/bicep/main.bicep  # Validate
./infra/scripts/deploy.sh -e dev -a plan      # Preview deployment

# Testing
cd backend && dotnet test            # Backend tests
cd frontend && npm run test:run      # Frontend unit tests
cd frontend && npm run e2e           # E2E tests
```

### Environment URLs

| Environment | Frontend | Backend | Swagger |
|-------------|----------|---------|---------|
| Local | http://localhost:5173 | http://localhost:5001 | http://localhost:5001/swagger |
| Dev | https://namematch-dev-web.*.azurecontainerapps.io | https://namematch-dev-api.*.azurecontainerapps.io | /swagger |
| Prod | https://namematch-prod-web.*.azurecontainerapps.io | https://namematch-prod-api.*.azurecontainerapps.io | N/A |

### Health Endpoints

| Endpoint | Purpose |
|----------|---------|
| `/health` | Full health check with database |
| `/health/live` | Liveness probe (always healthy) |
| `/health/ready` | Readiness probe (checks dependencies) |
