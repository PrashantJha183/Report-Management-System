# Notion Additions — Dynamic Code Module

Paste-ready blocks to add to the **Report Management System** Notion page
(`https://orchid-amber-92d.notion.site/Report-Management-System-38394ea95aab80488ba2ee66bf6d41c6`).

All blocks are **insert-only** — nothing existing is edited. Each section names the anchor (where to paste) and provides the markdown blocks to paste. Notion converts these headings/tables/code fences into native blocks on paste.

---

## 1. Anchor: 1.2.1 Administrative Modules

Paste a divider + a paragraph block immediately AFTER the paragraph "The Report Management System consists of seven independent administrative modules..." and its folder tree code block, and INSERT the extra tree lines into that tree code block.

Paste this paragraph block:

> In addition to the module list above, the system hosts the **Dynamic Code module** — an administration IDE for authoring C# runtime code overrides. Snippets are stored in the shared `CustomCode` table and executed at runtime by the ERP application, allowing endpoint behavior to be overridden without redeploying the ERP (see 4.9 Dynamic Code Module).

Insert these lines into the existing folder-tree code block:

```
├── Controllers/
│   ├── DynamicCodeController.cs
│   ├── NameSpaceController.cs
│   └── IntellisenseController.cs
├── Services/
│   ├── DynamicCodeService.cs
│   └── CustomCodeDAL.cs
├── Models/
│   └── DynamicCodeModels.cs
├── Views/
│   ├── DynamicCode/
│   │   ├── Index.cshtml
│   │   └── IDE.cshtml
│   └── Shared/
│       ├── _CodeEditor.cshtml
│       └── _NamespaceEditor.cshtml
├── js/
│   └── DynamicCode.js
└── Styles/
    └── DynamicCode.css
```

---

## 2. Anchor: 3.1.2 Key Components (Presentation Layer)

Append these bullets at the end of the Key Components bullet list:

### Dynamic Code Key Components

