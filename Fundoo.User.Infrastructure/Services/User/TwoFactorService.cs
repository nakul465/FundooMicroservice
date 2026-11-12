using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Fundoo.User.Application.Services;

namespace Fundoo.User.Infrastructure.Services.User
{
    public class TwoFactorService : ITwoFactorService
    {
        private static readonly char[] Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();
        private static readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> EmailOtpCache = new();

        public string GenerateSecretKey()
        {
            var bytes = new byte[20]; // 160-bit key recommended by RFC 6238
            RandomNumberGenerator.Fill(bytes);
            return ToBase32String(bytes);
        }

        public string GenerateQrCodeUri(string email, string secret)
        {
            var issuer = "Fundoo";
            return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
        }

        public bool ValidateTotpCode(string secret, string code)
        {
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code) || code.Length != 6)
            {
                return false;
            }

            try
            {
                var secretBytes = FromBase32String(secret);
                var currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var currentStep = currentUnixTime / 30;

                // Check window: previous (-1), current (0), next (+1) step for clock drift
                for (long i = -1; i <= 1; i++)
                {
                    var expectedCode = ComputeTotp(secretBytes, currentStep + i);
                    if (expectedCode == code)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public string GenerateEmailOtp(string email)
        {
            var randomCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            EmailOtpCache[email.ToLowerInvariant()] = (randomCode, DateTime.UtcNow.AddMinutes(5));
            return randomCode;
        }

        public bool ValidateEmailOtp(string email, string code)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var key = email.ToLowerInvariant();
            if (EmailOtpCache.TryGetValue(key, out var cached))
            {
                if (DateTime.UtcNow <= cached.Expiry && cached.Code == code.Trim())
                {
                    EmailOtpCache.TryRemove(key, out _);
                    return true;
                }
            }

            return false;
        }

        private static string ComputeTotp(byte[] secretBytes, long step)
        {
            var counterBytes = BitConverter.GetBytes(step);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counterBytes);
            }

            using var hmac = new HMACSHA1(secretBytes);
            var hash = hmac.ComputeHash(counterBytes);

            int offset = hash[^1] & 0x0F;
            int binaryCode = ((hash[offset] & 0x7F) << 24)
                           | ((hash[offset + 1] & 0xFF) << 16)
                           | ((hash[offset + 2] & 0xFF) << 8)
                           | (hash[offset + 3] & 0xFF);

            int otp = binaryCode % 1_000_000;
            return otp.ToString("D6");
        }

        private static string ToBase32String(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;

            var sb = new StringBuilder();
            int bitBuffer = 0;
            int bitCount = 0;

            foreach (var b in bytes)
            {
                bitBuffer = (bitBuffer << 8) | b;
                bitCount += 8;

                while (bitCount >= 5)
                {
                    bitCount -= 5;
                    int index = (bitBuffer >> bitCount) & 0x1F;
                    sb.Append(Base32Chars[index]);
                }
            }

            if (bitCount > 0)
            {
                int index = (bitBuffer << (5 - bitCount)) & 0x1F;
                sb.Append(Base32Chars[index]);
            }

            return sb.ToString();
        }

        private static byte[] FromBase32String(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return Array.Empty<byte>();

            var clean = input.Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "").TrimEnd('=');
            var result = new System.Collections.Generic.List<byte>();
            int bitBuffer = 0;
            int bitCount = 0;

            foreach (char c in clean)
            {
                int val = Array.IndexOf(Base32Chars, c);
                if (val < 0) continue;

                bitBuffer = (bitBuffer << 5) | val;
                bitCount += 5;

                if (bitCount >= 8)
                {
                    bitCount -= 8;
                    result.Add((byte)((bitBuffer >> bitCount) & 0xFF));
                }
            }

            return result.ToArray();
        }
    }
}
