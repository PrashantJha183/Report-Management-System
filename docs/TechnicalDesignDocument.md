# Technical Design Document — Report Management System

| **Project** | Report Management System |
|---|---|
| **Technology Stack** | ASP.NET MVC 5, .NET Framework 4.7, SQL Server, MySQL, jQuery, Bootstrap 3, ADO.NET |
| **Version** | 1.0.0 |
| **Prepared By** | Development Team |
| **Date** | June 2026 |

---

# 1. Executive Summary

## 1.1 Business Problem

The organization manages a large application with complex database tables defining report configurations, report columns, filtering criteria, and email alert schedules. Direct database access for managing these configurations poses significant risks:

- Accidental data corruption or deletion of critical records
- No audit trail of who changed what and when
- No validation of data integrity before saving
- No controlled access to configuration tables
- High dependency on DBAs and developers for routine changes

A dedicated administration interface was required — one that provides safe, UI-driven management of specific database tables without writing raw SQL or using database management tools.

## 1.2 Solution Overview

The Report Management System is an ASP.NET MVC 5 web application that provides inline-editable grids for managing report definitions, columns, filters, alert configurations, schedules, attachments, and company settings. The system supports both SQL Server and MySQL databases interchangeably, enforces column-level validation, prevents SQL injection through whitelisting, maintains a complete audit trail of all changes, and uses transactional batch saves to ensure data consistency.

The system also hosts the **Dynamic Code module** — an administration IDE (Monaco editor with IntelliSense) for authoring C# code snippets that are persisted to the shared `CustomCode` table. At runtime the ERP application intercepts matching controller/action requests and executes these snippets, allowing endpoint behavior to be overridden from Report without redeploying the ERP application.

## 1.3 Business Benefits

| Benefit | Description |
|---|---|
| **Reduced DB Dependency** | Non-technical users can manage configurations through a UI |
| **Better Maintainability** | Consistent CRUD patterns across all 7 business entities |
| **Auditability** | Every change is logged with full row snapshots as JSON |
| **Controlled Administration** | Session-based authentication restricts access |
| **Data Integrity** | Transactional saves prevent partial updates |
| **Cross-Platform DB** | Single codebase supports both SQL Server and MySQL |
| **Dynamic Extensibility** | ERP endpoint behavior can be overridden with C# code authored in Report — no ERP redeployment required |

---

# 2. System Overview

The system comprises seven business modules, each following an identical architecture pattern:

## 2.1 Report Management

The core module. Defines SQL report queries, WHERE clauses, ORDER BY clauses, master-detail relationships, and stored procedure flags. Each report is associated with a reference link item (the application module it belongs to). Reports form the parent entity for report columns, filtering columns, and alert configurations.

**Key fields:** ReportId, Code, Query, WhereClause, OrderBy, DetailQuery, DetailPrimaryKey, IsMasterDetail, IsStoreProcedure, ReportTypeId, IsLocationFilter, ReferenceLinkId

## 2.2 Report Columns

Defines the columns displayed in each report — their display names, underlying database column names, data types, grouping, sort order, and whether they serve as filter/search columns. Each column belongs to exactly one report.

**Key fields:** ReportColumnId, DisplayColumnName, TableColumnName, Datatype, IsDefaultColumn, IsSqlParameter, ReportId, GroupType, GroupIndex, SearchQuery, SrNo, IsFilterColumn, MainReportColumnId

## 2.3 Report Filtering Columns

Defines filter criteria applied to report columns. Each filter is linked to a report column and specifies the operator, value(s), and AND/OR conditions. Supports range filters (Value1/Value2).

**Key fields:** ReportFilterColumnId, ReportColumnId, ReportId, DataType, Operator, Value1, Value2, Condn, IsDisable

## 2.4 Alert Configuration

Defines email alert rules tied to reports. Specifies email subject/body templates, attachment file types, status (active/inactive), alert types, and multi-alert query support. This module uses manual identity generation (MAX+1) rather than auto-increment.

**Key fields:** AlertConfigId, AlertName, ReportId, IsEmailNotify, EmailSubject, EmailContentHeader, EmailContentUrl, AttachmentFileTypeId, EmailAttachmentUrl, StatusId, AlertTypeId, IsMultiAlert, MultiAlertQuery, EmailContentFooter

## 2.5 Alert Schedule

Defines scheduling rules for alerts — recipient emails (To, CC, BCC), frequency in hours, and the next scheduled run date. Each schedule belongs to exactly one alert configuration.

**Key fields:** AlertScheduleId, AlertConfigId, ToEmail, FrequencyHours, NextScheduleDate, BCCEmail, CCEmail

## 2.6 Alert Attachment

Defines file attachments linked to alert configurations. Supports different attachment file types and URLs.

**Key fields:** AlertConfigAttachmentId, AlertConfigId, AttachmentFileTypeId, EmailAttachmentUrl

## 2.7 Company Configuration

A simple key-value store for company-specific settings. Manages configuration values by company ID and code.

**Key fields:** CompanyConfigId, CompanyId, Code, ConfigValue, Description

## 2.8 Dynamic Code

Manages runtime code overrides consumed by the ERP application. Each record maps a controller/action pair to a C# code body (optionally with extra `using` namespaces) plus a free-text description. Report is the authoring tool — it provides a CRUD grid and an IDE with compilation, IntelliSense, dot-completion, and signature help. Execution happens on the ERP side: `CallBackService` intercepts requests whose controller/action match a stored record, recompiles the snippet on every request, and returns the resulting `JsonResult`. Report stores the code only; the ERP holds no cached copy, so edits take effect immediately.

**Key fields:** CustomCodeId, ControllerName, ActionName, Namespaces, CustomCode, Description

# 3. High Level Architecture

## 3.1 Architecture Diagram

```
Browser (jQuery + Bootstrap 3)
       |
       v
MVC Controllers (12 controllers)
       |
       v
Service Layer (16 services)
       |
       v
Data Access Layer (IDal / Dal)
       |
       v
SQL Server  /  MySQL
```

> **Note:** 3 of the 12 controllers (`DynamicCodeController`, `NameSpaceController`, `IntellisenseController`) and 2 of the 16 services (`DynamicCodeService`, `CustomCodeDAL`) belong to the Dynamic Code module. The Dynamic Code consumer (the ERP application) is a separate application that reads the same `CustomCode` table at runtime.

## 3.2 Layer Responsibilities

### Presentation Layer (Views + JavaScript)
- **Responsibilities:** Render HTML grids, handle user interactions, manage inline editing, validate input client-side, send AJAX requests
- **Key files:** `Views/*/Index.cshtml`, `js/ReportMaster.js`, `js/ReportColumn.js`, etc.
- **Dynamic Code files:** `Views/DynamicCode/Index.cshtml`, `Views/DynamicCode/IDE.cshtml`, `Views/Shared/_CodeEditor.cshtml` (Monaco), `Views/Shared/_NamespaceEditor.cshtml`, `js/DynamicCode.js`, `Styles/DynamicCode.css`
- **Dependencies:** jQuery 3.4.1, Bootstrap 3.4.1, monaco-editor 0.45.0 (CDN)

### Controller Layer
- **Responsibilities:** Handle HTTP requests, bind models, invoke services, return JSON or View results
- **Key files:** `Controllers/ReportController.cs`, `Controllers/AlertConfigController.cs`, etc.
- **Dynamic Code files:** `Controllers/DynamicCodeController.cs`, `Controllers/NameSpaceController.cs`, `Controllers/IntellisenseController.cs`
- **Design decisions:** No dependency injection container — services are instantiated directly in constructors. All controller action methods are synchronous despite services being async (simplified migration).

### Service Layer
- **Responsibilities:** Business logic, column whitelist validation, change grouping, SQL generation, audit logging, transaction management
- **Key files:** `Services/ReportService.cs`, `Services/AuditLogService.cs`, `Services/ServiceHelper.cs`
- **Dynamic Code files:** `Services/DynamicCodeService.cs`, `Services/CustomCodeDAL.cs`
- **Design decisions:** Services accept `IDal` interface for testability, with a fallback to `new Dal()`. Static utility classes (`ServiceHelper`, `ErrorLogger`, `QueryLogger`) handle cross-cutting concerns.

### Data Access Layer
- **Responsibilities:** Abstract database operations, manage connections and transactions, handle provider-specific SQL differences
- **Key files:** `Services/IDal.cs`, `Services/Dal.cs`
- **Design decisions:** Uses `DbProviderFactory` for database-agnostic access. Connection configuration is read once at static initialization from `DbConnection.xml`. Transaction support is instance-level with BeginTransaction/Commit/Rollback.
- **Known exception:** `CustomCodeDAL.cs` (Dynamic Code) bypasses the `DbProviderFactory` abstraction and hardcodes `System.Data.SqlClient` — see §14.4.

### Configuration Layer
- **Responsibilities:** Centralized configuration for table names, page sizes, timezone, DB connection path
- **Key files:** `Services/ReportConfig.cs`, `App_Data/DbConnection.xml`, `Web.config`
- **Design decisions:** All table names and page sizes are configurable via `Web.config` appSettings, allowing the same deployment to point to different database schemas.
- **Dynamic Code settings:** `DynamicCodeTable` (`CustomCode`), `DynamicCodePageSize` (`7`), `DefaultNamespaces` (base `using` set prepended to every snippet).

---

# 4. Complete Project Structure

