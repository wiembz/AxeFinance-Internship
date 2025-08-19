# Tech Dashboard - System Architecture

## 📋 Overview

The Tech Dashboard is a comprehensive problem-solving and project management platform built using modern enterprise-grade architecture patterns. It enables organizations to track technical problems, manage solutions, and maintain organizational structure through departments and projects.

## 🏗️ Architecture Pattern

### Clean Architecture Implementation
The system follows **Clean Architecture** principles with clear separation of concerns across four distinct layers:

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                   │
│                   (API Controllers)                     │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────┴───────────────────────────────────┐
│                  Application Layer                      │
│              (DTOs, Interfaces, Services)               │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────┴───────────────────────────────────┐
│                Infrastructure Layer                     │
│        (Data Access, External Services, Auth)          │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────┴───────────────────────────────────┐
│                    Domain Layer                         │
│              (Entities, Enums, Business Rules)         │
└─────────────────────────────────────────────────────────┘
```

## 🎯 Core Components

### 1. Domain Layer (`TechDashboardAPI.Domain`)
**Purpose**: Contains core business entities and rules

**Key Components**:
- **Entities**: User, Department, Project, Problem, Solution, ProblemForm, FormField
- **Enums**: UserRole, ProblemPriority, SolutionStatus, ProblemStatus
- **Value Objects**: Custom form field configurations
- **Business Rules**: Entity validation, relationship constraints

**Key Entities Relationships**:
```
User ────────┐
│            │
│     Department ──── Project ──── Problem ──── Solution
│            │         │             │
│            └─────────┴─────────────┴──── Forms & CustomFields
│
└──── ProblemLikes, Solutions (Creator)
```

### 2. Application Layer (`TechDashboardAPI.Application`)
**Purpose**: Defines application services interfaces and DTOs

**Key Components**:
- **DTOs**: Request/Response objects for all operations
- **Interfaces**: Service contracts (IAuthService, IDepartmentService, etc.)
- **Responses**: Standardized ApiResponse<T> wrapper pattern
- **Mappings**: Entity-to-DTO conversion logic

**DTO Categories**:
- **Auth DTOs**: LoginDto, RegisterDto, UserDto
- **Management DTOs**: DepartmentDto, ProjectDto, UserManagementDto
- **Problem DTOs**: CreateProblemDto, ProblemDto, ProblemFilterDto
- **Solution DTOs**: SolutionDto, CreateSolutionDto, SolutionApprovalDto

### 3. Infrastructure Layer (`TechDashboardAPI.Infrastructure`)
**Purpose**: Implements external concerns and data access

**Key Components**:

#### Data Access
- **ApplicationDbContext**: Entity Framework Core context
- **Migrations**: Database schema versioning
- **Configurations**: Entity relationship configurations

#### Authentication & Security
- **JWT Authentication**: Token-based stateless authentication (issuer/audience/signature validated, zero clock skew in non-dev)
- **Role-Based Authorization**: Hierarchical permission system using ClaimTypes.Role
- **Fake ADP Service**: Development-time authentication simulation (disabled in production)
- **Password Security**: PBKDF2-SHA256 with per-user salt and iterations (legacy hashes auto-upgraded on login)

#### External Services
- **Email Service**: SMTP-based notifications
- **File Storage Service**: Secure file upload/download
- **Caching Service**: Performance optimization layer

#### Middleware
- **Global Exception Handling**: Centralized error management
- **Security Headers**: CORS, security headers management
- **Request Logging**: Performance and audit logging

### 4. Presentation Layer (`TechDashboardAPI.API`)
**Purpose**: HTTP API endpoints and configuration

**Key Components**:
- **Controllers**: RESTful API endpoints with proper HTTP semantics
- **Middleware Pipeline**: Request/response processing chain
- **Configuration**: Dependency injection, authentication, Swagger
- **Validation**: Input validation and model binding

## 🔐 Security Architecture

### Authentication Flow
```
1. User Login Request
   ↓
2. Credentials Validation (Password + PBKDF2-SHA256)
   ↓
3. JWT Token Generation (with Role Claims)
   ↓
4. Token Return to Client
   ↓
5. Subsequent Requests (Authorization Header)
   ↓
