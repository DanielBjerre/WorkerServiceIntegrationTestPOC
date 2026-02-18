# Azure Container Apps: Consumption vs Dedicated Workload Profiles

## Executive Summary

This document provides a comprehensive comparison between **Consumption** and **Dedicated** workload profiles for Azure Container Apps to help you choose the right pricing model for your Worker Service deployment.

## Quick Decision Guide

| Your Scenario | Recommended Profile |
|---------------|-------------------|
| Development/Testing | **Consumption** |
| Unpredictable or variable workload | **Consumption** |
| Workload can scale to zero when idle | **Consumption** |
| Sporadic message processing | **Consumption** |
| Steady-state production workload | **Dedicated** |
| 24/7 continuous processing | **Dedicated** |
| Need guaranteed resources for SLAs | **Dedicated** |
| High resource requirements (>4 vCPU) | **Dedicated** |

## Detailed Comparison

### 1. Consumption Workload Profile

#### Overview
Pay-per-use model where you're charged only for actual resource consumption. Ideal for variable or unpredictable workloads.

#### Technical Specifications
| Feature | Value |
|---------|-------|
| **vCPU per container** | 0.25 - 4 vCPU |
| **Memory per container** | 0.5 GB - 8 GB |
| **Infrastructure** | Shared multi-tenant |
| **Scale to zero** | Yes |
| **Minimum replicas** | 0 |
| **Cold start** | Yes (~2-5 seconds) |

#### Pricing Structure
```
Total Cost = vCPU Consumption + Memory Consumption + Requests
```

**Example Pricing (Approximate):**
- vCPU: $0.000012 per vCPU-second
- Memory: $0.000001389 per GB-second
- Requests: $0.40 per million requests

**Monthly Cost Examples:**

| Scenario | vCPU | Memory | Runtime | Est. Cost |
|----------|------|--------|---------|-----------|
| Light usage (50 hrs/month) | 0.25 | 0.5 GB | 50 hrs | $2-5 |
| Medium usage (200 hrs/month) | 0.5 | 1 GB | 200 hrs | $10-20 |
| Variable (500 hrs/month) | 1.0 | 2 GB | 500 hrs | $40-70 |

#### Advantages
✅ **Cost-effective for variable workloads** - No charges when idle  
✅ **No upfront commitment** - Pay only for what you use  
✅ **Fast to get started** - No provisioning delays  
✅ **Automatic resource management** - Platform handles infrastructure  
✅ **Good for development** - Low cost for testing  

#### Disadvantages
❌ **Cold starts** - Initial request may be slower after idle period  
❌ **Resource limits** - Maximum 4 vCPU and 8 GB per container  
❌ **Shared infrastructure** - Potential "noisy neighbor" effects  
❌ **Less predictable costs** - Varies with actual usage  
❌ **No resource guarantees** - Resources subject to availability  

#### Best Use Cases
- Development and testing environments
- Event-driven applications with sporadic activity
- Batch processing with variable schedules
- Cost-sensitive applications with tolerance for cold starts
- Workloads that can scale to zero during off-hours

---

### 2. Dedicated Workload Profile

#### Overview
Reserved capacity model with dedicated compute nodes. You pay for reserved infrastructure regardless of usage. Ideal for production workloads requiring consistent performance.

#### Technical Specifications

**Available Profiles:**

| Profile | vCPU | Memory | Approximate Hourly Cost |
|---------|------|--------|------------------------|
| **D4** | 4 | 16 GB | $0.20 - $0.25 |
| **D8** | 8 | 32 GB | $0.40 - $0.50 |
| **D16** | 16 | 64 GB | $0.80 - $1.00 |
| **D32** | 32 | 128 GB | $1.60 - $2.00 |

**Other Specifications:**
| Feature | Value |
|---------|-------|
| **Infrastructure** | Dedicated nodes |
| **Scale to zero** | No (minimum 1 node) |
| **Cold start** | No |
| **Isolation** | Workload-level isolation |

#### Pricing Structure
```
Total Cost = (Profile hourly rate × Hours) + Requests
```

**Monthly Cost Examples:**

| Profile | Hours/Month | Est. Monthly Cost |
|---------|-------------|-------------------|
| D4 | 730 (24/7) | $145-$180 |
| D8 | 730 (24/7) | $290-$365 |
| D16 | 730 (24/7) | $580-$730 |
| D32 | 730 (24/7) | $1,160-$1,460 |

#### Advantages
✅ **Guaranteed resources** - Dedicated capacity always available  
✅ **No cold starts** - Always warm and ready  
✅ **Consistent performance** - No "noisy neighbor" issues  
✅ **Higher limits** - Up to 32 vCPU and 128 GB per container  
✅ **Predictable costs** - Fixed monthly expenses  
✅ **Better for SLAs** - More reliable performance characteristics  