```
Report/
|
|-- App_Data/
|   |-- DbConnection.xml              # Database connection config (SQL Server / MySQL)
|   |-- ErrorLogs/                    # XML error logs with stack traces
|   |-- QueryLogs/                    # XML logs of INSERT/UPDATE/DELETE queries
|
|-- App_Start/
|   |-- BundleConfig.cs               # CSS/JS bundling configuration
|   |-- FilterConfig.cs               # Global filters (HandleError + LoginAuthorizeAttribute)
|   |-- RouteConfig.cs                # URL routing ({controller}/{action}/{id})
|
|-- Content/
|   |-- bootstrap.css                 # Bootstrap 3.4.1
|   |-- Site.css                      # Minimal global overrides
|
|-- Controllers/
|   |-- HomeController.cs             # Login/Logout + landing page
|   |-- ReportController.cs           # Report Master CRUD
|   |-- ReportColumnController.cs     # Report Columns CRUD
|   |-- ReportFilteringColumnController.cs
|   |-- AlertConfigController.cs      # Alert Config CRUD
|   |-- AlertScheduleController.cs    # Alert Schedule CRUD
|   |-- AlertConfigAttachmentController.cs
|   |-- CompanyConfigController.cs    # Company Config CRUD
|   |-- AuditTrailController.cs       # Audit log viewer + deleted records
|   |-- DynamicCodeController.cs      # Dynamic Code grid + IDE (save/delete/id mapping)
|   |-- NameSpaceController.cs        # Namespace autocomplete (reflection)
|   |-- IntellisenseController.cs     # Monaco dot-completion + signature help
|
|-- Models/
|   |-- comMstReport.cs               # ComMstReport, ReportGridViewModel, ReportChange, etc.
|   |-- ComMstReportColumn.cs         # Same DTO pattern for columns
|   |-- comMstReportFilteringColumn.cs
|   |-- ComMstAlertConfig.cs
|   |-- ComMstAlertSchedule.cs
|   |-- ComMstAlertConfigAttachment.cs
|   |-- ComMstCompanyConfig.cs
|   |-- AuditLog.cs                   # AuditLog entity + AuditLogGridViewModel
|   |-- DynamicCodeModels.cs          # GridItem, Change, Request/Result, GridViewModel, IDEViewModel
|
|-- Services/
|   |-- IDal.cs                       # Data access interface (async)
|   |-- Dal.cs                        # DbProviderFactory implementation (SQL Server + MySQL)
|   |-- ReportConfig.cs               # Static configuration from Web.config
|   |-- ServiceHelper.cs              # Try/catch wrappers for save/delete operations
|   |-- ReportService.cs              # Report master business logic
|   |-- ReportColumnService.cs        # Report column business logic
|   |-- ReportFilteringColumnService.cs
|   |-- AlertConfigService.cs
|   |-- AlertScheduleService.cs
|   |-- AlertConfigAttachmentService.cs
|   |-- CompanyConfigService.cs
|   |-- AuditLogService.cs            # Audit log read/write operations
|   |-- ErrorLogger.cs                # Thread-safe XML error logging
|   |-- QueryLogger.cs                # Thread-safe XML query logging
|   |-- DynamicCodeService.cs         # Dynamic Code CRUD + IDE save + audit logging
|   |-- CustomCodeDAL.cs              # CustomCode table CRUD (SqlClient-only)
|
|-- Scripts/
|   |-- jquery-3.4.1.js               # jQuery
|   |-- bootstrap.js                  # Bootstrap 3.4.1
|   |-- jquery.validate.js            # jQuery Validation
|   |-- modernizr-2.8.3.js
|
|-- Styles/
|   |-- ReportMaster.css              # Report Master grid (729 lines)
|   |-- ReportColumn.css              # Report Column grid (566 lines)
|   |-- ReportFilteringColumn.css     # Filtering Column grid (636 lines)
|   |-- AlertConfig.css               # Alert Config grid (576 lines)
|   |-- AlertSchedule.css             # Alert Schedule grid (565 lines)
|   |-- AlertConfigAttachment.css     # Alert Attachment grid (565 lines)
|   |-- CompanyConfig.css             # Company Config grid (562 lines)
|   |-- DynamicCode.css               # Dynamic Code grid + IDE panel (843 lines)
|
|-- js/
|   |-- ReportMaster.js               # Report Master grid client logic
|   |-- ReportColumn.js               # Report Column grid client logic
|   |-- ReportFilteringColumn.js      # Filtering Column grid client logic
|   |-- AlertConfig.js                # Alert Config grid client logic
|   |-- AlertSchedule.js              # Alert Schedule grid client logic
|   |-- AlertConfigAttachment.js      # Alert Attachment grid client logic
|   |-- CompanyConfig.js              # Company Config grid client logic
|   |-- UserMaster.js                 # User Master grid client logic
|   |-- shared-grid.js                # Shared grid helpers
|   |-- DynamicCode.js                # Dynamic Code grid + IDE panel client logic
|
|-- Views/
|   |-- _ViewStart.cshtml
|   |-- Home/Index.cshtml             # Login page (standalone, no layout)
|   |-- Report/Index.cshtml           # Report Master grid
|   |-- ReportColumn/Index.cshtml     # Report Column grid
|   |-- ReportFilteringColumn/Index.cshtml
|   |-- AlertConfig/Index.cshtml
|   |-- AlertSchedule/Index.cshtml
|   |-- AlertConfigAttachment/Index.cshtml
|   |-- CompanyConfig/Index.cshtml
|   |-- AuditTrail/AuditLogs.cshtml   # Audit history + deleted records viewer
|   |-- DynamicCode/Index.cshtml      # Dynamic Code grid + inline Monaco IDE panel
|   |-- DynamicCode/IDE.cshtml        # Standalone IDE page (Layout = null)
|   |-- Shared/_CodeEditor.cshtml     # Monaco editor partial (IntelliSense providers)
|   |-- Shared/_NamespaceEditor.cshtml# Namespace textarea + autocomplete suggestions
|   |-- Shared/_Layout.cshtml         # Master layout (navbar, logout, scripts)
|
|-- Global.asax.cs                    # Application_Start (filters, routes, bundles)
|-- Web.config                        # App settings, provider configuration
|-- packages.config                   # 33 NuGet packages
```

## 4.1 Folder Purposes

| Folder | Purpose | Responsibility | Key Files | Dependencies |
|--------|---------|---------------|-----------|--------------|
| `App_Start/` | ASP.NET bootstrap | Register routes, filters, bundles | `FilterConfig.cs`, `RouteConfig.cs`, `BundleConfig.cs` | MVC, Web.Optimization |
| `Controllers/` | HTTP request handling | Receive requests, return responses | All 12 controllers | Services, Models |
| `Models/` | Data transfer objects | Entity definitions, ViewModels, DTOs | 9 model files | None |
| `Services/` | Business logic + data access | CRUD, validation, audit, logging | 16 service files | System.Data, Newtonsoft |
| `Views/` | UI templates | Razor rendering | 15+ view files | Models, Layout |
| `Scripts/` | Client-side libraries | jQuery, Bootstrap, validation | 5+ library files | None |
| `js/` | Per-module client logic | Grid editing, IDE panel, toolbars | 8 custom JS files | jQuery, Monaco |
| `Styles/` | Page-specific CSS | Grid styling, design system | 8 CSS files | Bootstrap |
| `App_Data/` | Configuration + logs | XML config, error/query logs | `DbConnection.xml` | File system |

# 5. Database Architecture

## 5.1 Entity Relationship Diagram

```
com_mst_report (1) ---< (many) com_mst_reportcolumn
      |
      +---< (many) com_mst_reportfilteringcolumn
      |
      +---< (many) com_mst_alertconfig
                |
                +---< (many) com_mst_alertschedule
                |
                +---< (many) com_mst_alertconfigattachment

com_mst_link_item (1) ---< (many) com_mst_report

CustomCode (standalone)   # No FK relationships; written by Report, read by the ERP at runtime
```

## 5.2 Relationship Details

| Parent | Child | Foreign Key | Business Significance |
|--------|-------|-------------|----------------------|
| com_mst_report | com_mst_reportcolumn | ReportId | A report has multiple columns |
| com_mst_report | com_mst_reportfilteringcolumn | ReportId | A report has multiple filters |
| com_mst_report | com_mst_alertconfig | ReportId | Alerts are based on report data |
| com_mst_reportcolumn | com_mst_reportfilteringcolumn | ReportColumnId | Filters apply to specific columns |
| com_mst_alertconfig | com_mst_alertschedule | AlertConfigId | One alert can have multiple schedules |
| com_mst_alertconfig | com_mst_alertconfigattachment | AlertConfigId | One alert can have multiple attachments |
| com_mst_link_item | com_mst_report | ReferenceLinkId | Categorizes reports by application module |

## 5.3 Table: com_mst_report

Stores report definitions — the SQL query, WHERE clause, sorting, master-detail config, etc.

| Column | Type | Description |
|--------|------|-------------|
| ReportId | int (PK) | Auto-increment identity |
| ReferenceLinkId | int | FK to com_mst_link_item |
| Code | varchar | Report code |
| Query | text | Main SQL SELECT query |
| WhereClause | text | Additional WHERE conditions |
| OrderBy | text | ORDER BY clause |
| DetailQuery | text | Detail/master-detail query |
| DetailPrimaryKey | varchar | PK field name for detail |
| IsMasterDetail | bit | Is this a master-detail report? |
| IsStoreProcedure | bit | Is Query a stored procedure call? |
| ReportTypeId | int | Type classification |
| IsLocationFilter | bit | Enable location-based filtering |

## 5.4 Table: com_mst_reportcolumn

| Column | Type | Description |
|--------|------|-------------|
| ReportColumnId | int (PK) | Auto-increment identity |
| DisplayColumnName | varchar | Column header shown in UI |
| TableColumnName | varchar | Actual DB column name |
| Datatype | varchar | string, int, datetime, decimal |
| IsDefaultColumn | bit | Show by default? |
| IsSqlParameter | bit | Is this a SQL parameter? |
| ReportId | int | FK to com_mst_report |
| GroupType | varchar | Grouping category |
| GroupIndex | int | Order within group |
| SearchQuery | text | Custom search query |
| SrNo | int | Display order |
| IsFilterColumn | bit | Can this column be filtered? |
| MainReportColumnId | int | FK to parent column (sub-columns) |

## 5.5 Table: com_mst_reportfilteringcolumn

| Column | Type | Description |
|--------|------|-------------|
| ReportFilterColumnId | int (PK) | Auto-increment identity |
| ReportColumnId | int | FK to com_mst_reportcolumn |
| ReportId | int | FK to com_mst_report |
| DataType | varchar | Data type for filter |
| Operator | varchar | Filter operator (e.g., =, LIKE, >) |
| Value1 | varchar | Filter value (or min for range) |
| Value2 | varchar | Max value for range filters |
| Condn | varchar | AND/OR condition |
| IsDisable | bit | Is this filter disabled? |

## 5.6 Table: com_mst_alertconfig

| Column | Type | Description |
|--------|------|-------------|
| AlertConfigId | int (PK) | NOT auto-increment (MAX+1) |
| AlertName | varchar | Name of the alert |
| ReportId | int | FK to com_mst_report |
| IsEmailNotify | bit | Send email? |
| EmailSubject | varchar | Email subject template |
| EmailContentHeader | text | Email body header |
| EmailContentUrl | varchar | Link URL in email |
| AttachmentFileTypeId | int | Type of attachment file |
| EmailAttachmentUrl | varchar | URL for attachment |
| StatusId | int | Status (30=Active, 200=Inactive) |
| AlertTypeId | int | Alert type classification |
| IsMultiAlert | bit | Allow multiple simultaneous alerts? |
| MultiAlertQuery | text | Query for multi-alert logic |
| EmailContentFooter | text | Email footer |
| CreatedBy | int | Who created (auto-set on INSERT) |
| CreatedOn | datetime | When created (IST on INSERT) |
| UpdatedBy | int | Who last updated |
| UpdatedOn | datetime | When last updated (IST on UPDATE) |

## 5.7 Table: com_mst_alertschedule

| Column | Type | Description |
|--------|------|-------------|
| AlertScheduleId | int (PK) | Auto-increment identity |
| AlertConfigId | int | FK to com_mst_alertconfig |
| ToEmail | varchar | Recipient email(s) |
| FrequencyHours | int | How often to send (in hours) |
| NextScheduleDate | datetime | Next scheduled send time |
| BCCEmail | varchar | BCC recipients |
| CCEmail | varchar | CC recipients |

## 5.8 Table: com_mst_alertconfigattachment

| Column | Type | Description |
|--------|------|-------------|
| AlertConfigAttachmentId | int (PK) | Auto-increment identity |
| AlertConfigId | int | FK to com_mst_alertconfig |
| AttachmentFileTypeId | int | File type identifier |
| EmailAttachmentUrl | varchar | URL/path to the file |

## 5.9 Audit Log Table (com_mst_audit_log)

| Column | Type | Description |
|--------|------|-------------|
| AuditLogId | int (PK) | Auto-increment identity |
| TableName | varchar | Table that was modified |
| RecordId | int | PK of the modified record |
| OperationType | varchar | UPDATE or DELETE |
| OldValues | text | Full row JSON snapshot |
| CreatedOn | datetime | Timestamp (IST) |
| CreatedBy | int | Who made the change |

## 5.10 Table: CustomCode

Stores runtime code overrides. Written by the Report Dynamic Code module and read/executed by the ERP application at runtime. The table lives in the database of each environment (SQL Server for most environments, MySQL for GEETEE).

| Column | Type | Description |
|--------|------|-------------|
| CustomCodeId | int (PK) | Auto-increment identity |
| ControllerName | nvarchar | Controller to intercept (e.g., `VehicleCallBack`) |
| ActionName | nvarchar | Action to intercept (e.g., `GetVehicleModelOnManufacturer`) |
| Namespaces | nvarchar | Comma-separated `using` namespaces for compilation |
| CustomCode | nvarchar(max) | C# code body — must return a `JsonResult` |
| Description | nvarchar | Human-readable purpose of the override |

