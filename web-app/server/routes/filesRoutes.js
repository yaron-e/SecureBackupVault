const express = require('express');
const filesController = require('../controllers/filesController');
const auth = require('../middleware/auth');

const router = express.Router();

// Protect all routes
router.use(auth);

// Get all files for the user
router.get('/', filesController.getFiles);

// Download a file
router.get('/download/:s3Key', filesController.downloadFile);

// Delete a file
router.delete('/:s3Key', filesController.deleteFile);

// Get user stats
router.get('/stats', filesController.getUserStats);

module.exports = router;
