﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autofac.Extras.Moq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.Payments.DataLocks.Domain.Models;
using SFA.DAS.Payments.DataLocks.Domain.Services;
using SFA.DAS.Payments.DataLocks.Domain.Services.CourseValidation;
using SFA.DAS.Payments.Model.Core;
using SFA.DAS.Payments.Model.Core.Entities;
using SFA.DAS.Payments.Model.Core.OnProgramme;

namespace SFA.DAS.Payments.DataLocks.Domain.UnitTests.Services.CourseValidation
{
    [TestFixture]
    public class EarningPeriodsValidationProcessorTests
    {
        private AutoMock mocker;
        private List<PriceEpisode> priceEpisodes;
        private List<ApprenticeshipModel> apprenticeships;
        private List<DataLockFailure> dataLockFailures;
        private LearningAim aim;
        private const int AcademicYear = 1819;
        private readonly int ukprn = 123;

        [SetUp]
        public void SetUp()
        {
            mocker = AutoMock.GetLoose();
            priceEpisodes = new List<PriceEpisode>
            {
                new PriceEpisode{Identifier = "pe-1"},
                new PriceEpisode{Identifier = "pe-2"},
                new PriceEpisode{Identifier = "pe-3"}
            };
            apprenticeships = new List<ApprenticeshipModel>
            {
                new ApprenticeshipModel
                {
                    Id = 1,
                    AccountId = 21,
                    Ukprn = ukprn,
                    ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                    {
                        new ApprenticeshipPriceEpisodeModel{Id = 90},
                        new ApprenticeshipPriceEpisodeModel{Id = 91},
                        new ApprenticeshipPriceEpisodeModel{Id = 92},
                        new ApprenticeshipPriceEpisodeModel{Id = 93},
                    }
                },
                new ApprenticeshipModel
                {
                    Id = 2,
                    AccountId = 22,
                    Ukprn = ukprn,
                    ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                    {
                        new ApprenticeshipPriceEpisodeModel{Id = 94},
                        new ApprenticeshipPriceEpisodeModel{Id = 95},
                        new ApprenticeshipPriceEpisodeModel{Id = 96},
                        new ApprenticeshipPriceEpisodeModel{Id = 97},
                    },
                }
            };

            dataLockFailures = new List<DataLockFailure>
            {
                new DataLockFailure
                {
                    ApprenticeshipPriceEpisodeIds = new List<long>(),
                    DataLockError = DataLockErrorCode.DLOCK_03
                }
            };

            aim = new LearningAim();
        }

        [Test]
        public void MatchedPriceEpisodeIsReturnedInValidPeriods()
        {
            var earning = new OnProgrammeEarning
            {
                Periods = new List<EarningPeriod> { new EarningPeriod { Amount = 1, PriceEpisodeIdentifier = "pe-1" } }.AsReadOnly()
            };
            mocker.Mock<ICourseValidationProcessor>()
                .Setup(x => x.ValidateCourse(It.Is<DataLockValidationModel>(model => model.Apprenticeship.Id == 1)))
                .Returns(() => new CourseValidationResult { MatchedPriceEpisode = new ApprenticeshipPriceEpisodeModel { Id = 90 } });
            mocker.Mock<ICourseValidationProcessor>()
                .Setup(x => x.ValidateCourse(It.Is<DataLockValidationModel>(model => model.Apprenticeship.Id == 2)))
                .Returns(() => new CourseValidationResult { DataLockFailures = dataLockFailures });

            var periods = mocker.Create<EarningPeriodsValidationProcessor>().ValidatePeriods(
                ukprn,
                1,
                priceEpisodes,
                earning.Periods.ToList(),
                (TransactionType)earning.Type,
                apprenticeships,
                aim,
                AcademicYear);

            periods.ValidPeriods.Count.Should().Be(1);
            periods.ValidPeriods.All(p => p.ApprenticeshipPriceEpisodeId == 90).Should().Be(true);
        }

