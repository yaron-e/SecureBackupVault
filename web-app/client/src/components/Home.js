import React from 'react';
import { Link } from 'react-router-dom';

const Home = () => {
  return (
    <div className="home-container">
      <section className="hero-section">
        <div className="hero-content">
          <h1>Secure Your Digital Life with Military-Grade Encryption</h1>
          <p>
            Protect your most important files with our cascade encryption technology,
            combining AES-256, Twofish, and Serpent algorithms for unmatched security.
          </p>
          <div className="hero-buttons">
            <Link to="/login" className="primary-btn">Get Started</Link>
            <a href="#features" className="secondary-btn">Learn More</a>
          </div>
        </div>
        <div className="hero-image">
          <div className="encryption-animation">
            <div className="file-icon"></div>
            <div className="encryption-layers">
              <div className="layer layer-1">AES-256</div>
              <div className="layer layer-2">Twofish</div>
              <div className="layer layer-3">Serpent</div>
            </div>
            <div className="secure-icon"></div>
          </div>
        </div>
      </section>

      <section id="features" className="features-section">
        <h2>Why Choose Our Backup Solution?</h2>
        <div className="features-grid">
          <div className="feature-card">
            <div className="feature-icon encryption-icon"></div>
            <h3>Triple-Layer Encryption</h3>
            <p>
              Your files are protected with cascade encryption using AES-256, Twofish,
              and Serpent algorithms - the same level of security used by government agencies.
            </p>
          </div>
          <div className="feature-card">
            <div className="feature-icon cloud-icon"></div>
            <h3>Secure Cloud Storage</h3>
            <p>
              Files are safely stored in AWS S3 with encryption keys managed separately
              in AWS KMS for maximum protection against unauthorized access.
            </p>
          </div>
          <div className="feature-card">
            <div className="feature-icon auto-icon"></div>
            <h3>Automatic Backups</h3>
            <p>
              Our Windows desktop app monitors your important files and folders,
              automatically backing them up whenever changes are detected.
            </p>
          </div>
          <div className="feature-card">
            <div className="feature-icon access-icon"></div>
            <h3>Access Anywhere</h3>
            <p>
              Manage your backups and restore files from any device through our
              secure web interface - your data is always at your fingertips.
            </p>
          </div>
        </div>
      </section>

      <section className="how-it-works-section">
        <h2>How It Works</h2>
        <div className="steps-container">
          <div className="step">
            <div className="step-number">1</div>
            <h3>Install the Desktop App</h3>
            <p>Download and install our Windows application on your computer.</p>
          </div>
          <div className="step">
            <div className="step-number">2</div>
            <h3>Select Files to Protect</h3>
            <p>Choose which files and folders you want to keep securely backed up.</p>
          </div>
          <div className="step">
            <div className="step-number">3</div>
            <h3>Automatic Encryption</h3>
            <p>Your files are automatically encrypted with three layers of protection.</p>
          </div>
          <div className="step">
            <div className="step-number">4</div>
            <h3>Secure Cloud Storage</h3>
            <p>Encrypted files are securely uploaded and stored in AWS S3.</p>
          </div>
        </div>
      </section>

      <section className="cta-section">
        <div className="cta-content">
          <h2>Ready to Secure Your Files?</h2>
          <p>Get started today with our powerful backup solution.</p>
          <Link to="/login" className="primary-btn">Create an Account</Link>
        </div>
      </section>
    </div>
  );
};

export default Home;