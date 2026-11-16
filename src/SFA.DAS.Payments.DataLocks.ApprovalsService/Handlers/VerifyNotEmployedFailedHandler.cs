using System.Threading.Tasks;
using Autofac;
using NServiceBus;
using SFA.DAS.EmploymentVerificationService.Messages.Learner.Events;
using SFA.DAS.Payments.Application.Infrastructure.Ioc;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.Application.Infrastructure.Telemetry;
using SFA.DAS.Payments.DataLocks.Application.Services;

namespace SFA.DAS.Payments.DataLocks.ApprovalsService.Handlers;

public class VerifyNotEmployedFailedHandler : BaseApprovalsMessageHandler<VerifyNotEmployedFailed>
{
    public VerifyNotEmployedFailedHandler(ITelemetry telemetry, IPaymentLogger logger, IContainerScopeFactory factory) : base(telemetry, logger, factory)
    {
    }

    protected override async Task HandleMessage(VerifyNotEmployedFailed message, IMessageHandlerContext context, ILifetimeScope scope)
    {
        Logger.LogDebug($"Handling VerifyNotEmployedFailed for Apprenticeship Id: {message.ApprenticeshipId}, CorrelationId: {message.CorrelationId}.");

        var processor = scope.Resolve<IApprenticeshipHiringPaymentProcessor>();
        await processor.ProcessFailed(message);

        Logger.LogInfo($"Finished handling VerifyNotEmployedFailed for Apprenticeship Id: {message.ApprenticeshipId}, CorrelationId: {message.CorrelationId}.");
    }
}