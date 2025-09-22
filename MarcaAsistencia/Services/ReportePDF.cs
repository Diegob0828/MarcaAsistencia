using iTextSharp.text;
using iTextSharp.text.pdf;
using MarcaAsistencia.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace MarcaAsistencia.Services
{
    public class ReportePDF
    {
        public void GenerarReporte(List<Asistencia> asistencias, int month, int year, string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Document doc = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter.GetInstance(doc, fs);
                doc.Open();

                var titulo = new Paragraph($"Reporte de Asistencias - {new DateTime(year, month, 1):MMMM yyyy}")
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20f
                };
                doc.Add(titulo);

        
                PdfPTable table = new PdfPTable(6);
                table.WidthPercentage = 100;

             
                string[] headers = { "Empleado", "Fecha", "Entrada", "Ida Comer", "Vuelta Comer", "Salida" };
                foreach (var h in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(h))
                    {
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    };
                    table.AddCell(cell);
                }

                foreach (var a in asistencias)
                {
                    table.AddCell(a.NombreEmpleado);
                    table.AddCell(a.Fecha.ToShortDateString());
                    table.AddCell(a.Entrada?.ToString("HH:mm") ?? "");
                    table.AddCell(a.IdaComer?.ToString("HH:mm") ?? "");
                    table.AddCell(a.VueltaComer?.ToString("HH:mm") ?? "");
                    table.AddCell(a.Salida?.ToString("HH:mm") ?? "");
                }

                doc.Add(table);
                doc.Close();
            }
        }
    }
}
