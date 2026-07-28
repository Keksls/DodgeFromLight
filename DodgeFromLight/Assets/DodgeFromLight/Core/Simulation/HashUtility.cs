using System.Security.Cryptography;
using System.Text;

namespace DodgeFromLight.Core.Simulation
{
    internal static class HashUtility
    {
        /// <summary>
        /// Computes a lowercase SHA-256 hash for canonical text.
        /// </summary>
        internal static string ComputeSha256(string canonicalText)
        {
            // Hash UTF-8 bytes so results are stable across supported platforms.
            using (var algorithm = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(canonicalText ?? string.Empty);
                return ToHex(algorithm.ComputeHash(bytes));
            }
        }

        /// <summary>
        /// Converts binary hash bytes to a stable lowercase hexadecimal string.
        /// </summary>
        private static string ToHex(byte[] bytes)
        {
            // Avoid culture-sensitive formatting in replay and content hashes.
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var value in bytes)
                builder.Append(value.ToString("x2"));
            return builder.ToString();
        }
    }
}