Notes:
- **No `HttpMethod` column** — the ERP matches by controller + action name only (see §6.3).
- The persisted `Namespaces` value deliberately excludes `System.Collections.Generic` (RSuite's compiler header already includes it) and any `Report.*` namespaces (Report's own app assemblies cannot be resolved by the ERP runtime process).

---

# 6. Request Lifecycle

## 6.1 Flow Diagram

```
User
  | Clicks Save button
  v
Browser (jQuery)
  | validateForm() -> collectChanges() -> build changes[]
  | confirm modal -> $.ajax POST /SaveChanges
  v
Controller -> Service -> Dal
  | Capture before-image JSON
  | BeginTransaction
  | Execute INSERT/UPDATE batch
  | Get identity mappings
  | Commit
  | Write audit log
  v
Browser (jQuery)
  | Update data-* attributes
  | Map temp IDs to real IDs
  | Update display cells
  | Show success modal
  v
User sees updated row
```

## 6.2 Step-by-Step Walkthrough

1. **User Interaction:** The user clicks the Save button in an inline edit form row
2. **Client Validation:** `validateForm()` checks required fields; shows toast error if invalid
3. **Change Detection:** `collectChanges()` iterates form fields, compares current values against `data-*` attributes on the data row, and builds a `changes[]` array
4. **Confirmation:** A Bootstrap modal asks the user to confirm
5. **AJAX Request:** On confirmation, `$.ajax()` sends POST with `{ mode: "SAVE", changes: [...] }` as JSON
6. **Controller Deserialization:** ASP.NET MVC model binding deserializes JSON into DTO
7. **Service Processing:** Groups changes by row ID. For existing rows captures current DB state as JSON. For new rows (negative temp ID) prepares INSERT statements
8. **Transaction:** Database transaction opened. All SQL executed atomically
9. **Identity Mapping:** New row INSERTs return generated identity via SCOPE_IDENTITY() or LAST_INSERT_ID()
10. **Audit Logging:** Captured JSON snapshot written to audit log
11. **Response:** Result object with status and ID mappings returned to client
12. **UI Update:** JavaScript updates data-* attributes, replaces temp IDs, hides edit form, shows success

## 6.3 Dynamic Code Request Lifecycle

### 6.3.1 Report-side (authoring)

```
Grid flow:
User edits grid cell (Controller/Action/Description)
  -> Save -> POST DynamicCode/SaveChanges { mode, changes[] }
    -> DynamicCodeService.SaveChangesAsync
      -> NEW row (temp ID < 0): INSERT via CustomCodeDAL.AddAsync -> return real ID
      -> EXISTING row: capture before-image JSON -> UPDATE -> write audit (UPDATE)
  -> IdMappings returned -> temp IDs replaced client-side

IDE flow:
User opens IDE -> fills Controller/Action/Description/Namespaces/Code
  -> POST DynamicCode/SaveCode (IDEViewModel)
    -> Build namespace string (strip System.Collections.Generic + Report.*)
    -> AssemblyManager.RemoveAsseblyModel
    -> RSuite DynamicCodeExecutor.CompileCode (gacList: System.Web.dll, Microsoft.CSharp.dll)
    -> DynamicCodeService.SaveCodeFromIDEAsync -> INSERT/UPDATE + audit (UPDATE)
```

### 6.3.2 ERP-side (runtime interception)

```
HTTP Request to an ERP controller/action
  -> Global.asax.cs Application_BeginRequest
    -> CallBackService.IsDynamicActionPresent(url)
      -> GetByControllerAction(controller, action)   # controller + action only, no HTTP verb
      -> if match: rewrite URL to DynamicCallBack/Execute
    -> CallBackService.Execute
      -> read Namespaces + CustomCode from CustomCode table
      -> CompilerService.Compile(code, namespaces)   # recompiled EVERY request, no cache
      -> Activator.CreateInstance -> MethodInfo.Invoke
      -> HttpContext.Current.Response.Write(JsonResult) / return result
```

Because every request is recompiled, an override saved in Report is active on the very next ERP request. Generated wrapper classes are **not** `Controller` instances, so snippet bodies must construct `JsonResult` explicitly (`new JsonResult { Data = ..., JsonRequestBehavior = JsonRequestBehavior.AllowGet }`) rather than calling the controller helper `Json(...)`.

---

# 7. Inline Grid Editing Architecture

## 7.1 Flow Diagram

```
User clicks Edit -> populateEditForm() copies data-* to form fields
  -> User modifies fields -> User clicks Save
    -> validateForm() checks required fields
    -> collectChanges() compares form vs data-* builds changes[]
    -> confirm modal
    -> $.ajax POST /SaveChanges { mode: "SAVE", changes: [...] }
    -> Server processes batch (INSERT/UPDATE in transaction)
    -> Server maps temp IDs to real IDs
    -> Server writes audit log
    -> Returns { Status: "SAVED", IdMappings: { "-1": 42 } }
    -> updateDataRow() refreshes DOM
    -> success modal
```

## 7.2 Change Tracking Model

Each change object captures a single cell-level modification:

```json
{
  "ReportId": -1,
  "Column": "DisplayColumnName",
  "OldValue": "",
  "NewValue": "Customer Name",
  "IsNew": true
}
```

| Field | Description |
|-------|-------------|
| ReportId | Row ID (negative = new, positive = existing) |
| Column | Column name being modified |
| OldValue | Value before the change (from data-* attribute) |
| NewValue | Value after the change (from form field) |
| IsNew | True for newly created rows |

## 7.3 Temp ID System

When the user adds a new row:

1. A decrementing counter (tempIdCounter) generates negative IDs: -1, -2, -3...
2. The new row's data-reportid attribute is set to this negative temp ID
3. Server groups changes by row ID. Negative IDs trigger INSERT statements
4. After INSERT, server retrieves real identity and returns mapping: { "-1": 42 }
5. Client JavaScript replaces data-reportid values using this mapping

## 7.4 Batch Save Transaction

All SQL operations within a single save request are wrapped in a database transaction:

```
BeginTransaction()
  -> INSERT INTO com_mst_report (...) VALUES (...)
  -> SELECT SCOPE_IDENTITY() AS NewId, @tempId AS TempId
  -> UPDATE com_mst_report SET Code = @val WHERE ReportId = @pk
  -> INSERT INTO com_mst_audit_log (...) VALUES (...)
Commit()
```

If any operation fails, Rollback() is called and no partial changes persist.

## 7.5 Client-Side Row Management

| State | Description |
|-------|-------------|
| View mode | Data row visible, edit form hidden |
| Editing | Data row highlighted, edit form visible below it |
| New row | Inline form at top of table, data row has negative temp ID |
| Saving | Confirmation modal shown, buttons disabled |
| Saved | Data row updated, edit form hidden, success modal shown |

---

# 8. Audit Logging Architecture

## 8.1 Flow Diagram

```
User edits a row -> User clicks Save
  -> Service.SaveChangesAsync() called
  -> For each existing row:
       Dal.GetRowAsJsonAsync() -> SELECT * FROM table WHERE PK = @id
       -> Full row serialized to JSON via Newtonsoft.Json
  -> Begin database transaction
  -> Execute INSERT/UPDATE SQL
  -> Commit transaction
  -> AuditLogService.LogChangeAsync()
       INSERT INTO com_mst_audit_log (TableName, RecordId, OperationType, OldValues, CreatedOn, CreatedBy)
  -> Return result to client

User deletes a row:
  -> Capture row JSON before DELETE
  -> DELETE FROM table WHERE PK = @id
  -> Log DELETE with captured JSON
```

## 8.2 Why Full Row JSON Was Chosen

Rather than logging only the changed column and its old value, the system captures the entire row state before any modification.

| Advantage | Description |
|-----------|-------------|
| Complete Recovery | Full row JSON provides everything needed to reconstruct a deleted or corrupted record |
| Context Preservation | View all column values at time of change, not just the one that changed |
| Deleted Record Restoration | JSON snapshot is the only surviving record of deleted data |
| Simplified Querying | No need to join with original table (which may have changed since the audit entry) |
| Database-Agnostic | JSON stored as text, no JSON-specific DB features required |
| Self-Contained | Each audit entry is a complete, independent record of row state |

## 8.3 Capture Code (Dal.cs)

```csharp
public async Task<string> GetRowAsJsonAsync(string tableName, string pkColumn, object pkValue)
{
    var sql = $"SELECT * FROM {tableName} WHERE {pkColumn} = @pk";
    var dt = await ExecuteQueryAsync(sql, CreateParameter("@pk", pkValue));
    if (dt.Rows.Count == 0) return null;
    var dict = new Dictionary<string, object>();
    foreach (DataColumn col in dt.Columns)
        dict[col.ColumnName] = dt.Rows[0][col] == DBNull.Value ? null : dt.Rows[0][col];
    return JsonConvert.SerializeObject(dict);
}
```

## 8.4 Write Code (AuditLogService.cs)

```csharp
public async Task LogChangeAsync(string tableName, int recordId, string operationType,
    string oldValues, string createdBy = null)
{
    if (oldValues == null) return;
    var sql = $"INSERT INTO {ReportConfig.AuditLogTable} (...) VALUES (...)";
    var parameters = new List<DbParameter>
    {
        _dal.CreateParameter("@tableName", tableName),
        _dal.CreateParameter("@recordId", recordId),
        _dal.CreateParameter("@operationType", operationType),
        _dal.CreateParameter("@oldValues", oldValues),
        _dal.CreateParameter("@createdOn", _dal.GetCurrentIstTime()),
        _dal.CreateParameter("@createdBy", createdBy ?? (object)DBNull.Value)
    };
    await _dal.ExecuteNonQueryAsync(sql, parameters.ToArray());
}
```

## 8.5 Audit Integration in Save (ReportService.cs)

```csharp
// Capture before-images for all existing rows being updated
var auditEntries = new List<(int RecordId, string OldValues)>();
foreach (var rowGroup in rows)
{
    if (rowGroup.Key < 0) continue; // skip new rows
    var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.ReportTable, "ReportId", rowGroup.Key);
    if (oldJson != null)
        auditEntries.Add((rowGroup.Key, oldJson));
}

try
{
    _dal.BeginTransaction();
    var dt = await _dal.ExecuteQueryAsync(sql.ToString(), parameters.ToArray());
    _dal.Commit();

    foreach (var entry in auditEntries)
        await _auditLog.LogChangeAsync(ReportConfig.ReportTable, entry.RecordId, "UPDATE", entry.OldValues);

    return result;
}
catch (Exception ex)
{
    _dal.Rollback();
    result.Status = "ERROR:" + ex.Message;
    return result;
}
```

## 8.6 Audit Trail Viewer

The AuditTrailController provides two views:

1. **AuditLogs** — Displays change history for a specific table/record pair with pagination. Shows timestamp, operation type (color-coded), and full JSON snapshot in a scrollable pre block.

2. **DeletedRecords** — Lists all DELETE operations for a given table. For com_mst_report deletions, resolves ReferenceLinkId to link item names for readability.

3. **Dynamic Code integration** — The Dynamic Code grid uses the same audit pattern: UPDATE and DELETE actions capture the before-image of the `CustomCode` row as JSON via `GetRowAsJsonAsync()` and write it to `com_mst_audit_log` (TableName = `CustomCode`, RecordId = `CustomCodeId`). The grid exposes an inline **Deleted Records** button that opens `AuditTrail/AuditLogs.cshtml` with `AuditTableName=CustomCode` and `RecordIdLabel=Code ID`. Audit failures on delete are swallowed in a try/catch so a completed delete is never rolled back.

