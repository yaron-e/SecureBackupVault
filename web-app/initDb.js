require('dotenv').config();
const { Pool } = require('pg');
const { drizzle } = require('drizzle-orm/node-postgres');
const { migrate } = require('drizzle-orm/node-postgres/migrator');
const { serial, text, varchar, timestamp, bigint, integer, pgTable } = require('drizzle-orm/pg-core');

console.log('Initializing database...');

// Create a PostgreSQL client
const pool = new Pool({
  connectionString: process.env.DATABASE_URL,
  ssl: process.env.NODE_ENV === 'production' ? { rejectUnauthorized: false } : false
});

// Define schema directly here for initialization
const users = pgTable('users', {
  id: serial('id').primaryKey(),
  name: varchar('name', { length: 255 }),
  email: varchar('email', { length: 255 }).notNull().unique(),
  password: varchar('password', { length: 255 }),
  googleId: varchar('google_id', { length: 255 }).unique(),
  createdAt: timestamp('created_at').defaultNow(),
  updatedAt: timestamp('updated_at').defaultNow()
});

const fileBackups = pgTable('file_backups', {
  id: serial('id').primaryKey(),
  userId: integer('user_id').references(() => users.id, { onDelete: 'cascade' }),
  fileName: varchar('file_name', { length: 255 }).notNull(),
  s3Key: varchar('s3_key', { length: 255 }).notNull().unique(),
  keyId: varchar('key_id', { length: 255 }).notNull(),
  size: bigint('size', { mode: 'number' }).notNull(),
  lastModified: timestamp('last_modified').notNull(),
  createdAt: timestamp('created_at').defaultNow()
});

// Initialize the database
async function initializeDatabase() {
  try {
    const db = drizzle(pool);
    
    // Create tables directly using SQL
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
    
    console.log('Database tables created successfully');
    process.exit(0);
  } catch (error) {
    console.error('Error initializing database:', error);
    process.exit(1);
  }
}

// Run initialization
initializeDatabase();