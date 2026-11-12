using System;
using System.Threading.Tasks;
using SFA.DAS.Payments.DataLocks.Messages.Events;
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.DataLocks.Domain.Services.Apprenticeships
{
    public interface IApprenticeshipEmploymentVerificationRepository : IDisposable
    {
        Task<ApprenticeshipEmploymentVerificationModel> Get(Guid correlationId, long apprenticeshipId);
        Task Add(ApprenticeshipEmploymentVerificationModel request);
        Task<bool> UpdatePreEmploymentStatus(Guid correlationId, long apprenticeshipId, EmploymentCheckResult status);
    }
}