6. Token Validation & Authorization
```

### Role Hierarchy
- **SuperAdmin** (Highest): Full system control, user management
- **Admin**: Department/project management, user oversight  
- **Contributor**: Create problems/solutions, edit own content
- **Viewer** (Lowest): Read-only access to authorized content

### Security Features
- **JWT Tokens**: Stateless, signed (HS256), strict issuer/audience validation
- **Password Security**: PBKDF2-SHA256 with per-user salts and versioned hashes
- **Input Validation**: Comprehensive server-side validation
- **Authorization**: Role-based access control via policies and RequireRole
- **CORS Policy**: Controlled cross-origin access (AllowedOrigins in production)
- **Security Headers**: CSP/HSTS/COOP/CORP with optional Swagger relaxations in production
- **File Upload Security**: Type whitelist, size limits, path normalization/traversal protection

## 💾 Data Architecture

### Database Design
**Technology**: Entity Framework Core with SQL Server

**Key Design Patterns**:
- **Soft Delete**: IsActive flag instead of physical deletion
- **Audit Trails**: Created/Modified timestamps and user tracking
- **Relationships**: Proper foreign key constraints with cascade rules
- **Indexes**: Performance optimization on frequently queried columns

### Entity Relationships
```sql
-- Core Entities
Users (1) ────────── (*) Departments
Departments (1) ───── (*) Projects  
Projects (1) ────── (*) Problems
Problems (1) ────── (*) Solutions

-- Cross-References
Users (*) ────────── (*) ProblemLikes
Problems (1) ────── (*) ProblemFieldValues
Forms (1) ──────── (*) FormFields
```

### Database Features
- **Connection Pooling**: Optimized database connections
- **Lazy Loading Disabled**: Explicit Include() for performance
- **Query Optimization**: Strategic indexing and query patterns
- **Transaction Management**: ACID compliance for critical operations

## 🔄 Request/Response Flow

### Typical API Request Flow
```
1. HTTP Request → API Controller
   ↓
2. Input Validation & Model Binding
   ↓  
3. JWT Authentication & Authorization
   ↓
4. Controller → Service Layer (Business Logic)
   ↓
5. Service → Repository/DbContext (Data Access)
   ↓
6. Database Query/Operation
   ↓
7. Entity → DTO Mapping
   ↓
8. ApiResponse<T> Wrapper
   ↓
9. HTTP Response (JSON)
```

## 📊 Performance Considerations

### Optimization Strategies
- **Async/Await**: Non-blocking I/O operations throughout
- **Pagination**: Large dataset handling with skip/take patterns
- **Caching**: Memory caching for frequently accessed data
- **Database Indexing**: Strategic indexes on search/filter columns
- **Lazy Loading Prevention**: Explicit relationship loading
- **Connection Pooling**: Efficient database connection management

### Scalability Features
- **Stateless Design**: JWT tokens enable horizontal scaling
- **Service Layer**: Business logic separation for microservices migration
- **Clean Architecture**: Easy component replacement/enhancement
- **Configuration-Based**: Environment-specific settings management

## 🧪 Testing Architecture

### Testing Strategy
- **Unit Tests**: Service layer business logic testing
- **Integration Tests**: Database operations and API endpoints
- **Authentication Tests**: JWT token validation and role-based access
- **Mock Services**: Isolated testing with proper mocking

### Test Categories
- **AuthServiceTests**: Authentication and authorization logic
- **ControllerTests**: API endpoint behavior and responses
- **ServiceTests**: Business logic validation
- **DatabaseTests**: Entity operations and relationships

## 🔧 Development Environment

### Development Features
- **Fake ADP Service**: Simulates enterprise authentication
- **In-Memory Database**: Fast testing without external dependencies
- **Swagger Integration**: Interactive API documentation and testing
- **Hot Reload**: Development-time code changes without restart
- **Detailed Logging**: Development-friendly error messages and debugging

## 📚 Technology Stack

### Backend Framework
- **.NET 8**: Latest framework with performance improvements
- **ASP.NET Core Web API**: RESTful API framework
- **Entity Framework Core**: ORM with Code-First approach
- **PBKDF2-SHA256**: Password hashing (versioned, per-user salt)
- **JWT**: JSON Web Tokens for authentication

### Development Tools
- **Swagger/OpenAPI**: API documentation and testing
- **Entity Framework CLI**: Database migrations and tooling
- **Visual Studio**: Primary IDE with debugging support
- **Postman/Thunder Client**: API testing and development

This architecture provides a solid foundation for enterprise-level problem management with room for future enhancements like real-time notifications, advanced analytics, and microservices migration.

