# Cloudflare DNS Setup for hatchaname.com

This guide walks through configuring Cloudflare as the DNS provider for `hatchaname.com` (registered at Namecheap) and connecting it to Azure Container Apps.

## Domain Structure

| Environment | Domain | Target |
|-------------|--------|--------|
| **Production** | `hatchaname.com` | Frontend |
| **Production** | `www.hatchaname.com` | Frontend |
| **Production** | `api.hatchaname.com` | Backend API |
| **Dev** | `dev.hatchaname.com` | Frontend |
| **Dev** | `api-dev.hatchaname.com` | Backend API |

## Prerequisites

- Domain registered at Namecheap: `hatchaname.com`
- Azure Container Apps deployed (frontend and backend) for both dev and prod
- Azure CLI installed and authenticated

---

## Step 1: Create a Cloudflare Account

1. Go to [cloudflare.com](https://www.cloudflare.com)
2. Click **Sign Up**
3. Enter your email and create a password
4. Verify your email

---

## Step 2: Add Your Domain to Cloudflare

1. Once logged in, click **"Add a site"** (or **"Add site"** button)
2. Enter: `hatchaname.com`
3. Click **Continue**
4. Select the **Free** plan
5. Click **Continue**

Cloudflare will scan for existing DNS records (there probably won't be any yet, which is fine).

---

## Step 3: Get Cloudflare Nameservers

After the scan, Cloudflare will display **two nameservers** like:

```
eric.ns.cloudflare.com
lisa.ns.cloudflare.com
```

> **Note**: Your actual nameserver names will be different. Copy these down - you'll need them for the next step.

---

## Step 4: Update Nameservers at Namecheap

1. Log in to [Namecheap](https://www.namecheap.com)
2. Go to **Dashboard** → **Domain List**
3. Click **Manage** next to `hatchaname.com`
4. Find the **Nameservers** section
5. Change from "Namecheap BasicDNS" to **"Custom DNS"**
6. Enter the two Cloudflare nameservers you copied
7. Click the **green checkmark** to save

---

## Step 5: Verify in Cloudflare

1. Go back to Cloudflare
2. Click **"Done, check nameservers"**
3. Cloudflare will verify the nameserver update

> **Note**: Propagation can take **15 minutes to 24 hours**, but usually completes within 1-2 hours. Cloudflare will email you when your domain is active.

---

## Step 6: Get Azure Container Apps FQDNs

Run these commands to get your Azure Container Apps hostnames:

```bash
# Production FQDNs
az containerapp show -n namematch-prod-web -g namematch-prod-rg --query "properties.configuration.ingress.fqdn" -o tsv
az containerapp show -n namematch-prod-api -g namematch-prod-rg --query "properties.configuration.ingress.fqdn" -o tsv

# Dev FQDNs
az containerapp show -n namematch-dev-web -g namematch-dev-rg --query "properties.configuration.ingress.fqdn" -o tsv
az containerapp show -n namematch-dev-api -g namematch-dev-rg --query "properties.configuration.ingress.fqdn" -o tsv
```

Example output:
```
# Production
namematch-prod-web.proudocean-abc12345.eastus.azurecontainerapps.io
namematch-prod-api.proudocean-abc12345.eastus.azurecontainerapps.io

# Dev
namematch-dev-web.proudocean-abc12345.eastus.azurecontainerapps.io
namematch-dev-api.proudocean-abc12345.eastus.azurecontainerapps.io
```

---

## Step 7: Configure DNS Records in Cloudflare

Once your domain is active, go to **DNS** → **Records** in Cloudflare and add all records:

### Production Records

| Type | Name | Target | Proxy Status | TTL |
|------|------|--------|--------------|-----|
| CNAME | `@` | `<prod-frontend-fqdn>` | DNS only (grey cloud) | Auto |
| CNAME | `www` | `<prod-frontend-fqdn>` | DNS only (grey cloud) | Auto |
| CNAME | `api` | `<prod-backend-fqdn>` | DNS only (grey cloud) | Auto |

### Dev Records

| Type | Name | Target | Proxy Status | TTL |
|------|------|--------|--------------|-----|
| CNAME | `dev` | `<dev-frontend-fqdn>` | DNS only (grey cloud) | Auto |
| CNAME | `api-dev` | `<dev-backend-fqdn>` | DNS only (grey cloud) | Auto |

### Example (all records):

| Type | Name | Target | Proxy Status |
|------|------|--------|--------------|
| CNAME | `@` | `namematch-prod-web.proudocean-abc12345.eastus.azurecontainerapps.io` | DNS only |
| CNAME | `www` | `namematch-prod-web.proudocean-abc12345.eastus.azurecontainerapps.io` | DNS only |
| CNAME | `api` | `namematch-prod-api.proudocean-abc12345.eastus.azurecontainerapps.io` | DNS only |
| CNAME | `dev` | `namematch-dev-web.proudocean-abc12345.eastus.azurecontainerapps.io` | DNS only |
| CNAME | `api-dev` | `namematch-dev-api.proudocean-abc12345.eastus.azurecontainerapps.io` | DNS only |

> **Important**: Start with "DNS only" (grey cloud icon) until Azure SSL certificates are configured. You can enable proxying (orange cloud) later for CDN benefits.

---

## Step 8: Add Custom Domains to Azure Container Apps

Custom domains are configured via Bicep infrastructure-as-code. You can either redeploy with the `enableCustomDomains` parameter or run manual Azure CLI commands.

### Option A: Bicep Deployment (recommended)

After DNS records are configured and propagated, redeploy with custom domains enabled:

```bash
# Deploy dev with custom domains
az deployment sub create \
  --location eastus \
  --template-file infra/bicep/main.bicep \
  --parameters environment=dev \
  --parameters enableCustomDomains=true \
  --parameters postgresAdminPassword="$POSTGRES_PASSWORD" \
  --parameters jwtKey="$JWT_KEY"

# Deploy prod with custom domains
az deployment sub create \
  --location eastus \
  --template-file infra/bicep/main.bicep \
  --parameters environment=prod \
  --parameters enableCustomDomains=true \
  --parameters postgresAdminPassword="$POSTGRES_PASSWORD" \
  --parameters jwtKey="$JWT_KEY"
```

### Option B: Manual Azure CLI commands

#### Production Frontend (hatchaname.com, www.hatchaname.com)

```bash
# Add hostnames
az containerapp hostname add \
  --name namematch-prod-web \
  --resource-group namematch-prod-rg \
  --hostname hatchaname.com

az containerapp hostname add \
  --name namematch-prod-web \
  --resource-group namematch-prod-rg \
  --hostname www.hatchaname.com

# Bind managed SSL certificates
az containerapp hostname bind \
  --name namematch-prod-web \
  --resource-group namematch-prod-rg \
  --hostname hatchaname.com \
  --environment namematch-prod-env \
  --validation-method CNAME

az containerapp hostname bind \
  --name namematch-prod-web \
  --resource-group namematch-prod-rg \
  --hostname www.hatchaname.com \
  --environment namematch-prod-env \
  --validation-method CNAME
```

#### Production Backend (api.hatchaname.com)

```bash
az containerapp hostname add \
  --name namematch-prod-api \
  --resource-group namematch-prod-rg \
  --hostname api.hatchaname.com

az containerapp hostname bind \
  --name namematch-prod-api \
  --resource-group namematch-prod-rg \
  --hostname api.hatchaname.com \
  --environment namematch-prod-env \
  --validation-method CNAME
```

#### Dev Frontend (dev.hatchaname.com)

```bash
az containerapp hostname add \
  --name namematch-dev-web \
  --resource-group namematch-dev-rg \
  --hostname dev.hatchaname.com

az containerapp hostname bind \
  --name namematch-dev-web \
  --resource-group namematch-dev-rg \
  --hostname dev.hatchaname.com \
  --environment namematch-dev-env \
  --validation-method CNAME
```

#### Dev Backend (api-dev.hatchaname.com)

```bash
az containerapp hostname add \
  --name namematch-dev-api \
  --resource-group namematch-dev-rg \
  --hostname api-dev.hatchaname.com

az containerapp hostname bind \
  --name namematch-dev-api \
  --resource-group namematch-dev-rg \
  --hostname api-dev.hatchaname.com \
  --environment namematch-dev-env \
  --validation-method CNAME
```

---

## Step 9: Update Backend CORS

Update CORS to allow requests from the custom domains:

### Production

```bash
az containerapp ingress cors update \
  --name namematch-prod-api \
  --resource-group namematch-prod-rg \
  --allowed-origins "https://hatchaname.com" "https://www.hatchaname.com" \
  --allowed-methods "GET" "POST" "PUT" "DELETE" "OPTIONS" "PATCH" \
  --allowed-headers "*" \
  --allow-credentials true
```

### Dev

```bash
az containerapp ingress cors update \
  --name namematch-dev-api \
  --resource-group namematch-dev-rg \
  --allowed-origins "https://dev.hatchaname.com" \
  --allowed-methods "GET" "POST" "PUT" "DELETE" "OPTIONS" "PATCH" \
  --allowed-headers "*" \
  --allow-credentials true
```

---

## Step 10: Verify Setup

1. **Test the domain**: Visit `https://hatchaname.com` in your browser
2. **Test www**: Visit `https://www.hatchaname.com`
3. **Test API**: Visit `https://api.hatchaname.com/health`
4. **Check SSL**: Ensure the padlock icon appears (valid certificate)

---

## Step 11: (Optional) Enable Cloudflare Proxy

Once everything is working with "DNS only", you can enable Cloudflare's proxy (orange cloud) for:

- **CDN caching** - faster load times globally
- **DDoS protection** - automatic attack mitigation
- **SSL/TLS** - Cloudflare's edge certificates

To enable:
1. Go to **DNS** → **Records** in Cloudflare
2. Click the grey cloud icon next to each record to turn it orange
3. Set SSL/TLS mode to **Full (strict)** under **SSL/TLS** → **Overview**

---

## Troubleshooting

### DNS not propagating
- Use [dnschecker.org](https://dnschecker.org) to verify propagation
- Wait up to 24 hours for full propagation

### SSL certificate errors
- Ensure DNS records are set to "DNS only" (grey cloud) initially
- Wait 10-15 minutes for Azure managed certificates to provision
- Check certificate status: `az containerapp hostname list -n namematch-prod-web -g namematch-prod-rg`

### CORS errors
- Verify CORS is updated with the exact domain (including https://)
- Check browser console for specific CORS error messages

### Domain not resolving
- Verify nameservers are correctly set in Namecheap
- Confirm domain is "Active" in Cloudflare dashboard

---

## Checklist

### Initial Setup
- [ ] Create Cloudflare account
- [ ] Add `hatchaname.com` to Cloudflare
- [ ] Copy Cloudflare nameservers
- [ ] Update nameservers in Namecheap (Custom DNS)
- [ ] Wait for propagation / Cloudflare email confirmation

### DNS Records (Cloudflare)
- [ ] Get Azure Container Apps FQDNs (dev and prod)
- [ ] Add production CNAME records (`@`, `www`, `api`)
- [ ] Add dev CNAME records (`dev`, `api-dev`)

### Production Environment
- [ ] Add `hatchaname.com` hostname to frontend
- [ ] Add `www.hatchaname.com` hostname to frontend
- [ ] Add `api.hatchaname.com` hostname to backend
- [ ] Bind SSL certificates for all production domains
- [ ] Update production backend CORS
- [ ] Test production endpoints

### Dev Environment
- [ ] Add `dev.hatchaname.com` hostname to frontend
- [ ] Add `api-dev.hatchaname.com` hostname to backend
- [ ] Bind SSL certificates for dev domains
- [ ] Update dev backend CORS
- [ ] Test dev endpoints

### Optional
- [ ] Enable Cloudflare proxy (after SSL is working)

---

## Quick Reference

### Production

| URL | Purpose |
|-----|---------|
| `https://hatchaname.com` | Frontend (primary) |
| `https://www.hatchaname.com` | Frontend (www) |
| `https://api.hatchaname.com` | Backend API |
| `https://api.hatchaname.com/health` | Health check |

### Dev

| URL | Purpose |
|-----|---------|
| `https://dev.hatchaname.com` | Frontend |
| `https://api-dev.hatchaname.com` | Backend API |
| `https://api-dev.hatchaname.com/health` | Health check |