---

# 9. Authentication Architecture

## 9.1 Flow Diagram

```
HTTP Request
  -> LoginAuthorizeAttribute.OnAuthorization()
    -> Is controller = "Home"?
      -> Yes: Allow anonymous access
      -> No: Is Session["IsLoggedIn"] == true?
        -> Yes: Grant access
        -> No: Redirect to ~/Home/Index (login page)

Login form submitted (POST /Home/Login)
  -> Validate username == "admin" && password == "admin"
    -> Yes: Session["IsLoggedIn"] = true, redirect to Report/Index
    -> No: ViewBag.Error = "Invalid credentials", return login view

Logout (POST /Home/Logout)
  -> Session.Clear()
  -> Redirect to Home/Index
```

## 9.2 LoginAuthorizeAttribute (FilterConfig.cs)

```csharp
public class LoginAuthorizeAttribute : AuthorizeAttribute
{
    public override void OnAuthorization(AuthorizationContext filterContext)
    {
        string controller = filterContext.RouteData.Values["controller"]?.ToString();
        if (controller == "Home") return;

        if (filterContext.HttpContext.Session["IsLoggedIn"] == null ||
            !(bool)filterContext.HttpContext.Session["IsLoggedIn"])
        {
            filterContext.Result = new RedirectResult("~/Home/Index");
        }
    }
}
```

## 9.3 Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Registered globally | All controllers automatically protected except Home |
| HomeController excluded | Allows anonymous access for the login page |
| Redirect instead of 401 | Prevents IIS authentication dialog from appearing |
| Uses AuthorizeAttribute base | Proper MVC integration rather than action filter |

## 9.4 Session Handling

- **Storage:** In-process session (Web.config)
- **Key:** Session["IsLoggedIn"] (boolean flag)
- **Initialization:** Set by HomeController.Login() on success
- **Cleanup:** Cleared by HomeController.Logout() or session timeout
- **Credentials:** Hardcoded admin/admin (security limitation)

---

# 10. Major Enhancements Implemented

## 10.1 Cross-Database Support (SQL Server + MySQL)

| Aspect | Detail |
|--------|--------|
| Problem | The system needed to support both SQL Server and MySQL without code changes |
| Solution | DbProviderFactory pattern in Dal.cs. Configuration in DbConnection.xml with DbType element |
| Implementation | SQL differences abstracted: GetPaginationClause(), GetTopClause(), GetIdentityFunction(), GetIdentityCastType(), GetNullFunction(), GetTableLockHint() |
| Files Modified | Services/Dal.cs, Services/IDal.cs, Services/ReportConfig.cs |
| Outcome | Single codebase deploys to either database by changing one XML element |

## 10.2 Column Whitelist for SQL Injection Protection

| Aspect | Detail |
|--------|--------|
| Problem | Column names in dynamic SQL were vulnerable to SQL injection |
| Solution | Each service has a static HashSet<string> whitelist of allowed column names |
| Implementation | if (!ColumnWhitelist.Contains(change.Column)) continue; silently skips unauthorized columns |
| Files Modified | All service files (e.g., ReportService.cs line 27) |
| Outcome | Dynamically constructed column names restricted to known-safe values |

## 10.3 Audit Logging with Full Row JSON Snapshots

| Aspect | Detail |
|--------|--------|
| Problem | No audit trail existed |
| Solution | AuditLogService with GetRowAsJsonAsync() captures full row state before any modification |
| Implementation | Dal.GetRowAsJsonAsync() executes SELECT * and serializes to JSON. Captured before UPDATE and DELETE |
| Files Modified | Services/AuditLogService.cs (new), Services/Dal.cs, all service files |
| Outcome | Complete change history with recovery capability |

## 10.4 Temp ID Mapping for New Rows

| Aspect | Detail |
|--------|--------|
| Problem | New rows created client-side had no database identity until saved |
| Solution | JavaScript generates negative temp IDs. Server maps them to real IDs after INSERT |
| Implementation | INSERT followed by SELECT SCOPE_IDENTITY() or LAST_INSERT_ID(). Results as IdMappings dictionary |
| Files Modified | All service SaveChangesAsync() methods, all JS files |
| Outcome | Seamless client-side row creation without page reload |

## 10.5 Optimized Pagination with COUNT(*) OVER()

| Aspect | Detail |
|--------|--------|
| Problem | Original pagination required two round-trips (COUNT then SELECT) |
| Solution | Combined into single query using COUNT(*) OVER() window function |
| Implementation | SELECT ..., COUNT(*) OVER() AS TotalRecords FROM ... ORDER BY ... OFFSET ... FETCH NEXT ... |
| Files Modified | All service Get*Async() methods |
| Outcome | Halved database round-trips for paginated queries |

## 10.6 Migration from Synchronous to Async

| Aspect | Detail |
|--------|--------|
| Problem | Original code used synchronous ADO.NET, blocking threads during DB operations |
| Solution | Migrated all DB operations to async/await. Old sync code preserved in OLD_SYNC regions |
| Implementation | ExecuteQueryAsync, ExecuteScalarAsync, ExecuteNonQueryAsync, ExecuteReaderAsync |
| Files Modified | Services/Dal.cs, Services/IDal.cs, all service files |
| Outcome | Non-blocking I/O, better thread pool utilization |

## 10.7 Consolidated UPDATE Statements

| Aspect | Detail |
|--------|--------|
| Problem | Original code generated one UPDATE per changed column per row |
| Solution | Consolidated into a single UPDATE per row with all changed columns |
| Implementation | Changed from loop generating individual UPDATEs to single SET col1=@v1, col2=@v2, ... |
| Files Modified | All service SaveChangesAsync() methods |
| Outcome | Fewer DB round-trips, better transaction performance |

## 10.8 Autocomplete for Link Items

| Aspect | Detail |
|--------|--------|
| Problem | Selecting link items required knowing the numeric ID |
| Solution | Client-side autocomplete with debounced input filtering and dropdown suggestions |
| Implementation | jQuery input event with 300ms debounce, ul suggestions list positioned below input |
| Files Modified | js/ReportMaster.js |
| Outcome | User-friendly selection of link items by name |

## 10.9 Dependency Checking Before Delete

| Aspect | Detail |
|--------|--------|
| Problem | Deleting a report could orphan its columns, filters, and alert configs |
| Solution | Before deletion, service checks for dependent records and returns structured error message |
| Implementation | Returns "DEPENDENT:Report Columns:N" format, parsed client-side for meaningful message |
| Files Modified | All service Delete*Async() methods |
| Outcome | Prevents accidental deletion of records with dependencies |

## 10.10 Static Configuration via ReportConfig

| Aspect | Detail |
|--------|--------|
| Problem | Table names and page sizes were hardcoded in each service |
| Solution | ReportConfig static class reading from Web.config appSettings with sensible defaults |
| Implementation | GetSetting(), GetIntSetting() helper methods. All table names, page sizes, timezone configurable |
| Files Modified | Services/ReportConfig.cs (new), all service files updated |
| Outcome | Centralized configuration, deployable to different environments without recompilation |

## 10.11 Dynamic Code Module

| Aspect | Detail |
|--------|--------|
| Problem | No way to override or extend ERP action code without redeploying the ERP application |
| Solution | Report provides a CRUD grid plus an inline Monaco IDE where admins author C# snippets. Snippets are stored in the shared `CustomCode` table (controller/action/namespaces/code/description). The ERP's `CallBackService` intercepts matching controller/action requests at runtime and executes the stored code |
| Implementation | Grid + IDE panel on `DynamicCode/Index.cshtml`; Monaco 0.45.0 (CDN) with keyword/dot-completion and signature help via `IntellisenseController`; namespace autocomplete via `NameSpaceController`; compilation uses the RSuite `DynamicCodeExecutor`; per-request recompilation on the ERP side means edits apply immediately |
| Files Modified | Controllers: `DynamicCodeController.cs`, `NameSpaceController.cs`, `IntellisenseController.cs`; Services: `DynamicCodeService.cs`, `CustomCodeDAL.cs`; Models: `DynamicCodeModels.cs`; Views: `DynamicCode/Index.cshtml`, `DynamicCode/IDE.cshtml`, `Shared/_CodeEditor.cshtml`, `Shared/_NamespaceEditor.cshtml`; JS: `js/DynamicCode.js`; CSS: `Styles/DynamicCode.css`; Config: `Web.config` appSettings (`DynamicCodeTable`, `DynamicCodePageSize`, `DefaultNamespaces`), `ReportConfig.cs` |
| Outcome | Admins write and store C# code with IntelliSense from Report — the ERP executes it at runtime without redeployment |

---

# 11. Problems Faced & Solutions

## 11.1 Duplicate Row Creation on Double-Click

| Aspect | Detail |
|--------|--------|
| Problem | Double-clicking Add Row created two empty rows |
| Root Cause | No guard against rapid clicks. Event fires twice before first row renders |
| Solution | Disable button: $(this).prop('disabled', true), re-enable on completion |
| Files Modified | All JS files (ReportMaster.js:358) |

## 11.2 Save Button Double-Click During AJAX

| Aspect | Detail |
|--------|--------|
| Problem | Rapid double-click on Save triggers two AJAX calls, causing duplicate INSERTs |
| Root Cause | No guard on Save button during AJAX call |
| Solution | Disable Save button before AJAX, re-enable in complete callback. Also disable modal OK button |
| Files Modified | All JS files (ReportMaster.js:280-356) |

## 11.3 Column Name SQL Injection via Dynamic SQL

| Aspect | Detail |
|--------|--------|
| Problem | Column names in dynamic SQL were directly concatenated from user input |
| Root Cause | Original code constructed SQL like UPDATE table SET + change.Column + = @value without validation |
| Solution | HashSet<string> ColumnWhitelist in each service. All column names validated before use in SQL |
| Files Modified | All service files |

## 11.4 Manual AlertConfigId Race Conditions

| Aspect | Detail |
|--------|--------|
| Problem | AlertConfigId uses MAX(id)+1 instead of auto-increment, can produce duplicates under concurrency |
| Root Cause | SELECT MAX(id)+1 is not atomic under concurrent writes |
| Partial Solution | Table lock hints: WITH (TABLOCKX, HOLDLOCK) for SQL Server, FOR UPDATE for MySQL. Effective only within transactions |
| Files Modified | Services/AlertConfigService.cs, Services/ReportConfig.cs |
| Current Status | Mitigated but not fully resolved |

## 11.5 Page Size Inconsistency

| Aspect | Detail |
|--------|--------|
| Problem | Each service had hardcoded, inconsistent page sizes with confusing fallback logic |
| Root Cause | Original code used if (pageSize < 10) pageSize = 7 — magic numbers |
| Solution | Centralized all page sizes in ReportConfig.cs with clear configuration keys |
| Files Modified | Services/ReportConfig.cs, all service files |

## 11.6 JSON Formatting for Audit Logs

| Aspect | Detail |
|--------|--------|
| Problem | Raw JSON in audit logs was difficult to read |
| Root Cause | JSON was stored as compact single-line string |
| Solution | JSON parsed and re-serialized with indentation: JSON.stringify(JSON.parse(json), null, 2). Displayed in pre block |
| Files Modified | Views, JS audit handler |

## 11.7 Parent-Child Data Integrity on Delete

| Aspect | Detail |
|--------|--------|
| Problem | Deleting a report could orphan its columns, filters, and alert configs |
| Root Cause | No dependency checks before deletion |
| Solution | SELECT COUNT(*) checks before each delete. Structured "DEPENDENT:Type:Count" message |
| Files Modified | All service Delete*Async() methods |

