using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.Payments.Application.Repositories;
using SFA.DAS.Payments.DataLocks.Domain.Services.Apprenticeships;
using SFA.DAS.Payments.DataLocks.Messages.Events;
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.DataLocks.Application.Repositories
{
    public class ApprenticeshipEmploymentVerificationRepository : IApprenticeshipEmploymentVerificationRepository
    {
        private readonly IPaymentsDataContext dataContext;

        public ApprenticeshipEmploymentVerificationRepository(IPaymentsDataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<ApprenticeshipEmploymentVerificationModel> GetByCorrelationIdAndApprenticeshipId(Guid correlationId, long apprenticeshipId)
        {
            return await dataContext.ApprenticeshipEmploymentVerification
                .FirstOrDefaultAsync(x =>
                    x.ApprenticeshipId == apprenticeshipId &&
                    x.PreEmploymentCorrelationId == correlationId)
                .ConfigureAwait(false);
        }

        public async Task Add(ApprenticeshipEmploymentVerificationModel request)
        {
            await dataContext.ApprenticeshipEmploymentVerification.AddAsync(request).ConfigureAwait(false);
            await dataContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<bool> SetPreEmploymentStatus(Guid correlationId, long apprenticeshipId, EmploymentCheckResult status)
        {
            var record = await dataContext.ApprenticeshipEmploymentVerification
                .FirstOrDefaultAsync(x =>
                    x.ApprenticeshipId == apprenticeshipId &&
                    x.PreEmploymentCorrelationId == correlationId)
                .ConfigureAwait(false);

            if (record == null)
            {
                return false;
            }

            record.PreEmploymentStatus = (byte)status;
            record.UpdateDate = DateTime.UtcNow;

            await dataContext.SaveChangesAsync().ConfigureAwait(false);

            return true;
        }

        public void Dispose()
        {
            (dataContext as PaymentsDataContext)?.Dispose();
        }
    }
}
