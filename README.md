# Secure Backup System

A comprehensive backup solution with triple-layer cascade encryption (AES-256 → Twofish → Serpent), consisting of a Windows desktop application for local backup and a web interface for remote file management.

## Features

### Windows Application
- Scan your local system for files to back up
- Select specific folders and file types/extensions to include
- Military-grade cascade encryption (AES-256 → Twofish → Serpent)
- Automatic backup on file creation or modification
- Secure storage in AWS S3
- Encryption keys securely stored in AWS KMS

### Web Interface
- View all your backed-up files
- Download and decrypt files from anywhere
- Delete files you no longer need
- Authentication with Google accounts or email/password
- Secure access to your encrypted files

## Security Architecture

### Cascade Encryption
The system implements a triple-layer cascade encryption approach:

1. **First Layer: AES-256** - Advanced Encryption Standard with 256-bit key length
2. **Second Layer: Twofish** - A symmetric key block cipher with a 256-bit key
3. **Third Layer: Serpent** - A symmetric key block cipher with a 256-bit key

This cascade approach ensures that even if one encryption algorithm is compromised, your data remains protected by the other layers.

### Key Management
- Encryption keys are generated using secure random number generation
- Keys are securely stored in AWS Key Management Service (KMS)
- Keys are never stored locally or in plaintext
- Each file has unique encryption keys

## Setup Instructions

### Prerequisites
- Windows 10 or later
- .NET 6.0 Runtime or later
- AWS Account with access to S3 and KMS services
- Node.js and npm (for web interface development/deployment)
- PostgreSQL database (for web interface)

### Windows Application Setup

1. **AWS Configuration**
   - Create an S3 bucket for storing encrypted files
   - Create a KMS key for encryption key storage
   - Configure AWS credentials on your machine using AWS CLI:
     ```
     aws configure
     ```

2. **Application Configuration**
   - Open `appsettings.json` and update the following settings:
     - AWS Region
     - S3 Bucket Name
     - KMS Key ID

3. **Running the Application**
   - Build and run the application using Visual Studio or the .NET CLI:
     