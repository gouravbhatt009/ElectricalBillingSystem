# Phase 3 — Invoice Creation, Dynamic Items, Calculations

Builds on Phase 1 (auth, settings) and Phase 2 (customers, service master).
This phase adds the core billing screen: creating, editing, listing, and
viewing invoices, with automatic Qty × Rate and GST calculation.

## What's new

### Folder structure additions

```
/Controllers
    InvoicesController.cs
/Models
    Invoice.cs
    InvoiceItem.cs
    InvoiceStatus.cs
/ViewModels
    InvoiceViewModel.cs
    InvoiceItemInputViewModel.cs
    InvoiceListItemViewModel.cs      (also holds InvoiceSearchFilter)
    InvoiceDetailsViewModel.cs
/Services
    IInvoiceService.cs
    InvoiceService.cs
/Helpers
    AmountInWordsConverter.cs
/Views
    /Invoices
        Index.cshtml         (search + history list)
        Create.cshtml
        Edit.cshtml
        Details.cshtml
        _InvoiceForm.cshtml  (shared by Create/Edit)
/wwwroot/js
    invoice.js               (dynamic rows, live totals, customer autocomplete)
/Migrations
    Script_Phase3_Invoices.sql
```

### Modified files

- **Data/ApplicationDbContext.cs** — added `Invoices` / `InvoiceItems`
  `DbSet`s, unique index on `InvoiceNumber`, decimal precision, and FK
  cascade rules.
- **Program.cs** — registered `IInvoiceService`.
- **Services/CustomerService.cs** — `GetDetailsAsync` now computes real
  `TotalInvoices` / `TotalBilledAmount` / `TotalPendingAmount` from the
  Invoices table (previously a placeholder).
- **Views/Customers/Details.cshtml** — shows the billing summary and a
  "+ New Bill for this Customer" shortcut.

## Design decisions worth knowing about

- **Invoice numbers** (`ELB-2026-0001`) are generated server-side by
  finding the highest existing number for the prefix + current year and
  incrementing. On the rare chance two saves race at the same instant,
  `CreateAsync` retries up to 5 times on a unique-constraint violation —
  duplicates are structurally impossible (`UQ_Invoices_InvoiceNumber`).
- **Totals are never trusted from the browser.** The JS on the Create/Edit
  screen calculates Subtotal/CGST/SGST/Grand Total live so your father sees
  the right numbers as he types, but `InvoiceService` recomputes everything
  server-side from the posted line items before saving. Only `GstEnabled`,
  `CgstPercent`, and `SgstPercent` are taken from the form (legitimate user
  choices); `SubTotal`/`CgstAmount`/`SgstAmount`/`GrandTotal` are always
  server-derived.
- **GST is a per-invoice choice.** The Create screen pre-fills the GST
  toggle and percentages from Business Settings, but your father can turn
  GST on/off (and adjust the percentages) for that specific bill — matches
  "GST must be optional" from the spec while still giving sensible defaults.
- **GST is snapshotted onto the invoice** (not looked up live from
  Settings) so that changing the default GST% later never alters an
  already-issued bill.
- **Service quick-pick + custom text both work.** Each item row has a
  dropdown of active Service Master entries; picking one fills the
  description and default rate (editable). Choosing "-- Custom --" leaves
  the text box free for manual typing, per the spec.
- **A blank leftover row never blocks saving.** `Qty`/`Rate` are nullable
  on the input model, and the service silently drops any row without a
  description or with zero/blank quantity — so a stray empty row a user
  forgot to remove doesn't throw a validation error.
- **Cancel, not delete.** Invoices can be marked Cancelled (soft) but are
  never hard-deleted, preserving the billing history. Cancelled invoices
  can't be edited further.
- **Payments/PDF/Print/WhatsApp are intentionally not wired yet** — the
  Details page has a small note about that so nothing looks broken. They
  arrive in Phase 4 (PDF/Print/WhatsApp) and Phase 5 (Payments, full
  Dashboard stats). No dead buttons were added for them.

## Applying the database changes

**Option A — EF Core migrations (recommended):**

```bash
dotnet ef migrations add Phase3_Invoices
dotnet ef database update
```

**Option B — run the SQL script directly** (if you're not using EF
migrations tooling):

```
Migrations/Script_Phase3_Invoices.sql
```

Run it after `Script_Phase1_InitialCreate.sql` and
`Script_Phase2_CustomersAndServices.sql`.

## How to run

Same as Phase 1/2:

```bash
dotnet restore
dotnet run
```

Then log in and go to **Bills → + New Bill**.

## Testing this phase against the acceptance flow

This phase covers the invoice-creation slice of the Section 25 acceptance
test:

1. Login → Dashboard → Customers → Add a customer (Phase 2)
2. **Bills → + New Bill**
3. Search and select the customer (type 2+ characters of name or mobile)
4. Pick "Power Factor Maintenance" from the item dropdown — description
   and rate auto-fill; adjust Qty to 1
5. Click **+ Add Item**, pick or type a second item, enter its Qty/Rate
6. Watch Subtotal/Grand Total update live as you type
7. Toggle **Add GST** on/off and watch CGST/SGST rows appear/disappear
8. Click **Save Invoice** → redirects to the Details page showing the
   bill number, items, totals, and Amount in Words
9. **Bills** → confirm the new invoice appears in the list; search by bill
   number, customer name, mobile, date range, and status
10. Open the bill → **Edit Bill** → change a quantity → Save → confirm
    totals recalculated
11. **Cancel Invoice** on a test bill → confirm it shows as Cancelled and
    Edit is no longer offered
12. Go back to that customer's **Details** page → confirm Total Invoices /
    Total Billed / Total Pending now reflect the bills you created

Everything above should compile and run cleanly. Let me know once you've
tried it and I'll continue with **Phase 4: PDF generation, Print, and the
WhatsApp sharing workflow** — using the attached original bill as the PDF
layout reference, as specified.