## 11.8 Old Sync Code Cleanup Strategy

| Aspect | Detail |
|--------|--------|
| Problem | Codebase contained both sync and async versions creating confusion |
| Root Cause | During async migration, old sync code was preserved as comments |
| Solution | Wrapped in OLD_SYNC regions with descriptive comments. Can be safely removed in future cleanup |
| Files Modified | Services/Dal.cs, all service files |

## 11.9 Cross-Database Pagination Differences

| Aspect | Detail |
|--------|--------|
| Problem | SQL Server uses OFFSET...FETCH NEXT, MySQL uses LIMIT...OFFSET |
| Root Cause | Different SQL dialects |
| Solution | Abstracted behind IDal.GetPaginationClause(offset, pageSize) with provider-specific implementations |
| Files Modified | Services/Dal.cs, Services/IDal.cs |

## 11.10 "The name 'Json' does not exist in the current context" in Dynamic Code

| Aspect | Detail |
|--------|--------|
| Problem | Snippets using `return Json(...)` fail compilation at runtime |
| Root Cause | The generated dynamic wrapper class is not a `Controller`, so the MVC `Json()` helper method does not exist in its scope |
| Solution | Snippets must construct `JsonResult` explicitly: `return new JsonResult { Data = ..., JsonRequestBehavior = JsonRequestBehavior.AllowGet };` |
| Files Modified | Depends on the snippet — no application code change; documented in `_CodeEditor.cshtml` guidance and §21 |

## 11.11 Broken Code Saved Despite Compile Failure

| Aspect | Detail |
|--------|--------|
| Problem | A snippet that fails to compile is still persisted; the ERP then throws on every matching request |
| Root Cause | `DynamicCodeController.SaveCode` runs `executor.CompileCode(...)` but never inspects the returned compile result (`rm.Errors` / `rm.Success`) before calling `SaveCodeFromIDEAsync` (DynamicCodeController.cs:180-198) |
| Solution | Check the compile result and return the compiler errors without saving when compilation fails |
| Files Modified | `Controllers/DynamicCodeController.cs` (pending fix) |
| Current Status | Open — verified when a `return Json(...)` body was saved successfully despite a failed compile |

## 11.12 CustomCodeDAL Hardcoded to SqlClient

| Aspect | Detail |
|--------|--------|
| Problem | Dynamic Code grid/login fails with a MySQL provider error on GEETEE |
| Root Cause | `Services/CustomCodeDAL.cs` instantiates `System.Data.SqlClient` types directly instead of using `Dal.GetFactory()`, so it works on SQL Server environments but not MySQL (GEETEE) |
| Solution | Mirror the `GetFactory()` provider switch (`MYSQL` -> `MySqlClientFactory.Instance`, `SQLSERVER` -> `SqlClientFactory.Instance`) in `CustomCodeDAL` |
| Files Modified | `Services/CustomCodeDAL.cs` (pending fix) |

---

# 12. Security Architecture

## 12.1 Session Authentication

```csharp
// Login
Session["IsLoggedIn"] = true;

// Authorization check (global filter)
if (filterContext.HttpContext.Session["IsLoggedIn"] == null ||
    !(bool)filterContext.HttpContext.Session["IsLoggedIn"])
{
    filterContext.Result = new RedirectResult("~/Home/Index");
}
```

## 12.2 SQL Injection Prevention

Three layers of protection:

| Layer | Mechanism | Location |
|-------|-----------|----------|
| Parameterized Queries | All user values use DbParameter objects | Dal.cs |
| Column Whitelisting | Column names checked against HashSet before dynamic SQL | Each service (ReportService.cs:27) |
| Sort Column Validation | Sort column expressions validated via switch statement | Each service (GetSortColumnExpression()) |

## 12.3 Column Whitelist Pattern

```csharp
private static readonly HashSet<string> ColumnWhitelist = new HashSet<string>
{
    "ReferenceLinkId", "Code", "Query", "WhereClause", "OrderBy",
    "DetailQuery", "DetailPrimaryKey", "IsMasterDetail", "IsStoreProcedure",
    "ReportTypeId", "IsLocationFilter"
};

if (!ColumnWhitelist.Contains(change.Column))
    continue;
```

## 12.4 Security Limitations

| Limitation | Impact | Recommendation |
|------------|--------|----------------|
| Hardcoded credentials (admin/admin) | Any developer reading code knows credentials | Move to secure storage or integrate with AD |
| No role-based access | All authenticated users have full access | Implement RBAC (read-only vs admin roles) |
| No HTTPS enforcement | Credentials in cleartext | Enforce HTTPS via IIS or RequireHttpsAttribute |
| In-process session | Session data lost on app pool recycle | Consider distributed cache for production |
| No CSRF protection | AJAX POST endpoints lack anti-forgery tokens | Add ValidateAntiForgeryToken to all POST actions |

---

# 13. Logging & Monitoring

## 13.1 Logging Architecture

```
Application Code -> ExecuteNonQueryAsync() (INSERT/UPDATE/DELETE)
  -> QueryLogger.Log(query) -> App_Data/QueryLogs/QueryLog_YYYY-MM-DD.xml

Exception Handler (try/catch in Dal.cs methods)
  -> ErrorLogger.Log(ex, query) -> App_Data/ErrorLogs/ErrorLog_YYYY-MM-DD.xml
```

## 13.2 Query Logger (Services/QueryLogger.cs)

- **Scope:** Logs only INSERT, UPDATE, DELETE queries (not SELECT)
- **Format:** XML with CDATA-wrapped query text and timestamp
- **Location:** App_Data/QueryLogs/QueryLog_YYYY-MM-DD.xml
- **Thread safety:** lock statement prevents concurrent write corruption
- **Failure handling:** Silently catches exceptions

```xml
<QueryLogs>
  <Log Time="2026-06-18 14:30:00">
    <![CDATA[UPDATE com_mst_report SET Code = 'ABC' WHERE ReportId = 1]]>
  </Log>
</QueryLogs>
```

## 13.3 Error Logger (Services/ErrorLogger.cs)

- **Scope:** All exceptions caught in Dal.cs methods
- **Content:** Exception message, stack trace, SQL query that caused it
- **Location:** App_Data/ErrorLogs/ErrorLog_YYYY-MM-DD.xml

```xml
<ErrorLogs>
  <Log Time="2026-06-18 14:30:00">
    <Message><![CDATA[Invalid column name 'xyz']]></Message>
    <StackTrace><![CDATA[at Report.Services.Dal.ExecuteQueryAsync(...)]]></StackTrace>
    <Query><![CDATA[SELECT * FROM com_mst_report WHERE InvalidColumn = @p]]></Query>
  </Log>
</ErrorLogs>
```

## 13.4 Audit Logger

- **Scope:** All data modifications (UPDATE and DELETE) with full row JSON snapshots
- **Format:** JSON stored in com_mst_audit_log database table
- **Retention:** Depends on database backup strategy

---

# 14. Cross Database Architecture

## 14.1 Provider Abstraction

```csharp
private DbProviderFactory GetFactory()
{
    switch (_dbType.ToUpper())
    {
        case "MYSQL":
            return MySqlClientFactory.Instance;
        case "SQLSERVER":
        default:
            return SqlClientFactory.Instance;
    }
}
```

## 14.2 Configuration (App_Data/DbConnection.xml)

```xml
<Connection>
  <ConnectionString>Server=...;database=...;uid=...;pwd=...</ConnectionString>
  <DbType>SQLSERVER</DbType>   <!-- or MYSQL -->
</Connection>
```

## 14.3 SQL Syntax Differences

| Feature | SQL Server | MySQL | Abstraction Method |
|---------|-----------|-------|--------------------|
| Pagination | OFFSET n ROWS FETCH NEXT m ROWS ONLY | LIMIT m OFFSET n | GetPaginationClause() |
| Top N | SELECT TOP n | LIMIT n | GetTopClause() |
| Identity | SCOPE_IDENTITY() | LAST_INSERT_ID() | GetIdentityFunction() |
| Identity cast | INT | SIGNED | GetIdentityCastType() |
| Null coalesce | ISNULL(x, 0) | IFNULL(x, 0) | GetNullFunction() |
| Table lock | WITH (TABLOCKX, HOLDLOCK) | FOR UPDATE | GetTableLockHint() |

## 14.4 Dynamic Code Cross-Database Limitation

The main data access path (`IDal`/`Dal`) is provider-agnostic through `DbProviderFactory`, but **`CustomCodeDAL.cs` is the exception**: it hardcodes `System.Data.SqlClient` (`SqlConnection`, `SqlCommand`, `SqlParameter`) and does not consult `Session["DbType"]` or `Dal.GetFactory()`. Consequences:

- Works on SQL Server environments (e.g., `RLogic9-Dev`).
- Fails on MySQL environments (GEETEE `rlogic9geeteeuat`) with a provider/type error as soon as the Dynamic Code screen queries `CustomCode`.

The intended fix mirrors the §14.1 `GetFactory()` switch inside `CustomCodeDAL`, resolving the provider from `Session["DbType"]` (set in `HomeController.Login`, lines 68-69).

---

# 15. Configuration Management

## 15.1 Configuration Sources

| Source | Purpose | Location |
|--------|---------|----------|
| Web.config appSettings | Table names, page sizes, timezone, DB config path | Root of web application |
| DbConnection.xml | Database connection string and provider type | App_Data/DbConnection.xml |
| ReportConfig.cs | Static wrapper for all configuration values | Services/ReportConfig.cs |

## 15.2 Web.config Settings (appSettings)

| Key | Default Value | Description |
|-----|---------------|-------------|
| DbConnectionConfigPath | ~/App_Data/DbConnection.xml | Path to DB connection file |
| TimezoneId | India Standard Time | Timezone for all timestamps |
| ReportTable | com_mst_report | Report definitions table |
| ReportColumnTable | com_mst_reportcolumn | Report columns table |
| ReportFilteringColumnTable | com_mst_reportfilteringcolumn | Filtering columns table |
| AlertConfigTable | com_mst_alertconfig | Alert configurations table |
| AlertScheduleTable | com_mst_alertschedule | Alert schedules table |
| AlertConfigAttachmentTable | com_mst_alertconfigattachment | Alert attachments table |
| LinkItemTable | com_mst_link_item | Link items reference table |
| CompanyConfigTable | com_mst_companyconfig | Company configurations table |
| AuditLogTable | com_mst_audit_log | Audit log table |
| ReportPageSize | 5 | Pagination size for reports |
| MaxPageSize | 100 | Upper bound for any page size |
| DynamicCodeTable | CustomCode | Backing table for Dynamic Code entries |
| DynamicCodePageSize | 7 | Grid page size for Dynamic Code |
| DefaultNamespaces | Report.Controllers, Report.Services, System.Web.Mvc, System.Collections.Generic, System.Linq | Base `using` set applied to every compiled snippet |

## 15.3 Table Name Configuration Pattern

```csharp
// Before:
var sql = "SELECT * FROM com_mst_report WHERE ReportId = @id";

// After:
var sql = $"SELECT * FROM {ReportConfig.ReportTable} WHERE ReportId = @id";
```

This allows the same deployment to work with different table schemas by simply changing Web.config.

---

# 16. Deployment Guide

## 16.1 Prerequisites

| Requirement | Version/Detail |
|-------------|----------------|
| Windows Server | 2012 R2 or later |
| IIS | 7.5 or later with ASP.NET 4.7 feature |
| .NET Framework | 4.7 |
| Database | SQL Server 2012+ OR MySQL 5.7+ |

## 16.2 IIS Setup

