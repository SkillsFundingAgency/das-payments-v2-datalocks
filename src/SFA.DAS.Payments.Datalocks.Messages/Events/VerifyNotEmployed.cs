using System;
using SFA.DAS.Payments.Messages.Common.Events;

namespace SFA.DAS.Payments.DataLocks.Messages.Events
{
    public class VerifyNotEmployed
    {
        public Guid CorrelationId { get; set; }
        public long ULN { get; set; }
        public long ApprenticeshipId { get; set; }
        public long EmployerAccountIdentifier { get; set; }
        public int? DurationInDays { get; set; }
        public DateTime StartDate { get; set; }
        public string Reference { get; set; }
    }

    public class VerifyNotEmployedCompleted
    {
        public Guid CorrelationId { get; set; }
        public long ULN { get; set; }
        public long ApprenticeshipId { get; set; }
        public long EmployerAccountIdentifier { get; set; }
        public bool NotEmployed { get; set; }
    }

    public enum EmploymentCheckResult : byte
    {
        NotChecked = 1,
        Pending,
        Employed,
        NotEmployed,
        Failed
    }
}
