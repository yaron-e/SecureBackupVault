import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import App from './components/App';
import Login from './components/Login';
import FileDashboard from './components/FileDashboard';

// Custom route that redirects to login if user is not authenticated
const ProtectedRoute = ({ element, isAuthenticated }) => {
  return isAuthenticated ? element : <Navigate to="/login" />;
};

const AppRoutes = ({ user, onLoginSuccess, onLogout }) => {
  return (
    <Router>
      <Routes>
        <Route
          path="/"
          element={<App user={user} onLogout={onLogout} />}
        >
          <Route
            index
            element={
              user ? (
                <FileDashboard />
              ) : (
                <Navigate to="/login" />
              )
            }
          />
          <Route
            path="/login"
            element={
              user ? (
                <Navigate to="/" />
              ) : (
                <Login onLoginSuccess={onLoginSuccess} />
              )
            }
          />
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute
                isAuthenticated={!!user}
                element={<FileDashboard />}
              />
            }
          />
        </Route>
      </Routes>
    </Router>
  );
};

export default AppRoutes;