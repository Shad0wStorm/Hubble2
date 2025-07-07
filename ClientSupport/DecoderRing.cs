using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ClientSupport
{
    public class DecoderRing
    {
        static byte[] salt = System.Text.Encoding.Unicode.GetBytes("EWhcarwachahdstusotbboeahlntoyitwwvltdovxswtyottdoamaifahicohsmbmohhfavephmhdtlafhthmtairohcahphbsbaewagwhflsoamia");

        public String Decode(String encoded)
        {
            try
            {
                byte[] decryptedData = ProtectedData.Unprotect(
                    Convert.FromBase64String(encoded), salt,
                    DataProtectionScope.CurrentUser);
                return System.Text.Encoding.Unicode.GetString(decryptedData);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public String Encode(String plain)
        {
            byte[] encryptedData = ProtectedData.Protect(
                System.Text.Encoding.Unicode.GetBytes(plain), salt,
                DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        public static String BytesToHex(Byte[] data)
        {
            StringBuilder text = new StringBuilder();
            for (int b = 0; b < data.Length; ++b)
            {
                text.Append(data[b].ToString("x2"));
            }
            return text.ToString();
        }

        public static String HexToString(String source)
        {
            int length = source.Length;
            length = length - (length % 2);
            StringBuilder result = new StringBuilder();
            Byte byteValue;
            for (int start = 0; start < length; start += 2)
            {
                String charString = source.Substring(start, 2);
                if (!Byte.TryParse(charString,System.Globalization.NumberStyles.HexNumber, null, out byteValue))
                {
                    return null;
                }
                result.Append((char)byteValue);
            }
            return result.ToString();
        }

        public String SHA1Encode(String source, int outputCount=0)
        {
            Byte[] input = System.Text.Encoding.ASCII.GetBytes(source);
            Byte[] result;
            using(SHA1 sha = new SHA1CryptoServiceProvider())
            {
                result = sha.ComputeHash(input);
            }

            String hash = BytesToHex(result);
            if (outputCount>0)
            {
                if (hash.Length > outputCount)
                {
                    hash = hash.Substring(0, outputCount);
                }
            }
            return hash;
        }

        public String SHA1EncodeFile(String path, out long length)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return SHA1EncodeStream(fs, out length);
            }
        }

        public String SHA1EncodeStream(Stream data, out long length)
        {
            using (SHA1 sha = new SHA1CryptoServiceProvider())
            {
                byte[] hashValue;

                // Setting blockSizeMB computes the hash on the stream contents
                // letting the underlying library handle memory.
                // Using a value of 0 seems to work best for single threaded
                // processing and always causes fewer page faults.
                // Using a non zero value (1,2,4 tested) increases the number
                // of page faults but does appear to improve performance for
                // multi-threaded processing, though external conditions can
                // easily change the overall runtime by more than the
                // improvement can offer.
                // As it is not possible to demonstrate a clear advantage at
                // this time across all use cases the value is being left at
                // 0 for now to retain previous behaviour, while allowing
                // future testing if required.
                const int blockSizeMB = 0;
                const int blockSize = blockSizeMB * 1024 * 1024;

                length = data.Length;
                if ((length < blockSize) || (blockSize ==0))
                {
                    data.Position = 0;
                    hashValue = sha.ComputeHash(data);
                }
                else
                {
                    byte[] buffer = new byte[blockSize];
                    int processed = 0;
                    int got = 0;
                    while ((length - processed) >= blockSize)
                    {
                        got = data.Read(buffer, 0, blockSize);
                        processed += sha.TransformBlock(buffer, 0, got, buffer, 0);
                    }
                    got = data.Read(buffer, 0, (int)(length - processed));
                    sha.TransformFinalBlock(buffer, 0, (int)(length - processed));
                    hashValue = sha.Hash;
                }

                String hash = BytesToHex(hashValue);

                data.Position = 0;

                return hash;
            }
        }

        public String MD5EncodeFile(String path, out long length)
        {

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return MD5EncodeStream(fs, out length);
            }
        }

        public String MD5EncodeStream(Stream data, out long length)
        {
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                byte[] hashValue;

                length = data.Length;
                data.Position = 0;
                hashValue = md5.ComputeHash(data);

                String hash = BytesToHex(hashValue);

                data.Position = 0;

                return hash;
            }
        }

        public void SHA1Test()
        {
            String dog = SHA1Encode("The quick brown fox jumps over the lazy dog");
            String cog = SHA1Encode("The quick brown fox jumps over the lazy cog");
            String empty = SHA1Encode("");

            if (dog != "2fd4e1c67a2d28fced849ee1bb76e7391b93eb12")
            {
                throw new Exception("Dog does not match expected hash");
            }
            if (cog != "de9f2c7fd25e1b3afad3e85a0bd17d9b100db4b3")
            {
                throw new Exception("Cog does not match expected hash");
            }
            if (empty != "da39a3ee5e6b4b0d3255bfef95601890afd80709")
            {
                throw new Exception("Dog does not match expected hash");
            }
        }
    }
}
