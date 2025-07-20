# Claims Processing System - Skills Demonstration

🚀 **Built for job application showcase** - A comprehensive microservices-based claims processing system demonstrating full-stack expertise with modern technologies including ASP.NET Core Blazor, gRPC, PostgreSQL, and Docker.

🌐 **Live Demo:** <a href="https://unum.forbush.biz" target="_blank">unum.forbush.biz</a>

## 💼 What This Demonstrates

This project showcases professional software engineering skills through:
- ⚡ **Rapid Development** - Complex enterprise application built quickly
- 🏗️ **Architecture Design** - Microservices with proper separation of concerns
- 💾 **Database Engineering** - PostgreSQL with Entity Framework migrations
- 🔐 **Security Implementation** - JWT authentication, role-based authorization
- 🐳 **DevOps Skills** - Docker containerization and deployment automation
- 🎨 **Full-Stack Development** - Blazor Server frontend with responsive UI
- 🧩 **System Integration** - gRPC services, message queues, and API gateways

## 🛠️ Technologies Showcased

**Backend:** ASP.NET Core 8 • Entity Framework Core • AWS Aurora PostgreSQL • gRPC • JWT Authentication  
**Frontend:** Blazor Server • Bootstrap 5 • JavaScript • SignalR  
**Infrastructure:** Docker • Docker Compose • Nginx • RabbitMQ  
**Cloud:** AWS Aurora PostgreSQL • AWS RDS • AWS VPC Security Groups  
**Development:** Git • CI/CD • RESTful APIs • Microservices Architecture

## 🏗️ Architecture

This system follows a microservices architecture with the following components:

- **Claims Processor**: gRPC service for claim submission and management
- **Document Service**: gRPC service for document processing and metadata extraction
- **API Gateway**: REST API gateway with authentication and routing
- **Frontend**: Blazor Server application with responsive UI and real-time updates
- **Database**: AWS Aurora PostgreSQL cluster for scalable data persistence
- **Message Queue**: RabbitMQ for event-driven communication
- **Reverse Proxy**: Nginx with SSL termination and load balancing

## 🚀 Quick Start

### Prerequisites

- Docker and Docker Compose
- Git
- PowerShell (Windows) or Bash (Linux/macOS)

### Development Setup

#### For Windows (PowerShell):
```powershell
# Clone and start the system
git clone <repository-url>
cd claims-processing-system
./scripts/dev-init.ps1
```

#### For Linux/macOS (Bash):
```bash
# Clone and start the system
git clone <repository-url>
cd claims-processing-system
./scripts/dev-start.sh
```

### Access Points

- **Frontend**: https://localhost
- **API Gateway**: https://localhost/api/health
- **RabbitMQ Management**: http://localhost:15672


### Demo Credentials

- **john.doe@example.com** / password
- **jane.smith@example.com** / password
- **RabbitMQ**: claims / claims123

## 📁 Project Structure

```
project-root/
├── docker-compose.yml          # Main orchestration file
├── nginx/
│   ├── default.conf           # Nginx configuration
│   └── ssl/                   # SSL certificates
├── services/
│   ├── api-gateway/           # REST API Gateway (Node.js)
│   ├── claims-processor/      # Claims gRPC Service (Go)
│   └── doc-service/          # Document gRPC Service (Go)
├── frontend/                  # React TypeScript App
├── scripts/
│   ├── dev-init.ps1          # Windows development script
│   ├── dev-start.sh          # Linux/macOS development script
│   ├── clean-containers.sh   # Container management
│   └── generate-ssl.sh       # SSL certificate generation
└── storage/                   # File storage directory
```

## 🛠️ Development

### Available Scripts

#### Windows PowerShell:
```powershell
# Start development environment
./scripts/dev-init.ps1

# Clean and restart
./scripts/dev-init.ps1 -Clean

# View logs
./scripts/dev-init.ps1 -Logs
```

#### Linux/macOS Bash:
```bash
# Start development environment
./scripts/dev-start.sh

# Container management
./scripts/clean-containers.sh --help
./scripts/clean-containers.sh --status
./scripts/clean-containers.sh --logs
./scripts/clean-containers.sh --clean
```

