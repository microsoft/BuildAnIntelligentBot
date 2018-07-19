# Talking to my Bot

This lab will show you how to build a bot using the new Microsoft Bot Framework SDK V4 and Azure. The lab scenario focuses on Contoso Restaurant, a fictional restaurant that wants their customers to be able to make reservations using an interactive bot. In order to accomplish this, customers will need to be able to query information using natural language, QnA Maker and speech cognitive services as Bing Speech, Custom Speech and Speech (preview).

## Setup your environment

### A) Setup your Azure subscription

This lab **requires** an Azure subscription.

### B) Install Project Template in Visual Studio

Follow the next steps to install the *Bot Builder SDK V4 for Visual Studio*, if you already have it you can skip this section.

1. [] Open **Microsoft Edge** and navigate to ++https://marketplace.visualstudio.com/items?itemName=BotBuilder.botbuilderv4++.
1. [] Click **Download**.
1. [] When prompted, click **Open**.
1. [] Click **Modify** and complete the installation.

### C) Download the lab materials

Follow the next steps to Download the sample code provided for this lab, it includes a prebuilt React front-end that uses the *Bot Framework Web Chat* component and a Chat Bot based in the *Bot Builder SDK V4 for Visual Studio* template.

1. [] Open **Microsoft Edge** and navigate to ++https://aka.ms/botframeworklab++.
1. [] When prompted, click **Open**.
1. [] Click **Extract** and then **Extract all**.
1. [] Click **Browse...** and then **Downloads**.
1. [] Click **Select Folder** and then **Extract**.

### D) Install Bot Framework Emulator

Follow the next steps to install the *Bot Framework Emulator V4*, if you already have it you can skip this section.

1. [] Open **Microsoft Edge** and navigate to ++https://github.com/Microsoft/BotFramework-Emulator/releases++.
1. [] Download the lastest *4.x* version available.
1. [] When prompted, click **Open**.
1. [] Complete the installation.

## Create a basic Bot

Let's create a basic bot using the SDK V4, we'll run it locally in Visual Studio 2017 using the Bot Framework Emulator.

### A) Create Project

1. [] Open **Visual Studio 2017** from the Start Menu.
1. [] Click **Create new project...**. You can also find this option in VS Menu: **File > New > Project**.
1. [] In the search box from the top right corner type: `bot`.
1. [] Select `Bot Builder Echo Bot V4`.
1. [] Provide a name for your new project: `ChatBot`.
1. [] Click **Ok**.

    > [!NOTE] The template has pre-installed the latest preview version of the new Bot Framework SDK.
1. [] Run the app by clicking on the **IIS Express** button in Visual Studio (with the green play icon).

### B) Debugging with Bot Framework Emulator

The bot emulator provides a convenient way to interact and debug your bot locally.

1. [] Open **EchoBot.cs** from the **Bots** folder.
1. [] Put a **breakpoint** on line 31.
1. [] Run the app by clicking on the **IIS Express** button in Visual Studio (with the green play icon).

    > [!NOTE] A new web page with the Web Chat will be opened in your browser. Don't use this one yet as we'll configure it later.
1. [] Open the **botframework-emulator** from the Start Menu.
1. [] Click **Open** and select the file `ChatBot.bot` from your source code.

    > [!NOTE] Previously we had to provide the bot endpoint to the emulator but now it can read all the configuration from a file.
1. [] **Type** ++Hello++ and press enter.
1. [] Return to **Visual Studio** and wait for the breakpoint to be hit.
1. [] **Mouse over** the `context.Activity.Text` variable to see your input message.
1. [] Press **Continue** in the toolbar.
1. [] **Remove** the breakpoint.
1. [] Go back to the emulator and see the response from the bot.
1. [] **Stop** debugging by clicking the stop button in Visual Studio's toolbar.

### C) Add Welcome message

Notice that when you start a conversation the bot is not showing any initial message, let's add a welcome message to display it to the user at the beginning of the conversations:

1. [] Open **EchoBot.cs** and modify the `OnTurn` method, add the following *else if* to the `if (context.Activity.Type == ActivityTypes.Message)`:

    ```cs
    else if (context.Activity.Type == ActivityTypes.ConversationUpdate && context.Activity.MembersAdded.FirstOrDefault()?.Id == context.Activity.Recipient.Id)
    {
        await context.SendActivity("Hi! I'm a restaurant assistant bot. I can add help you with your reservation.");
    }
    ```

### D) Test in Bot Emulator

Let's run the bot to see LUIS in action.

1. [] Run the app by clicking on the **IIS Express** button in Visual Studio (with the green play icon).
1. [] Return to the **botframework-emulator**.
1. [] Click the **Start Over** button to start a new conversation.
1. [] See the welcome message displayed at the beginning of the conversation.
1. [] Go back to the emulator and see the response from the bot.
1. [] **Stop** debugging by clicking the stop button in Visual Studio's toolbar.

