#!/bin/bash

# Deployment script for Worker Service to Azure Container Apps
# Supports both Consumption and Dedicated workload profiles

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install it from https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    print_error "Not logged in to Azure. Please run 'az login' first."
    exit 1
fi

# Parse command line arguments
WORKLOAD_PROFILE_TYPE=${1:-"Consumption"}
RESOURCE_GROUP=${2:-"rg-workerservice"}
LOCATION=${3:-"eastus"}
SQL_ADMIN_PASSWORD=${4}

# Validate workload profile type
if [[ "$WORKLOAD_PROFILE_TYPE" != "Consumption" && "$WORKLOAD_PROFILE_TYPE" != "Dedicated" ]]; then
    print_error "Invalid workload profile type. Must be 'Consumption' or 'Dedicated'."
    echo "Usage: $0 [Consumption|Dedicated] [resource-group] [location] [sql-password]"
    exit 1
fi

# Check if SQL password is provided
if [ -z "$SQL_ADMIN_PASSWORD" ]; then
    print_error "SQL Admin Password is required."
    echo "Usage: $0 [Consumption|Dedicated] [resource-group] [location] [sql-password]"
    exit 1
fi

print_info "=== Azure Container Apps Deployment ==="
print_info "Workload Profile: $WORKLOAD_PROFILE_TYPE"
print_info "Resource Group: $RESOURCE_GROUP"
print_info "Location: $LOCATION"
echo ""

# Create resource group if it doesn't exist
print_info "Creating resource group..."
az group create \
    --name "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --output none

print_info "Resource group created/verified."

# Select parameter file based on workload profile
if [ "$WORKLOAD_PROFILE_TYPE" = "Consumption" ]; then
    PARAM_FILE="infra/parameters.consumption.json"
else
    PARAM_FILE="infra/parameters.dedicated.json"
fi

print_info "Using parameter file: $PARAM_FILE"

# Deploy infrastructure
print_info "Deploying infrastructure (this may take 10-15 minutes)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file infra/main.bicep \
    --parameters @"$PARAM_FILE" \
    --parameters sqlAdminPassword="$SQL_ADMIN_PASSWORD" \
    --output json)

if [ $? -ne 0 ]; then
    print_error "Deployment failed!"
    exit 1
fi

print_info "Deployment completed successfully!"
echo ""

# Extract outputs
ACR_LOGIN_SERVER=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.properties.outputs.containerRegistryLoginServer.value')
CONTAINER_APP_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.properties.outputs.containerAppFqdn.value')
SERVICE_BUS_NAMESPACE=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.properties.outputs.serviceBusNamespace.value')
SQL_SERVER_FQDN=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.properties.outputs.sqlServerFqdn.value')

print_info "=== Deployment Summary ==="
echo "Container Registry: $ACR_LOGIN_SERVER"
echo "Container App: $CONTAINER_APP_NAME"
echo "Service Bus Namespace: $SERVICE_BUS_NAMESPACE"
echo "SQL Server: $SQL_SERVER_FQDN"
echo "Workload Profile: $WORKLOAD_PROFILE_TYPE"
echo ""

print_info "=== Next Steps ==="
echo "1. Build and push your Docker image:"
echo "   az acr login --name ${ACR_LOGIN_SERVER%%.*}"
echo "   docker build -t $ACR_LOGIN_SERVER/workerservice:latest ."
echo "   docker push $ACR_LOGIN_SERVER/workerservice:latest"
echo ""
echo "2. The Container App will automatically pull and run the new image."
echo ""
echo "3. Monitor your application:"
echo "   az containerapp logs show --name <app-name> --resource-group $RESOURCE_GROUP --follow"
echo ""

print_info "Deployment completed successfully!"
