using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChatBot.Models;
using ChatBot.TranslatorSpeech;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using static ChatBot.TranslatorSpeech.SpeechClient;

namespace ChatBot.Services
{
    public class TranslatorSpeechService
    {
        private const string BaseUrl = "dev.microsofttranslator.com";
        private const int AudioChunkSizeInMs = 100;
        private const long IdleSecondsToWait = 15;
        private static readonly WaveFormat WaveFormat = new WaveFormat(16000, 16, 1);

        private SpeechClient s2smtClient;
        private BinaryWriter binaryWriter;
        private int textToSpeechBytes = 0;
        private long lastReceivedPacketTick = 0;
        private long currentFileStartTicks = 0;
        private int audioBytesSent = 0;
        private string correlationId;
        private CancellationTokenSource streamAudioFromFileInterrupt = null;
        private string subscriptionKey;

        private List<TranscriptUtterance> Transcripts { get; } = new List<TranscriptUtterance>();

        public TranslatorSpeechService(IOptions<MySettings> config)
        {
            this.subscriptionKey = config.Value.TranslatorSpeechSubscriptionKey;
        }

        public TranslatorSpeechService(string subscriptionKey)
        {
            this.subscriptionKey = subscriptionKey;
        }

        public async Task<TranscriptUtterance> SpeechToTranslatedText(string audioUrl, string sourceLanguage, string targetLanguage)
        {
            Transcripts.Clear();

            // Setup speech translation client options
            var options = GetSpeechTranslateClientOptions(sourceLanguage, targetLanguage);
            this.textToSpeechBytes = 0;
            this.audioBytesSent = 0;

            var sendAudio = ConnectAsync(options).ContinueWith((t) => SendAudioMessage(t, audioUrl))
                .ContinueWith((t) => {
                    if (t.IsFaulted)
                    {
                        Trace.TraceError("Failed to start sending audio. Exception: {0}", t.Exception);
                    }
                    else
                    {
                        Disconnect();
                    }
                });

            await sendAudio;

            return Transcripts.FirstOrDefault();
        }

        private void SendAudioMessage(Task connectionTask, string audioUrl)
        {
            if (connectionTask.IsFaulted || connectionTask.IsCanceled || !s2smtClient.IsConnected())
            {
                Trace.TraceError("Unable to connect: cid='{0}', Error: {1}'.",
                    this.correlationId, connectionTask.Exception);
            }
            else
            {
                // Send the WAVE header
                s2smtClient.SendBinaryMessage(new ArraySegment<byte>(GetWaveHeader()));
                streamAudioFromFileInterrupt = new CancellationTokenSource();
                int totalChunksSent = 0;
                lastReceivedPacketTick = DateTime.Now.Ticks;
                Trace.TraceInformation($"Starting file {audioUrl}");
                Task currTask = Task.Run(async() => totalChunksSent = await this.StreamFile(audioUrl, streamAudioFromFileInterrupt.Token, totalChunksSent))
                    .ContinueWith((x) =>
                    {
                        if (x.IsFaulted)
                        {
                            Trace.TraceError("Error while streaming audio from input file {0}. Exception: {1}", audioUrl, x.Exception);
                        }
                        else
                        {
                            Trace.TraceInformation("Done streaming audio from input file {0}.", audioUrl);
                        }
                    });

                CheckTaskResult(currTask);
                Trace.TraceInformation("Connected: cid='{0}'", this.correlationId);
            }
        }

        private async Task<int> StreamFile(string path, CancellationToken token, int initChunks)
        {

            var wavFile = new WavFileAudioSource(path);
            await wavFile.LoadFile();
            var audioSource = new AudioSourceCollection(new IAudioSource[] {
                wavFile,
                new WavSilenceAudioSource(2000),
            });

            var handle = new AutoResetEvent(true);
            int wait = AudioChunkSizeInMs;
            long audioChunkSizeInTicks = TimeSpan.TicksPerMillisecond * (long)(AudioChunkSizeInMs);
            long tnext = DateTime.Now.Ticks + AudioChunkSizeInMs;
            int chunksSent = initChunks;
            foreach (var chunk in audioSource.Emit(AudioChunkSizeInMs))
            {
                if (token.IsCancellationRequested)
                {
                    return chunksSent;
                }

                // Send chunk to speech translation service
                this.OnAudioDataAvailable(chunk);
                ++chunksSent;
                handle.WaitOne(wait);
                tnext = tnext + audioChunkSizeInTicks;
                wait = (int)((tnext - DateTime.Now.Ticks) / TimeSpan.TicksPerMillisecond);
                if (wait < 0) wait = 0;
            }
            return chunksSent;
        }