### E) Open the sample Bot

Before we continue adding services to our bot, we'll open the sample code provided in order to have access to prebuilt services and models to speed up the lab.

1. [] Click **File** from Visual Studio menu.
1. [] Click **Open Project/Solution**.
1. [] Select the **solution file** `Downloads\src\TalkToMyBot.sln` and wait for it to load.
1. [] Right click the **Solution** and click `Restore NuGet Packages`.


## Add LUIS to your bot

### A) Create a LUIS subscription

Language Understanding (LUIS) allows your application to understand what a person wants in their own words. LUIS uses machine learning to allow developers to build applications that can receive user input in natural language and extract meaning from it.

While LUIS has a standalone portal for building the model, it uses Azure for subscription management.

Create the LUIS resource in Azure:

1. [] Return to the Azure Portal (++portal.azure.com++).
1. [] Click **Create Resource [+]**  from the left menu and search for **Language Understanding**.
1. [] **Select** the first result and then click the **Create** button.
1. [] Provide the required information:
    * App name: `luis-<your_initials>`.
    * Location: `West US`.
    * Pricing tier: `F0 (5 Calls per second, 10K Calls per month)`.
    * Create a new resource group with the name: `ttmb-lab-<your initials>`.
    * **Confirm** that you read and understood the terms by **checking** the box.
1. [] Click **Create**. This step might take a few seconds.
1. [] Once the deployment is complete, you will see a **Deployment succeeded** notification.
1. [] Go to **All Resources** in the left pane and **search** for the new resource (`build-bot-luis-<your initials>`).
1. [] **Click** on the resource.
1. [] Go to the **Keys** page.
1. [] Copy the **Key 1** value into **Notepad**.

    > [!NOTE] We'll need this key later on.

### B) Import and extend the LUIS model

Before calling LUIS, we need to train it with the kinds of phrases we expect our users to use.

1. [] Login to the **LUIS portal** (++www.luis.ai++).

    > [!NOTE] Use the same credentials as you used for logging into Azure.
1. [] **Scroll down** to the bottom of the welcome page.
1. [] Click **Create an app**.
1. [] Select **United States** from the country list.
1. [] Check the **I agree** checkbox.
1. [] Click the **Continue** button.
1. [] From `My Apps`, click **Import new app**. 
1. [] **Select** the base model from `Downloads\talktomybotlab\resources\talk-to-my-bot.json`.
1. [] Click on the **Done** button.
1. [] **Wait** for the import to complete.
1. [] Click on the **Train** button and wait for it to finish.
1. [] Click the **Test** button to open the test panel.
1. [] **Type** ++I need a dinner reservation++ and press enter.

    > [!NOTE] It should return the `ReserveTable` intent.
1. [] Click the **Test** button in the top right to close the test panel.
1. [] Add a **new intent**:
    * Click on **Intents**.
    * Click on **Create new intent**.
    * Type the new intent name: ++TodaysSpecialty++
    * Click **Done**.
1. [] Add a new **utterance**:
    * Type: ++what's today's special?++
    * Press **Enter**.
1. [] Add another new **utterance**:
    * Type: ++what's the dish of the day?++
    * Press **Enter**.
1. [] **Test** your new intent:
    * Click on the **Train** button and wait for it to finish.
    * Click on **Test** button.
    * Type the following test utterance: ++what's today's special?++
    * Press **Enter**.

        > [!NOTE] The test should return the `TodaysSpecialty` intent.
1. [] Publish your application:
    * Go to the **Publish** tab.
    * Click **Add key**. You'll need to scroll down to find the button.
    * Select the only **tenant**.
    * Select the only **subscription name**.
    * Select the **key** that you created before.
    * Click on **Add Key**.
    * Click on the **Publish** button next to the *Production* slot.
    * Wait for the process to finish.
1. [] Go to the **Settings** tab.
1. [] **Copy** the LUIS *Application ID* to Notepad.

    > [!NOTE] We'll need this app ID later on.

### C) Install LUIS package

1. [] In Visual Studio, right click on the `ChatBot` project and click **Manage NuGet Packages**.
1. [] Mark the **Include Prerelease** checkbox.
1. [] Select the **Browse** tab and search for ++Microsoft.Bot.Builder.Ai.LUIS++.
1. [] Click on the NuGet package, select the latest version and click **Install**.
1. [] Repeat the previous steps to install the ++Microsoft.Bot.Builder.Dialogs++ package.

### D) Add LUIS middleware to your bot

Like all of the Cognitive Services, LUIS is accessible via a RESTful endpoint. However, the Bot Builder SDK has an inbuilt middleware component we can use to simplify this integration. This transparently calls LUIS before invoking our code, allowing our code to focus on processing the user's intent rather than natural language.

