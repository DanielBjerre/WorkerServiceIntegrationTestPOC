# Azure Container Apps Infrastructure

This directory contains Bicep Infrastructure-as-Code (IaC) templates for deploying the Worker Service to Azure Container Apps with support for both **Consumption** and **Dedicated** workload profiles.

## Overview

The infrastructure deploys a complete environment for running the Worker Service on Azure, including:

- **Azure Container Apps Environment** - with configurable workload profiles
- **Azure Container Registry** - for hosting Docker images
- **Azure Service Bus** - for message queuing
- **Azure SQL Database** - for persistent storage
- **Log Analytics Workspace** - for monitoring and diagnostics

## Workload Profile Types

### Consumption Workload Profile

**Best for:** Development, testing, and variable workloads with unpredictable traffic patterns.

**Characteristics:**
- **Pay-per-use pricing** - You only pay for actual resource consumption (vCPU seconds and memory GB-seconds)
- **Automatic scaling to zero** - No costs when idle
- **Shared infrastructure** - Resources are shared with other customers
- **Lower resource limits** - Maximum 4 vCPU and 8 GB memory per container
- **Cost-effective for sporadic workloads**

**Pricing Model:**
```
Cost = (vCPU-seconds × vCPU price) + (GB-seconds × Memory price) + (Requests × Request price)
```

**Use when:**
- Workload has unpredictable or highly variable traffic
- Cost optimization is critical
- Scaling to zero during idle periods is acceptable
- Resource requirements are within consumption limits

### Dedicated Workload Profile

**Best for:** Production workloads requiring guaranteed resources and consistent performance.

**Characteristics:**
- **Reserved capacity pricing** - You pay for reserved infrastructure regardless of usage
- **Dedicated compute nodes** - Guaranteed resources not shared with other tenants
- **Higher resource limits** - Up to 32 vCPU and 128 GB memory per container (depending on profile)
- **Predictable performance** - No "noisy neighbor" issues
- **Better for steady-state workloads**

**Available Profiles:**
- **D4** - 4 vCPU, 16 GB memory
- **D8** - 8 vCPU, 32 GB memory
- **D16** - 16 vCPU, 64 GB memory
- **D32** - 32 vCPU, 128 GB memory

**Pricing Model:**
```
Cost = (Hours × Profile hourly rate) + (Requests × Request price)
```

**Use when:**
- Workload runs continuously or has predictable patterns
- Guaranteed resources are required for SLAs
- Performance consistency is critical
- Higher resource limits are needed

## Deployment

### Prerequisites

1. Azure CLI installed and authenticated:
   ```bash
   az login
   az account set --subscription <subscription-id>
   ```

2. Azure subscription with appropriate permissions

3. Docker image built and tagged (see main README)

### Deployment Steps

#### 1. Create Resource Group

```bash
az group create \
  --name rg-workerservice \
  --location eastus
```

#### 2. Deploy with Consumption Profile

For development/testing or variable workloads:

```bash
az deployment group create \
  --resource-group rg-workerservice \
  --template-file infra/main.bicep \
  --parameters infra/parameters.consumption.json \
  --parameters sqlAdminPassword='<your-secure-password>'
```

**Key Parameters:**
- `workloadProfileType`: "Consumption"
- `minReplicas`: 1 (can scale to zero if needed)
- `maxReplicas`: 10

#### 3. Deploy with Dedicated Profile

For production workloads requiring guaranteed resources:

```bash
az deployment group create \
  --resource-group rg-workerservice \
  --template-file infra/main.bicep \
  --parameters infra/parameters.dedicated.json \
  --parameters sqlAdminPassword='<your-secure-password>'
```

**Key Parameters:**
- `workloadProfileType`: "Dedicated"
- `dedicatedWorkloadProfileName`: "D4" (or D8, D16, D32)
- `minReplicas`: 2 (dedicated profiles don't scale to zero)
- `maxReplicas`: 20

#### 4. Push Docker Image to ACR

After deployment, push your Docker image:

```bash
# Get ACR login server from deployment outputs
ACR_NAME=$(az deployment group show \
  --resource-group rg-workerservice \
  --name main \
  --query properties.outputs.containerRegistryLoginServer.value -o tsv)

# Login to ACR
az acr login --name ${ACR_NAME}

# Build and push image
docker build -t ${ACR_NAME}/workerservice:latest .
docker push ${ACR_NAME}/workerservice:latest
```

#### 5. Update Container App with New Image

The Container App will automatically pull the new image on next deployment or restart.

## Cost Comparison Example

**Scenario:** Worker service processing messages 8 hours per day, 5 days per week

### Consumption Profile
- Active: 160 hours/month
- vCPU: 0.25 (per container)
- Memory: 0.5 GB
- Cost: ~$5-10/month (varies with actual usage)

### Dedicated D4 Profile
- Reserved: 730 hours/month (24/7)
- vCPU: 4 vCPU available
- Memory: 16 GB available
- Cost: ~$150-200/month (fixed, regardless of usage)

**Recommendation:** Use Consumption for this scenario (70-80% cost savings)

## Monitoring and Management

### View Container App Logs

```bash
az containerapp logs show \
  --name <container-app-name> \
  --resource-group rg-workerservice \
  --follow
```

### Scale Container App

```bash
az containerapp update \
  --name <container-app-name> \
  --resource-group rg-workerservice \
  --min-replicas 2 \
  --max-replicas 15
```

### View Metrics in Azure Portal

1. Navigate to your Container App in the Azure Portal
2. Select "Metrics" from the left menu
3. Add metrics:
   - Replica Count
   - CPU Usage
   - Memory Usage
   - Request Count

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│  Container Apps Environment                             │
│  (Consumption or Dedicated Workload Profile)            │
│                                                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Container App (Worker Service)                  │  │
│  │  - Auto-scales based on Service Bus queue depth │  │
│  │  - Processes messages from queue                 │  │
│  │  - Saves results to SQL Database                 │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                    │                    │
                    │                    │
         ┌──────────▼────────┐    ┌─────▼─────────┐
         │  Service Bus      │    │  SQL Database │
         │  - Queue          │    │  - Entities   │
         └───────────────────┘    └───────────────┘
```

## Security Considerations

1. **Secrets Management**: Use Azure Key Vault for storing sensitive configuration (demonstrated in parameter files)
2. **Network Security**: Consider using VNet integration for production workloads
3. **Managed Identities**: Update to use Managed Identities instead of connection strings for production
4. **SQL Firewall**: Restrictfirewall rules to only allow Container Apps

## Cleanup

To delete all resources:

```bash
az group delete --name rg-workerservice --yes --no-wait
```

## Additional Resources

- [Azure Container Apps Documentation](https://learn.microsoft.com/en-us/azure/container-apps/)
- [Workload Profiles](https://learn.microsoft.com/en-us/azure/container-apps/workload-profiles-overview)
- [Container Apps Pricing](https://azure.microsoft.com/en-us/pricing/details/container-apps/)
- [Bicep Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
