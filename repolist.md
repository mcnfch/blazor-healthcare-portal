# Repository File List - Essential Files Only

This document lists all essential files that should be included in the `blazor-healthcare-portal` repository for the skills demonstration.

## 📋 Essential Repository Files

### 🔧 Project Configuration
```
/.gitignore                           # Git ignore rules
/README.md                           # Project documentation and skills showcase
/docker-compose.yml                  # Container orchestration
/repolist.md                        # This file - documents repository contents
```

### 🏗️ Infrastructure & Deployment
```
/.github/workflows/ci-cd.yml        # CI/CD pipeline configuration
/nginx/default.conf                 # Nginx reverse proxy configuration
/nginx/ssl/cert.pem                 # SSL certificate
/nginx/ssl/key.pem                  # SSL private key
/scripts/generate-ssl.sh            # SSL certificate generation script
/scripts/init-db.sql                # Database initialization script
/scripts/init-healthcare-db.sql     # Healthcare-specific database setup
```

### 🗄️ Database & Data Management
```
/database_helpers.sql               # Database utility functions
/import_basic_codes.py              # Medical codes import script
/import_full_icd10.py              # ICD-10 codes import script
/import_medical_codes.py            # Comprehensive medical codes import
```

### 🔧 Microservices Architecture

#### API Gateway Service
```
/services/api-gateway/
├── ApiGateway.csproj               # C# project file
├── Dockerfile                     # Container definition
├── Models/
│   ├── DTOs.cs                    # Data transfer objects
│   └── HealthcareDbContext.cs     # Database context
├── Protos/
│   ├── claims.proto               # gRPC claims service definition
│   └── document.proto             # gRPC document service definition
└── Services/
    ├── AuthService.cs             # Authentication service
    └── GrpcClients.cs            # gRPC client implementations
```

#### Claims Processor Service
```
/services/claims-processor/
├── ClaimsProcessor.csproj         # C# project file
├── Dockerfile                    # Container definition
├── Program.cs                     # Main application entry point
├── appsettings.json              # Configuration
├── appsettings.Development.json   # Development configuration
├── Models/
│   └── HealthcareDbContext.cs    # Database context
├── Protos/
│   └── claims.proto              # gRPC service definition
└── Services/
    ├── ClaimsService.cs          # Claims processing logic
    └── Messaging/
        ├── IMessagePublisher.cs  # Message queue interface
        └── RabbitMQPublisher.cs  # RabbitMQ implementation
```

#### Document Service
```
/services/doc-service/
├── DocumentService.csproj        # C# project file
├── Dockerfile                   # Container definition
├── Program.cs                    # Main application entry point
├── appsettings.json             # Configuration
├── Models/
│   └── DocumentDbContext.cs     # Database context
├── Protos/
│   └── document.proto           # gRPC service definition
└── Services/
    ├── DocumentProcessor.cs     # Document processing logic
    └── DocumentService.cs       # Document service implementation
```

### 🎨 Frontend Application (Blazor Server)
```
/services/frontend-blazor/
├── BlazorApp.csproj             # C# project file
├── Dockerfile                   # Container definition
├── Program.cs                   # Application startup and configuration
├── App.razor                    # Root application component
├── _Imports.razor              # Global imports
├── appsettings.json            # Configuration
│
├── Components/                  # Reusable UI components
│   ├── AdminDashboard.razor
│   ├── CustomerServiceDashboard.razor
│   ├── PatientDashboard.razor
│   └── ProviderDashboard.razor
│
├── Controllers/                 # MVC controllers for API endpoints
│   └── DocumentsController.cs  # Document view/download API
│
├── Models/                      # Data models and DTOs
│   ├── DTOs.cs                 # Data transfer objects
│   └── HealthcareDbContext.cs  # Entity Framework database context
│
├── Pages/                       # Blazor pages/routes
│   ├── Dashboard.razor         # Main dashboard
│   ├── Login.razor            # Authentication page
│   ├── Profile.razor          # User profile management
│   │
│   ├── Claims.razor           # Patient claims view
│   ├── ClaimDetails.razor     # Individual claim details
│   ├── SubmitClaim.razor      # Claim submission form
│   │
│   ├── Providers.razor        # Provider directory
│   ├── ProviderDetails.razor  # Provider information
│   │
│   ├── Documents.razor        # Document management
│   │
│   ├── Admin*.razor          # Admin-specific pages (7 files)
│   ├── Adjuster*.razor       # Claims adjuster pages (7 files)
│   │
│   └── Shared/
│       ├── _Host.cshtml       # Blazor host page
│       └── _Layout.cshtml     # Layout template
│
├── Services/                   # Business logic services
│   ├── AuthService.cs         # Authentication service
│   ├── ClaimsService.cs       # Claims management
│   ├── CustomAuthenticationStateProvider.cs
│   ├── DashboardService.cs    # Dashboard data
│   ├── FileDocumentService.cs # Document handling
│   └── GrpcClients.cs        # gRPC client implementations
│
├── Shared/                     # Shared UI components
│   ├── MainLayout.razor       # Main application layout
│   └── NavMenu.razor         # Navigation menu
│
├── Protos/                     # gRPC service definitions
│   ├── claims.proto
│   └── document.proto
│
└── wwwroot/                    # Static web assets
    ├── css/
    │   └── site.css           # Application styles
    ├── favicon.ico
    ├── favicon.svg
    └── icon-192.png
```

## 📊 File Count Summary

- **Total Essential Files**: ~85 files
- **Microservices**: 3 services (API Gateway, Claims Processor, Document Service)
- **Frontend Pages**: 21 Blazor pages (Patient + Admin + Adjuster views)
- **Configuration Files**: 8 files
- **Database Scripts**: 4 files
- **Infrastructure**: 6 files

## 🚫 Archived Files (Not for Repository)

The following files have been moved to `/archive/` as they are not essential for the skills demonstration:

- Development/debugging tools
- MCP server configurations
- Temporary data files
- Legacy React frontend (replaced by Blazor)
- Raw medical code data files

## 🎯 Repository Purpose

This file structure demonstrates:
- **Microservices Architecture**: Clean separation of concerns
- **Full-Stack Development**: Backend services + frontend application
- **Professional Organization**: Proper project structure
- **Production Ready**: Docker containers, SSL, database migrations
- **Healthcare Domain**: Medical coding, claims processing, document management
- **Enterprise Features**: Authentication, authorization, role-based access

## 📝 Notes

- All files listed are actively used in the application
- Each service has its own Dockerfile for containerization
- gRPC protobuf files are shared between services
- Frontend follows Blazor Server best practices
- Database context files enable Entity Framework migrations
- Configuration files support different environments