1. [] In **Visual Studio**, open **Startup.cs**
1. [] **Add** the following namespaces at the top of the file:

    ```cs
    using Microsoft.Bot.Builder.Ai.LUIS;
    ```
1. [] **Add** the LUIS middleware to the `ConfigureServices` method *after* the ConversationState is added to the Middleware:

    ```cs
    options.Middleware.Add(
        new LuisRecognizerMiddleware(
            new LuisModel(
                "<your_luis_app_id>",
                "<your_luis_subscription_key>",
                new Uri("https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/"))
        ));
    ```

    > [!ALERT] Make sure you replace `<your_luis_app_id>` and `<your_luis_subscription_key>` with the values you captured in Notepad earlier in the lab.

### E) Adjust your Bot to process LUIS results

1. [] Open **EchoBot.cs**.
1. [] **Add** the following namespace at the top of the file:

    ```cs
    using Microsoft.Bot.Builder.Ai.LUIS;
    ```
1. [] **Replace** the contents of the `if (context.Activity.Type == ActivityTypes.Message)` statement with the following code:

    ```cs
    if (!context.Responded)
    {
        var result = context.Services.Get<RecognizerResult>(LuisRecognizerMiddleware.LuisRecognizerResultKey);
        var topIntent = result?.GetTopScoringIntent();
        
        // Your code goes here
    }
    ```

    > [!NOTE] The first step is to extract the LUIS *intent* from the context. This is populated by the middleware.
1. [] **Add** the following code snippet where indicated:

    ```cs
    switch (topIntent != null ? topIntent.Value.intent : null)
    {
        case "TodaysSpecialty":
            await context.SendActivity($"For today we have the following options: {string.Join(", ", BotConstants.Specialties)}");
            break;
        default:
            await context.SendActivity("Sorry, I didn't understand that.");
            break;
    }
    ```

    > [!NOTE] This switch will send the user's message to the right handler based on the LUIS intent name.


### D) Test LUIS configuration

Let's run the bot to see LUIS in action.

1. [] Run the app by clicking on the **IIS Express** button in Visual Studio (with the green play icon).
1. [] Return to the **botframework-emulator**.
1. [] Click the **Start Over** button to start a new conversation.
1. [] **Type** ++what are the specialties for today?++ and press enter.
1. [] See the response, LUIS is processing our input and the bot can handle it accordingly.

## Using the Bot Framework SDK


### A) Add a visual response to your bot: Carousel

Bots are capable of interacting with users through more than just text-based chat. *TodaysSpecialties* intent allows customers to review the different options in the menu for today's specialties. Currently, this method is returning the options as a simple text. Let's modify it and return a carousel.

1. [] Open **EchoBot.cs**.
1. [] **Add** the following namespaces at the top of the file:

    ```cs
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Builder.Prompts.Choices;
    ```
1. [] **Add** the following method:

    ```cs
    private async Task TodaysSpecialtiesHandler(ITurnContext context)
    {
        var actions = new[]
        {
            new CardAction(type: ActionTypes.ShowImage, title: "Carbonara", value: "Carbonara", image: $"{BotConstants.Site}/auto_600x400.png"),
            new CardAction(type: ActionTypes.ShowImage, title: "Pizza", value: "Pizza", image: $"{BotConstants.Site}/property_600x400.jpg"),
            new CardAction(type: ActionTypes.ShowImage, title: "Lasagna", value: "Lasagna", image: $"{BotConstants.Site}/life_600x400.jpg")
          };

        var choices = actions.Select(x => new Choice { Action = x, Value = (string)x.Value }).ToList();
        var cards = actions
          .Select(x => new HeroCard
          {
              Images = new List<CardImage> { new CardImage(x.Image) },
              Buttons = new List<CardAction> { x }
          }.ToAttachment())
          .ToList();
        var activity = (Activity)MessageFactory.Carousel(cards, "For today we have:");

        await context.SendActivity(activity);
    }
    ```
1. [] Modify the `OnTurn` method, replace this line
    ```cs
    await context.SendActivity($"For today we have the following options: {string.Join(", ", BotConstants.Specialties)}");
    ```

    with
    ```cs
    await TodaysSpecialtiesHandler(context);
    ```

### B) Setup the conversation flow

Now that our bot support LUIS, we'll finish the implementation by using the SDK to query the reservation date, confirm the reservation, and ask any clarifying questions. And we'll skip questions if the user has already provided the information as part of their initial utterance.

1. [] Open **EchoBot.cs**.
1. [] **Modify** the `OnTurn` method by adding the following lines to the beginning of the method:

    ```cs
    var state = context.GetConversationState<ReservationData>();
    var dialogContext = dialogs.CreateContext(context, state);
    await dialogContext.Continue();
    ```

    Also add the following case to the switch statement, before the **default** case:

    ```cs
    case "ReserveTable":
        var amountPeople = result.Entities["AmountPeople"] != null ? (string)result.Entities["AmountPeople"]?.First : null;
        var time = GetTimeValueFromResult(result);
        ReservationHandler(dialogContext, amountPeople, time);
        break;
    ```