        private void OnAudioDataAvailable(ArraySegment<byte> data)
        {
            if (s2smtClient != null)
            {
                s2smtClient.SendBinaryMessage(new ArraySegment<byte>(data.Array, data.Offset, data.Count));
                audioBytesSent += data.Count;
            }
        }

        private void SendTextMessage(Task connectionTask, string message)
        {
            if (connectionTask.IsFaulted || connectionTask.IsCanceled || !s2smtClient.IsConnected())
            {
                Trace.TraceError("Unable to connect: cid='{0}', Error: {1}'.",
                    this.correlationId, connectionTask.Exception);
            }
            else
            {
                lastReceivedPacketTick = DateTime.Now.Ticks;
                Task currTask = Task.Run(() => this.s2smtClient.SendTextMessage(message))
                    .ContinueWith((x) =>
                    {
                        if (x.IsFaulted)
                        {
                            Trace.TraceError("Error while sending text for tts. Exception: {0}", x.Exception);
                        }
                        else
                        {
                            Trace.TraceInformation("Done sending test for tts");
                        }
                    });

                CheckTaskResult(currTask);
                Trace.TraceInformation("Connected: cid='{0}'", this.correlationId);
            }
        }

        private byte[] GetWaveHeader()
        {
            using (var stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8);
                writer.Write(Encoding.UTF8.GetBytes("RIFF"));
                writer.Write(0);
                writer.Write(Encoding.UTF8.GetBytes("WAVE"));
                writer.Write(Encoding.UTF8.GetBytes("fmt "));
                WaveFormat.Serialize(writer);
                writer.Write(Encoding.UTF8.GetBytes("data"));
                writer.Write(0);

                stream.Position = 0;
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        private async Task ConnectAsync(SpeechClientOptions options)
        {
            if (s2smtClient != null && s2smtClient.IsConnected())
            {
                return;
            }

            if (options.GetType() != typeof(SpeechTranslateClientOptions))
            {
                throw new InvalidOperationException("Type of SpeechClientOptions is not supported.");
            }
            options.AuthHeaderValue = await AzureAuthenticationService.GetAccessToken(subscriptionKey);

            // Create the client
            s2smtClient = new SpeechClient((SpeechTranslateClientOptions)options, CancellationToken.None);
            TextMessageDecoder textDecoder = TextMessageDecoder.CreateTranslateDecoder();

            s2smtClient.OnBinaryData += (c, a) => { AddSamplesToStream(a); };
            s2smtClient.OnEndOfBinaryData += (c, a) => { AddSamplesToStream(a); };
            s2smtClient.OnTextData += (c, a) => { textDecoder.AppendData(a); lastReceivedPacketTick = DateTime.Now.Ticks; };
            s2smtClient.OnEndOfTextData += (c, a) =>
            {
                textDecoder.AppendData(a);
                lastReceivedPacketTick = DateTime.Now.Ticks;
                textDecoder
                    .Decode()
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            Trace.TraceError("Failed to decode incoming text message: {0}", t.Exception);
                        }
                        else
                        {
                            object msg = t.Result;
                            TranscriptUtterance utterance = null;
                            if (msg.GetType() == typeof(FinalResultMessage))
                            {
                                var final = msg as FinalResultMessage;
                                long offset = long.Parse(final.AudioTimeOffset);
                                long duration = long.Parse(final.AudioTimeSize);
                                TimeSpan currFileStartTime = TimeSpan.FromTicks(offset - currentFileStartTicks);
                                TimeSpan currFileEndime = TimeSpan.FromTicks(currFileStartTime.Ticks + duration);
                                Trace.TraceInformation("Final recognition {0} ({1} - {2}): {3}", final.Id, currFileStartTime.ToString(), currFileEndime.ToString(), final.Recognition);
                                Trace.TraceInformation("Final translation {0}: {1}", final.Id, final.Translation);
                                utterance = new TranscriptUtterance();
                                utterance.Recognition = final.Recognition;
                                utterance.Translation = final.Translation;
                            }
                            if (msg.GetType() == typeof(PartialResultMessage))
                            {
                                // Partial results are not used in this lab, leaving code as a reference
                                var partial = msg as PartialResultMessage;
                                Trace.TraceInformation("Partial recognition {0}: {1}", partial.Id, partial.Recognition);
                                Trace.TraceInformation("Partial translation {0}: {1}", partial.Id, partial.Translation);
                                utterance = new TranscriptUtterance();
                                utterance.Recognition = partial.Recognition;
                                utterance.Translation = partial.Translation;
                            }

                            if (utterance != null)
                            {
                                Transcripts.Add(utterance);
                            }
                        }
                    });
            };
            s2smtClient.Failed += (c, ex) =>
            {
                Trace.TraceError("SpeechTranslation client reported an error: {0}", ex);
            };
            s2smtClient.Disconnected += (c, ea) =>
            {
                Trace.TraceInformation("Connection has been lost.");
                Trace.TraceInformation($"Errors (if any): \n{string.Join("\n", s2smtClient.Errors)}");
            };

