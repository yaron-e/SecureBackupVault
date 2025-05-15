import React, { useState } from 'react';
import { useAuth } from '../hooks/useApi';

const Login = ({ onLoginSuccess }) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isRegister, setIsRegister] = useState(false);
  const [name, setName] = useState('');
  
  const { login, register, googleAuth, isLoading, error, clearError } = useAuth();

  const handleSubmit = async (e) => {
    e.preventDefault();
    clearError();
    
    try {
      let response;
      
      if (isRegister) {
        response = await register(name, email, password);
      } else {
        response = await login(email, password);
      }
      
      // Store the token in localStorage
      localStorage.setItem('token', response.token);
      
      // Call the parent component's success handler
      if (onLoginSuccess) {
        onLoginSuccess(response.user);
      }
    } catch (err) {
      console.error('Authentication error:', err);
    }
  };

  const handleGoogleLogin = async (response) => {
    try {
      const token = response.credential;
      const data = await googleAuth(token);
      
      // Store the token
      localStorage.setItem('token', data.token);
      
      // Call success handler
      if (onLoginSuccess) {
        onLoginSuccess(data.user);
      }
    } catch (err) {
      console.error('Google authentication error:', err);
    }
  };

  return (
    <div className="auth-container">
      <div className="auth-form-container">
        <h1>{isRegister ? 'Create an Account' : 'Login to Your Account'}</h1>
        
        {error && (
          <div className="error-message">
            <p>{error}</p>
          </div>
        )}
        
        <form onSubmit={handleSubmit} className="auth-form">
          {isRegister && (
            <div className="form-group">
              <label htmlFor="name">Name</label>
              <input
                type="text"
                id="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required={isRegister}
                disabled={isLoading}
              />
            </div>
          )}
          
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              type="email"
              id="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              disabled={isLoading}
            />
          </div>
          
          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              type="password"
              id="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              disabled={isLoading}
            />
          </div>
          
          <button
            type="submit"
            className="auth-button"
            disabled={isLoading}
          >
            {isLoading ? 'Processing...' : isRegister ? 'Create Account' : 'Login'}
          </button>
        </form>
        
        <div className="auth-divider">
          <span>OR</span>
        </div>
        
        <div className="social-auth">
          <div id="googleSignInButton"></div>
        </div>
        
        <div className="auth-switch">
          <p>
            {isRegister
              ? 'Already have an account?'
              : "Don't have an account yet?"}
            <button
              className="switch-button"
              onClick={() => {
                setIsRegister(!isRegister);
                clearError();
              }}
              disabled={isLoading}
            >
              {isRegister ? 'Login' : 'Register'}
            </button>
          </p>
        </div>
      </div>
    </div>
  );
};

export default Login;