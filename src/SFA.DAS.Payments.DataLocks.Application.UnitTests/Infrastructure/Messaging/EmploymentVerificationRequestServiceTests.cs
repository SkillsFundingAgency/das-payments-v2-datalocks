using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NServiceBus;
using NUnit.Framework;
using SFA.DAS.EmploymentVerificationService.Messages.Learner.Commands;
using SFA.DAS.Payments.Application.Messaging;
using SFA.DAS.Payments.DataLocks.Application.Infrastructure.Messaging;

namespace SFA.DAS.Payments.DataLocks.Application.UnitTests.Infrastructure.Messaging
{
    [TestFixture]
    public class EmploymentVerificationRequestServiceTests
    {
        [Test]
        public async Task PublishVerifyNotEmployed_PublishesMessageWithExpectedValues()
        {
            var correlationId = Guid.NewGuid();
            var apprenticeshipId = 123L;
            var uln = 456789L;
            var accountId = 987L;
            var employmentStartDate = new DateTime(2026, 7, 1);
            var durationInDays = 60;
            VerifyNotEmployed publishedMessage = null;

            var endpointInstanceFactory = new Mock<IEndpointInstanceFactory>();
            var endpointInstance = new Mock<IEndpointInstance>();

            endpointInstanceFactory
                .Setup(factory => factory.GetEndpointInstance())
                .ReturnsAsync(endpointInstance.Object);

            endpointInstance
                .Setup(instance => instance.Publish(
                    It.IsAny<object>(),
                    It.IsAny<PublishOptions>(),
                    CancellationToken.None))
                .Callback<object, PublishOptions, CancellationToken>(
                    (message, _, _) => publishedMessage = message as VerifyNotEmployed)
                .Returns(Task.CompletedTask);

            var sut = new EmploymentVerificationRequestService(endpointInstanceFactory.Object);

            await sut.PublishVerifyNotEmployed(
                correlationId,
                apprenticeshipId,
                uln,
                accountId,
                employmentStartDate,
                durationInDays);

            publishedMessage.Should().NotBeNull();
            publishedMessage.CorrelationId.Should().Be(correlationId);
            publishedMessage.ApprenticeshipId.Should().Be(apprenticeshipId);
            publishedMessage.ULN.Should().Be(uln);
            publishedMessage.EmployerAccountIdentifier.Should().Be(accountId);
            publishedMessage.StartDate.Should().Be(employmentStartDate);
            publishedMessage.Reference.Should().Be(
                $"Apprenticeship Hiring Payment Eligibility Check - ApprenticeshipId:{apprenticeshipId}");
            publishedMessage.DurationInDays.Should().Be(durationInDays);

            endpointInstanceFactory.Verify(
                factory => factory.GetEndpointInstance(),
                Times.Once);

            endpointInstance.Verify(
                instance => instance.Publish(
                    It.IsAny<object>(),
                    It.IsAny<PublishOptions>(),
                    CancellationToken.None),
                Times.Once);
        }
    }
}