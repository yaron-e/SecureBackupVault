const { pgTable, serial, text, varchar, timestamp, bigint, integer, jsonb, index } = require('drizzle-orm/pg-core');
const { relations } = require('drizzle-orm');

// Users table
const users = pgTable('users', {
  id: serial('id').primaryKey(),
  name: varchar('name', { length: 255 }),
  email: varchar('email', { length: 255 }).notNull().unique(),
  password: varchar('password', { length: 255 }),
  googleId: varchar('google_id', { length: 255 }).unique(),
  replitId: varchar('replit_id', { length: 255 }).unique(),
  profileImage: varchar('profile_image', { length: 1024 }),
  createdAt: timestamp('created_at').defaultNow(),
  updatedAt: timestamp('updated_at').defaultNow()
});

// File backups table
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

// Define relations
const usersRelations = relations(users, ({ many }) => ({
  fileBackups: many(fileBackups)
}));

const fileBackupsRelations = relations(fileBackups, ({ one }) => ({
  user: one(users, {
    fields: [fileBackups.userId],
    references: [users.id]
  })
}));

// Session storage for authentication
const sessions = pgTable('sessions', {
  sid: varchar('sid').primaryKey(),
  sess: jsonb('sess').notNull(),
  expire: timestamp('expire').notNull()
}, (table) => {
  return {
    expireIdx: index('IDX_session_expire').on(table.expire)
  };
});

module.exports = {
  users,
  fileBackups,
  sessions,
  usersRelations,
  fileBackupsRelations
};