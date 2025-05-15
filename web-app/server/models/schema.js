const { pool } = require('../db');

// Execute SQL query to create tables if they don't exist
async function initializeDatabase() {
  try {
    await pool.query(`
      CREATE TABLE IF NOT EXISTS users (
        id SERIAL PRIMARY KEY,
        name VARCHAR(255),
        email VARCHAR(255) UNIQUE NOT NULL,
        password VARCHAR(255),
        google_id VARCHAR(255) UNIQUE,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      );

      CREATE TABLE IF NOT EXISTS file_backups (
        id SERIAL PRIMARY KEY,
        user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
        file_name VARCHAR(255) NOT NULL,
        s3_key VARCHAR(255) UNIQUE NOT NULL,
        key_id VARCHAR(255) NOT NULL,
        size BIGINT NOT NULL,
        last_modified TIMESTAMP NOT NULL,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
      );
    `);

    console.log('Database schema initialized successfully');
  } catch (error) {
    console.error('Error initializing database schema:', error);
    throw error;
  }
}

// User model
const User = {
  // Create a new user
  async create(userData) {
    const { name, email, password, googleId } = userData;
    
    const query = `
      INSERT INTO users (name, email, password, google_id)
      VALUES ($1, $2, $3, $4)
      RETURNING id, name, email, created_at
    `;
    
    const result = await pool.query(query, [name, email, password, googleId]);
    return result.rows[0];
  },

  // Find user by ID
  async findById(id) {
    const query = 'SELECT id, name, email, created_at FROM users WHERE id = $1';
    const result = await pool.query(query, [id]);
    return result.rows[0] || null;
  },

  // Find user by email
  async findByEmail(email) {
    const query = 'SELECT * FROM users WHERE email = $1';
    const result = await pool.query(query, [email]);
    return result.rows[0] || null;
  },

  // Find user by Google ID
  async findByGoogleId(googleId) {
    const query = 'SELECT * FROM users WHERE google_id = $1';
    const result = await pool.query(query, [googleId]);
    return result.rows[0] || null;
  },

  // Update user
  async update(id, userData) {
    const { name, email, password } = userData;
    
    const query = `
      UPDATE users
      SET name = $1, email = $2, password = $3, updated_at = CURRENT_TIMESTAMP
      WHERE id = $4
      RETURNING id, name, email, created_at
    `;
    
    const result = await pool.query(query, [name, email, password, id]);
    return result.rows[0];
  }
};

// FileBackup model
const FileBackup = {
  // Create a new file backup record
  async create(fileData) {
    const { userId, fileName, s3Key, keyId, size, lastModified } = fileData;
    
    const query = `
      INSERT INTO file_backups (user_id, file_name, s3_key, key_id, size, last_modified)
      VALUES ($1, $2, $3, $4, $5, $6)
      RETURNING *
    `;
    
    const result = await pool.query(query, [userId, fileName, s3Key, keyId, size, lastModified]);
    return result.rows[0];
  },

  // Find file backup by S3 key
  async findByS3Key(s3Key) {
    const query = 'SELECT * FROM file_backups WHERE s3_key = $1';
    const result = await pool.query(query, [s3Key]);
    return result.rows[0] || null;
  },

  // Get all file backups for a user
  async findByUserId(userId) {
    const query = `
      SELECT * FROM file_backups
      WHERE user_id = $1
      ORDER BY last_modified DESC
    `;
    
    const result = await pool.query(query, [userId]);
    return result.rows;
  },

  // Delete a file backup
  async delete(s3Key, userId) {
    const query = 'DELETE FROM file_backups WHERE s3_key = $1 AND user_id = $2 RETURNING *';
    const result = await pool.query(query, [s3Key, userId]);
    return result.rows[0] || null;
  },

  // Get user stats (total files, total size, last backup time)
  async getUserStats(userId) {
    const query = `
      SELECT 
        COUNT(*) as total_files,
        COALESCE(SUM(size), 0) as total_size,
        MAX(last_modified) as last_backup
      FROM file_backups
      WHERE user_id = $1
    `;
    
    const result = await pool.query(query, [userId]);
    return result.rows[0];
  }
};

module.exports = {
  initializeDatabase,
  User,
  FileBackup
};
