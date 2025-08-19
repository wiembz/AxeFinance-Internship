# Tech Dashboard API - Comprehensive Testing Guide

## 🎯 Overview

This guide provides complete API testing instructions for the Tech Dashboard system. All endpoints are RESTful and return JSON responses using a standardized `ApiResponse<T>` pattern.

## 🔐 Authentication System

### Base URLs
- Development (launchSettings): `http://localhost:5108` and `https://localhost:7130`
- API prefix: `/api`
- Production: `https://yourdomain.com`

### Response Format
All API endpoints use a consistent response format:

```json
// Success Response
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { /* actual response data */ }
}

// Error Response  
{
  "success": false,
  "message": "Error description",
  "errors": ["Detailed error 1", "Detailed error 2"]
}
```

## 🚀 Getting Started

### Step 1: Check System Health
```http
GET /api/health
Content-Type: application/json
```

Alternative health endpoints (middleware-based):
```http
GET /health/live   // liveness (no DB)
GET /health/ready  // readiness (includes DB check)
```

### Step 2: Check SuperAdmin Status
```http
GET /api/auth/superadmin-exists
```

**Response**:
```json
{
  "success": true,
  "message": "SuperAdmin status retrieved",
  "data": {
    "exists": false  // true if SuperAdmin already created
  }
}
```

## 🔑 Authentication Endpoints

### 1. Register SuperAdmin (First User Only)
```http
POST /api/auth/register
Content-Type: application/json

{
  "firstName": "Super",
  "lastName": "Admin",
  "email": "admin@techdashboard.com",
  "password": "Admin@123456"
}
```

**Success Response**:
```json
{
  "success": true,
  "message": "User registered successfully as SuperAdmin",
  "data": {
    "id": 1,
    "firstName": "Super",
    "lastName": "Admin", 
    "email": "admin@techdashboard.com",
    "role": "SuperAdmin"
  }
}
```

### 2. Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@techdashboard.com",
  "password": "Admin@123456"
}
```

### 3. Fake ADP Login (Development Only)
```http
POST /api/auth/fakeadp/login
Content-Type: application/json

{
  "email": "admin@techdashboard.com",
  "password": "Admin@123456"
}
```

Note: Fake ADP must be enabled in configuration (Disabled in Production).

**Login Success Response**:
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": 1,
      "firstName": "Super",
      "lastName": "Admin",
      "email": "admin@techdashboard.com",
      "role": "SuperAdmin"
    }
  }
}
```

### 4. Get Current User Profile
```http
GET /api/auth/current-user
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

## 👥 User Management

### Get All Users (Admin+ Required)
```http
GET /api/users?page=1&pageSize=10&search=john&departmentId=1&role=Admin&isActive=true
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

**Query Parameters**:
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 10, max: 100)
- `search`: Search by name or email
- `departmentId`: Filter by department
- `role`: Filter by role (SuperAdmin, Admin, Contributor, Viewer)
- `isActive`: Filter by active status (true/false)

### Get User by ID
```http
GET /api/users/1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Create User (Admin+ Required)
```http
POST /api/users
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@company.com",
  "password": "Str0ng#Passw0rd!",
  "role": "Contributor",
  "departmentId": 1
}
```

### Update User Profile (Self or Admin+)
```http
PUT /api/users/1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "firstName": "John Updated",
  "lastName": "Doe Updated",
  "email": "john.updated@company.com",
  "departmentId": 2
}
```

### Update User Role (Admin+ Required)
```http
PUT /api/users/1/role
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "role": "Admin"
}
```

### Activate/Deactivate User (Admin+ Required)
```http
# Activate user
PUT /api/users/1/activate
Authorization: Bearer YOUR_JWT_TOKEN_HERE

# Deactivate user
PUT /api/users/1/deactivate
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

## 🏢 Department Management

### Get All Departments
```http
GET /api/departments?page=1&pageSize=10&search=engineering&includeInactive=false
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Get Department by ID
```http
GET /api/departments/1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Create Department (Admin+ Required)
```http
POST /api/departments
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "name": "Information Technology",
  "description": "IT Department managing all technology solutions and infrastructure"
}
```

### Update Department (Admin+ Required)
```http
PUT /api/departments/1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "name": "IT & Digital Services",
  "description": "Updated IT department with expanded digital services"
}
```

