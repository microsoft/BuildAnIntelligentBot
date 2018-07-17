using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatBot.Models
{
    /// <summary>
    /// Holds one utterance for the transcript
    /// </summary>
    public class TranscriptUtterance
    {
        public string Recognition;
        public string Translation;
        public string TranslationAudioUrl;
    }
}
