using SFA.DAS.Payments.DataLocks.Model.Entities;

namespace SFA.DAS.Payments.DataLocks.Messages.Events
{
    public class PriceEpisodeStatusChange
    {
        public decimal AgreedPrice { get; set; }
        public LegacyDataLockEvent DataLock { get; set; }
        public LegacyDataLockEventCommitmentVersion[] CommitmentVersions { get; set; }
        public LegacyDataLockEventError[] Errors { get; set; }
        public LegacyDataLockEventPeriod[] Periods { get; set; }
    }
}