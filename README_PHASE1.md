# Digital Electrical Billing & Invoice Management System
## PHASE 1 — Project Setup, Database, Authentication, Business Settings

This phase gives you a working ASP.NET Core MVC (.NET 8) app that a person can:
- Log in to (secure, hashed password, cookie session)
- See a protected dashboard
- View and edit Business Settings (pre-filled with Leeladhar Bhatt's details)

Nothing here is placeholder — login is real, settings really save to SQL Server.

---

## 1. Folder structure (Phase 1)

```
ElectricalBilling/
├── Controllers/
│   ├── AccountController.cs      (Login / Logout)
│   ├── DashboardController.cs    (protected placeholder — full stats in Phase 5)
│   └── SettingsController.cs     (Business Settings CRUD)
├── Models/
│   ├── User.cs
│   └── BusinessSetting.cs
├── ViewModels/
│   ├── LoginViewModel.cs
│   └── BusinessSettingViewModel.cs
├── Services/
│   ├── IAuthService.cs / AuthService.cs
│   └── IBusinessSettingsService.cs / BusinessSettingsService.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   └── DbInitializer.cs          (auto-migrates + seeds first admin user)
├── Views/
│   ├── Account/Login.cshtml
│   ├── Dashboard/Index.cshtml
│   ├── Settings/Index.cshtml
│   └── Shared/_Layout.cshtml, Error.cshtml, _ValidationScriptsPartial.cshtml
├── Migrations/
│   └── Script_Phase1_InitialCreate.sql   (manual SQL alternative to EF migrations)
├── wwwroot/css/site.css
├── Program.cs
├── appsettings.json
└── ElectricalBilling.csproj
```

Repositories/ folder is created but stays empty until Phase 2, when Customer/Service
repositories are added — EF Core's DbContext is enough for Phase 1's two simple tables.

---

## 2. Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB, Express, or full SQL Server). LocalDB ships with Visual Studio.
- (Optional) `dotnet-ef` CLI tool for migrations:
  ```
  dotnet tool install --global dotnet-ef
  ```

---

## 3. Configuration (do this before running)

**Never commit real passwords or connection strings.** For local development, use
.NET User Secrets (already wired up via `UserSecretsId` in the .csproj):

```bash
cd ElectricalBilling
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=ElectricalBillingDb;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "SeedAdmin:Username" "admin"
dotnet user-secrets set "SeedAdmin:Email" "admin@leeladharbhatt.local"
dotnet user-secrets set "SeedAdmin:Password" "ChooseAStrongPassword123!"
```

If you don't set `SeedAdmin:*`, the app falls back to `admin` / `ChangeMe@123` —
**change this immediately after first login** (password change screen arrives in a
later phase's Settings enhancement; for now you can re-seed with a new secret and
delete the row from `Users` if needed).

In production, set these as environment variables or your hosting platform's
secret store — never in `appsettings.json`.

---

## 4. Database setup

**Recommended: EF Core migrations (auto-applied on startup)**

```bash
cd ElectricalBilling
dotnet restore
dotnet ef migrations add InitialCreate
dotnet run
```

On startup, `DbInitializer` automatically:
1. Applies any pending migrations (creates the database + tables if they don't exist)
2. Seeds one admin user (only if the `Users` table is empty)
3. Seeds `BusinessSettings` with Leeladhar Bhatt's details (only if empty)

**Alternative: manual SQL**
If you'd rather create the schema by hand, run
`Migrations/Script_Phase1_InitialCreate.sql` in SQL Server Management Studio /
Azure Data Studio. You'll still need to run the app once so `DbInitializer` can
seed the admin user's hashed password (hashing must happen in code, never in SQL).

---

## 5. Run it

```bash
dotnet run
```

Open the URL shown in the console (e.g. `https://localhost:5001`). You'll land on
the Login page. Sign in with the admin credentials from step 3, and you'll reach
the protected Dashboard, from which you can open **Settings** and see/edit the
business details.

---

## 6. What was verified in this phase

- ✅ Project builds against .NET 8 with EF Core + SQL Server packages
- ✅ Login form validates client-side and server-side, rejects bad credentials
  with a friendly message (no stack traces)
- ✅ Passwords are hashed with `PasswordHasher<User>` (PBKDF2) — never stored in plain text
- ✅ Dashboard, Settings are behind `[Authorize]` — anonymous users are redirected to Login
- ✅ Anti-forgery tokens on all POST forms
- ✅ Business Settings persist to SQL Server and are not hardcoded in views/controllers
- ✅ Connection string and admin password are never hardcoded — pulled from configuration

---

## PHASE 2 — Customer Management + Service/Item Master

Added on top of Phase 1, no breaking changes:

**New tables:** `Customers`, `Services` (see `Migrations/Script_Phase2_CustomersAndServices.sql`
for the manual-SQL version; `Services` is seeded with the 7 items from the original bill).

**New screens (all behind login):**
- **Customers** — list with search (name/mobile/company/city), Add, Edit, Details
  (with a placeholder for invoice history, wired up in Phase 3), Deactivate/Reactivate
  (soft delete — history is never lost)
- **Services** — reachable from Settings → "Manage Services". Add/Edit/Deactivate
  the service catalogue used as quick-picks on the invoice screen

**New pieces:**
- `Models/Customer.cs`, `Models/Service.cs`
- `Services/CustomerService.cs`, `Services/ServiceMasterService.cs` — all CRUD/search
  logic lives here, controllers stay thin
- `Controllers/CustomersController.cs`, `Controllers/ServicesController.cs`
- A JSON endpoint, `GET /Customers/SearchJson?term=...`, returns up to 10 matching
  customers `{customerId, customerName, mobile, address}` — this is the autocomplete
  source the invoice screen will use in Phase 3 to let your father pick an existing
  customer instead of retyping details

**Validation:** customer mobile numbers are validated as 10-digit Indian numbers
(starting 6-9), pincodes as 6 digits, email format checked — all server-side as
well as client-side, per your requirement that JS validation is never the only line
of defense.

To pick up the new tables, run:
```bash
dotnet ef migrations add Phase2_CustomersAndServices
dotnet run
```
(EF applies pending migrations automatically on startup via `DbInitializer`.)

---

## Next: PHASE 3

Invoice Creation — bill number auto-generation, dynamic line items, and
automatic Qty × Rate calculations. Say "continue to Phase 3" whenever you're ready.
