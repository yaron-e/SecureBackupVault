using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;

namespace SecureBackup.Services
{
    /// <summary>
    /// Provides services for interacting with AWS S3 and KMS
    /// </summary>
    public class AwsService
    {
        private readonly string _bucketName;
        private readonly string _kmsKeyId;
        private readonly AmazonS3Client _s3Client;
        private readonly AmazonKeyManagementServiceClient _kmsClient;

        public AwsService(string accessKey, string secretKey, string region, string bucketName, string kmsKeyId)
        {
            _bucketName = bucketName;
            _kmsKeyId = kmsKeyId;

            // Initialize AWS clients
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            _s3Client = new AmazonS3Client(accessKey, secretKey, regionEndpoint);
            _kmsClient = new AmazonKeyManagementServiceClient(accessKey, secretKey, regionEndpoint);
        }

        /// <summary>
        /// Uploads an encrypted file to S3
        /// </summary>
        /// <param name="filePath">Path to the encrypted file</param>
        /// <param name="userId">ID of the user uploading the file</param>
        /// <returns>S3 object key of the uploaded file</returns>
        public async Task<string> UploadFileAsync(string filePath, string userId)
        {
            var fileName = Path.GetFileName(filePath);
            var s3Key = $"{userId}/{Guid.NewGuid()}/{fileName}";

            try
            {
                // Create a request to upload the file to S3
                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = s3Key,
                    FilePath = filePath,
                    ContentType = "application/octet-stream",
                    Metadata =
                    {
                        ["user-id"] = userId,
                        ["original-filename"] = fileName,
                        ["upload-date"] = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
                    }
                };

                // Upload the file to S3
                await _s3Client.PutObjectAsync(putRequest);

                return s3Key;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading file to S3: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Stores encryption keys in KMS
        /// </summary>
        /// <param name="keys">Dictionary containing encryption keys</param>
        /// <returns>KMS key ID for retrieving keys</returns>
        public async Task<string> StoreEncryptionKeysAsync(Dictionary<string, byte[]> keys)
        {
            try
            {
                // Convert keys dictionary to JSON
                var keysJson = new Dictionary<string, string>();
                foreach (var key in keys)
                {
                    keysJson[key.Key] = Convert.ToBase64String(key.Value);
                }
                
                var jsonString = JsonSerializer.Serialize(keysJson);
                var plaintext = Encoding.UTF8.GetBytes(jsonString);

                // Encrypt the keys using KMS
                var encryptRequest = new EncryptRequest
                {
                    KeyId = _kmsKeyId,
                    Plaintext = new MemoryStream(plaintext)
                };

                var encryptResponse = await _kmsClient.EncryptAsync(encryptRequest);
                
                // Generate a unique key ID for this set of keys
                var keyId = Guid.NewGuid().ToString();
                
                // Store the encrypted keys in S3
                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = $"keys/{keyId}",
                    InputStream = encryptResponse.CiphertextBlob,
                    Metadata =
                    {
                        ["kms-key-id"] = _kmsKeyId,
                        ["created-date"] = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
                    }
                };

                await _s3Client.PutObjectAsync(putRequest);

                return keyId;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error storing encryption keys: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves encryption keys from KMS
        /// </summary>
        /// <param name="keyId">KMS key ID for retrieving keys</param>
        /// <returns>Dictionary containing decrypted encryption keys</returns>
        public async Task<Dictionary<string, byte[]>> RetrieveEncryptionKeysAsync(string keyId)
        {
            try
            {
                // Get the encrypted keys from S3
                var getRequest = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = $"keys/{keyId}"
                };

                var s3Response = await _s3Client.GetObjectAsync(getRequest);
                
                // Read the encrypted data
                byte[] encryptedData;
                using (var ms = new MemoryStream())
                {
                    await s3Response.ResponseStream.CopyToAsync(ms);
                    encryptedData = ms.ToArray();
                }

                // Decrypt the data using KMS
                var decryptRequest = new DecryptRequest
                {
                    CiphertextBlob = new MemoryStream(encryptedData)
                };

                var decryptResponse = await _kmsClient.DecryptAsync(decryptRequest);
                
                // Convert decrypted data to Dictionary
                byte[] decryptedData;
                using (var ms = new MemoryStream())
                {
                    await decryptResponse.Plaintext.CopyToAsync(ms);
                    decryptedData = ms.ToArray();
                }

                var jsonString = Encoding.UTF8.GetString(decryptedData);
                var keysJson = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
                
                // Convert Base64 strings back to byte arrays
                var keys = new Dictionary<string, byte[]>();
                foreach (var key in keysJson)
                {
                    keys[key.Key] = Convert.FromBase64String(key.Value);
                }

                return keys;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving encryption keys: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Downloads a file from S3
        /// </summary>
        /// <param name="s3Key">S3 object key of the file to download</param>
        /// <param name="outputPath">Path where the downloaded file will be saved</param>
        /// <returns>Original file name from metadata</returns>
        public async Task<string> DownloadFileAsync(string s3Key, string outputPath)
        {
            try
            {
                // Get the file from S3
                var getRequest = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = s3Key
                };

                var response = await _s3Client.GetObjectAsync(getRequest);
                
                // Save the file
                using (var fileStream = new FileStream(outputPath, FileMode.Create))
                {
                    await response.ResponseStream.CopyToAsync(fileStream);
                }

                // Return the original file name
                return response.Metadata["original-filename"] ?? Path.GetFileName(s3Key);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error downloading file from S3: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes a file and its associated encryption keys from S3
        /// </summary>
        /// <param name="s3Key">S3 object key of the file to delete</param>
        /// <param name="keyId">ID of the associated encryption keys</param>
        public async Task DeleteFileAsync(string s3Key, string keyId)
        {
            try
            {
                // Delete the file from S3
                var deleteFileRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = s3Key
                };

                await _s3Client.DeleteObjectAsync(deleteFileRequest);

                // Delete the keys from S3
                var deleteKeysRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = $"keys/{keyId}"
                };

                await _s3Client.DeleteObjectAsync(deleteKeysRequest);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting file from S3: {ex.Message}", ex);
            }
        }
    }
}