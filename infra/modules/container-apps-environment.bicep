// Container Apps Environment module
// Supports both consumption and dedicated workload profiles
param name string
param location string
param logAnalyticsWorkspaceId string

@allowed([
  'Consumption'
  'Dedicated'
])
param workloadProfileType string = 'Consumption'

@description('Dedicated workload profile name (e.g., D4, D8, D16). Only used when workloadProfileType is Dedicated.')
param dedicatedWorkloadProfileName string = 'D4'

// Define workload profiles based on type
var consumptionWorkloadProfiles = []

var dedicatedWorkloadProfiles = [
  {
    name: dedicatedWorkloadProfileName
    workloadProfileType: dedicatedWorkloadProfileName
    minimumCount: 1
    maximumCount: 3
  }
]

var workloadProfiles = workloadProfileType == 'Consumption' ? consumptionWorkloadProfiles : dedicatedWorkloadProfiles

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: name
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: reference(logAnalyticsWorkspaceId, '2023-09-01').customerId
        sharedKey: listKeys(logAnalyticsWorkspaceId, '2023-09-01').primarySharedKey
      }
    }
    workloadProfiles: workloadProfiles
  }
}

output id string = containerAppsEnvironment.id
output name string = containerAppsEnvironment.name
output defaultDomain string = containerAppsEnvironment.properties.defaultDomain
