using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ERPSystem.Modules.Projects.Application.Services;

// =============================================================================
// Sprint 62 / DEC-198 — Billing PDF Export.
//
// Generates a bilingual (Arabic + English) Progress Billing certificate
// using QuestPDF 2024.10.0 (MIT, pure C#, no native deps). The PDF is
// produced synchronously — small in-memory payload (~tens of KB), no async
// I/O on the render path. The controller streams the bytes back as
// `application/pdf`.
//
// License note: QuestPDF Community is free for orgs with annual revenue
// under $1M USD. See https://www.questpdf.com/license/ for the terms.
// =============================================================================

/// <summary>
/// Flat data record consumed by <see cref="IPdfExportService.GenerateBillingPdf"/>.
/// All fields are passed in by the controller — the service does no DB I/O.
/// </summary>
public record BillingPdfModel(
    string ProjectCode,
    string ProjectName,
    string? ContractNumber,
    string BillingNumber,
    DateTime BillingDate,
    DateTime? PeriodFrom,
    DateTime? PeriodTo,
    decimal WorkCompletedPercent,
    decimal GrossAmount,
    decimal AdvanceDeducted,
    decimal RetentionDeducted,
    decimal RegionalPremiumDeducted,
    decimal NetAmountAfterPremium,
    string? Notes
);

public interface IPdfExportService
{
    /// <summary>
    /// Render a bilingual Progress Billing certificate as a PDF byte array.
    /// </summary>
    byte[] GenerateBillingPdf(BillingPdfModel model);
}

/// <summary>
/// Sprint 62 / DEC-198 — QuestPDF-based billing PDF renderer.
///
/// <para><b>Layout</b>:</para>
/// <list type="bullet">
///   <item>Header — bilingual title + project meta (code, name, contract, billing #, date)</item>
///   <item>Period — from/to + work-completed percent</item>
///   <item>Totals — Gross, Advance Deducted, Retention Deducted, Regional Premium Deducted, Net After Premium</item>
///   <item>Notes block (if provided)</item>
///   <item>Signatures block — three lines (PM, Client, Contractor)</item>
/// </list>
///
/// <para><b>L19 / DEC-095</b>: this service is a pure renderer — it never
/// reads <c>company_id</c> or <c>user_id</c> from a request, and never
/// touches the DB. The controller is responsible for passing in
/// already-resolved, already-scoped data.</para>
/// </summary>
public sealed class PdfExportService : IPdfExportService
{
    private static int _licenseSet;

