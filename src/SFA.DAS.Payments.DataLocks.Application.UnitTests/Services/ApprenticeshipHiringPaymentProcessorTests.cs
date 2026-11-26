using System;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.EmploymentVerificationService.Messages.Learner.Commands;
using SFA.DAS.EmploymentVerificationService.Messages.Learner.Events;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.DataLocks.Application.Infrastructure.Messaging;
using SFA.DAS.Payments.DataLocks.Application.Services;
using SFA.DAS.Payments.DataLocks.Domain.Services.Apprenticeships;
using SFA.DAS.Payments.DataLocks.Messages.Events;
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.DataLocks.Application.UnitTests.Services
{
    [TestFixture]
    public class ApprenticeshipHiringPaymentProcessorTests
    {
        private Mock<IPaymentLogger> logger;
        private Mock<IMapper> mapper;
        private Mock<IApprenticeshipEmploymentVerificationService> employmentVerificationService;
        private Mock<IEmploymentVerificationMessageSender> employmentVerificationRequestService;

        [SetUp]
        public void SetUp()
        {
            logger = new Mock<IPaymentLogger>();
            mapper = new Mock<IMapper>();
            employmentVerificationService = new Mock<IApprenticeshipEmploymentVerificationService>();
            employmentVerificationRequestService = new Mock<IEmploymentVerificationMessageSender>();

            employmentVerificationService
                .Setup(service => service.Save(It.IsAny<ApprenticeshipEmploymentVerificationModel>()))
                .Returns(Task.CompletedTask);

            employmentVerificationRequestService
                .Setup(sender => sender.Send(It.IsAny<VerifyNotEmployed>()))
                .Returns(Task.CompletedTask);

            SetupMappedModel(isLevyPayer: false);
        }

        [Test]
        public async Task Process_WithNullEvent_ThrowsArgumentNullException()
        {
            Func<Task> action = () => CreateSut().Process(null);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Test]
        public async Task Process_WithLevyPayer_DoesNotSaveOrPublish()
        {
            var createdEvent = CreateEvent(
                new DateTime(2026, 10, 1),
                new DateTime(2001, 10, 2));

            createdEvent.ApprenticeshipEmployerTypeOnApproval =
                SFA.DAS.CommitmentsV2.Types.ApprenticeshipEmployerType.Levy;

            await CreateSut().Process(createdEvent);

            employmentVerificationService.Verify(
                service => service.Save(It.IsAny<ApprenticeshipEmploymentVerificationModel>()),
                Times.Never);

            VerifyNoVerificationRequestPublished();
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public async Task Process_WithMissingStartDateOrDateOfBirth_DoesNotSaveOrPublish(
            bool missingStartDate,
            bool missingDateOfBirth)
        {
            var createdEvent = CreateEvent(
                missingStartDate ? default : new DateTime(2026, 10, 1),
                missingDateOfBirth ? default : new DateTime(2001, 10, 2));

            await CreateSut().Process(createdEvent);

            employmentVerificationService.Verify(
                service => service.Save(It.IsAny<ApprenticeshipEmploymentVerificationModel>()),
                Times.Never);

            VerifyNoVerificationRequestPublished();
        }

        [Test]
        public async Task Process_WithLearningStartDateBeforeEligibilityDate_DoesNotSaveOrPublish()
        {
            await CreateSut().Process(
                CreateEvent(new DateTime(2026, 9, 30), new DateTime(2001, 10, 2)));

            employmentVerificationService.Verify(
                service => service.Save(It.IsAny<ApprenticeshipEmploymentVerificationModel>()),
                Times.Never);

            VerifyNoVerificationRequestPublished();
        }

        [Test]
        public async Task Process_WithLearnerAge25OrOlder_DoesNotSaveOrPublish()
        {
            await CreateSut().Process(
                CreateEvent(new DateTime(2026, 10, 1), new DateTime(2001, 10, 1)));

            employmentVerificationService.Verify(
                service => service.Save(It.IsAny<ApprenticeshipEmploymentVerificationModel>()),
                Times.Never);

            VerifyNoVerificationRequestPublished();
        }

        [Test]
        public async Task Process_ForOctober1_CreatesPendingRecordAndRequestsVerification()
        {
            ApprenticeshipEmploymentVerificationModel pendingModel = null;

            employmentVerificationService
                .Setup(service => service.Save(It.IsAny<ApprenticeshipEmploymentVerificationModel>()))
                .Callback<ApprenticeshipEmploymentVerificationModel>(model => pendingModel = model)
                .Returns(Task.CompletedTask);

            await CreateSut().Process(
                CreateEvent(new DateTime(2026, 10, 1), new DateTime(2001, 10, 2)));

            pendingModel.Should().NotBeNull();
            pendingModel.ApprenticeshipId.Should().Be(123);
            pendingModel.PreEmploymentStatus.Should().Be((byte)EmploymentValidationStatus.Pending);
            pendingModel.PreEmploymentCorrelationId.Should().NotBeEmpty();
            pendingModel.FirstHiringGrantStatus.Should().Be((byte)EmploymentValidationStatus.NotChecked);
            pendingModel.SecondHiringGrantStatus.Should().Be((byte)EmploymentValidationStatus.NotChecked);

            employmentVerificationRequestService.Verify(
                sender => sender.Send(It.Is<VerifyNotEmployed>(message =>
                    message.ApprenticeshipId == 123 &&
                    message.ULN == 456789 &&
                    message.EmployerAccountIdentifier == 987 &&
                    message.StartDate == new DateTime(2026, 7, 1) &&
                    message.DurationInDays == 60 &&
                    message.Reference ==
                        "Apprenticeship Hiring Payment Eligibility Check - ApprenticeshipId:123")),
                Times.Once);
        }

        [Test]
        public async Task Process_ForOtherEligibleStartDate_Uses150DayLookback()
        {
            await CreateSut().Process(
                CreateEvent(new DateTime(2026, 10, 2), new DateTime(2001, 10, 3)));

            employmentVerificationRequestService.Verify(
                sender => sender.Send(It.Is<VerifyNotEmployed>(message =>
                    message.ApprenticeshipId == 123 &&
                    message.ULN == 456789 &&
                    message.EmployerAccountIdentifier == 987 &&
                    message.StartDate == new DateTime(2026, 5, 5) &&
                    message.DurationInDays == 60)),
                Times.Once);
        }

        [TestCase(true, EmploymentValidationStatus.NotEmployed)]
        [TestCase(false, EmploymentValidationStatus.Employed)]
        public async Task Process_UpdatesStatusFromCompletionMessage(
            bool notEmployed,
            EmploymentValidationStatus expectedStatus)
        {
            var correlationId = Guid.NewGuid();

            employmentVerificationService
                .Setup(service => service.SetPreEmploymentStatus(correlationId, 123, expectedStatus))
                .ReturnsAsync(true);

            await CreateSut().ProcessCompleted(new VerifyNotEmployedCompleted
            {
                CorrelationId = correlationId,
                ApprenticeshipId = 123,
                NotEmployed = notEmployed
            });

            employmentVerificationService.Verify(
                service => service.SetPreEmploymentStatus(correlationId, 123, expectedStatus),
                Times.Once);
        }

        [Test]
        public async Task Process_WhenRecordIsNotFound_CompletesWithoutRetrying()
        {
            var correlationId = Guid.NewGuid();

            employmentVerificationService
                .Setup(service => service.SetPreEmploymentStatus(
                    correlationId,
                    123,
                    EmploymentValidationStatus.NotEmployed))
                .ReturnsAsync(false);

            Func<Task> action = () => CreateSut().ProcessCompleted(new VerifyNotEmployedCompleted
            {
                CorrelationId = correlationId,
                ApprenticeshipId = 123,
                NotEmployed = true
            });

            await action.Should().NotThrowAsync();

            employmentVerificationService.Verify(
                service => service.SetPreEmploymentStatus(
                    correlationId,
                    123,
                    EmploymentValidationStatus.NotEmployed),
                Times.Once);
        }

        [Test]
        public async Task Process_WithNullMessage_ThrowsArgumentNullException()
        {
            Func<Task> action = () => CreateSut().Process(null);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Test]
        public async Task ProcessCompleted_WithNullMessage_ThrowsArgumentNullException()
        {
            Func<Task> action = () => CreateSut().ProcessCompleted(null);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Test]
        public async Task Process_WhenServiceThrows_RethrowsTheException()
        {
            var correlationId = Guid.NewGuid();
            var expectedException = new InvalidOperationException("Service failure");

            employmentVerificationService
                .Setup(service => service.SetPreEmploymentStatus(
                    correlationId,
                    123,
                    EmploymentValidationStatus.Employed))
                .ThrowsAsync(expectedException);

            Func<Task> action = () => CreateSut().ProcessCompleted(new VerifyNotEmployedCompleted
            {
                CorrelationId = correlationId,
                ApprenticeshipId = 123,
                NotEmployed = false
            });

            var exception = await action.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().Be("Service failure");
        }

        private ApprenticeshipHiringPaymentProcessor CreateSut()
        {
            return new ApprenticeshipHiringPaymentProcessor(
                logger.Object,
                mapper.Object,
                employmentVerificationRequestService.Object,
                employmentVerificationService.Object);
        }

        private void SetupMappedModel(bool isLevyPayer)
        {
            mapper
                .Setup(instance => instance.Map<ApprenticeshipModel>(It.IsAny<object>()))
                .Returns(new ApprenticeshipModel
                {
                    IsLevyPayer = isLevyPayer,
                    Uln = 456789
                });
        }

        private static ApprenticeshipCreatedEvent CreateEvent(DateTime startDate, DateTime dateOfBirth)
        {
            return new ApprenticeshipCreatedEvent
            {
                AccountId = 987,
                ApprenticeshipId = 123,
                Uln = "456789",
                StartDate = startDate,
                DateOfBirth = dateOfBirth
            };
        }

        private void VerifyNoVerificationRequestPublished()
        {
            employmentVerificationRequestService.Verify(
                sender => sender.Send(It.IsAny<VerifyNotEmployed>()),
                Times.Never);
        }
    }
}