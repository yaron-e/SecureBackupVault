require('dotenv').config();

/** @type {import('drizzle-kit').Config} */
module.exports = {
  schema: './shared/schema.js',
  out: './drizzle',
  driver: 'pg',
  dbCredentials: {
    connectionString: process.env.DATABASE_URL,
  },
  strict: true,
  verbose: true,
};