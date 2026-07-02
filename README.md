# Report Management System

A centralized enterprise administration platform for managing report configurations, report columns, filtering rules, email alert schedules, attachments, company settings, users, authentication, authorization, and audit history.

Developed using **ASP.NET MVC 5** on **.NET Framework 4.7**, the application supports both **SQL Server** and **MySQL** through a provider-independent Data Access Layer based on `DbProviderFactory`.

The system replaces manual database administration with a secure, web-based management portal featuring dynamic database connectivity, XML-based authentication, role-based authorization, comprehensive audit logging, and centralized configuration management.

---

# Features

## Configuration Management

- Report Master Management
- Report Column Management
- Report Filtering Column Management
- Alert Configuration Management
- Alert Schedule Management
- Alert Attachment Management
- Company Configuration Management

## User & Security

- XML-Based User Management
- Session-Based Authentication
- Role-Based Authorization
- PBKDF2 Password Hashing
- Automatic Legacy Password Migration
- Self-Service Password Change
- Administrative Password Reset
- User Activation / Deactivation
- Soft User Management

## Monitoring & Auditing

- Audit Trail with JSON Snapshots
- XML Query Logging
- XML Error Logging
- Transaction-Based Processing
- Dependency Validation

## Platform Features

- Dynamic Database Selection
- SQL Server Support
- MySQL Support
- Provider-Based Database Abstraction
- Runtime Connection Switching
- Centralized Configuration Management

---

# Technology Stack

| Layer                 | Technology                                 |
| --------------------- | ------------------------------------------ |
| Backend               | ASP.NET MVC 5.2.7                          |
| Framework             | .NET Framework 4.7                         |
| Frontend              | Razor Views, Bootstrap 3.4.1, jQuery 3.4.1 |
| Business Layer        | Service-Oriented Architecture              |
| Data Access           | ADO.NET + DbProviderFactory                |
| Database              | SQL Server / MySQL                         |
| Authentication        | XML-Based Authentication                   |
| Authorization         | XML-Based Role Management                  |
| Password Security     | PBKDF2 (Rfc2898DeriveBytes)                |
| Logging               | XML Query Logger & XML Error Logger        |
| Hosting               | IIS                                        |
| Dependency Management | NuGet                                      |

---

# Architecture

```
Presentation Layer
        │
        ▼
Controller Layer
        │
        ▼
Service Layer
        │
        ▼
Data Access Layer
        │
        ▼
DbProviderFactory
        │
        ▼
SQL Server / MySQL
```

---

# Project Structure

```text
Report/
│
├── App_Data/
│   ├── Users.xml
│   ├── Roles.xml
│   ├── DbConnection.xml
│   ├── QueryLogs/
│   └── ErrorLogs/
│
├── Controllers/
├── Models/
├── Services/
├── Views/
├── Styles/
├── js/
├── App_Start/
├── docs/
│
├── Global.asax
├── Web.config
├── Report.csproj
└── README.md
```

---

# Available Modules

- Report Master
- Report Columns
- Report Filtering Columns
- Alert Configuration
- Alert Schedule
- Alert Attachments
- Company Configuration
- User Management
- Audit Trail

---

# Security Features

The application implements multiple security mechanisms.

- XML-Based Authentication
- Role-Based Authorization
- Session-Based Security
- PBKDF2 Password Hashing
- Cryptographic Salt Generation
- Configurable Hash Iterations
- Timing Attack Resistant Password Verification
- Automatic Legacy Password Migration
- Audit Logging
- Query Logging
- Error Logging
- Transaction Processing

---

# Authentication

Authentication is performed using XML-based user management.

During login the user selects

- Username
- Password
- Target Database

After successful authentication the application creates an authenticated ASP.NET Session containing

- User Information
- Role
- Active Database
- Connection String
- Provider Type

---

# User Management

The application includes a centralized User Management module supporting

- User Creation
- User Modification
- Password Reset
- Account Activation
- Account Deactivation
- Role Assignment
- Password Lifecycle Management
- Authentication Support
- Administrative Password Management

---

# Audit Logging

Every UPDATE and DELETE operation records the previous state of the affected record as a JSON snapshot before committing changes.

The audit framework provides

- Historical Record Tracking
- Deleted Record Recovery
- Change Timeline
- User Activity Tracking

---

# Internal Documentation

Comprehensive project documentation is available in Notion.

Documentation includes

- Functional Documentation
- Technical Design Documentation
- System Architecture
- Database Design
- Authentication Architecture
- User Management
- Password Management
- Security Architecture
- Deployment Guide
- Development Guidelines
- Known Limitations
- Future Roadmap

Documentation

https://app.notion.com/p/Report-Management-System-38394ea95aab80488ba2ee66bf6d41c6?source=copy_link

---

# Prerequisites

- Windows 10 or later
- Visual Studio 2022 (recommended)
- .NET Framework 4.7 Developer Pack
- SQL Server or MySQL
- IIS Express or IIS
- NuGet Package Manager

---

# Getting Started

## Clone Repository

```bash
git clone https://github.com/PrashantJha183/Report-Management-System.git

cd Report-Management-System
```

## Restore Packages

```bash
nuget restore packages.config
```

## Configure Database

Configure

```
App_Data/DbConnection.xml
```

## Configure Users

Configure

```
App_Data/Users.xml
```

## Configure Roles

Configure

```
App_Data/Roles.xml
```

## Build

```bash
msbuild Report.csproj /p:Configuration=Debug
```

## Run

Open the solution in Visual Studio.

Press

```
F5
```

or

```
Ctrl + F5
```

---

# Default Login

> Development environment only.

```
Username : superadmin

Password : superadmin9
```

---

# Development Workflow

```bash
git checkout -b feature/feature-name

git add .

git commit -m "Implemented feature"

git push origin feature/feature-name
```

---

# Development Guidelines

- Keep business logic inside Services.
- Avoid database access inside Controllers.
- Maintain compatibility with SQL Server and MySQL.
- Use transaction-based processing.
- Update XML configuration when adding users or roles.
- Maintain audit logging for data modifications.
- Preserve provider-independent DAL implementation.
- Follow existing architectural patterns.

---

# Known Limitations

Current limitations include

- XML-Based User Repository
- No Password History Enforcement
- No Password Expiration Policy
- No Multi-Factor Authentication (MFA)
- No Account Lockout Policy
- No Password Reset Token Workflow
- Manual AlertConfigId Generation
- Bootstrap 3 Dependency
- No Automated Unit Tests
- XML Log Growth
- Session-Based Authentication (single-server session)

---

# Future Enhancements

Planned improvements include

- ASP.NET Identity
- Active Directory Integration
- OAuth / OpenID Connect
- Multi-Factor Authentication
- Bootstrap 5
- REST API
- Distributed Session Storage
- Unit Testing
- Log Archival
- Soft Delete Framework
- Password History Enforcement
- Password Expiration Policy

---

# Security Notice

Do not commit the following files to public repositories.

```
App_Data/Users.xml

App_Data/Roles.xml

App_Data/DbConnection.xml

Web.config

Web.Debug.config

Web.Release.config
```

Store production credentials, connection strings, and secrets securely outside source control.

---

# Repository Access

This repository is intended for authorized developers and stakeholders associated with the Report Management System project.

---

# License

This project is proprietary and confidential.

Unauthorized copying, modification, distribution, or commercial use is prohibited without prior written permission.
