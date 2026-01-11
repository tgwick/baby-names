@description('Name of the Container App')
param name string

@description('Location for the resource')
param location string

@description('Container Apps Environment ID')
param containerAppsEnvironmentId string

@description('Container Registry login server')
param containerRegistryLoginServer string

@description('Image name')
param imageName string = 'namematch-api'

@description('Image tag')
param imageTag string = 'latest'

@description('Use placeholder image for initial deployment')
param usePlaceholderImage bool = true

@description('CPU cores')
param cpu string = '0.25'

@description('Memory')
param memory string = '0.5Gi'

@description('Minimum replicas')
param minReplicas int = 0

@description('Maximum replicas')
param maxReplicas int = 3

@description('PostgreSQL host FQDN')
param postgresHost string

@description('PostgreSQL database name')
param postgresDatabase string = 'namematch'

@description('Key Vault URI')
param keyVaultUri string

@description('Key Vault name')
param keyVaultName string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Frontend app FQDN for CORS')
param frontendFqdn string = ''

@description('Environment (dev or prod)')
@allowed(['dev', 'prod'])
param environment string = 'dev'

@description('Custom domain hostnames for this backend (empty array to disable)')
param customDomains array = []

@description('Custom domain origins for CORS (frontend custom domains)')
param corsCustomOrigins array = []

@description('Container Apps Environment name (for certificate references)')
param containerAppsEnvironmentName string = ''

var actualImage = usePlaceholderImage ? 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest' : '${containerRegistryLoginServer}/${imageName}:${imageTag}'

// Create managed certificates for custom domains
resource certificates 'Microsoft.App/managedEnvironments/managedCertificates@2024-03-01' = [for domain in customDomains: {
  name: '${containerAppsEnvironmentName}/cert-${replace(domain, '.', '-')}'
  location: location
  properties: {
    subjectName: domain
    domainControlValidation: 'CNAME'
  }
}]

// Build custom domain bindings with certificate references
var customDomainBindings = [for (domain, i) in customDomains: {
  name: domain
  bindingType: 'SniEnabled'
  certificateId: certificates[i].id
}]

// Build CORS allowed origins - include both Azure FQDN and custom domains
var azureFqdnOrigin = frontendFqdn != '' ? ['https://${frontendFqdn}'] : []
var customOrigins = [for origin in corsCustomOrigins: 'https://${origin}']
var allCorsOrigins = empty(corsCustomOrigins) ? (frontendFqdn != '' ? azureFqdnOrigin : ['*']) : union(azureFqdnOrigin, customOrigins)

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
        customDomains: customDomainBindings
        corsPolicy: {
          allowedOrigins: allCorsOrigins
          allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS', 'PATCH']
          allowedHeaders: ['*']
          allowCredentials: true
          maxAge: 3600
        }
      }
      // Always configure registry so app deployments can pull images
      registries: [
        {
          server: containerRegistryLoginServer
          identity: 'system'
        }
      ]
      secrets: [
        {
          name: 'postgres-password'
          keyVaultUrl: '${keyVaultUri}secrets/postgres-password'
          identity: 'system'
        }
        {
          name: 'jwt-key'
          keyVaultUrl: '${keyVaultUri}secrets/jwt-key'
          identity: 'system'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: actualImage
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment == 'prod' ? 'Production' : 'Development'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              value: 'Host=${postgresHost};Database=${postgresDatabase};Username=pgadmin;Password=placeholder;Ssl Mode=Require'
            }
            {
              name: 'ConnectionStrings__DefaultConnection__Password'
              secretRef: 'postgres-password'
            }
            {
              name: 'Jwt__Key'
              secretRef: 'jwt-key'
            }
            {
              name: 'Jwt__Issuer'
              value: 'NameMatch'
            }
            {
              name: 'Jwt__Audience'
              value: 'NameMatchUsers'
            }
            {
              name: 'Jwt__ExpiryInMinutes'
              value: '60'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
            }
          ]
          probes: usePlaceholderImage ? [] : [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health/live'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 10
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

// Key Vault Secrets User role for the container app
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerApp.id, keyVault.id, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: containerApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output fqdn string = containerApp.properties.configuration.ingress.fqdn
output principalId string = containerApp.identity.principalId
output appId string = containerApp.id
output appName string = containerApp.name