#### Disadvantages
❌ **Higher minimum cost** - Pay for reserved capacity 24/7  
❌ **Wasted capacity** - Pay even when idle  
❌ **Requires planning** - Must estimate capacity needs  
❌ **Less flexible** - Can't scale to zero  
❌ **Provisioning time** - New profiles may take minutes to provision  

#### Best Use Cases
- Production workloads with SLA requirements
- Continuous 24/7 processing
- Applications requiring consistent low latency
- High-throughput message processing
- Workloads requiring >4 vCPU or >8 GB memory
- Cost-predictable budgeting requirements

---

## Cost Analysis Examples

### Scenario 1: Intermittent Message Processing
**Workload:** Process messages 8 hours per day, Monday-Friday

| Profile Type | Details | Monthly Cost |
|-------------|---------|--------------|
| **Consumption** | 0.5 vCPU, 1 GB, ~160 hrs/month | **$10-15** ✅ |
| **Dedicated D4** | Reserved 24/7, mostly idle | **$145-180** |

**Winner:** Consumption (90% cost savings)

### Scenario 2: 24/7 Production Workload
**Workload:** Continuous message processing, 2 containers, 1 vCPU, 2 GB each

| Profile Type | Details | Monthly Cost |
|-------------|---------|--------------|
| **Consumption** | 2 containers × 1 vCPU × 730 hrs | **$120-150** |
| **Dedicated D4** | Can run 2 containers on D4 | **$145-180** ✅ |

**Winner:** Dedicated (similar cost, better performance)

### Scenario 3: High-Performance Requirements
**Workload:** 6 vCPU, 12 GB memory, 24/7

| Profile Type | Details | Monthly Cost |
|-------------|---------|--------------|
| **Consumption** | Not supported (exceeds 4 vCPU limit) | **N/A** |
| **Dedicated D8** | Required for 6+ vCPU | **$290-365** ✅ |

**Winner:** Dedicated (only option)

---

## Migration Path

### Starting with Consumption
1. Deploy with Consumption profile
2. Monitor actual usage and costs
3. If running >70% of the time, consider Dedicated
4. If need >4 vCPU or >8 GB, migrate to Dedicated

### Switching Between Profiles
Both parameter files are provided. To switch:

```bash
# From Consumption to Dedicated
az deployment group create \
  --resource-group rg-workerservice \
  --template-file infra/main.bicep \
  --parameters @infra/parameters.dedicated.json

# From Dedicated to Consumption
az deployment group create \
  --resource-group rg-workerservice \
  --template-file infra/main.bicep \
  --parameters @infra/parameters.consumption.json
```

---

## Recommendations by Use Case

### For This Worker Service POC

Given the Worker Service characteristics:
- Asynchronous message processing from Service Bus
- Variable message arrival patterns
- Not time-critical processing
- Development/testing focus

**Recommendation:** **Start with Consumption profile**

**Rationale:**
1. Lower cost for development/testing
2. Can scale to zero when no messages
3. Auto-scales based on queue depth
4. Easy to upgrade to Dedicated later if needed

### For Production Deployment

If deploying to production, consider:

**Use Consumption if:**
- Message volume is highly variable
- Acceptable to have occasional cold starts
- Cost optimization is priority
- Can tolerate 2-5 second startup delay

**Use Dedicated if:**
- Processing >16 hours per day continuously
- Need guaranteed sub-second response times
- SLA requires consistent performance
- Message volume justifies reserved capacity

---

## Monitoring and Optimization

### Key Metrics to Monitor

1. **Replica count** - How often are you scaling?
2. **CPU/Memory usage** - Are you hitting limits?
3. **Request/Message latency** - Are cold starts impacting performance?
4. **Cost trends** - Is actual cost aligned with expectations?

### Optimization Tips

**For Consumption:**
- Set appropriate min/max replicas
- Tune scale rules for your message patterns
- Consider pre-warming if cold starts are problematic
- Monitor for consistent usage patterns that might benefit from Dedicated

**For Dedicated:**
- Right-size your profile (don't over-provision)
- Maximize node utilization by running multiple containers
- Consider using multiple smaller profiles vs one large profile
- Monitor for idle time that might benefit from Consumption

---

## Additional Resources

- [Azure Container Apps Pricing Calculator](https://azure.microsoft.com/en-us/pricing/calculator/)
- [Workload Profiles Documentation](https://learn.microsoft.com/en-us/azure/container-apps/workload-profiles-overview)
- [Cost Optimization Best Practices](https://learn.microsoft.com/en-us/azure/container-apps/cost-optimization)
