using System;
using NServiceBus;
using SFA.DAS.Payments.Core.Configuration;

namespace SFA.DAS.Payments.DataLocks.Application.Infrastructure.Messaging
{
    public interface IEmploymentVerificationEndpointConfigurationFactory
    {
        EndpointConfiguration Create();
    }

    public class EmploymentVerificationEndpointConfigurationFactory: IEmploymentVerificationEndpointConfigurationFactory
    {
        private readonly IConfigurationHelper configHelper;
        private readonly IApplicationConfiguration applicationConfiguration;

        public EmploymentVerificationEndpointConfigurationFactory(
            IConfigurationHelper configHelper,
            IApplicationConfiguration config)
        {
            this.configHelper = configHelper ?? throw new ArgumentNullException(nameof(configHelper));
            this.applicationConfiguration = config ?? throw new ArgumentNullException(nameof(config));
        }

        public EndpointConfiguration Create()
        {
            var endpointConfiguration = new EndpointConfiguration(applicationConfiguration.EndpointName);

            var conventions = endpointConfiguration.Conventions();

            conventions.DefiningMessagesAs(type =>
                (type.Namespace?.StartsWith("SFA.DAS.EmploymentVerificationService") ?? false)
                && (type.Namespace?.Contains(".Messages") ?? false));

            conventions.DefiningCommandsAs(type =>
                (type.Namespace?.StartsWith("SFA.DAS.EmploymentVerificationService") ?? false)
                && (type.Namespace?.Contains(".Commands") ?? false));

            conventions.DefiningEventsAs(type =>
                (type.Namespace?.StartsWith("SFA.DAS.EmploymentVerificationService") ?? false)
                && (
                    (type.Namespace?.Contains(".Events") ?? false) ||
                    (type.Namespace?.Contains(".Core") ?? false)
                ));

            endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();

            var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();

            transport.ConnectionString(
                configHelper.GetConnectionString("DASServiceBusConnectionString"));

            endpointConfiguration.SendOnly();

            return endpointConfiguration;
        }
    }
}