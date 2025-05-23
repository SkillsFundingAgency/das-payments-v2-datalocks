﻿using Autofac.Extras.Moq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.Payments.DataLocks.Domain.Models;
using SFA.DAS.Payments.DataLocks.Domain.Services.CourseValidation;
using SFA.DAS.Payments.Model.Core;
using SFA.DAS.Payments.Model.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;

namespace SFA.DAS.Payments.DataLocks.Domain.UnitTests.Services.CourseValidation
{
    [TestFixture]
    public class CourseValidatorsProcessorTest
    {
        private AutoMock mocker;
        private List<ICourseValidator> courseValidators;
        private DataLockValidationModel dataLockValidationModel;
        private Mock<ICourseValidator> negotiatedPriceValidator;
        private Mock<ICourseValidator> apprenticeshipPauseValidator;
        
        [SetUp]
        public void Prepare()
        {
            var earningPeriod = new EarningPeriod
            {
                Period = 1
            };

            dataLockValidationModel = new DataLockValidationModel
            {
                EarningPeriod = earningPeriod,
                PriceEpisode = new PriceEpisode(),
                Apprenticeship = new ApprenticeshipModel
                {
                    Id = 1,
                    Uln = 100,
                    ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                    {
                        new ApprenticeshipPriceEpisodeModel
                            {Id = 99, ApprenticeshipId = 1, Cost = 100, StartDate = DateTime.Today}
                    }
                }
            };
            
            negotiatedPriceValidator = new Mock<ICourseValidator>();
            apprenticeshipPauseValidator = new Mock<ICourseValidator>();

            courseValidators = new List<ICourseValidator>
            {
                negotiatedPriceValidator.Object,
                apprenticeshipPauseValidator.Object,
            };
            mocker = AutoMock.GetStrict(cfg => cfg.RegisterInstance(courseValidators).As<List<ICourseValidator>>());
//            mocker.Provide<List<ICourseValidator>>(courseValidators);
        }

        [Test]
        public void UsesAllCourseValidators()
        {
            negotiatedPriceValidator
                .Setup(validator => validator.Validate(It.IsAny<DataLockValidationModel>()))
                .Returns(() => new ValidationResult())
                .Verifiable();
            apprenticeshipPauseValidator
                .Setup(validator => validator.Validate(It.IsAny<DataLockValidationModel>()))
                .Returns(() => new ValidationResult())
                .Verifiable();

            var courseValidator = mocker.Create<CourseValidationProcessor>();
            courseValidator.ValidateCourse(dataLockValidationModel);
            
            negotiatedPriceValidator
                .Verify(x => x.Validate(It.IsAny<DataLockValidationModel>()), Times.Once);
            apprenticeshipPauseValidator
                .Verify(x => x.Validate(It.IsAny<DataLockValidationModel>()), Times.Once);
        }

        [Test]
        public void PassesAllApprenticeshipPriceEpisodesToEachValidator()
        {
            dataLockValidationModel.Apprenticeship = new ApprenticeshipModel
            {
                ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                {
                    new ApprenticeshipPriceEpisodeModel {Id = 50},
                    new ApprenticeshipPriceEpisodeModel {Id = 51},
                    new ApprenticeshipPriceEpisodeModel {Id = 52}
                }
            };
            
            negotiatedPriceValidator
                .Setup(validator => validator.Validate(It.IsAny<DataLockValidationModel>()))
                .Returns(() => new ValidationResult
                {
                    ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                    {
                        new ApprenticeshipPriceEpisodeModel {Id = 52}
                    }
                });
            apprenticeshipPauseValidator
                .Setup(validator => validator.Validate(It.IsAny<DataLockValidationModel>()))
                .Returns(() => new ValidationResult
                {
                    ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                    {
                        new ApprenticeshipPriceEpisodeModel {Id = 52}
                    }
                });


            var courseValidator = mocker.Create<CourseValidationProcessor>();
            courseValidator.ValidateCourse(dataLockValidationModel);
            
            negotiatedPriceValidator
                .Verify(validator => validator.Validate(It.Is<DataLockValidationModel>(model =>
                    model.Apprenticeship.ApprenticeshipPriceEpisodes.All(ape =>
                        ape.Id == 50 || ape.Id == 51 || ape.Id == 52) &&
                    model.Apprenticeship.ApprenticeshipPriceEpisodes.Count == 3)), Times.Once);

            apprenticeshipPauseValidator
                .Verify(validator => validator.Validate(It.Is<DataLockValidationModel>(model =>
                    model.Apprenticeship.ApprenticeshipPriceEpisodes.All(ape =>
                        ape.Id == 50 || ape.Id == 51 || ape.Id == 52) &&
                    model.Apprenticeship.ApprenticeshipPriceEpisodes.Count == 3)), Times.Once);

        }

