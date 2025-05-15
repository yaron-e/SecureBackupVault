import React, { createContext, useState, useEffect } from 'react';
import { registerUser, loginUser, googleAuth, getCurrentUser, logoutUser } from '../services/auth';

// Create auth context
export const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  // Check if user is logged in on initial load
  useEffect(() => {
    const checkAuth = async () => {
      const token = localStorage.getItem('token');
      if (token) {
        try {
          const userData = await getCurrentUser();
          setUser(userData);
          setIsAuthenticated(true);
        } catch (err) {
          // Token may be invalid or expired
          console.error('Error verifying auth token:', err);
          localStorage.removeItem('token');
          localStorage.removeItem('user');
        }
      }
      setIsLoading(false);
    };

    checkAuth();
  }, []);

  // Register new user
  const register = async (name, email, password) => {
    try {
      const data = await registerUser(name, email, password);
      localStorage.setItem('token', data.token);
      localStorage.setItem('user', JSON.stringify(data.user));
      setUser(data.user);
      setIsAuthenticated(true);
      return data;
    } catch (error) {
      throw error;
    }
  };

  // Login user
  const login = async (email, password) => {
    try {
      const data = await loginUser(email, password);
      localStorage.setItem('token', data.token);
      localStorage.setItem('user', JSON.stringify(data.user));
      setUser(data.user);
      setIsAuthenticated(true);
      return data;
    } catch (error) {
      throw error;
    }
  };

  // Google authentication
  const loginWithGoogle = async () => {
    return new Promise((resolve, reject) => {
      // Load Google's authentication SDK if not already loaded
      if (!window.google) {
        reject(new Error('Google SDK not loaded'));
        return;
      }

      try {
        // Initialize Google Sign-In
        window.google.accounts.id.initialize({
          client_id: process.env.GOOGLE_CLIENT_ID || '764086051850-6qr4p6gpi6hn506pt8ejuq83di341hur.apps.googleusercontent.com',
          callback: async (response) => {
            try {
              // Send the ID token to our backend
              const data = await googleAuth(response.credential);
              localStorage.setItem('token', data.token);
              localStorage.setItem('user', JSON.stringify(data.user));
              setUser(data.user);
              setIsAuthenticated(true);
              resolve(data);
            } catch (error) {
              console.error('Error during Google authentication:', error);
              reject(error);
            }
          },
        });

        // Prompt the user to select a Google account
        window.google.accounts.id.prompt();
      } catch (error) {
        console.error('Error initializing Google Sign-In:', error);
        reject(error);
      }
    });
  };

  // Logout user
  const logout = () => {
    logoutUser();
    setUser(null);
    setIsAuthenticated(false);
  };

  const authContextValue = {
    user,
    isAuthenticated,
    isLoading,
    register,
    login,
    loginWithGoogle,
    logout
  };

  return (
    <AuthContext.Provider value={authContextValue}>
      {children}
    </AuthContext.Provider>
  );
};