        [Test]
        public void MatchesApprenticeship()
        {
            var earning = new OnProgrammeEarning
            {
                Periods = new List<EarningPeriod> { new EarningPeriod
                {
                    Amount = 1,
                    PriceEpisodeIdentifier = "pe-1",
                } }.AsReadOnly()
            };
            mocker.Mock<ICourseValidationProcessor>()
                .Setup(x => x.ValidateCourse(It.Is<DataLockValidationModel>(model => model.Apprenticeship.Id == 1)))
                .Returns(() => new CourseValidationResult { MatchedPriceEpisode = new ApprenticeshipPriceEpisodeModel { Id = 90 } });
            mocker.Mock<ICourseValidationProcessor>()
                .Setup(x => x.ValidateCourse(It.Is<DataLockValidationModel>(model => model.Apprenticeship.Id == 2)))
                .Returns(() => new CourseValidationResult { MatchedPriceEpisode = new ApprenticeshipPriceEpisodeModel { Id = 96 } });

            var periods = mocker.Create<EarningPeriodsValidationProcessor>().ValidatePeriods(
                ukprn,
                1,
                priceEpisodes,
                earning.Periods.ToList(),
                (TransactionType)earning.Type,
                apprenticeships,
                aim,
                AcademicYear);
            periods.ValidPeriods.Count.Should().Be(2);
            periods.ValidPeriods.Any(p => p.ApprenticeshipPriceEpisodeId == 90 && p.AccountId == 21).Should().Be(true);
            periods.ValidPeriods.Any(p => p.ApprenticeshipPriceEpisodeId == 96 && p.AccountId == 22).Should().Be(true);
        }

        [Test]
        public void DataLocksAreReturnedInInvalidPeriods()
        {
            var earning = new OnProgrammeEarning
            {
                Periods = new List<EarningPeriod> { new EarningPeriod { Amount = 1, PriceEpisodeIdentifier = "pe-1" } }.AsReadOnly()
            };
            mocker.Mock<ICourseValidationProcessor>()
                .Setup(x => x.ValidateCourse(It.Is<DataLockValidationModel>(model => model.Apprenticeship.Id == 1)))
                .Returns(() => new CourseValidationResult { MatchedPriceEpisode = new ApprenticeshipPriceEpisodeModel { Id = 90 } });
            mocker.Mock<ICourseValidationProcessor>()
                .Setup(x => x.ValidateCourse(It.Is<DataLockValidationModel>(model => model.Apprenticeship.Id == 2)))
                .Returns(() => new CourseValidationResult { DataLockFailures = dataLockFailures });

            var periods = mocker.Create<EarningPeriodsValidationProcessor>().ValidatePeriods(
                ukprn,
                1,
                priceEpisodes,
                earning.Periods.ToList(),
                (TransactionType)earning.Type,
                apprenticeships,
                aim,
                AcademicYear);

            periods.InValidPeriods.Count.Should().Be(1);
            periods.InValidPeriods.All(p => p.DataLockFailures[0].DataLockError == DataLockErrorCode.DLOCK_03).Should().BeTrue();
        }

        [Test]
        public void ZeroAmountPeriodsAreAddedToValidPeriod()
        {
            var earning = new OnProgrammeEarning
            {
                Periods = new List<EarningPeriod> { new EarningPeriod { Amount = 0, PriceEpisodeIdentifier = "pe-1" } }.AsReadOnly()
            };
            mocker.Mock<ICourseValidationProcessor>()
                .Setup(x => x.ValidateCourse(It.Is<DataLockValidationModel>(model => model.Apprenticeship.Id == 1)))
                .Returns(() => new CourseValidationResult { MatchedPriceEpisode = new ApprenticeshipPriceEpisodeModel { Id = 90 } });
            mocker.Mock<ICourseValidationProcessor>()
                .Setup(x => x.ValidateCourse(It.Is<DataLockValidationModel>(model => model.Apprenticeship.Id == 2)))
                .Returns(() => new CourseValidationResult { DataLockFailures = dataLockFailures });

            var periods = mocker.Create<EarningPeriodsValidationProcessor>().ValidatePeriods(
                ukprn,
                1,
                priceEpisodes,
                earning.Periods.ToList(),
                (TransactionType)earning.Type,
                apprenticeships,
                aim,
                AcademicYear);

            periods.ValidPeriods.Count.Should().Be(1);
        }