1. Add App_Data folder with write permissions for the application pool identity
2. Enable App_Data folder for read access
3. Application pool uses .NET CLR version 4.0
4. Application pool identity: ApplicationPoolIdentity (recommended)

## 16.3 Database Setup

1. Ensure all required tables exist: com_mst_report, com_mst_reportcolumn, com_mst_reportfilteringcolumn, com_mst_alertconfig, com_mst_alertschedule, com_mst_alertconfigattachment, com_mst_companyconfig, com_mst_audit_log, com_mst_link_item, CustomCode
2. Configure App_Data/DbConnection.xml with correct connection string and DbType
3. Verify network connectivity between IIS and database server
4. For Dynamic Code on MySQL (GEETEE), note that `CustomCodeDAL` is currently SQL Server-only (see §14.4)

## 16.4 Deployment Checklist

- [ ] Build project in Release configuration
- [ ] Publish to a local folder
- [ ] Copy published files to IIS site directory
- [ ] Set App_Data folder permissions (write for app pool)
- [ ] Configure App_Data/DbConnection.xml with target database
- [ ] Verify Web.config appSettings match environment
- [ ] Browse to site URL and confirm login page loads
- [ ] Perform test login (admin/admin)
- [ ] Verify grid loads with data
- [ ] Perform test edit and save
- [ ] Verify audit log entry created
- [ ] Verify error/query log files created in App_Data
- [ ] Verify Dynamic Code grid loads and edits persist to the CustomCode table
- [ ] Write a test override in Report and confirm the ERP picks it up on the next request (recompiled per request — no ERP redeploy needed)

## 16.5 Rollback Strategy

1. Keep previous deployment folder intact (rename rather than delete)
2. To roll back, swap IIS application path to previous folder
3. Restore App_Data/DbConnection.xml if modified

---

# 17. Known Limitations

| # | Limitation | Impact | Details |
|---|------------|--------|---------|
| 1 | Hardcoded credentials (admin/admin) | Security risk | No password hashing, no configurable auth |
| 2 | No role-based access | All users have full access | Every authenticated user can create/edit/delete any record |
| 3 | Manual AlertConfigId (MAX+1) | Race condition risk | Duplicate IDs possible under concurrent writes |
| 4 | Hard delete strategy | No recovery | No IsDeleted flag. Full row JSON preserved in audit log |
| 5 | Legacy Bootstrap 3 | Outdated UI | Bootstrap 3 superseded by Bootstrap 5 |
| 6 | No unit tests | Regression risk | No test project. All changes require manual testing |
| 7 | Hardcoded IST timezone | Regional limitation | All timestamps use India Standard Time |
| 8 | No pagination for link items | Performance issue | GetLinkItems() returns ALL items |
| 9 | Old sync code preserved | Code clutter | OLD_SYNC regions take ~30% of service files |
| 10 | No anti-forgery tokens | CSRF vulnerability | POST endpoints lack ValidateAntiForgeryToken |
| 11 | Controller actions are synchronous | Thread pool inefficiency | Actions return ActionResult instead of async Task<ActionResult> |
| 12 | SaveCode persists compile-failed code | Broken overrides activate in the ERP | DynamicCodeController.SaveCode ignores the compile result before saving (see §11.11) |
| 13 | CustomCodeDAL is SQL Server-only | Dynamic Code fails on MySQL/GEETEE | DAL bypasses DbProviderFactory and hardcodes SqlClient (see §14.4) |
| 14 | The ERP matches without HTTP verb | GET and POST overrides can collide | Matching uses controller + action only; no HttpMethod column in CustomCode |
| 15 | Monaco editor loaded from CDN | External network dependency | Editor won't load without internet access; consider local bundling |
| 16 | Dynamic Code executes arbitrary C# | Security/operational risk | No sandbox; capability is gated only by admin authentication |

---

# 18. Future Roadmap

## 18.1 Short Term (0-6 Months)

- Add ValidateAntiForgeryToken to all POST actions
- Move credentials from code to configuration or environment variables
- Add basic role-based authorization (Admin, Viewer)
- Clean up OLD_SYNC code blocks
- Migrate controller actions to async (async Task<ActionResult>)
- Add soft delete with IsDeleted flag
- Enforce the compile-result check in DynamicCodeController.SaveCode so broken code is never saved
- Make CustomCodeDAL provider-agnostic (mirror Dal.GetFactory()) so Dynamic Code works on MySQL
- Add an HttpMethod column to the CustomCode table so an override can target a specific HTTP verb

## 18.2 Medium Term (6-12 Months)

- Add proper unit tests (xUnit + Moq)
- Replace DbConnection.xml with standard connectionStrings section
- Implement distributed caching (Redis) for frequently accessed data
- Convert AlertConfigId to auto-increment
- Add pagination and search for link items dropdown
- Runtime assembly caching with invalidation on save instead of per-request recompilation of Dynamic Code snippets
- Dynamic Code versioning and rollback (history table or snapshot field in CustomCode)

## 18.3 Long Term (12-24 Months)

- Migrate to ASP.NET Core 8 with built-in DI, appsettings.json, Kestrel hosting
- Replace session auth with ASP.NET Core Identity or OpenID Connect
- REST API layer for external integrations
- React or Vue.js frontend replacing server-rendered Razor views
- Upgrade Bootstrap 3 to Bootstrap 5
- Audit dashboard with search, filtering, visualization
- Automated CI/CD pipeline deployment
- Sandboxed execution for Dynamic Code snippets (AppDomain isolation or Roslyn scripting sandbox)
- Role-gated publish approval workflow for Dynamic Code overrides before they reach production

---

# 19. Knowledge Transfer Guide

## 19.1 Important Files for New Developers

| File | Why It Matters |
|------|----------------|
| Global.asax.cs | Application entry point - registers filters, routes, bundles |
| App_Start/FilterConfig.cs | Defines LoginAuthorizeAttribute - the authentication gate |
| Services/Dal.cs | Core data access - DbProviderFactory, transactions, async pattern |
| Services/IDal.cs | Data access interface - all DB operations go through this |
| Services/ReportConfig.cs | Configuration hub - table names, page sizes, all configurable values |
| Services/AuditLogService.cs | Audit trail - how changes are logged |
| Services/ReportService.cs | Model service - complete CRUD pattern used by all services |
| Models/comMstReport.cs | ViewModel/DTO pattern - entity, change DTO, request/response, grid VM |
| js/ReportMaster.js | Client-side grid editing - edit, save, delete, audit, add row |
| Styles/ReportMaster.css | Styling pattern - modern design system used by all pages |
| Controllers/ReportController.cs | Controller pattern - Index (GET), SaveChanges (POST), Delete (POST) |
| Controllers/DynamicCodeController.cs | Dynamic Code controller — grid, IDE compile+save, GetCode JSON for inline IDE |
| Services/DynamicCodeService.cs | Dynamic Code service — GetAll/SaveChanges/Delete with audit logging, SaveCodeFromIDEAsync |
| Services/CustomCodeDAL.cs | CustomCode table CRUD — note: hardcodes SqlClient (see §14.4) |
| Models/DynamicCodeModels.cs | Dynamic Code DTOs — GridItem, Change, GridViewModel, IDEViewModel |
| Views/DynamicCode/Index.cshtml | Dynamic Code grid + inline IDE panel + Deleted Records button |
| Views/Shared/_CodeEditor.cshtml | Monaco editor partial — keyword/dot-completion, signature help providers |
| js/DynamicCode.js | Dynamic Code client logic — showIDE/populateIDE/loadClassesForNamespaces |
| ERP-side: CallBackService.cs | ERP-side runtime interception — isDynamicActionPresent, per-request recompile |

## 19.2 How to Add a New Module

1. **Database:** Create the table, add its name to Web.config appSettings
2. **Model:** Create model file with entity, grid VM, change DTO, request DTO, result DTO
3. **Service:** Create service class extending CRUD pattern (whitelist, GetAsync, SaveChangesAsync, DeleteAsync)
4. **Controller:** Create controller with Index (GET), SaveChanges (POST), Delete (POST)
5. **View:** Create Index.cshtml following existing grid patterns (table, toolbar, pagination, edit forms, modals)
6. **JavaScript:** Create JS file following existing patterns (edit, save, delete, audit, add row, pagination)
7. **CSS:** Create stylesheet following existing design system
8. **Navigation:** Add link in _Layout.cshtml
9. **Edit form:** Add edit row in the view with fields matching the whitelist

> **Dynamic Code special case:** Dynamic Code does not follow the standard grid-save pattern for the Code/Namespaces fields — those are edited exclusively through the IDE panel (`_CodeEditor.cshtml`) via the `DynamicCode/SaveCode` action, which performs a CodeDOM compilation before saving. The grid `SaveChanges` action handles only the metadata columns (ControllerName, ActionName, Description).

## 19.3 How Audit Works

1. Before any UPDATE or DELETE, service calls _dal.GetRowAsJsonAsync(table, pkColumn, pkValue)
2. This executes SELECT * FROM table WHERE PK = @id and serializes result row to JSON
3. JSON string stored in com_mst_audit_log.OldValues
4. After successful save, AuditLogService.LogChangeAsync() writes the audit record
5. Audit viewer reads these records and displays in paginated table
6. Full row JSON allows reconstructing exact row state at time of modification

## 19.4 How Authentication Works

1. Global LoginAuthorizeAttribute intercepts every request
2. If controller is Home, filter allows access (anonymous login page)
3. Otherwise checks Session["IsLoggedIn"] == true
4. If not authenticated, redirects to ~/Home/Index
5. Login validates hardcoded credentials and sets session
6. Logout clears session

## 19.5 How CRUD Works

| Operation | Client-Side | Server-Side |
|-----------|-------------|-------------|
| Create | Add Row -> Edit -> Save -> POST SaveChanges with negative temp ID | INSERT with SCOPE_IDENTITY() -> return real ID -> map temp to real |
| Read | Page load -> GET Index with pagination/sort/search params | SELECT with COUNT(*) OVER(), pagination, JOINs |
| Update | Edit -> Save -> POST SaveChanges with row ID and changed columns | Capture JSON before -> UPDATE in transaction -> write audit |
| Delete | Click Delete -> confirm -> POST Delete/{id} | Check dependencies -> capture JSON -> DELETE -> write audit |

## 19.6 Common Issues & FAQ

| Issue | Investigation | Solution |
|-------|---------------|----------|
| "No changes in this row" on Save | Check collectChanges() - form values match data-* attributes | Make genuine change in at least one field |
| Audit log not appearing | Check GetRowAsJsonAsync() returned non-null JSON | Verify the row exists and has data |
| "ERROR: ..." on Save | Check ErrorLogs/ directory for stack trace | Can be SQL syntax, constraint violation, or connection issue |
| Pagination showing wrong total | Check COUNT(*) OVER() in SQL | Verify no GROUP BY affecting window function |
| Query logs empty | Check QueryLogger.Log() filters INSERT/UPDATE/DELETE only | SELECT queries intentionally not logged |
| Dynamic Code "Json does not exist" at runtime | Snippet uses `Json(...)` instead of `new JsonResult { ... }` | Generated wrapper is not a Controller; must construct JsonResult explicitly (see §21.5) |
| Dynamic Code saved but the ERP still returns original response | Controller or action name does not match the actual ERP endpoint | Double-check exact controller+action (no HTTP verb considered); check the ERP route mapping |
| Dynamic Code screen fails on GEETEE login | `CustomCodeDAL` uses SqlClient against a MySQL database | Known limitation (see §14.4); pending fix |
| Dynamic Code override not visible on the ERP edit form | Many ERP forms reset the dropdown value after `$.getJSON` populates options | Verify the override is live by inspecting the network request to the overridden endpoint directly, or use a New form rather than an edit form |
| IntelliSense shows no types | `NameSpaceController.Suggest` returns empty assembly list | Ensure the namespaces string is populated and the referenced assemblies are loadable in the Report process (check `AppDomain.GetAssemblies()` availability) |

