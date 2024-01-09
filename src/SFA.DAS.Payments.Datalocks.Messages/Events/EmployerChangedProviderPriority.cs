using System;
using System.Collections.Generic;
using SFA.DAS.Payments.Messages.Common.Events;

namespace SFA.DAS.Payments.DataLocks.Messages.Events
{
    public class EmployerChangedProviderPriority : IEvent
    {
        public EmployerChangedProviderPriority()
        {
            EventId = Guid.NewGuid();
            EventTime = DateTimeOffset.UtcNow;
        }

        public long EmployerAccountId { get; set; }
        public List<long> OrderedProviders { get; set; }
        public Guid EventId { get; set; }
        public DateTimeOffset EventTime { get; set; }
    }
}