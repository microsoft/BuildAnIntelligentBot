# Talking to my Bot

This lab will show you how to build a bot using the new Microsoft Bot Framework SDK V4, the new Speech (Preview) service and other Cognitive Services. The lab scenario focuses on Contoso Restaurant, a fictional restaurant that wants their customers to be able to make reservations using an interactive bot. In order to accomplish this, customers will need to be able to query information using natural language, QnA Maker and speech cognitive services as Translator Speech, Custom Speech and Speech (preview).

## Setup your environment

### A) Setup your Azure subscription

This lab **requires** an Azure subscription. If you delete the resources at the end of the session, total charges will be less than $1 so we strongly recommend using an existing subscription if available.

If you need a new Azure subscription, then there are a couple of options to get a free subscription:

1. The easiest way to sign up for an Azure subscription is with VS Dev Essentials and a personal Microsoft account (like @outlook.com). This does require a credit card; however, there is a spending limit in the subscription so it won't be charged unless you explicitly remove the limit.
    * Open Microsoft Edge and go to the [Microsoft VS Dev Essentials site](https://visualstudio.microsoft.com/dev-essentials/).
    * Click **Join or access now**.
    * Sign in using your personal Microsoft account.
    * If prompted, click Confirm to agree to the terms and conditions.
    * Find the Azure tile and click the **Activate** link.
1. Alternatively, if the above isn't suitable, you can sign up for a free Azure trial.
    * Open Microsoft Edge and go to the [free Azure trial page](https://azure.microsoft.com/en-us/free/).
    * Click **Start free**.
    * Sign in using your personal Microsoft account.
1. Complete the Azure sign up steps and wait for the subscription to be provisioned. This usually only takes a couple of minutes.

Please see the lab team if any of the above steps present any problems.

### B) Install Project Template in Visual Studio

Follow the next steps to install the *Bot Builder SDK V4 for Visual Studio* template, if you already have it you can skip this section.

1. [] We will use Visual Studio 2017 with Bot template to develop a bot. If you don't have Visual Studio you can download it from the following URL given below.
    * Download Visual Studio 2017 from https://www.visualstudio.com/downloads/.
    * Refer Visual Studio 2017 system requirement from https://www.visualstudio.com/en-us/productinfo/vs2017-system-requirements-vs.
1. [] Open **Microsoft Edge** and navigate to ++https://marketplace.visualstudio.com/items?itemName=BotBuilder.botbuilderv4++.
1. [] Click **Download**.
1. [] When prompted, click **Open**.
1. [] Click **Modify** and complete the template installation.

### C) Download the lab materials

Follow the next steps to download the sample code provided for this lab, it includes a prebuilt React front-end that uses the *Bot Framework Web Chat* component and a Chat Bot based in the *Bot Builder SDK V4 for Visual Studio* template.

1. [] Open **Microsoft Edge** and navigate to ++https://aka.ms/botframeworklab++.
1. [] When prompted, click **Open**.
1. [] Click **Extract** and then **Extract all**.
1. [] Click **Browse...** and then **Downloads**.
1. [] Click **Select Folder** and then **Extract**.

### D) Install Bot Framework Emulator

Follow the next steps to install the *Bot Framework Emulator V4*, if you already have it you can skip this section.

1. [] Open **Microsoft Edge** and navigate to ++https://github.com/Microsoft/Bot Framework Emulator/releases++.
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
1. [] Open the **Bot Framework Emulator** from the Start Menu.
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

1. [] Open **EchoBot.cs** and modify the `OnTurn` method, add the following *else if* to the end of the method:

    ```cs
    else if (context.Activity.Type == ActivityTypes.ConversationUpdate && context.Activity.MembersAdded.FirstOrDefault()?.Id == context.Activity.Recipient.Id)
    {
        await context.SendActivity("Hi! I'm a restaurant assistant bot. I can add help you with your reservation.");
    }
    ```

1. [] Let's run the bot to see the welcome message.
    * Run the app by clicking on the **IIS Express** button in Visual Studio (with the green play icon).
    * Return to the **Bot Framework Emulator**.
    * Click the **Start Over** button to start a new conversation.
    * See the welcome message displayed at the beginning of the conversation.
    * **Stop** debugging by clicking the stop button in Visual Studio's toolbar.


## Add LUIS to your bot

Language Understanding (LUIS) allows your application to understand what a person wants in their own words. LUIS uses machine learning to allow developers to build applications that can receive user input in natural language and extract meaning from it.

### A) Create a LUIS subscription

While LUIS has a standalone portal for building the model, it uses Azure for subscription management.

Create the LUIS resource in Azure:

1. [] Return to the Azure Portal (++portal.azure.com++).
1. [] Click **Create Resource [+]**  from the left menu and search for **Language Understanding**.
1. [] **Select** the first result and then click the **Create** button.
1. [] Provide the required information:
    * App name: `chatbot-luis-<your_initials>`.
    * Location: `West US`.
    * Pricing tier: `F0 (5 Calls per second, 10K Calls per month)`.
    * Create a new resource group with the name: `ttmb-lab-<your initials>`.
    * **Confirm** that you read and understood the terms by **checking** the box.
1. [] Click **Create**. This step might take a few seconds.
1. [] Once the deployment is complete, you will see a **Deployment succeeded** notification.
1. [] Go to **All Resources** in the left pane and **search** for the new resource (`chatbot-luis-<your initials>`).
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
    * Type: ++what is the specialty for today?++
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


### D) Install LUIS package

The Bot Builder SDK V4 provides a package to integrate LUIS with your bot. Follow the next steps to install the new package in the sample code.

Before we continue adding services to our bot, we'll open the sample code provided in order to have access to prebuilt services and models to speed up the lab.

1. [] Click **File** from Visual Studio menu.
1. [] Click **Open Project/Solution**.
1. [] Select the **solution file** `Downloads\talktomybotlab\src\TalkToMyBot.sln` and wait for it to load.
1. [] Right click the **Solution** and click `Restore NuGet Packages`.

Now let's install the LUIS package from NuGet:

1. [] Right click on the `ChatBot` project and click **Manage NuGet Packages**.
1. [] Mark the **Include Prerelease** checkbox.
1. [] Select the **Browse** tab and search for ++Microsoft.Bot.Builder.Ai.LUIS++.
1. [] Click on the NuGet package, select the latest version and click **Install**.

### E) Add LUIS middleware to your bot

Like all of the Cognitive Services, LUIS is accessible via a RESTful endpoint. However, the Bot Builder SDK has an inbuilt middleware component we can use to simplify this integration. This transparently calls LUIS before invoking our code, allowing our code to focus on processing the user's intent rather than natural language.

1. [] In **Visual Studio**, open **Startup.cs**
1. [] **Add** the following namespace at the top of the file:

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

### F) Adjust your Bot to process LUIS results

Modify the Bot code to handle the results from LUIS.

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


### G) Test LUIS configuration

Let's run the bot to see LUIS in action.

1. [] Run the app by clicking on the **IIS Express** button in Visual Studio (with the green play icon).
1. [] Return to the **Bot Framework Emulator**.
1. [] Click **File** -> **Open**.
1. [] Select the **bot file** `Downloads\talktomybotlab\src\ChatBot.bot` and wait for it to load.

    > [!NOTE] We are now using the sample code therefore we have to open a new bot file.
1. [] Click the **Start Over** button to start a new conversation.
1. [] **Type** ++what is the specialty for today??++ and press enter.
1. [] See the response, LUIS is processing our input and the bot can handle it accordingly.

## Customising your Bot

### A) Add a visual response to your bot: Carousel

Bots are capable of interacting with users through more than just text-based chat. *TodaysSpecialties* intent allows customers to review the different options in the menu for today's recommendations. Currently, this method is returning the options as a simple text. Let's modify it and return a carousel.

1. [] Open **EchoBot.cs**.
1. [] **Add** the following method:

    ```cs
    private async Task TodaysSpecialtiesHandler(ITurnContext context)
    {
        var actions = new[]
        {
            new CardAction(type: ActionTypes.ShowImage, title: "Carbonara", value: "Carbonara", image: $"{BotConstants.Site}/carbonara.jpg"),
            new CardAction(type: ActionTypes.ShowImage, title: "Pizza", value: "Pizza", image: $"{BotConstants.Site}/pizza.jpg"),
            new CardAction(type: ActionTypes.ShowImage, title: "Lasagna", value: "Lasagna", image: $"{BotConstants.Site}/lasagna.jpg")
        };

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

### B) Test the visual response

Let's run the bot to see how the bot displays the response using an advanced card.

1. [] Run the app by clicking on the **IIS Express** button in Visual Studio (with the green play icon).
1. [] Return to the **Bot Framework Emulator**.
1. [] Click the **Start Over** button to start a new conversation.
1. [] Type ++What is today's specialty?++ and press **enter**.
1. [] The bot will display a set of food recommendations using images in a Carousel.
1. [] **Stop** debugging by clicking the stop button in VS toolbar.

### C) Install Dialogs package

The *Dialogs* package from NuGet allows to build Dialog sets that can be used to setup conversations as a sequence of steps.

1. [] Right click on the `ChatBot` project and click **Manage NuGet Packages**.
1. [] Mark the **Include Prerelease** checkbox.
1. [] Select the **Browse** tab and search for ++Microsoft.Bot.Builder.Dialogs++.
1. [] Click on the NuGet package, select the latest version and click **Install**.

### D) Setup the conversation flow

Now that our bot support LUIS, we'll finish the implementation by using the SDK to query the reservation date, confirm the reservation, and ask any clarifying questions. And we'll skip questions if the user has already provided the information as part of their initial utterance.

1. [] Open **EchoBot.cs**.
1. [] **Add** the following namespaces at the top of the file:

    ```cs
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Prompts;
    using Microsoft.Recognizers.Text;
    using ConfirmPrompt = Microsoft.Bot.Builder.Dialogs.ConfirmPrompt;
    using TextPrompt = Microsoft.Bot.Builder.Dialogs.TextPrompt;
    ```

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

1. [] Add the following method to extract the reservation date:

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

1. [] **Add** the following method to request the reservation date if it wasn't provided in the initial utterance:

    ```cs
    private async Task TimeStep(DialogContext dialogContext, object result, SkipStepFunction next)
    {
        var state = dialogContext.Context.GetConversationState<ReservationData>();
        if (string.IsNullOrEmpty(state.Time))
        {
            var msg = "When do you need the reservation?";
            await dialogContext.Prompt(PromptStep.TimePrompt, msg);
        }
        else
        {
            await next();
        }
    }
    ```

1. [] **Add** the following method to request the amount of people in the reservation if it wasn't provided in the initial utterance:

    ```cs
    private async Task AmountPeopleStep(DialogContext dialogContext, object result, SkipStepFunction next)
    {
        var state = dialogContext.Context.GetConversationState<ReservationData>();

        if (result != null)
        {
            var time = (result as TextResult).Value;
            state.Time = time;
        }

        if (state.AmountPeople == null)
        {
            var msg = "How many people will you need the reservation for?";
            await dialogContext.Prompt(PromptStep.AmountPeoplePrompt, msg);
        }
        else
        {
            await next();
        }
    }

    ```

    > [!NOTE] Notice how we extract the response from the previous step and update the state in each step.

1. [] **Add** the following method to request the name on the reservation:

    ```cs
    private async Task NameStep(DialogContext dialogContext, object result, SkipStepFunction next)
    {
        var state = dialogContext.Context.GetConversationState<ReservationData>();

        if (result != null)
        {
            state.AmountPeople = (result as TextResult).Value;
        }

        if (state.FullName == null)
        {
            var msg = "And the name on the reservation?";
            await dialogContext.Prompt(PromptStep.NamePrompt, msg);
        }
        else
        {
            await next();
        }
    }

    ```

1. [] **Add** the following methods to request a confirmation and finalize the reservation:

    ```cs
    private async Task ConfirmationStep(DialogContext dialogContext, object result, SkipStepFunction next)
    {
        var state = dialogContext.Context.GetConversationState<ReservationData>();

        if (result != null)
        {
            state.FullName = (result as TextResult).Value;
        }

        if (state.Confirmed == null)
        {
            var msg = $"Ok. Let me confirm the information: This is a reservation for {state.Time} for {state.AmountPeople} people. Is that correct?";
            var retryMsg = "Please confirm, say 'yes' or 'no' or something like that.";

            await dialogContext.Prompt(
              PromptStep.ConfirmationPrompt,
              msg);
        }
        else
        {
            await next();
        }
    }
    ```

1. [] *Add* the following method to verify the confirmation response and finish the flow.
    ```cs
    private async Task FinalStep(DialogContext dialogContext, object result, SkipStepFunction next)
    {
        var state = dialogContext.Context.GetConversationState<ReservationData>();
        if (result != null)
        {
            var confirmation = (result as ConfirmResult).Confirmation;
            string msg = null;
            if (confirmation)
            {
                msg = $"Great, we will be expecting you this {state.Time}. Thanks for your reservation {state.FirstName}!";
            }
            else
            {
                msg = "Thanks for using the Contoso Assistance. See you soon!";
            }

            await dialogContext.Context.SendActivity(msg, _ttsService.GenerateSsml(msg, BotConstants.EnglishLanguage));
        }

        await dialogContext.End(state);
    }
    ```

1. [] **Add** the following method to validate that the amount of people specified is a number:
    ```cs
    private async Task AmountPeopleValidator(ITurnContext context, TextResult result)
    {
        if (!int.TryParse(result.Value, out int numberPeople))
        {
            result.Status = PromptStatus.NotRecognized;
            var msg = "The amount of people should be a number.";
            await context.SendActivity(msg);
        }
    }
    ```

### E) Test the conversation flow

Let's run the bot to see how LUIS processes the new conversation flow.

1. [] Run the app by clicking on the **IIS Express** button in Visual Studio (with the green play icon).
1. [] Return to the **Bot Framework Emulator**.
1. [] Click the **Start Over** button to start a new conversation.
1. [] Type ++I need a dinner reservation for tomorrow at 7:30 pm++ and press **enter**.
1. [] Return to **Visual Studio** and wait for the breakpoint to be hit.
1. [] **Mouse over** the `amountPeople` and `time` variables to inspect their content.

    > [!NOTE] Only the time will have a value, as this is the only piece of information provided in our initial utterance.

1. [] Press **Continue** in the toolbar.
1. [] Return to the **Bot Framework Emulator**.
1. [] The bot will request the amount of people in the reservation. Type a ++abc++ and press **enter**.

    > [!NOTE] Notice the validation message returned by the bot, in this case the amount has to be a number.
1. [] Type a ++2++ and press **enter**.
1. [] The bot will request the name of the reservation. Type a ++Jane Olson++ and press **enter**.
1. [] The bot will request a confirmation. Type ++yes++ and pres **enter**.
    > [!NOTE] At this point the conversation flow will be finished.
1. [] **Stop** debugging by clicking the stop button in VS toolbar.
1. [] **Remove** the breakpoint previously added.


## Adding Knowledge Base to your Bot

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
1. [] Return to the **Bot Framework Emulator**.
1. [] Click the **Start Over** button to start a new conversation.
1. [] **Type** ++where are you located?++ and press **enter**.
1. [] Return to **Visual Studio** and **stop** debugging by clicking the stop button in the toolbar.

## Implementing Personality Chat

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
1. [] Return to the **Botframework Emulator**.
1. [] Click the **Start Over** button to start a new conversation.
1. [] **Type** ++how old are you?++ and press **enter**. See the response from your bot.
1. [] Return to **Visual Studio** and **stop** debugging by clicking the stop button in the toolbar.
1. [] Try changing the personality from **Startup.cs** and see how the bot response changes accordingly to fit the selected personality.

    > [!NOTE] Review the image below to see which utterances you can try and what output to expect in each case.

## Add Speech Support

In this section we will enable speech in Web Chat to recognize speech and send the transcript to the bot. We will also use Text to Speech from Speech (Preview) to generate SSML and send audio back from the Web Chat.

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
1. [] Go to **All Resources** in the left pane and **search** for the new resource (`speech-preview-<your initials>`).
1. [] **Click** on the resource.
1. [] Go to the **Keys** page.
1. [] Copy the **Key 1** value into **Notepad**.

    > [!NOTE] We'll need this key later on.

### B) Create Azure Bot Service

The Azure Bot Service is an integrated offering for building and hosting bots. It pulls together the Microsoft Bot Framework for core bot functionality and Azure Web Apps for hosting. In this lab, we'll be using an Azure Web App to host our bot and use Direct Line Channel to integrate the Web Chat with the bot service.

1. [] Log into the Azure Portal (++portal.azure.com++).
1. [] In the **New** blade, search for **Web App Bot**.
1. [] **Select** the first result and then click the **Create** button.
1. [] Provide the required information:
    * Bot name: `chatbot-<your initials>`
    * Use existing resource group: `ttmb-lab-<your initials>`.
    * Location: `West US`
    * Pricing tier: `F0 (10K Premium Messages)`
    * App name: `chatbot-<your initials>`
    * Bot template: `Basic C#`
    * Azure Storage: create a new one with the recommended name.
    * Application Insights Location: `West US`
1. [] Click on **App service plan/Location**.
1. [] Click **Create New**.
1. [] Provide the required information:
    * App Service plan name: `chatbot-<your initials>`
    * Location: `West US`
1. [] Click **OK** to save the new App service plan.
1. [] Click **Create** to deploy the service. This step might take a few moments.
1. [] Once the deployment is completed you will see a **Deployment succeeded** notification.
1. [] Go to **All Resources** in the left pane and **search** for the new resource (`chatbot-<your initials>`).
1. [] Click on the **Web App Bot** to open it.
1. [] Click on the **Test in Web Chat** option in the menu to test the new bot.
1. [] Type **Hello** into the built-in chat control, you should get a response from your bot.

### B) Set Up Web Chat

Speech is available as a component called `SpeechRecognizer` in the Web Chat control. The Speech Recognizer generates a transcript from an audio, this will allow us to get the text from the user speech and then send that text to the bot. Before we can set up the speech capabilities, we need to configure the web chat control.

1. [] From the **Azure Portal**, click on the **Channels** option of your **Web App Bot**.
1. [] Under **Add a featured channel**, select **Configure Direct Line Channel**.
1. [] In the **Secret Keys** section, click the **Show** toggle button to display the password.
1. [] **Copy** the password to clipboard.
1. [] **Return** to Visual Studio.
1. [] Open **default.html** in the **wwwroot** folder.

    > [!NOTE] The web chat control has already been imported into the page, we just need to configure it. This Web Chat was modified to use the Speech (preview) websockets instead of the general Bing service.
1. [] **Replace** the `direct-line-secret` from line 134 with the value on clipboard.
1. [] **Replace** the Speech Subscription key `subscription-key` from line 43 with the value previously obtained from Azure.

### C) Add Text to Speech to bot

For this scenario we will generate the audio SSML in the backend (our bot code). We'll use one of the pre set voices and play the audio by using the Speech synthesizer component from the Web Chat. The synthesizer was adjusted to use the Speech (preview) websockets endpoint, which in the background uses the regular Bing text-to-speech feature.

1. [] Open **EchoBot.cs**.
1. [] **Add** the following variable at the beginning of the **EchoBot** class:

    ```cs
    private readonly TextToSpeechService _ttsService;
    ```
1. [] **Add** the following line at the end of the constructor method to initialize the text-to-speech service.:

    ```cs
    _ttsService = new TextToSpeechService(config);
    ```
1. [] **Modify** the *OnTurn* method. Look for the welcome message `"Hi! I'm a restaurant assistant bot.` and replace the next line to include the audio SSML in the response:

    ```cs
    await context.SendActivity(msg, _ttsService.GenerateSsml(msg, BotConstants.EnglishLanguage));
    ```

1. [] **Modify** the *TimeStep* method. Look for the message `When do you need the reservation?`:

    ```cs
    await dialogContext.Prompt(PromptStep.TimePrompt, msg, new PromptOptions { Speak = _ttsService.GenerateSsml(msg, BotConstants.EnglishLanguage) });
    ```

    > [!NOTE] Notice that we set the SSML to the **Speak** Prompt option. As an alternative we could just send the message and the Bot Framework Web Chat will generate the SSML for you, but for this lab we will send the SSML from the backend.

1. [] Repeat the previous step to modify the *Prompts* from `AmountPeopleStep` and `NameStep` methods. Add the following parameter to each Prompt:
    `new PromptOptions { Speak = _ttsService.GenerateSsml(msg, BotConstants.EnglishLanguage)`

1. [] **Modify** the *ConfirmationStep* method. Add the following parameter to each Prompt:
    ```cs
    new PromptOptions
    {
        Speak = _ttsService.GenerateSsml(msg, BotConstants.EnglishLanguage),
        RetryPromptString = retryMsg,
        RetrySpeak = _ttsService.GenerateSsml(retryMsg, BotConstants.EnglishLanguage)
    }
    ```

1. [] **Modify** the *FinalStep* method. Replace the `dialogContext.Context.SendActivity` line with the following code:

    ```cs
    await dialogContext.Context.SendActivity(msg, _ttsService.GenerateSsml(msg, BotConstants.EnglishLanguage));
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
1. [] Select the bot **Web App** previously created under the group **ttmb-lab-<your initials>**.
1. [] Click **OK** and wait for the deployment to complete. This step might take a few minutes.

### E) Test in Web Chat

1. [] Open your chat bot in a browser: `https://chatbot-<your initials>.azurewebsites.net/`.
1. [] Click the **Speech** option.
1. [] Click the microphone icon once and say ++I need a table reservation++.
1. [] Wait for the bot to reply, you should get an audio response back.
1. [] Finish the conversation flow using audio.

## Add Custom Speech support

This section will show you how to get started using the Custom Speech Service to improve the accuracy of speech-to-text transcription in your application. We'll use a Language Model to improve the output of some food domain-specific language.

### A) Create a Custom Speech subscription

In order to compare the performance of the custom speech service, we'll create a custom speech model to process more specific language. This requires link your speech subscription from the CRIS Portal.

1. Log into the [CRIS Dashboard](https://cris.ai/) (cris.ai).
1. Click on your user account in the right side of the top ribbon and click on **Subscriptions** in the drop-down menu.
1. Click **Connect existing subscription** and provide the requested information:
    * Name: `cris-svc-<your initials>`
    * Subscription Key: *paste speech subscription value previously obtained from Azure Portal*
1. Click **Add**. It will display a page with the result of your subscription validation.

### B) Create Language Model

Building a custom language model allows us to improve the vocabulary of our speech model and specify the pronuntiation of specific words.

1. Open `Downloads\talktomybotlab\resources\custom_speech_language_ds.txt` and review its contents. This file contains text examples of queries and utterances expected from users.
1. Click the **Custom Speech** drop-down menu on the top and select **Adaptation Data**.
1. Click **Import** next to *Language Datasets* and provide the requested information:
    * Name: `Food Language Model`.
    * Language data file (.txt): navigate and select `Downloads\talktomybotlab\resources\custom_speech_pronunciation_ds.txt` in the `Downloads` folder.
1. Click **Import**. It will display a table with your new dataset. Wait for the **Status** to change to **Complete**.
1. Open `Downloads\talktomybotlab\resources\custom_speech_pronunciation_ds.txt`. This file contains custom phonetic pronunciations for specific words.
1. Click the **Custom Speech** drop-down menu on the top and select **Adaptation Data**.
1. Click **Import** next to *Pronunciation Datasets* and provide the requested information:
    * Name: `Custom Pronunciation Data`.
    * Language data file (.txt): navigate and select `TalkToMyBot/resources/custom_speech_pronunciation_ds.txt` in the `Downloads` folder
1. Click **Import**. It will display a table with your new dataset. Wait for the **Status** to change to Complete.
1. Click the **Custom Speech** drop-down menu on the top and select **Language Models**.
1. Click **Create New** and provide the requested information:
    * Name: `Food Language Model`.
    * Base Language Model: `Microsoft Search and Dictation Model`.
    * Language Data: auto populated with the sample data previously created.
    * Pronunciation Data: `Custom Pronuncation Data`.
    * Subscription: auto populated with the subscription previously added.
1. Click **Create**. It will display a table with your new model. This can take several minutes to complete.

### C) Create custom speech-to-text endpoint

Now that our model has finished building, we can quickly turn it into a web service endpoint.

1. Return to the [CRIS](https://cris.ai) (cris.ai) portal.
1. Click the **Custom Speech** drop-down menu on the top and select **Language Models**.
1. Wait for the **Status** of the `Food Language Model` to change to Complete.
1. Click the **Custom Speech** drop-down menu on the top and select **Endpoints**.
1. Click **Create New** and provide the requested information:
    * Name: `cris-ttmb-<your initials>`
    * Subscription: auto populated with the subscription previously added
    * Scenario: Universal.
    * Language Model: the model previously created should appear selected
    * Accept terms & conditions
1. Click **Create**. It will display a table with your new deployment. Wait for the **State** to change to Complete.
1. Click **Details**. It will display the URLs of your custom endpoint for use with either an HTTP request or with the Microsoft Cognitive Services Speech Client Library (which uses Web Sockets).
1. Copy the displayed **Endpoint ID** into Notepad as you'll need it later.
    > [!NOTE] Notice the **WebSocket API** endpoint URL (the one marked for audio up to 15 seconds), in the background our Web Chat uses that endpoint to execute the speech to text transcription.

### D) Modify chat bot to support Custom Speech

1. [] **Return** to Visual Studio.
1. [] Open **default.html** in the **wwwroot** folder.
1. [] **Replace** the Custom Speech Endpoint Id `custom-speech-endpoint-id` from line 45 with the value previously obtained from Cris dashboard.

    > [!NOTE] The prebuilt chat bot already provides support for Custom Speech web sockets, we just have to add the configuration. Notice that we can access all the speech service using the same Speech subscription key.

### E) Test Custom Speech in Web Chat

In order to compare the Speech service with the Custom Speech model that we configured, we will be using the **risotto** and **calzone** words that are part of the pronunciations dataset. The pronuntiation for these words varies from Italian to English, carefully listen to the output audios and notice the difference as we will be using an Italian accent to try this scenario.

Follow the next steps to listen the words pronunciation:
1. [] Open your browser and go to `https://www.bing.com/Translator`.
1. [] Type in `calzone` and select **Italian** from the list of languages.
1. [] Click on the **listen** icon.
1. [] In the second pane from the translator select **English** from the list of languages.
1. [] Click the **listen** icon and compare the pronunciation between English and Italian for this word.
1. [] Repeat the previous steps for `risotto`.

Deploy and test custom speech:
1. [] Go to Visual Studio and **Right-click** the `ChatBot` project.
1. [] Click **Publish**.
1. [] Click **Publish** again and wait for the deployment to complete. This step might take a few minutes.
1. [] Open your chat bot in a browser: `https://chatbot-<your initials>.azurewebsites.net/`.
1. [] Click the **Speech** option and try the following utterances using an *Italian* accent for the highlighted words:
    * Click the microphone icon once and say ++Do you have **risotto**?++
    * Click the microphone icon once and say ++Do you have **calzone**?++

    > [!NOTE] The default speech service won't be able to understand these utterances and will reply with: `Sorry, I didn't understand that`.

1. [] Click the **Custom Speech** option and try the same utterances again:
    * Click the microphone icon once and say ++Do you have **risotto**?++
    * The bot will reply with a response from our Knowledge Base: *We have a large selection of risottos in our menu, our recommendation is risotto ai funghi.*
    * Click the microphone icon once and say ++Do you have **calzone**?++
    * The bot will reply with a response from our Knowledge Base: *We sell calzones only Fridays.*

    > [!NOTE] Notice that Custom Speech is able to pick up the food options that we specified using a different pronunciation. The responses from QnA Maker don't return audio as we only adjusted our main flow for text-to-speech support.

## Custom Translator (Translator Speech)

By combining the Translator Speech API with our bot, we can build a solution that can interact with a global audience using speech and different languages. The service also allows to create and deploy customized neural machine translations (NMT), meaning users can start talking to the bot in whatever language they choose and using a system that understands the terminology used in their specific business and industry. For this lab we won't be using the NMT customization feature, instead we will use the Speech to Text translation capabilites of the service by uploading a prerecorded French audio file. The sample code provides a prebuilt Translator Speech Service that we will use to translate the speech to English and pass the translated transcript to LUIS. Once the bot finishes processing the intent it will respond using audio and French language, for this purpose we will use the Translator Text and the existing Text to Speech capabilities from our bot.

### A) Create a Translator Speech subscription

Before using the Translator Speech service we have to create the resource in Azure.

1. [] Return to the Azure Portal (++portal.azure.com++).
1. [] Click **Create Resource [+]**  from the left menu and search for **Translator Speech**.
1. [] **Select** the *Translator Speech* result and then click the **Create** button.
1. [] Provide the required information:
    * Name: `translator-speech-<your_initials>`.
    * Subscritpion: your azure subscription.
    * Pricing tier: `F0 (10H Up to 10 hours of audio input)`.
    * Use existing resource group: `ttmb-lab-<your initials>`.
1. [] Click **Create**. This step might take a few seconds.
1. [] Once the deployment is complete, you will see a **Deployment succeeded** notification.
1. [] Go to **All Resources** in the left pane and **search** for the new resource (`translator-speech-<your initials>`).
1. [] **Click** on the resource.
1. [] Go to the **Keys** page.
1. [] Copy the **Key 1** value into **Notepad**.

    > [!NOTE] We'll need this key later on for Speech to Text translation.

### B) Create a Translator Text subscription

Before using the Translator Text service we have to create the resource in Azure.

1. [] Return to the Azure Portal (++portal.azure.com++).
1. [] Click **Create Resource [+]**  from the left menu and search for **Translator Text**.
1. [] **Select** the *Translator Text* result and then click the **Create** button.
1. [] Provide the required information:
    * Name: `translator-text-<your_initials>`.
    * Subscritpion: your azure subscription.
    * Pricing tier: `F0 (2M Up to 2M characters translated)`.
    * Use existing resource group: `ttmb-lab-<your initials>`.
1. [] Click **Create**. This step might take a few seconds.
1. [] Once the deployment is complete, you will see a **Deployment succeeded** notification.
1. [] Go to **All Resources** in the left pane and **search** for the new resource (`translator-text-<your initials>`).
1. [] **Click** on the resource.
1. [] Go to the **Keys** page.
1. [] Copy the **Key 1** value into **Notepad**.

    > [!NOTE] We'll need this key later on for text translation.

### C) Add translation support to the bot

The Translator Speech API allows to integrate the service into existing applications, workflows and websites. For this lab we created a custom middleware to add the speech translation (stt) support to our bot pipeline.

1. [] Open the **Startup.cs** file.
1. [] **Update** the `ConfigureServices` method by adding the following code snippet **before** the LUIS middleware:

    ```cs
    options.Middleware.Add(
        new TranslatorSpeechMiddleware(
            Configuration["TranslatorSpeechSubscriptionKey"],
            Configuration["TranslatorTextSubscriptionKey"]
        ));
    ```
    > [!ALERT] Make sure you put this after the LUIS middleware. Otherwise the incoming message will be sent to LUIS *before* it's translated to English (which is the language understood by our LUIS model).
1. [] Open the **appsettings.json** file.
1. [] Replace the **<your_translator_speech_subscription_key>** and **<your_translator_text_subscription_key>** values with the keys previously obtained from Azure.

### D) Test the speech translation

Let's see the translation middleware in action by asking for discounts in French.

Deploy and test translator speech:
1. [] Go to Visual Studio and **Right-click** the `ChatBot` project.
1. [] Click **Publish**.
1. [] Click **Publish** again and wait for the deployment to complete. This step might take a few minutes.
1. [] Open your chat bot in a browser: `https://chatbot-<your initials>.azurewebsites.net/`.
1. [] Click the **Speech** option .
1. [] Click the **Test bot translator with an audio file** option.
1. [] Click the **Browse** button and select the file `Downloads\talktomybotlab\resources\discounts_french.wav`.
1. [] Select the **French** language from the drop down list.

    > [!NOTE] If you want to see all the languages available you can use the languages endpoint: https://dev.microsofttranslator.com/Languages?api-version=1.0&scope=text,speech,tts
1. [] Click **Submit Audio** and wait for the bot to respond back. This might take a few seconds as it has to upload your audio file.
1. [] The bot wil respond with audio and with the text "Cette semaine, nous avons un rabais de 25% dans l’ensemble de notre sélection de vins".

## Custom Voice

Custom Voice provides a way to create custom voices used to generate audio. The custom voices are deployed as fonts, the models are called **Voice Fonts**. We'll create a new font and use it in our bot output.

### A) Enable custom voice in bot

1. [] Open **EchoBot.cs**.
1. [] **Add** the following variables at the beginning of the **EchoBot** class:

    ```cs
    private const bool UseCustomVoice = false;
    ```
1. [] Open **TextToSpeechService.cs** and see the `GenerateSsml` method. // TODO: finish adding detail

### B) Test Voice Font in Web Chat

// TODO: Copy instructions from previous test and modify


## Adding a Bot to a Web Application

We'll see how simple it is to add the web chat widget to a web application, using the bot deployed in Azure. In `Downloads\talktomybotlab\resources` you will find a folder named `restaurant` with a standalone website, which could easily be replaced with a modern single page application (SPA). You can see this website in action by serving it from an HTTP server or by opening `index.html` in a browser.

### A) Add the Web Chat Libraries**

The web chat control that we'll add to the static website will use the same libraries as our bot: `botchat.js` and `CongnitiveServices.js`. These library files have already been placed in the `lib` directory in the website's root folder along with the stylesheet `botchat.css`, which has a set of default styles for the web chat control. To enable these, we'll need to add the corresponding references in the website's index file `index.html`.

1. Find `Downloads\talktomybot\resources\restaurant-lab.zip` and uncompress it in the same directory.
1. **Add** in the following snippet **before** the custom web chat stylesheet `bot.css`:
    ```html
    <link href="./lib/botchat.css" rel="stylesheet" />
    ```
1. **Add** the following code snippet right before the closing HEAD tag: `</head>`.
    ```html
    <script src="./lib/botchat.js"></script>
    <script src="./lib/CognitiveServices.js"></script>
    ```

### B) Add the Web Chat widget

We'll add the Bot Framework Web Chat control to our static page `index.html`.
    ```html
    <div id="bot"></div>
    ```

### C) Add the initialization script
Now that we have in place the logic that makes the web chat work and the DOM element that will host the web chat control, all that is left is to add the initialization script that will create a working instance of the web chat control. Immediately after the web chat DOM element added in the previous step, add the following snippet:

    ```js
    <script>
      const speechSubscriptionKey = <your-speech-subscription-key>;

      const user = {
        id: 'userid',
        name: 'username',
      };

      const bot = {
        id: 'botid',
        name: 'botname',
      };

      const speechRecognizer = new CognitiveServices.SpeechRecognizer({
        subscriptionKey: speechSubscriptionKey,
      });

      const speechSynthesizer = new CognitiveServices.SpeechSynthesizer({
        subscriptionKey: speechSubscriptionKey,
        customVoiceEndpointUrl: 'https://westus.tts.speech.microsoft.com/cognitiveservices/v1',
      });

      const speechOptions = {
        speechRecognizer,
        speechSynthesizer,
      };

      BotChat.App({
          bot: bot,
          locale: 'en-us',
          user: user,
          speechOptions: speechOptions,
          directLine: {
            secret: <direct-line-secret>,
            webSocket: true,
          },
        },
        document.getElementById('bot')
      );
    </script>
    ```
    > [!NOTE] Replace `<your-speech-subscription-key>` and `<direct-line-secret>` with the same values previously obtained.

### D) Test the bot using the web chat widget

1. Open `index.html` in a browser.
1. Click the microphone icon and talk to your bot: "What is today's specialty?".
1. The bot should respond back, now you are able to interact with your existing bot from a new web site.


*Media Elements and Templates. You may copy and use images, clip art, animations, sounds, music, shapes, video clips and templates provided with the sample application and identified for such use in documents and projects that you create using the sample application. These use rights only apply to your use of the sample application and you may not redistribute such media otherwise.*
