@description('Name of the Container App')
param name string

@description('Location for the resource')
param location string

@description('Container Apps Environment ID')
param containerAppsEnvironmentId string

@description('Container Registry login server')
param containerRegistryLoginServer string

@description('Image name')
param imageName string = 'namematch-web'

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

@description('Backend API URL')
param apiUrl string

@description('Custom domain hostnames (empty array to disable)')
param customDomains array = []

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
      }
      // Always configure registry so app deployments can pull images
      registries: [
        {
          server: containerRegistryLoginServer
          identity: 'system'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'web'
          image: actualImage
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: [
            {
              name: 'API_URL'
              value: apiUrl
            }
          ]
          probes: usePlaceholderImage ? [] : [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
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
                concurrentRequests: '100'
              }
            }
          }
        ]
      }
    }
  }
}

output fqdn string = containerApp.properties.configuration.ingress.fqdn
output principalId string = containerApp.identity.principalId
output appId string = containerApp.id
output appName string = containerApp.name
