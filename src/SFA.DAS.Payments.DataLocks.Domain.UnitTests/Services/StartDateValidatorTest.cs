﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.Payments.DataLocks.Domain.Models;
using SFA.DAS.Payments.DataLocks.Domain.Services;
using SFA.DAS.Payments.Model.Core;
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.DataLocks.Domain.UnitTests.Services
{
    [TestFixture]
    public class StartDateValidatorTest
    {
        private DateTime startDate = DateTime.UtcNow;
        private string priceEpisodeIdentifier = "pe-1";
        private EarningPeriod period;

        [OneTimeSetUp]
        public void Setup()
        {
            period = new EarningPeriod
            {
                Period = 1
            };
        }

        [Test]
        public void ReturnsNoDataLockErrors()
        {
            var apprenticeship = new ApprenticeshipModel
            {
                ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                {
                    new ApprenticeshipPriceEpisodeModel
                    {
                        StartDate = startDate.AddDays(-1)
                    }
                }
            };
            
            var validation = new DataLockValidationModel
            {
                PriceEpisode = new PriceEpisode {StartDate = startDate, Identifier = priceEpisodeIdentifier},
                EarningPeriod = period,
                Apprenticeship = apprenticeship
            };

            var validator = new StartDateValidator();

            var result = validator.Validate(validation);

            result.Any().Should().BeFalse();
        }

        [Test]
        public void ReturnsDataLockErrorWhenNotBegun()
        {
            var apprenticeship = new ApprenticeshipModel
            {
                ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                {
                    new ApprenticeshipPriceEpisodeModel
                    {
                        StartDate = startDate.AddDays(1)
                    }
                }
            };

            var validation = new DataLockValidationModel
            {
                PriceEpisode = new PriceEpisode { StartDate = startDate, Identifier = priceEpisodeIdentifier },
                EarningPeriod = period,
                Apprenticeship = apprenticeship
            };

            var validator = new StartDateValidator();

            var result = validator.Validate(validation);

            result.Should().HaveCount(1);
            result.First().DataLockErrorCode.Should().Be(DataLockErrorCode.DLOCK_09);
        }

        [Test]
        public void ReturnsDataLockErrorWhenGapBetweenApprenticeshipsButStartDateMatchesOne()
        {         
            var priceEpisodeId = 3L;
            startDate = DateTime.UtcNow.AddDays(-5);

            var apprenticeship = new ApprenticeshipModel
            {
                ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                {
                    new ApprenticeshipPriceEpisodeModel
                    {
                        StartDate = startDate.AddDays(-10)
                    },
                    new ApprenticeshipPriceEpisodeModel
                    {
                        Id = priceEpisodeId,
                        StartDate = startDate.AddDays(2)
                    }
                }
            };
            
            var validation = new DataLockValidationModel
            {
                PriceEpisode = new PriceEpisode { StartDate = startDate, Identifier = priceEpisodeIdentifier },
                EarningPeriod = period,
                Apprenticeship = apprenticeship
            };

            var validator = new StartDateValidator();

            var result = validator.Validate(validation);

            result.Should().HaveCount(1);

            var firstError = result.First();

            firstError.DataLockErrorCode.Should().Be(DataLockErrorCode.DLOCK_09);
            firstError.ApprenticeshipPriceEpisodeIdentifier.Should().Be(priceEpisodeId);
        }
    }
}