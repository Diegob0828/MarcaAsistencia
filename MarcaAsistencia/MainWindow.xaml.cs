using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MarcaAsistencia.Data;
using MarcaAsistencia.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Data.SqlClient;
using Microsoft.Win32;
using MarcaAsistencia.Views;
using PdfDoc = iTextSharp.text.Document;
using PdfParagraph = iTextSharp.text.Paragraph;
using PdfFontFactory = iTextSharp.text.FontFactory;
using PdfElement = iTextSharp.text.Element;
using PdfBaseColor = iTextSharp.text.BaseColor;
using PdfPTable = iTextSharp.text.pdf.PdfPTable;
using PdfPCell = iTextSharp.text.pdf.PdfPCell;
using PdfPhrase = iTextSharp.text.Phrase;
using PdfWriter = iTextSharp.text.pdf.PdfWriter;
using PdfPageSize = iTextSharp.text.PageSize;

namespace MarcaAsistencia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                CargarAsistencias();
                System.Diagnostics.Debug.WriteLine("MainWindow inicializada correctamente.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MainWindow constructor: {ex.Message}");
                MessageBox.Show($"Error al inicializar MainWindow: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }
        private void BtnEntrada_Click(object sender, RoutedEventArgs e) => Registrar(a => a.Entrada = DateTime.Now, "Entrada");
        private void BtnIdaComer_Click(object sender, RoutedEventArgs e) => Registrar(a => a.IdaComer = DateTime.Now, "Ida a comer");
        private void BtnVueltaComer_Click(object sender, RoutedEventArgs e) => Registrar(a => a.VueltaComer = DateTime.Now, "Vuelta de comer");
        private void BtnSalida_Click(object sender, RoutedEventArgs e) => Registrar(a => a.Salida = DateTime.Now, "Salida");
        private void BtnRefrescar_Click(object sender, RoutedEventArgs e) => CargarAsistencias();

        private void BtnGestionarUsuarios_Click(object sender, RoutedEventArgs e)
        {
            new UserManagementWindow().ShowDialog();
            CargarAsistencias(); // Recargar para ver cambios
        }

        private void Registrar(Action<Asistencia> accion, string tipo)
        {
            var empleado = txtEmpleado.Text?.Trim();
            if (string.IsNullOrEmpty(empleado))
            {
                lblMensaje.Text = "Ingrese nombre del empleado.";
                return;
            }
            using (var db = new AppDbContext())
            {
                var hoy = DateTime.Today;
                var registro = db.Asistencias.FirstOrDefault(a => a.NombreEmpleado == empleado && a.Fecha == hoy);
                if (registro == null)
                {
                    registro = new Asistencia
                    {
                        NombreEmpleado = empleado,
                        Fecha = hoy
                    };
                    db.Asistencias.Add(registro);
                }
                accion(registro);
                db.SaveChanges();
            }
            lblMensaje.Text = $"{tipo} registrada: {DateTime.Now:HH:mm:ss}";
            CargarAsistencias();
        }

        private void CargarAsistencias()
        {
            using (var db = new AppDbContext())
            {
                dgAsistencias.ItemsSource = db.Asistencias
                                              .OrderByDescending(a => a.Fecha)
                                              .ThenBy(a => a.NombreEmpleado)
                                              .ToList();
            }
        }

        private void BtnGenerarReporte_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string connectionString = "Server=DESKTOP-5OD0LL7;Database=MarcaAsistenciaDB;Trusted_Connection=True;TrustServerCertificate=True;";

                int mes = DateTime.Now.Month - 1;
                int año = DateTime.Now.Year;
                if (mes == 0) { mes = 12; año--; }
                DateTime inicio = new DateTime(año, mes, 1);
                DateTime fin = inicio.AddMonths(1);
                var asistencias = new List<Asistencia>();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                SELECT Id, NombreEmpleado, Fecha, Entrada, IdaComer, VueltaComer, Salida
                FROM Asistencias
                WHERE Fecha >= @inicio AND Fecha < @fin
                ORDER BY NombreEmpleado, Fecha";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@inicio", inicio);
                        cmd.Parameters.AddWithValue("@fin", fin);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                asistencias.Add(new Asistencia
                                {
                                    Id = reader.GetInt32(0),
                                    NombreEmpleado = reader.GetString(1),
                                    Fecha = reader.GetDateTime(2),
                                    Entrada = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                                    IdaComer = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                                    VueltaComer = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                    Salida = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6),
                                });
                            }
                        }
                    }
                }

                if (asistencias.Count == 0)
                {
                    MessageBox.Show("No hay registros en la base de datos para ese mes.");
                    return;
                }

                // DEFINICIONES DE HORARIOS
                TimeSpan horaEntradaEsperada = new TimeSpan(9, 0, 0); // 9:00 AM
                TimeSpan horaSalidaEsperada = new TimeSpan(18, 0, 0); // 6:00 PM

                // GENERAR DÍAS LABORALES DEL MES
                List<DateTime> diasLaborales = new List<DateTime>();
                for (DateTime d = inicio; d < fin; d = d.AddDays(1))
                {
                    if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                    {
                        diasLaborales.Add(d);
                    }
                }

                // Agrupar por empleado
                var agrupado = asistencias.GroupBy(a => a.NombreEmpleado).ToDictionary(g => g.Key, g => g.ToList());

                // Diálogo para guardar PDF
                var dlg = new SaveFileDialog
                {
                    FileName = $"Asistencias-{año}-{mes:D2}.pdf",
                    Filter = "PDF files (*.pdf)|*.pdf"
                };
                if (dlg.ShowDialog() != true) return;

                using (var fs = new FileStream(dlg.FileName, FileMode.Create))
                {
                    var doc = new PdfDoc(PdfPageSize.A4, 40, 40, 40, 40);
                    PdfWriter.GetInstance(doc, fs);
                    doc.Open();

                    // FUENTES DEFINIDAS AQUÍ
                    var fuenteTitulo = PdfFontFactory.GetFont(PdfFontFactory.HELVETICA_BOLD, 16);
                    var fuenteSubtitulo = PdfFontFactory.GetFont(PdfFontFactory.HELVETICA_BOLD, 12);
                    var fuenteNormal = PdfFontFactory.GetFont(PdfFontFactory.HELVETICA, 9);
                    var fuenteBold = PdfFontFactory.GetFont(PdfFontFactory.HELVETICA_BOLD, 9);

                    // Título principal
                    var titulo = new PdfParagraph($"REPORTE DE ASISTENCIAS", fuenteTitulo);
                    titulo.Alignment = PdfElement.ALIGN_CENTER;
                    titulo.SpacingAfter = 5f;
                    doc.Add(titulo);

                    var subtitulo = new PdfParagraph($"{new DateTime(año, mes, 1):MMMM yyyy}", fuenteSubtitulo);
                    subtitulo.Alignment = PdfElement.ALIGN_CENTER;
                    subtitulo.SpacingAfter = 20f;
                    doc.Add(subtitulo);

                    // Por cada empleado
                    foreach (var emp in agrupado.OrderBy(x => x.Key))
                    {
                        var empleado = emp.Key;
                        var regs = emp.Value.OrderBy(r => r.Fecha).ToList();

                        // Título del empleado
                        var tituloEmpleado = new PdfParagraph($"EMPLEADO: {empleado}", fuenteSubtitulo);
                        tituloEmpleado.Alignment = PdfElement.ALIGN_CENTER;
                        tituloEmpleado.SpacingAfter = 10f;
                        doc.Add(tituloEmpleado);

                        // RESUMEN CORREGIDO (usando diasLaborales definido arriba)
                        int diasAsistidos = regs.Count(r => r.Entrada.HasValue && r.Salida.HasValue);
                        int llegadasTarde = regs.Count(r => r.Entrada.HasValue && r.Entrada.Value.TimeOfDay > horaEntradaEsperada);
                        int salidasTempranas = regs.Count(r => r.Salida.HasValue && r.Salida.Value.TimeOfDay < horaSalidaEsperada);
                        int faltas = diasLaborales.Count - regs.Count; // AHORA SÍ ESTÁ DEFINIDO

                        var resumen = new PdfParagraph(
                            $"Días asistidos: {diasAsistidos} | Faltas: {faltas} | Llegadas tarde: {llegadasTarde} | Salidas tempranas: {salidasTempranas}",
                            fuenteBold);
                        resumen.Alignment = PdfElement.ALIGN_CENTER;
                        resumen.SpacingAfter = 15f;
                        doc.Add(resumen);

                        // Tabla detallada
                        var tabla = CrearTablaAsistenciaCompleta(regs, diasLaborales, horaEntradaEsperada, horaSalidaEsperada, fuenteNormal, fuenteBold);
                        doc.Add(tabla);

                        // Separador entre empleados
                        doc.Add(new PdfParagraph(" "));
                        var linea = new PdfParagraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(1f, 100, PdfBaseColor.LIGHT_GRAY, Element.ALIGN_CENTER, -1)));
                        doc.Add(linea);
                        doc.Add(new PdfParagraph(" "));
                    }

                    doc.Close();
                }
                MessageBox.Show("Reporte generado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar reporte: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // MÉTODO 1: Crear tabla completa
        private PdfPTable CrearTablaAsistenciaCompleta(List<Asistencia> asistencias, List<DateTime> diasLaborales,
            TimeSpan horaEntradaEsperada, TimeSpan horaSalidaEsperada,
            iTextSharp.text.Font fuenteNormal, iTextSharp.text.Font fuenteBold)
        {
            var tabla = new PdfPTable(8);
            tabla.WidthPercentage = 100;
            tabla.SetWidths(new float[] { 1.4f, 1.1f, 1.1f, 1.3f, 1.1f, 0.9f, 0.9f, 1.2f });
            tabla.DefaultCell.BorderWidth = 0.5f;
            tabla.SpacingBefore = 10f;
            tabla.SpacingAfter = 10f;

            // Encabezados
            string[] headers = { "FECHA", "ENTRADA", "IDA COMER", "VUELTA COMER", "SALIDA", "TARDE", "TEMPRANO", "HORAS TRAB." };
            foreach (var header in headers)
            {
                var headerCell = new PdfPCell(new PdfPhrase(header.ToUpper(), fuenteBold))
                {
                    BackgroundColor = new PdfBaseColor(70, 130, 180), // Azul acero
                    PaddingTop = 8,
                    PaddingBottom = 8,
                    PaddingLeft = 5,
                    PaddingRight = 5,
                    HorizontalAlignment = PdfElement.ALIGN_CENTER,
                    VerticalAlignment = PdfElement.ALIGN_MIDDLE
                };
                tabla.AddCell(headerCell);
            }

            // Para cada día laboral del mes
            foreach (var dia in diasLaborales.OrderBy(d => d))
            {
                var asistenciaDelDia = asistencias.FirstOrDefault(a => a.Fecha.Date == dia.Date);

                if (asistenciaDelDia != null)
                {
                    // DÍA CON ASISTENCIA
                    AgregarFilaAsistenciaCompleta(tabla, asistenciaDelDia, horaEntradaEsperada, horaSalidaEsperada, fuenteNormal, fuenteBold);
                }
                else
                {
                    // DÍA DE FALTA
                    AgregarFilaFaltaCompleta(tabla, dia, fuenteNormal, fuenteBold);
                }
            }

            return tabla;
        }

        // MÉTODO 2: Fila de asistencia completa
        private void AgregarFilaAsistenciaCompleta(PdfPTable tabla, Asistencia asistencia, TimeSpan horaEntradaEsperada,
            TimeSpan horaSalidaEsperada, iTextSharp.text.Font fuenteNormal, iTextSharp.text.Font fuenteBold)
        {
            var colorNormal = PdfBaseColor.WHITE;

            // 1. FECHA
            tabla.AddCell(CrearCeldaTexto(asistencia.Fecha.ToString("dd/MM/yyyy"), fuenteNormal, colorNormal, true));

            // 2. ENTRADA (resaltada si es tarde)
            string entradaTexto = asistencia.Entrada.HasValue ? asistencia.Entrada.Value.ToString("HH:mm") : "";
            var colorEntrada = asistencia.Entrada.HasValue && asistencia.Entrada.Value.TimeOfDay > horaEntradaEsperada
                ? new PdfBaseColor(255, 220, 220) : colorNormal;
            tabla.AddCell(CrearCeldaTexto(entradaTexto, fuenteNormal, colorEntrada, true));

            // 3. IDA A COMER
            string idaComerTexto = asistencia.IdaComer.HasValue ? asistencia.IdaComer.Value.ToString("HH:mm") : "";
            tabla.AddCell(CrearCeldaTexto(idaComerTexto, fuenteNormal, colorNormal, true));

            // 4. VUELTA DE COMER
            string vueltaComerTexto = asistencia.VueltaComer.HasValue ? asistencia.VueltaComer.Value.ToString("HH:mm") : "";
            tabla.AddCell(CrearCeldaTexto(vueltaComerTexto, fuenteNormal, colorNormal, true));

            // 5. SALIDA (resaltada si es temprana)
            string salidaTexto = asistencia.Salida.HasValue ? asistencia.Salida.Value.ToString("HH:mm") : "";
            var colorSalida = asistencia.Salida.HasValue && asistencia.Salida.Value.TimeOfDay < horaSalidaEsperada
                ? new PdfBaseColor(255, 220, 220) : colorNormal;
            tabla.AddCell(CrearCeldaTexto(salidaTexto, fuenteNormal, colorSalida, true));

            // 6. TARDE (SÍ/NO)
            bool esTarde = asistencia.Entrada.HasValue && asistencia.Entrada.Value.TimeOfDay > horaEntradaEsperada;
            var textoTarde = esTarde ? "SÍ" : "NO";
            var colorTarde = esTarde ? new PdfBaseColor(255, 150, 150) : new PdfBaseColor(200, 255, 200);
            tabla.AddCell(CrearCeldaTexto(textoTarde, fuenteBold, colorTarde, true));

            // 7. TEMPRANO (SÍ/NO)
            bool esTemprano = asistencia.Salida.HasValue && asistencia.Salida.Value.TimeOfDay < horaSalidaEsperada;
            var textoTemprano = esTemprano ? "SÍ" : "NO";
            var colorTemprano = esTemprano ? new PdfBaseColor(255, 150, 150) : new PdfBaseColor(200, 255, 200);
            tabla.AddCell(CrearCeldaTexto(textoTemprano, fuenteBold, colorTemprano, true));

            // 8. HORAS TRABAJADAS
            string horasTexto = "";
            if (asistencia.Entrada.HasValue && asistencia.Salida.HasValue)
            {
                TimeSpan total = asistencia.Salida.Value - asistencia.Entrada.Value;
                if (asistencia.IdaComer.HasValue && asistencia.VueltaComer.HasValue)
                {
                    total -= (asistencia.VueltaComer.Value - asistencia.IdaComer.Value);
                }
                horasTexto = $"{total.Hours:D2}:{total.Minutes:D2}h";
            }
            tabla.AddCell(CrearCeldaTexto(horasTexto, fuenteBold, colorNormal, true));
        }

        // MÉTODO 3: Fila de falta completa
        private void AgregarFilaFaltaCompleta(PdfPTable tabla, DateTime dia, iTextSharp.text.Font fuenteNormal, iTextSharp.text.Font fuenteBold)
        {
            var colorFalta = new PdfBaseColor(255, 200, 200); // Rosa claro

            // 1. FECHA
            tabla.AddCell(CrearCeldaTexto(dia.ToString("dd/MM/yyyy"), fuenteBold, colorFalta, true));

            // 2-5. CAMPOS VACÍOS
            for (int i = 0; i < 4; i++)
            {
                tabla.AddCell(CrearCeldaTexto("", fuenteNormal, colorFalta, true));
            }

            // 6-7. TARDE Y TEMPRANO VACÍOS
            tabla.AddCell(CrearCeldaTexto("", fuenteNormal, colorFalta, true));
            tabla.AddCell(CrearCeldaTexto("", fuenteNormal, colorFalta, true));

            // 8. FALTA EN ROJO
            var celdaFalta = new PdfPCell(new PdfPhrase("FALTA", fuenteBold))
            {
                PaddingTop = 6,
                PaddingBottom = 6,
                PaddingLeft = 4,
                PaddingRight = 4,
                HorizontalAlignment = PdfElement.ALIGN_CENTER,
                VerticalAlignment = PdfElement.ALIGN_MIDDLE,
                BackgroundColor = new PdfBaseColor(220, 50, 50), // Rojo intenso
                BorderWidth = 1f,
                BorderColor = PdfBaseColor.BLACK
            };
            tabla.AddCell(celdaFalta);
        }

        // MÉTODO AUXILIAR: Crear celda de texto (REUTILIZABLE)
        private PdfPCell CrearCeldaTexto(string texto, iTextSharp.text.Font fuente, PdfBaseColor colorFondo, bool centrado)
        {
            var celda = new PdfPCell(new PdfPhrase(texto, fuente))
            {
                PaddingTop = 6,
                PaddingBottom = 6,
                PaddingLeft = 4,
                PaddingRight = 4,
                HorizontalAlignment = centrado ? PdfElement.ALIGN_CENTER : PdfElement.ALIGN_LEFT,
                VerticalAlignment = PdfElement.ALIGN_MIDDLE,
                BackgroundColor = colorFondo,
                BorderWidth = 0.5f
            };
            return celda;
        }
    }
}