1. [] Add the following method:

    ```cs
    private string GetTimeValueFromResult(RecognizerResult result)
    {
        var timex = (string)result.Entities["datetime"]?.First["timex"].First;
        if (timex != null)
        {
            timex = timex.Contains(":") ? timex : $"{timex}:00";
            return DateTime.Parse(timex).ToString("MMMM dd \\a\\t HH:mm tt");
        }

        return null;
    }
    ```

    > [!NOTE] The time returned by Luis contains the datetime as a string in a property called 'timex'.

1. [] **Add** the `ReservationHandler` method to start the conversation flow for the table reservation intent:

    ```cs
    private async void ReservationHandler(DialogContext dialogContext, int amountPeople, string time)
    {
        var state = dialogContext.Context.GetConversationState<ReservationData>();
        state.AmountPeople = amountPeople.ToString();
        state.Time = time;
        await dialogContext.Begin(PromptStep.GatherInfo);
    }
    ```

    > [!NOTE] The method invokes the `WaterfallStep` dialog created in the constructor by referencing it by name (`GatherInfo`).
1. Put a breakpoint on the `await dialogContext.Begin(PromptStep.GatherInfo);` line in the `ReservationHandler`.
1. [] **Add** the following class to the EchoBot.cs file:

    ```cs
    public static class PromptStep
    {
        public const string GatherInfo = "gatherInfo";
        public const string TimePrompt = "timePrompt";
        public const string AmountPeoplePrompt = "amountPeoplePrompt";
        public const string NamePrompt = "namePrompt";
        public const string ConfirmationPrompt = "confirmationPrompt";
    }
    ```
1. [] **Add** the variable at the beginning of the class: `private readonly DialogSet dialogs;`
1. [] **Add** the following constructor:

    ```cs
    public EchoBot()
    {
        _dialogs = new DialogSet();
        _dialogs.Add(PromptStep.TimePrompt, new TextPrompt());
        _dialogs.Add(PromptStep.AmountPeoplePrompt, new TextPrompt(AmountPeopleValidator));
        _dialogs.Add(PromptStep.NamePrompt, new TextPrompt());
        _dialogs.Add(PromptStep.ConfirmationPrompt, new ConfirmPrompt(Culture.English));
        _dialogs.Add(PromptStep.GatherInfo, new WaterfallStep[] { TimeStep, AmountPeopleStep, NameStep, ConfirmationStep, FinalStep });
    }
    ```

    > [!NOTE] This will setup the conversation flow passing the Luis results between the steps.

-- TODO: Add step methods (wait for flow review)

1. [] **Add** the following method to request the reservation date if it wasn't provided in the initial utterance:

    ```cs

    ```

1. [] **Add** the following method to request the amount of people in the reservation if it wasn't provided in the initial utterance:

    ```cs

    ```

    > [!NOTE] Notice how we extract the response from the previous step and update the state in each step.

1. [] **Add** the following method to request the name on the reservation:

    ```cs

    ```

1. [] **Add** the following methods to request a confirmation and finalize the reservation:

    ```cs

    ```

1. [] **Add** the following method to validate that the amount of people specified is a number:
    ```cs
    private async Task AmountPeopleValidator(ITurnContext context, TextResult result)
    {
        if (!int.TryParse(result.Value, out int numberPeople))
        {
            result.Status = PromptStatus.NotRecognized;
            await context.SendActivity("The amount of people should be a number.");
        }
    }
    ```

### C) Test the conversation flow

Let's run the bot to see how LUIS processes the new conversation flow.

1. [] Run the app by clicking on the **IIS Express** button in Visual Studio (with the green play icon).
1. [] Return to the **botframework-emulator**.
1. [] Click the **Start Over** button to start a new conversation.
1. [] Type ++I need a dinner reservation for tomorrow at 7:30 pm++ and press **enter**.
1. [] Return to **Visual Studio** and wait for the breakpoint to be hit.
1. [] **Mouse over** the `amountPeople` and `time` variables to inspect their content.

    > [!NOTE] Only the time will have a value, as this is the only piece of information provided in our initial utterance.

1. [] Press **Continue** in the toolbar.
1. [] Return to the **botframework-emulator**.
1. [] The bot will request the amount of people in the reservation. Type a ++abc++ and press **enter**.

    > [!NOTE] Notice the validation message returned by the bot, in this case the amount has to be a number.
