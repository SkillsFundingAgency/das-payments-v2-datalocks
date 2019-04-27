﻿using System.Collections.Generic;
using System.Linq;
using SFA.DAS.Payments.DataLocks.Domain.Models;
using SFA.DAS.Payments.Model.Core.Entities;

namespace SFA.DAS.Payments.DataLocks.Domain.Services.CourseValidation
{
    public class ApprenticeshipPauseValidator : BaseCourseDateValidator, ICourseValidator
    {
        protected override void SetDataLockErrorCode(ValidationResult result, ApprenticeshipModel apprenticeship)
        {
           result.DataLockErrorCode = DataLockErrorCode.DLOCK_12;
        }

        protected override List<ApprenticeshipPriceEpisodeModel> GetValidApprenticeshipPriceEpisodes(DataLockValidationModel dataLockValidationModel)
        {
           
            return dataLockValidationModel.Apprenticeship.ApprenticeshipPriceEpisodes.Where(o => !o.Removed)?.ToList();
        }

        protected override bool FailedValidation(ApprenticeshipStatus apprenticeshipStatus, List<ApprenticeshipPriceEpisodeModel> apprenticeshipPriceEpisodes)
        {
            return apprenticeshipStatus == ApprenticeshipStatus.Paused;
        }

    }
}