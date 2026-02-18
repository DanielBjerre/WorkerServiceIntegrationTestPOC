# Quick Reference Guide - Container Apps Deployment

## File Structure

```
infra/
├── main.bicep                                  # Main deployment orchestrator
├── modules/
│   ├── container-app.bicep                    # Worker service container app
│   ├── container-apps-environment.bicep       # Environment with workload profiles
│   ├── container-registry.bicep               # Azure Container Registry
│   ├── log-analytics.bicep                    # Monitoring workspace
│   ├── service-bus.bicep                      # Message queue
│   └── sql-server.bicep                       # Database server
├── parameters.consumption.json                 # Consumption profile config (with Key Vault)
├── parameters.consumption.local.template.json  # Consumption template (copy for local use)
├── parameters.dedicated.json                   # Dedicated profile config (with Key Vault)
├── parameters.dedicated.local.template.json    # Dedicated template (copy for local use)
├── deploy.sh                                   # Automated deployment script
├── README.md                                   # Detailed documentation
└── COMPARISON.md                               # Consumption vs Dedicated comparison
```

## Quick Start Commands

### Using Deployment Script (Easiest)

```bash
# Consumption profile
./infra/deploy.sh Consumption rg-workerservice eastus "YourSecurePassword123!"

# Dedicated profile
./infra/deploy.sh Dedicated rg-workerservice eastus "YourSecurePassword123!"
```

### Using Azure CLI Directly

```bash
# Create resource group
az group create --name rg-workerservice --location eastus

# Deploy with Consumption profile
az deployment group create \
  --resource-group rg-workerservice \
  --template-file infra/main.bicep \
  --parameters infra/parameters.consumption.json \
  --parameters sqlAdminPassword='YourSecurePassword123!'

# Deploy with Dedicated profile
az deployment group create \
  --resource-group rg-workerservice \
  --template-file infra/main.bicep \
  --parameters infra/parameters.dedicated.json \
  --parameters sqlAdminPassword='YourSecurePassword123!'
```

## Key Differences

| Feature | Consumption | Dedicated |
|---------|------------|-----------|
| **Pricing** | Pay-per-use | Reserved capacity |
| **Scale to zero** | ✅ Yes | ❌ No |
| **Max vCPU** | 4 | 32 |
| **Max Memory** | 8 GB | 128 GB |
| **Cold starts** | Yes (~2-5s) | No |
| **Best for** | Variable workloads | 24/7 production |

## Parameter Customization

### Key Parameters in main.bicep

- `resourcePrefix` - Prefix for all resource names (default: "workerservice")
- `location` - Azure region (default: resource group location)
- `workloadProfileType` - "Consumption" or "Dedicated"
- `dedicatedWorkloadProfileName` - "D4", "D8", "D16", or "D32" (if Dedicated)
- `imageTag` - Container image tag (default: "latest")
- `minReplicas` - Minimum container instances
- `maxReplicas` - Maximum container instances
- `sqlAdminLogin` - SQL admin username
- `sqlAdminPassword` - SQL admin password (use Key Vault!)

## Resources Deployed

1. **Log Analytics Workspace** - For monitoring and diagnostics
2. **Container Registry** - ACR for hosting Docker images
3. **Service Bus Namespace** - With a queue named "messages"
4. **SQL Server & Database** - For persistent storage
5. **Container Apps Environment** - With chosen workload profile
6. **Container App** - The worker service itself

## Post-Deployment Steps

1. **Build and push Docker image:**
   ```bash
   az acr login --name <registry-name>
   docker build -t <registry>.azurecr.io/workerservice:latest .
   docker push <registry>.azurecr.io/workerservice:latest
   ```

2. **Monitor the application:**
   ```bash
   az containerapp logs show \
     --name <app-name> \
     --resource-group rg-workerservice \
     --follow
   ```

3. **Check deployment outputs:**
   ```bash
   az deployment group show \
     --resource-group rg-workerservice \
     --name main \
     --query properties.outputs
   ```

## Validation

Validate Bicep templates before deployment:

```bash
az bicep build --file infra/main.bicep
```

## Troubleshooting

### Common Issues

1. **"Invalid workload profile"**
   - Ensure `dedicatedWorkloadProfileName` matches available profiles: D4, D8, D16, D32

2. **"Key Vault reference failed"**
   - Update parameter files with correct Key Vault details
   - Or pass `sqlAdminPassword` directly via CLI

3. **"Container image not found"**
   - Ensure Docker image is pushed to ACR before Container App starts
   - Check image name and tag match parameters

4. **"SQL connection failed"**
   - Verify SQL firewall rules allow Container Apps
   - Check connection string in Container App environment variables

## Cost Estimation

### Consumption Profile (Low Usage)
- Container Apps: ~$5-10/month
- Service Bus: ~$10/month
- SQL Database: ~$5/month (Basic)
- Container Registry: ~$5/month (Basic)
- **Total: ~$25-30/month**

### Dedicated D4 Profile (24/7)
- Container Apps: ~$150-180/month
- Service Bus: ~$10/month
- SQL Database: ~$5/month (Basic)
- Container Registry: ~$5/month (Basic)
- **Total: ~$170-200/month**

## Cleanup

Delete all resources:

```bash
az group delete --name rg-workerservice --yes --no-wait
```

## Additional Documentation

- [README.md](README.md) - Comprehensive deployment guide
- [COMPARISON.md](COMPARISON.md) - Detailed comparison with cost analysis
- [Azure Container Apps Docs](https://learn.microsoft.com/en-us/azure/container-apps/)
