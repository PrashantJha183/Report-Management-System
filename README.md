# Report Management System

A web-based administration platform for managing report configurations, report columns, filtering rules, email alert schedules, attachments, company settings, and audit logs.

Built using ASP.NET MVC 5 and .NET Framework 4.7, the application supports both SQL Server and MySQL through a provider-agnostic data access layer based on `DbProviderFactory`.

---

## Features

- Report Master Management
- Report Column Configuration
- Report Filtering Column Configuration
- Alert Configuration Management
- Alert Schedule Management
- Alert Attachment Management
- Company Configuration Management
- Audit Trail & Activity Logging
- Database Provider Abstraction (SQL Server / MySQL)
- Centralized Error Logging
- Dynamic Report Configuration Support

---

## Technology Stack

| Layer | Technology |
|---------|------------|
| Backend | ASP.NET MVC 5.2.7 |
| Framework | .NET Framework 4.7 |
| Frontend | Razor Views, jQuery 3.4.1, Bootstrap 3.4.1 |
| Database | SQL Server / MySQL |
| Data Access | ADO.NET |
| Logging | Custom XML-Based Logging |
| Dependency Management | NuGet |

---

## Project Structure

```text
Report/
│
├── Controllers/
├── Models/
├── Services/
├── Views/
├── App_Start/
├── App_Data/
├── Scripts/
├── Content/
├── Styles/
├── js/
├── docs/
│
├── Report.csproj
├── packages.config
├── Web.config
└── README.md
```

---

## Internal Documentation

Comprehensive functional, technical, architecture, database, deployment, and implementation documentation is available at:

https://app.notion.com/p/Report-Management-System-38394ea95aab80488ba2ee66bf6d41c6?source=copy_link

### Documentation Includes

- Functional Documentation
- Technical Design Documentation
- Module Specifications
- Database Design
- Architecture Overview
- Deployment Documentation
- Security Considerations
- Development Guidelines
- Known Limitations
- Future Enhancement Roadmap

---

## Prerequisites

- Windows 10 or later
- Visual Studio 2017 or later
- .NET Framework 4.7 Developer Pack
- SQL Server or MySQL
- NuGet Package Manager

---

## Getting Started

### Clone Repository

```bash
git clone https://github.com/PrashantJha183/Report-Management-System.git
cd Report-Management-System
```

### Restore NuGet Packages

```bash
nuget restore packages.config
```

### Configure Database

Edit:

```text
App_Data/DbConnection.xml
```

### Build Application

```bash
msbuild Report.csproj /p:Configuration=Debug
```

### Run Application

Press F5 or Ctrl + F5 from Visual Studio.

---

## Default Login

> For development and demonstration environments only.

```text
Username : admin
Password : admin
```

---

## Available Modules

- Report Master
- Report Columns
- Filtering Columns
- Alert Configuration
- Alert Schedule
- Alert Attachments
- Company Configuration
- Audit Trail

---

## Architecture Overview

```text
Presentation Layer
        │
        ▼
Controller Layer
        │
        ▼
Service Layer
        │
        ▼
DAL Layer (DbProviderFactory)
        │
        ▼
SQL Server / MySQL
```

---

## Development Workflow

```bash
git checkout -b feature/feature-name
git add .
git commit -m "Implemented feature"
git push origin feature/feature-name
```

---

## Coding Guidelines

- Follow existing project coding standards.
- Keep business logic inside service classes.
- Avoid direct database access from controllers.
- Maintain compatibility with both SQL Server and MySQL.
- Update whitelist mappings when introducing new database fields.
- Test before creating pull requests.

---

## Known Limitations

- Hardcoded admin credentials
- No role-based authorization
- No CSRF protection
- No automated tests
- Manual AlertConfigId generation (MAX+1)
- Hard delete operations
- Bootstrap 3 dependency

---

## Security Notice

Sensitive configuration values should never be committed:

```text
App_Data/DbConnection.xml
Web.config
Web.Debug.config
Web.Release.config
```

Production credentials, connection strings, API keys, and secrets must be managed securely outside source control.

---

## Repository Access

This repository is intended for authorized developers and stakeholders associated with the project.
