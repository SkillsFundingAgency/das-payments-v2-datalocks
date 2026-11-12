using System.Threading.Tasks;
using Autofac;
using NServiceBus;
using SFA.DAS.Payments.Application.Infrastructure.Ioc;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Application.Infrastructure.Telemetry;
using SFA.DAS.Payments.Core;
using SFA.DAS.Payments.DataLocks.Application.Services;
using SFA.DAS.Payments.DataLocks.Messages.Events;

namespace SFA.DAS.Payments.DataLocks.ApprovalsService.Handlers
{
    public class VerifyNotEmployedCompletedHandler : BaseApprovalsMessageHandler<VerifyNotEmployedCompleted>
    {
        public VerifyNotEmployedCompletedHandler(ITelemetry telemetry, IPaymentLogger logger, IContainerScopeFactory factory) : base(telemetry, logger, factory)
        {
        }

        protected override async Task HandleMessage(VerifyNotEmployedCompleted message, IMessageHandlerContext context, ILifetimeScope scope)
        {
            Logger.LogDebug($"Handling VerifyNotEmployedCompleted for Apprenticeship Id: {message.ApprenticeshipId}, CorrelationId: {message.CorrelationId}.");

            var processor = scope.Resolve<IApprenticeshipEmploymentVerificationProcessor>();
            await processor.Process(message);

            Logger.LogInfo($"Finished handling VerifyNotEmployedCompleted for Apprenticeship Id: {message.ApprenticeshipId}, CorrelationId: {message.CorrelationId}.");
        }
    }
}
