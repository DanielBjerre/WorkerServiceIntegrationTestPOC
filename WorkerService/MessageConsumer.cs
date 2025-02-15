
using Azure.Messaging.ServiceBus;

namespace WorkerService;

public abstract class MessageConsumer<T>(ILogger<MessageConsumer<T>> logger, ServiceBusClient serviceBusClient) : BackgroundService
{
    private readonly ILogger<MessageConsumer<T>> _logger = logger;
    private readonly ServiceBusClient _serviceBusClient = serviceBusClient;
    protected abstract string QueueName { get; }

    public Action? OnMessageConsumed { get; set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            var processor = _serviceBusClient.CreateProcessor(QueueName);
            processor.ProcessMessageAsync += MessageHandler;
            processor.ProcessErrorAsync += ErrorHandler;
            await processor.StartProcessingAsync(stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        OnMessageConsumed?.Invoke();
        return Task.CompletedTask;
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        var data = args.Message.Body.ToObjectFromJson<T>()!;
        await HandleMessage(data, args.CancellationToken);
        OnMessageConsumed?.Invoke();
    }

    protected abstract Task HandleMessage(T data, CancellationToken cancellationToken);
}