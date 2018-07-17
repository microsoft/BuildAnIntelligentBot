using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChatBot.TranslatorSpeech
{
    //
    // Messages from Server to Client 
    //

    /// <summary>
    /// Defines a partial result.
    /// </summary>
    [DataContract]
    public class PartialResultMessage
    {
        /// Message type identifier.
        [DataMember(Name = "type")]
        public string Type = "partial";
        /// Partial result "major.minor" identifier (e.g. "23.4").
        [DataMember(Name = "id")]
        public string Id;
        /// Recognized text.
        [DataMember(Name = "recognition")]
        public string Recognition;
        /// Translation of the recognized text.
        [DataMember(Name = "translation", EmitDefaultValue = false)]
        public string Translation;

        /// <summary>
        /// Time offset in clicks of the start of the partial recognition in ticks relative to the beginning of streaming.
        /// </summary>
        [DataMember(Name = "audioTimeOffset")]
        public string AudioTimeOffset;
        /// <summary>
        /// Duration in ticks of the partial recognition
        /// </summary>
        [DataMember(Name = "audioTimeSize")]
        public string AudioTimeSize;
    }

    
    /// <summary>
    /// Defines a final result.
    /// </summary>
    [DataContract]
    public class FinalResultMessage
    {
        /// Message type identifier.
        [DataMember(Name = "type")]
        public string Type = "final";
        /// Partial result "major" identifier.
        [DataMember(Name = "id")]
        public string Id;
        /// Recognized text.
        [DataMember(Name = "recognition")]
        public string Recognition;
        /// Translation of the recognized text.
        [DataMember(Name = "translation", EmitDefaultValue = false)]
        public string Translation;
        /// <summary>
        /// Time offset in clicks of the start of the recognition in ticks relative to the beginning of streaming.
        /// </summary>
        [DataMember(Name = "audioTimeOffset")]
        public string AudioTimeOffset;
        /// <summary>
        /// Duration in ticks of the recognition
        /// </summary>
        [DataMember(Name = "audioTimeSize")]
        public string AudioTimeSize;

    }

   
}
