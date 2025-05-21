import React, { useState, useEffect } from 'react';
import ReactDOM from 'react-dom';
import { useAuth } from './hooks/useApi';
import AppRoutes from './routes';
import './styles/main.css';

const Root = () => {
  const [user, setUser] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const { getProfile } = useAuth();

  // Check if user is already logged in
  useEffect(() => {
    const checkAuth = async () => {
      const token = localStorage.getItem('token');
      
      if (token) {
        try {
          const userData = await getProfile();
          setUser(userData);
        } catch (err) {
          // Token is invalid or expired
          localStorage.removeItem('token');
          console.error('Authentication error:', err);
        }
      }
      
      setIsLoading(false);
    };

    checkAuth();
  }, []);

  const handleLoginSuccess = (userData) => {
    setUser(userData);
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    setUser(null);
  };

  if (isLoading) {
    return <div className="loading-container">Loading...</div>;
  }

  return (
    <AppRoutes 
      user={user} 
      onLoginSuccess={handleLoginSuccess} 
      onLogout={handleLogout} 
    />
  );
};

ReactDOM.render(
  <React.StrictMode>
    <Root />
  </React.StrictMode>,
  document.getElementById('root')
);