### Delete Department (Admin+ Required)
```http
DELETE /api/departments/1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

## 📊 Project Management

### Get All Projects
```http
GET /api/projects?page=1&pageSize=10&departmentId=1&search=portal&isActive=true
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Get Projects by Department
```http
GET /api/projects/department/1?page=1&pageSize=10
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Get Project by ID
```http
GET /api/projects/1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Create Project (Admin+ Required)
```http
POST /api/projects
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "name": "Customer Portal Enhancement",
  "departmentId": 1
}
```

### Update Project (Admin+ Required)
```http
PUT /api/projects/1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "name": "Customer Portal v2.0",
  "departmentId": 1
}
```

### Delete Project (Admin+ Required)
```http
DELETE /api/projects/1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

## 🔧 Problem Management

### Get All Problems
```http
GET /api/problems?page=1&pageSize=10&projectId=1&search=login&priority=High&status=Open&createdBy=1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

**Query Parameters**:
- `projectId`: Filter by project
- `search`: Search in title and description
- `priority`: Low, Medium, High, Critical
- `status`: Open, InProgress, Resolved, Closed
- `createdBy`: Filter by creator user ID

### Get Problem by ID
```http
GET /api/problems/1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Create Problem
```http
POST /api/problems
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "title": "Login Performance Issue",
  "description": "Users experiencing slow login times during peak hours",
  "priority": "High",
  "projectId": 1,
  "tags": ["performance", "authentication", "login"],
  "customFields": {
    "impact": "50+ users affected",
    "environment": "Production",
    "browserInfo": "Chrome 91+"
  }
}
```

### Create Problem with File Upload
```http
POST /api/problems
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: multipart/form-data

title=Login Performance Issue
description=Users experiencing slow login times
priority=High
projectId=1
tags=performance,authentication
attachment=[FILE_DATA]
customFields={"impact":"50+ users","environment":"Production"}
```

### Update Problem (Creator or Admin+)
```http
PUT /api/problems/1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "title": "Login Performance Issue - Updated",
  "description": "Updated description with more details",
  "priority": "Critical",
  "status": "InProgress",
  "tags": ["performance", "authentication", "urgent"]
}
```

### Delete Problem (Creator or Admin+)
```http
DELETE /api/problems/1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Like/Unlike Problem
```http
# Like problem
POST /api/problems/1/like
Authorization: Bearer YOUR_JWT_TOKEN_HERE

# Unlike problem
DELETE /api/problems/1/like
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Get Problem Tags
```http
GET /api/problems/tags?search=auth
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

## ⚡ Solution Management

### Get All Solutions
```http
GET /api/solutions?page=1&pageSize=10&problemId=1&status=Pending&createdBy=1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Get Solutions for Problem
```http
GET /api/solutions/problem/1?page=1&pageSize=10
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Create Solution
```http
POST /api/solutions
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "title": "Database Query Optimization",
  "description": "Optimized login queries and added database indexing",
  "problemId": 1,
  "implementation": "Applied new indexes on user authentication tables and optimized JOIN queries",
  "testing": "Performed load testing with 100 concurrent users, reduced login time by 75%",
  "azureDevOpsLink": "https://dev.azure.com/company/project/_workitems/edit/12345"
}
```

### Create Solution with File
```http
POST /api/solutions
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: multipart/form-data

title=Database Query Optimization
description=Optimized login queries
problemId=1
implementation=Applied new indexes
testing=Load testing performed
attachment=[FILE_DATA]
```

### Update Solution (Creator or Admin+)
```http
PUT /api/solutions/1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "title": "Updated Solution Title",
  "description": "Updated description",
  "implementation": "Updated implementation details",
  "testing": "Additional testing performed"
}
```

### Approve/Reject Solution (Admin+ Required)
```http
POST /api/solutions/1/approve
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "isApproved": true,
  "adminNotes": "Excellent solution, well tested and documented"
}
```

### Delete Solution (Creator or Admin+)
```http
DELETE /api/solutions/1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

## 📁 File Management

### Upload File
```http
POST /api/files/upload
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: multipart/form-data

file=[FILE_DATA]
```

**Success Response**:
```json
{
  "success": true,
  "message": "File uploaded successfully",
  "data": {
    "fileId": "12345",
    "fileName": "document.pdf",
    "fileSize": 1048576,
    "contentType": "application/pdf",
    "uploadedAt": "2024-01-15T10:30:00Z"
  }
}
```

### Download File
```http
GET /api/files/download/12345
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Get File Metadata
```http
GET /api/files/metadata/12345
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Delete File (Creator or Admin+)
```http
DELETE /api/files/12345
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

