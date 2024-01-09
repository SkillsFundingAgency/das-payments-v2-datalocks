using System;
using SFA.DAS.Payments.Messages.Common;

namespace SFA.DAS.Payments.DataLocks.Messages.Events
{
    public class EarningFailedDataLockMatching : DataLockEvent, IMonitoredMessage
    {
        public DateTime StartDate { get; set; }
    }
}