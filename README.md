# Payments V2 Data Locks

<img src="https://avatars.githubusercontent.com/u/9841374?s=200&v=4" align="right" alt="UK Government logo">

[![Build Status](https://dev.azure.com/sfa-gov-uk/DCT/_apis/build/status/GitHub/Service%20Fabric/SkillsFundingAgency.das-payments-v2-datalocks?branchName=main)](https://dev.azure.com/sfa-gov-uk/DCT/_apis/build/status/GitHub/Service%20Fabric/SkillsFundingAgency.das-payments-v2-datalocks?branchName=main)
[![Jira Project](https://img.shields.io/badge/Jira-Project-blue)](https://skillsfundingagency.atlassian.net/secure/RapidBoard.jspa?rapidView=782&projectKey=PV2)
[![Confluence Project](https://img.shields.io/badge/Confluence-Project-blue)](https://skillsfundingagency.atlassian.net/wiki/spaces/NDL/pages/3700621400/Provider+and+Employer+Payments+Payments+BAU)
[![License](https://img.shields.io/badge/license-MIT-lightgrey.svg?longCache=true&style=flat-square)](https://en.wikipedia.org/wiki/MIT_License)


The Payments V2 Data Locks ServiceFabric application provides functionality for validating the learner data held in the Digital Apprenticeship Service (DAS) service against the data submitted in the Individual Learner Record (ILR) in the Submit Learner Data service (SLD)

More information here: https://skillsfundingagency.atlassian.net/wiki/spaces/NDL/pages/533856257/b.+Data+Locks+Application

## How It Works

This repository contains stateful and stateless ServiceFabric services for handling data lock events (mismatches between the data held in DAS and SLD)

For example:

* When an apprenticeship is started, stopped, paused, or updated in the DAS Approvals service
* When apprenticeship details or training provider earnings are updated in the ILR submissions

Where there are differences between the submissions in the DAS systems and ILR data, data lock events will be generated and any earnings that were due to be paid will be held back.

## 🚀 Installation

### Pre-Requisites

Setup instructions can be found at the following link, which will help you set up your environment and access the correct repositories: https://skillsfundingagency.atlassian.net/wiki/spaces/NDL/pages/950927878/Development+Environment+-+Payments+V2+DAS+Space

### Config


As detailed in: https://skillsfundingagency.atlassian.net/wiki/spaces/NDL/pages/644972941/Developer+Configuration+Settings

Select the configuration for the DataLocks application

## 🔗 External Dependencies

N/A

## Technologies

* .NetCore 2.1/3.1/6
* Azure SQL Server
* Azure Functions
* Azure Service Bus
* ServiceFabric

## 🐛 Known Issues

N/A
