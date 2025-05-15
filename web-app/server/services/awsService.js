const AWS = require('aws-sdk');
const fs = require('fs');

// Configure AWS
AWS.config.update({
  region: process.env.AWS_REGION || 'us-east-1'
});

// Initialize S3 and KMS
const s3 = new AWS.S3();
const kms = new AWS.KMS();

// S3 bucket name
const BUCKET_NAME = process.env.S3_BUCKET_NAME || 'secure-backup-bucket';

// Download a file from S3
exports.downloadFile = async (s3Key, outputPath) => {
  try {
    const params = {
      Bucket: BUCKET_NAME,
      Key: s3Key
    };
    
    const data = await s3.getObject(params).promise();
    
    // Write file to disk
    fs.writeFileSync(outputPath, data.Body);
    
    return {
      metadata: data.Metadata
    };
  } catch (error) {
    console.error('Error downloading file from S3:', error);
    throw error;
  }
};

// Delete a file from S3
exports.deleteFile = async (s3Key) => {
  try {
    const params = {
      Bucket: BUCKET_NAME,
      Key: s3Key
    };
    
    await s3.deleteObject(params).promise();
    
    return true;
  } catch (error) {
    console.error('Error deleting file from S3:', error);
    throw error;
  }
};

// Retrieve encryption keys from KMS
exports.getEncryptionKeys = async (keyId) => {
  try {
    // Get the encrypted data from S3
    const s3Params = {
      Bucket: BUCKET_NAME,
      Key: `keys/${keyId}`
    };
    
    const s3Data = await s3.getObject(s3Params).promise();
    
    // Decrypt the data using KMS
    const kmsParams = {
      CiphertextBlob: s3Data.Body,
      KeyId: process.env.KMS_KEY_ID
    };
    
    const kmsData = await kms.decrypt(kmsParams).promise();
    
    // Parse the decrypted JSON
    const keysObject = JSON.parse(kmsData.Plaintext.toString());
    
    return {
      aesKey: Buffer.from(keysObject.AesKey, 'base64'),
      twofishKey: Buffer.from(keysObject.TwofishKey, 'base64'),
      serpentKey: Buffer.from(keysObject.SerpentKey, 'base64'),
      aesIv: Buffer.from(keysObject.AesIv, 'base64'),
      twofishIv: Buffer.from(keysObject.TwofishIv, 'base64'),
      serpentIv: Buffer.from(keysObject.SerpentIv, 'base64')
    };
  } catch (error) {
    console.error('Error retrieving encryption keys:', error);
    throw error;
  }
};

// Delete encryption keys
exports.deleteEncryptionKeys = async (keyId) => {
  try {
    // Delete the encryption keys from S3
    const params = {
      Bucket: BUCKET_NAME,
      Key: `keys/${keyId}`
    };
    
    await s3.deleteObject(params).promise();
    
    // Note: In a real-world scenario, you would also handle KMS alias deletion
    // But we won't implement that here as it requires more complex IAM permissions
    
    return true;
  } catch (error) {
    console.error('Error deleting encryption keys:', error);
    throw error;
  }
};
