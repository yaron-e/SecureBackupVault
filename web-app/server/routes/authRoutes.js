const express = require('express');
const authController = require('../controllers/authController');
const auth = require('../middleware/auth');

const router = express.Router();

// Register a new user
router.post('/register', authController.register);

// Login a user
router.post('/login', authController.login);

// Google authentication
router.post('/google', authController.googleAuth);

// Get current user profile (protected route)
router.get('/me', auth, authController.getMe);

module.exports = router;
