const { db } = require('./db');
const { eq } = require('drizzle-orm');
const { users } = require('../shared/schema');

// Interface for storage implementations
class IStorage {
  async getUser(id) { throw new Error('Not implemented'); }
  async getUserByUsername(username) { throw new Error('Not implemented'); }
  async getUserByGoogleId(googleId) { throw new Error('Not implemented'); }
  async getUserByReplitId(replitId) { throw new Error('Not implemented'); }
  async createUser(userData) { throw new Error('Not implemented'); }
  async updateUser(id, userData) { throw new Error('Not implemented'); }
  async upsertUser(userData) { throw new Error('Not implemented'); }
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

  async getUserByGoogleId(googleId) {
    const [user] = await db.select().from(users).where(eq(users.googleId, googleId));
    return user || undefined;
  }
  
  async getUserByReplitId(replitId) {
    const [user] = await db.select().from(users).where(eq(users.replitId, replitId));
    return user || undefined;
  }

  async createUser(insertUser) {
    const [user] = await db
      .insert(users)
      .values(insertUser)
      .returning();
    return user;
  }

  async updateUser(id, userData) {
    const [user] = await db
      .update(users)
      .set({
        ...userData,
        updatedAt: new Date()
      })
      .where(eq(users.id, id))
      .returning();
    return user;
  }
  
  async upsertUser(userData) {
    // First check if user exists by replitId
    if (userData.id) {
      const existingUser = await this.getUserByReplitId(userData.id);
      if (existingUser) {
        // Update existing user
        return await this.updateUser(existingUser.id, {
          ...userData,
          replitId: userData.id,
          profileImage: userData.profileImageUrl
        });
      }
    }
    
    // Then check if user exists by email
    if (userData.email) {
      const existingUser = await this.getUserByUsername(userData.email);
      if (existingUser) {
        // Update existing user
        return await this.updateUser(existingUser.id, {
          ...userData,
          replitId: userData.id,
          profileImage: userData.profileImageUrl
        });
      }
    }
    
    // Create new user
    return await this.createUser({
      name: `${userData.firstName || ''} ${userData.lastName || ''}`.trim() || 'User',
      email: userData.email || `user-${userData.id}@secure-backup.app`,
      password: null,
      googleId: null,
      replitId: userData.id,
      profileImage: userData.profileImageUrl
    });
  }
}

// Export a singleton instance
const storage = new DatabaseStorage();
module.exports = { IStorage, DatabaseStorage, storage };