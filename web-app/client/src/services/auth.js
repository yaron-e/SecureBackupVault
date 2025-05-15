import api from './api';

/**
 * Register a new user
 * @param {string} name - User's full name
 * @param {string} email - User's email address
 * @param {string} password - User's password
 */
export const registerUser = async (name, email, password) => {
  try {
    const response = await api.post('/auth/register', {
      name,
      email,
      password
    });
    return response.data;
  } catch (error) {
    console.error('Registration error:', error);
    throw error;
  }
};

/**
 * Login a user with email and password
 * @param {string} email - User's email address
 * @param {string} password - User's password
 */
export const loginUser = async (email, password) => {
  try {
    const response = await api.post('/auth/login', {
      email,
      password
    });
    return response.data;
  } catch (error) {
    console.error('Login error:', error);
    throw error;
  }
};

/**
 * Authenticate or register a user with a Google token
 * @param {string} token - Google OAuth token
 */
export const googleAuth = async (token) => {
  try {
    const response = await api.post('/auth/google', { token });
    return response.data;
  } catch (error) {
    console.error('Google auth error:', error);
    throw error;
  }
};

/**
 * Get the current user profile
 */
export const getCurrentUser = async () => {
  try {
    const response = await api.get('/auth/me');
    return response.data;
  } catch (error) {
    console.error('Get user error:', error);
    throw error;
  }
};

/**
 * Logout the user (client-side only)
 */
export const logoutUser = () => {
  localStorage.removeItem('token');
  localStorage.removeItem('user');
};
