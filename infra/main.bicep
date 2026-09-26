targetScope = 'resourceGroup'

@description('Short, globally unique prefix for this environment.')
@minLength(3)
@maxLength(12)
param namePrefix string

@allowed([
  'dev'
  'test'
  'staging'
  'prod'
])
param environmentName string = 'dev'

param location string = resourceGroup().location
param tags object = {}

var resourceTags = union(tags, {
  application: 'MPFAI'
  environment: environmentName
  managedBy: 'Bicep'
})
var suffix = uniqueString(subscription().id, resourceGroup().id, namePrefix, environmentName)

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take('${namePrefix}${environmentName}${suffix}', 24)
  location: location
  tags: resourceTags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
    supportsHttpsTrafficOnly: true
  }
}

resource storageBlob 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 14
    }
    isVersioningEnabled: true
  }
}

resource quarantineContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageBlob
  name: 'upload-quarantine'
  properties: {
    publicAccess: 'None'
  }
}

resource guidelineContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageBlob
  name: 'guideline-originals'
  properties: {
    publicAccess: 'None'
  }
}

resource evidenceContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: storageBlob
  name: 'evidence-originals'
  properties: {
    publicAccess: 'None'
  }
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('${namePrefix}-${environmentName}-kv-${suffix}', 24)
  location: location
  tags: resourceTags
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enablePurgeProtection: environmentName == 'prod'
    softDeleteRetentionInDays: 90
    publicNetworkAccess: 'Enabled'
  }
}

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: take('${namePrefix}-${environmentName}-logs-${suffix}', 63)
  location: location
  tags: resourceTags
  properties: {
    retentionInDays: environmentName == 'prod' ? 90 : 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: take('${namePrefix}-${environmentName}-appi-${suffix}', 260)
  location: location
  kind: 'web'
  tags: resourceTags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
    Request_Source: 'rest'
  }
}

resource appPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: take('${namePrefix}-${environmentName}-plan-${suffix}', 60)
  location: location
  tags: resourceTags
  kind: 'linux'
  sku: {
    name: environmentName == 'prod' ? 'P1v3' : 'B1'
    tier: environmentName == 'prod' ? 'PremiumV3' : 'Basic'
    capacity: 1
  }
  properties: {
    reserved: true
  }
}

resource messaging 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: take('${namePrefix}-${environmentName}-sb-${suffix}', 50)
  location: location
  tags: resourceTags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    minimumTlsVersion: '1.2'
    disableLocalAuth: true
    publicNetworkAccess: 'Enabled'
  }
}

resource sowQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: messaging
  name: 'sow-analyze'
  properties: {
    lockDuration: 'PT5M'
    maxDeliveryCount: 5
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    deadLetteringOnMessageExpiration: true
  }
}

resource reportQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: messaging
  name: 'report-import'
  properties: {
    lockDuration: 'PT5M'
    maxDeliveryCount: 5
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    deadLetteringOnMessageExpiration: true
  }
}

output storageAccountId string = storage.id
output storageAccountName string = storage.name
output keyVaultId string = vault.id
output logAnalyticsWorkspaceId string = logs.id
output applicationInsightsResourceId string = insights.id
output appServicePlanId string = appPlan.id
output serviceBusNamespaceId string = messaging.id
