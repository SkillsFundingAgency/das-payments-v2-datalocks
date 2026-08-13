using System;

namespace SFA.DAS.Payments.DataLocks.Messages.Events
{
    public class PayableGSLApprenticeshipEarningsEvent: DataLockEvent, IPayableEarningEvent
    {
        public DateTime StartDate { get; set; }
        public int? AgeAtStartOfLearning { get; set; }
    }
}