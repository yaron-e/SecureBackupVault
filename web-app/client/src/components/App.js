import React, { useState, useEffect } from 'react';
import { useAuth } from '../hooks/useApi';
import Navbar from './Navbar';
import Login from './Login';
import FileDashboard from './FileDashboard';

const App = () => {
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
    setUser(null);
  };

  if (isLoading) {
    return <div className="loading-container">Loading...</div>;
  }

  return (
    <div className="app-container">
      <Navbar user={user} onLogout={handleLogout} />
      
      <main className="main-content">
        {user ? (
          <FileDashboard />
        ) : (
          <Login onLoginSuccess={handleLoginSuccess} />
        )}
      </main>
      
      <footer className="app-footer">
        <p>&copy; {new Date().getFullYear()} Secure Backup. All rights reserved.</p>
      </footer>
    </div>
  );
};

export default App;