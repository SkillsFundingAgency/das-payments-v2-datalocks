using Microsoft.Extensions.Configuration;
using Reqnroll;
using SFA.DAS.Payments.Messages.Common;

namespace SFA.DAS.Payments.DataLocks.Tests.Specs.StepDefinitions
{
    [Binding]
    public class TestRunBindings
    {
        public static IEndpointInstance DASEndpoint { get; private set; }
        public static IConfiguration Config { get; private set; }

        [BeforeTestRun]
        public static async Task SetUpMessaging()
        {
            Config = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appSettings.json"))
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appSettings.development.json"), true)
                .Build();

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            DASEndpoint = await CreateEndpoint("DASServiceBusConnectionString");
        }

        public static async Task<IEndpointInstance> CreateEndpoint(string connectionName, bool sendOnly = false)
        {
            var endpointName = Config["EvsQueueName"];
            if (string.IsNullOrWhiteSpace(endpointName))
            {
                throw new InvalidOperationException("Configuration value 'EvsQueueName' is missing.");
            }

            var connectionString = Config.GetConnectionString(connectionName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{connectionName}' is missing from configuration.");
            }

            var endpointConfig = new EndpointConfiguration(endpointName);
            endpointConfig.Conventions().DefiningMessagesAs(type => type.IsMessage());
            endpointConfig.UseSerialization<NewtonsoftJsonSerializer>();

            if (sendOnly)
            {
                endpointConfig.SendOnly();
            }

            Console.WriteLine($"Using connection string configuration: {connectionName}");

            var transport = new AzureServiceBusTransport(connectionString)
            {
                UseWebSockets = Config["UseWebSockets"]?.ToLower() == "true"
            };

            endpointConfig.UseTransport(transport);
            endpointConfig.EnableInstallers();

            var startable = await Endpoint.Create(endpointConfig);
            return await startable.Start();
        }
    }
}