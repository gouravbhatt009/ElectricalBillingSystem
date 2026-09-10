# Phase 4 — PDF Generation + Print + WhatsApp

Builds on the existing Phase 1–3 codebase (auth, settings, customers,
service master, invoice creation). **Nothing from Phase 1–3 was restarted,
replaced, or had its schema changed.** This phase is purely additive: a new
PDF service, a download endpoint, and UI wiring on pages that already
existed.

---

## 1. Files created

```
/Services
    IInvoicePdfService.cs
    InvoicePdfService.cs
```

- **IInvoicePdfService** — one method, `GeneratePdfAsync(int invoiceId)` →
  `byte[]?` (null = invoice not found).
- **InvoicePdfService** — QuestPDF document composition. It depends on the
  *existing* `IInvoiceService` and calls `GetDetailsAsync(id)` to get the
  invoice — the same call the Details page already uses — so every number
  on the PDF (Subtotal, CGST, SGST, Grand Total, Amount in Words) is
  whatever Phase 3 already calculated and stored. **Nothing is
  recalculated in the PDF layer.**

No new models, no new DbContext members, no duplicate Invoice/InvoiceItem
classes — the existing Phase 3 entities and `AmountInWordsConverter` are
reused as-is.

## 2. Files modified

| File | Change |
|---|---|
| `ElectricalBilling.csproj` | Added the QuestPDF package reference (was a placeholder comment since Phase 3). |
| `Program.cs` | Set `QuestPDF.Settings.License = LicenseType.Community;` at startup; registered `IInvoicePdfService`. |
| `Services/InvoiceService.cs` | `GetDetailsAsync` now also loads `BusinessSetting` (via the existing `IBusinessSettingsService` it already depended on) and populates a new `Business` block on the view model. No existing method signature changed. |
| `ViewModels/InvoiceDetailsViewModel.cs` | Added `Business` property (`InvoiceBusinessInfoViewModel`) — the letterhead fields the PDF/print/Details header need. Purely additive; every existing property is untouched. |
| `Controllers/InvoicesController.cs` | Added `IInvoicePdfService` to the constructor and a new `DownloadPdf(int id)` action. Every existing action (`Index`, `Create`, `Edit`, `Details`, `Cancel`) is unchanged. |
| `Views/Invoices/Details.cshtml` | Added the business letterhead block, Download PDF / Print / Share on WhatsApp buttons, and print-friendly markup (`.print-area`, `.no-print`). All existing content (items table, totals, amount in words, Edit/Cancel/Back buttons) is preserved. |
| `Views/Invoices/Index.cshtml` | Added a "PDF" quick-download link next to the existing View/Edit actions on each row. |
| `wwwroot/css/site.css` | Added a `@media print` block (hides nav/buttons/alerts/forms, sets A4 page size and margins). Existing styles untouched. |

## 3. NuGet packages added

```
QuestPDF, Version 2024.10.4
```

QuestPDF was chosen because it's actively maintained, fully compatible
with .NET 8, has no native/OS dependency beyond SkiaSharp (which it pulls
in transitively), and its Community license is free for a small business
like this one (see below).

**Note on the version pin:** I couldn't reach NuGet from this sandbox to
confirm the exact latest patch, so `2024.10.4` is a known-good version as
of my training data — not necessarily today's latest. If `dotnet restore`
can't resolve it, either delete the `Version` attribute and run
`dotnet add package QuestPDF` (which will fetch current latest and update
the csproj for you), or bump the version number by hand.

## 4. Configuration changes

- `Program.cs` now calls `QuestPDF.Settings.License = LicenseType.Community;`
  once at startup. This is QuestPDF's free tier for companies with under
  $1M USD annual revenue — appropriate for this business, but worth
  knowing about if that ever changes (see questpdf.com/license).
- No changes to `appsettings.json`, connection strings, or authentication.

## 5. Database migration status

**No database migration required for Phase 4.** No model, DbSet, or column
changed. `Business` on `InvoiceDetailsViewModel` is populated at read time
from the existing `BusinessSettings` table — nothing new is persisted.

## 6. PDF generation implementation

- `GET /Invoices/DownloadPdf/{id}` — `[Authorize]`-protected (inherited
  from the controller-level attribute already on `InvoicesController`).
- Flow: look up the invoice via `IInvoiceService.GetDetailsAsync` (reused,
  handles the "not found" case exactly like `Details` already does) →
  build the filename from the real invoice number (e.g.
  `ELB-2026-0001.pdf`, sanitized against invalid filename characters) →
  call `IInvoicePdfService.GeneratePdfAsync` → return
  `File(bytes, "application/pdf", fileName)`.
- Layout mirrors the original paper bill: "Bill" / mobile line at the top,
  centered business name/description/address, Bill No/Date row, `M/s`
  customer block, bordered item table (S.No/Particular/Qty/Rate/Amount),
  right-aligned totals block (Subtotal → CGST/SGST when enabled → bold
  Grand Total), Amount in Words, Payment Status, notes, bank/UPI details
  when configured, and a signature line (embeds an actual signature image
  if `BusinessSetting.SignaturePath` points at a real file under
  `wwwroot`; otherwise just leaves a clean blank signature line — nothing
  broken or placeholder-looking).
- A4 page size, 1.5cm margins, bordered table matching "clean borders,
  proper spacing" from the spec — no cards, gradients, or dashboard-style
  elements.
- **Currency is printed as "Rs." rather than "₹" inside the PDF.** This
  isn't a downgrade — it matches the original bill photo itself, which
  prints "Rs." rather than a rupee glyph. It also sidesteps a real font
  risk: the ₹ glyph isn't guaranteed to exist in every font available to
  the PDF renderer on every OS (see the Linux note below), so using the
  same notation as the source bill is both more faithful and safer. The
  web UI is untouched and still shows ₹ everywhere, since browsers handle
  Unicode fallback fine.
