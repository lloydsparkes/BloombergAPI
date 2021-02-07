/// <summary>
/// Copyright 2019. Bloomberg Finance L.P. Permission is hereby granted, free of
/// charge, to any person obtaining a copy of this software and associated
/// documentation files (the "Software"), to deal in the Software without
/// restriction, including without limitation the rights to use, copy, modify,
/// merge, publish, distribute, sublicense, and/or sell copies of the Software,
/// and to permit persons to whom the Software is furnished to do so, subject to
/// the following conditions: The above copyright notice and this permission
/// notice shall be included in all copies or substantial portions of the
/// Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE.
/// </summary>

using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

// This file was originally copied from the Bloomberg HAPI Samples
// S:\Technology\Development\Bloomberg\HAPI - SampleConnect\BeapAuth.cs

namespace Bloomberg.API.HAPI.Authentication
{
    /// <summary>
    /// The following class represents BEAP credentials and provides method for
    /// loading BEAP credentials from file (typically called credential.txt).
    /// credential.txt is generated for each account and needed to authorize on
    /// BEAP HTTP service.
    /// Note: Please obtain credential.txt file first before launching any sample.
    /// </summary>
    public class Credential
    {
        // accout region, use 'default' value unless being instructed otherwise
        private const string DRegion = "default";
        // Once created each token will be valid only for the following specified period(in seconds).
        // This value is ajustable on client side, but note that increasing this value too much 
        // might not suffice for security concerns, as well as too little value could lead to 
        // JWT token invalidation before it being received by the beap server due to network delays.
        // You can adjust this value for your need or use default value unless you definitely know 
        // that you need to change this value.
        private const int DLifetime = 25;
        /// <summary>
        /// Provides access to client id parsed from credential.txt file.
        /// </summary>
        [JsonProperty("client_id")]
        public string ClientId { get; set; }
        /// <summary>
        /// Accepts client secret form json deserializer and decodes it to bytes.
        /// </summary>
        [JsonProperty("client_secret")]
        public string ClientSecret { set { DecodedSecret = FromHexString(value); } }
        /// <summary>
        /// Provides access to decoded client secret.
        /// </summary>
        public byte[] DecodedSecret { get; private set; }
        /// <summary>
        /// Converts hexadecimal string to bytes.
        /// </summary>
        /// <param name="s">Input hexadecimal string.</param>
        /// <returns></returns>
        static private byte[] FromHexString(string input)
        {
            return Enumerable.Range(0, input.Length)
                .Where(charIdx => charIdx % 2 == 0)
                .Select(charIdx => Convert.ToByte(input.Substring(charIdx, 2), 16))
                .ToArray();
        }
        /// <summary>
        /// Loads credentials from credential.txt file.
        /// </summary>
        /// <param name="dCredentialPath">Path to credential.txt file.</param>
        /// <returns></returns>
        public static Credential LoadCredential(string dCredentialPath = "credential.txt")
        {
            try
            {
                using StreamReader fileInput = new StreamReader(dCredentialPath);
                using var jsonInput = new JsonTextReader(fileInput);
                var clientCredential = new JsonSerializer().Deserialize<Credential>(jsonInput);
                string clientSecret = Convert.ToBase64String(clientCredential.DecodedSecret);

                Console.WriteLine($"client id: {clientCredential.ClientId}");
                Console.WriteLine($"client secret (base64 encoded): {clientSecret}");
                Console.WriteLine();

                return clientCredential;
            }
            catch (JsonReaderException error)
            {
                Console.Error.WriteLine($"Cannot read credential file, probably not in JSON format: {error.Message}");
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Cannot access credential file, check file permissions.");
            }
            catch (ArgumentException error)
            {
                Console.Error.WriteLine($"{error.Message}");
            }
            catch (IOException error)
            {
                Console.Error.WriteLine($"Cannot open credential file \nerror description: {error.Message}");
            }
            Environment.Exit(-1);
            return null;
        }

        /// <summary>
        /// Creates new JWT token for the given input request parameters.
        /// </summary>
        /// <param name="host">the beap host being accessed.</param>
        /// <param name="path">URI path of the accessed endpoint.</param>
        /// <param name="method">HTTP method used to access the endpoint.</param>
        /// <returns></returns>
        internal string CreateToken(string host, string path, string method)
        {
            string guid = Guid.NewGuid().ToString();
            // Get unix timestamp
            long issueTime = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            // Create key for JWT signature
            SymmetricSecurityKey securityKey = new SymmetricSecurityKey(DecodedSecret);
            // Define JWT signing key and algorythm
            SigningCredentials signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            // Create JWT header container object
            var header = new JwtHeader(signingCredentials);
            // Create JWT payload container object
            var payload = new JwtPayload
            {
                { JwtRegisteredClaimNames.Iss, ClientId },
                { JwtRegisteredClaimNames.Iat, issueTime },
                { JwtRegisteredClaimNames.Nbf, issueTime },
                { JwtRegisteredClaimNames.Exp, issueTime + DLifetime },
                { "host", host },
                { "path", path },
                { "region", DRegion },
                { "jti", guid },
                { "method", method },
                { "client_id", ClientId }
            };
            // Create JWT token object
            JwtSecurityToken jwtToken = new JwtSecurityToken(header, payload);
            // Serialize JWT token object to base64 encoded string
            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }
    }
}