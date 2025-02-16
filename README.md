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
* Assert state changes

The problem with this flow is: how does your test know when the message has been consumed?

This POC atleast tries to figure out a solution.

The test subscribes to an Action in the consumer, that get invokes when a message has been consumed.
This way the test knows when it can continue to do assertions. 

## Example
You should be able to just clone the repo, go into the WorkerService.Tests project and run the 'dotnet run' command.
