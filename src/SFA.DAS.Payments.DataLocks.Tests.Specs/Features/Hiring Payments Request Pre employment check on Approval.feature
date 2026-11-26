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
And  the payments service stores the request details with a status of "Pending"