using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.EmploymentVerificationService.Messages.Learner.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Payments.DataLocks.Tests.Specs.Handlers
{
    public class VerifyNotEmployedCompletedHandler : IHandleMessages<VerifyNotEmployedCompleted>
    {
        public Task Handle(VerifyNotEmployedCompleted message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }
}
