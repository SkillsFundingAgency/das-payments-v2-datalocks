using System;
using System.Threading.Tasks;
using SFA.DAS.Payments.DataLocks.Messages.Events;
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.DataLocks.Domain.Services.Apprenticeships
{
    public interface IApprenticeshipEmploymentVerificationService
    {
        Task<ApprenticeshipEmploymentVerificationModel> GetByCorrelationIdAndApprenticeshipId(Guid correlationId, long apprenticeshipId);
        Task CreatePending(ApprenticeshipEmploymentVerificationModel model);
        Task<bool> SetPreEmploymentStatus(Guid correlationId, long apprenticeshipId, EmploymentValidationStatus status);
    }

    public class ApprenticeshipEmploymentVerificationService : IApprenticeshipEmploymentVerificationService
    {
        private readonly IApprenticeshipEmploymentVerificationRepository repository;

        public Task<ApprenticeshipEmploymentVerificationModel> GetByCorrelationIdAndApprenticeshipId(Guid correlationId, long apprenticeshipId)
        {
            return repository.GetByCorrelationIdAndApprenticeshipId(correlationId, apprenticeshipId);
        }

        public ApprenticeshipEmploymentVerificationService(IApprenticeshipEmploymentVerificationRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task CreatePending(ApprenticeshipEmploymentVerificationModel model)
        {
            await repository.Add(model).ConfigureAwait(false);
        }

        public Task<bool> SetPreEmploymentStatus(Guid correlationId, long apprenticeshipId, EmploymentValidationStatus status)
        {
            return repository.SetPreEmploymentStatus(correlationId, apprenticeshipId, status);
        }
    }
}