    /// <summary>
    /// Set the QuestPDF license exactly once per process (Community for SMB).
    /// QuestPDF will throw at render time if the license is unset.
    /// </summary>
    private static void EnsureLicense()
    {
        if (System.Threading.Interlocked.Exchange(ref _licenseSet, 1) == 0)
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }
    }

    public byte[] GenerateBillingPdf(BillingPdfModel m)
    {
        EnsureLicense();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(36);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Arial"));

                page.Header().Element(HeaderElement(m));
                page.Content().Element(ContentElement(m));
                page.Footer().Element(FooterElement);
            });
        });

        return document.GeneratePdf();
    }

    // ===== Header =====
    private static Action<IContainer> HeaderElement(BillingPdfModel m) => container =>
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("شهادة الدفع المرحلي / Progress Billing Certificate")
                .FontSize(16).Bold();
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("المشروع / Project: ").Bold().FontSize(9);
                        t.Span($"{m.ProjectCode} — {m.ProjectName}").FontSize(9);
                    });
                    c.Item().Text(t =>
                    {
                        t.Span("العقد / Contract #: ").Bold().FontSize(9);
                        t.Span(m.ContractNumber ?? "—").FontSize(9);
                    });
                });
                row.ConstantItem(180).AlignRight().Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("رقم المستخلص / Billing #: ").Bold().FontSize(9);
                        t.Span(m.BillingNumber).FontSize(9);
                    });
                    c.Item().Text(t =>
                    {
                        t.Span("التاريخ / Date: ").Bold().FontSize(9);
                        t.Span(m.BillingDate.ToString("yyyy-MM-dd")).FontSize(9);
                    });
                });
            });
            col.Item().PaddingTop(4).LineHorizontal(0.6f);
        });
    };

    // ===== Content =====
    private static Action<IContainer> ContentElement(BillingPdfModel m) => container =>
    {
        container.PaddingVertical(8).Column(col =>
        {
            // Period + Work completed
            col.Item().Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("الفترة / Period: ").Bold();
                    t.Span(FormatPeriod(m.PeriodFrom, m.PeriodTo));
                });
                row.ConstantItem(200).AlignRight().Text(t =>
                {
                    t.Span("نسبة الإنجاز / Work Completed: ").Bold();
                    t.Span($"{m.WorkCompletedPercent:0.##} %");
                });
            });

            col.Item().PaddingTop(10).Element(TotalsTable(m));

            if (!string.IsNullOrWhiteSpace(m.Notes))
            {
                col.Item().PaddingTop(10).Text(t =>
                {
                    t.Span("ملاحظات / Notes: ").Bold();
                    t.Span(m.Notes!);
                });
            }

            col.Item().PaddingTop(20).Element(SignaturesBlock);
        });
    };

    private static Action<IContainer> TotalsTable(BillingPdfModel m) => container =>
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
            });

            // Header
            table.Header(h =>
            {
                h.Cell().Element(HeaderCell).Text("البند / Item");
                h.Cell().Element(HeaderCell).AlignRight().Text("المبلغ (د.ل) / Amount (LYD)");
            });

            // Body rows
            table.Cell().Element(BodyCell).Text("الإجمالي / Gross Amount");
            table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(m.GrossAmount));

            table.Cell().Element(BodyCell).Text("الدفعة المقدمة المخصومة / Advance Deducted");
            table.Cell().Element(BodyCell).AlignRight().Text($"({FormatMoney(m.AdvanceDeducted)})");

            table.Cell().Element(BodyCell).Text("الاحتجاز المخصوم / Retention Deducted");
            table.Cell().Element(BodyCell).AlignRight().Text($"({FormatMoney(m.RetentionDeducted)})");

            table.Cell().Element(BodyCell).Text("خصم المنطقة (NDB+CIT+SS) / Regional Premium");
            table.Cell().Element(BodyCell).AlignRight().Text($"({FormatMoney(m.RegionalPremiumDeducted)})");

            // Net row — emphasised
            table.Cell().Element(NetCell).Text("الصافي بعد الخصم / Net After Premium").Bold();
            table.Cell().Element(NetCell).AlignRight().Text(FormatMoney(m.NetAmountAfterPremium)).Bold();
        });
    };

    // ===== Signatures =====
    private static Action<IContainer> SignaturesBlock => container =>
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().PaddingTop(20).LineHorizontal(0.4f);
                c.Item().PaddingTop(4).AlignCenter().Text("مدير المشروع / Project Manager");
            });
            row.ConstantItem(20);
            row.RelativeItem().Column(c =>
            {
                c.Item().PaddingTop(20).LineHorizontal(0.4f);
                c.Item().PaddingTop(4).AlignCenter().Text("العميل / Client");
            });
            row.ConstantItem(20);
            row.RelativeItem().Column(c =>
            {
                c.Item().PaddingTop(20).LineHorizontal(0.4f);
                c.Item().PaddingTop(4).AlignCenter().Text("المقاول / Contractor");
            });
        });
    };

    // ===== Footer =====
    private static Action<IContainer> FooterElement => container =>
    {
        container.AlignCenter().Text(t =>
        {
            t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
            t.Span("ERP-SYSTEM — Sprint 62 / DEC-198 — Page ");
            t.CurrentPageNumber();
            t.Span(" / ");
            t.TotalPages();
        });
    };

    // ===== Cell styles =====
    private static IContainer HeaderCell(IContainer c) =>
        c.Background(Colors.Grey.Lighten3).Border(0.5f).Padding(4);

    private static IContainer BodyCell(IContainer c) =>
        c.BorderBottom(0.4f).BorderColor(Colors.Grey.Lighten1).Padding(4);

    private static IContainer NetCell(IContainer c) =>
        c.Background(Colors.Grey.Lighten4).Border(0.5f).Padding(4);

    // ===== Formatters =====
    private static string FormatMoney(decimal v) => v.ToString("N4", System.Globalization.CultureInfo.InvariantCulture);

    private static string FormatPeriod(DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue)
            return $"{from:yyyy-MM-dd} → {to:yyyy-MM-dd}";
        if (from.HasValue) return $"من {from:yyyy-MM-dd}";
        if (to.HasValue) return $"إلى {to:yyyy-MM-dd}";
        return "—";
    }
}
