import React, { useContext } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Navbar from './components/Navbar';
import Dashboard from './pages/Dashboard';
import Login from './pages/Login';
import Register from './pages/Register';
import { AuthContext } from './context/AuthContext';

function App() {
  const { isAuthenticated, isLoading } = useContext(AuthContext);

  // Create a protected route component
  const ProtectedRoute = ({ children }) => {
    if (isLoading) {
      return (
        <div className="flex items-center justify-center min-h-screen">
          <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-500"></div>
        </div>
      );
    }

    if (!isAuthenticated) {
      return <Navigate to="/login" />;
    }

    return children;
  };

  return (
    <Router>
      <div className="flex flex-col min-h-screen">
        <Navbar />
        <main className="flex-grow container mx-auto px-4 py-8">
          <Routes>
            <Route path="/" element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            } />
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
          </Routes>
        </main>
        <footer className="bg-gray-800 text-white p-4">
          <div className="container mx-auto text-center text-sm">
            <p>© {new Date().getFullYear()} Secure Backup. All rights reserved.</p>
            <p className="mt-1">Your files are secured with cascade encryption (AES-256 → Twofish → Serpent)</p>
          </div>
        </footer>
      </div>
    </Router>
  );
}

export default App;