## 📝 Form Configuration (SuperAdmin Only)

### Get Form Fields
```http
GET /api/forms/fields?projectId=1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Create Form Field (SuperAdmin Only)
```http
POST /api/forms/fields
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "name": "impactLevel",
  "label": "Business Impact Level",
  "type": "Select",
  "isRequired": true,
  "options": ["Low", "Medium", "High", "Critical"],
  "order": 1,
  "projectId": 1
}
```

### Update Form Field (SuperAdmin Only)
```http
PUT /api/forms/fields/1
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "label": "Updated Impact Level",
  "isRequired": false,
  "options": ["Low", "Medium", "High", "Critical", "Emergency"]
}
```

## 🧪 Test Endpoints (Development)

### Basic Ping
```http
GET /api/test/ping
```

### Authentication Test
```http
GET /api/test/auth-test
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### Admin Role Test
```http
GET /api/test/admin-test
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### SuperAdmin Role Test
```http
GET /api/test/superadmin-test
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

## 📊 Quick Testing Workflow

### 1. Initial Setup
```bash
# 1. Check health
GET /api/health

# 2. Check if SuperAdmin exists
GET /api/auth/superadmin-exists

# 3. Register first user (becomes SuperAdmin)
POST /api/auth/register
{
  "firstName": "Test",
  "lastName": "Admin",
  "email": "test@example.com",
  "password": "Test@123456"
}

# 4. Login to get JWT token
POST /api/auth/login
{
  "email": "test@example.com",
  "password": "Test@123456"
}
```

### 2. Create Organizational Structure
```bash
# 1. Create department
POST /api/departments
{
  "name": "IT Department",
  "description": "Information Technology"
}

# 2. Create project
POST /api/projects
{
  "name": "Test Project",
  "description": "Test project for API testing",
  "departmentId": 1
}

# 3. Create additional user
POST /api/users
{
  "firstName": "John",
  "lastName": "Developer",
  "email": "john@example.com",
  "password": "Developer@123",
  "role": "Contributor",
  "departmentId": 1
}
```

### 3. Test Problem-Solution Workflow
```bash
# 1. Create problem
POST /api/problems
{
  "title": "Test Problem",
  "description": "This is a test problem",
  "priority": "Medium",
  "projectId": 1
}

# 2. Create solution
POST /api/solutions
{
  "title": "Test Solution",
  "description": "This is a test solution",
  "problemId": 1,
  "implementation": "Test implementation",
  "testing": "Test validation"
}

# 3. Approve solution (as Admin+)
POST /api/solutions/1/approve
{
  "isApproved": true,
  "adminNotes": "Good solution"
}
```

## 🔧 Error Handling Examples

### Common Error Responses

#### Validation Error (400)
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    "Email is required",
    "Password must be at least 8 characters"
  ]
}
```

#### Unauthorized (401)
```json
{
  "success": false,
  "message": "Authentication required",
  "errors": ["Please provide a valid JWT token"]
}
```

#### Forbidden (403)
```json
{
  "success": false,
  "message": "Insufficient permissions",
  "errors": ["SuperAdmin role required for this operation"]
}
```

#### Not Found (404)
```json
{
  "success": false,
  "message": "Resource not found",
  "errors": ["User with ID 999 not found"]
}
```

## 📋 Testing Checklist

### Authentication Testing
- [ ] SuperAdmin registration works
- [ ] Login returns valid JWT token
- [ ] Token validation works on protected endpoints
- [ ] Role-based authorization enforced
- [ ] Token expiration handled properly

### CRUD Operations Testing
- [ ] Create operations work with proper validation
- [ ] Read operations return correct data with pagination
- [ ] Update operations modify data correctly
- [ ] Delete operations (soft delete) work properly
- [ ] Proper authorization for all operations

### File Operations Testing
- [ ] File upload with validation works
- [ ] File download returns correct content
- [ ] File metadata retrieval works
- [ ] File deletion works properly

### Error Handling Testing
- [ ] Validation errors return proper 400 responses
- [ ] Authentication errors return 401
- [ ] Authorization errors return 403
- [ ] Not found errors return 404
- [ ] Server errors return 500 with safe error messages

---

## 🎯 Success Criteria

Your API testing is successful when:
- All authentication flows work properly
- CRUD operations function correctly for all entities
- File upload/download works securely
- Role-based authorization is enforced
- Error responses are consistent and informative
- Performance is acceptable under normal load

**Your Tech Dashboard API is ready for production use!**
