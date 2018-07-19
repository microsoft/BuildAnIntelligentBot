using ChatBot.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ChatBot.Models
{
    /// <summary>
    /// Creates an instance with a SSML body to send text to the
    /// Text To Speech API.
    /// </summary>
    public class TextToSpeechRequest
    {
        /// <summary>
        /// The input options
        /// </summary>
        private readonly InputOptions _inputOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextToSpeechRequest"/> class.
        /// </summary>
        /// <param name="input">The input.</param>
        public TextToSpeechRequest(InputOptions input)
        {
            _inputOptions = input;
        }

        /// <summary>
        /// Sends the specified text to be spoken to the TTS service and saves the response audio to a file.
        /// </summary>
        public async Task<byte[]> Speak()
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler { CookieContainer = cookieContainer };
            var client = new HttpClient(handler);

            var headers = await _inputOptions.GetDefaultHeaders();
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, _inputOptions.RequestUri)
            {
                Content = new StringContent(_inputOptions.GenerateSsml())
            };

            using (var responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None))
            {
                responseMessage.EnsureSuccessStatusCode();
                var httpStream = await responseMessage.Content.ReadAsStreamAsync();

                // Convert stream to byte array
                using (var memoryStream = new MemoryStream())
                {
                    await httpStream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Inputs Options for the TTS Service.
        /// </summary>
        public abstract class InputOptions
        {
            protected readonly string _message;
            protected readonly string _subscriptionKey;

            public InputOptions(string message, string subscriptionKey)
            {
                _message = message;
                _subscriptionKey = subscriptionKey;
            }

            public abstract Uri RequestUri { get; }

            public abstract Task<IEnumerable<KeyValuePair<string, string>>> GetDefaultHeaders();

            public abstract string GenerateSsml();
        }

        public class BingInputOptions : InputOptions
        {
            // Gender values
            public const string GenderMale = "Male";
            public const string GenderFemale = "Female";

            private readonly string _language;
            private readonly string _gender;

            public BingInputOptions(string language, string gender, string message, string subscriptionKey)
                : base(message, subscriptionKey)
            {
                _language = language;
                _gender = gender;
            }

            public override Uri RequestUri => new Uri(BotConstants.TextToSpeechUri);

            public override async Task<IEnumerable<KeyValuePair<string, string>>> GetDefaultHeaders()
            {
                var headers = new Dictionary<string, string>();
                headers.Add("Content-Type", "application/ssml+xml");
                headers.Add("X-Microsoft-OutputFormat", "riff-16khz-16bit-mono-pcm");

                // Authorization Header
                var token = await AzureAuthenticationService.GetAccessToken(_subscriptionKey, "https://westus.api.cognitive.microsoft.com/sts/v1.0/issueToken");
                headers.Add("Authorization", $"Bearer {token}");

                // Guids are randomly generated, refer to the doc
                headers.Add("X-Search-AppId", "89D79F5FE49F405BB9693FEBBACD1399");
                headers.Add("X-Search-ClientID", "B42CA050BC8F4AFBB34CA7514BBC9C2D");

                // The software originating the request
                headers.Add("User-Agent", "TTSClient");

                return headers;
            }

            public override string GenerateSsml()
            {
#pragma warning disable SA1118 // Parameter must not span multiple lines
                var ssmlDoc = new XDocument(
                  new XElement(
                    "speak",
                    new XAttribute("version", "1.0"),
                    new XAttribute(XNamespace.Xml + "lang", "en-US"),
                    new XElement(
                      "voice",
                      new XAttribute(XNamespace.Xml + "lang", _language),
                      new XAttribute(XNamespace.Xml + "gender", _gender),
                      new XAttribute("name", GetLocaleVoiceName().Value),
                      new XRaw(_message))));

                return ssmlDoc.ToString();
#pragma warning restore SA1118 // Parameter must not span multiple lines
            }

            private KeyValuePair<string, string> GetLocaleVoiceName()
            {
                var dictionary = new Dictionary<string, KeyValuePair<string, string>>();

                // List here: https://www.microsoft.com/cognitive-services/en-us/speech-api/documentation/API-Reference-REST/BingVoiceOutput#SupLocales
                const string prefix = "Microsoft Server Speech Text to Speech Voice";
                if (GenderMale.Equals(_gender))
                {
                    dictionary["en"] = new KeyValuePair<string, string>("en-US", $"{prefix} (en-US, BenjaminRUS)");
                    dictionary["es"] = new KeyValuePair<string, string>("es-ES", $"{prefix} (es-ES, Pablo, Apollo)");
                    dictionary["fr"] = new KeyValuePair<string, string>("fr-FR", $"{prefix} (fr-FR, Paul, Apollo)");
                    dictionary["de"] = new KeyValuePair<string, string>("de-DE", $"{prefix} (de-DE, Stefan, Apollo)");
                    dictionary["ja"] = new KeyValuePair<string, string>("ja-JP", $"{prefix} (ja-JP, Ichiro, Apollo)");
                    dictionary["ru"] = new KeyValuePair<string, string>("ru-RU", $"{prefix} (ru-RU, Pavel, Apollo)");
                    dictionary["zh"] = new KeyValuePair<string, string>("zh-CN", $"{prefix} (zh-CN, Kangkang, Apollo)");
                }
                else
                {
                    dictionary["en"] = new KeyValuePair<string, string>("en-US", $"{prefix} (en-US, JessaRUS)");
                    dictionary["es"] = new KeyValuePair<string, string>("es-ES", $"{prefix} (es-ES, Laura, Apollo)");
                    dictionary["fr"] = new KeyValuePair<string, string>("fr-FR", $"{prefix} (fr-FR, Julie, Apollo)");
                    dictionary["de"] = new KeyValuePair<string, string>("de-DE", $"{prefix} (de-DE, Hedda)");
                    dictionary["ja"] = new KeyValuePair<string, string>("ja-JP", $"{prefix} (ja-JP, Ayumi, Apollo)");
                    dictionary["ru"] = new KeyValuePair<string, string>("ru-RU", $"{prefix} (ru-RU, Irina, Apollo)");
                    dictionary["zh"] = new KeyValuePair<string, string>("zh-CN", $"{prefix} (zh-CN, Yaoyao, Apollo)");
                }

                dictionary["it"] = new KeyValuePair<string, string>("it-IT", $"{prefix} (it-IT, Cosimo, Apollo)");
                dictionary["ar"] = new KeyValuePair<string, string>("ar-EG", $"{prefix} (ar-EG, Hoda)");
                dictionary["hi"] = new KeyValuePair<string, string>("hi-IN", $"{prefix} (hi-IN, Kalpana, Apollo)");
                dictionary["ko"] = new KeyValuePair<string, string>("ko-KR", $"{prefix} (ko-KR,HeamiRUS)");
                dictionary["pt"] = new KeyValuePair<string, string>("pt-BR", $"{prefix} (pt-BR, Daniel, Apollo)");

                if (dictionary.ContainsKey(_language))
                {
                    return dictionary[_language];
                }

                return dictionary["en"];
            }
        }

        /// <summary>
        /// Inputs Options for the TTS Service.
        /// </summary>
        public class VoiceFontInputOptions : InputOptions
        {
            private readonly string _uri;
            private readonly string _name;

            public VoiceFontInputOptions(string uri, string name, string message, string subscriptionKey)
                : base(message, subscriptionKey)
            {
                _uri = uri;
                _name = name;
            }

            public override Uri RequestUri => new Uri(_uri);

            public override async Task<IEnumerable<KeyValuePair<string, string>>> GetDefaultHeaders()
            {
                var headers = new Dictionary<string, string>();
                headers.Add("Content-Type", "application/ssml+xml");
                headers.Add("X-Microsoft-OutputFormat", "riff-16khz-16bit-mono-pcm");

                // Guids are randomly generated
                headers.Add("X-FD-ClientID", "E2A9C387-2699-4950-98D5-613880C73A37");
                headers.Add("X-FD-ImpressionGUID", "8090C26A-B881-4E4A-9124-D69B5328D29F");

                // Authorization Header
                var token = await AzureAuthenticationService.GetAccessToken(_subscriptionKey, "https://westus.api.cognitive.microsoft.com/sts/v1.0/issueToken");
                headers.Add("Authorization", $"Bearer {token}");

                return headers;
            }

            public override string GenerateSsml()
            {
                var cleanMessage = _message;

                try
                {
                    // Voice Fonts don't support SSML right now so let's strip out tags
                    var messageDoc = XDocument.Parse($"<root>{cleanMessage}</root>");
                    cleanMessage = string.Join(" ", messageDoc.Descendants().Where(x => !x.HasElements && !string.IsNullOrEmpty(x.Value)).Select(x => x.Value?.Trim()));
                }
                catch (Exception)
                {
                }

#pragma warning disable SA1118 // Parameter must not span multiple lines
                XNamespace ns = "http://www.w3.org/2001/10/synthesis";
                var ssmlDoc = new XDocument(
                  new XElement(
                    ns + "speak",
                    new XAttribute("version", "1.0"),
                    new XAttribute(XNamespace.Xmlns + "mstts", "http://www.w3.org/2001/mstts"),
                    new XAttribute(XNamespace.Xmlns + "emo", "http://www.w3.org/2009/10/emotionml"),
                    new XAttribute(XNamespace.Xml + "lang", "en-US"),
                    new XElement(
                      ns + "voice",
                      new XAttribute("name", _name),
                      new XRaw(cleanMessage))));

                return ssmlDoc.ToString();
#pragma warning restore SA1118 // Parameter must not span multiple lines
            }
        }

        private class XRaw : XText
        {
            public XRaw(string text)
              : base(text)
            {
            }

            public override void WriteTo(System.Xml.XmlWriter writer)
            {
                writer.WriteRaw(Value);
            }
        }
    }
}
