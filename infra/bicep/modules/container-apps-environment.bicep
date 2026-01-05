@description('Name of the Container Apps Environment')
param name string

@description('Location for the resource')
param location string

@description('Log Analytics workspace ID')
param logAnalyticsWorkspaceId string

@description('Log Analytics workspace key')
@secure()
param logAnalyticsWorkspaceKey string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: name
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: split(logAnalyticsWorkspaceId, '/')[8]
        sharedKey: logAnalyticsWorkspaceKey
      }
    }
    zoneRedundant: false
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
}

output environmentId string = containerAppsEnvironment.id
output environmentName string = containerAppsEnvironment.name
output defaultDomain string = containerAppsEnvironment.properties.defaultDomain
