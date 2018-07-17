using ChatBot.Models;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatBot.Services
{
    public class TextToSpeechService
    {
        private const string CacheKey = "1";

        private readonly string _voiceFontUri;
        private readonly string _voiceFontName;

        public TextToSpeechService(IOptions<MySettings> config)
        {
            _voiceFontUri = config.Value.VoiceFontUri;
            _voiceFontName = config.Value.VoiceFontName;
        }

        public string GenerateSsml(string message, bool useCustomVoiceFont = false)
        {
            // TODO: If web socket works for Custom Voice, simplify the TextToSpeechRequest class
            var options = useCustomVoiceFont
                ? new TextToSpeechRequest.VoiceFontInputOptions(_voiceFontUri, _voiceFontName, message, null) as TextToSpeechRequest.InputOptions
                : new TextToSpeechRequest.BingInputOptions(BotConstants.EnglishLanguage, TextToSpeechRequest.BingInputOptions.GenderFemale, message, null);
            return options.GenerateSsml();
        }

        internal static string ByteToHexBit(byte[] bytes)
        {
            // Based on https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/14333437#14333437
            var c = new char[bytes.Length * 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = bytes[i] & 0xF;
                c[(i * 2) + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }

            return new string(c);
        }

        private string GenerateHash(string message, string locale, params string[] others)
        {
            var sha = new SHA256Managed();
            var othersCombined = string.Join("__", others);
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes($"{CacheKey}_{locale}__{message}__{othersCombined}"));
            return ByteToHexBit(hash);
        }
    }
}
