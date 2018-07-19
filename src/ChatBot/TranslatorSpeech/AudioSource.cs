using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatBot.TranslatorSpeech
{

    public interface IAudioSource
    {
        // Emit the audio in chunks of given duration.
        IEnumerable<ArraySegment<byte>> Emit(int chunkDurationInMs);
    }

    /// Audio source is WAV file (16bit PCM 16kHz - 320 bytes / 10ms)
    public class WavFileAudioSource : IAudioSource
    {
        private byte[] data;

        public string SourceFile { get; private set; }

        /// <summary>
        /// Creates an audio source from a WAV file (16bit PCM 16kHz - 320 bytes / 10ms).
        /// Emit the entire file (RIFF header and all sections).
        /// </summary>
        /// <param name="url">Url of the audio file.</param>
        public WavFileAudioSource(string url)
        {
            this.SourceFile = url;
        }

        public async Task LoadFile()
        {
            using (WebClient webClient = new WebClient())
            {
                this.data = await webClient.DownloadDataTaskAsync(this.SourceFile);
            }

            using (MemoryStream stream = new MemoryStream())
            {
                // WAV HEADER
                //  chunk type "RIFF" (0x52494646)
                //  RIFF type "WAVE" (0x57415645)
                int chunkType = BitConverter.ToInt32(this.data, 0);
                if (chunkType != 0x46464952) throw new InvalidDataException("Invalid WAV file");
                UInt32 size = BitConverter.ToUInt32(this.data, 4);
                int riffType = BitConverter.ToInt32(this.data, 8);
                if (riffType != 0x45564157) throw new InvalidDataException("Invalid WAV file");
                // Read WAV chunks
                int chunkStartIndex = 12;
                while (chunkStartIndex < (size - 8))
                {
                    chunkType = BitConverter.ToInt32(this.data, chunkStartIndex);
                    char[] ct = ASCIIEncoding.ASCII.GetChars(this.data, chunkStartIndex, 4);
                    int chunkSize = (int)BitConverter.ToUInt32(this.data, chunkStartIndex + 4);
                    // chunk type "data" (0x61746164)
                    if (chunkType == 0x61746164)
                    {
                        stream.Write(this.data, chunkStartIndex + 8, chunkSize - 8);
                    }
                    chunkStartIndex += 8 + chunkSize;
                }

                this.data = stream.ToArray();
            }
        }

        public IEnumerable<ArraySegment<byte>> Emit(int chunkDurationInMs)
        {
            if ((chunkDurationInMs < 10) || ((chunkDurationInMs % 10) != 0))
            {
                throw new ArgumentException("chunkDurationInMs must be a factor of 10");
            }

            int packetsPerChunk = chunkDurationInMs / 10;
            int bytesPerChunk = 320 * packetsPerChunk;
            int position = 0;
            int bytesRemaining = data.Length;
            while (bytesRemaining >= bytesPerChunk)
            {
                yield return new ArraySegment<byte>(data, position, bytesPerChunk);
                bytesRemaining -= bytesPerChunk;
                position += bytesPerChunk;
            }
            if (bytesRemaining > 0)
            {
                byte[] buffer = new byte[bytesPerChunk];
                Buffer.BlockCopy(data, position, buffer, 0, bytesRemaining);
                yield return new ArraySegment<byte>(buffer, 0, bytesPerChunk);
            }
        }

    }

    /// Audio source generating silence matching WAV 16bit PCM 16kHz - 320 bytes / 10ms
    public class WavSilenceAudioSource : IAudioSource
    {
        public int DurationInMs { get; set; }

        public WavSilenceAudioSource(int durationInMs)
        {
            if ((durationInMs < 10) || ((durationInMs % 10) != 0))
            {
                throw new ArgumentException("durationInMs must be a factor of 10");
            }
            this.DurationInMs = durationInMs;
        }

        public IEnumerable<ArraySegment<byte>> Emit(int chunkDurationInMs)
        {
            int packetsPerChunk = chunkDurationInMs / 10;
            int bytesPerChunk = 320 * packetsPerChunk;
            byte[] data = new byte[bytesPerChunk];
            int timeRemainingInMs = this.DurationInMs;
            while (timeRemainingInMs >= 0)
            {
                yield return new ArraySegment<byte>(data, 0, bytesPerChunk);
                timeRemainingInMs -= chunkDurationInMs;
            }
        }
    }

    /// Audio source which is a collection of other audio sources.
    public class AudioSourceCollection : IAudioSource
    {
        public event EventHandler<IAudioSource> OnNewSourceDataEmit;        
        private IEnumerable<IAudioSource> Sources;

        public AudioSourceCollection(IEnumerable<IAudioSource> sources)
        {
            this.Sources = sources;
        }

        public IEnumerable<ArraySegment<byte>> Emit(int chunkDurationInMs)
        {
            foreach (var source in this.Sources)
            {
                if(this.OnNewSourceDataEmit != null)
                { 
                    this.OnNewSourceDataEmit(this, source);
                }
                foreach (var chunk in source.Emit(chunkDurationInMs))
                {
                    yield return chunk;                    
                }
            }
        }
    }

}
