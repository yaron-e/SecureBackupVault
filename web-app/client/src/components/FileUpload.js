import React, { useState, useRef } from 'react';

const FileUpload = ({ onUploadComplete }) => {
  const [files, setFiles] = useState([]);
  const [uploading, setUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState({});
  const [error, setError] = useState(null);
  const fileInputRef = useRef(null);

  const handleFileChange = (e) => {
    setError(null);
    const selectedFiles = Array.from(e.target.files);
    
    if (selectedFiles.length > 0) {
      setFiles(selectedFiles);
    }
  };

  const handleUpload = async () => {
    if (files.length === 0) {
      setError('Please select files to upload');
      return;
    }

    setUploading(true);
    setError(null);
    
    const uploadPromises = files.map(async (file) => {
      // Initialize progress for this file
      setUploadProgress(prev => ({
        ...prev,
        [file.name]: 0
      }));
      
      try {
        // In a real implementation, this would connect to your backend API
        // and upload the file to AWS S3 via the secure desktop app
        
        // Simulate upload progress
        for (let progress = 0; progress <= 100; progress += 10) {
          setUploadProgress(prev => ({
            ...prev,
            [file.name]: progress
          }));
          
          // Simulate network delay
          await new Promise(resolve => setTimeout(resolve, 300));
        }
        
        return {
          name: file.name,
          size: file.size,
          uploaded: true
        };
      } catch (err) {
        console.error(`Error uploading ${file.name}:`, err);
        setError(`Error uploading ${file.name}: ${err.message}`);
        
        setUploadProgress(prev => ({
          ...prev,
          [file.name]: 0
        }));
        
        return {
          name: file.name,
          uploaded: false,
          error: err.message
        };
      }
    });
    
    try {
      const results = await Promise.all(uploadPromises);
      
      // Reset the file input
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
      
      setFiles([]);
      setUploading(false);
      setUploadProgress({});
      
      // Call the callback with upload results
      if (onUploadComplete) {
        onUploadComplete(results);
      }
    } catch (err) {
      setError('An error occurred during upload');
      setUploading(false);
    }
  };

  const formatFileSize = (bytes) => {
    if (bytes === 0) return '0 B';
    
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    
    return parseFloat((bytes / Math.pow(1024, i)).toFixed(2)) + ' ' + sizes[i];
  };

  return (
    <div className="file-upload-container">
      <h2>Upload Files for Backup</h2>
      
      {error && (
        <div className="error-message">
          <p>{error}</p>
        </div>
      )}
      
      <div className="file-input-area">
        <input
          type="file"
          multiple
          onChange={handleFileChange}
          disabled={uploading}
          ref={fileInputRef}
          className="file-input"
          id="file-input"
        />
        <label htmlFor="file-input" className="file-input-label">
          {files.length > 0 ? `${files.length} file(s) selected` : 'Choose Files'}
        </label>
      </div>
      
      {files.length > 0 && (
        <div className="selected-files">
          <h3>Selected Files</h3>
          <ul className="file-list">
            {files.map((file, index) => (
              <li key={index} className="file-item">
                <div className="file-info">
                  <span className="file-name">{file.name}</span>
                  <span className="file-size">{formatFileSize(file.size)}</span>
                </div>
                
                {uploading && (
                  <div className="progress-bar-container">
                    <div 
                      className="progress-bar" 
                      style={{ width: `${uploadProgress[file.name] || 0}%` }}
                    ></div>
                    <span className="progress-text">{uploadProgress[file.name] || 0}%</span>
                  </div>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}
      
      <div className="upload-actions">
        <button
          className="upload-button"
          onClick={handleUpload}
          disabled={uploading || files.length === 0}
        >
          {uploading ? 'Uploading...' : 'Upload Files'}
        </button>
        
        {uploading && (
          <button
            className="cancel-button"
            onClick={() => {
              setUploading(false);
              setUploadProgress({});
            }}
          >
            Cancel
          </button>
        )}
      </div>
      
      <div className="upload-instructions">
        <p>
          <strong>Note:</strong> Files will be encrypted with AES-256, Twofish,
          and Serpent cascade encryption before being uploaded to secure storage.
        </p>
        <p>
          For actual backups, please use the Windows desktop application which
          provides automatic file monitoring and encryption.
        </p>
      </div>
    </div>
  );
};

export default FileUpload;