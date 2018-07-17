using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChatBot.Models;
using ChatBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;

namespace ChatBot.TranslatorSpeech
{
    public class TranslatorSpeechMiddleware : IMiddleware
    {
        private readonly TranslatorSpeechService _translatorSpeechService;
        private readonly TranslatorTextService _translatorTextService;
        private readonly TextToSpeechService _textToSpeechService;

        public TranslatorSpeechMiddleware(string translatorSpeechKey, string translatorKey)
        {
            if (string.IsNullOrEmpty(translatorSpeechKey))
                throw new ArgumentNullException(nameof(translatorSpeechKey));
            if (string.IsNullOrEmpty(translatorKey))
                throw new ArgumentNullException(nameof(translatorKey));
            this._translatorTextService = new TranslatorTextService(translatorKey);
            this._translatorSpeechService = new TranslatorSpeechService(translatorSpeechKey);
            this._textToSpeechService = new TextToSpeechService();
        }


        public virtual async Task OnTurn(ITurnContext context, MiddlewareSet.NextDelegate next)
        {
            if (context.Activity.Type == ActivityTypes.Message)
            {
                IMessageActivity message = context.Activity.AsMessageActivity();
                if (message != null)
                {
                    await TranslateMessageAsync(context, message, true).ConfigureAwait(false);

                    context.OnSendActivities(async (newContext, activities, nextSend) =>
                    {
                        //Translate messages sent to the user to user language
                        List<Task> tasks = new List<Task>();
                        foreach (Activity currentActivity in activities.Where(a => a.Type == ActivityTypes.Message))
                        {
                            tasks.Add(TranslateMessageAsync(newContext, currentActivity.AsMessageActivity()));
                        }

                        if (tasks.Any())
                            await Task.WhenAll(tasks).ConfigureAwait(false);

                        return await nextSend();
                    });

                    context.OnUpdateActivity(async (newContext, activity, nextUpdate) =>
                    {
                        //Translate messages sent to the user to user language
                        if (activity.Type == ActivityTypes.Message)
                        {
                            await TranslateMessageAsync(newContext, activity.AsMessageActivity()).ConfigureAwait(false);
                        }

                        return await nextUpdate();
                    });
                }
            }
            
            await next().ConfigureAwait(false);
        }

        /// <summary>
        /// Translate .Text field of a message
        /// </summary>
        /// <param name="context"/>
        /// <param name="message"></param>
        /// <param name="sourceLanguage"/>
        /// <param name="targetLanguage"></param>
        /// <returns></returns>
        private async Task TranslateMessageAsync(ITurnContext context, IMessageActivity message, bool receivingMessage = false)
        {
            var text = message.Text;
            var audioUrl = GetAudioUrl(context.Activity);
            var state = context.GetConversationState<ReservationData>();
            var conversationLanguage = receivingMessage ? context.Activity.Locale : state.ConversationLanguage ?? BotConstants.EnglishLanguage;

            if (string.IsNullOrEmpty(state.ConversationLanguage) || !conversationLanguage.Equals(state.ConversationLanguage))
            {
                state.ConversationLanguage = conversationLanguage;
            }

            // Skip translation if the source language is already English
            if (!conversationLanguage.Contains(BotConstants.EnglishLanguage))
            {
                // STT target language will be English for this lab
                if (!string.IsNullOrEmpty(audioUrl) && string.IsNullOrEmpty(text))
                {
                    var transcript = await this._translatorSpeechService.SpeechToTranslatedText(audioUrl, conversationLanguage, BotConstants.EnglishLanguage);
                    if (transcript != null)
                    {
                        text = transcript.Translation;
                    }
                }
                else
                {
                    // Use TTS translation
                    text = await _translatorTextService.Translate(BotConstants.EnglishLanguage, conversationLanguage, message.Text);
                    context.Activity.Text = text;

                    var ssml = _textToSpeechService.GenerateSsml(text, conversationLanguage);
                    await SendAudioResponse(context, text, ssml);
                }
            }

            message.Text = text;
        }

        private string GetAudioUrl(Activity activity)
        {
            var regex = new Regex(BotConstants.ValidAudioContentTypes, RegexOptions.IgnoreCase);

            var attachment = ((List<Attachment>)activity?.Attachments)?
              .FirstOrDefault(item => regex.Matches(item.ContentType).Count > 0);

            return attachment?.ContentUrl;
        }

        private async Task SendAudioResponse(ITurnContext context, string message, string ssml)
        {
            var audioMsg = context.Activity.CreateReply();
            audioMsg.Type = "PlayAudio";
            audioMsg.Text = message;
            audioMsg.Speak = ssml;

            await context.SendActivity(audioMsg);
        }
    }
}
