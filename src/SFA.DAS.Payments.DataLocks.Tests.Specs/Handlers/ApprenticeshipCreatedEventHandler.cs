using System.Collections.Concurrent;
using SFA.DAS.CommitmentsV2.Messages.Events;

namespace SFA.DAS.Payments.DataLocks.Tests.Specs.Handlers;

public class ApprenticeshipCreatedEventHandler : IHandleMessages<ApprenticeshipCreatedEvent>
{
    public Task Handle(ApprenticeshipCreatedEvent message, IMessageHandlerContext context)
    {
        throw new NotImplementedException();
    }
}
