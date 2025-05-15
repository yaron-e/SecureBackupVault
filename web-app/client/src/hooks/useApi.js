import { useState, useCallback } from 'react';
import api from '../services/api';

/**
 * A hook for making API calls with loading and error states
 */
export function useApi() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);

  const execute = useCallback(async (apiCall, options = {}) => {
    const { onSuccess, onError } = options;
    setIsLoading(true);
    setError(null);

    try {
      const response = await apiCall();
      
      if (onSuccess) {
        onSuccess(response);
      }
      
      return response;
    } catch (err) {
      const errorMessage = err.response?.data?.message || err.message || 'An error occurred';
      setError(errorMessage);
      
      if (onError) {
        onError(errorMessage);
      }
      
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return {
    execute,
    isLoading,
    error,
    clearError: () => setError(null)
  };
}

/**
 * Hook to handle file operations
 */
export function useFiles() {
  const { execute, isLoading, error, clearError } = useApi();

  const getFiles = useCallback(() => {
    return execute(() => api.getFiles());
  }, [execute]);

  const downloadFile = useCallback((s3Key, keyId, fileName) => {
    return execute(() => api.downloadFile(s3Key, keyId, fileName));
  }, [execute]);

  const deleteFile = useCallback((s3Key) => {
    return execute(() => api.deleteFile(s3Key));
  }, [execute]);

  const getUserStats = useCallback(() => {
    return execute(() => api.getUserStats());
  }, [execute]);

  return {
    getFiles,
    downloadFile,
    deleteFile,
    getUserStats,
    isLoading,
    error,
    clearError
  };
}

/**
 * Hook to handle user authentication
 */
export function useAuth() {
  const { execute, isLoading, error, clearError } = useApi();
  
  const login = useCallback((email, password) => {
    return execute(() => api.loginUser(email, password));
  }, [execute]);
  
  const register = useCallback((name, email, password) => {
    return execute(() => api.registerUser(name, email, password));
  }, [execute]);
  
  const googleAuth = useCallback((token) => {
    return execute(() => api.googleAuth(token));
  }, [execute]);
  
  const getProfile = useCallback(() => {
    return execute(() => api.getCurrentUser());
  }, [execute]);
  
  const logout = useCallback(() => {
    api.logoutUser();
  }, []);
  
  return {
    login,
    register,
    googleAuth,
    getProfile,
    logout,
    isLoading,
    error,
    clearError
  };
}