        [Test]
        public void ReturnsMatchedApprenticeshipPriceEpisode()
        {
            dataLockValidationModel.Apprenticeship = new ApprenticeshipModel
            {
                ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                {
                    new ApprenticeshipPriceEpisodeModel {Id = 50, Removed = true},
                    new ApprenticeshipPriceEpisodeModel {Id = 51},
                    new ApprenticeshipPriceEpisodeModel {Id = 52},
                    new ApprenticeshipPriceEpisodeModel {Id = 53},
                    new ApprenticeshipPriceEpisodeModel {Id = 54},
                }
            };
           
            negotiatedPriceValidator
                .Setup(validator => validator.Validate(It.IsAny<DataLockValidationModel>()))
                .Returns(() => new ValidationResult
                {
                    ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                    {
                        new ApprenticeshipPriceEpisodeModel {Id = 52},
                        new ApprenticeshipPriceEpisodeModel {Id = 53},
                    }
                });
            apprenticeshipPauseValidator
                .Setup(validator => validator.Validate(It.IsAny<DataLockValidationModel>()))
                .Returns(() => new ValidationResult
                {
                    ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                    {
                        new ApprenticeshipPriceEpisodeModel {Id = 52},
                        new ApprenticeshipPriceEpisodeModel {Id = 54}
                    }
                });
            var courseValidator = mocker.Create<CourseValidationProcessor>();
            var result = courseValidator.ValidateCourse(dataLockValidationModel);
            
            negotiatedPriceValidator
                .Verify(validator => validator.Validate(It.IsAny<DataLockValidationModel>()), Times.Once);

            apprenticeshipPauseValidator
                .Verify(validator => validator.Validate(It.IsAny<DataLockValidationModel>()), Times.Once);

            result.DataLockFailures.Should().BeEmpty();
            result.MatchedPriceEpisode.Should().NotBeNull();
            result.MatchedPriceEpisode.Id.Should().Be(52);
        }

        [Test]
        public void ReturnsNoPriceEpisodeIfThereWereFailures()
        {
            dataLockValidationModel.Apprenticeship = new ApprenticeshipModel
            {
                ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                {
                    new ApprenticeshipPriceEpisodeModel {Id = 50},
                    new ApprenticeshipPriceEpisodeModel {Id = 51},
                    new ApprenticeshipPriceEpisodeModel {Id = 52},
                    new ApprenticeshipPriceEpisodeModel {Id = 53},
                    new ApprenticeshipPriceEpisodeModel {Id = 54},
                }
            };

            negotiatedPriceValidator
                .Setup(validator => validator.Validate(It.IsAny<DataLockValidationModel>()))
                .Returns(() => new ValidationResult
                {
                    DataLockErrorCode = DataLockErrorCode.DLOCK_07,
                    ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                    {
                        new ApprenticeshipPriceEpisodeModel {Id = 52},
                    }
                });
            apprenticeshipPauseValidator
                .Setup(validator => validator.Validate(It.IsAny<DataLockValidationModel>()))
                .Returns(() => new ValidationResult
                {
                    DataLockErrorCode = DataLockErrorCode.DLOCK_12,
                    ApprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
                    {
                        new ApprenticeshipPriceEpisodeModel {Id = 52},
                    }
                });
            var courseValidator = mocker.Create<CourseValidationProcessor>();
            var result = courseValidator.ValidateCourse(dataLockValidationModel);

            negotiatedPriceValidator
                .Verify(validator => validator.Validate(It.IsAny<DataLockValidationModel>()), Times.Once);

            apprenticeshipPauseValidator
                .Verify(validator => validator.Validate(It.IsAny<DataLockValidationModel>()), Times.Once);

            result.MatchedPriceEpisode.Should().BeNull();
        }

        [Test]
        public void ReturnsAllDataLockFailures()
        {
            var apprenticeshipPriceEpisodes = new List<ApprenticeshipPriceEpisodeModel>
            {
                new ApprenticeshipPriceEpisodeModel {Id = 50},
                new ApprenticeshipPriceEpisodeModel {Id = 51},
                new ApprenticeshipPriceEpisodeModel {Id = 52},
                new ApprenticeshipPriceEpisodeModel {Id = 53}
            };

            dataLockValidationModel.Apprenticeship = new ApprenticeshipModel
            {
                Id = 100,
                ApprenticeshipPriceEpisodes = apprenticeshipPriceEpisodes
            };

            negotiatedPriceValidator
                .Setup(validator => validator.Validate(It.IsAny<DataLockValidationModel>()))
                .Returns(() => new ValidationResult
                {
                    DataLockErrorCode = DataLockErrorCode.DLOCK_07
                });

            apprenticeshipPauseValidator
                .Setup(validator => validator.Validate(It.IsAny<DataLockValidationModel>()))
                .Returns(() => new ValidationResult
                {
                    DataLockErrorCode = DataLockErrorCode.DLOCK_12
                });

            var courseValidator = mocker.Create<CourseValidationProcessor>();
            var result = courseValidator.ValidateCourse(dataLockValidationModel);

            negotiatedPriceValidator
                .Verify(validator => validator.Validate(It.IsAny<DataLockValidationModel>()), Times.Once);

            apprenticeshipPauseValidator
                .Verify(validator => validator.Validate(It.IsAny<DataLockValidationModel>()), Times.Once);

            result.Should().NotBeNull();
            result.MatchedPriceEpisode.Should().BeNull();
            result.DataLockFailures.Should().HaveCount(2);
            result.DataLockFailures.All(x => x.ApprenticeshipId == 100).Should().BeTrue();
            result.DataLockFailures.Any(x =>
                    x.DataLockError == DataLockErrorCode.DLOCK_07 &&
                    x.ApprenticeshipPriceEpisodeIds.All(o => apprenticeshipPriceEpisodes.Select(a => a.Id).Contains(o)))
                .Should().BeTrue();
            result.DataLockFailures.Any(x =>
                    x.DataLockError == DataLockErrorCode.DLOCK_12 &&
                    x.ApprenticeshipPriceEpisodeIds.All(o => apprenticeshipPriceEpisodes.Select(a => a.Id).Contains(o)))
                .Should().BeTrue();
        }
        
    }
}