using System;
using System.Threading.Tasks;
using NServiceBus;
using SFA.DAS.Payments.Core.Configuration;

namespace SFA.DAS.Payments.DataLocks.Application.Infrastructure.Messaging
{
    public interface IEmploymentVerificationEndpoint
    {
        Task<IEndpointInstance> GetEndpoint();
    }

    public class EmploymentVerificationEndpoint : IEmploymentVerificationEndpoint
    {
        private readonly Lazy<Task<IEndpointInstance>> endpoint;

        public EmploymentVerificationEndpoint(
            IEmploymentVerificationEndpointConfigurationFactory factory)
        {
            endpoint = new Lazy<Task<IEndpointInstance>>(() =>
            {
                var endpointConfiguration = factory.Create();
                return Endpoint.Start(endpointConfiguration);
            });
        }

        public Task<IEndpointInstance> GetEndpoint()
        {
            return endpoint.Value;
        }
    }
}