        [Test]
        public void PopulatesTransferSenderAccountIdForMatchedApprenticeship()
        {
            var earning = new OnProgrammeEarning
            {
                Periods = new List<EarningPeriod> { new EarningPeriod
                {
                    Amount = 1,
                    PriceEpisodeIdentifier = "pe-1",
                } }.AsReadOnly()
            };
            mocker.Mock<ICourseValidationProcessor>()
                .Setup(x => x.ValidateCourse(It.Is<DataLockValidationModel>(model => model.Apprenticeship.Id == 1)))
                .Returns(() => new CourseValidationResult { MatchedPriceEpisode = new ApprenticeshipPriceEpisodeModel { Id = 90 } });
            mocker.Mock<ICourseValidationProcessor>()
                .Setup(x => x.ValidateCourse(It.Is<DataLockValidationModel>(model => model.Apprenticeship.Id == 2)))
                .Returns(() => new CourseValidationResult { MatchedPriceEpisode = new ApprenticeshipPriceEpisodeModel { Id = 96 } });

            apprenticeships.FirstOrDefault().TransferSendingEmployerAccountId = 999;
            var periods = mocker.Create<EarningPeriodsValidationProcessor>().ValidatePeriods(
                ukprn,
                1, priceEpisodes,
                earning.Periods.ToList(),
                (TransactionType)earning.Type,
                apprenticeships,
                aim,
                AcademicYear);

            periods.ValidPeriods.Count.Should().Be(2);
            periods.ValidPeriods.Any(p => p.ApprenticeshipId == 1 && p.TransferSenderAccountId == 999).Should().Be(true);
        }

        [Test]
        public void OnlyValidateApprenticeshipsWithinADeliveryPeriods()
        {
            apprenticeships = new List<ApprenticeshipModel>
            {
                new ApprenticeshipModel
                {
                    Id = 1,
                    AccountId = 21,
                    Ukprn = ukprn,
                    ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                    {
                        new ApprenticeshipPriceEpisodeModel{Id = 90},
                        new ApprenticeshipPriceEpisodeModel{Id = 91},
                        new ApprenticeshipPriceEpisodeModel{Id = 92},
                        new ApprenticeshipPriceEpisodeModel{Id = 93},
                    },
                    EstimatedStartDate =new DateTime(2018, 9,15)
                }
            };

            var earning = new OnProgrammeEarning
            {
                Periods = new List<EarningPeriod>
                {
                    new EarningPeriod
                    {
                        Amount = 1,
                        PriceEpisodeIdentifier = "pe-1",
                        Period = 1
                    }
                }.AsReadOnly()
            };
            
            mocker.Mock<ICalculatePeriodStartAndEndDate>()
                .Setup(x => x.GetPeriodDate(1, AcademicYear))
                .Returns(() => (new DateTime(2018, 8, 1), new DateTime(2018, 8, 31)));

            mocker.Mock<ICourseValidationProcessor>()
                .Setup(x => x.ValidateCourse(It.IsAny<DataLockValidationModel>()))
                .Returns(() => new CourseValidationResult());

            var periods = mocker.Create<EarningPeriodsValidationProcessor>()
                .ValidatePeriods(ukprn, 1, priceEpisodes, earning.Periods.ToList(), (TransactionType)earning.Type, apprenticeships, aim, AcademicYear);

            mocker.Mock<ICourseValidationProcessor>()
                .Verify(x => x.ValidateCourse(It.IsAny<DataLockValidationModel>()), Times.Never);

            periods.ValidPeriods.Count.Should().Be(0);
            periods.InValidPeriods.Count.Should().Be(1);
            periods.InValidPeriods.Any(p => p.DataLockFailures.Count() == 1 && p.DataLockFailures.All(d => d.DataLockError == DataLockErrorCode.DLOCK_02))
                .Should().Be(true);
        }

    }
}