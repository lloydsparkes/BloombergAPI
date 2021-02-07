using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;


namespace Bloomberg.API.HAPI.Decoder
{
    /// <summary>
    /// This file was building using code from:
    ///
    /// https://stackoverflow.com/questions/59770092/how-can-i-decrypt-a-file-in-c-sharp-which-has-been-encrypted-by-des-exe
    /// https://stackoverflow.com/questions/1615252/des-initialization-vector-in-c-sharp (PasswordToKey)
    /// https://gist.github.com/alexforster/3b41ea70ac6fc98bc8c1b5afc093d4af
    ///
    /// This class will provide static methods to turn something like:
    ///
    /// begin 600 test.bbg
    /// M^W$+:8D,Q9^*:+:&lt;HD?W=KXL5.AM&gt;(&amp;":N(?52&amp;FFV@BH36[.P4]+1?Q*Z&gt;3
    /// ........ middle removed for brevity
    /// M^R@] N'\&amp;\L$F5*1OUH90AX;*C^5X8P36^ !Z+&amp;]-?K^)7!"X3FN2,,Y_W"5
    /// '0@K-S"'8T
    /// 
    /// end
    ///
    /// TO:
    ///
    /// START-OF-FILE
    /// RUNDATE=20200716
    /// # Required headers
    /// PROGRAMNAME=getdata
    /// REPLYFILENAME=test.bbg
    /// 
    /// # Scheduling headers
    /// PROGRAMFLAG=adhoc
    /// 
    /// START-OF-FIELDS
    /// ...REMOVED FOR BREVITY
    /// END-OF-FIELDS
    /// 
    /// TIMESTARTED=Thu Jul 16 16:02:58 BST 2020
    /// START-OF-DATA
    /// ...REMOVED FOR BREVITY
    /// END-OF-DATA
    /// TIMEFINISHED=Thu Jul 16 16:03:15 BST 2020
    /// END-OF-FILE
    ///
    /// Assuming we have a string containing the raw data e.g. var blob = "begin 600............\n\nend"
    ///
    /// var decoded = BloombergDecoder.Decrypt(blob, "password")
    /// 
    /// </summary>
    public class BloombergDecoder
    {
        public static Stream DecodeResponse(MemoryStream sourceStream)
        {
            // Decoding attempt
            bool decode = false, decompress = false, decrypt = true;
            // Check first line
            string firstLine = null;
            using (var reader = new StreamReader(sourceStream, Encoding.Default, true, 128, true))
            {
                firstLine = reader.ReadLine();
                sourceStream.Position = 0;
            }

            if (firstLine != null && firstLine.StartsWith("begin 600"))
            {
                decode = true;
                if (firstLine.EndsWith(".gz")) decompress = true;
            }

            // If we need to decode, we need to do other bits too

            if (decode)
            {
                Stream resultStream = null;

                // We are just wrapping these streams up inside of each other to produce a nice chain. 
                resultStream = UUDecode(sourceStream);

                if (decrypt)
                {
                    resultStream = Decrypt(resultStream);
                }

                if (decompress)
                {
                    resultStream = Decompress(resultStream);
                }

                return resultStream;
            }

            return sourceStream;
        }

        public static Stream Decrypt(Stream blobStream, string password = "")
        {
            var secretKey = PasswordToKey(password);
            var iv = new byte[8];

            blobStream.Position = 0;
            
            var algo = new DESCryptoServiceProvider();
            algo.Mode = CipherMode.CBC;
            algo.Padding = PaddingMode.None;

            var stream = new CryptoStream(blobStream, algo.CreateDecryptor(secretKey, iv), CryptoStreamMode.Read);
            return stream;
        }

        public static Stream Decompress(Stream input)
        {
            return new GZipStream(input, CompressionMode.Decompress, true);
        }

