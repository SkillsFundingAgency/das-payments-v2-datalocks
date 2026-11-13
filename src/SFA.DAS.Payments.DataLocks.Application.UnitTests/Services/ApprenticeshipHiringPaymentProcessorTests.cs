using AutoMapper;
using FluentAssertions;
using Moq;
using NServiceBus;
using NUnit.Framework;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.EmploymentVerificationService.Messages.Learner.Commands;
using SFA.DAS.EmploymentVerificationService.Messages.Learner.Events;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Application.Messaging;
using SFA.DAS.Payments.DataLocks.Application.Services;
using SFA.DAS.Payments.DataLocks.Domain.Services.Apprenticeships;
using SFA.DAS.Payments.DataLocks.Messages.Events;
using SFA.DAS.Payments.Model.Core.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.Payments.DataLocks.Application.UnitTests.Services
{
    [TestFixture]
    public class ApprenticeshipHiringPaymentProcessorTests
    {
        private Mock<IPaymentLogger> logger;
        private Mock<IMapper> mapper;
        private Mock<IEndpointInstanceFactory> endpointInstanceFactory;
        private Mock<IEndpointInstance> endpointInstance;
        private Mock<IApprenticeshipEmploymentVerificationService> employmentVerificationService;

        [SetUp]
        public void SetUp()
        {
            logger = new Mock<IPaymentLogger>();
            mapper = new Mock<IMapper>();
            endpointInstanceFactory = new Mock<IEndpointInstanceFactory>();
            endpointInstance = new Mock<IEndpointInstance>();
            employmentVerificationService = new Mock<IApprenticeshipEmploymentVerificationService>();

            endpointInstanceFactory
                .Setup(factory => factory.GetEndpointInstance())
                .ReturnsAsync(endpointInstance.Object);

            endpointInstance
                .Setup(instance => instance.Publish(
                    It.IsAny<object>(),
                    It.IsAny<PublishOptions>(),
                    CancellationToken.None))
                .Returns(Task.CompletedTask);

            employmentVerificationService
                .Setup(service => service.CreatePending(It.IsAny<ApprenticeshipEmploymentVerificationModel>()))
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
            SetupMappedModel(isLevyPayer: true);

            await CreateSut().Process(
                CreateEvent(new DateTime(2026, 10, 1), new DateTime(2001, 10, 2)));

            employmentVerificationService.Verify(
                service => service.CreatePending(It.IsAny<ApprenticeshipEmploymentVerificationModel>()),
                Times.Never);

            endpointInstance.Verify(
                instance => instance.Publish(
                    It.IsAny<object>(),
                    It.IsAny<PublishOptions>(),
                    CancellationToken.None),
                Times.Never);
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
                service => service.CreatePending(It.IsAny<ApprenticeshipEmploymentVerificationModel>()),
                Times.Never);

            endpointInstance.Verify(
                instance => instance.Publish(
                    It.IsAny<object>(),
                    It.IsAny<PublishOptions>(),
                    CancellationToken.None),
                Times.Never);
        }

        [Test]
        public async Task Process_WithLearningStartDateBeforeEligibilityDate_DoesNotSaveOrPublish()
        {
            await CreateSut().Process(
                CreateEvent(new DateTime(2026, 9, 30), new DateTime(2001, 10, 2)));

            employmentVerificationService.Verify(
                service => service.CreatePending(It.IsAny<ApprenticeshipEmploymentVerificationModel>()),
                Times.Never);

            endpointInstance.Verify(
                instance => instance.Publish(
                    It.IsAny<object>(),
                    It.IsAny<PublishOptions>(),
                    CancellationToken.None),
                Times.Never);
        }

        [Test]
        public async Task Process_WithLearnerAge25OrOlder_DoesNotSaveOrPublish()
        {
            await CreateSut().Process(
                CreateEvent(new DateTime(2026, 10, 1), new DateTime(2001, 10, 1)));

            employmentVerificationService.Verify(
                service => service.CreatePending(It.IsAny<ApprenticeshipEmploymentVerificationModel>()),
                Times.Never);

            endpointInstance.Verify(
                instance => instance.Publish(
                    It.IsAny<object>(),
                    It.IsAny<PublishOptions>(),
                    CancellationToken.None),
                Times.Never);
        }

        [Test]
        public async Task Process_ForOctober1_CreatesPendingRecordAndPublishesVerification()
        {
            ApprenticeshipEmploymentVerificationModel pendingModel = null;
            VerifyNotEmployed publishedMessage = null;

            employmentVerificationService
                .Setup(service => service.CreatePending(It.IsAny<ApprenticeshipEmploymentVerificationModel>()))
                .Callback<ApprenticeshipEmploymentVerificationModel>(model => pendingModel = model)
                .Returns(Task.CompletedTask);

            endpointInstance
                .Setup(instance => instance.Publish(
                    It.IsAny<object>(),
                    It.IsAny<PublishOptions>(),
                    CancellationToken.None))
                .Callback<object, PublishOptions, CancellationToken>(
                    (message, _, _) => publishedMessage = (VerifyNotEmployed)message)
                .Returns(Task.CompletedTask);

            await CreateSut().Process(
                CreateEvent(new DateTime(2026, 10, 1), new DateTime(2001, 10, 2)));

            pendingModel.Should().NotBeNull();
            pendingModel.ApprenticeshipId.Should().Be(123);
            pendingModel.PreEmploymentStatus.Should().Be((byte)EmploymentValidationStatus.Pending);
            pendingModel.PreEmploymentCorrelationId.Should().NotBeEmpty();
            pendingModel.FirstHiringGrantStatus.Should().Be((byte)EmploymentValidationStatus.NotChecked);
            pendingModel.SecondHiringGrantStatus.Should().Be((byte)EmploymentValidationStatus.NotChecked);

            publishedMessage.Should().NotBeNull();
            publishedMessage.CorrelationId.Should().Be(pendingModel.PreEmploymentCorrelationId.ToString());
            publishedMessage.ApprenticeshipId.Should().Be(123);
            publishedMessage.ULN.Should().Be(456789);
            publishedMessage.EmployerAccountIdentifier.Should().Be(987);
            publishedMessage.StartDate.Should().Be(new DateTime(2026, 7, 1));
            publishedMessage.DurationInDays.Should().Be(60);
        }

        [Test]
        public async Task Process_ForOtherEligibleStartDate_Uses150DayLookback()
        {
            VerifyNotEmployed publishedMessage = null;

            endpointInstance
                .Setup(instance => instance.Publish(
                    It.IsAny<object>(),
                    It.IsAny<PublishOptions>(),
                    CancellationToken.None))
                .Callback<object, PublishOptions, CancellationToken>(
                    (message, _, _) => publishedMessage = (VerifyNotEmployed)message)
                .Returns(Task.CompletedTask);

            await CreateSut().Process(
                CreateEvent(new DateTime(2026, 10, 2), new DateTime(2001, 10, 3)));

            publishedMessage.Should().NotBeNull();
            publishedMessage.StartDate.Should().Be(new DateTime(2026, 5, 5));
            publishedMessage.DurationInDays.Should().Be(60);
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
                endpointInstanceFactory.Object,
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
                StartDate = startDate,
                DateOfBirth = dateOfBirth
            };
        }
    }
}