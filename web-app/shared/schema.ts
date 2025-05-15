import { pgTable, serial, text, varchar, timestamp, bigint, integer } from 'drizzle-orm/pg-core';
import { relations } from 'drizzle-orm';

// Users table
export const users = pgTable('users', {
  id: serial('id').primaryKey(),
  name: varchar('name', { length: 255 }),
  email: varchar('email', { length: 255 }).notNull().unique(),
  password: varchar('password', { length: 255 }),
  googleId: varchar('google_id', { length: 255 }).unique(),
  createdAt: timestamp('created_at').defaultNow(),
  updatedAt: timestamp('updated_at').defaultNow()
});

// File backups table
export const fileBackups = pgTable('file_backups', {
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
export const usersRelations = relations(users, ({ many }) => ({
  fileBackups: many(fileBackups)
}));

export const fileBackupsRelations = relations(fileBackups, ({ one }) => ({
  user: one(users, {
    fields: [fileBackups.userId],
    references: [users.id]
  })
}));

// Type definitions
export type User = typeof users.$inferSelect;
export type InsertUser = typeof users.$inferInsert;

export type FileBackup = typeof fileBackups.$inferSelect;
export type InsertFileBackup = typeof fileBackups.$inferInsert;