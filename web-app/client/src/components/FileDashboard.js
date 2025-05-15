import React, { useState, useEffect } from 'react';
import { useFiles } from '../hooks/useApi';

const FileDashboard = () => {
  const [files, setFiles] = useState([]);
  const [stats, setStats] = useState({ totalFiles: 0, totalSize: '0 B', lastBackup: null });
  const { getFiles, downloadFile, deleteFile, getUserStats, isLoading, error } = useFiles();

  useEffect(() => {
    fetchFiles();
    fetchStats();
  }, []);

  const fetchFiles = async () => {
    try {
      const data = await getFiles();
      setFiles(data);
    } catch (err) {
      console.error('Error fetching files:', err);
    }
  };

  const fetchStats = async () => {
    try {
      const data = await getUserStats();
      setStats(data);
    } catch (err) {
      console.error('Error fetching user stats:', err);
    }
  };

  const handleDownload = async (file) => {
    try {
      await downloadFile(file.s3Key, file.keyId, file.fileName);
    } catch (err) {
      console.error('Error downloading file:', err);
    }
  };

  const handleDelete = async (s3Key) => {
    if (!window.confirm('Are you sure you want to delete this file?')) {
      return;
    }

    try {
      await deleteFile(s3Key);
      // Refresh file list and stats after deletion
      fetchFiles();
      fetchStats();
    } catch (err) {
      console.error('Error deleting file:', err);
    }
  };

  const formatDate = (dateString) => {
    if (!dateString) return 'Never';
    const date = new Date(dateString);
    return date.toLocaleString();
  };

  return (
    <div className="dashboard-container">
      <h1>Your Secure Backups</h1>
      
      {error && (
        <div className="error-message">
          <p>{error}</p>
        </div>
      )}
      
      <div className="stats-container">
        <div className="stat-card">
          <h3>Total Files</h3>
          <p>{stats.totalFiles}</p>
        </div>
        <div className="stat-card">
          <h3>Total Size</h3>
          <p>{stats.totalSize}</p>
        </div>
        <div className="stat-card">
          <h3>Last Backup</h3>
          <p>{formatDate(stats.lastBackup)}</p>
        </div>
      </div>
      
      <div className="files-container">
        <h2>Your Files</h2>
        
        {isLoading ? (
          <div className="loading">Loading files...</div>
        ) : files.length === 0 ? (
          <div className="empty-state">
            <p>No files backed up yet.</p>
            <p>Use the desktop app to start backing up your files.</p>
          </div>
        ) : (
          <table className="files-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Size</th>
                <th>Last Modified</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {files.map((file) => (
                <tr key={file.id}>
                  <td>{file.fileName}</td>
                  <td>{formatFileSize(file.size)}</td>
                  <td>{formatDate(file.lastModified)}</td>
                  <td className="file-actions">
                    <button
                      className="download-btn"
                      onClick={() => handleDownload(file)}
                      disabled={isLoading}
                    >
                      Download
                    </button>
                    <button
                      className="delete-btn"
                      onClick={() => handleDelete(file.s3Key)}
                      disabled={isLoading}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};

// Helper function to format file size
const formatFileSize = (bytes) => {
  if (bytes === 0) return '0 B';
  
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(1024));
  
  return parseFloat((bytes / Math.pow(1024, i)).toFixed(2)) + ' ' + sizes[i];
};

export default FileDashboard;