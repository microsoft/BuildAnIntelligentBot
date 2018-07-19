using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBot.TranslatorSpeech
{
    internal class QueueItem
    {
        public QueueItem(WebSocketMessageType opCode, ArraySegment<byte> content)
        {
            if (opCode == WebSocketMessageType.Close) throw new ArgumentOutOfRangeException("opCode");
            if (content.Array == null) throw new ArgumentNullException("content");

            this.OpCode = opCode;
            this.Content = content;
            this.CompletionSource = new TaskCompletionSource<bool>();
        }

        /// Message type.
        public WebSocketMessageType OpCode { get; private set; }
        /// Message content.
        public ArraySegment<byte> Content { get; private set; }
        /// Completion source to signal to sender when message has been sent.
        public TaskCompletionSource<bool> CompletionSource { get; private set; }
    }

    public class SpeechClient : IDisposable
    {
        /// <summary>
        /// Supported features of the API
        /// </summary>
        [Flags]
        public enum Features
        {
            /// <summary>
            /// Gets the text to speech (TTS) audio of the translation
            /// </summary>
            TextToSpeech = 1,

            /// <summary>
            /// Gets partial speech recognitions (hypotheses)
            /// </summary>
            Partial = 2,

            /// <summary>
            /// Returns timing offsets from the beginning of the stream for recognitions.
            /// </summary>
            TimingInfo = 3,
        }

        /// <summary>
        /// Defines how profanities need to be handled by the service
        /// </summary>
        public enum ProfanityFilter
        {
            /// <summary>
            /// Only moderate profanities will be returned in text and audio
            /// </summary>
            Moderate,

            /// <summary>
            /// No profanity filter is done server side
            /// </summary>
            Off,

            /// <summary>
            /// All profanities will be removed from text and audio by the service
            /// </summary>
            Strict
        }

        private const int ReceiveChunkSize = 8*1024;
        private const int SendChunkSize = 8*1024;

        private SpeechClientOptions options;
        private CancellationToken cancellationToken;
        private ClientWebSocket webSocketclient;
        private Uri clientWsUri;

        public event EventHandler<ArraySegment<byte>> OnTextData;
        public event EventHandler<ArraySegment<byte>> OnEndOfTextData;
        public event EventHandler<ArraySegment<byte>> OnBinaryData;
        public event EventHandler<ArraySegment<byte>> OnEndOfBinaryData;
        public event EventHandler Disconnected;
        public event EventHandler<Exception> Failed;

        /// Queue of messages waiting to be sent.
        private BlockingCollection<QueueItem> outgoingMessageQueue = new BlockingCollection<QueueItem>();

        public List<string> Errors { get; } = new List<string>();

        public SpeechClient(SpeechTranslateClientOptions options, CancellationToken cancellationToken)
        {
            this.Init(options, cancellationToken);
            StringBuilder query = new StringBuilder();
            if (options.TranslateTo == "yue")
                //Skip setting the voice in case of yue (Cantonese). Server side bug.
                query.AppendFormat("from={0}&to={1}", options.TranslateFrom, options.TranslateTo);
            else
                query.AppendFormat("from={0}&to={1}", options.TranslateFrom, options.TranslateTo);
            if (!String.IsNullOrWhiteSpace(options.Voice))
            {
                query.AppendFormat("&voice={0}", options.Voice);
            }
            if (!String.IsNullOrWhiteSpace(options.Features))
            {
                query.AppendFormat("&features={0}", options.Features);
            }
            if (!String.IsNullOrWhiteSpace(options.Profanity))
            {
                query.AppendFormat("&profanity={0}", options.Profanity);
            }
            if (options.Experimental)
            {
                query.AppendFormat("&flight={0}", "experimental");
            }
            this.clientWsUri = new Uri(string.Format("{0}://{1}/speech/translate?{2}&api-version=1.0", "wss", this.Hostname, query.ToString()));
        }


        private void Init(SpeechClientOptions options, CancellationToken cancellationToken)
        {
            if (options == null) throw new ArgumentNullException("options");
            if (cancellationToken == null) throw new ArgumentNullException("cancellationToken");

            this.options = options;
            this.cancellationToken = cancellationToken;
            this.webSocketclient = new ClientWebSocket();
            webSocketclient.Options.SetRequestHeader(this.options.AuthHeaderKey, this.options.AuthHeaderValue);
            webSocketclient.Options.SetRequestHeader("X-ClientAppId", this.options.ClientAppId.ToString());
            if (!string.IsNullOrWhiteSpace(this.options.CorrelationId))
            {
                webSocketclient.Options.SetRequestHeader("X-CorrelationId", this.options.CorrelationId);
            }

        }

        public string Hostname { get { return this.options.Hostname; } }

        //TODO: replace this: public string RequestId { get { return this.webSocketclient.RequestId; } }

        public async Task Connect()
        {
            try
            {
                await webSocketclient.ConnectAsync(this.clientWsUri, this.cancellationToken);
                // Start receive and send loops
                var receiveTask = Task.Run(() => this.StartReceiving())
                    .ContinueWith((t) => ReportError(t))
                    .ConfigureAwait(false);
                var sendTask = Task.Run(() => this.StartSending())
                    .ContinueWith((t) => ReportError(t))
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.InnerException.ToString());
                throw;
            }
            
        }

        public bool IsConnected()
        {
            WebSocketState wsState = WebSocketState.None;
            try
            {
                wsState = this.webSocketclient.State;
            }
            catch (ObjectDisposedException)
            {
                wsState = WebSocketState.None;
            }
            return ((this.cancellationToken.IsCancellationRequested == false)
                 && ((wsState == WebSocketState.Open) || (wsState == WebSocketState.CloseReceived)));
        }

        public async Task Disconnect()
        {
            if (this.IsConnected())
            {
                try
                {
                    await this.webSocketclient.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, this.cancellationToken);
                }
                finally
                {
                    if (this.Disconnected != null) this.Disconnected(this, EventArgs.Empty);
                }
            }
        }

        public void SendBinaryMessage(ArraySegment<byte> content)
        {
            SendMessage(WebSocketMessageType.Binary, content);
        }

        public void SendTextMessage(string text)
        {
            SendMessage(WebSocketMessageType.Text, new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)));
        }

        private void SendMessage(WebSocketMessageType messageType, ArraySegment<byte> content)
        {
            var msg = new QueueItem(messageType, content);
            this.outgoingMessageQueue.Add(msg);
        }

        /// Starts a loop to send websocket messages queued in the outgoing message queue.
        private async Task StartSending()
        {
            while (this.IsConnected())
            {
                QueueItem item = null;
                if (this.outgoingMessageQueue.TryTake(out item, 100))
                {
                    try
                    {
                        await this.webSocketclient.SendAsync(item.Content, item.OpCode, true, this.cancellationToken);
                        item.CompletionSource.TrySetResult(true);
                    }
                    catch (OperationCanceledException)
                    {
                        item.CompletionSource.TrySetCanceled();
                    }
                    catch (ObjectDisposedException)
                    {
                        item.CompletionSource.TrySetCanceled();
                    }
                    catch (Exception ex)
                    {
                        item.CompletionSource.TrySetException(ex);
                        throw;
                    }
                }
            }
        }

        //Receive loop
        private async Task StartReceiving()
        {
            var buffer = new byte[ReceiveChunkSize];
            var arraySegmentBuffer = new ArraySegment<byte>(buffer);
            Task<WebSocketReceiveResult> receiveTask = null;
            bool disconnecting = false;
            while (this.IsConnected() && !disconnecting)
            {
                if (receiveTask == null)
                {
                    receiveTask = this.webSocketclient.ReceiveAsync(arraySegmentBuffer, this.cancellationToken);
                }
                try
                {
                    if (receiveTask.Wait(100))
                    {
                        WebSocketReceiveResult result = await receiveTask;
                        receiveTask = null;
                        EventHandler<ArraySegment<byte>> handler = null;
                        switch (result.MessageType)
                        {
                            case WebSocketMessageType.Close:
                                disconnecting = true;
                                Errors.Add($"{DateTime.Now} : Disconnecting web socket with status {result.CloseStatus} for the following reason: {result.CloseStatusDescription}");
                                await this.Disconnect();
                                break;
                            case WebSocketMessageType.Binary:
                                handler = result.EndOfMessage ? this.OnEndOfBinaryData : this.OnBinaryData;
                                break;
                            case WebSocketMessageType.Text:
                                handler = result.EndOfMessage ? this.OnEndOfTextData : this.OnTextData;
                                break;
                        }
                        if (handler != null)
                        {
                            var data = new byte[result.Count];
                            Array.Copy(buffer, data, result.Count);
                            handler(this, new ArraySegment<byte>(data));
                        }
                    }
                }
                catch(Exception e)
                {
                    if (!(e.InnerException is TaskCanceledException))
                    {
                        Trace.TraceError("Exception while receiving messages: {0}", e.ToString());
                    }
                }
            }
        }

        public void Dispose()
        {
            if (this.webSocketclient != null)
            {
                webSocketclient.Dispose();
            }
        }

        private void ReportError(Task task)
        {
            if (task.IsFaulted)
            {
                if (this.Failed != null) Failed(this, task.Exception);
            }
        }
    }
}
