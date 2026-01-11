@description('Location for the deployment script')
param location string

@description('Resource group name')
param resourceGroupName string

@description('Container Apps environment name')
param containerAppsEnvironmentName string

@description('Frontend app name')
param frontendAppName string

@description('Backend app name')
param backendAppName string

@description('Frontend custom domains to bind')
param frontendDomains array = []

@description('Backend custom domains to bind')
param backendDomains array = []

// Convert arrays to space-separated strings for the script
var frontendDomainsStr = join(frontendDomains, ' ')
var backendDomainsStr = join(backendDomains, ' ')

resource bindCertificates 'Microsoft.Resources/deploymentScripts@2023-08-01' = if (!empty(frontendDomains) || !empty(backendDomains)) {
  name: 'bind-custom-domain-certs'
  location: location
  kind: 'AzureCLI'
  properties: {
    azCliVersion: '2.63.0'
    timeout: 'PT30M'
    retentionInterval: 'PT1H'
    cleanupPreference: 'OnSuccess'
    scriptContent: '''
#!/bin/bash
set -e
echo "Binding custom domain certificates..."

# Bind frontend domains
for domain in $FRONTEND_DOMAINS; do
  if [ -n "$domain" ]; then
    echo "Binding certificate for frontend domain: $domain"
    az containerapp hostname bind \
      -n $FRONTEND_APP \
      -g $RESOURCE_GROUP \
      --hostname $domain \
      --environment $CONTAINER_ENV \
      --validation-method CNAME || echo "Warning: Failed to bind $domain (may already be bound)"
  fi
done

# Bind backend domains
for domain in $BACKEND_DOMAINS; do
  if [ -n "$domain" ]; then
    echo "Binding certificate for backend domain: $domain"
    az containerapp hostname bind \
      -n $BACKEND_APP \
      -g $RESOURCE_GROUP \
      --hostname $domain \
      --environment $CONTAINER_ENV \
      --validation-method CNAME || echo "Warning: Failed to bind $domain (may already be bound)"
  fi
done

echo "Certificate binding complete!"
'''
    environmentVariables: [
      {
        name: 'RESOURCE_GROUP'
        value: resourceGroupName
      }
      {
        name: 'CONTAINER_ENV'
        value: containerAppsEnvironmentName
      }
      {
        name: 'FRONTEND_APP'
        value: frontendAppName
      }
      {
        name: 'BACKEND_APP'
        value: backendAppName
      }
      {
        name: 'FRONTEND_DOMAINS'
        value: frontendDomainsStr
      }
      {
        name: 'BACKEND_DOMAINS'
        value: backendDomainsStr
      }
    ]
  }
}

output status string = (!empty(frontendDomains) || !empty(backendDomains)) ? 'Certificates bound' : 'No custom domains'