- If a signature/logo file is missing or unreadable, `InvoicePdfService`
  logs a warning and simply omits it — it never throws or produces a
  broken image placeholder.

## 7. Print implementation

- "Print" button on the Details page calls `window.print()` directly
  (matches the existing inline-`onclick` pattern already used by the
  Cancel button).
- The same content block the PDF renders from (letterhead, customer,
  items, totals, amount in words, payment status, signature area) is
  wrapped in `.print-area` on the Details page, so what prints matches
  what downloads.
- `@media print` in `site.css` hides the navbar, all buttons/forms, and
  alert banners; sets `@page { size: A4; margin: 12mm; }`.
- Per the spec's fallback guidance: Download PDF is called out as the more
  reliable option (consistent fonts regardless of the browser/OS printing
  it), with in-browser Print offered as a convenience alongside it — not
  a second, different-looking design.

## 8. WhatsApp implementation

- "Share on WhatsApp" builds `https://wa.me/?text=<url-encoded message>`
  **entirely server-side in Razor** (no extra JS file needed) using the
  invoice's real `CustomerName`, `Business.BusinessName`, `InvoiceNumber`,
  and `GrandTotal`.
- Uses the no-fixed-number `wa.me/?text=` form (opens WhatsApp with a
  contact picker) rather than guessing `wa.me/<customer number>` — the
  stored `Customer.Mobile` isn't guaranteed to include a country code, so
  guessing a number risks sending to the wrong contact or a malformed
  link. This is the reliable choice, not a shortcut.
- An inline note under the buttons explicitly tells the user the honest
  workflow: **download the PDF first, then attach it manually inside the
  WhatsApp chat** — no false claim about automatic attachment, per the
  spec.

## 9. Build result

**I could not run `dotnet build` in this sandbox** — there is no .NET SDK
installed here, and outbound access to `nuget.org` (needed to restore
QuestPDF and the existing EF Core packages) is blocked by this
environment's network policy. This is an environment limitation, not a
skipped step.

What I did instead, to compensate:
- Manually re-read every existing Phase 1–3 file I touched or depended on
  (models, interfaces, DI registrations, view models) before writing new
  code, so signatures match exactly.
- Checked brace/tag balance and structure by hand across all new/edited
  files (Details.cshtml's div/table/tr counts verified programmatically).
- Cross-checked every QuestPDF API call I used (`Document.Create`,
  `page.Header().Element(...)`, `Func<IContainer,IContainer>` cell
  styling, `Table`/`ColumnsDefinition`, `Image(...).FitArea()`, etc.)
  against QuestPDF's documented fluent API and its official invoice
  sample pattern.

**Please run this yourself before relying on it:**

```bash
dotnet restore
dotnet build
```

If `dotnet build` reports anything, paste the error back to me and I'll
fix it immediately — I'd rather you catch it on a real SDK than me
assert a clean build I can't actually verify here.

## 10. Runtime test result

Same limitation — I can't run the app in this sandbox (no SQL Server, no
.NET runtime, no port to browse to). **Not yet runtime-tested by me.**
Once you run it locally, please walk through:

1. Login → Bills → open any existing invoice (or create one via Phase 3
   if you don't have one yet)
2. **Download PDF** → open it → check: business header, description,
   address, mobile at top; Bill No/Date; M/s customer block; every item
   row with correct Qty/Rate/Amount; Subtotal; CGST/SGST rows appear only
   when that invoice had GST enabled; Grand Total; Amount in Words;
   Payment Status line; signature area at the bottom (blank line if no
   signature configured — none is, out of the box)
3. **Print** from the Details page → confirm the browser print preview
   shows only the invoice content (no navbar, no buttons, no alerts) and
   fits an A4 page sensibly
4. **Share on WhatsApp** → confirm it opens WhatsApp (web or app) with the
   message pre-filled with the real bill number, amount, and customer
   name, and that the on-page note about manually attaching the PDF makes
   sense
5. Confirm nothing from Phase 3 broke: create a new invoice, edit one,
   cancel one, search the Bills list — all should work exactly as before,
   since none of that code was touched beyond adding the "PDF" link to
   the Index table

## 11. Remaining genuine issues

- **Not built or run by me** — see sections 9–10. This is the main honest
  caveat: everything above is a careful manual review against the
  existing codebase and QuestPDF's documented API, not a verified
  compile.
- **Font availability on Linux hosts.** The PDF uses `"Arial"` as the text
  font. That's reliably present if this is hosted on Windows (the likely
  target given the rest of the stack). If you ever deploy to a Linux
  container, QuestPDF/SkiaSharp needs an actual font file available to
  the OS (`fontconfig` + a TTF, e.g. `apt-get install -y fontconfig
  fonts-liberation`) or PDF generation can fail at render time. Easy fix
  if/when it comes up — flagging it now rather than letting it be a
  surprise later.
- **Partial payment status has no real numbers yet.** `InvoiceStatus.Partial`
  exists in the enum but nothing in the app can currently set it (only
  `Pending` on create and `Cancelled` via the Cancel button), and there's
  no `AmountPaid` field until Phase 5's Payments module exists. The PDF
  and Details page both say so plainly instead of inventing a split —
  this will get real numbers once Phase 5 is built.
- **QuestPDF version pin unverified** — see section 3.

---

Stopping here as instructed — not moving to Phase 5. Let me know how the
build/run goes and I'll fix anything that comes up.
