﻿using System.Collections.Generic;
using SFA.DAS.Payments.Model.Core;

namespace SFA.DAS.Payments.DataLocks.Model.Entities
{
    public class DataLockFailureModel
    {
        public long Id { get; set; }
        public long Ukprn { get; set; }
        public string LearnerReferenceNumber { get; set; }
        public string LearningAimReference { get; set; }
        public long LearnerUln { get; set; }
        public int LearningAimProgrammeType { get; set; }
        public int LearningAimStandardCode { get; set; }
        public int LearningAimFrameworkCode { get; set; }
        public int LearningAimPathwayCode { get; set; }
        public short AcademicYear { get; set; }
        public byte DeliveryPeriod { get; set; }
        public List<DataLockFailure> Errors { get; set; }
    }
}