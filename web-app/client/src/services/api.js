import axios from 'axios';

// Create axios instance with base URL
const api = axios.create({
  baseURL: '/api',
  timeout: 30000 // 30 seconds timeout for larger file operations
});

// Add auth token to requests if available
api.interceptors.request.use(
  config => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  error => {
    return Promise.reject(error);
  }
);

// Auth API

/**
 * Register a new user
 * @param {string} name - User's full name
 * @param {string} email - User's email address
 * @param {string} password - User's password
 */
export const registerUser = async (name, email, password) => {
  try {
    const response = await api.post('/auth/register', {
      name,
      email,
      password
    });
    return response.data;
  } catch (error) {
    console.error('Error registering user:', error);
    throw error;
  }
};

/**
 * Login a user with email and password
 * @param {string} email - User's email address
 * @param {string} password - User's password
 */
export const loginUser = async (email, password) => {
  try {
    const response = await api.post('/auth/login', {
      email,
      password
    });
    return response.data;
  } catch (error) {
    console.error('Error logging in:', error);
    throw error;
  }
};

/**
 * Authenticate or register a user with a Google token
 * @param {string} token - Google OAuth token
 */
export const googleAuth = async (token) => {
  try {
    const response = await api.post('/auth/google', { token });
    return response.data;
  } catch (error) {
    console.error('Error with Google authentication:', error);
    throw error;
  }
};

/**
 * Get the current user profile
 */
export const getCurrentUser = async () => {
  try {
    const response = await api.get('/auth/me');
    return response.data;
  } catch (error) {
    console.error('Error fetching user profile:', error);
    throw error;
  }
};

/**
 * Logout the user (client-side only)
 */
export const logoutUser = () => {
  localStorage.removeItem('token');
};

// Files API

/**
 * Get list of backed up files for the user
 */
export const getFiles = async () => {
  try {
    const response = await api.get('/files');
    return response.data;
  } catch (error) {
    console.error('Error fetching files:', error);
    throw error;
  }
};

/**
 * Download and decrypt a file
 * @param {string} s3Key - The S3 object key
 * @param {string} keyId - The KMS key ID for decryption
 * @param {string} fileName - Original file name for download
 */
export const downloadFile = async (s3Key, keyId, fileName) => {
  try {
    const response = await api.get('/files/download', {
      params: { s3Key, keyId },
      responseType: 'blob'
    });
    
    // Create a download link and trigger it
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', fileName);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
    
    return true;
  } catch (error) {
    console.error('Error downloading file:', error);
    throw error;
  }
};

/**
 * Delete a file from S3
 * @param {string} s3Key - The S3 object key
 */
export const deleteFile = async (s3Key) => {
  try {
    await api.delete(`/files/${s3Key}`);
    return true;
  } catch (error) {
    console.error('Error deleting file:', error);
    throw error;
  }
};

/**
 * Get user statistics (total files, size, last backup)
 */
export const getUserStats = async () => {
  try {
    const response = await api.get('/files/stats');
    return response.data;
  } catch (error) {
    console.error('Error fetching user stats:', error);
    throw error;
  }
};

export default api;
