# Local Development Setup Guide

This guide explains how to set up the project for local development using the example files.

## 📋 Prerequisites

- Docker and Docker Compose
- Git
- .NET 8.0 SDK (for local development)
- Node.js (if working on frontend components)

## 🚀 Quick Setup

### 1. Clone the Repository
```bash
git clone git@github.com:mcnfch/blazor-healthcare-portal.git
cd blazor-healthcare-portal
```

### 2. Set Up Environment Configuration
```bash
# Copy environment template
cp examples/.env.example .env

# Edit .env with your local settings
nano .env
```

### 3. Set Up Docker Override (Optional)
```bash
# Copy Docker override template for local customization
cp examples/docker-compose.override.yml.example docker-compose.override.yml

# Edit ports and settings as needed
nano docker-compose.override.yml
```

### 4. Generate SSL Certificates
```bash
# Generate self-signed certificates for HTTPS
./scripts/generate-ssl.sh
```

### 5. Set Up Production Configuration (Optional)
```bash
# For production deployment
cp examples/appsettings.Production.json.example services/frontend-blazor/appsettings.Production.json

# Edit with production database and security settings
nano services/frontend-blazor/appsettings.Production.json
```

### 6. Start the Application
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f
```

### 7. Initialize Database (First Run)
```bash
# Import medical codes (optional)
python3 import_basic_codes.py

# Or import full ICD-10 dataset
python3 import_full_icd10.py
```

## 🔗 Access Points

- **Application**: https://localhost
- **Health Check**: https://localhost/api/health
- **RabbitMQ Management**: http://localhost:15672

## 👤 Demo Accounts

- **Patient**: patient@example.com / password
- **Admin**: admin@example.com / password
- **Claims Adjuster**: adjuster@example.com / password
- **Provider**: provider@example.com / password

## 🔧 Development Commands

```bash
# View service status
docker-compose ps

# View logs for specific service
docker-compose logs -f frontend

# Restart a service
docker-compose restart frontend

# Stop all services
docker-compose down

# Clean restart
docker-compose down -v && docker-compose up -d
```

## 📁 Important Files Created

After setup, you'll have these additional files (not tracked in git):

- `.env` - Environment configuration
- `docker-compose.override.yml` - Local Docker customizations  
- `nginx/ssl/cert.pem` - SSL certificate
- `nginx/ssl/key.pem` - SSL private key
- `services/frontend-blazor/appsettings.Production.json` - Production config

## 🚨 Security Notes

- Never commit `.env` files or production configurations
- Change default passwords in production
- Use real SSL certificates for production deployment
- Update JWT secrets with secure random values

## 🐛 Troubleshooting

### Port Conflicts
If default ports are in use, edit `docker-compose.override.yml`:
```yaml
services:
  postgres:
    ports:
      - "5433:5432"  # Changed from 5432
```

### SSL Issues
Regenerate certificates:
```bash
rm nginx/ssl/*.pem
./scripts/generate-ssl.sh
```

### Database Issues
Reset database:
```bash
docker-compose down -v
docker-compose up -d postgres
# Wait for postgres to start, then:
docker-compose up -d
```