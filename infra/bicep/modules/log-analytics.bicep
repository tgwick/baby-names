@description('Name of the Log Analytics workspace')
param name string

@description('Location for the resource')
param location string

@description('Retention period in days')
param retentionDays int = 30

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: name
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: retentionDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: -1
    }
  }
}

output workspaceId string = workspace.id
output customerId string = workspace.properties.customerId
output primaryKey string = listKeys(workspace.id, '2023-09-01').primarySharedKey
