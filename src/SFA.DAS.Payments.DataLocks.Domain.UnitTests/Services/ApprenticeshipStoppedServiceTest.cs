﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Moq;
using NUnit.Framework;
using SFA.DAS.Payments.DataLocks.Domain.Models;
using SFA.DAS.Payments.DataLocks.Domain.Services.Apprenticeships;
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.DataLocks.Domain.UnitTests.Services
{
    public class ApprenticeshipStoppedServiceTest
    {

        private AutoMock mocker;

        [SetUp]
        public void SetUp()
        {
            mocker = AutoMock.GetLoose();
        }
        
        [Test]
        public async Task Updates_Stopped_Apprenticeship()
        {
            var updatedApprenticeship = new UpdatedApprenticeshipStoppedModel
            {
                ApprenticeshipId = 629959,
                StopDate = DateTime.Today
            };

            var apprenticeshipModel = new ApprenticeshipModel
            {
                Id = updatedApprenticeship.ApprenticeshipId,
                Uln = 1,
                AgreedOnDate = DateTime.Today.AddDays(-2),
                EstimatedStartDate = DateTime.Today.AddDays(-1),
                EstimatedEndDate = DateTime.Today.AddYears(2),
                ProgrammeType = 25,
                StandardCode = 17,

                ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                {
                    new ApprenticeshipPriceEpisodeModel
                    {
                        Id = 1,
                        ApprenticeshipId = updatedApprenticeship.ApprenticeshipId,
                        StartDate = new DateTime(2017,08,06),
                        Cost = 15000.00m
                    },
                    new ApprenticeshipPriceEpisodeModel
                    {
                        Id =161,
                        ApprenticeshipId = updatedApprenticeship.ApprenticeshipId,
                        StartDate = new DateTime(2017,08,10),
                        Cost = 1000.00m
                    }
                }
            };

            mocker.Mock<IApprenticeshipRepository>()
                .Setup(x => x.Get(It.Is<long>(id => id == apprenticeshipModel.Id)))
                .ReturnsAsync(apprenticeshipModel);

            var service = mocker.Create<ApprenticeshipStoppedService>();
            await service.UpdateApprenticeship(updatedApprenticeship);

            mocker.Mock<IApprenticeshipRepository>()
                .Verify(x => x.UpdateApprenticeship(It.Is<ApprenticeshipModel>(model =>
                    model.Id == updatedApprenticeship.ApprenticeshipId
                    && model.StopDate == updatedApprenticeship.StopDate
                    && model.Status == ApprenticeshipStatus.Stopped
                    && model.ApprenticeshipPriceEpisodes.Count == 2
                    && model.ApprenticeshipPriceEpisodes[0].Cost == apprenticeshipModel.ApprenticeshipPriceEpisodes[0].Cost
                    && model.ApprenticeshipPriceEpisodes[0].StartDate == apprenticeshipModel.ApprenticeshipPriceEpisodes[0].StartDate
                    && model.ApprenticeshipPriceEpisodes[1].StartDate == apprenticeshipModel.ApprenticeshipPriceEpisodes[1].StartDate
                    && model.ApprenticeshipPriceEpisodes[1].Cost == apprenticeshipModel.ApprenticeshipPriceEpisodes[1].Cost)
                ), Times.Once);

            mocker.Mock<IApprenticeshipRepository>()
                .Verify(x => x.Get(It.Is<long>(id => id == apprenticeshipModel.Id)), Times.Exactly(2));

        }

    }
}