---

# 20. Project Metrics

| Category | Count | Details |
|----------|-------|---------|
| Business Entities | 10 | Report, ReportColumn, ReportFilteringColumn, AlertConfig, AlertSchedule, AlertConfigAttachment, CompanyConfig, AuditLog, LinkItem, CustomCode |
| Controllers | 12 | Report, ReportColumn, ReportFilteringColumn, AlertConfig, AlertSchedule, AlertConfigAttachment, CompanyConfig, Home, AuditTrail, DynamicCode, NameSpace, Intellisense |
| Services | 16 | IDal, Dal, ReportConfig, ServiceHelper, ErrorLogger, QueryLogger, AuditLogService, DynamicCodeService, CustomCodeDAL + 7 business services |
| Views | 15 | 7 entity Index views, DynamicCode/Index + IDE, 2 Home views, 1 AuditLogs, Shared/_Layout, Shared/_CodeEditor, Shared/_NamespaceEditor, 1 Error |
| JavaScript Files | 8 | One per business entity + DynamicCode.js (Grid + IDE panel) |
| CSS Files | 8 custom + 2 Bootstrap | One per entity + DynamicCode.css (grid + IDE panel) + Bootstrap framework |
| Model Files | 9 | One per entity + AuditLog + DynamicCodeModels |
| Database Tables | 10 | 7 business + 1 audit log + 1 link item reference + 1 CustomCode |
| Audit Layers | 2 | Application-level (full row JSON) + Database-level (transactional) |
| Enhancements | 11 | Cross-DB, whitelist, audit, temp ID, pagination optimization, async migration, consolidated UPDATE, autocomplete, dependency checks, centralized config, Dynamic Code module |
| Problems Solved | 13 | Double-click rows, save click, SQL injection, identity race, page sizes, JSON formatting, orphan deletes, sync cleanup, cross-DB pagination, autocomplete positioning, Json-in-dynamic-scope, broken-code-save, SqlClient-hardcode |
| NuGet Packages | 33 | MVC 5.2.7, jQuery 3.4.1, Bootstrap 3.4.1, MySql.Data 8.0.33, Newtonsoft.Json 12.0.2 |
| Lines of Code (approx.) | ~13,000 | C# ~5,800, JavaScript ~3,100, CSS ~4,800, Razor ~1,000+ |

---

# 21. Dynamic Code Module — Detailed Architecture

## 21.1 Purpose

The Dynamic Code module allows administrators to write, compile, and store C# code snippets directly from the Report UI. These snippets are then intercepted and executed at runtime by the **ERP** application, enabling endpoint behavior (such as changing dropdown values on a master form) to be overridden without modifying or redeploying the ERP application.

Report is the authoring tool. The ERP is the execution environment. The two applications share the same database and communicate exclusively through the `CustomCode` table.

## 21.2 Database Schema — `CustomCode`

| Column | Type | Description |
|--------|------|-------------|
| CustomCodeId | int (PK) | Auto-increment identity; displayed as "Code ID" in the grid |
| ControllerName | nvarchar | MVC controller name to intercept (e.g., `VehicleCallBack`) |
| ActionName | nvarchar | MVC action name to intercept (e.g., `GetVehicleModelOnManufacturer`) |
| Namespaces | nvarchar | Comma-separated `using` statements (e.g., `System.Web.Mvc, System.Linq`) |
| CustomCode | nvarchar(max) | C# code body |
| Description | nvarchar | Human-readable purpose of the override |

