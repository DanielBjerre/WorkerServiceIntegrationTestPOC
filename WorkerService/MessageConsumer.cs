
using Azure.Messaging.ServiceBus;

namespace WorkerService;

/// <summary>
/// This is the base consumer class. 
/// A class like this would ideally be distributed through a NuGet package which you could then inherit from in your own messageconsumer
/// It ensures that all consumptions of messages invokes a public action that you could then subscrite to in your test, so you know that the message has been consumed and you can start asserting.
/// </summary>
/// <typeparam name="TMessageBody"></typeparam>
/// <param name="logger"></param>
/// <param name="serviceBusClient"></param>
public abstract class MessageConsumer<TMessageBody>(ILogger<MessageConsumer<TMessageBody>> logger, ServiceBusClient serviceBusClient) : BackgroundService
{
    private readonly ILogger<MessageConsumer<TMessageBody>> _logger = logger;
    private readonly ServiceBusClient _serviceBusClient = serviceBusClient;
    protected abstract string QueueName { get; }

    public Action? OnMessageConsumed { get; set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var processor = _serviceBusClient.CreateProcessor(QueueName);
                processor.ProcessMessageAsync += MessageHandler;
                processor.ProcessErrorAsync += ErrorHandler;
                await processor.StartProcessingAsync(stoppingToken);
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
        }
    }

    protected virtual Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "An error occurred while consument message with error: {ErrorMessage}", args.Exception.Message);
        OnMessageConsumed?.Invoke();
        return Task.CompletedTask;
    }

    protected virtual async Task MessageHandler(ProcessMessageEventArgs args)
    {
        var messageBody = args.Message.Body.ToObjectFromJson<TMessageBody>()!;
        await HandleMessage(messageBody, args.Message, args.CancellationToken);
        OnMessageConsumed?.Invoke();
    }

    protected abstract Task HandleMessage(TMessageBody messageBody, ServiceBusReceivedMessage message, CancellationToken cancellationToken);
}