        public static Stream UUDecode(Stream input)
        {
            var result = new MemoryStream();

            using (var source = new StreamReader(input, Encoding.ASCII, true, 128, true))
            {
                string ascii;
                while ((ascii = source.ReadLine()) != null)
                {
                    var encodedBuffer = Encoding.ASCII.GetBytes(ascii);

                    if (ascii.StartsWith("begin")) continue; // Skip the lin beginning with begin
                    if (ascii == "end" || encodedBuffer[0] == 0x60 || encodedBuffer.Length == 0 ||
                        encodedBuffer[0] == ' ') break;

                    var nrDecoded = encodedBuffer[0] - 32;
                    var decoded = new byte[nrDecoded];

                    for (int i = 1, j = 0;; i += 4)
                    {
                        var tmp = encodedBuffer.Skip(i).Take(4).Select(b => (byte) ((b - 0x20) & 0x3F)).ToArray();

                        decoded[j++] = (byte) (tmp[0] << 2 | tmp[1] >> 4);
                        if (j == nrDecoded) break;

                        decoded[j++] = (byte) (tmp[1] << 4 | tmp[2] >> 2);
                        if (j == nrDecoded) break;

                        decoded[j++] = (byte) (tmp[2] << 6 | tmp[3]);
                        if (j == nrDecoded) break;
                    }

                    result.Write(decoded, 0, nrDecoded);
                }
            }

            result.Position = 0;
            return result;
        }

        private static readonly byte[] OddParityTable =
        {
            1, 1, 2, 2, 4, 4, 7, 7, 8, 8, 11, 11, 13, 13, 14, 14,
            16, 16, 19, 19, 21, 21, 22, 22, 25, 25, 26, 26, 28, 28, 31, 31,
            32, 32, 35, 35, 37, 37, 38, 38, 41, 41, 42, 42, 44, 44, 47, 47,
            49, 49, 50, 50, 52, 52, 55, 55, 56, 56, 59, 59, 61, 61, 62, 62,
            64, 64, 67, 67, 69, 69, 70, 70, 73, 73, 74, 74, 76, 76, 79, 79,
            81, 81, 82, 82, 84, 84, 87, 87, 88, 88, 91, 91, 93, 93, 94, 94,
            97, 97, 98, 98, 100, 100, 103, 103, 104, 104, 107, 107, 109, 109, 110, 110,
            112, 112, 115, 115, 117, 117, 118, 118, 121, 121, 122, 122, 124, 124, 127, 127,
            128, 128, 131, 131, 133, 133, 134, 134, 137, 137, 138, 138, 140, 140, 143, 143,
            145, 145, 146, 146, 148, 148, 151, 151, 152, 152, 155, 155, 157, 157, 158, 158,
            161, 161, 162, 162, 164, 164, 167, 167, 168, 168, 171, 171, 173, 173, 174, 174,
            176, 176, 179, 179, 181, 181, 182, 182, 185, 185, 186, 186, 188, 188, 191, 191,
            193, 193, 194, 194, 196, 196, 199, 199, 200, 200, 203, 203, 205, 205, 206, 206,
            208, 208, 211, 211, 213, 213, 214, 214, 217, 217, 218, 218, 220, 220, 223, 223,
            224, 224, 227, 227, 229, 229, 230, 230, 233, 233, 234, 234, 236, 236, 239, 239,
            241, 241, 242, 242, 244, 244, 247, 247, 248, 248, 251, 251, 253, 253, 254, 254
        };

        public static byte[] PasswordToKey(string password)
        {
            if (password == null)
                throw new ArgumentNullException("password");
            if (password == "")
                throw new ArgumentException("password");

            var key = new byte[8];

            for (var i = 0; i < password.Length; i++)
            {
                var c = (int) password[i];
                if (i % 16 < 8)
                {
                    key[i % 8] ^= (byte) (c << 1);
                }
                else
                {
                    // reverse bits e.g. 11010010 -> 01001011
                    c = ((c << 4) & 0xf0) | ((c >> 4) & 0x0f);
                    c = ((c << 2) & 0xcc) | ((c >> 2) & 0x33);
                    c = ((c << 1) & 0xaa) | ((c >> 1) & 0x55);
                    key[7 - i % 8] ^= (byte) c;
                }
            }

            AddOddParity(key);

            var target = new byte[8];
            var passwordBuffer =
                Encoding.ASCII.GetBytes(password).Concat(new byte[8])
                    .Take(password.Length + (8 - password.Length % 8) % 8).ToArray();

            var des = DES.Create();
            var encryptor = des.CreateEncryptor(key, key);
            for (var x = 0; x < passwordBuffer.Length / 8; ++x)
                encryptor.TransformBlock(passwordBuffer, 8 * x, 8, target, 0);

            AddOddParity(target);

            return target;
        }


        private static void AddOddParity(byte[] buffer)
        {
            for (var i = 0; i < buffer.Length; ++i) buffer[i] = OddParityTable[buffer[i]];
        }
    }
}
