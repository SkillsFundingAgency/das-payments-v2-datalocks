using System;
using System.Threading.Tasks;
using SFA.DAS.Payments.DataLocks.Messages.Events;
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.DataLocks.Domain.Services.Apprenticeships
{
    public interface IApprenticeshipEmploymentVerificationService
    {
        Task<ApprenticeshipEmploymentVerificationModel> Get(Guid correlationId, long apprenticeshipId);
        Task CreatePending(ApprenticeshipEmploymentVerificationModel model);
        Task<bool> UpdatePreEmploymentStatus(Guid correlationId, long apprenticeshipId, EmploymentCheckResult status);
    }

    public class ApprenticeshipEmploymentVerificationService : IApprenticeshipEmploymentVerificationService
    {
        private readonly IApprenticeshipEmploymentVerificationRepository repository;

        public ApprenticeshipEmploymentVerificationService(IApprenticeshipEmploymentVerificationRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task CreatePending(ApprenticeshipEmploymentVerificationModel model)
        {
            await repository.Add(model).ConfigureAwait(false);
        }

        public Task<bool> UpdatePreEmploymentStatus(Guid correlationId, long apprenticeshipId, EmploymentCheckResult status)
        {
            return repository.UpdatePreEmploymentStatus(correlationId, apprenticeshipId, status);
        }

        public Task<ApprenticeshipEmploymentVerificationModel> Get(Guid correlationId, long apprenticeshipId)
        {
            return repository.Get(correlationId, apprenticeshipId);
        }
    }
}