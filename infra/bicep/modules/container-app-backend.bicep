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
        corsPolicy: {
          allowedOrigins: frontendFqdn != '' ? [
            'https://${frontendFqdn}'
          ] : [
            '*'
          ]
          allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS', 'PATCH']
          allowedHeaders: ['*']
          allowCredentials: true
          maxAge: 3600
        }
      }
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
          image: '${containerRegistryLoginServer}/${imageName}:${imageTag}'
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
              value: 'Host=${postgresHost};Database=${postgresDatabase};Username=pgadmin;Password=placeholder'
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
          probes: [
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
