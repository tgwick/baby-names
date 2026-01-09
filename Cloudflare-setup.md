# Cloudflare DNS Setup for hatchaname.com

This guide walks through configuring Cloudflare as the DNS provider for `hatchaname.com` (registered at Namecheap) and connecting it to Azure Container Apps.

## Prerequisites

- Domain registered at Namecheap: `hatchaname.com`
- Azure Container Apps deployed (frontend and backend)
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
# Get frontend FQDN
az containerapp show \
  -n namematch-prod-web \
  -g namematch-prod-rg \
  --query "properties.configuration.ingress.fqdn" -o tsv

# Get backend FQDN
az containerapp show \
  -n namematch-prod-api \
  -g namematch-prod-rg \
  --query "properties.configuration.ingress.fqdn" -o tsv
```

Example output:
```
namematch-prod-web.proudocean-abc12345.eastus.azurecontainerapps.io
namematch-prod-api.proudocean-abc12345.eastus.azurecontainerapps.io
```

---

## Step 7: Configure DNS Records in Cloudflare

Once your domain is active, go to **DNS** → **Records** in Cloudflare and add:

| Type | Name | Target | Proxy Status | TTL |
|------|------|--------|--------------|-----|
| CNAME | `@` | `<frontend-fqdn>` | DNS only (grey cloud) | Auto |
| CNAME | `www` | `<frontend-fqdn>` | DNS only (grey cloud) | Auto |
| CNAME | `api` | `<backend-fqdn>` | DNS only (grey cloud) | Auto |

### Example:

| Type | Name | Target | Proxy Status |
|------|------|--------|--------------|
| CNAME | `@` | `namematch-prod-web.proudocean-abc12345.eastus.azurecontainerapps.io` | DNS only |
| CNAME | `www` | `namematch-prod-web.proudocean-abc12345.eastus.azurecontainerapps.io` | DNS only |
| CNAME | `api` | `namematch-prod-api.proudocean-abc12345.eastus.azurecontainerapps.io` | DNS only |

> **Important**: Start with "DNS only" (grey cloud icon) until Azure SSL certificates are configured. You can enable proxying (orange cloud) later for CDN benefits.

---

## Step 8: Add Custom Domain to Azure Container Apps

### Add hostnames to frontend app:

```bash
# Add apex domain
az containerapp hostname add \
  --name namematch-prod-web \
  --resource-group namematch-prod-rg \
  --hostname hatchaname.com

# Add www subdomain
az containerapp hostname add \
  --name namematch-prod-web \
  --resource-group namematch-prod-rg \
  --hostname www.hatchaname.com
```

### Bind managed SSL certificates (free):

```bash
# Bind certificate for apex domain
az containerapp hostname bind \
  --name namematch-prod-web \
  --resource-group namematch-prod-rg \
  --hostname hatchaname.com \
  --environment namematch-prod-env \
  --validation-method CNAME

# Bind certificate for www
az containerapp hostname bind \
  --name namematch-prod-web \
  --resource-group namematch-prod-rg \
  --hostname www.hatchaname.com \
  --environment namematch-prod-env \
  --validation-method CNAME
```

---

## Step 9: Update Backend CORS

Update CORS to allow requests from the new domain:

```bash
az containerapp ingress cors update \
  --name namematch-prod-api \
  --resource-group namematch-prod-rg \
  --allowed-origins "https://hatchaname.com" "https://www.hatchaname.com" \
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

- [ ] Create Cloudflare account
- [ ] Add `hatchaname.com` to Cloudflare
- [ ] Copy Cloudflare nameservers
- [ ] Update nameservers in Namecheap (Custom DNS)
- [ ] Wait for propagation / Cloudflare email confirmation
- [ ] Get Azure Container Apps FQDNs
- [ ] Add CNAME records in Cloudflare (DNS only mode)
- [ ] Add custom hostnames to Azure Container App
- [ ] Bind managed SSL certificates
- [ ] Update backend CORS settings
- [ ] Test all endpoints
- [ ] (Optional) Enable Cloudflare proxy

---

## Quick Reference

| URL | Purpose |
|-----|---------|
| `https://hatchaname.com` | Frontend (primary) |
| `https://www.hatchaname.com` | Frontend (www) |
| `https://api.hatchaname.com` | Backend API |
| `https://api.hatchaname.com/health` | Health check |
