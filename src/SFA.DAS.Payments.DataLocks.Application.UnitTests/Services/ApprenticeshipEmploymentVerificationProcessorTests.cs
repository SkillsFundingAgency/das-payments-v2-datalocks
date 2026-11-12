using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.DataLocks.Application.Services;
using SFA.DAS.Payments.DataLocks.Domain.Services.Apprenticeships;
using SFA.DAS.Payments.DataLocks.Messages.Events;

namespace SFA.DAS.Payments.DataLocks.Application.UnitTests.Services
{
    [TestFixture]
    public class ApprenticeshipEmploymentVerificationProcessorTests
    {
        private Mock<IPaymentLogger> logger;
        private Mock<IApprenticeshipEmploymentVerificationService> employmentVerificationService;

        [SetUp]
        public void SetUp()
        {
            logger = new Mock<IPaymentLogger>();
            employmentVerificationService = new Mock<IApprenticeshipEmploymentVerificationService>();
        }

        [TestCase(true, EmploymentCheckResult.NotEmployed)]
        [TestCase(false, EmploymentCheckResult.Employed)]
        public async Task Process_UpdatesStatusFromCompletionMessage(
            bool notEmployed,
            EmploymentCheckResult expectedStatus)
        {
            var correlationId = Guid.NewGuid();

            employmentVerificationService
                .Setup(service => service.UpdatePreEmploymentStatus(correlationId, 123, expectedStatus))
                .ReturnsAsync(true);

            await CreateSut().Process(new VerifyNotEmployedCompleted
            {
                CorrelationId = correlationId,
                ApprenticeshipId = 123,
                NotEmployed = notEmployed
            });

            employmentVerificationService.Verify(
                service => service.UpdatePreEmploymentStatus(correlationId, 123, expectedStatus),
                Times.Once);
        }

        [Test]
        public async Task Process_WhenRecordIsNotFound_CompletesWithoutRetrying()
        {
            var correlationId = Guid.NewGuid();

            employmentVerificationService
                .Setup(service => service.UpdatePreEmploymentStatus(
                    correlationId,
                    123,
                    EmploymentCheckResult.NotEmployed))
                .ReturnsAsync(false);

            Func<Task> action = () => CreateSut().Process(new VerifyNotEmployedCompleted
            {
                CorrelationId = correlationId,
                ApprenticeshipId = 123,
                NotEmployed = true
            });

            await action.Should().NotThrowAsync();

            employmentVerificationService.Verify(
                service => service.UpdatePreEmploymentStatus(
                    correlationId,
                    123,
                    EmploymentCheckResult.NotEmployed),
                Times.Once);
        }

        [Test]
        public async Task Process_WithNullMessage_ThrowsArgumentNullException()
        {
            Func<Task> action = () => CreateSut().Process(null);

            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [Test]
        public async Task Process_WhenServiceThrows_RethrowsTheException()
        {
            var correlationId = Guid.NewGuid();
            var expectedException = new InvalidOperationException("Service failure");

            employmentVerificationService
                .Setup(service => service.UpdatePreEmploymentStatus(
                    correlationId,
                    123,
                    EmploymentCheckResult.Employed))
                .ThrowsAsync(expectedException);

            Func<Task> action = () => CreateSut().Process(new VerifyNotEmployedCompleted
            {
                CorrelationId = correlationId,
                ApprenticeshipId = 123,
                NotEmployed = false
            });

            var exception = await action.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Message.Should().Be("Service failure");
        }

        private ApprenticeshipEmploymentVerificationProcessor CreateSut()
        {
            return new ApprenticeshipEmploymentVerificationProcessor(
                logger.Object,
                employmentVerificationService.Object);
        }
    }
}