using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SecureBackup.Services
{
    /// <summary>
    /// Provides encryption and decryption services using cascade encryption with AES-256, Twofish, and Serpent.
    /// </summary>
    public class EncryptionService
    {
        private const int KeySize = 32; // 256 bits
        private const int IvSize = 16;  // 128 bits

        /// <summary>
        /// Encrypts a file using cascade encryption (AES-256 -> Twofish -> Serpent)
        /// </summary>
        /// <param name="inputFilePath">Path to the file to encrypt</param>
        /// <param name="outputFilePath">Path where the encrypted file will be saved</param>
        /// <returns>A dictionary containing all encryption keys and IVs</returns>
        public async Task<Dictionary<string, byte[]>> EncryptFileAsync(string inputFilePath, string outputFilePath)
        {
            // Generate random keys and IVs for each algorithm
            var aesKey = GenerateRandomBytes(KeySize);
            var twofishKey = GenerateRandomBytes(KeySize);
            var serpentKey = GenerateRandomBytes(KeySize);

            var aesIv = GenerateRandomBytes(IvSize);
            var twofishIv = GenerateRandomBytes(IvSize);
            var serpentIv = GenerateRandomBytes(IvSize);

            // Read file content
            byte[] fileContent = await File.ReadAllBytesAsync(inputFilePath);

            // Step 1: Encrypt with AES-256
            byte[] aesEncrypted = EncryptAes(fileContent, aesKey, aesIv);

            // Step 2: Encrypt the AES result with Twofish
            byte[] twofishEncrypted = EncryptTwofish(aesEncrypted, twofishKey, twofishIv);

            // Step 3: Encrypt the Twofish result with Serpent
            byte[] serpentEncrypted = EncryptSerpent(twofishEncrypted, serpentKey, serpentIv);

            // Write the IVs and the encrypted content to the output file
            using (var outputStream = new FileStream(outputFilePath, FileMode.Create))
            {
                // Write IVs first so they can be retrieved during decryption
                await outputStream.WriteAsync(aesIv, 0, aesIv.Length);
                await outputStream.WriteAsync(twofishIv, 0, twofishIv.Length);
                await outputStream.WriteAsync(serpentIv, 0, serpentIv.Length);
                
                // Write encrypted content
                await outputStream.WriteAsync(serpentEncrypted, 0, serpentEncrypted.Length);
            }

            // Return the keys and IVs for storage in KMS
            return new Dictionary<string, byte[]>
            {
                { "AesKey", aesKey },
                { "TwofishKey", twofishKey },
                { "SerpentKey", serpentKey },
                { "AesIv", aesIv },
                { "TwofishIv", twofishIv },
                { "SerpentIv", serpentIv }
            };
        }

        /// <summary>
        /// Decrypts a file that was encrypted using cascade encryption (AES-256 -> Twofish -> Serpent)
        /// </summary>
        /// <param name="inputFilePath">Path to the encrypted file</param>
        /// <param name="outputFilePath">Path where the decrypted file will be saved</param>
        /// <param name="keys">Dictionary containing all encryption keys and IVs</param>
        public async Task DecryptFileAsync(string inputFilePath, string outputFilePath, Dictionary<string, byte[]> keys)
        {
            // Read the encrypted file
            byte[] encryptedContent = await File.ReadAllBytesAsync(inputFilePath);

            // Extract IVs from the beginning of the file
            int position = 0;
            byte[] aesIv = encryptedContent.Skip(position).Take(IvSize).ToArray();
            position += IvSize;

            byte[] twofishIv = encryptedContent.Skip(position).Take(IvSize).ToArray();
            position += IvSize;

            byte[] serpentIv = encryptedContent.Skip(position).Take(IvSize).ToArray();
            position += IvSize;

            // Extract the encrypted content (after the IVs)
            byte[] encrypted = encryptedContent.Skip(position).ToArray();

            // Decryption in reverse order
            
            // Step 1: Decrypt Serpent layer
            byte[] serpentDecrypted = DecryptSerpent(encrypted, keys["SerpentKey"], serpentIv);

            // Step 2: Decrypt Twofish layer
            byte[] twofishDecrypted = DecryptTwofish(serpentDecrypted, keys["TwofishKey"], twofishIv);

            // Step 3: Decrypt AES layer
            byte[] aesDecrypted = DecryptAes(twofishDecrypted, keys["AesKey"], aesIv);

            // Write decrypted content to output file
            await File.WriteAllBytesAsync(outputFilePath, aesDecrypted);
        }

        /// <summary>
        /// Generates random bytes for keys and IVs
        /// </summary>
        /// <param name="length">The number of bytes to generate</param>
        /// <returns>Random bytes</returns>
        private byte[] GenerateRandomBytes(int length)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] randomBytes = new byte[length];
                rng.GetBytes(randomBytes);
                return randomBytes;
            }
        }

        #region AES-256 Implementation

        private byte[] EncryptAes(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        private byte[] DecryptAes(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        #endregion

        #region Twofish Implementation
        
        // Note: In a real-world scenario, we would use a proper Twofish implementation
        // For this example, we'll use a placeholder that simulates Twofish with AES
        // In a production environment, you would use a library like BouncyCastle
        private byte[] EncryptTwofish(byte[] data, byte[] key, byte[] iv)
        {
            // Simulating Twofish with AES for the demonstration
            // In a real implementation, replace with actual Twofish
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        private byte[] DecryptTwofish(byte[] data, byte[] key, byte[] iv)
        {
            // Simulating Twofish with AES for the demonstration
            // In a real implementation, replace with actual Twofish
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        #endregion

        #region Serpent Implementation

        // Note: Similar to Twofish, in a real-world scenario, we would use a proper Serpent implementation
        // For this example, we'll use a placeholder that simulates Serpent with AES
        private byte[] EncryptSerpent(byte[] data, byte[] key, byte[] iv)
        {
            // Simulating Serpent with AES for the demonstration
            // In a real implementation, replace with actual Serpent
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        private byte[] DecryptSerpent(byte[] data, byte[] key, byte[] iv)
        {
            // Simulating Serpent with AES for the demonstration
            // In a real implementation, replace with actual Serpent
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        #endregion
    }
}