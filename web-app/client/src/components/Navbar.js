import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useApi';

const Navbar = ({ user, onLogout }) => {
  const { logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    if (onLogout) {
      onLogout();
    }
    navigate('/');
  };

  return (
    <nav className="navbar">
      <div className="navbar-logo">
        <Link to="/" className="logo-link">
          <h1>Secure Backup</h1>
        </Link>
      </div>

      <div className="navbar-menu">
        <Link to="/" className="nav-link">Home</Link>
        {user && <Link to="/dashboard" className="nav-link">Dashboard</Link>}
        <a href="/#features" className="nav-link">Features</a>
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
          <Link to="/login" className="login-button">
            Login
          </Link>
        )}
      </div>
    </nav>
  );
};

export default Navbar;