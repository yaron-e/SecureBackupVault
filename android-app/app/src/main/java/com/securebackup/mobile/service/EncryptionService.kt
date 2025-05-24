package com.securebackup.mobile.service

import android.content.Context
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import java.io.File
import java.security.KeyStore
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec
import javax.crypto.spec.SecretKeySpec
import javax.inject.Inject
import javax.inject.Singleton
import kotlin.random.Random

@Singleton
class EncryptionService @Inject constructor(
    private val context: Context
) {
    
    companion object {
        private const val ANDROID_KEYSTORE = "AndroidKeyStore"
        private const val KEY_ALIAS = "SecureBackupKey"
        private const val TRANSFORMATION_AES = "AES/GCM/NoPadding"
        private const val IV_SIZE = 12
        private const val TAG_SIZE = 16
    }
    
    init {
        generateOrGetMasterKey()
    }
    
    /**
     * Encrypts a file using triple-layer cascade encryption:
     * 1. AES-256-GCM
     * 2. Twofish (simulated with additional AES layer)
     * 3. Serpent (simulated with additional AES layer)
     */
    suspend fun encryptFile(inputFile: File): File {
        val outputFile = File.createTempFile("encrypted_", ".enc", context.cacheDir)
        
        var currentFile = inputFile
        
        // Layer 1: AES-256-GCM
        val aesKey = generateRandomKey()
        val aesEncryptedFile = encryptWithAES(currentFile, aesKey, "aes")
        
        // Layer 2: Twofish (simulated with AES)
        val twofishKey = generateRandomKey()
        val twofishEncryptedFile = encryptWithAES(aesEncryptedFile, twofishKey, "twofish")
        aesEncryptedFile.delete()
        
        // Layer 3: Serpent (simulated with AES)
        val serpentKey = generateRandomKey()
        val serpentEncryptedFile = encryptWithAES(twofishEncryptedFile, serpentKey, "serpent")
        twofishEncryptedFile.delete()
        
        // Store encryption keys securely
        storeEncryptionKeys(aesKey, twofishKey, serpentKey)
        
        // Move final encrypted file to output
        serpentEncryptedFile.renameTo(outputFile)
        
        return outputFile
    }
    
    /**
     * Decrypts a file using reverse cascade decryption
     */
    suspend fun decryptFile(encryptedFile: File, originalFileName: String): File {
        val outputFile = File(context.cacheDir, originalFileName)
        
        // Retrieve encryption keys
        val (aesKey, twofishKey, serpentKey) = getEncryptionKeys()
        
        var currentFile = encryptedFile
        
        // Layer 3: Decrypt Serpent (AES)
        val serpentDecryptedFile = decryptWithAES(currentFile, serpentKey, "serpent_dec")
        
        // Layer 2: Decrypt Twofish (AES)
        val twofishDecryptedFile = decryptWithAES(serpentDecryptedFile, twofishKey, "twofish_dec")
        serpentDecryptedFile.delete()
        
        // Layer 1: Decrypt AES
        val aesDecryptedFile = decryptWithAES(twofishDecryptedFile, aesKey, "aes_dec")
        twofishDecryptedFile.delete()
        
        // Move final decrypted file to output
        aesDecryptedFile.renameTo(outputFile)
        
        return outputFile
    }
    
    private fun encryptWithAES(inputFile: File, key: SecretKey, prefix: String): File {
        val outputFile = File.createTempFile("${prefix}_", ".tmp", context.cacheDir)
        
        val cipher = Cipher.getInstance(TRANSFORMATION_AES)
        cipher.init(Cipher.ENCRYPT_MODE, key)
        
        val iv = cipher.iv
        val inputBytes = inputFile.readBytes()
        val encryptedBytes = cipher.doFinal(inputBytes)
        
        // Write IV + encrypted data
        outputFile.writeBytes(iv + encryptedBytes)
        
        return outputFile
    }
    
    private fun decryptWithAES(inputFile: File, key: SecretKey, prefix: String): File {
        val outputFile = File.createTempFile("${prefix}_", ".tmp", context.cacheDir)
        
        val inputBytes = inputFile.readBytes()
        val iv = inputBytes.sliceArray(0..IV_SIZE-1)
        val encryptedData = inputBytes.sliceArray(IV_SIZE until inputBytes.size)
        
        val cipher = Cipher.getInstance(TRANSFORMATION_AES)
        val spec = GCMParameterSpec(TAG_SIZE * 8, iv)
        cipher.init(Cipher.DECRYPT_MODE, key, spec)
        
        val decryptedBytes = cipher.doFinal(encryptedData)
        outputFile.writeBytes(decryptedBytes)
        
        return outputFile
    }
    
    private fun generateRandomKey(): SecretKey {
        val keyGenerator = KeyGenerator.getInstance("AES")
        keyGenerator.init(256)
        return keyGenerator.generateKey()
    }
    
    private fun generateOrGetMasterKey(): SecretKey {
        val keyStore = KeyStore.getInstance(ANDROID_KEYSTORE)
        keyStore.load(null)
        
        return if (keyStore.containsAlias(KEY_ALIAS)) {
            keyStore.getKey(KEY_ALIAS, null) as SecretKey
        } else {
            val keyGenerator = KeyGenerator.getInstance("AES", ANDROID_KEYSTORE)
            val keyGenParameterSpec = KeyGenParameterSpec.Builder(
                KEY_ALIAS,
                KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT
            )
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                .setKeySize(256)
                .build()
            
            keyGenerator.init(keyGenParameterSpec)
            keyGenerator.generateKey()
        }
    }
    
    private fun storeEncryptionKeys(aesKey: SecretKey, twofishKey: SecretKey, serpentKey: SecretKey) {
        val masterKey = generateOrGetMasterKey()
        
        // In a real implementation, you would encrypt and store these keys securely
        // For now, we'll use Android's EncryptedSharedPreferences or similar
        val sharedPrefs = androidx.security.crypto.EncryptedSharedPreferences.create(
            "encryption_keys",
            masterKey.algorithm,
            context,
            androidx.security.crypto.EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
            androidx.security.crypto.EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
        )
        
        with(sharedPrefs.edit()) {
            putString("aes_key", android.util.Base64.encodeToString(aesKey.encoded, android.util.Base64.DEFAULT))
            putString("twofish_key", android.util.Base64.encodeToString(twofishKey.encoded, android.util.Base64.DEFAULT))
            putString("serpent_key", android.util.Base64.encodeToString(serpentKey.encoded, android.util.Base64.DEFAULT))
            apply()
        }
    }
    
    private fun getEncryptionKeys(): Triple<SecretKey, SecretKey, SecretKey> {
        val masterKey = generateOrGetMasterKey()
        
        val sharedPrefs = androidx.security.crypto.EncryptedSharedPreferences.create(
            "encryption_keys",
            masterKey.algorithm,
            context,
            androidx.security.crypto.EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
            androidx.security.crypto.EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
        )
        
        val aesKeyBytes = android.util.Base64.decode(sharedPrefs.getString("aes_key", ""), android.util.Base64.DEFAULT)
        val twofishKeyBytes = android.util.Base64.decode(sharedPrefs.getString("twofish_key", ""), android.util.Base64.DEFAULT)
        val serpentKeyBytes = android.util.Base64.decode(sharedPrefs.getString("serpent_key", ""), android.util.Base64.DEFAULT)
        
        return Triple(
            SecretKeySpec(aesKeyBytes, "AES"),
            SecretKeySpec(twofishKeyBytes, "AES"),
            SecretKeySpec(serpentKeyBytes, "AES")
        )
    }
}