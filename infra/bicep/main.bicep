targetScope = 'subscription'

// ============================================
// Parameters
// ============================================

@description('Environment name')
@allowed(['dev', 'prod'])
param environment string

@description('Azure region for resources')
param location string = 'eastus'

@description('Project name for resource naming')
param projectName string = 'namematch'

@description('PostgreSQL administrator password')
@secure()
param postgresAdminPassword string

@description('JWT signing key (minimum 32 characters)')
@secure()
param jwtKey string

@description('Location for PostgreSQL (some subscriptions have regional restrictions)')
param postgresLocation string = ''

@description('Backend image tag')
param backendImageTag string = 'latest'

@description('Frontend image tag')
param frontendImageTag string = 'latest'

// ============================================
// Variables
// ============================================

// Resource naming (Azure naming conventions)
var resourceGroupName = '${projectName}-${environment}-rg'
var acrName = replace('${projectName}${environment}acr', '-', '')
var keyVaultName = '${projectName}-${environment}-kv'
var logAnalyticsName = '${projectName}-${environment}-logs'
var appInsightsName = '${projectName}-${environment}-ai'
var containerEnvName = '${projectName}-${environment}-env'
var postgresServerName = '${projectName}-${environment}-pg'
var backendAppName = '${projectName}-${environment}-api'
var frontendAppName = '${projectName}-${environment}-web'

// Environment-specific settings
var isProd = environment == 'prod'
var effectivePostgresLocation = empty(postgresLocation) ? location : postgresLocation
var postgresSku = isProd ? 'Standard_D2ds_v5' : 'Standard_B1ms'
var postgresStorage = isProd ? 64 : 32
var containerAppsCpu = isProd ? '0.5' : '0.25'
var containerAppsMemory = isProd ? '1Gi' : '0.5Gi'
var containerAppsMinReplicas = isProd ? 2 : 0
var containerAppsMaxReplicas = isProd ? 10 : 3
var logRetentionDays = isProd ? 90 : 30
var acrSku = isProd ? 'Standard' : 'Basic'

// ============================================
// Resource Group
// ============================================

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: {
    environment: environment
    project: projectName
    managedBy: 'bicep'
  }
}

// ============================================
// Monitoring
// ============================================

module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'logAnalytics-${environment}'
  scope: rg
  params: {
    name: logAnalyticsName
    location: location
    retentionDays: logRetentionDays
  }
}

module appInsights 'modules/app-insights.bicep' = {
  name: 'appInsights-${environment}'
  scope: rg
  params: {
    name: appInsightsName
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

// ============================================
// Security
// ============================================

module keyVault 'modules/key-vault.bicep' = {
  name: 'keyVault-${environment}'
  scope: rg
  params: {
    name: keyVaultName
    location: location
    postgresPassword: postgresAdminPassword
    jwtKey: jwtKey
    enablePurgeProtection: true // Once enabled, cannot be disabled
  }
}

// ============================================
// Container Registry
// ============================================

module acr 'modules/container-registry.bicep' = {
  name: 'acr-${environment}'
  scope: rg
  params: {
    name: acrName
    location: location
    sku: acrSku
  }
}

// ============================================
// Database
// ============================================

module postgres 'modules/postgresql.bicep' = {
  name: 'postgres-${environment}'
  scope: rg
  params: {
    name: postgresServerName
    location: effectivePostgresLocation
    administratorPassword: postgresAdminPassword
    skuName: postgresSku
    storageSizeGB: postgresStorage
    databaseName: 'namematch'
    backupRetentionDays: isProd ? 14 : 7
  }
}

// ============================================
// Container Apps
// ============================================

module containerEnv 'modules/container-apps-environment.bicep' = {
  name: 'containerEnv-${environment}'
  scope: rg
  params: {
    name: containerEnvName
    location: location
    logAnalyticsCustomerId: logAnalytics.outputs.customerId
    logAnalyticsWorkspaceKey: logAnalytics.outputs.primaryKey
  }
}

module backendApp 'modules/container-app-backend.bicep' = {
  name: 'backendApp-${environment}'
  scope: rg
  params: {
    name: backendAppName
    location: location
    containerAppsEnvironmentId: containerEnv.outputs.environmentId
    containerRegistryLoginServer: acr.outputs.loginServer
    imageName: 'namematch-api'
    imageTag: backendImageTag
    cpu: containerAppsCpu
    memory: containerAppsMemory
    minReplicas: containerAppsMinReplicas
    maxReplicas: containerAppsMaxReplicas
    postgresHost: postgres.outputs.fqdn
    postgresDatabase: 'namematch'
    keyVaultUri: keyVault.outputs.vaultUri
    keyVaultName: keyVaultName
    appInsightsConnectionString: appInsights.outputs.connectionString
    frontendFqdn: '' // Will be updated after frontend is created
    environment: environment
  }
}

module frontendApp 'modules/container-app-frontend.bicep' = {
  name: 'frontendApp-${environment}'
  scope: rg
  params: {
    name: frontendAppName
    location: location
    containerAppsEnvironmentId: containerEnv.outputs.environmentId
    containerRegistryLoginServer: acr.outputs.loginServer
    imageName: 'namematch-web'
    imageTag: frontendImageTag
    cpu: containerAppsCpu
    memory: containerAppsMemory
    minReplicas: containerAppsMinReplicas
    maxReplicas: containerAppsMaxReplicas
    apiUrl: 'https://${backendApp.outputs.fqdn}'
  }
}

// ============================================
// ACR Pull Role Assignments
// ============================================

module backendAcrPull 'modules/acr-role-assignment.bicep' = {
  name: 'backendAcrPull-${environment}'
  scope: rg
  params: {
    acrName: acr.outputs.acrName
    principalId: backendApp.outputs.principalId
  }
}

module frontendAcrPull 'modules/acr-role-assignment.bicep' = {
  name: 'frontendAcrPull-${environment}'
  scope: rg
  params: {
    acrName: acr.outputs.acrName
    principalId: frontendApp.outputs.principalId
  }
}

// ============================================
// Outputs
// ============================================

output resourceGroupName string = rg.name
output acrLoginServer string = acr.outputs.loginServer
output acrName string = acrName
output backendFqdn string = backendApp.outputs.fqdn
output backendUrl string = 'https://${backendApp.outputs.fqdn}'
output frontendFqdn string = frontendApp.outputs.fqdn
output frontendUrl string = 'https://${frontendApp.outputs.fqdn}'
output keyVaultUri string = keyVault.outputs.vaultUri
output appInsightsConnectionString string = appInsights.outputs.connectionString
output postgresHost string = postgres.outputs.fqdn
