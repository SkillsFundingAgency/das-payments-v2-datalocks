using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.CommitmentsV2.Types;
using SFA.DAS.EmploymentVerificationService.Messages.Learner.Commands;
using SFA.DAS.Payments.DataLocks.Tests.Specs.Handlers;
using SFA.DAS.Payments.Messages.Common.Events;
using SFA.DAS.Payments.Model.Core.Entities;
using SFA.DAS.Payments.Model.Core.OnProgramme;

namespace SFA.DAS.Payments.DataLocks.Tests.Specs.StepDefinitions
{
    [Binding]
    public sealed class StepDefinitions
    {
        private ScenarioContext scenarioContext;
        private MessagingContext messagingContext;
        private TestSession testSession;

        public StepDefinitions(ScenarioContext scenarioContext, MessagingContext messagingContext, TestSession testSession)
        {
            this.scenarioContext = scenarioContext;
            this.messagingContext = messagingContext;
            this.testSession = testSession;
        }

        [BeforeScenario]
        public void BeforeScenario()
        {
            VerifyNotEmployedHandler.ReceivedMessages.Clear();
        }

        [AfterScenario]
        public async Task CleanUpEmploymentVerification()
        {
            const string key = "ApprenticeshipCreatedEvent";

            if (scenarioContext.ContainsKey(key))
            {
                var apprenticeship = GetCreatedEvent();

                await testSession.DataContext
                    .ClearApprenticeshipEmploymentVerificationAsync(apprenticeship.ApprenticeshipId);
            }
        }

        [Given("a new apprenticeship has been approved")]
        public void GivenANewApprenticeshipHasBeenApproved()
        {
            var message = new ApprenticeshipCreatedEvent
            {
                ApprenticeshipId = 1830457,
                StandardUId = "ST0116_1.2",
                TrainingCourseVersion = "1.2",
                TrainingCourseOption = "",
                AgreedOn = new DateTime(2026, 10, 12, 9, 41, 27, DateTimeKind.Utc),
                CreatedOn = new DateTime(2026, 10, 12, 9, 41, 27, DateTimeKind.Utc),
                Uln = "1234567890",
                ProviderId = 10005077,
                AccountId = 30060,
                AccountLegalEntityId = 2818,
                AccountLegalEntityPublicHashedId = "XEGE5X",
                LegalEntityName = "Example Manufacturing Ltd",
                StartDate = new DateTime(2026, 11, 1),
                EndDate = new DateTime(2028, 5, 1),
                PriceEpisodes =
                [
                    new PriceEpisode
                    {
                        FromDate = new DateTime(2026, 11, 1),
                        ToDate = null,
                        Cost = 12000m,
                        TrainingPrice = 10500m,
                        EndPointAssessmentPrice = 1500m
                    }
                ],
                TrainingType = (ProgrammeType)0,
                DeliveryModel = DeliveryModel.Regular,
                TrainingCode = "91",
                TransferSenderId = null,
                ApprenticeshipEmployerTypeOnApproval =
                    SFA.DAS.CommitmentsV2.Types.ApprenticeshipEmployerType.NonLevy,
                ContinuationOfId = 1755210,
                DateOfBirth = new DateTime(2004, 3, 15),
                ActualStartDate = null,
                FirstName = "Alex",
                LastName = "Example",
                ApprenticeshipHashedId = "V7NM8R",
                LearnerDataId = 48211,
                LearningType = SFA.DAS.Common.Domain.Types.LearningType.Apprenticeship
            };

            scenarioContext.Set(message, "ApprenticeshipCreatedEvent");
        }

        [Given("the employer is non levy")]
        public void WhenTheEmployerIsNonLevy()
        {
            var message = GetCreatedEvent();
            message.ApprenticeshipEmployerTypeOnApproval = SFA.DAS.CommitmentsV2.Types.ApprenticeshipEmployerType.NonLevy;
            scenarioContext.Set(message, "ApprenticeshipCreatedEvent");
        }

        [Given("the learning start date is on or after {int}nd Oct {int}")]
        public void WhenTheLearningStartDateIsOnOrAfterNdOct(int p0, int p1)
        {
            var message = GetCreatedEvent();
            message.StartDate = new DateTime(2026, 10, 2);
            scenarioContext.Set(message, "ApprenticeshipCreatedEvent");
        }

        [Given("the learner is aged under {int} at the learning start date")]
        public void WhenTheLearnerIsAgedUnderAtTheLearningStartDate(int p0)
        {
            var message = GetCreatedEvent();
            message.DateOfBirth = new DateTime(2002, 10, 2);
            scenarioContext.Set(message, "ApprenticeshipCreatedEvent");
        }

        [When("the payments service receives the ApprenticeshipCreated message")]
        public async Task WhenThePaymentsServiceReceivesTheApprenticeshipCreatedMessage()
        {
            var message = GetCreatedEvent();
            await testSession.DASMessageContext.Send<ApprenticeshipCreatedEvent>(message);
        }

        [Then("the payments service creates and publishes a VerifyNotEmployed request with the following details mapped from the incoming message")]
        public async Task ThenThePaymentsServiceCreatesAndPublishesAVerifyNotEmployedRequestAsync()
        {
            var apprenticeship = GetCreatedEvent();

            await testSession.WaitForIt(
                () => VerifyNotEmployedHandler
                    .GetMessages(apprenticeship.ApprenticeshipId)
                    .Any(),
                "VerifyNotEmployed message was not received");

            var message = VerifyNotEmployedHandler.GetMessages(apprenticeship.ApprenticeshipId).Single();

            message.ApprenticeshipId
                .Should().Be(apprenticeship.ApprenticeshipId);

            message.ULN
                .Should().Be(long.Parse(apprenticeship.Uln));

            message.EmployerAccountIdentifier
                .Should().Be(apprenticeship.AccountId);

            message.StartDate
                .Should().Be(apprenticeship.StartDate.Date.AddDays(-150));

            message.DurationInDays
                .Should().Be(60);
        }

        [Then("the payments service stores the request details with a status of {string}")]
        public async Task ThenThePaymentsServiceStoresTheRequestDetailsWithAStatusOf(string status)
        {
            var apprenticeship = GetCreatedEvent();
            var message = VerifyNotEmployedHandler
                .GetMessages(apprenticeship.ApprenticeshipId)
                .Single();
            var expectedStatus = Enum.Parse<EmploymentValidationStatus>(status, ignoreCase: true);

            await testSession.WaitForIt(
                async () =>
                {
                    var verification = await testSession.DataContext
                        .ApprenticeshipEmploymentVerification
                        .AsNoTracking()
                        .SingleOrDefaultAsync(x =>
                            x.ApprenticeshipId == apprenticeship.ApprenticeshipId &&
                            x.PreEmploymentCorrelationId == message.CorrelationId);

                    return verification?.PreEmploymentStatus == (byte)expectedStatus;
                },
                $"Apprenticeship employment verification was not stored with {status} status for apprenticeship {apprenticeship.ApprenticeshipId} and correlation ID {message.CorrelationId}");

            var storedVerification = await testSession.DataContext
                .ApprenticeshipEmploymentVerification
                .AsNoTracking()
                .SingleAsync(x =>
                    x.ApprenticeshipId == apprenticeship.ApprenticeshipId &&
                    x.PreEmploymentCorrelationId == message.CorrelationId);

            storedVerification.PreEmploymentStatus.Should().Be((byte)expectedStatus);
        }

        private ApprenticeshipCreatedEvent GetCreatedEvent()
        {
            return scenarioContext.Get<ApprenticeshipCreatedEvent>("ApprenticeshipCreatedEvent");
        }
    }
}