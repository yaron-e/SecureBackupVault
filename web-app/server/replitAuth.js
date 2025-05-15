const { Issuer, Strategy } = require('openid-client');
const passport = require('passport');
const session = require('express-session');
const memoize = require('memoizee');
const connectPg = require('connect-pg-simple');
const { storage } = require('./storage');

if (!process.env.REPLIT_DOMAINS) {
  throw new Error("Environment variable REPLIT_DOMAINS not provided");
}

const getOidcConfig = memoize(
  async () => {
    // Discover the OIDC provider configuration
    const issuer = await Issuer.discover(
      process.env.ISSUER_URL || "https://replit.com/oidc"
    );
    return new issuer.Client({
      client_id: process.env.REPL_ID,
      redirect_uris: process.env.REPLIT_DOMAINS.split(',').map(
        domain => `https://${domain}/api/callback`
      ),
      response_types: ['code'],
    });
  },
  { maxAge: 3600 * 1000 }
);

function getSession() {
  const sessionTtl = 7 * 24 * 60 * 60 * 1000; // 1 week
  const pgStore = connectPg(session);
  const sessionStore = new pgStore({
    conString: process.env.DATABASE_URL,
    createTableIfMissing: false,
    ttl: sessionTtl,
    tableName: "sessions",
  });
  return session({
    secret: process.env.SESSION_SECRET || 'secure-backup-session-secret',
    store: sessionStore,
    resave: false,
    saveUninitialized: false,
    cookie: {
      httpOnly: true,
      secure: true,
      maxAge: sessionTtl,
    },
  });
}

function updateUserSession(user, tokens) {
  const claims = tokens.claims();
  user.claims = claims;
  user.access_token = tokens.access_token;
  user.refresh_token = tokens.refresh_token;
  user.expires_at = claims.exp;
}

async function upsertUser(claims) {
  // Since we're using our existing storage, let's adapt it to our needs
  const userExists = await storage.getUserByUsername(claims.email);
  
  if (userExists) {
    await storage.updateUser(userExists.id, {
      name: `${claims.first_name || ''} ${claims.last_name || ''}`.trim(),
      profileImage: claims.profile_image_url
    });
    return userExists;
  } else {
    return await storage.createUser({
      name: `${claims.first_name || ''} ${claims.last_name || ''}`.trim(),
      email: claims.email,
      password: null, // No password for Replit auth users
      googleId: null,
      replitId: claims.sub,
      profileImage: claims.profile_image_url
    });
  }
}

async function setupAuth(app) {
  app.set("trust proxy", 1);
  app.use(getSession());
  app.use(passport.initialize());
  app.use(passport.session());

  const client = await getOidcConfig();

  const verify = async (tokenSet, userinfo, done) => {
    try {
      const user = {};
      updateUserSession(user, tokenSet);
      await upsertUser(tokenSet.claims());
      done(null, user);
    } catch (err) {
      done(err);
    }
  };

  for (const domain of process.env.REPLIT_DOMAINS.split(",")) {
    const strategy = new Strategy(
      {
        client,
        params: {
          scope: "openid email profile offline_access"
        }
      },
      verify
    );
    passport.use(`replitauth:${domain}`, strategy);
  }

  passport.serializeUser((user, cb) => cb(null, user));
  passport.deserializeUser((user, cb) => cb(null, user));

  app.get("/api/login", (req, res, next) => {
    passport.authenticate(`replitauth:${req.hostname}`, {
      prompt: "login consent",
      scope: ["openid", "email", "profile", "offline_access"],
    })(req, res, next);
  });

  app.get("/api/callback", (req, res, next) => {
    passport.authenticate(`replitauth:${req.hostname}`, {
      successReturnToOrRedirect: "/",
      failureRedirect: "/api/login",
    })(req, res, next);
  });

  app.get("/api/logout", (req, res) => {
    req.logout((err) => {
      if (err) {
        console.error("Logout error:", err);
        return res.redirect('/');
      }
      res.redirect('/');
    });
  });
}

const isAuthenticated = async (req, res, next) => {
  const user = req.user;

  if (!req.isAuthenticated() || !user || !user.expires_at) {
    return res.status(401).json({ message: "Unauthorized" });
  }

  const now = Math.floor(Date.now() / 1000);
  if (now <= user.expires_at) {
    return next();
  }

  const refreshToken = user.refresh_token;
  if (!refreshToken) {
    return res.redirect("/api/login");
  }

  try {
    const client = await getOidcConfig();
    const tokenResponse = await client.refresh(refreshToken);
    updateUserSession(user, tokenResponse);
    return next();
  } catch (error) {
    console.error("Token refresh error:", error);
    return res.redirect("/api/login");
  }
};

module.exports = {
  setupAuth,
  isAuthenticated,
  getSession
};