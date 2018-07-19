using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBot.TranslatorSpeech
{
    [DataContract]
    public class ResultType
    {
        /// Message type identifier.
        [DataMember(Name = "type")]
        public string MessageType { get; set; }
    }

    public class TextMessageDecoder
    {
        private MemoryStream buffer;
        private Dictionary<string, Type> resultTypeMap;

        public static TextMessageDecoder CreateTranslateDecoder()
        {
            var map = new Dictionary<string, Type>()
            {
                { "final", typeof(FinalResultMessage) },
                { "partial", typeof(PartialResultMessage) }
            };
            return new TextMessageDecoder(map);
        }

        /*
        public static TextMessageDecoder CreateDetectAndTranslateDecoder()
        {
            var map = new Dictionary<string, Type>()
            {
                { "final", typeof(Microsoft.MT.Api.Protocols.SpeechTranslation.DetectAndTranslate.FinalResultMessage) },
                { "partial", typeof(Microsoft.MT.Api.Protocols.SpeechTranslation.DetectAndTranslate.PartialResultMessage) }


            };
            return new TextMessageDecoder(map);
        }
        */ 

        private TextMessageDecoder(Dictionary<string,Type> mapper)
        {
            this.resultTypeMap = mapper;
            this.buffer = new MemoryStream();
        }

        public void AppendData(ArraySegment<byte> data)
        {
            buffer.Write(data.Array, data.Offset, data.Count);
        }

        public Task<object> Decode()
        {
            var ms = Interlocked.Exchange<MemoryStream>(ref this.buffer, new MemoryStream());
            ms.Position = 0;
            return Task<object>.Run(() => {
                object msg = null;
                using (var reader = new StreamReader(ms, System.Text.Encoding.UTF8))
                {
                    var json = reader.ReadToEnd();

                    Debug.Print("This is the language code " + json); //added by KFA

                    var result = JsonConvert.DeserializeObject<ResultType>(json);



                    if (string.Compare(result.MessageType, "final", StringComparison.Ordinal) == 0)
                    {
                        var final = JsonConvert.DeserializeObject(json, this.resultTypeMap["final"]);
                        msg = final;
                    }
                    else if (string.Compare(result.MessageType, "partial", StringComparison.Ordinal) == 0)
                    {
                        var partial = JsonConvert.DeserializeObject(json, this.resultTypeMap["partial"]);
                        msg = partial;
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format("Invalid text message: type='{0}'.", result.MessageType));
                    }
                }
                return msg;
            });
        }
    }

    /// <summary>
    /// Captures TTS audio data to file. Each TTS segment has its own file.
    /// </summary>
    public class BinaryMessageDecoder : IDisposable
    {
        /// Keeps track of number of TTS segments received.
        private int segmentId = 0;
        /// Keeps track of number of bytes that need to be received in order to complete the current segment.
        private int remainingBytes = 0;
        /// Formatted string to generate the file name given the segment ID
        private string format;
        /// Stream to write to the file.
        private FileStream stream;

        /// <summary>
        /// Constructs an instance which will capture TTS segment has the location specified by the format string.
        /// Argument format is of the form "c:\some-path\some-prefix-{0}.wav". Placeholder {0} will be replaced
        /// with the segment ID to generate the full file name.
        /// </summary>
        public BinaryMessageDecoder(string filePathFormat)
        {
            this.format = filePathFormat;
            this.stream = null;
        }

        public void AppendData(ArraySegment<byte> data)
        {
            if (this.remainingBytes <= 0)
            {
                int chunkType = BitConverter.ToInt32(data.Array, data.Offset);
                if (chunkType != 0x46464952) throw new InvalidDataException("Invalid WAV file");
                int size = (int)(BitConverter.ToUInt32(data.Array, data.Offset + 4));
                int riffType = BitConverter.ToInt32(data.Array, data.Offset + 8);
                if (riffType != 0x45564157) throw new InvalidDataException("Invalid WAV file");
                this.remainingBytes = size;
                this.stream = File.Create(String.Format(this.format, segmentId));
            }
            // Write all bytes (including header)
            this.stream.Write(data.Array, data.Offset, data.Count);
            this.remainingBytes -= data.Count;
            if (this.remainingBytes <= 0)
            {
                this.stream.Close();
                this.stream = null;
                this.segmentId++;
            }
        }

        public void Dispose()
        {
            var s = this.stream;
            if (s != null)
            {
                s.Close();
            }
        }
    }
}
