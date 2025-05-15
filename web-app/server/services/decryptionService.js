const crypto = require('crypto');
const fs = require('fs');
const stream = require('stream');
const awsService = require('./awsService');

// Constants
const IV_SIZE = 16; // 128 bits

// Decrypt a file using cascade decryption (Serpent -> Twofish -> AES-256)
exports.decryptFile = async (encryptedFilePath, outputFilePath, keyId) => {
  try {
    // Get encryption keys from AWS KMS
    const keys = await awsService.getEncryptionKeys(keyId);
    
    // Read the encrypted file
    const encryptedData = fs.readFileSync(encryptedFilePath);
    
    // Extract IVs from the beginning of the file
    let position = 0;
    const aesIv = encryptedData.slice(position, position + IV_SIZE);
    position += IV_SIZE;
    
    const twofishIv = encryptedData.slice(position, position + IV_SIZE);
    position += IV_SIZE;
    
    const serpentIv = encryptedData.slice(position, position + IV_SIZE);
    position += IV_SIZE;
    
    // Get the encrypted content (after the IVs)
    const encryptedContent = encryptedData.slice(position);
    
    // Decrypt in reverse order
    
    // Step 1: Decrypt Serpent layer
    const serpentDecrypted = decryptSerpent(encryptedContent, keys.serpentKey, serpentIv);
    
    // Step 2: Decrypt Twofish layer
    const twofishDecrypted = decryptTwofish(serpentDecrypted, keys.twofishKey, twofishIv);
    
    // Step 3: Decrypt AES layer
    const aesDecrypted = decryptAes(twofishDecrypted, keys.aesKey, aesIv);
    
    // Write decrypted file
    fs.writeFileSync(outputFilePath, aesDecrypted);
    
    return true;
  } catch (error) {
    console.error('Error decrypting file:', error);
    throw error;
  }
};

// AES-256 decryption
function decryptAes(data, key, iv) {
  try {
    const decipher = crypto.createDecipheriv('aes-256-cbc', key, iv);
    return Buffer.concat([decipher.update(data), decipher.final()]);
  } catch (error) {
    console.error('Error during AES decryption:', error);
    throw error;
  }
}

// Twofish decryption (using OpenSSL EVP)
function decryptTwofish(data, key, iv) {
  try {
    // Since Node.js doesn't natively support Twofish, we'll use a workaround with OpenSSL EVP
    // Note: In a production environment, you would use a proper Twofish implementation library
    
    // For demonstration, we'll use AES as a placeholder
    // In a real implementation, you would need to include a proper Twofish library
    const decipher = crypto.createDecipheriv('aes-256-cbc', key, iv);
    return Buffer.concat([decipher.update(data), decipher.final()]);
  } catch (error) {
    console.error('Error during Twofish decryption:', error);
    throw error;
  }
}

// Serpent decryption (using OpenSSL EVP)
function decryptSerpent(data, key, iv) {
  try {
    // Since Node.js doesn't natively support Serpent, we'll use a workaround with OpenSSL EVP
    // Note: In a production environment, you would use a proper Serpent implementation library
    
    // For demonstration, we'll use AES as a placeholder
    // In a real implementation, you would need to include a proper Serpent library
    const decipher = crypto.createDecipheriv('aes-256-cbc', key, iv);
    return Buffer.concat([decipher.update(data), decipher.final()]);
  } catch (error) {
    console.error('Error during Serpent decryption:', error);
    throw error;
  }
}
