using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using NServiceBus;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Application.Messaging;
using SFA.DAS.Payments.DataLocks.Domain.Services.Apprenticeships;
using SFA.DAS.Payments.DataLocks.Messages.Events;
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.DataLocks.Application.Services
{
    public interface IApprenticeshipHiringPaymentProcessor
    {
        Task Process(ApprenticeshipCreatedEvent createdEvent);
    }

    public class ApprenticeshipHiringPaymentProcessor : IApprenticeshipHiringPaymentProcessor
    {
        private readonly IPaymentLogger logger;
        private readonly IMapper mapper;
        private readonly IEndpointInstanceFactory endpointInstanceFactory;
        private readonly IApprenticeshipEmploymentVerificationService employmentVerificationService;

        private const int LearnerAgeThreshold = 25;
        private const int MaximumPreEmploymentDays = 90;
        private const int PreEmploymentCheckDurationDays = 60;

        private static readonly DateTime EarliestEligibleLearningStartDate = new DateTime(2026, 10, 1);
        private static readonly DateTime SpecialCaseEmploymentCheckStartDate = new DateTime(2026, 7, 1);

        public ApprenticeshipHiringPaymentProcessor(
            IPaymentLogger logger,
            IMapper mapper,
            IEndpointInstanceFactory endpointInstanceFactory,
            IApprenticeshipEmploymentVerificationService employmentVerificationService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            this.endpointInstanceFactory = endpointInstanceFactory ?? throw new ArgumentNullException(nameof(endpointInstanceFactory));
            this.employmentVerificationService = employmentVerificationService ?? throw new ArgumentNullException(nameof(employmentVerificationService));
        }

        public async Task Process(ApprenticeshipCreatedEvent createdEvent)
        {
            try
            {
                if (createdEvent == null)
                {
                    throw new ArgumentNullException(nameof(createdEvent));
                }

                var model = mapper.Map<ApprenticeshipModel>(createdEvent);

                logger.LogDebug($"Now checking apprenticeship hiring payment eligibility for Apprenticeship id: {createdEvent.ApprenticeshipId}.");

                if (model.IsLevyPayer)
                {
                    logger.LogInfo($"Hiring payment not applicable for apprenticeship {createdEvent.ApprenticeshipId} because the employer is a levy payer.");
                    return;
                }

                var learningStartDate = createdEvent.StartDate;
                var learnerDateOfBirth = createdEvent.DateOfBirth;

                if (learningStartDate == default || learnerDateOfBirth == default)
                {
                    logger.LogInfo($"Hiring payment skipped for apprenticeship {createdEvent.ApprenticeshipId} because the learning start date or date of birth was not supplied.");
                    return;
                }

                if (learningStartDate.Date < EarliestEligibleLearningStartDate)
                {
                    logger.LogInfo($"Hiring payment not eligible for apprenticeship {createdEvent.ApprenticeshipId} because the learning start date is before 01/10/2026.");
                    return;
                }

                var ageAtStartDate = CalculateAgeAt(learningStartDate, learnerDateOfBirth);
                if (ageAtStartDate >= LearnerAgeThreshold)
                {
                    logger.LogInfo($"Hiring payment not eligible for apprenticeship {createdEvent.ApprenticeshipId} because the learner is age {ageAtStartDate} at learning start date.");
                    return;
                }

                var employmentStartDate = learningStartDate.Date == EarliestEligibleLearningStartDate
                    ? SpecialCaseEmploymentCheckStartDate
                    : learningStartDate.Date.AddDays(-MaximumPreEmploymentDays);


                var correlationId = Guid.NewGuid();
                var apprenticeshipId = createdEvent.ApprenticeshipId;
                var uln = model.Uln;
                var accountId = createdEvent.AccountId;

                var employmentVerificationModel = new ApprenticeshipEmploymentVerificationModel
                {
                    ApprenticeshipId = apprenticeshipId,
                    PreEmploymentStatus = (byte)EmploymentCheckResult.Pending,
                    PreEmploymentCorrelationId = correlationId,
                    FirstHiringGrantStatus = (byte)EmploymentCheckResult.NotChecked,
                    FirstHiringGrantCorrelationId = null,
                    SecondHiringGrantStatus = (byte)EmploymentCheckResult.NotChecked,
                    SecondHiringGrantCorrelationId = null,
                    CreationDate = DateTime.UtcNow,
                    UpdateDate = DateTime.UtcNow
                };
                await employmentVerificationService.CreatePending(employmentVerificationModel).ConfigureAwait(false);

                var verifyNotEmployedMessage = new VerifyNotEmployed
                {
                    CorrelationId = correlationId,
                    ApprenticeshipId = apprenticeshipId,
                    ULN = uln,
                    EmployerAccountIdentifier = accountId,
                    StartDate = employmentStartDate,
                    Reference = string.Empty,
                    DurationInDays = PreEmploymentCheckDurationDays
                };
                var endpointInstance = await endpointInstanceFactory.GetEndpointInstance().ConfigureAwait(false);
                await endpointInstance.Publish(verifyNotEmployedMessage, new PublishOptions()).ConfigureAwait(false);

                logger.LogInfo($"Published VerifyNotEmployed message for apprenticeship {createdEvent.ApprenticeshipId}. Employment check start date: {employmentStartDate:yyyy-MM-dd}.");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing the apprenticeship hiring payment eligibility check. Error: {ex.Message}", ex);
                throw;
            }
        }

        private static int CalculateAgeAt(DateTime learningStartDate, DateTime dateOfBirth)
        {
            var age = learningStartDate.Year - dateOfBirth.Year;
            if (learningStartDate.Date < dateOfBirth.Date.AddYears(age))
            {
                age--;
            }

            return age;
        }
    }
}