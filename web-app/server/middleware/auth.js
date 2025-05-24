const jwt = require("jsonwebtoken");

const JWT_SECRET = process.env.JWT_SECRET || "secure-backup-jwt-secret-key";

// JWT Authentication middleware
const authMiddleware = (req, res, next) => {
  console.log("Entered auth middleware");
  try {
    // Get token from Authorization header
    const authHeader = req.headers.authorization;

    if (!authHeader || !authHeader.startsWith("Bearer ")) {
      return res.status(401).json({ message: "Authentication required" });
    }

    const token = authHeader.split(" ")[1];

    // Verify token
    const decoded = jwt.verify(token, JWT_SECRET);

    // Add user data to request
    req.user = decoded;

    next();
  } catch (error) {
    if (error.name === "TokenExpiredError") {
      return res.status(401).json({ message: "Token expired" });
    }

    if (error.name === "JsonWebTokenError") {
      return res.status(401).json({ message: "Invalid token" });
    }

    next(error);
  }
};

module.exports = authMiddleware;
