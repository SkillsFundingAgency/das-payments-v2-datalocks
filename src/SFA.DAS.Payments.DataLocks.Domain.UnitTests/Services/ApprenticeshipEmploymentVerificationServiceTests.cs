using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.Payments.DataLocks.Domain.Services.Apprenticeships;
using SFA.DAS.Payments.DataLocks.Messages.Events;
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.DataLocks.Domain.UnitTests.Services
{
    [TestFixture]
    public class ApprenticeshipEmploymentVerificationServiceTests
    {
        private Mock<IApprenticeshipEmploymentVerificationRepository> repository;
        private ApprenticeshipEmploymentVerificationService sut;

        [SetUp]
        public void SetUp()
        {
            repository = new Mock<IApprenticeshipEmploymentVerificationRepository>();
            sut = new ApprenticeshipEmploymentVerificationService(repository.Object);
        }

        [Test]
        public async Task GetByCorrelationIdAndApprenticeshipId_DelegatesToRepository()
        {
            var correlationId = Guid.NewGuid();
            var apprenticeshipId = 123L;
            var expected = new ApprenticeshipEmploymentVerificationModel
            {
                ApprenticeshipId = apprenticeshipId,
                PreEmploymentCorrelationId = correlationId
            };

            repository
                .Setup(x => x.GetByCorrelationIdAndApprenticeshipId(correlationId, apprenticeshipId))
                .ReturnsAsync(expected);

            var result = await sut.GetByCorrelationIdAndApprenticeshipId(correlationId, apprenticeshipId);

            result.Should().BeSameAs(expected);
            repository.Verify(
                x => x.GetByCorrelationIdAndApprenticeshipId(correlationId, apprenticeshipId),
                Times.Once);
        }

        [Test]
        public async Task CreatePending_DelegatesToRepository()
        {
            var model = new ApprenticeshipEmploymentVerificationModel
            {
                ApprenticeshipId = 123,
                PreEmploymentCorrelationId = Guid.NewGuid()
            };

            repository
                .Setup(x => x.Add(model))
                .Returns(Task.CompletedTask);

            await sut.CreatePending(model);

            repository.Verify(x => x.Add(model), Times.Once);
        }

        [TestCase(EmploymentCheckResult.NotEmployed, true)]
        [TestCase(EmploymentCheckResult.Employed, false)]
        public async Task SetPreEmploymentStatus_DelegatesToRepository(
            EmploymentCheckResult status,
            bool expectedResult)
        {
            var correlationId = Guid.NewGuid();
            const long apprenticeshipId = 123;

            repository
                .Setup(x => x.SetPreEmploymentStatus(correlationId, apprenticeshipId, status))
                .ReturnsAsync(expectedResult);

            var result = await sut.SetPreEmploymentStatus(correlationId, apprenticeshipId, status);

            result.Should().Be(expectedResult);
            repository.Verify(
                x => x.SetPreEmploymentStatus(correlationId, apprenticeshipId, status),
                Times.Once);
        }
    }
}