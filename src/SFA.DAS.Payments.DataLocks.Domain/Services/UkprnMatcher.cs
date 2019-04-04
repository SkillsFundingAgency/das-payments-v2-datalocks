﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SFA.DAS.Payments.DataLocks.Domain.Interfaces;
using SFA.DAS.Payments.DataLocks.Domain.Models;

namespace SFA.DAS.Payments.DataLocks.Domain.Services
{
    public class UkprnMatcher: IUkprnMatcher
    {
        private readonly IDataLockLearnerCache dataLockLearnerCache;

        public UkprnMatcher(IDataLockLearnerCache dataLockLearnerCache)
        {
            this.dataLockLearnerCache = dataLockLearnerCache;
        }
        public async Task<DataLockErrorCode?> MatchUkprn()
        {
            var hasLearnerData = await dataLockLearnerCache.HasLearnerRecords();
            if (!hasLearnerData) return DataLockErrorCode.DLOCK_01;

            return default(DataLockErrorCode?);
        }
    }
}
