using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using SecureBackup.Models;

namespace SecureBackup.Services
{
    public class EncryptionService
    {
        private readonly AwsService _awsService;

        // Constants for encryption
        private const int AES_KEY_SIZE = 32; // 256 bits
        private const int TWOFISH_KEY_SIZE = 32; // 256 bits
        private const int SERPENT_KEY_SIZE = 32; // 256 bits
        private const int IV_SIZE = 16; // 128 bits
        private const int GCM_TAG_SIZE = 16; // 128 bits

        public EncryptionService(AwsService awsService)
        {
            _awsService = awsService;
        }

        /// <summary>
        /// Encrypts a file using cascade encryption (AES-256 -> Twofish -> Serpent)
        /// </summary>
        /// <param name="sourceFilePath">Path to the file to encrypt</param>
        /// <param name="outputFilePath">Path to save the encrypted file</param>
        /// <returns>Metadata about the encrypted file</returns>
        public async Task<EncryptionResult> EncryptFileAsync(string sourceFilePath, string outputFilePath)
        {
            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("Source file not found", sourceFilePath);
            }

            // Generate encryption keys
            var aesKey = GenerateRandomBytes(AES_KEY_SIZE);
            var twofishKey = GenerateRandomBytes(TWOFISH_KEY_SIZE);
            var serpentKey = GenerateRandomBytes(SERPENT_KEY_SIZE);
            
            // Generate initialization vectors
            var aesIv = GenerateRandomBytes(IV_SIZE);
            var twofishIv = GenerateRandomBytes(IV_SIZE);
            var serpentIv = GenerateRandomBytes(IV_SIZE);

            using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            using (var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
            {
                // Write metadata (IVs) to the beginning of the file
                outputStream.Write(aesIv, 0, aesIv.Length);
                outputStream.Write(twofishIv, 0, twofishIv.Length);
                outputStream.Write(serpentIv, 0, serpentIv.Length);

                // Apply cascade encryption
                using (var aesEncrypted = new MemoryStream())
                {
                    // First layer: AES-256
                    EncryptWithAes(sourceStream, aesEncrypted, aesKey, aesIv);
                    aesEncrypted.Position = 0;
                    
                    using (var twofishEncrypted = new MemoryStream())
                    {
                        // Second layer: Twofish
                        EncryptWithTwofish(aesEncrypted, twofishEncrypted, twofishKey, twofishIv);
                        twofishEncrypted.Position = 0;
                        
                        // Third layer: Serpent
                        EncryptWithSerpent(twofishEncrypted, outputStream, serpentKey, serpentIv);
                    }
                }
            }

            // Store encryption keys in AWS KMS
            var keyId = await _awsService.StoreEncryptionKeysAsync(
                Path.GetFileName(sourceFilePath),
                aesKey, 
                twofishKey, 
                serpentKey, 
                aesIv, 
                twofishIv, 
                serpentIv);

            return new EncryptionResult
            {
                SourceFilePath = sourceFilePath,
                EncryptedFilePath = outputFilePath,
                KeyId = keyId
            };
        }

