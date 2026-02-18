# Async messaging integration test 
Proof of concept for using WebApplicationFactory to conduct integration testt in a worker service that consumes messages from a servicebus queue

## Description
It is difficult to do integration tests as you would for an api, which follows the pattern of:
* Call endpoint
* Get response
* Assert result / state

With a worker service that asynchronously consumes a message from an queue, the pattern would be:
* Send message to queue
* Wait for worker to consume message
* Assert state

The problem with this flow is: how does your test know when the message has been consumed?

This POC atleast tries to figure out a solution.

The test subscribes to an Action in the consumer, that get invokes when a message has been consumed.
This way the test knows when it can continue to do assertions. 

## Example
You should be able to just clone the repo, go into the WorkerService.Tests project and run the 'dotnet run' command.

## Deployment to Azure

This repository includes Infrastructure-as-Code (Bicep) templates for deploying the Worker Service to **Azure Container Apps** with support for both **Consumption** and **Dedicated** workload profiles.

### Quick Start

```bash
# Deploy with Consumption profile (pay-per-use)
./infra/deploy.sh Consumption rg-workerservice eastus "YourSecurePassword123!"

# Deploy with Dedicated profile (reserved capacity)
./infra/deploy.sh Dedicated rg-workerservice eastus "YourSecurePassword123!"
```

### Documentation

- **[Infrastructure README](infra/README.md)** - Detailed deployment guide and architecture
- **[Consumption vs Dedicated Comparison](infra/COMPARISON.md)** - Complete comparison of workload profiles with cost analysis

### What Gets Deployed

- Azure Container Apps Environment (with configurable workload profile)
- Azure Container Registry (for Docker images)
- Azure Service Bus (queue for messages)
- Azure SQL Database (for persistent storage)
- Log Analytics Workspace (for monitoring)
- Container App (the worker service)

### Choosing Between Consumption and Dedicated

- **Consumption**: Best for development, testing, and variable workloads. Pay only for actual usage. Can scale to zero.
- **Dedicated**: Best for production workloads requiring guaranteed resources and consistent performance. Reserved capacity pricing.

See [COMPARISON.md](infra/COMPARISON.md) for detailed cost analysis and recommendations.
