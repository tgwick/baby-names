# NameMatch Azure Deployment Guide

Step-by-step instructions to deploy NameMatch to Azure.

## Prerequisites

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) installed
- Logged into Azure (`az login`)
- GitHub repository with the `infra` branch

---

## Step 1: Create Azure Service Principal

This allows GitHub Actions to deploy to Azure.

```bash
# Get your subscription ID
az account show --query id -o tsv

# Create service principal (save the output!)
az ad sp create-for-rbac \
  --name "namematch-github-actions" \
  --role Contributor \
  --scopes /subscriptions/<YOUR_SUBSCRIPTION_ID> \
  --sdk-auth
```

**Save these values from the output:**

| Output Field | GitHub Secret Name |
|--------------|-------------------|
| `clientId` | `AZURE_CLIENT_ID` |
| `tenantId` | `AZURE_TENANT_ID` |
| `subscriptionId` | `AZURE_SUBSCRIPTION_ID` |

---

## Step 2: Configure GitHub OIDC

For passwordless authentication from GitHub Actions, create federated credentials:

```bash
# Set your App ID from Step 1
APP_ID=<clientId from step 1>

# Create federated credential for main branch
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<owner>/<repo>:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Create federated credential for develop branch
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-develop",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<owner>/<repo>:ref:refs/heads/develop",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Create federated credential for dev environment
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-env-dev",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<owner>/<repo>:environment:dev",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Create federated credential for prod environment
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-env-prod",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<owner>/<repo>:environment:prod",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

> **Note:** Replace `<owner>/<repo>` with your GitHub repository path (e.g., `tgwick/baby-names`).

---

## Step 3: Generate Secure Passwords

```bash
# Generate PostgreSQL admin password
openssl rand -base64 24

# Generate JWT signing key (minimum 32 characters)
openssl rand -base64 48
```

**Save these values securely - you'll need them in Step 4!**

---

## Step 4: Configure GitHub Secrets

Go to: **GitHub → Repository → Settings → Secrets and variables → Actions**

### Repository Secrets

Click **New repository secret** and add:

| Name | Value |
|------|-------|
| `AZURE_CLIENT_ID` | clientId from Step 1 |
| `AZURE_TENANT_ID` | tenantId from Step 1 |
| `AZURE_SUBSCRIPTION_ID` | subscriptionId from Step 1 |

### Environment Secrets

1. Click **Environments** in the left sidebar
2. Click **New environment**, name it `dev`, click **Configure environment**
3. Under **Environment secrets**, click **Add secret** and add:

| Name | Value |
|------|-------|
| `POSTGRES_ADMIN_PASSWORD` | PostgreSQL password from Step 3 |
| `JWT_KEY` | JWT key from Step 3 |

4. Repeat steps 2-3 for `prod` environment (use **different passwords** for production!)

---

## Step 5: Push Infrastructure Branch

Your GitHub Personal Access Token needs the `workflow` scope to push workflow files.

**Option A: Update PAT scope**
1. Go to GitHub → Settings → Developer settings → Personal access tokens
2. Edit your token and add the `workflow` scope
3. Push normally:
   ```bash
   git push origin infra
   ```

**Option B: Use SSH**
```bash
git remote set-url origin git@github.com:<owner>/<repo>.git
git push origin infra
```

---

## Step 6: Deploy Infrastructure

### Option A: Via GitHub Actions (Recommended)

1. Go to **Actions → Infrastructure Deployment**
2. Click **Run workflow**
3. Select:
   - Branch: `infra`
   - Environment: `dev`
   - Action: `plan`
4. Review the output to see what will be created
5. Run again with Action: `deploy`

### Option B: Via Azure CLI (Direct)

```bash
az deployment sub create \
  --name namematch-dev-$(date +%Y%m%d%H%M) \
  --location eastus \
  --template-file infra/bicep/main.bicep \
  --parameters environment=dev \
  --parameters postgresAdminPassword="<your-postgres-password>" \
  --parameters jwtKey="<your-jwt-key>"