1. [] Type a ++2++ and press **enter**.
1. [] The bot will request the name of the reservation. Type a ++Jane Olson++ and press **enter**.
1. [] The bot will request a confirmation. Type ++yes++ and pres **enter**.
    > [!NOTE] At this point the conversation flow will be finished.
1. [] **Stop** debugging by clicking the stop button in VS toolbar.
1. [] **Remove** the breakpoint previously added.


## Knowledge Base

### A) Set up QnA Maker

While the Bot Builder SDK makes building sophisticated dialog flows easy, this won't always scale well. QnA Maker can intelligently build a knowledge base of question and answer pairs and help respond to common user questions.

Setup your QnA Maker instance:

1. [] Return to the **Azure Portal** (++portal.azure.com++).
1. [] Click **Create Resource [+]**  from the left menu and search for **QnA Maker**.
1. [] **Select** the first result and then click the **Create** button.
1. [] Provide the required information:
    * Name: `build-qna-<your initials>`
    * Management Pricing tier: `F0 (3 Calls per second)`
    * Use existing resource group: `ttmb-lab-<your initials>`
    * Search pricing tier: `F0 (3 Indexes, 10K Documents)`
    * Search Location: `West US`
    * App name: `build-qna-<your initials>`
    * Website Location: `West US`
    * Application Insights Location: `West US 2`
1. [] Click **Create** to deploy the service. This step might take a few moments.
1. [] Log into the **QnA Maker portal** (++qnamaker.ai++) using your **Azure** credentials.
1. [] Create a knowledge base:
    * Click on **Create a knowledge base**.
    * Click on **Create new service**.
    * Scroll down to **Step 2**: Connect your QnA service to your KB.
    * Select the previously created Azure service.
    * Scroll down to **Step 3**: Name your KB.
    * Enter the name of the KB: `qna-<your initials>`
    * Scroll down to **Step 4**: Populate your KB.
    * Click **Add File** and select the knowledge base file provided: `resources/qna-ttmb-KB.tsv`
1. [] Click **Create your KB** and wait for the new instance to load.

    > [!NOTE] You will be redirected to the QnA dashboard and it will display the questions in you knowledge base, these are populated from the previously loaded file