- **No `HttpMethod` column** — the ERP matches by controller + action only (`CallBackService.GetByControllerAction`).
- `System.Collections.Generic` is stripped from the stored `Namespaces` value (the RSuite compiler header already includes it).
- `Report.*` namespaces are stripped (Report's own assemblies cannot be resolved by the ERP runtime process).

## 21.3 Architecture Overview

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  Report (admin authoring)                                                    │
│                                                                              │
│  ┌──────────────────┐  ┌────────────────────┐  ┌──────────────────────────┐  │
│  │ DynamicCode      │  │ NameSpace          │  │ Intellisense             │  │
│  │ Controller       │  │ Controller         │  │ Controller               │  │
│  │  Index (GET)     │  │  Suggest (GET)     │  │  Completions (POST)      │  │
│  │  SaveChanges     │  │  NamespaceEditor   │  │  Signatures (POST)       │  │
│  │  SaveCode        │  │   partial (GET)    │  │  CodeEditor partial      │  │
│  │  GetCode (JSON)  │  └────────────────────┘  └──────────────────────────┘  │
│  │  Delete (POST)   │                                                        │
│  └────────┬─────────┘                                                        │
│           │                                                                  │
│  ┌────────▼──────────────────────────────────────────────────┐               │
│  │ DynamicCodeService                                        │               │
│  │  GetAllAsync → in-memory sort + pagination                │               │
│  │  SaveChangesAsync → grouped INSERT/UPDATE + audit         │               │
│  │  SaveCodeFromIDEAsync → compile-guarded save (pending)    │               │
│  │  DeleteAsync → audit then delete                          │               │
│  └────────┬──────────────────────────────────────────────────┘               │
│           │                                                                  │
│  ┌────────▼─────────────────────┐   ┌──────────────────────┐                │
│  │ AuditLogService              │   │ RSuite DynamicCode   │                │
│  │  LogChangeAsync (before/after)│  │ Executor             │                │
│  └──────────────────────────────┘   │  CompileCode(...)    │                │
│                                     └──────────┬───────────┘                │
│           │                                    │ compilation                 │
│  ┌────────▼─────────────────────┐              │                            │
│  │ CustomCodeDAL                │◄─────────────┘                            │
│  │  CRUD (hardcoded SqlClient)  │                                           │
│  └────────┬─────────────────────┘                                           │
│           │                                                                  │
│      ┌────▼────┐                                                            │
│      │ DB Table │  CustomCode (shared with the ERP)                           │
│      └─────────┘                                                            │
└──────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────────┐
│  ERP (runtime execution)                                                  │
│                                                                              │
│  HTTP Request                                                                │
│    → Global.asax.cs  Application_BeginRequest                                │
│      → CallBackService.IsDynamicActionPresent(request.Url)                   │
│        → GetByControllerAction(controller, effectiveAction)                  │
│           (controller + action only; no HTTP verb check)                     │
│      → rewrite path to DynamicCallBack/Execute                               │
│                                                                              │
│  CallBackService.Execute()                                                   │
│    → CustomCodeDAL.GetCode(controller, action)                               │
│    → CompilerService.Compile(code, namespaces)                               │
│    → Activator.CreateInstance → MethodInfo.Invoke                             │
│    → HttpContext.Current.Response.Write(result) / return result              │
│                                                                              │
│  Recompiles per request (no cache) → edits in Report apply immediately      │
└──────────────────────────────────────────────────────────────────────────────┘
```

## 21.4 Report-Side Components

### 21.4.1 DynamicCodeController (`Controllers/DynamicCodeController.cs`)

| Action | HTTP | Purpose |
|--------|------|---------|
| `Index(DynamicCodeGridViewModel)` | GET | Renders the grid page. Populates `ViewBag.AuditTableName = "CustomCode"`, `ViewBag.RecordIdLabel = "Code ID"`. |
| `SaveChanges(SaveDynamicCodeChangesRequest)` | POST | Inline grid save. Delegates to `DynamicCodeService.SaveChangesAsync` with temp-ID mapping. |
| `GetCode(int id)` | GET | Returns a single row as JSON to populate the inline IDE panel fields. |
| `SaveCode(IDEViewModel)` | POST | Compiles the code via `RSuite.Infrastructure.Core.DynamicExecution.DynamicCodeExecutor.CompileCode(...)`, then saves via `DynamicCodeService.SaveCodeFromIDEAsync`. Uses `AssemblyManager.RemoveAsseblyModel` to clear cached assemblies. Strips `System.Collections.Generic` and `Report.*` namespaces before persisting. |
| `Delete(int id)` | POST | Delegates to `DynamicCodeService.DeleteAsync`. |
| `LoadClassesFromNamespaces(string Namespaces)` | POST | Returns all public type names from the given namespaces (for Monaco keyword autocomplete). |

**Known bug (§11.11):** `SaveCode` compiles via `executor.CompileCode(...)` but never inspects `rm.Success` / `rm.Errors` before calling `SaveCodeFromIDEAsync` — broken code can be saved and activated.

**Route cache comments:** Several actions contain commented-out `DynamicRouteCache.RefreshAsync()` calls — the ERP now owns the route cache; Report does not need to refresh it.

### 21.4.2 DynamicCodeService (`Services/DynamicCodeService.cs`)

| Method | Description |
|--------|-------------|
| `GetAllAsync(filter)` | Loads all rows via `CustomCodeDAL.GetAllAsync`, applies in-memory sort + skip/take pagination. Does not use SQL `COUNT(*) OVER()` because the table is small. |
| `SaveChangesAsync(changes, mode)` | Groups changes by row ID. Negative temp IDs → `CustomCodeDAL.AddAsync`. Existing IDs → capture before-image via `Dal.GetRowAsJsonAsync` → update → audit log (`UPDATE`). |
| `DeleteAsync(id)` | Captures before-image JSON → deletes → audit log (`DELETE`) inside try/catch. Audit failure never fails a completed delete. |
| `SaveCodeFromIDEAsync(model)` | For existing rows: loads current, calls `MergeForUpdate` (only overwrites non-blank incoming fields), updates, audits. For new rows: calls `AddAsync`. |
| `GetCodeByIdAsync(id)` / `GetByControllerActionAsync(controller, action)` | Read-only lookups on `CustomCodeDAL`. |
| `GetNextIdentityAsync()` | Returns next `CustomCodeId` for the grid's temp-ID generator. |

Audit logging uses `ReportConfig.DynamicCodeTable` (`CustomCode`) and the primary key column `CustomCodeId`.

### 21.4.3 CustomCodeDAL (`Services/CustomCodeDAL.cs`)

Standalone DAL for the `CustomCode` table. Uses `System.Data.SqlClient` directly (`SqlConnection`, `SqlCommand`, `SqlParameter`) — **does not** use `Dal.GetFactory()` or the `DbProviderFactory` abstraction.

| Method | Description |
|--------|-------------|
| `GetAllAsync(searchText)` | `SELECT * FROM CustomCode` with optional LIKE filter on `ControllerName`/`ActionName`/`Description`. |
| `GetByIdAsync(id)` | Single row by `CustomCodeId`. |
| `GetByControllerActionAsync(controller, action)` | Used by the ERP runtime path. |
| `AddAsync(model)` | `INSERT` returning `SCOPE_IDENTITY()`. |
| `UpdateAsync(model)` | `UPDATE` on all 5 data fields by `CustomCodeId`. |
| `DeleteAsync(id)` | `DELETE` by `CustomCodeId`. |
| `GetNextIdAsync()` | `SELECT ISNULL(MAX(CustomCodeId),0)+1`. |

**Cross-database limitation:** See §14.4 — works on SQL Server environments only; fails on MySQL (GEETEE).

### 21.4.4 NameSpaceController (`Controllers/NameSpaceController.cs`)

| Action | Purpose |
|--------|---------|
| `NamespaceEditor(string ids, ...)` (partial) | Renders the `_NamespaceEditor.cshtml` partial: a textarea for namespace entry + a suggestion panel. Builds a namespace trie from `AppDomain.CurrentDomain.GetAssemblies()`. |
| `Suggest(string filter, string namespc, string parent)` | GET: returns JSON array of matching namespace/class names for the suggestion panel. |

### 21.4.5 IntellisenseController (`Controllers/IntellisenseController.cs`)

| Action | Purpose |
|--------|---------|
| `CodeEditor(...)` (partial) | Renders the `_CodeEditor.cshtml` partial — Monaco editor with IntelliSense providers wired up. |
| `Completions(string fullcode, string expression)` | POST: resolves the dot-expression type and returns its public properties and methods as Monaco `CompletionItem`s. Uses `Microsoft.CSharp.CSharpCodeProvider` to infer the expression's compile-time type, then reflect on it. |
| `Signatures(string expression)` | POST: returns method parameter signatures for the given method expression. |

**Type/member caching:** `IntellisenseController` maintains static `Dictionary<string, Type>` caches of resolved types and their members for the session lifetime. Includes a test-only `Vehicle` class at the top of the file for manual IntelliSense verification.

### 21.4.6 Views

#### `Views/DynamicCode/Index.cshtml`

Grid page with the following sections:
- **Toolbar:** search box, sort-column dropdown (Code ID / Controller Name / Action Name / Description), page-size selector, Add Row button.
- **Grid table:** columns (Code ID, Controller Name, Action Name, Description, Actions). Actions column contains Edit, IDE, and Delete buttons. Inline edit form uses the same `data-*` attribute pattern as other entities.
- **IDE panel** (`#ideSection`): embedded in the page; shown/hidden via `DynamicCode.js showIDE()`. Fields: Controller Name, Action Name, Description, Namespaces (textarea + `_NamespaceEditor` suggestion box), Code (Monaco via `_CodeEditor.cshtml`).
- **Audit handlers:** inline JS handlers for View Audit / Deleted Records call `AuditTrail/AuditLogs.cshtml` with `AuditTableName=CustomCode`, `RecordIdLabel=Code ID`.
- **Pagination:** same Bootstrap 3 pagination as other entity grids.

#### `Views/DynamicCode/IDE.cshtml`

Standalone IDE page (Layout = null). Loads the same `_CodeEditor.cshtml` Monaco partial. Used as a fallback; the primary IDE experience is the inline panel in `Index.cshtml`.

#### `Views/Shared/_CodeEditor.cshtml`

Razor partial that renders a full Monaco editor (monaco-editor 0.45.0 loaded from `cdnjs.cloudflare.com` CDN). Provides three custom providers:

| Provider | Trigger | Purpose |
|----------|---------|---------|
| `registerCompletionItemProvider("csharp")` | typing | Keyword + class name autocomplete (server-provided class list + C# keyword list) |
| `registerCompletionItemProvider("csharp", ["."])` | `.` | Dot-completion: posts the expression prefix to `IntellisenseController.Completions` and returns properties/methods |
| `registerSignatureHelpProvider("csharp", ["("])` | `(` | Method signature help: posts to `IntellisenseController.Signatures` and returns parameter signatures |

Editor config: `language: "csharp"`, `theme: "vs-dark"`, `automaticLayout: true`, `quickSuggestions: { other: true }`, `wordBasedSuggestions: "off"`. Hidden field syncs editor content on content change and form submit.

#### `Views/Shared/_NamespaceEditor.cshtml`

Razor partial with a textarea and a suggestion `<ul>`. Suggestion list populated via AJAX to `NameSpaceController.Suggest`. Selected namespace appended to textarea.

### 21.4.7 JavaScript — `js/DynamicCode.js`

| Function | Purpose |
|----------|---------|
| `showIDE(id)` | Fetches row via `DynamicCode/GetCode?id=...`, populates IDE panel fields, calls `loadClassesForNamespaces`, shows `#ideSection`. |
| `hideIDE()` / `clearIDE()` | Hides IDE panel and clears all fields. |
| `loadClassesForNamespaces(namespaces)` | Posts to `NameSpaceController.Suggest` with a filter; results are passed to Monaco via `window.updateClasses(...)` (defined in `_CodeEditor.cshtml`). |
| Grid handlers | Add Row, Save (collectChanges + confirm modal + `SaveChanges` AJAX), Delete (confirm + `Delete` AJAX), Edit row, Open IDE, View Audit / Deleted Records (open `AuditTrail/AuditLogs.cshtml` in iframe via same handler pattern as other modules). |

### 21.4.8 CSS — `Styles/DynamicCode.css`

843 lines. Key sections:
- **Grid** (same design-system tokens as other entities): `.modern-table-shell`, `.modern-report-table`, `.grid-text`, `.action-cell`, `.edit-row`, `.editing`.
- **Toolbar + pagination:** `.report-toolbar`, `.bottom-bar`, `.pagination`.
- **Modals:** `#deleteConfirmModal`, `#saveConfirmModal`, `#successModal` (same style as other entities).
- **IDE panel** (`#ideSection`): `.ide-panel`, `.ide-panel-header`, `.ide-panel-fields` (3-column grid), `.ide-panel-namespaces` (fixed 420px), `.ide-panel-editor` (flex), `.ide-panel-footer`. Monaco height fix: `#ideSection .editor-container > div { height: 420px !important }`.
- Responsive: `@media (max-width: 991px)` and `@media (max-width: 575px)` rules.

### 21.4.9 Models — `Models/DynamicCodeModels.cs`

| Class | Purpose |
|-------|---------|
| `DynamicCodeGridItem` | Grid row DTO: `DynamicCodeId`, `ControllerName`, `ActionName`, `Description`. |
| `DynamicCodeChange` | Single cell change DTO: `DynamicCodeId`, `Column`, `OldValue`, `NewValue`, `IsNew`. |
| `SaveDynamicCodeChangesRequest` | POST body: `Mode` (SAVE/CANCEL), `List<DynamicCodeChange>`. |
| `SaveDynamicCodeChangesResult` | POST response: `Status`, `Dictionary<string,int> IdMappings`. |
| `DynamicCodeGridViewModel` | Grid VM: `SearchText`, `SortColumn`/`SortDirection`, `PageNumber`, `PageSize`, `TotalRecords`, `Items`. |
| `IDEViewModel` | IDE form VM: `CodeID`, `ControllerName`, `ActionName`, `Namespaces`, `Code`, `Description`. |

### 21.4.10 Configuration

| Source | Key | Value | Purpose |
|--------|-----|-------|---------|
| `Web.config` (line 47) | `DynamicCodeTable` | `CustomCode` | Table name for all Dynamic Code DAL queries |
| `Web.config` (line 48) | `DynamicCodePageSize` | `7` | Default grid page size |
| `Web.config` (line 51) | `DefaultNamespaces` | `Report.Controllers, Report.Services, System.Web.Mvc, System.Collections.Generic, System.Linq` | Base `using` set; shown in IDE panel on load |
| `ReportConfig.cs` | `DynamicCodeTable` | reads from `Web.config` | Static accessor consumed by `DynamicCodeService` and `CustomCodeDAL` |

## 21.5 ERP Runtime Model

### 21.5.1 Request Interception

`Global.asax.cs` registers `Application_BeginRequest`, which calls:

```
CallBackService.IsDynamicActionPresent(request.Url)
```

This resolves the controller and action from the URL path, then calls `CustomCodeDAL.GetByControllerAction(controller, action)`. If a matching row exists, the request path is rewritten to `DynamicCallBack/Execute` and the original controller/action logic is bypassed entirely.

**Matching is by controller + action only** — no HTTP verb (GET/POST) is considered. If both GET and POST to the same controller/action have different semantics, only one override is possible.

### 21.5.2 Compilation and Execution

`CallBackService.Execute()` reads the `Namespaces` and `CustomCode` fields, invokes `CompilerService.Compile(code, namespaces)`, and then:

```csharp
// Simplified from CallBackService.cs
var assembly = compileResult.CompiledAssembly;
var instance = assembly.CreateInstance("DynamicCode.Namespace.ClassName");
var method = instance.GetType().GetMethod("Execute");
var result = method.Invoke(instance, null);
// result is written directly to the HTTP response
```

**Recompiles every request** — there is no assembly cache. Edits saved in Report take effect on the very next matching ERP request.

### 21.5.3 Code Body Requirements

The compiled class is **not** a `Controller`. The `Json()` helper (inherited from `Controller`) does not exist in scope.

```csharp
// ✗ WRONG — "The name 'Json' does not exist in the current context"
return Json(new { data = "value" });

// ✓ CORRECT — explicit JsonResult construction
return new JsonResult
{
    Data = new { ProductModelCollection = new[] { new { Id = 1, label = "DYNAMIC MODEL" } } },
    JsonRequestBehavior = JsonRequestBehavior.AllowGet
};
```

**Verified override example:**
- Controller: `VehicleCallBack`
- Action: `GetVehicleModelOnManufacturer`
- Code: returns `ProductModelCollection` with a single DYNAMIC MODEL option
- Visible on the ERP Vehicle master page (`/Vehicle?...`) Manufacturer → Vehicle Model dropdown (new-record form)

## 21.6 Audit Integration

- Grid UPDATE and DELETE operations follow the same audit pattern as all other Report entities: `DynamicCodeService.GetRowAsJsonAsync()` captures the before-image via `Dal.GetRowAsJsonAsync(ReportConfig.DynamicCodeTable, "CustomCodeId", id)` and writes it to `com_mst_audit_log` via `AuditLogService.LogChangeAsync`.
- Audit capture on DELETE is wrapped in a try/catch — a completed delete is never rolled back due to audit failure.
- The grid includes a **Deleted Records** button (same pattern as other modules) that opens `AuditTrail/AuditLogs.cshtml` in an iframe with `AuditTableName=CustomCode` and `RecordIdLabel=Code ID`.

## 21.7 Known Issues

| # | Issue | Location | Impact | Status |
|---|-------|----------|--------|--------|
| 1 | **SaveCode persists broken code** | `DynamicCodeController.cs:180-198` | A snippet that fails compilation is still saved; the ERP then errors on every matching request | Open (§11.11) |
| 2 | **CustomCodeDAL hardcodes SqlClient** | `CustomCodeDAL.cs` | Dynamic Code screen fails on MySQL (GEETEE) | Open (§14.4) |
| 3 | **No HTTP verb in matching** | `CallBackService` (ERP) | GET and POST to the same controller/action cannot be overridden independently | Current design limitation |
| 4 | **Dead override row** | `CustomCode` table data | `VehicleCallBack/GetVehicleModelByVehicle` has no UI callers; its override row generates errors on every GET | Data cleanup needed |
| 5 | **Monaco from CDN** | `_CodeEditor.cshtml` | External network dependency; IDE won't load without internet | Low priority |

## 21.8 Verification Test Case

**Override tested:** `VehicleCallBack/GetVehicleModelOnManufacturer` (GET)

| Step | Detail |
|------|--------|
| 1 | In Report, open Dynamic Code grid → Add Row: Controller = `VehicleCallBack`, Action = `GetVehicleModelOnManufacturer`, Description = `Override Vehicle Model on Vehicle master` |
| 2 | Open IDE → Namespaces = default (`System.Web.Mvc, System.Linq`) |
| 3 | Code: `return new JsonResult { Data = new { ProductModelCollection = new[] { new { Id = 1, label = "DYNAMIC MODEL" } } }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };` |
| 4 | Click Save Code |
| 5 | In the ERP, open the Vehicle master → New form: change Manufacturer dropdown → Vehicle Model dropdown should show **DYNAMIC MODEL** |
| 6 | On edit forms, the effect may not be visible due to `Vehicle.js:72-74` (resets Vehicle Model to current value on Manufacturer change) — verify using the New form or network inspection |

---

*End of Technical Design Document*
