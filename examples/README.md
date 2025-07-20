# Example Configuration Files

This directory contains template and example files for setting up the healthcare claims portal.

## 📁 Files Included

### Environment Configuration
- **`.env.example`** - Environment variables template
- **`appsettings.Production.json.example`** - Production configuration template
- **`docker-compose.override.yml.example`** - Docker development overrides

### Setup Guide
- **`local-development-setup.md`** - Complete setup instructions

## 🚀 Quick Start

1. **Copy environment file:**
   ```bash
   cp examples/.env.example .env
   ```

2. **Edit with your settings:**
   ```bash
   nano .env
   ```

3. **Generate SSL certificates:**
   ```bash
   ./scripts/generate-ssl.sh
   ```

4. **Start the application:**
   ```bash
   docker-compose up -d
   ```

## 🔐 Security Notes

- These are **example files only** - never use in production without changes
- Change all default passwords and secrets
- Use environment-specific configurations
- Never commit actual `.env` or production config files

## 📋 Required Files Not in Repository

The following files are needed for functionality but ignored by git:

### SSL Certificates (auto-generated)
- `nginx/ssl/cert.pem`
- `nginx/ssl/key.pem`

### Environment Configuration (copy from examples)
- `.env`
- `docker-compose.override.yml` (optional)
- `services/frontend-blazor/appsettings.Production.json` (production only)

### Runtime Directories (auto-created)
- `uploads/` - User uploaded documents
- `storage/` - Application storage

See `local-development-setup.md` for detailed instructions.