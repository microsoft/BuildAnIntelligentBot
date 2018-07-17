using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBot.TranslatorSpeech
{

    /// <summary>
    /// Defines the set of parameters available to configure the client.
    /// </summary>
    public abstract class SpeechClientOptions
    {
        public string Hostname { get; set; }
        public string AuthHeaderKey { get; set; }
        public string AuthHeaderValue { get; set; }
        public string Features { get; set; }
        public string Profanity { get; set; }
        public Guid ClientAppId { get; set; }
        public string CorrelationId { get; set; }
        public bool Experimental { get; set; }

    }

    /// <summary>
    /// Defines the set of parameters to configure the client in order to use Translate endpoint.
    /// </summary>
    public class SpeechTranslateClientOptions : SpeechClientOptions
    {
        public string TranslateFrom { get; set; }
        public string TranslateTo { get; set; }
        public string Voice { get; set; }
    }

    /// <summary>
    /// Defines the set of parameters to configure the client in order to use DetectAndTranslate endpoint.
    /// </summary>
    public class SpeechDetectAndTranslateClientOptions : SpeechClientOptions
    {
        /// Array of selected languages for DetectAndTranslate.
        public string[] Languages { get; set; }
        /// Array of selected voices for DetectAndTranslate.
        public string[] Voices { get; set; }
    }
}
