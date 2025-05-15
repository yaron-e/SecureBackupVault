import React from 'react';
import { useAuth } from '../hooks/useApi';

const Navbar = ({ user, onLogout }) => {
  const { logout } = useAuth();

  const handleLogout = () => {
    logout();
    localStorage.removeItem('token');
    if (onLogout) {
      onLogout();
    }
  };

  return (
    <nav className="navbar">
      <div className="navbar-logo">
        <h1>Secure Backup</h1>
      </div>

      <div className="navbar-links">
        {user ? (
          <>
            <span className="welcome-message">Welcome, {user.name || 'User'}</span>
            <button className="logout-button" onClick={handleLogout}>
              Logout
            </button>
          </>
        ) : (
          <span className="auth-message">Please login to access your backups</span>
        )}
      </div>
    </nav>
  );
};

export default Navbar;