// Main Bicep template for AI Recruitment Platform - Phase 1
// Region: South Africa North (free/lowest tier where possible)

targetScope = 'resourceGroup'

@description('Base name for all resources')
param baseName string = 'studentlink'

@description('Environment (dev, staging, prod)')
param environment string = 'dev'

@description('Location for all resources')
param location string = 'southafricanorth'

@description('SQL Administrator login')
@secure()
param sqlAdminLogin string

@description('SQL Administrator password')
@secure()
param sqlAdminPassword string

var uniqueSuffix = uniqueString(resourceGroup().id)
var resourcePrefix = '${baseName}-${environment}-${uniqueSuffix}'

// Storage Account for CV uploads
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: toLower(replace('${resourcePrefix}storage', '-', ''))
  location: location
  sku: {
    name: 'Standard_LRS' // Lowest cost, locally redundant
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

// Blob container for CVs
resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource cvContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobServices
  name: 'cvs'
  properties: {
    publicAccess: 'None'
  }
}

// Azure SQL Server
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: '${resourcePrefix}-sql'
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled' // For dev; use private endpoint in prod
  }
}

// SQL Firewall rule - allow Azure services
resource sqlFirewallAzure 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// SQL Firewall rule - allow your IP (update this)
resource sqlFirewallDev 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowDeveloperAccess'
  properties: {
    startIpAddress: '0.0.0.0' // Replace with your IP
    endIpAddress: '255.255.255.255' // Replace with your IP
  }
}

// Azure SQL Database (Basic tier - free/lowest cost)
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: '${baseName}db'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2GB
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
  }
}

// Service Bus Namespace (Basic tier - lowest cost)
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: '${resourcePrefix}-sb'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    minimumTlsVersion: '1.2'
  }
}

// Service Bus Queue for CV parsing
resource cvParseQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'cv-parse-queue'
  properties: {
    maxDeliveryCount: 10
    defaultMessageTimeToLive: 'P1D' // 1 day
    deadLetteringOnMessageExpiration: true
    requiresDuplicateDetection: false
    requiresSession: false
  }
}

// Key Vault (Standard tier - lowest cost)
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${resourcePrefix}-kv'
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
    enabledForDeployment: false
    enabledForTemplateDeployment: true
    publicNetworkAccess: 'Enabled'
  }
}

// Application Insights (no extra cost, usage-based)
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${resourcePrefix}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${resourcePrefix}-ai'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// App Service Plan (Free F1 tier)
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${resourcePrefix}-asp'
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// App Service for Identity API
resource identityApiApp 'Microsoft.Web/sites@2023-01-01' = {
  name: '${resourcePrefix}-identity-api'
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: false // Not available in Free tier
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Development'
        }
      ]
    }
  }
}

// Outputs
output storageAccountName string = storageAccount.name
output storageAccountKey string = storageAccount.listKeys().keys[0].value
output storageBlobEndpoint string = storageAccount.properties.primaryEndpoints.blob
output sqlServerName string = sqlServer.name
output sqlDatabaseName string = sqlDatabase.name
output sqlConnectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
output serviceBusNamespace string = serviceBusNamespace.name
output serviceBusConnectionString string = listKeys('${serviceBusNamespace.id}/authorizationRules/RootManageSharedAccessKey', serviceBusNamespace.apiVersion).primaryConnectionString
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
output identityApiUrl string = 'https://${identityApiApp.properties.defaultHostName}'
output identityApiName string = identityApiApp.name
