
# Database Schema Overview

This project uses EF Core (SQL Server). Below is a high-level schema aligned with `TechDashboardAPI.Domain` entities.

## Core Tables
- Users
  - Id (PK), FirstName, LastName, Email (unique), PasswordHash, Role, DepartmentId (FK), IsActive, CreatedAt, UpdatedAt
- Departments
  - Id (PK), Name (unique), Description, IsActive, CreatedAt, UpdatedAt
- Projects
  - Id (PK), DepartmentId (FK), Name, Description, IsActive, CreatedAt, UpdatedAt
- Problems
  - Id (PK), ProjectId (FK), Title, Description, Status, Priority, CreatedByUserId (FK), CreatedAt, UpdatedAt
- Solutions
  - Id (PK), ProblemId (FK), Title, Description, Status, CreatedByUserId (FK), CreatedAt, UpdatedAt
- ProblemForms / FormFields
  - ProblemForm(Id PK, Name, Description)
  - FormField(Id PK, ProblemFormId FK, Name, Label, FieldType, IsRequired, OptionsJson)
- ProblemFieldValues
  - Id (PK), ProblemId (FK), FieldId (FK), Value
- ProblemLikes
  - Id (PK), ProblemId (FK), UserId (FK), CreatedAt

## Relationships
- Department 1—* Projects
- Project 1—* Problems
- Problem 1—* Solutions
- User 1—* Problems (CreatedBy)
- User 1—* Solutions (CreatedBy)
- Problem 1—* ProblemFieldValues
- ProblemForm 1—* FormFields
- User *—* Problems (via ProblemLikes)

## Indices and Constraints
- Users.Email unique index
- FKs with cascade behavior as per migrations
- Suggested indices: Problems(ProjectId, Status, Priority), Solutions(ProblemId, Status)

## Migrations
- Add migrations from Infrastructure project (Code-First).
- Apply migrations using API as startup project.

See also: `Docs/architecture.md` for data model context.

