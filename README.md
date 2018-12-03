iTrainer365
=========================

- [Before installation](#before-installation)
- [Installation](#installation)
- [Configuration](#configuration)
- [Debugging](#debugging)
- [Introduction](#introduction)
- [Features](#features)
- [Quality](#quality)
- [Contribute](#contribute)
- [Authors](#authors)
- [License](#license)


## Before installation

Before installation you need to have:

* A workable and globally available [SharePoint Online site](https://support.office.com/en-us/article/create-a-site-in-sharepoint-online-4d1e11bf-8ddc-499d-b889-2b48d10b1ce8) containing a list of questions and answers. Scheme of the list will be determined later during the process of [configuration](#configuration)
* A workable and globally available instance of [QnA Maker service](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/how-to/set-up-qnamaker-service-azure)
* A workable and globally available instance of [SQL Azure](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-get-started-portal)


## Installation

* First you need to create a bot with [Azure Bot Service](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-quickstart?view=azure-bot-service-3.0)
* Use *Functions Bot* as a type of bot to deploy and *Consumption plan* as a Hosting plan
* During the deployment you need to chose the *Basic* template for your bot
* After the template is succefully deployed you need to set up [continuous deployment](https://docs.microsoft.com/en-us/azure/azure-functions/functions-continuous-deployment#set-up-continuous-deployment) using [the repository](https://github.com/Chernevsky/iTrainer365) as a sorce of code
* Use **seqnum.name.sql** scripts in [migrations](https://github.com/Chernevsky/iTrainer365/tree/master/migrations) folder to build database scheme. The order of scripts to run is represented by sequnce number in the beginning of filename
* To compile all the resources use **runResourcesCompilation.cmd** script in [PostDeployScripts](https://github.com/Chernevsky/iTrainer365/tree/master/PostDeployScripts) folder


## Configuration

Using the **Application settings** section in the [Application settings blade](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings#settings) you need to set following parameters:

| APP SETTING NAME | VALUE (Example) | Description |
| ----------| ----------- | ----------- |
| AzureWebJobsStorage | - | *Connection string to storage of the bot* |
| WEBSITE_TIME_ZONE | - | *Full name of time zone* |
| AdminChannelAccountName | Admin name | - |
| AdminChannelAccountId | admin@your-site-name.com | - |
| AdminChannelId | email | - |
| AdminServiceUrl | https://email.botframework.com/ | - |
| MessageFeedbackQueue | bot-queue-feedback-massages | Feedback Queue Name |
| MessageSubscriptionQueue | bot-queue-subscription-massages | Subscription Queue Name |
| SharePointSiteUrl | https://your-site-name.sharepoint.com/ | [SharePoint Url](#before-installation) |
| SharePointUserName | bot@your-site-name.com | [SharePoint Username](#before-installation) |
| SharePointUserPassword | BotPa$$W0rD | [SharePoint Password](#before-installation) |
| SharePointExerciseRegularFolder | Tasks | Folder Name |
| SharePointExerciseTestFolder | Tests | Folder Name |
| SharePointExerciseListTitle | Exercises | List Name |
| SharePointExerciseFieldID | ID | Field Name |
| SharePointExerciseFieldTitle | Title | Field Name |
| SharePointExerciseFieldSubtitle | Subtitle | Field Name |
| SharePointExerciseFieldLanguage | Language | Field Name |
| SharePointExerciseFieldTaskText | Text | Field Name |
| SharePointExerciseFieldButton1 | Button1 | Field Name |
| SharePointExerciseFieldButton2 | Button2 | Field Name |
| SharePointExerciseFieldButton3 | Button3 | Field Name |
| SharePointExerciseFieldButton1Answer | Answer1 | Field Name |
| SharePointExerciseFieldButton2Answer | Answer2 | Field Name |
| SharePointExerciseFieldButton3Answer | Answer3 | Field Name |
| SharePointExerciseFieldCorrectAnswer | Correct | Field Name |
| SharePointExerciseFieldDate | Date | Field Name |
| SharePointExerciseFieldActive | Active | Field Name |
| SharePointCompanyListTitle | Companies | List Name |
| SharePointCompanyFieldID | ID | Field Name |
| SharePointCompanyFieldTitle | Title | Field Name |
| SharePointCompanyFieldCode | Code | Field Name |
| SharePointCompanyFieldTenantId | TenantId | Field Name |
| SharePointCompanyFieldActive | Active | Field Name |
| Locale | en | Locale |
| DefaultLocale | en | Default Locale |
| DefaultTimezone | +3 | Default Timezone |
| BotAccountName_email | Bot | Bot Name for email channel |
| BotAccountId_email | bot@your-site-name.com | Bot Id for email channel |
| BotAccountName_skype | Bot | Bot Name for Skype channel |
| BotAccountId_skype | *Skype ID* | Bot Id for Skype channel |
| BotAccountName_msteams | Bot | Bot Name for MS Teams channel |
| BotAccountId_msteams | *MS Teams ID* | Bot Id for MS Teams channel |
| BotAccountName_telegram | yourbotname | Bot Name for Telegram channel |
| BotAccountId_telegram | yourbot | Bot Id for Telegram channel |
| BotAccountName_webchat | Bot | Bot Name for WebChat channel |
| BotAccountId_webchat | bot | Bot Id for WebChat channel |
| BotAccountName_emulator | Bot | Bot Name for Emulator channel |
| BotAccountId_emulator | bot | Bot Id for Emulator channel |
| AzureWebJobsBotFrameworkDirectLineEndpoint | https://directline.botframework.com/ | DirectLine Endpoint |
| AzureWebJobsBotFrameworkDirectLineSecret | - | *Direct Line Secret* |
| TimerTriggerPingUrl | https://bot-app-name.azurewebsites.net/ | - |
| QnAEndpointHostName | https://qna-service-name.azurewebsites.net/qnamaker | [QnA Endpoint](#before-installation) |
| QnAAuthKey | - | [*QnA AuthKey*](#before-installation) |
| QnASubscriptionKey | - | [*QnA SubscriptionKey*](#before-installation) |
| QnAKnowledgebaseId | - | [*QnA KnowledgebaseId*](#before-installation) |
| QnAScoreMinimum | 0,3 | QnA Minimum Score to show any answer |
| QnAMaximumOptions | 3 | QnA Maximum Options to show |
| QnAScoreMinToOneAnswer | 0,99 | QnA Minimum Score to show one answer |

Using the **Connection strings** section in the [Application settings blade](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings#settings) you need to set following parameters:

| CONNECTION STRING NAME | VALUE (Example) | Description |
| ----------| ----------- | ----------- |
| SQLConnectionString | - | [*Connection string to the SQLAzure database*](#before-installation) |


## Debugging a C# Azure Bot Service bot in Visual Studio 

To learn how to debug Azure Bot Service bots, please visit [https://aka.ms/bf-docs-azure-debug](https://aka.ms/bf-docs-azure-debug)


## Introduction 

**What is iTrainer365?**

It is an educational chat-bot for continuously improving your skills

    
## Features

iTrainer365 has following features:
1. **Task** - With this command, the chat-bot will send you today's task
1. **Progress** - With this command, the  chat-bot will provide you with statistics of your educational progress for the entire period of interaction with the chat-bot
1. **Test** - With this command, you can run an in-depth test
1. **Company** - this command allows you to specify your company
1. **Question** - This command is designed to communicate with an expert on Microsoft Office 365
1. **Settings** - This command allows you to subscribe to daily tasks, and set the time at which the chat-bot will send them


## Contribute

Contributions to the bot are always welcome!

* Report any bugs or issues you find on the [issue tracker](https://github.com/Chernevsky/iTrainer365/issues/new).
* You can grab the source code at the bot's [Git repository](https://github.com/Chernevsky/iTrainer365).


## Authors

* Publisher [Artyom Chernevsky](https://github.com/Chernevsky)
* Developer [Denis Frolov](https://github.com/denisyfrolov)


## License

The code base is licensed under the GNU General Public License v3.0.