- Views/DynamicCode/Index.cshtml — Dynamic Code grid + inline Monaco IDE panel
- Views/DynamicCode/IDE.cshtml — standalone IDE page (Layout = null)
- Views/Shared/_CodeEditor.cshtml — monaco-editor 0.45.0 partial (C# language, vs-dark theme, IntelliSense providers)
- Views/Shared/_NamespaceEditor.cshtml — namespace textarea + autocomplete suggestion panel
- js/DynamicCode.js — grid/IDE client logic (showIDE, populateIDE, loadClassesForNamespaces)
- Styles/DynamicCode.css — Dynamic Code grid + IDE panel styling

---

## 3. Anchor: 3.2.2 Controllers (Controller Layer)

Append these bullets to the Controllers list:

### Dynamic Code Controllers

- DynamicCodeController — grid listing, inline save (SaveChanges), IDE save (SaveCode with compilation), GetCode, Delete
- NameSpaceController — namespace autocomplete (Suggest) for the namespace editor
- IntellisenseController — Monaco dot-completion (Completions) and method signature help (Signatures)

---

## 4. Anchor: 3.3.2 Core Services (Service Layer)

Append these bullets to the Core Services list:

### Dynamic Code Services

- DynamicCodeService — grid CRUD with temp-ID mapping, IDE save (SaveCodeFromIDEAsync), audit logging on UPDATE/DELETE
- CustomCodeDAL — CustomCode table CRUD (GetAll/GetById/GetByControllerAction/Add/Update/Delete/GetNextId)

The two services bring the total service count from 10 to 12.

---

## 5. Anchor: 3.4 Data Access Layer / 3.5 Configuration Layer

Insert a short note block after the Data Access layer description, and add the Web.config keys as bullets under 3.5.2 Key Components.

### Dynamic Code — Data Access Note

Unlike the main `IDal`/`Dal` path, `CustomCodeDAL` does NOT use the `DbProviderFactory` abstraction — it hardcodes `System.Data.SqlClient`. The Dynamic Code module therefore works on SQL Server environments but fails on MySQL (GEETEE). See section 6.7 Dynamic Code Cross-Database Limitation.

### Dynamic Code Web.config Settings (append to 3.5.2 Key Components bullets)

- Web.config → `DynamicCodeTable` = `CustomCode` — backing table for Dynamic Code entries
- Web.config → `DynamicCodePageSize` = `7` — grid page size
- Web.config → `DefaultNamespaces` = `Report.Controllers, Report.Services, System.Web.Mvc, System.Collections.Generic, System.Linq` — base using set applied to every snippet

---

## 6. Anchor: 4. Core Business Modules (new 4.9 Dynamic Code Module)

Paste a divider, then this full deep-dive section at the END of section 4 (after 4.8 User Management).

### 4.9 Dynamic Code Module

The Dynamic Code module allows administrators to write, compile, and store C# code snippets from Report. These snippets are intercepted and executed at runtime by the ERP application, enabling endpoint behavior to be overridden without modifying or redeploying the ERP.

#### 4.9.1 Capabilities

- Grid-based management of runtime code overrides with search, sort, and pagination
- Inline Monaco editor (monaco-editor 0.45.0 via CDN) with C# syntax, keyword/class autocomplete, dot-completion, and method signature help
- Namespace autocomplete while authoring (suggestion tree built from loaded assemblies)
- Server-side compilation on save using the RSuite DynamicCodeExecutor
- Full audit trail: before-image JSON snapshot is logged for every UPDATE and DELETE
- Deleted Records viewer integration (opens AuditTrail/AuditLogs.cshtml with AuditTableName = CustomCode)

#### 4.9.2 Architectural Components

Table: CustomCode

| Column | Type | Description |
|--------|------|-------------|
| CustomCodeId | int (PK) | Auto-increment identity; shown as "Code ID" in the grid |
| ControllerName | nvarchar | Controller to intercept (e.g., VehicleCallBack) |
| ActionName | nvarchar | Action to intercept (e.g., GetVehicleModelOnManufacturer) |
| Namespaces | nvarchar | Comma-separated using namespaces for compilation |
| CustomCode | nvarchar(max) | C# code body — must return a JsonResult |
| Description | nvarchar | Human-readable purpose of the override |

There is NO HttpMethod column — the ERP matches by controller + action name only.

Controllers:

- DynamicCodeController
  - Index (GET) — grid page; sets ViewBag.AuditTableName = "CustomCode", ViewBag.RecordIdLabel = "Code ID"
  - SaveChanges (POST) — inline grid save with temp-ID mapping
  - SaveCode (POST) — compiles via RSuite DynamicCodeExecutor, then SaveCodeFromIDEAsync
  - GetCode (GET) — returns a row as JSON for the inline IDE panel
  - Delete (POST) — delete with audit capture
  - LoadClassesFromNamespaces (POST) — type names for Monaco autocomplete
- NameSpaceController — Suggest (GET) namespace/class suggestions; NamespaceEditor partial
- IntellisenseController — Completions (POST) dot-completion; Signatures (POST) method signature help; CodeEditor partial

Services:

- DynamicCodeService — GetAllAsync (in-memory sort/pagination), SaveChangesAsync, DeleteAsync, SaveCodeFromIDEAsync, GetByControllerActionAsync
- CustomCodeDAL — CRUD for the CustomCode table

Views:

- Views/DynamicCode/Index.cshtml — grid + inline IDE panel (#ideSection) + audit/Deleted Records handlers
- Views/DynamicCode/IDE.cshtml — standalone IDE fallback page
- Views/Shared/_CodeEditor.cshtml — Monaco editor partial with 3 IntelliSense providers
- Views/Shared/_NamespaceEditor.cshtml — namespace entry with suggestion list

Client-side: js/DynamicCode.js (showIDE, hideIDE, clearIDE, loadClassesForNamespaces, grid handlers). Styling: Styles/DynamicCode.css (grid + IDE panel).

Namespace handling on save (DynamicCodeController.SaveCode):

- System.Collections.Generic is excluded from the persisted Namespaces value (the compiler header already includes it)
- Report.* namespaces are excluded (Report's own assemblies cannot be resolved by the ERP runtime process)
- gacList: System.Web.dll, Microsoft.CSharp.dll

#### 4.9.3 Runtime Execution Model

```
HTTP Request to ERP controller/action
  → Global.asax.cs Application_BeginRequest
    → CallBackService.IsDynamicActionPresent(url)
        → GetByControllerAction(controller, action)   # controller + action only, no HTTP verb
        → on match: rewrite URL to DynamicCallBack/Execute
    → CallBackService.Execute
        → read Namespaces + CustomCode from CustomCode table
        → CompilerService.Compile(code, namespaces)   # recompiled EVERY request, no cache
        → Activator.CreateInstance → MethodInfo.Invoke
        → result written to HTTP response
```

Because every request is recompiled, a snippet saved in Report is active on the very next ERP request.

Code body requirement — the generated wrapper class is NOT a Controller, so the Json() helper does not exist:

```
✗ WRONG — "The name 'Json' does not exist in the current context"
return Json(new { data = "value" });

✓ CORRECT — explicit JsonResult construction
return new JsonResult
{
    Data = new { ProductModelCollection = new[] { new { Id = 1, label = "DYNAMIC MODEL" } } },
    JsonRequestBehavior = JsonRequestBehavior.AllowGet
};
```

Verified override: VehicleCallBack / GetVehicleModelOnManufacturer returns ProductModelCollection with a single DYNAMIC MODEL option, visible in the Vehicle Model dropdown on the ERP Vehicle master page (new-record form).

#### 4.9.4 Audit Integration

- UPDATE: before-image JSON captured via Dal.GetRowAsJsonAsync(DynamicCodeTable, "CustomCodeId", id) → AuditLogService.LogChangeAsync writes to com_mst_audit_log
- DELETE: same before-image capture; the audit write is wrapped in try/catch so a completed delete is never rolled back by an audit failure
- The grid exposes an inline Deleted Records button opening AuditTrail/AuditLogs.cshtml with AuditTableName = CustomCode, RecordIdLabel = Code ID

#### 4.9.5 Known Issues

| # | Issue | Impact | Location |
|---|-------|--------|----------|
| 1 | SaveCode persists broken code | Compile-failed snippet is still saved and activated in the ERP | DynamicCodeController.cs (compile result never checked before save) |
| 2 | CustomCodeDAL hardcodes SqlClient | Dynamic Code fails on MySQL (GEETEE) | CustomCodeDAL.cs (bypasses DbProviderFactory) |
| 3 | Matching without HTTP verb | GET and POST to the same controller/action cannot be overridden independently | CallBackService (ERP) |
| 4 | Monaco loaded from CDN | IDE unavailable without internet access | Views/Shared/_CodeEditor.cshtml |
| 5 | No sandbox | Snippets run with full process privileges, gated only by role | Dynamic Code execution path |

---

## 7. Anchor: 6. Cross Database Support (new 6.7)

Paste at the end of section 6 (after 6.6 Database Provider Features).

### 6.7 Dynamic Code Cross-Database Limitation

The main data access path (IDal/Dal) is provider-agnostic through DbProviderFactory, but CustomCodeDAL is the exception:

- CustomCodeDAL instantiates System.Data.SqlClient types directly and does not consult the session-selected DbType
- Consequence: Dynamic Code works on SQL Server environments but fails on MySQL (GEETEE) with a provider/type error as soon as the Dynamic Code screen queries the CustomCode table
- Intended fix: mirror the GetFactory() provider switch (MYSQL → MySqlClientFactory.Instance, SQLSERVER → SqlClientFactory.Instance) inside CustomCodeDAL, resolving the provider from the session DbType

---

## 8. Anchor: 9. Audit Logging Architecture (new 9.6)

Paste at the end of section 9 (after 9.5).

### 9.6 Dynamic Code Audit Integration

- Grid UPDATE and DELETE operations follow the same audit pattern as every business module: before-image JSON snapshot → com_mst_audit_log
- DELETE audit capture is wrapped in a try/catch — a completed delete is never rolled back due to audit failure
- The Dynamic Code grid provides a Deleted Records button that opens the audit viewer filtered to the CustomCode table (AuditTableName = CustomCode, RecordIdLabel = Code ID)

---

## 9. Anchor: 12. Major Enhancements Implemented (new 12.12)

Paste at the end of section 12 (after 12.11 User Management Module).

### 12.12 Dynamic Code Module

#### 12.12.1 Business Challenge

There was no way to override or extend ERP endpoint behavior without modifying and redeploying the ERP application.

#### 12.12.2 Solution Implemented

- Report provides a CRUD grid plus an inline Monaco IDE where administrators author C# snippets
- Snippets are stored in the shared CustomCode table mapped by controller + action
- The ERP's CallBackService intercepts matching requests at runtime and executes the stored code, recompiling on every request so edits take effect immediately

#### Technical Implementation

- DynamicCodeController / NameSpaceController / IntellisenseController; DynamicCodeService + CustomCodeDAL
- Monaco 0.45.0 (CDN) with keyword/dot-completion and signature help via IntellisenseController
- Compilation via the RSuite DynamicCodeExecutor; snippet bodies must construct JsonResult explicitly

#### Benefits

- Endpoint behavior (e.g., dropdown options) can be overridden without redeploying the ERP
- Changes propagate on the next request (recompiled per request)
- Full audit trail on override create/update/delete

---

## 10. Anchor: 13. Security, Deployment & Operational Considerations (new 13.11)

Paste at the end of section 13 (after 13.10 Password Security).

### 13.11 Dynamic Code Security Considerations

- Dynamic Code executes arbitrary compiled C# in the ERP process — a high-privilege capability gated by authentication and role only
- There is no sandbox: a snippet has access to the ERP's process, configuration, and database connection
- The Monaco editor is loaded from a CDN — the IDE depends on internet access; consider local bundling for air-gapped environments
- Recommended controls: restrict Dynamic Code to the administrator role, add a publish/approval workflow for production overrides, and audit all override writes (already enforced via com_mst_audit_log)