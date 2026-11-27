using System.Collections.Concurrent;
using SFA.DAS.EmploymentVerificationService.Messages.Learner.Commands;

namespace SFA.DAS.Payments.DataLocks.Tests.Specs.Handlers;

public class VerifyNotEmployedHandler : IHandleMessages<VerifyNotEmployed>
{
    public static ConcurrentBag<VerifyNotEmployed> ReceivedMessages = new ConcurrentBag<VerifyNotEmployed>();

    public Task Handle(VerifyNotEmployed message, IMessageHandlerContext context)
    {
        Console.WriteLine($"Received VerifyNotEmployed: {message.ApprenticeshipId}, {message.CorrelationId}, {message.GetType().FullName}");
        ReceivedMessages.Add(message);
        return Task.CompletedTask;
    }

    public static IEnumerable<VerifyNotEmployed> GetMessages(long apprenticeshipId)
    {
        return ReceivedMessages.Where(message => message.ApprenticeshipId == apprenticeshipId);
    }
}