### Service Management

```bash
# View all services
docker-compose ps

# View logs for specific service
docker-compose logs -f claims-processor

# Restart a service
docker-compose restart api-gateway

# Stop all services
docker-compose down
```

## 🧪 Testing

### Unit Tests
```bash
# Test Go services
cd services/claims-processor && go test ./...
cd services/doc-service && go test ./...

# Test Node.js API Gateway
cd services/api-gateway && npm test

# Test React Frontend
cd frontend && npm test
```

### Integration Tests
```bash
# Health check all services
curl https://localhost/api/health

# Test claim submission
curl -X POST https://localhost/api/claims/submit \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"claimType":"Auto Insurance","description":"Test claim","amount":1000,"incidentDate":"2024-01-15"}'
```

## 🔒 Security Features

- **HTTPS with TLS 1.2/1.3**: All communications encrypted
- **JWT Authentication**: Secure API access
- **Rate Limiting**: Protection against abuse
- **Security Headers**: HSTS, XSS protection, etc.
- **Input Validation**: Comprehensive request validation
- **Container Security**: Non-root containers, minimal images

## 📊 Monitoring & Logging

### Container Health
```bash
./scripts/clean-containers.sh --health
```

### Service Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f claims-processor
```

### Metrics
- Nginx status: http://localhost:8090/nginx_status (internal only)
- RabbitMQ management: http://localhost:15672

## 🚀 Deployment

### CI/CD Pipeline

The project includes GitHub Actions for:
- Automated testing
- Code linting and security scanning
- Docker image building
- Staging and production deployment

### Production Deployment

1. Build production images:
```bash
docker-compose -f docker-compose.prod.yml build
```

2. Deploy with environment-specific configurations:
```bash
docker-compose -f docker-compose.prod.yml up -d
```

### Environment Variables

Key environment variables for production:

```env
# Database
DATABASE_URL=postgresql://user:pass@host:5432/dbname

# RabbitMQ
RABBITMQ_URL=amqp://user:pass@host:5672/

# JWT Secret
JWT_SECRET=your-secure-secret-key

# API URLs
CLAIMS_PROCESSOR_URL=claims-processor:50051
DOC_SERVICE_URL=doc-service:50052
REACT_APP_API_URL=https://your-domain.com
```

## 📋 API Documentation

### Authentication Endpoints
- `POST /auth/login` - User login

### Claims Endpoints
- `POST /claims/submit` - Submit new claim
- `GET /claims` - List user claims
- `GET /claims/:id` - Get claim details
- `PUT /claims/:id/status` - Update claim status
- `POST /claims/:id/documents` - Upload documents

### Health Check
- `GET /health` - Service health status

## 🔧 Troubleshooting

### Common Issues

1. **Port conflicts**: Ensure ports 80, 443, 5432, 5672, 15672 are available
2. **SSL certificates**: Run `./scripts/generate-ssl.sh` if HTTPS fails
3. **Container startup**: Check logs with `docker-compose logs <service>`
4. **Database connection**: Verify PostgreSQL is healthy

### Support

- Check service status: `./scripts/clean-containers.sh --status`
- View logs: `docker-compose logs -f`
- Health checks: `./scripts/clean-containers.sh --health`

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests: `npm test` and `go test ./...`
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🏆 Features Implemented

- ✅ Microservices architecture with gRPC
- ✅ REST API Gateway with authentication
- ✅ **Blazor Server** frontend with SignalR
- ✅ PostgreSQL database with Entity Framework migrations
- ✅ RabbitMQ message queuing
- ✅ Document processing (PDF, JPEG upload/viewing)
- ✅ Nginx reverse proxy with SSL
- ✅ Docker containerization
- ✅ Development automation scripts
- ✅ JWT authentication with role-based authorization
- ✅ Multi-role dashboard system (Patient, Provider, Admin, Claims Adjuster)
- ✅ Medical coding integration (ICD-10, CPT)
- ✅ Claims lifecycle management