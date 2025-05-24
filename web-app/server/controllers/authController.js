const bcrypt = require("bcryptjs");
const jwt = require("jsonwebtoken");
const { OAuth2Client } = require("google-auth-library");
const { eq } = require("drizzle-orm");
const { db } = require("../db");
const { users } = require("../../shared/schema");
const { storage } = require("../storage");

// Configure Google OAuth client
const googleClient = new OAuth2Client(process.env.GOOGLE_CLIENT_ID);

// JWT token secret
const JWT_SECRET = process.env.JWT_SECRET || "secure-backup-jwt-secret-key";

// Generate JWT token for user
const generateToken = (user) => {
  return jwt.sign(
    {
      id: user.id,
      email: user.email,
    },
    JWT_SECRET,
    { expiresIn: "7d" },
  );
};

// Register new user
exports.register = async (req, res, next) => {
  try {
    const { name, email, password } = req.body;

    // Validate input
    if (!email || !password) {
      return res.status(400).json({
        message: "Email and password are required",
      });
    }

    // Check if user already exists
    const existingUser = await storage.getUserByUsername(email);
    if (existingUser) {
      return res.status(400).json({
        message: "User with this email already exists",
      });
    }

    // Hash password
    const salt = await bcrypt.genSalt(10);
    const hashedPassword = await bcrypt.hash(password, salt);

    // Create user
    const user = await storage.createUser({
      name,
      email,
      password: hashedPassword,
      googleId: null,
    });

    // Generate token
    const token = generateToken(user);

    // Return user data and token
    res.status(201).json({
      user: {
        id: user.id,
        name: user.name,
        email: user.email,
      },
      token,
    });
  } catch (error) {
    next(error);
  }
};

// Login user
exports.login = async (req, res, next) => {
  console.log("In the login function");
  try {
    const { email, password } = req.body;

    // Validate input
    if (!email || !password) {
      return res.status(400).json({
        message: "Email and password are required",
      });
    }

    // Check if user exists
    const user = await storage.getUserByUsername(email);
    if (!user) {
      return res.status(401).json({
        message: "Invalid credentials",
      });
    }

    // Check if user has a password (might be Google account)
    if (!user.password) {
      return res.status(401).json({
        message: "This account uses Google authentication",
      });
    }

    // Verify password
    const isMatch = await bcrypt.compare(password, user.password);
    if (!isMatch) {
      return res.status(401).json({
        message: "Invalid credentials",
      });
    }

    // Generate token
    const token = generateToken(user);

    // Return user data and token
    res.status(200).json({
      user: {
        id: user.id,
        name: user.name,
        email: user.email,
      },
      token,
    });
  } catch (error) {
    next(error);
  }
};

// Google authentication
exports.googleAuth = async (req, res, next) => {
  try {
    const { token } = req.body;

    // Verify Google token
    const ticket = await googleClient.verifyIdToken({
      idToken: token,
      audience: process.env.GOOGLE_CLIENT_ID,
    });

    const payload = ticket.getPayload();
    const googleId = payload.sub;
    const email = payload.email;
    const name = payload.name;

    // Check if user already exists
    let user = await storage.getUserByGoogleId(googleId);

    if (!user) {
      // Check if user exists by email
      user = await storage.getUserByUsername(email);

      if (user) {
        // Update existing user with Google ID
        user = await storage.updateUser(user.id, {
          ...user,
          googleId,
        });
      } else {
        // Create new user
        user = await storage.createUser({
          name,
          email,
          password: null,
          googleId,
        });
      }
    }

    // Generate token
    const jwtToken = generateToken(user);

    // Return user data and token
    res.status(200).json({
      user: {
        id: user.id,
        name: user.name,
        email: user.email,
      },
      token: jwtToken,
    });
  } catch (error) {
    next(error);
  }
};

// Get current user profile
exports.getMe = async (req, res, next) => {
  try {
    // req.user is set from the auth middleware
    const user = await storage.getUser(req.user.id);

    if (!user) {
      return res.status(404).json({
        message: "User not found",
      });
    }

    res.status(200).json({
      id: user.id,
      name: user.name,
      email: user.email,
      createdAt: user.createdAt,
    });
  } catch (error) {
    next(error);
  }
};