            await s2smtClient.Connect();
        }

        private void AddSamplesToStream(ArraySegment<byte> a)
        {
            if (this.textToSpeechBytes <= 0)
            {
                int chunkType = BitConverter.ToInt32(a.Array, a.Offset);
                if (chunkType != 0x46464952) throw new InvalidDataException("Invalid WAV file");
                int size = (int)(BitConverter.ToUInt32(a.Array, a.Offset + 4));
                int riffType = BitConverter.ToInt32(a.Array, a.Offset + 8);
                if (riffType != 0x45564157) throw new InvalidDataException("Invalid WAV file");
                textToSpeechBytes = size;
                this.binaryWriter = new BinaryWriter(new MemoryStream());
            }

            this.binaryWriter.Write(a.Array, a.Offset, a.Count);

            // Adjust remaining bytes
            textToSpeechBytes -= a.Count;
            if (this.textToSpeechBytes <= 0)
            {
                this.binaryWriter.Close();
            }
        }

        private SpeechTranslateClientOptions GetSpeechTranslateClientOptions(string sourceLanguage, string targetLanguage)
        {
            this.correlationId = Guid.NewGuid().ToString("D").Split('-')[0].ToUpperInvariant();

            // Setup speech translation client options
            var options = new SpeechTranslateClientOptions()
            {
                TranslateFrom = sourceLanguage,
                TranslateTo = targetLanguage
            };

            options.Hostname = BaseUrl;
            options.AuthHeaderKey = "Authorization";
            options.AuthHeaderValue = ""; // set later in ConnectAsync.
            options.ClientAppId = new Guid("EA66703D-90A8-436B-9BD6-7A2707A2AD99");
            options.CorrelationId = this.correlationId;
            options.Features = SpeechClient.Features.TimingInfo.ToString();
            options.Profanity = ProfanityFilter.Strict.ToString();
            options.Experimental = false;

            return options;
        }

        private void Disconnect()
        {
            var disconnect = s2smtClient.Disconnect()
                    .ContinueWith((t) =>
                    {
                        if (t.IsFaulted)
                        {
                            Trace.TraceError("Disconnect call to client failed. {0}", t.Exception);
                        }
                        s2smtClient.Dispose();
                        s2smtClient = null;
                    })
                    .ContinueWith((t) => {
                        if (t.IsFaulted)
                        {
                            Trace.TraceError("Disconnected but there were errors. {0}", t.Exception);
                        }
                        else
                        {
                            Trace.TraceInformation("Disconnected. cid='{0}'", correlationId);
                        }
                    });

            disconnect.Wait();
            while (disconnect.Status != TaskStatus.RanToCompletion)
            {
                Trace.TraceInformation("Thread ID: {0}, Status: {1}", Thread.CurrentThread.ManagedThreadId, disconnect.Status);
            }
        }

        private void CheckTaskResult(Task currentTask)
        {
            bool done = false;
            bool error = false;
            while (!done)
            {
                if (currentTask.Status == TaskStatus.Canceled || currentTask.Status == TaskStatus.Faulted)
                {
                    Trace.TraceError("Task was canceled or faulted.");
                    done = true;
                    error = true;
                }
                else if (!s2smtClient.IsConnected())
                {
                    Trace.TraceInformation($"Client is not connected");
                    Trace.TraceError($"Errors from client (if any): \n{string.Join("\n", s2smtClient.Errors)}");
                    done = true;
                    error = true;
                }
                else if (currentTask.Status == TaskStatus.RanToCompletion)
                {
                    long ticksSinceLastResult = DateTime.Now.Ticks - this.lastReceivedPacketTick;
                    if (TimeSpan.TicksPerSecond * IdleSecondsToWait < ticksSinceLastResult)
                    {
                        Trace.TraceInformation($"Ticks since last result: {ticksSinceLastResult}, waitTicks {TimeSpan.TicksPerSecond * IdleSecondsToWait}");
                        Trace.TraceInformation($"Finished tts");
                        done = true;
                    }
                }
                if (!done)
                {
                    Thread.Sleep(10);
                }

                if (error)
                {
                    break;
                }
            }
        }
    }
}
