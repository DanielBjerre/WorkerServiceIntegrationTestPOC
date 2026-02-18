// Container App (Worker Service) module
param name string
param location string
param containerAppsEnvironmentId string
param containerRegistryName string
param imageName string
param imageTag string

@allowed([
  'Consumption'
  'Dedicated'
])
param workloadProfileType string = 'Consumption'

param dedicatedWorkloadProfileName string = 'D4'
param minReplicas int = 1
param maxReplicas int = 10

@secure()
param serviceBusConnectionString string

@secure()
param sqlConnectionString string

param queueName string

// Get Container Registry credentials
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: containerRegistryName
}

var containerRegistryUsername = containerRegistry.listCredentials().username
var containerRegistryPassword = containerRegistry.listCredentials().passwords[0].value

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    workloadProfileName: workloadProfileType == 'Consumption' ? 'Consumption' : dedicatedWorkloadProfileName
    configuration: {
      secrets: [
        {
          name: 'registry-password'
          value: containerRegistryPassword
        }
        {
          name: 'servicebus-connection'
          value: serviceBusConnectionString
        }
        {
          name: 'sql-connection'
          value: sqlConnectionString
        }
      ]
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistryUsername
          passwordSecretRef: 'registry-password'
        }
      ]
      activeRevisionsMode: 'Single'
    }
    template: {
      containers: [
        {
          name: 'workerservice'
          image: '${containerRegistry.properties.loginServer}/${imageName}:${imageTag}'
          resources: {
            cpu: json(workloadProfileType == 'Consumption' ? '0.25' : '1.0')
            memory: workloadProfileType == 'Consumption' ? '0.5Gi' : '2Gi'
          }
          env: [
            {
              name: 'ServiceBusConnectionString'
              secretRef: 'servicebus-connection'
            }
            {
              name: 'QueueName'
              value: queueName
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'sql-connection'
            }
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'servicebus-queue-length'
            custom: {
              type: 'azure-servicebus'
              metadata: {
                queueName: queueName
                messageCount: '10'
              }
              auth: [
                {
                  secretRef: 'servicebus-connection'
                  triggerParameter: 'connection'
                }
              ]
            }
          }
        ]
      }
    }
  }
}

output fqdn string = containerApp.properties.configuration.ingress != null ? containerApp.properties.configuration.ingress.fqdn : ''
output name string = containerApp.name
