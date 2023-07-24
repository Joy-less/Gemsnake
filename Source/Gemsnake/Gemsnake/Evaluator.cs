#define DYNAMIC_KEYWORD_SUPPORTED // If this line is commented out, the object keyword will be used rather than the dynamic keyword.

using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace Gemsnake
{
    public abstract class Evaluator {
        public const string Author = "Joyless";
        public const string Version = "1.0.1";

        protected const float ReadFrequency = 60f; // How many times per second the client and server should read the network stream for new messages
        protected readonly static Encoding MessageEncoding = Encoding.UTF8; // The text encoding of the messages

        protected const int BufferSize = 8192; // The maximum number of bytes per ReadAsync; uses memory but more performant for long messages
        protected const byte EndOfLengthByte = 255; // Which byte should be reserved to mark the end of the message length (LengthBytes EndOfLengthByte MessageBytes)

        // const Type DynamicType = typeof(dynamic);

        protected TcpClient? Client;
        protected NetworkStream? Stream;
        protected Process? ExecutableProcess;

        protected readonly List<string> Responses = new();

        public void WaitForConnection() {
            WaitForConnectionAsync().Wait();
        }
        public async Task WaitForConnectionAsync() {
            while (Stream == null) {
                await Task.Delay(15);
            }
        }
        public class EvaluationResult {
            public readonly string? ErrorMessage;
            public readonly long? ErrorLine;
#if DYNAMIC_KEYWORD_SUPPORTED
            public readonly dynamic? ReturnValue;
            public EvaluationResult(string? ErrorMessage, long? ErrorLine, dynamic? ReturnValue) {
#else
            public readonly object? ReturnValue;
            public EvaluationResult(string? ErrorMessage, long? ErrorLine, object? ReturnValue) {
#endif
                this.ErrorMessage = ErrorMessage;
                this.ErrorLine = ErrorLine;
                this.ReturnValue = ReturnValue;
            }
            public string FormatError() {
                return "Error on line " + (ErrorLine.ToString() ?? "?") + ": " + ErrorMessage;
            }
        }
        public EvaluationResult Evaluate(string Code) {
            return EvaluateAsync(Code).Result;
        }
        public async Task<EvaluationResult> EvaluateAsync(string Code) {
            Guid EvaluateGuid = Guid.NewGuid();
            string EvaluateGuidPrefix = EvaluateGuid + ":";
            await SendToServer(EvaluateGuidPrefix + Code);
            while (true) {
                lock (Responses) {
                    int FindResponse = Responses.FindIndex(x => x.StartsWith(EvaluateGuidPrefix));
                    if (FindResponse != -1) {
                        // Found response
                        string Response = Responses[FindResponse];
                        Responses.RemoveAt(FindResponse);
                        Response = Response[EvaluateGuidPrefix.Length..];
                        // Return the response
                        const string BadFormatException = "Response was not in the correct format";
#if DYNAMIC_KEYWORD_SUPPORTED
                        Dictionary<string, dynamic?> RawResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic?>>(Response) ?? throw new Exception(BadFormatException);
#else
                        Dictionary<string, object?> RawResponse = JsonConvert.DeserializeObject<Dictionary<string, object?>>(Response) ?? throw new Exception(BadFormatException);
#endif
                        return new EvaluationResult(
                            (string?)RawResponse["error_message"],
                            (long?)RawResponse["error_line"],
                            RawResponse["result"]
                        );
                    }
                }
                await Task.Delay(15);
            }
        }
        /// <summary>Called automatically when the evaluator is no longer referenced.</summary>
        public virtual void Stop() {
            ExecutableProcess?.Kill();
            Stream?.Close();
            Stream = null;
            Client?.Close();
            Client = null;
        }

        protected static Process RunProcess(string ProcessPath, params string[] Arguments) {
            // Run the process with arguments
            Process Process = new() {
                StartInfo = new ProcessStartInfo() {
                    FileName = ProcessPath
                }
            };
            foreach (string Argument in Arguments) {
                Process.StartInfo.ArgumentList.Add(Argument);
            }
            Process.Start();

            return Process;
        }
        protected static int SecondsToMilliseconds(float Seconds) {
            return (int)(Seconds * 1000);
        }
        protected static double GetUnixTimeStamp() {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d;
        }
        protected static byte[] CreatePacket(string Message) {
            // Get message as bytes
            byte[] Bytes = MessageEncoding.GetBytes(Message);
            // Insert message length bytes
            {
                List<byte> MessageLengthBytes = new();
                // Add the digits of the message length as bytes (e.g 670 becomes {67, 0})
                string ByteCount = Bytes.Length.ToString();
                string ByteCountDigits = "";
                foreach (char ByteCountDigit in ByteCount) {
                    if (byte.TryParse(ByteCountDigits + ByteCountDigit, out byte Result) && Result != EndOfLengthByte
                        && Result.ToString().Length == ByteCountDigits.Length + 1) { // Ensure no zeros have been omitted
                    }
                    else {
                        MessageLengthBytes.Add(byte.Parse(ByteCountDigits));
                        ByteCountDigits = "";
                    }
                    ByteCountDigits += ByteCountDigit;
                }
                if (ByteCountDigits.Length != 0) MessageLengthBytes.Add(byte.Parse(ByteCountDigits));
                // Add the end of message length byte
                MessageLengthBytes.Add(EndOfLengthByte);
                // Build the packet
                MessageLengthBytes.AddRange(Bytes);
                Bytes = MessageLengthBytes.ToArray();
            }
            return Bytes;
        }
        protected async void Listen(int Port) {
            // Initialise variables
            while (Client == null) {
                try {
                    Client = new TcpClient("localhost", Port);
                }
                catch {
                }
            }
            Stream = Client.GetStream();

            List<byte> CurrentBytes = new();
            byte[] Buffer = new byte[BufferSize];

            // Read messages while connected
            while (true) {
                // Read all the available data
                while (Stream.DataAvailable) {
                    // Add the data to CurrentBytes
                    int BytesRead = await Stream.ReadAsync(Buffer);
                    CurrentBytes.AddRange(Buffer.ToList().GetRange(0, BytesRead));
                }
                // Check for a complete message in the data
                while (CurrentBytes.Count > 0) {
                    bool GotCompletedMessage = false;
                    for (int i = 0; i < CurrentBytes.Count; i++) {
                        // Find end of message length byte
                        if (CurrentBytes[i] == EndOfLengthByte) {
                            // Get the message length
                            int MessageLength = int.Parse(string.Concat(CurrentBytes.GetRange(0, i)));
                            // Check if the message is complete
                            if (CurrentBytes.Count - (i + 1) >= MessageLength) {
                                // Take the message
                                byte[] Message = CurrentBytes.GetRange(i + 1, MessageLength).ToArray();
                                CurrentBytes.RemoveRange(0, (i + 1) + MessageLength);
                                // Handle the message
                                lock (Responses) {
                                    Responses.Add(MessageEncoding.GetString(Message));
                                }
                                // Mark got completed message as true
                                GotCompletedMessage = true;
                            }
                            // Break (the end message length byte has been reached)
                            break;
                        }
                    }
                    if (GotCompletedMessage == false) break;
                }
                // Wait until the next read
                await Task.Delay(SecondsToMilliseconds(1 / ReadFrequency));
            }
        }
        protected async Task SendToServer(string Message) {
            await WaitForConnectionAsync();
            if (Stream == null) throw new NullReferenceException();
            // Send bytes
            await Stream.WriteAsync(CreatePacket(Message)).AsTask();
        }

        ~Evaluator() {
            Stop();
        }
    }
}
