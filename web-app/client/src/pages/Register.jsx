import React, { useState, useContext, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AuthContext } from '../context/AuthContext';
import AuthForm from '../components/AuthForm';

const Register = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const { register, loginWithGoogle, isAuthenticated } = useContext(AuthContext);
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
      await register(formData.name, formData.email, formData.password);
      navigate('/');
    } catch (err) {
      setError(
        err.response?.data?.message || 
        'Failed to create account. Please check your information and try again.'
      );
      console.error('Registration error:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleGoogleLogin = async () => {
    try {
      await loginWithGoogle();
      navigate('/');
    } catch (err) {
      setError(
        err.response?.data?.message || 
        'Failed to register with Google. Please try again.'
      );
      console.error('Google registration error:', err);
    }
  };

  return (
    <div className="flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-md mb-8">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-gray-800">Secure Backup</h1>
          <p className="text-gray-600 mt-2">Create an account to access your securely encrypted files</p>
        </div>

        <AuthForm
          isLogin={false}
          onSubmit={handleSubmit}
          loading={loading}
          error={error}
          onGoogleLogin={handleGoogleLogin}
        />

        <div className="text-center mt-6">
          <p className="text-gray-600">
            Already have an account?{' '}
            <Link to="/login" className="text-blue-500 hover:text-blue-700 font-semibold">
              Sign in
            </Link>
          </p>
        </div>
      </div>

      <div className="w-full max-w-xl mt-4 bg-green-50 rounded-lg p-6 border border-green-200">
        <h3 className="text-xl font-semibold text-green-800 mb-3">Why use Secure Backup?</h3>
        <ul className="list-disc list-inside text-green-700 space-y-2">
          <li>
            <strong>Triple-layer encryption</strong> - Your files are secured with cascade encryption (AES-256 → Twofish → Serpent)
          </li>
          <li>
            <strong>Secure key management</strong> - Encryption keys are securely stored in AWS KMS
          </li>
          <li>
            <strong>Easy access</strong> - Access your files from anywhere via our web interface
          </li>
          <li>
            <strong>Automated backups</strong> - Set up automatic backups with the Windows application
          </li>
          <li>
            <strong>Full control</strong> - View, download, and manage your backed-up files
          </li>
        </ul>
      </div>
    </div>
  );
};

export default Register;