1. [] Click **Save and retrain**. This should take a minute.
1. [] Click **Publish** to start the publishing process and then **Publish** again to confirm.
1. [] From the sample HTTP request, get the:
    * **URL** (it should be `https://qna-<your initials>.azurewebsites.net/qnamaker`)
    * **SubscriptionKey** from the header
    * **KnowledgeBaseId** from the URI (it's a GUID)

### C) Install QnA Maker package

1. [] In Visual Studio, right click on the `ChatBot` project and click **Manage NuGet Packages**.
    > [!NOTE]  Make sure the **Include Prerelease** checkbox is selected.

1. [] Select the Browse tab and search for ++Microsoft.Bot.Builder.Ai.QnA++.
1. [] Select the NuGet package, select the latest version and click **Install**.

### B) Add QnA Maker to the bot

The Bot Builder SDK has native support for adding QnA Maker to your bot.

1. [] Return to **Visual Studio**.
1. [] Open **Startup.cs**.
1. [] **Add** the following namespaces at the top of the file:

    ```cs
    using Microsoft.Bot.Builder.Ai.QnA;
    ```
1. [] **Add** the QnA middleware after the LUIS middleware:

    ```cs
    options.Middleware.Add(
      new QnAMakerMiddleware(
        new QnAMakerEndpoint
        {
          Host = "<your qna maker url>",
          EndpointKey = "<your qna maker subscription key>",
          KnowledgeBaseId = "<your knowledge base id>"
        }, 
        new QnAMakerMiddlewareOptions
        {
          EndActivityRoutingOnAnswer = true,
          ScoreThreshold = 0.9f
        }));
    ```

    > [!ALERT] Make sure you update the host, endpoint key, and knowledge base ID.

### D) Test QnA Maker in Bot Emulator

1. [] Run the app by clicking on the **IIS Express** button in Visual Studio (with the green play icon).
1. [] Return to the **botframework-emulator**.
1. [] Click the **Start Over** button to start a new conversation.
1. [] **Type** ++where are you located?++ and press **enter**.
1. [] Return to **Visual Studio** and **stop** debugging by clicking the stop button in the toolbar.

## Personality Chat

Let's make our bot more user friendly by adding the Personality Chat. This cognitive service enhances the bot's conversational capabilities, by allowing to handle small talk using a chosen personality.

### A) Install Personality Chat package

1. [] In Visual Studio, right click on the `ChatBot` project and click **Manage NuGet Packages**.
    > [!NOTE]  Make sure the **Include Prerelease** checkbox is selected.

1. [] Select the Browse tab and search for ++Microsoft.Bot.Builder.PersonalityChat++.
1. [] Select the NuGet package, select the latest version and click **Install**.

### B) Add Personality Chat to the bot

The Bot Builder SDK has native support for adding QnA Maker to your bot.

1. [] Return to **Visual Studio**.
1. [] Open **Startup.cs**.
1. [] **Add** the following namespaces at the top of the file:

    ```cs
    using Microsoft.Bot.Builder.PersonalityChat;
    using Microsoft.Bot.Builder.PersonalityChat.Core;
    ```
1. [] **Add** the Personality Chat middleware after the QnA middleware:

    ```cs
    var personalityChatOptions = new PersonalityChatMiddlewareOptions(
                    respondOnlyIfChat: true,
                    scoreThreshold: 0.5F,
                    botPersona: PersonalityChatPersona.Humorous);
    options.Middleware.Add(new PersonalityChatMiddleware(personalityChatOptions));
    ```

    > [!NOTE] For this sample we will use the default "Humorous" personality, other options are: Professional and Friendly (default).

### C) Test Personality Chat in Bot Emulator

1. [] Run the app by clicking on the **IIS Express** button in Visual Studio (with the green play icon).
1. [] Return to the **botframework-emulator**.
1. [] Click the **Start Over** button to start a new conversation.
1. [] **Type** ++how old are you?++ and press **enter**. See the response from your bot.
1. [] Try changing the personality from **Startup.cs** and see how the bot response changes accordingly to fit the selected personality.
1. [] Return to **Visual Studio** and **stop** debugging by clicking the stop button in the toolbar.


## Add Bing Speech support

In this section we will enable speech in Web Chat to recognize speech and send the transcript to the bot. We will also use Text to Speech from Bing Speech API to generate SSML and send audio back to the Web Chat.

### A) Create Bing Speech subscription

1. [] Return to the Azure Portal (++portal.azure.com++).
1. [] Click **Create Resource [+]**  from the left menu and search for **Bing Speech**.
1. [] **Select** the first result and then click the **Create** button.
1. [] Provide the required information:
    * App name: `bing-speech-<your_initials>`.
    * Location: `West US`.
    * Pricing tier: `F0 (5 Calls per second, 5K Calls per month)`.
    * Use existing resource group: `ttmb-lab-<your initials>`.
    * **Confirm** that you read and understood the terms by **checking** the box.
1. [] Click **Create**. This step might take a few seconds.
1. [] Once the deployment is complete, you will see a **Deployment succeeded** notification.
1. [] Go to **All Resources** in the left pane and **search** for the new resource (`bing-speech-<your initials>`).
1. [] **Click** on the resource.
1. [] Go to the **Keys** page.
1. [] Copy the **Key 1** value into **Notepad**.

    > [!NOTE] We'll need this key later on.

### E) Create Azure Bot Service

The Azure Bot Service is an integrated offering for building and hosting bots. It pulls together the Microsoft Bot Framework for core bot functionality and Azure Web Apps for hosting. In this lab, we'll be using an Azure Web App to host our bot.

1. [] Log into the Azure Portal (++portal.azure.com++).
1. [] In the **New** blade, search for **Web App Bot**.
1. [] **Select** the first result and then click the **Create** button.
1. [] Provide the required information:
    * Bot name: `chat-bot-<your initials>`
    * Use existing resource group: `ttmb-lab-<your initials>`.
    * Location: `West US`
    * Pricing tier: `F0 (10K Premium Messages)`
    * App name: `chat-bot-<your initials>`
    * Bot template: `Basic C#`
    * Azure Storage: create a new one with the recommended name.
    * Application Insights Location: `West US`
1. [] Click on **App service plan/Location**.
1. [] Click **Create New**.
1. [] Provide the required information:
    * App Service plan name: `chat-bot-<your initials>`
    * Location: `West US`
1. [] Click **OK** to save the new App service plan.
1. [] Click **Create** to deploy the service. This step might take a few moments.
1. [] Once the deployment is completed you will see a **Deployment succeeded** notification.
1. [] Go to **All Resources** in the left pane and **search** for the new resource (`chat-bot-<your initials>`).
1. [] Click on the **Web App Bot** to open it.
1. [] Click on the **Test in Web Chat** option in the menu to test the new bot.
1. [] Type **Hello** into the built-in chat control, you should get a response from your bot.

### B) Setup Web Chat

Speech is available as a component called `SpeechRecognizer` in the Web Chat control. The Speech Recognizer generates a transcript from an audio, this will allow us to get the text from the user speech and then send that text to the bot. Before we can set up the speech capabilities, we need to configure the web chat control.

1. [] From the **Azure Portal**, click on the **Channels** option from your **Web App Bot**.
1. [] Under **Add a featured channel**, select **Configure Direct Line Channel**.
1. [] In the **Secret Keys** section, click the **Show** toggle button to display the password.
1. [] **Copy** the password to clipboard.
1. [] **Return** to Visual Studio.
1. [] Open **default.html** in the **wwwroot** folder.

    > [!NOTE] The web chat control has already been imported into the page. We just need to configure it.
1. [] **Replace** the `direct-line-secret` from line 156 with the value on clipboard.

### C) Add Text to Speech to bot

The Speech synthesizer component from the Web Chat allows to convert text to speech. It uses Bing Speech API by default but you can make your own providers to use other speech services.

1. [] Open **EchoBot.cs**.
1. [] **Add** the following variables at the beginning of the **EchoBot** class:

    ```cs
    private const bool USE_CUSTOM_VOICE = false;

    private readonly TextToSpeechService _ttsService;
    ```

    > [!NOTE] We'll use the *USE_CUSTOM_VOICE* constant as a flag to test the voice font later.
1. [] **Add** the following line at the end of the constructor method to initialize the text-to-speech service.:

    ```cs
    _ttsService = new TextToSpeechService(config);
    ```
1. [] **Modify** the *OnTurn* method. Look for the welcome message `"Hi! I'm a restaurant assistant bot.` and replace the next line to include the audio SSML in the response:

    ```cs
    await context.SendActivity(msg, _ttsService.GenerateSsml(msg, USE_CUSTOM_VOICE));
    ```

1. [] **Modify** the *TimeStep* method. Look for the message `When do you need the reservation?`:

    ```cs
    await dialogContext.Prompt(PromptStep.TimePrompt, msg, new PromptOptions { Speak = _ttsService.GenerateSsml(msg, USE_CUSTOM_VOICE) });
    ```

    > [!NOTE] Notice that we set the SSML to the **Speak** Prompt option. As an alternative we could just send the message and the Bot Framework Web Chat will generate the SSML for you, but for this lab we will send the SSML from the backend as it will allow us to include Custom Voice later.

1. [] Repeat the previous step to modify the *Prompts* from `AmountPeopleStep` and `NameStep` methods. Add the following parameter to each Prompt:
    `new PromptOptions { Speak = _ttsService.GenerateSsml(msg, USE_CUSTOM_VOICE)`

1. [] **Modify** the *ConfirmationStep* method. Add the following parameter to each Prompt:
    `new PromptOptions { Speak = msg, RetryPromptString = retryMsg, RetrySpeak = retryMsg })`

1. [] **Modify** the *FinalStep* method. Replace the `dialogContext.Context.SendActivity` line with the following code:

    ```cs
    await dialogContext.Context.SendActivity(msg, _ttsService.GenerateSsml(msg, USE_CUSTOM_VOICE));
    ```

### D) Deploy to Azure from Visual Studio

For the purposes of our lab, we'll be deploying directly from Visual Studio.

1. [] Click on the current connected account in the top right corner of **Visual Studio**.
1. [] Click on **Account Settings...**.
1. [] Click on the **Sign out** button.
1. [] Click on the **Sign in** button.
1. [] **Login** with the same credentials as you used for **Azure**.

    > [!NOTE] This will connect Visual Studio to your Azure subscription.
1. [] Click **Close**.
1. [] **Right-click** the `ChatBot` project.
1. [] Click **Publish**.
1. [] Mark the option `Select Existing`.
1. [] Click **Publish**.
1. [] Select the **Web App** under the only **resource group**.
1. [] Click **OK** and wait for the deployment to complete. This step might take a few minutes.

### E) Test in Web Chat

1. [] Open your chat bot in a browser: `https://chat-bot-<your initials>.azurewebsites.net/`.
1. [] A new web page with the Web Chat will be opened in your browser. Click the **Bing Speech** option.
1. [] Set the Bing subscription key previously obtained from Azure.
1. [] Click **Apply Settings**.
1. [] Click the microphone icon once and say ++I need a table reservation++.
1. [] Wait for the bot to reply, you should get an audio response back.
1. [] Finish the conversation flow using audio.
1. [] **Stop** debugging by clicking the stop button in VS toolbar.

## Add Custom Speech support

This section will show you how to get started using the Custom Speech Service to improve the accuracy of speech-to-text transcription in your application. We'll use a Language Model to improve the output of some food domain-specific language.

### A) Create a Custom Speech subscription

In order to compare the performance of the custom speech service, we'll create a custom speech model to process more specific language. This requires a subscription from the Azure Portal.

1. [] Return to the Azure Portal (++portal.azure.com++).
1. [] Click **Create Resource [+]**  from the left menu and search for **Custom Speech**.
1. [] **Select** the first result and then click the **Create** button.
1. [] Provide the required information:
    * App name: `custom-speech-<your_initials>`.
    * Location: `West US`.
    * Pricing tier: `F0 (Pay As You Go)`.
    * Use existing resource group: `ttmb-lab-<your initials>`.
1. [] Click **Create**. This step might take a few seconds.
1. [] Once the deployment is complete, you will see a **Deployment succeeded** notification.
1. [] Go to **All Resources** in the left pane and **search** for the new resource (`bing-speech-<your initials>`).
1. [] **Click** on the resource.
1. [] Go to the **Keys** page.
1. [] Copy the **Key 1** value into **Notepad**.

### B) Create Language Model

Building a custom language model allows us to improve the vocabulary of our speech model and specify the pronuntiation of specific words.

1. Open `Downloads\resources\Language Data.txt` and review its contents. This file contains text examples of queries and utterances expected of users.
1. Click the **Custom Speech** drop-down menu on the top and select **Adaptation Data**.
1. Click **Import** next to *Language Datasets* and provide the requested information:
    * Name: `Food Language Data`
    * Language Data (.txt): navigate and select `Language Data.txt` in the `Downloads` folder
1. Click **Import**. It will display a table with your new dataset. Wait for the **Status** to change to Complete.
1. Open `Downloads\Pronunciation.txt`. This file contains custom phonetic pronunciations for specific words.
1. Click the **Custom Speech** drop-down menu on the top and select **Adaptation Data**.
1. Click **Import** next to *Pronunciation Datasets* and provide the requested information:
    * Name: `Custom Pronunciation Data`
    * Language data file (.txt): navigate and select `Pronunciation.txt` in the `Downloads` folder
1. Click **Import**. It will display a table with your new dataset. Wait for the **Status** to change to Complete.
1. Click the **Custom Speech** drop-down menu on the top and select **Language Models**.
1. Click **Create New** and provide the requested information:
    * Name: `Food Language Model`
    * Base Language Model: `Microsoft Search and Dictation Model`
    * Language Data: auto populated with the sample data previously created
    * Pronunciation Data: `Custom Pronuncation Data`
    * Subscription: auto populated with the subscription previously added
1. Click **Create**. It will display a table with your new model. This can take several minutes to complete so we'll continue with setting up other parts of the model.

### C) Create custom speech-to-text endpoint

Now that our model has finished building, we can quickly turn it into a web service endpoint.

1. Return to the [CRIS](https://cris.ai) (cris.ai) portal.
1. Click the **Custom Speech** drop-down menu on the top and select **Language Models**.
1. Wait for the **Status** of the `Biology Language Model` to change to Complete.
1. Click the **Custom Speech** drop-down menu on the top and select **Deployments**.
1. Click **Create New** and provide the requested information:
    * Name: `cris-ttmb-<your initials>`
    * Base Model: `Microsoft Search and Dictation Model`
    * Subscription: auto populated with the subscription previously added
    * Language Model and Acoustic Model: the models previously created should appear selected
    * Accept terms & conditions
1. Click **Create**. It will display a table with your new deployment. Wait for the **State** to change to Complete.
1. Click **Details**. It will display the URLs of your custom endpoint for use with either an HTTP request or with the Microsoft Cognitive Services Speech Client Library (which uses Web Sockets).
1. Copy the displayed **Endpoint ID** into Notepad as you'll need it later.
    > [!NOTE] Notice the **WebSocket API** endpoint URL (the one marked for audio up to 15 seconds), in the background our Web Chat uses that endpoint to execute the speech to tex.

### E) Test Custom Speech in Web Chat

// TODO: Copy instructions from previous test just change the provider. Try an utterance in bing and then select cris to see that the output is better.

## Custom Voice

Custom Voice provides a way to create custom voices used to generate audio. The custom voices are deployed as fonts, the models are called **Voice Fonts**. We'll create a new font and use it in our bot output.

### A) Create a Speech (preview) subscription

1. [] Return to the Azure Portal (++portal.azure.com++).
1. [] Click **Create Resource [+]**  from the left menu and search for **Speech**.
1. [] **Select** the *Speech (preview)* result and then click the **Create** button.
1. [] Provide the required information:
    * App name: `speech-<your_initials>`.
    * Location: `West US`.
    * Pricing tier: `S0`.
    * Use existing resource group: `ttmb-lab-<your initials>`.
1. [] Click **Create**. This step might take a few seconds.
1. [] Once the deployment is complete, you will see a **Deployment succeeded** notification.
1. [] Go to **All Resources** in the left pane and **search** for the new resource (`bing-speech-<your initials>`).
1. [] **Click** on the resource.
1. [] Go to the **Keys** page.
1. [] Copy the **Key 1** value into **Notepad**.

### C) Enable custom voice in bot

1. [] Open **EchoBot.cs**.
1. [] **Add** the following variables at the beginning of the **EchoBot** class:

    ```cs
    private const bool USE_CUSTOM_VOICE = false;
1. [] Open **TextToSpeechService.cs** and see the `GenerateSsml` method. // TODO: finish adding detail

### C) Test Voice Font in Web Chat

// TODO: Copy instructions from previous test and modify
