import React, { useState, useContext, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AuthContext } from '../context/AuthContext';
import AuthForm from '../components/AuthForm';

const Login = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const { login, loginWithGoogle, isAuthenticated } = useContext(AuthContext);
  const navigate = useNavigate();

  // Redirect if already authenticated
  useEffect(() => {
    if (isAuthenticated) {
      navigate('/');
    }
  }, [isAuthenticated, navigate]);

  const handleSubmit = async (formData) => {
    try {
      setLoading(true);
      setError(null);
      await login(formData.email, formData.password);
      navigate('/');
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to login. Please check your credentials.');
      console.error('Login error:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleGoogleLogin = async () => {
    try {
      await loginWithGoogle();
      navigate('/');
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to login with Google. Please try again.');
      console.error('Google login error:', err);
    }
  };

  return (
    <div className="flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-md mb-8">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-gray-800">Secure Backup</h1>
          <p className="text-gray-600 mt-2">Access your securely encrypted files</p>
        </div>

        <AuthForm
          isLogin={true}
          onSubmit={handleSubmit}
          loading={loading}
          error={error}
          onGoogleLogin={handleGoogleLogin}
        />

        <div className="text-center mt-6">
          <p className="text-gray-600">
            Don't have an account?{' '}
            <Link to="/register" className="text-blue-500 hover:text-blue-700 font-semibold">
              Create one
            </Link>
          </p>
        </div>
      </div>

      <div className="w-full max-w-xl mt-4 bg-blue-50 rounded-lg p-6 border border-blue-200">
        <h3 className="text-xl font-semibold text-blue-800 mb-3">About Secure Backup</h3>
        <p className="text-blue-700 mb-2">
          Our application provides military-grade security for your files using triple-layer cascade encryption:
        </p>
        <ul className="list-disc list-inside text-blue-700 mb-3 space-y-1">
          <li>AES-256 encryption (first layer)</li>
          <li>Twofish encryption (second layer)</li>
          <li>Serpent encryption (third layer)</li>
        </ul>
        <p className="text-blue-700">
          Your encryption keys are securely stored in AWS Key Management Service (KMS) and are only accessible when you need to decrypt your files.
        </p>
      </div>
    </div>
  );
};

export default Login;
