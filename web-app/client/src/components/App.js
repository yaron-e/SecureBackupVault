import React from 'react';
import { Outlet } from 'react-router-dom';
import Navbar from './Navbar';

const App = ({ user, onLogout }) => {
  return (
    <div className="app-container">
      <Navbar user={user} onLogout={onLogout} />
      
      <main className="main-content">
        <Outlet />
      </main>
      
      <footer className="app-footer">
        <p>&copy; {new Date().getFullYear()} Secure Backup. All rights reserved.</p>
      </footer>
    </div>
  );
};

export default App;