```

---

## Step 7: Build and Push Docker Images

After infrastructure is deployed, push Docker images to Azure Container Registry:

```bash
# Login to Azure Container Registry
az acr login --name namematchdevacr

# Build and push backend
docker build -t namematchdevacr.azurecr.io/namematch-api:latest ./backend
docker push namematchdevacr.azurecr.io/namematch-api:latest

# Build and push frontend
docker build -t namematchdevacr.azurecr.io/namematch-web:latest ./frontend
docker push namematchdevacr.azurecr.io/namematch-web:latest
```

---

## Step 8: Verify Deployment

```bash
# Get the deployed URLs
BACKEND_URL=$(az containerapp show -n namematch-dev-api -g namematch-dev-rg \
  --query properties.configuration.ingress.fqdn -o tsv)
FRONTEND_URL=$(az containerapp show -n namematch-dev-web -g namematch-dev-rg \
  --query properties.configuration.ingress.fqdn -o tsv)

echo "Backend:  https://$BACKEND_URL"
echo "Frontend: https://$FRONTEND_URL"
echo "Swagger:  https://$BACKEND_URL/swagger"

# Test health endpoint
curl https://$BACKEND_URL/health
```

---

## Step 9: Enable CI/CD Auto-Deployment

Once infrastructure exists, merge to `develop` to enable automatic deployments:

```bash
git checkout develop
git merge infra
git push origin develop
```

The **CD Dev workflow** will now automatically build and deploy on every push to `develop`.

---

## Production Deployment

To deploy to production:

1. Ensure `prod` environment secrets are configured (Step 4)
2. Run Infrastructure Deployment workflow with `environment: prod`
3. Create a GitHub Release (e.g., `v1.0.0`)
4. The CD Prod workflow automatically promotes dev images to prod

---

## Troubleshooting

### View Container Logs

```bash
az containerapp logs show -n namematch-dev-api -g namematch-dev-rg --follow
```

### Check Deployment Status

```bash
az deployment sub list --query "[?contains(name,'namematch')]" -o table
```

### List Container App Revisions

```bash
az containerapp revision list -n namematch-dev-api -g namematch-dev-rg -o table
```

### Restart Container App

```bash
# Get current revision name
REVISION=$(az containerapp revision list -n namematch-dev-api -g namematch-dev-rg \
  --query "[0].name" -o tsv)

# Restart it
az containerapp revision restart -n namematch-dev-api -g namematch-dev-rg \
  --revision $REVISION
```

### Check Key Vault Access

```bash
az role assignment list \
  --scope /subscriptions/<sub-id>/resourceGroups/namematch-dev-rg/providers/Microsoft.KeyVault/vaults/namematch-dev-kv \
  -o table
```

### Delete and Recreate (Nuclear Option)

```bash
# Delete the entire resource group
az group delete --name namematch-dev-rg --yes --no-wait

# Re-run infrastructure deployment
```

---

## Cost Management

### Estimated Monthly Costs

| Environment | Cost | Notes |
|-------------|------|-------|
| Dev | ~$25/month | Scales to zero when idle |
| Prod | ~$175/month | Minimum 2 replicas always running |

### Stop Dev Environment (Save Costs)

```bash
# Scale to zero (stops billing for compute)
az containerapp update -n namematch-dev-api -g namematch-dev-rg \
  --min-replicas 0 --max-replicas 0
az containerapp update -n namematch-dev-web -g namematch-dev-rg \
  --min-replicas 0 --max-replicas 0
```

### Resume Dev Environment

```bash
az containerapp update -n namematch-dev-api -g namematch-dev-rg \
  --min-replicas 0 --max-replicas 3
az containerapp update -n namematch-dev-web -g namematch-dev-rg \
  --min-replicas 0 --max-replicas 3
```
