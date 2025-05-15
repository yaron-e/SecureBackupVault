const { db } = require('./db');
const { eq } = require('drizzle-orm');
const { users } = require('../shared/schema');

// Interface for storage implementations
class IStorage {
  async getUser(id) { throw new Error('Not implemented'); }
  async getUserByUsername(username) { throw new Error('Not implemented'); }
  async createUser(userData) { throw new Error('Not implemented'); }
}

// Database storage implementation
class DatabaseStorage extends IStorage {
  async getUser(id) {
    const [user] = await db.select().from(users).where(eq(users.id, id));
    return user || undefined;
  }

  async getUserByUsername(username) {
    const [user] = await db.select().from(users).where(eq(users.email, username));
    return user || undefined;
  }

  async createUser(insertUser) {
    const [user] = await db
      .insert(users)
      .values(insertUser)
      .returning();
    return user;
  }
}

// Export a singleton instance
const storage = new DatabaseStorage();
module.exports = { IStorage, DatabaseStorage, storage };