using ClosedXML.Excel;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Elements;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class ReporteDeAsistenciaController : Controller
{
    private const int PageSize = 20;
    private readonly IAttendanceReportService _service;

    public ReporteDeAsistenciaController(IAttendanceReportService service)
    {
        _service = service;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? fechaDesde, DateTime? fechaHasta, string? persona, string? area, int? areaDeptId, string? estado, int page = 1)
    {
        try
        {
            var model = await _service.GetReportAsync(new AttendanceReportRequest
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                Persona = persona,
                Area = area,
                AreaDeptId = areaDeptId,
                Estado = estado,
                PageNumber = page,
                PageSize = PageSize
            });

            return View(model);
        }
        catch (ArgumentException ex)
        {
            var filtersModel = await _service.BuildFilterModelAsync(new AttendanceReportRequest
            {
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                Persona = persona,
                Area = area,
                AreaDeptId = areaDeptId,
                Estado = estado,
                PageNumber = page,
                PageSize = PageSize
            });

            filtersModel.TotalRecords = 0;
            filtersModel.Rows = Array.Empty<AttendanceReportRowViewModel>();
            filtersModel.Persons = Array.Empty<AttendanceReportPersonSummaryViewModel>();
            filtersModel.ValidationMessage = ex.Message;

            return View(filtersModel);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportarExcel(DateTime? fechaDesde, DateTime? fechaHasta, string? persona, string? area, int? areaDeptId, string? estado)
    {
        var report = await _service.GetReportAsync(new AttendanceReportRequest
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            Persona = persona,
            Area = area,
            AreaDeptId = areaDeptId,
            Estado = estado,
            PageNumber = 1,
            PageSize = int.MaxValue
        });

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Reporte Asistencia MTPE");
        ws.Cell(1, 1).Value = "Reporte de Asistencia MTPE";
        ws.Cell(2, 1).Value = $"Rango: {report.FechaDesde:dd/MM/yyyy} - {report.FechaHasta:dd/MM/yyyy}";
        ws.Cell(3, 1).Value = $"RUC: {report.CompanyTaxId} - {report.CompanyName}";
        ws.Cell(4, 1).Value = $"Personas: {string.Join(", ", report.Persons.Select(x => x.Personal))}";
        ws.Range(1, 1, 1, 16).Merge().Style.Font.SetBold().Font.FontSize = 14;
        ws.Range(2, 1, 2, 16).Merge();
        ws.Range(3, 1, 3, 16).Merge();
        ws.Range(4, 1, 4, 16).Merge();

        var headers = new[] { "Código", "DNI", "Personal", "Área", "Fecha", "Horario Asignado", "Entra.", "Salid.", "Falta", "Horas EFECT.", "Horas PERM.", "Tarda. Entra.", "Salida Temp.", "Horas Extras", "Excepción", "Marcas Intermedias" };
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(6, i + 1).Value = headers[i];
            ws.Cell(6, i + 1).Style.Font.SetBold();
            ws.Cell(6, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var rowIndex = 7;
        foreach (var item in report.Rows)
        {
            ws.Cell(rowIndex, 1).Value = item.Codigo;
            ws.Cell(rowIndex, 2).Value = item.Dni;
            ws.Cell(rowIndex, 3).Value = item.Personal;
            ws.Cell(rowIndex, 4).Value = item.Area;
            ws.Cell(rowIndex, 5).Value = item.Fecha.ToString("dd/MM/yyyy");
            ws.Cell(rowIndex, 6).Value = item.HorarioAsignado;
            ws.Cell(rowIndex, 7).Value = item.Entrada;
            ws.Cell(rowIndex, 8).Value = item.Salida;
            ws.Cell(rowIndex, 9).Value = item.Falta;
            ws.Cell(rowIndex, 10).Value = item.HorasEfectivas;
            ws.Cell(rowIndex, 11).Value = item.HorasDePermanencia;
            ws.Cell(rowIndex, 12).Value = item.TardanzaEntrada;
            ws.Cell(rowIndex, 13).Value = item.SalidaTemprana;
            ws.Cell(rowIndex, 14).Value = item.HorasExtras;
            ws.Cell(rowIndex, 15).Value = item.Excepcion;
            ws.Cell(rowIndex, 16).Value = item.MarcasIntermedias;
            rowIndex++;
        }

        rowIndex++;
        ws.Cell(rowIndex, 1).Value = "Totales";
        ws.Cell(rowIndex, 1).Style.Font.SetBold();
        rowIndex++;

        foreach (var person in report.Persons)
        {
            ws.Cell(rowIndex, 1).Value = person.Personal;
            ws.Cell(rowIndex, 1).Style.Font.SetBold();
            ws.Cell(rowIndex, 2).Value = $"Código: {person.Codigo}";
            ws.Cell(rowIndex, 3).Value = $"DNI: {person.Dni}";
            rowIndex++;

            WriteSummaryRow(ws, rowIndex++, "Días de Asistencia", person.DiasAsistencia, "Días con Turno", person.DiasConTurno);
            WriteSummaryRow(ws, rowIndex++, "Días sin Turno", person.DiasSinTurno, "Feriados con Turno", person.FeriadosConTurno);
            WriteSummaryRow(ws, rowIndex++, "Feriados sin Turno", person.FeriadosSinTurno, "Días de Falta", person.DiasFalta);
            WriteSummaryRow(ws, rowIndex++, "Horas Efectivas", person.HorasEfectivas, "Horas Permanencia", person.HorasDePermanencia);
            WriteSummaryRow(ws, rowIndex++, "Tardanza", person.Tardanza, "Salida Temprana", person.SalidaTemprana);
            WriteSummaryRow(ws, rowIndex++, "Horas Extras", person.HorasExtras, "Días Justificados", person.DiasJustificados);
            WriteSummaryRow(ws, rowIndex++, "Horas Justificadas", person.HorasJustificadas, string.Empty, string.Empty);
            rowIndex++;
        }

        ws.Columns().AdjustToContents();
        ws.Range(6, 1, Math.Max(rowIndex - 1, 6), 16).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(6, 1, Math.Max(rowIndex - 1, 6), 16).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteAsistencia.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> EmitirReporte(DateTime? fechaDesde, DateTime? fechaHasta, string? persona, string? area, int? areaDeptId, string? estado)
    {
        var report = await _service.GetReportAsync(new AttendanceReportRequest
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            Persona = persona,
            Area = area,
            AreaDeptId = areaDeptId,
            Estado = estado,
            PageNumber = 1,
            PageSize = int.MaxValue
        });

        var persons = report.Persons;
        var reportFrom = report.FechaDesde;
        var reportTo = report.FechaHasta;
        var currentUser = User?.Identity?.IsAuthenticated == true
            ? (User.Identity?.Name ?? string.Empty)
            : string.Empty;

        var pdf = Document.Create(container =>
        {
            foreach (var person in persons)
            {
                container.Page(page =>
                {
                    page.Margin(18);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(x => x.FontSize(7.5f).FontFamily(Fonts.Arial));

                    page.Content().Column(column =>
                    {
                        column.Spacing(4);
                        column.Item().Text("Informe de Asistencia del Personal MTPE").Bold().FontSize(12).AlignCenter();
                        column.Item().Text($"Rango: {reportFrom:yyyy-MM-dd} a {reportTo:yyyy-MM-dd}").FontSize(8).AlignCenter();
                        column.Item().Text("(Refrigerio => HV, HR ó HN: horario que descuenta 45, 60 ó 90 minutos, HS o cualquier otro: horario que no descuenta.)").FontSize(7).AlignCenter();
                        column.Item().PaddingTop(2).Text($"RUC: {report.CompanyTaxId} - {report.CompanyName}").Bold().FontSize(8);
                        //column.Item().Text($"{report.CompanyTaxId} - {report.CompanyName}").FontSize(8);
                        /*    
                        column.Item().PaddingTop(2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(85);
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(CellInfoLabel).Text("DNI/N°AC").Bold();
                            table.Cell().Element(CellInfoValue).Text(person.Dni);

                            table.Cell().Element(CellInfoLabel).Text("Personal").Bold();
                            table.Cell().Element(CellInfoValue).Text(person.Personal);

                            table.Cell().Element(CellInfoLabel).Text("Área").Bold();
                            table.Cell().Element(CellInfoValue).Text(person.Area);
                        });
                        */
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(52);
                                columns.RelativeColumn();
                                columns.ConstantColumn(50);
                                columns.RelativeColumn();
                                columns.ConstantColumn(52);
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(CellHeader).Text("DNI/N°AC").Bold();
                            table.Cell().Element(CellBody).Text(person.Dni);

                            table.Cell().Element(CellHeader).Text("Personal").Bold();
                            table.Cell().Element(CellBody).Text(person.Personal);

                            table.Cell().Element(CellHeader).Text("Área").Bold();
                            table.Cell().Element(CellBody).Text(person.Area);
                        });
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(52);
                                columns.ConstantColumn(120);
                                columns.ConstantColumn(37);
                                columns.ConstantColumn(37);
                                columns.ConstantColumn(36);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(54);
                                columns.ConstantColumn(54);
                                columns.ConstantColumn(52);
                                columns.ConstantColumn(62);
                                columns.RelativeColumn();
                            });

                            string[] headers = { "Fecha", "Horario Asignado", "Entra.", "Salid.", "Falta", "Horas EFECT.", "Horas PERM.", "Tarda. Entra.", "Salida Temp.", "Horas Extras", "Excepción", "Marcas Intermedias" };
                            foreach (var header in headers)
                            {
                                table.Cell().Element(CellHeader).AlignMiddle().Text(header).Bold().FontSize(7);
                            }

                            foreach (var row in person.Rows)
                            {
                                table.Cell().Element(CellBody).AlignCenter().Text(row.Fecha.ToString("dd/MM/yyyy"));
                                table.Cell().Element(CellBody).Text($"{row.HorarioRango}");  //"{row.HorarioCodigo} {row.HorarioRango}"
                                table.Cell().Element(CellBody).AlignCenter().Text(row.Entrada);
                                table.Cell().Element(CellBody).AlignCenter().Text(row.Salida);
                                table.Cell().Element(c => StyledStatusCell(c, row.Falta == "Si" ? "falta" : null)).AlignCenter().Text(row.Falta);
                                table.Cell().Element(CellBody).AlignCenter().Text(row.HorasEfectivas);
                                table.Cell().Element(CellBody).AlignCenter().Text(row.HorasDePermanencia);
                                table.Cell().Element(c => StyledStatusCell(c, !string.IsNullOrWhiteSpace(row.TardanzaEntrada) ? "tardanza" : null)).AlignCenter().Text(row.TardanzaEntrada);
                                table.Cell().Element(c => StyledStatusCell(c, !string.IsNullOrWhiteSpace(row.SalidaTemprana) ? "salida-temprana" : null)).AlignCenter().Text(row.SalidaTemprana);
                                table.Cell().Element(c => StyledStatusCell(c, !string.IsNullOrWhiteSpace(row.HorasExtras) ? "horas-extras" : null)).AlignCenter().Text(row.HorasExtras);
                                table.Cell().Element(c => StyledStatusCell(c, !string.IsNullOrWhiteSpace(row.Excepcion) ? "excepcion" : null)).AlignCenter().Text(row.Excepcion);
                                table.Cell().Element(CellBody).Text(row.MarcasIntermedias);
                            }
                        });

                        column.Item().PaddingTop(4).Text($"Totales de {person.Personal}").Bold().FontSize(8.5f);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(50);
                                columns.RelativeColumn();
                                columns.ConstantColumn(50);
                                columns.RelativeColumn();
                                columns.ConstantColumn(50);
                                columns.RelativeColumn();
                                columns.ConstantColumn(50);                                
                            });

                            TotalsCell(table, "DÍAS PROGRAMADOS", person.DiasConTurno);
                            TotalsCell(table, "DÍAS JUSTIFICADOS", person.DiasJustificados);
                            TotalsCell(table, "HORAS EFECTIVAS", person.HorasEfectivas);
                            TotalsCell(table, "HORAS EXTRAS", person.HorasExtras);

                            TotalsCell(table, "DÍAS DE ASISTENCIA", person.DiasAsistencia);
                            TotalsCell(table, "DÍAS FERIADOS CON TURNO", person.FeriadosConTurno);
                            TotalsCell(table, "HORAS DE PERMANENCIA", person.HorasDePermanencia);
                            TotalsCell(table, "HORAS JUSTIFICADAS", person.HorasJustificadas);

                            TotalsCell(table, "DÍAS DE FALTA", person.DiasFalta);
                            TotalsCell(table, "DIAS FERIADOS SIN TURNO", person.FeriadosSinTurno);
                            TotalsCell(table, "HORAS DE TARDANZAS", person.Tardanza);
                            TotalsCell(table, string.Empty, string.Empty);

                            TotalsCell(table, "DÍAS SIN TURNO ASISTIDOS", person.DiasSinTurno);
                            TotalsCell(table, string.Empty, string.Empty);
                            TotalsCell(table, "HORAS DE SALIDAS TEMPRANAS", person.SalidaTemprana);
                            TotalsCell(table, string.Empty, string.Empty);
                        });
                    });

                    page.Footer().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(7));
                        text.Span("User: ");
                        text.Span(string.IsNullOrWhiteSpace(currentUser) ? "N/A" : currentUser);
                        text.Span("    Pag. ");
                        text.CurrentPageNumber();
                    });
                });
            }
        }).GeneratePdf();

        return File(pdf, "application/pdf", "ReporteAsistencia.pdf");
    }

    private static IContainer CellHeader(IContainer container)
    {
        return container
            .Border(0.7f)
            .BorderColor(Colors.Grey.Darken1)
            .Background(Colors.Grey.Lighten2)
            .PaddingVertical(3)
            .PaddingHorizontal(2);
    }

    private static IContainer CellBody(IContainer container)
    {
        return container
            .Border(0.7f)
            .BorderColor(Colors.Grey.Darken1)
            .PaddingVertical(2)
            .PaddingHorizontal(2);
    }

    private static IContainer CellInfoLabel(IContainer container)
    {
        return container.PaddingVertical(1);
    }

    private static IContainer CellInfoValue(IContainer container)
    {
        return container.PaddingVertical(1);
    }

    private static IContainer StyledStatusCell(IContainer container, string? state)
    {
        var baseCell = container
            .Border(0.7f)
            .BorderColor(Colors.Grey.Darken1)
            .PaddingVertical(2)
            .PaddingHorizontal(2);
        return state switch
        {
            "falta" => baseCell.Background("#f8d7da"),
            "tardanza" => baseCell.Background("#fff3cd"),
            "salida-temprana" => baseCell.Background("#ffe5b4"),
            "horas-extras" => baseCell.Background("#d1e7dd"),
            "excepcion" => baseCell.Background("#cfe2ff"),
            _ => baseCell
        };
    }

    private static void TotalsCell(QuestPDF.Fluent.TableDescriptor table, string label, string value)
    {
        table.Cell().Element(CellBody).Text(label).Bold();
        table.Cell().Element(CellBody).AlignCenter().Text(value);
    }

    private static void WriteSummaryRow(IXLWorksheet worksheet, int rowIndex, string leftLabel, string leftValue, string rightLabel, string rightValue)
    {
        worksheet.Cell(rowIndex, 1).Value = leftLabel;
        worksheet.Cell(rowIndex, 1).Style.Font.SetBold();
        worksheet.Cell(rowIndex, 2).Value = leftValue;
        worksheet.Cell(rowIndex, 3).Value = rightLabel;
        worksheet.Cell(rowIndex, 3).Style.Font.SetBold();
        worksheet.Cell(rowIndex, 4).Value = rightValue;
    }
}