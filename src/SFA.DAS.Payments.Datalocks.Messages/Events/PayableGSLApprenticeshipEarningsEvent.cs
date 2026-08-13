using System;
using SFA.DAS.Payments.Messages.Common;
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.DataLocks.Messages.Events
{
    public class PayableGSLApprenticeshipEarningsEvent: DataLockEvent, IPayableEarningEvent
    {
        public DateTime StartDate { get; set; }
        public int? AgeAtStartOfLearning { get; set; }
        public PayableGSLApprenticeshipEarningsEvent() => FundingPlatformType = FundingPlatformType.DigitalApprenticeshipService;
    }
}