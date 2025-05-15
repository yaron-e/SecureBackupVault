const { Pool } = require('pg');
const { drizzle } = require('drizzle-orm/node-postgres');
const schema = require('../shared/schema');

// Get database configuration from environment variables
const connectionString = process.env.DATABASE_URL;

// Create a new pool instance
const pool = new Pool({
  connectionString,
  ssl: process.env.NODE_ENV === 'production' ? { rejectUnauthorized: false } : false
});

// Create a Drizzle instance
const db = drizzle(pool);

// Export the pool and db for use in other modules
module.exports = { pool, db };
