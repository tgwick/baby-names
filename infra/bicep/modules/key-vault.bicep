@description('Name of the Key Vault')
param name string

@description('Location for the resource')
param location string

@description('PostgreSQL admin password')
@secure()
param postgresPassword string

@description('JWT signing key')
@secure()
param jwtKey string

@description('Enable purge protection')
param enablePurgeProtection bool = false

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: enablePurgeProtection
    publicNetworkAccess: 'Enabled'
  }
}

resource postgresPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'postgres-password'
  properties: {
    value: postgresPassword
    contentType: 'text/plain'
  }
}

resource jwtKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'jwt-key'
  properties: {
    value: jwtKey
    contentType: 'text/plain'
  }
}

output vaultUri string = keyVault.properties.vaultUri
output vaultId string = keyVault.id
output vaultName string = keyVault.name
