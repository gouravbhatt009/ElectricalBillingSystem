using ElectricalBilling.Models;
using ElectricalBilling.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ElectricalBilling.Services
{
    /// <summary>
    /// Renders an invoice already saved by Phase 3 as an A4 PDF that
    /// mirrors the original Leeladhar Bhatt paper bill. Every figure on
    /// the PDF (Subtotal, CGST, SGST, Grand Total, Amount in Words) comes
    /// straight from InvoiceService.GetDetailsAsync — nothing is
    /// recalculated here, so the PDF can never disagree with the app.
    /// </summary>
    public class InvoicePdfService : IInvoicePdfService
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<InvoicePdfService> _logger;

        public InvoicePdfService(IInvoiceService invoiceService, IWebHostEnvironment env, ILogger<InvoicePdfService> logger)
        {
            _invoiceService = invoiceService;
            _env = env;
            _logger = logger;
        }

        public async Task<byte[]?> GeneratePdfAsync(int invoiceId)
        {
            var invoice = await _invoiceService.GetDetailsAsync(invoiceId);
            if (invoice is null)
            {
                return null;
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Element(c => ComposeHeader(c, invoice));
                    page.Content().Element(c => ComposeContent(c, invoice));
                    page.Footer().Element(c => ComposeFooter(c, invoice));
                });
            });

            return document.GeneratePdf();
        }

        // -----------------------------------------------------------------
        // Header: the letterhead — business name, description, address,
        // mobile — matching the original printed bill's layout.
        // -----------------------------------------------------------------
        private void ComposeHeader(IContainer container, InvoiceDetailsViewModel invoice)
        {
            var logoBytes = TryLoadWwwRootFile(invoice.Business.LogoPath);

            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Bill").FontSize(9).SemiBold();
                    if (!string.IsNullOrWhiteSpace(invoice.Business.Mobile))
                    {
                        row.RelativeItem().AlignRight().Text($"Mob. {invoice.Business.Mobile}").FontSize(9).SemiBold();
                    }
                });

                column.Item().PaddingTop(2).Row(row =>
                {
                    if (logoBytes is not null)
                    {
                        row.ConstantItem(45).Height(45).Image(logoBytes).FitArea();
                    }

                    row.RelativeItem().AlignCenter().Text(invoice.Business.BusinessName)
                        .FontSize(22).Bold().FontColor(Colors.Blue.Darken3);

                    if (logoBytes is not null)
                    {
                        row.ConstantItem(45); // balances the logo so the title stays centered
                    }
                });

                if (!string.IsNullOrWhiteSpace(invoice.Business.Description))
                {
                    column.Item().AlignCenter().Text(invoice.Business.Description)
                        .FontSize(9).FontColor(Colors.Blue.Darken2);
                }

                if (!string.IsNullOrWhiteSpace(invoice.Business.Address))
                {
                    column.Item().AlignCenter().Text(invoice.Business.Address).FontSize(9);
                }

                var contactLine = string.Join("   |   ", new[]
                {
                    !string.IsNullOrWhiteSpace(invoice.Business.GSTIN) ? $"GSTIN: {invoice.Business.GSTIN}" : null,
                    !string.IsNullOrWhiteSpace(invoice.Business.Email) ? invoice.Business.Email : null
                }.Where(s => s is not null));

                if (!string.IsNullOrWhiteSpace(contactLine))
                {
                    column.Item().AlignCenter().Text(contactLine).FontSize(8).FontColor(Colors.Grey.Darken1);
                }

                column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Darken1);
            });
        }

        // -----------------------------------------------------------------
        // Content: bill no/date, customer block, item table, totals,
        // amount in words, payment status, notes.
        // -----------------------------------------------------------------
        private void ComposeContent(IContainer container, InvoiceDetailsViewModel invoice)
        {
            container.PaddingTop(10).Column(column =>
            {
                column.Spacing(8);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Bill No: ").SemiBold();
                        t.Span(invoice.InvoiceNumber);
                    });
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Date: ").SemiBold();
                        t.Span(invoice.InvoiceDate.ToString("dd/MM/yyyy"));
                    });
                });

                column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(cust =>
                {
                    cust.Item().Text(t =>
                    {
                        t.Span("M/s: ").SemiBold();
                        t.Span(invoice.CustomerName);
                    });

                    if (!string.IsNullOrWhiteSpace(invoice.CustomerAddress))
                    {
                        cust.Item().Text(invoice.CustomerAddress).FontSize(9);
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.CustomerMobile))
                    {
                        cust.Item().Text($"Mobile: {invoice.CustomerMobile}").FontSize(9);
                    }
                });

                column.Item().Element(c => ComposeItemsTable(c, invoice));

                column.Item().AlignRight().Width(240).Element(c => ComposeTotals(c, invoice));

                column.Item().Text(t =>
                {
                    t.Span("Amount in Words: ").SemiBold();
                    t.Span(invoice.AmountInWords).Italic();
                });

                column.Item().Element(c => ComposePaymentStatus(c, invoice));

                if (!string.IsNullOrWhiteSpace(invoice.Notes))
                {
                    column.Item().Text(t =>
                    {
                        t.Span("Notes: ").SemiBold();
                        t.Span(invoice.Notes);
                    });
                }
            });
        }

        private static void ComposeItemsTable(IContainer container, InvoiceDetailsViewModel invoice)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(32);
                    columns.RelativeColumn(4);
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(75);
                    columns.ConstantColumn(85);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("S.No");
                    header.Cell().Element(HeaderCell).Text("Particular");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Rate");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                });

                foreach (var item in invoice.Items)
                {
                    table.Cell().Element(BodyCell).Text(item.SNo.ToString());
                    table.Cell().Element(BodyCell).Text(item.Particular);
                    table.Cell().Element(BodyCell).AlignRight().Text(item.Qty.ToString("0.##"));
                    table.Cell().Element(BodyCell).AlignRight().Text($"Rs. {item.Rate:N2}");
                    table.Cell().Element(BodyCell).AlignRight().Text($"Rs. {item.Amount:N2}");
                }

                if (invoice.Items.Count == 0)
                {
                    // Defensive only — InvoiceService never persists an invoice with zero items.
                    table.Cell().ColumnSpan(5).Element(BodyCell).AlignCenter().Text("No items on this invoice.");
                }

                static IContainer HeaderCell(IContainer c) => c
                    .Background(Colors.Grey.Lighten3)
                    .Border(1).BorderColor(Colors.Grey.Darken1)
                    .Padding(4)
                    .DefaultTextStyle(x => x.SemiBold());

                static IContainer BodyCell(IContainer c) => c
                    .Border(1).BorderColor(Colors.Grey.Lighten1)
                    .Padding(4);
            });
        }

        private static void ComposeTotals(IContainer container, InvoiceDetailsViewModel invoice)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Subtotal");
                    row.RelativeItem().AlignRight().Text($"Rs. {invoice.SubTotal:N2}");
                });

                if (invoice.GstEnabled)
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"CGST ({invoice.CgstPercent:0.##}%)");
                        row.RelativeItem().AlignRight().Text($"Rs. {invoice.CgstAmount:N2}");
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"SGST ({invoice.SgstPercent:0.##}%)");
                        row.RelativeItem().AlignRight().Text($"Rs. {invoice.SgstAmount:N2}");
                    });
                }

                column.Item().PaddingTop(4).BorderTop(1).BorderColor(Colors.Black).PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("Grand Total").Bold().FontSize(12);
                    row.RelativeItem().AlignRight().Text($"Rs. {invoice.GrandTotal:N2}").Bold().FontSize(12);
                });
            });
        }

        private static void ComposePaymentStatus(IContainer container, InvoiceDetailsViewModel invoice)
        {
            container.Column(column =>
            {
                column.Item().Text(t =>
                {
                    t.Span("Payment Status: ").SemiBold();
                    t.Span(invoice.Status.ToString());
                });

                switch (invoice.Status)
                {
                    case InvoiceStatus.Paid:
                        column.Item().Text($"Paid Amount: Rs. {invoice.GrandTotal:N2}    Outstanding: Rs. 0.00").FontSize(9);
                        break;
                    case InvoiceStatus.Pending:
                        column.Item().Text($"Outstanding Amount: Rs. {invoice.GrandTotal:N2}").FontSize(9);
                        break;
                    case InvoiceStatus.Partial:
                        // No AmountPaid is tracked until the Payments module (Phase 5) exists —
                        // showing a fabricated split here would be misleading.
                        column.Item().Text("Partially paid. Exact paid/outstanding amounts will appear here once Payments (Phase 5) is added.").FontSize(9);
                        break;
                    case InvoiceStatus.Cancelled:
                        column.Item().Text("This invoice has been cancelled.").FontSize(9).FontColor(Colors.Red.Darken1);
                        break;
                }
            });
        }

        // -----------------------------------------------------------------
        // Footer: bank/UPI (if configured), signature area, invoice footer note.
        // -----------------------------------------------------------------
        private void ComposeFooter(IContainer container, InvoiceDetailsViewModel invoice)
        {
            var signatureBytes = TryLoadWwwRootFile(invoice.Business.SignaturePath);

            container.PaddingTop(8).Column(column =>
            {
                if (!string.IsNullOrWhiteSpace(invoice.Business.BankDetails) || !string.IsNullOrWhiteSpace(invoice.Business.UpiId))
                {
                    column.Item().Column(bank =>
                    {
                        if (!string.IsNullOrWhiteSpace(invoice.Business.BankDetails))
                        {
                            bank.Item().Text($"Bank Details: {invoice.Business.BankDetails}").FontSize(8);
                        }
                        if (!string.IsNullOrWhiteSpace(invoice.Business.UpiId))
                        {
                            bank.Item().Text($"UPI ID: {invoice.Business.UpiId}").FontSize(8);
                        }
                    });
                }

                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().AlignLeft().AlignBottom().Text("E. & O.E.").FontSize(8).Italic();

                    row.RelativeItem().AlignRight().Column(sig =>
                    {
                        if (signatureBytes is not null)
                        {
                            sig.Item().AlignRight().Width(120).Height(45).Image(signatureBytes).FitArea();
                        }
                        else
                        {
                            sig.Item().Height(45);
                        }

                        sig.Item().Width(140).AlignCenter().PaddingTop(2).BorderTop(1).BorderColor(Colors.Black)
                            .PaddingTop(2).Text("Signature").FontSize(8);
                    });
                });

                if (!string.IsNullOrWhiteSpace(invoice.Business.InvoiceFooter))
                {
                    column.Item().PaddingTop(8).AlignCenter().Text(invoice.Business.InvoiceFooter).FontSize(8).Italic();
                }
            });
        }

        /// <summary>
        /// Loads a file referenced by a BusinessSetting path (LogoPath/SignaturePath)
        /// relative to wwwroot. Returns null if not configured or not found — the
        /// PDF simply omits the image rather than breaking, per the Phase 4 spec.
        /// </summary>
        private byte[]? TryLoadWwwRootFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            try
            {
                var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/', '\\'));
                return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load image at {Path} for invoice PDF; continuing without it.", relativePath);
                return null;
            }
        }
    }
}
