// Main Bicep template for deploying Worker Service to Azure Container Apps
// Supports both consumption and dedicated workload profiles

targetScope = 'resourceGroup'

@description('The name prefix for all resources')
param resourcePrefix string = 'workerservice'

@description('The Azure region for all resources')
param location string = resourceGroup().location

@description('The workload profile type: Consumption or Dedicated')
@allowed([
  'Consumption'
  'Dedicated'
])
param workloadProfileType string = 'Consumption'

@description('The name of the dedicated workload profile (only used if workloadProfileType is Dedicated)')
param dedicatedWorkloadProfileName string = 'D4'

@description('Container image tag')
param imageTag string = 'latest'

@description('Minimum replicas for the container app')
param minReplicas int = 1

@description('Maximum replicas for the container app')
param maxReplicas int = 10

@description('SQL Server administrator login')
@secure()
param sqlAdminLogin string

@description('SQL Server administrator password')
@secure()
param sqlAdminPassword string

// Variables
var uniqueSuffix = uniqueString(resourceGroup().id)
var logAnalyticsName = '${resourcePrefix}-logs-${uniqueSuffix}'
var containerRegistryName = '${resourcePrefix}acr${uniqueSuffix}'
var containerAppsEnvName = '${resourcePrefix}-env-${uniqueSuffix}'
var containerAppName = '${resourcePrefix}-app-${uniqueSuffix}'
var serviceBusName = '${resourcePrefix}-sb-${uniqueSuffix}'
var sqlServerName = '${resourcePrefix}-sql-${uniqueSuffix}'
var sqlDatabaseName = 'WorkerServiceDB'
var queueName = 'messages'

// Log Analytics Workspace
module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'log-analytics-deployment'
  params: {
    name: logAnalyticsName
    location: location
  }
}

// Container Registry
module containerRegistry 'modules/container-registry.bicep' = {
  name: 'container-registry-deployment'
  params: {
    name: containerRegistryName
    location: location
  }
}

// Service Bus
module serviceBus 'modules/service-bus.bicep' = {
  name: 'service-bus-deployment'
  params: {
    name: serviceBusName
    location: location
    queueName: queueName
  }
}

// SQL Server and Database
module sqlServer 'modules/sql-server.bicep' = {
  name: 'sql-server-deployment'
  params: {
    serverName: sqlServerName
    databaseName: sqlDatabaseName
    location: location
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
  }
}

// Container Apps Environment
module containerAppsEnvironment 'modules/container-apps-environment.bicep' = {
  name: 'container-apps-env-deployment'
  params: {
    name: containerAppsEnvName
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
    workloadProfileType: workloadProfileType
    dedicatedWorkloadProfileName: dedicatedWorkloadProfileName
  }
}

// Container App (Worker Service)
module containerApp 'modules/container-app.bicep' = {
  name: 'container-app-deployment'
  params: {
    name: containerAppName
    location: location
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
    containerRegistryName: containerRegistryName
    imageName: 'workerservice'
    imageTag: imageTag
    workloadProfileType: workloadProfileType
    dedicatedWorkloadProfileName: dedicatedWorkloadProfileName
    minReplicas: minReplicas
    maxReplicas: maxReplicas
    serviceBusConnectionString: serviceBus.outputs.connectionString
    sqlConnectionString: sqlServer.outputs.connectionString
    queueName: queueName
  }
  dependsOn: [
    containerRegistry
  ]
}

// Outputs
output containerRegistryLoginServer string = containerRegistry.outputs.loginServer
output containerAppFqdn string = containerApp.outputs.fqdn
output serviceBusNamespace string = serviceBus.outputs.namespaceName
output sqlServerFqdn string = sqlServer.outputs.serverFqdn
output containerAppsEnvironmentId string = containerAppsEnvironment.outputs.id
output workloadProfileUsed string = workloadProfileType
