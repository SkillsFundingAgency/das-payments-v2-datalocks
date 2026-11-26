using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using NUnit.Framework;
using SFA.DAS.Payments.DataLocks.Tests.Specs.Data;
using SFA.DAS.Payments.Model.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Payments.DataLocks.Tests.Specs.StepDefinitions
{
    public class TestSession
    {
        private readonly Random random = new(Guid.NewGuid().GetHashCode());
        public TestSessionDataContext DataContext { get; }
        public TimeSpan TimeToWait => TimeSpan.FromSeconds(30);
        public TimeSpan TimeToPause => TimeSpan.FromSeconds(2);
        public long JobId { get; set; }
        public MessagingContext DASMessageContext { get; }

        public TestSession()
        {
            var cnn = TestRunBindings.Config["ConnectionStrings:PaymentsConnectionString"];
            DataContext = new TestSessionDataContext(cnn);
            JobId = GenerateId();
            DASMessageContext = new MessagingContext();
        }

        public long GenerateId(int maxValue = 1000000)
        {
            var id = random.Next(maxValue);
            //TODO: make sure that the id isn't already in use.
            return id;
        }

        public async Task WaitForIt(Func<Task<bool>> lookForIt, string failText)
        {
            var endTime = DateTime.Now.Add(TimeToWait);
            var lastRun = false;

            while (DateTime.Now < endTime || lastRun)
            {
                if (await lookForIt())
                {
                    if (lastRun) return;
                    lastRun = true;
                }
                else
                {
                    if (lastRun) break;
                }

                await Task.Delay(TimeToPause);
            }

            Assert.Fail($"{failText}  Time: {DateTime.Now:G}. Job Id: {JobId}");
        }

        public async Task WaitForIt(Func<bool> lookForIt, string failText)
        {
            var endTime = DateTime.Now.Add(TimeToWait);
            var lastRun = false;

            while (DateTime.Now < endTime || lastRun)
            {
                if (lookForIt())
                {
                    if (lastRun) return;
                    lastRun = true;
                }
                else
                {
                    if (lastRun) break;
                }

                await Task.Delay(TimeToPause);
            }

            Assert.Fail($"{failText}  Time: {DateTime.Now:G}. Job Id: {JobId}");
        }
    }
}