        /// <summary>
        /// Decrypts a file that was encrypted using cascade encryption
        /// </summary>
        /// <param name="encryptedFilePath">Path to the encrypted file</param>
        /// <param name="outputFilePath">Path to save the decrypted file</param>
        /// <param name="keyId">The KMS key ID for retrieving encryption keys</param>
        public async Task DecryptFileAsync(string encryptedFilePath, string outputFilePath, string keyId)
        {
            if (!File.Exists(encryptedFilePath))
            {
                throw new FileNotFoundException("Encrypted file not found", encryptedFilePath);
            }

            // Retrieve encryption keys from AWS KMS
            var keys = await _awsService.RetrieveEncryptionKeysAsync(keyId);

            using (var encryptedStream = new FileStream(encryptedFilePath, FileMode.Open, FileAccess.Read))
            using (var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
            {
                // Read the IVs from the beginning of the file
                byte[] aesIv = new byte[IV_SIZE];
                byte[] twofishIv = new byte[IV_SIZE];
                byte[] serpentIv = new byte[IV_SIZE];

                encryptedStream.Read(aesIv, 0, aesIv.Length);
                encryptedStream.Read(twofishIv, 0, twofishIv.Length);
                encryptedStream.Read(serpentIv, 0, serpentIv.Length);

                // Decrypt in reverse order
                using (var serpentDecrypted = new MemoryStream())
                {
                    // Unwrap Serpent
                    DecryptWithSerpent(encryptedStream, serpentDecrypted, keys.SerpentKey, serpentIv);
                    serpentDecrypted.Position = 0;
                    
                    using (var twofishDecrypted = new MemoryStream())
                    {
                        // Unwrap Twofish
                        DecryptWithTwofish(serpentDecrypted, twofishDecrypted, keys.TwofishKey, twofishIv);
                        twofishDecrypted.Position = 0;
                        
                        // Unwrap AES
                        DecryptWithAes(twofishDecrypted, outputStream, keys.AesKey, aesIv);
                    }
                }
            }
        }

        #region AES Encryption/Decryption

        private void EncryptWithAes(Stream inputStream, Stream outputStream, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write, leaveOpen: true))
                {
                    inputStream.CopyTo(cryptoStream);
                }
            }
        }

        private void DecryptWithAes(Stream inputStream, Stream outputStream, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read))
                {
                    cryptoStream.CopyTo(outputStream);
                }
            }
        }

        #endregion

        #region Twofish Encryption/Decryption

        private void EncryptWithTwofish(Stream inputStream, Stream outputStream, byte[] key, byte[] iv)
        {
            // Using BouncyCastle for Twofish
            var cipher = new CbcBlockCipher(new TwofishEngine());
            var parameters = new ParametersWithIV(new KeyParameter(key), iv);
            cipher.Init(true, parameters);

            ProcessWithBlockCipher(inputStream, outputStream, cipher);
        }

        private void DecryptWithTwofish(Stream inputStream, Stream outputStream, byte[] key, byte[] iv)
        {
            var cipher = new CbcBlockCipher(new TwofishEngine());
            var parameters = new ParametersWithIV(new KeyParameter(key), iv);
            cipher.Init(false, parameters);

            ProcessWithBlockCipher(inputStream, outputStream, cipher);
        }

        #endregion

        #region Serpent Encryption/Decryption

        private void EncryptWithSerpent(Stream inputStream, Stream outputStream, byte[] key, byte[] iv)
        {
            // Using BouncyCastle for Serpent
            var cipher = new CbcBlockCipher(new SerpentEngine());
            var parameters = new ParametersWithIV(new KeyParameter(key), iv);
            cipher.Init(true, parameters);

            ProcessWithBlockCipher(inputStream, outputStream, cipher);
        }

        private void DecryptWithSerpent(Stream inputStream, Stream outputStream, byte[] key, byte[] iv)
        {
            var cipher = new CbcBlockCipher(new SerpentEngine());
            var parameters = new ParametersWithIV(new KeyParameter(key), iv);
            cipher.Init(false, parameters);

            ProcessWithBlockCipher(inputStream, outputStream, cipher);
        }

        #endregion

        #region Helper Methods

        private void ProcessWithBlockCipher(Stream inputStream, Stream outputStream, CbcBlockCipher cipher)
        {
            int blockSize = cipher.GetBlockSize();
            byte[] buffer = new byte[blockSize];
            byte[] outputBuffer = new byte[blockSize];
            
            // Process all complete blocks
            int bytesRead;
            while ((bytesRead = inputStream.Read(buffer, 0, blockSize)) == blockSize)
            {
                cipher.ProcessBlock(buffer, 0, outputBuffer, 0);
                outputStream.Write(outputBuffer, 0, blockSize);
            }
            
            // Process the final block with padding
            if (bytesRead > 0)
            {
                // Implement PKCS7 padding
                byte[] paddedBlock = new byte[blockSize];
                Array.Copy(buffer, 0, paddedBlock, 0, bytesRead);
                byte padValue = (byte)(blockSize - bytesRead);
                for (int i = bytesRead; i < blockSize; i++)
                {
                    paddedBlock[i] = padValue;
                }
                
                cipher.ProcessBlock(paddedBlock, 0, outputBuffer, 0);
                outputStream.Write(outputBuffer, 0, blockSize);
            }
            else
            {
                // If the input is an exact multiple of the block size, add a padding block
                byte[] paddingBlock = new byte[blockSize];
                for (int i = 0; i < blockSize; i++)
                {
                    paddingBlock[i] = (byte)blockSize;
                }
                
                cipher.ProcessBlock(paddingBlock, 0, outputBuffer, 0);
                outputStream.Write(outputBuffer, 0, blockSize);
            }
        }

        private byte[] GenerateRandomBytes(int size)
        {
            byte[] randomBytes = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        #endregion
    }
    
    public class EncryptionResult
    {
        public string SourceFilePath { get; set; }
        public string EncryptedFilePath { get; set; }
        public string KeyId { get; set; }
    }
}
