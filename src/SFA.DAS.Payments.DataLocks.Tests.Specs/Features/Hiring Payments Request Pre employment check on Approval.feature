Feature: PV2-4343 Hiring Payments: Request Pre-employment check on Approval

As the payments service
We need to request an employment check when the apprenticeship is approved
So that we can ensure that the apprentice is eligible for the employer hiring payments

Scenario Outline: Apprentieship is approved - general case
Given a new apprenticeship has been approved
And the employer is non levy
And the learning start date is on or after 2nd Oct 2026
And the learner is aged under 25 at the learning start date
When the payments service receives the ApprenticeshipCreated message
Then the payments service creates and publishes a VerifyNotEmployed request with the following details mapped from the incoming message
| Outgoing VerifyNotEmployed command | Maps from incoming ApprenticeshipCreatedEvent |
| ULN                                | Uln                                           |
| ApprenticeshipId                   | ApprenticeshipId                              |
| EmployerAccountIdentifier          | AccountId                                     |
| StartDate                          | Learning Start date - 150 days                |
| DurationInDays                     | 60                                            |
And  the payments service stores the request details with a status of "Pending"


Scenario Outline: Apprenticeship is approved, learning start date is on 1st Oct 2026, special case - employment can start from 1st July
Given a new apprenticeship has been approved
And the employer is non levy
And the learning start date is on 1st Oct 2026
And the learner is aged under 25 at the learning start date
When the payments service receives the ApprenticeshipCreated message
Then the payments service creates and publishes a VerifyNotEmployed request with the following details mapped from the incoming message
| Outgoing VerifyNotEmployed command | Maps from incoming ApprenticeshipCreatedEvent |
| ULN                                | Uln                                           |
| ApprenticeshipId                   | ApprenticeshipId                              |
| EmployerAccountIdentifier          | AccountId                                     |
| StartDate                          | 01/07/2026						             |
| DurationInDays                     | 60                                            |
And  the payments service stores the request details with a status of "Pending"


Scenario Outline: Check completed - learner not employed (successful result)
Given a VerifyNotEmployed request has been published
When the payments service receives a VerifyNotEmployedCompleted message	
And  result contains NotEmployed=true
And the response is matched to the stored request
Then the stored request is updated with Pre-Employment Status set to "NotEmployed"


Scenario Outline: Check completed - learner employed (unsuccessful result)
Given a VerifyNotEmployed request has been published
When the payments service receives a VerifyNotEmployedCompleted message	
And  result contains NotEmployed=false
And the response is matched to the stored request
Then the stored request is updated with Pre-Employment Status set to "Employed"