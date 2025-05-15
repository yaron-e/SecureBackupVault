using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace SecureBackup.Services
{
    public class AwsService
    {
        private readonly IConfiguration _configuration;
        private readonly string _s3BucketName;
        private readonly string _kmsKeyId;
        private readonly RegionEndpoint _region;
        private readonly AmazonS3Client _s3Client;
        private readonly AmazonKeyManagementServiceClient _kmsClient;

        public AwsService(IConfiguration configuration)
        {
            _configuration = configuration;
            _s3BucketName = _configuration["AWS:S3:BucketName"];
            _kmsKeyId = _configuration["AWS:KMS:KeyId"];
            _region = RegionEndpoint.GetBySystemName(_configuration["AWS:Region"]);

            _s3Client = new AmazonS3Client(_region);
            _kmsClient = new AmazonKeyManagementServiceClient(_region);
        }

        /// <summary>
        /// Uploads an encrypted file to S3
        /// </summary>
        /// <param name="filePath">Path to the encrypted file</param>
        /// <param name="keyId">KMS key ID used for encryption</param>
        /// <returns>The S3 object key of the uploaded file</returns>
        public async Task<string> UploadFileToS3Async(string filePath, string keyId)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found", filePath);
            }

            string fileName = Path.GetFileName(filePath);
            string s3Key = $"backups/{DateTime.UtcNow:yyyy-MM-dd}/{Guid.NewGuid()}/{fileName}";

            // Create TransferUtility for easier S3 uploads
            var fileTransferUtility = new TransferUtility(_s3Client);

            // Set metadata including the KMS key ID
            var uploadRequest = new TransferUtilityUploadRequest
            {
                FilePath = filePath,
                BucketName = _s3BucketName,
                Key = s3Key,
                StorageClass = S3StorageClass.StandardInfrequentAccess
            };

            // Add metadata
            uploadRequest.Metadata.Add("KeyId", keyId);
            uploadRequest.Metadata.Add("OriginalFileName", fileName);
            uploadRequest.Metadata.Add("EncryptionDate", DateTime.UtcNow.ToString("o"));

            await fileTransferUtility.UploadAsync(uploadRequest);
            return s3Key;
        }

        /// <summary>
        /// Downloads an encrypted file from S3
        /// </summary>
        /// <param name="s3Key">S3 object key</param>
        /// <param name="destinationPath">Path to save the downloaded file</param>
        /// <returns>The metadata of the downloaded file</returns>
        public async Task<Dictionary<string, string>> DownloadFileFromS3Async(string s3Key, string destinationPath)
        {
            var getObjectRequest = new GetObjectRequest
            {
                BucketName = _s3BucketName,
                Key = s3Key
            };

            using (var response = await _s3Client.GetObjectAsync(getObjectRequest))
            using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
            {
                await response.ResponseStream.CopyToAsync(fileStream);
                
                // Convert response metadata to dictionary
                var metadata = new Dictionary<string, string>();
                foreach (var metadataItem in response.Metadata.Keys)
                {
                    metadata[metadataItem] = response.Metadata[metadataItem];
                }
                
                return metadata;
            }
        }

        /// <summary>
        /// Lists all backed up files
        /// </summary>
        /// <returns>List of S3 objects with metadata</returns>
        public async Task<List<S3ObjectWithMetadata>> ListBackupFilesAsync()
        {
            var result = new List<S3ObjectWithMetadata>();
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _s3BucketName,
                Prefix = "backups/"
            };

            ListObjectsV2Response response;
            do
            {
                response = await _s3Client.ListObjectsV2Async(listRequest);
                
                foreach (var s3Object in response.S3Objects)
                {
                    var metadataRequest = new GetObjectMetadataRequest
                    {
                        BucketName = _s3BucketName,
                        Key = s3Object.Key
                    };

                    try
                    {
                        var metadataResponse = await _s3Client.GetObjectMetadataAsync(metadataRequest);
                        
                        var objectWithMetadata = new S3ObjectWithMetadata
                        {
                            Key = s3Object.Key,
                            LastModified = s3Object.LastModified,
                            Size = s3Object.Size,
                            Metadata = new Dictionary<string, string>()
                        };

                        foreach (var key in metadataResponse.Metadata.Keys)
                        {
                            objectWithMetadata.Metadata[key] = metadataResponse.Metadata[key];
                        }

                        result.Add(objectWithMetadata);
                    }
                    catch (AmazonS3Exception)
                    {
                        // Skip objects that we can't get metadata for
                        continue;
                    }
                }

                listRequest.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated);

            return result;
        }

        /// <summary>
        /// Deletes a file from S3
        /// </summary>
        /// <param name="s3Key">S3 object key</param>
        public async Task DeleteFileFromS3Async(string s3Key)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _s3BucketName,
                Key = s3Key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);
        }

        /// <summary>
        /// Stores encryption keys in AWS KMS
        /// </summary>
        /// <returns>KMS key ID for retrieving the keys</returns>
        public async Task<string> StoreEncryptionKeysAsync(
            string fileName,
            byte[] aesKey, 
            byte[] twofishKey, 
            byte[] serpentKey, 
            byte[] aesIv, 
            byte[] twofishIv, 
            byte[] serpentIv)
        {
            // Create a JSON object with all keys
            var keysObject = new
            {
                FileName = fileName,
                AesKey = Convert.ToBase64String(aesKey),
                TwofishKey = Convert.ToBase64String(twofishKey),
                SerpentKey = Convert.ToBase64String(serpentKey),
                AesIv = Convert.ToBase64String(aesIv),
                TwofishIv = Convert.ToBase64String(twofishIv),
                SerpentIv = Convert.ToBase64String(serpentIv),
                Timestamp = DateTime.UtcNow
            };

            string jsonPayload = JsonConvert.SerializeObject(keysObject);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);

            // Encrypt the payload using the KMS key
            var encryptRequest = new EncryptRequest
            {
                KeyId = _kmsKeyId,
                Plaintext = new MemoryStream(payloadBytes)
            };

            var encryptResponse = await _kmsClient.EncryptAsync(encryptRequest);
            
            // Generate a unique ID for this set of keys
            string uniqueKeyId = Guid.NewGuid().ToString();
            
            // Store the encrypted keys in a KMS alias
            var createAliasRequest = new CreateAliasRequest
            {
                AliasName = $"alias/backup-keys-{uniqueKeyId}",
                TargetKeyId = _kmsKeyId
            };
            
            await _kmsClient.CreateAliasAsync(createAliasRequest);
            
            // Store the encrypted data in S3 for persistence
            var putObjectRequest = new PutObjectRequest
            {
                BucketName = _s3BucketName,
                Key = $"keys/{uniqueKeyId}",
                ContentType = "application/octet-stream",
                InputStream = encryptResponse.CiphertextBlob
            };
            
            await _s3Client.PutObjectAsync(putObjectRequest);
            
            return uniqueKeyId;
        }

        /// <summary>
        /// Retrieves encryption keys from AWS KMS
        /// </summary>
        /// <param name="keyId">KMS key ID</param>
        /// <returns>Decrypted encryption keys</returns>
        public async Task<EncryptionKeys> RetrieveEncryptionKeysAsync(string keyId)
        {
            // Get the encrypted data from S3
            var getObjectRequest = new GetObjectRequest
            {
                BucketName = _s3BucketName,
                Key = $"keys/{keyId}"
            };
            
            var response = await _s3Client.GetObjectAsync(getObjectRequest);
            
            // Decrypt the data using KMS
            var decryptRequest = new DecryptRequest
            {
                CiphertextBlob = response.ResponseStream,
                KeyId = _kmsKeyId
            };
            
            var decryptResponse = await _kmsClient.DecryptAsync(decryptRequest);
            
            // Parse the JSON
            string jsonPayload;
            using (var reader = new StreamReader(decryptResponse.Plaintext))
            {
                jsonPayload = await reader.ReadToEndAsync();
            }
            
            var keysObject = JsonConvert.DeserializeObject<dynamic>(jsonPayload);
            
            return new EncryptionKeys
            {
                AesKey = Convert.FromBase64String((string)keysObject.AesKey),
                TwofishKey = Convert.FromBase64String((string)keysObject.TwofishKey),
                SerpentKey = Convert.FromBase64String((string)keysObject.SerpentKey),
                AesIv = Convert.FromBase64String((string)keysObject.AesIv),
                TwofishIv = Convert.FromBase64String((string)keysObject.TwofishIv),
                SerpentIv = Convert.FromBase64String((string)keysObject.SerpentIv)
            };
        }
    }

    public class S3ObjectWithMetadata
    {
        public string Key { get; set; }
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }

    public class EncryptionKeys
    {
        public byte[] AesKey { get; set; }
        public byte[] TwofishKey { get; set; }
        public byte[] SerpentKey { get; set; }
        public byte[] AesIv { get; set; }
        public byte[] TwofishIv { get; set; }
        public byte[] SerpentIv { get; set; }
    }
}
