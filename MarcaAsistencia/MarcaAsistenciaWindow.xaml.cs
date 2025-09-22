using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using MarcaAsistencia.Data;
using MarcaAsistencia.Models;

namespace MarcaAsistencia.Views
{
    public partial class MarcaAsistenciaWindow : Window
    {
        private Usuario _usuarioActual;
        private DateTime _fechaHoy;

        public MarcaAsistenciaWindow(Usuario usuario)
        {
            try
            {
                InitializeComponent();
                _usuarioActual = usuario ?? throw new ArgumentNullException(nameof(usuario));
                System.Diagnostics.Debug.WriteLine($"MarcaAsistenciaWindow inicializada para: {_usuarioActual.Nombre}");
                CargarDatosUsuario();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MarcaAsistenciaWindow constructor: {ex.Message}");
                MessageBox.Show($"Error al inicializar vista de empleado: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void CargarDatosUsuario()
        {
            lblEmpleado.Text = $"Empleado: {_usuarioActual.Nombre}";
            _fechaHoy = DateTime.Today;
            lblFecha.Text = _fechaHoy.ToString("dddd, dd 'de' MMMM 'de' yyyy");

            ActualizarHorario();
            ActualizarEstadoBotones();
        }

        private void ActualizarHorario()
        {
            using (var db = new AppDbContext())
            {
                var asistencia = db.Asistencias
                    .FirstOrDefault(a => a.NombreEmpleado == _usuarioActual.Nombre &&
                                       a.Fecha == _fechaHoy);

                dgHorario.ItemsSource = new[]
                {
                    new { Evento = "Entrada", Hora = asistencia?.Entrada?.ToString("HH:mm") ?? "-",
                          Estado = asistencia?.Entrada != null ? "✓ Registrado" : "⏳ Pendiente" },
                    new { Evento = "Ida a Comer", Hora = asistencia?.IdaComer?.ToString("HH:mm") ?? "-",
                          Estado = asistencia?.IdaComer != null ? "✓ Registrado" : "⏳ Pendiente" },
                    new { Evento = "Vuelta de Comer", Hora = asistencia?.VueltaComer?.ToString("HH:mm") ?? "-",
                          Estado = asistencia?.VueltaComer != null ? "✓ Registrado" : "⏳ Pendiente" },
                    new { Evento = "Salida", Hora = asistencia?.Salida?.ToString("HH:mm") ?? "-",
                          Estado = asistencia?.Salida != null ? "✓ Registrado" : "⏳ Pendiente" }
                };
            }
        }

        private void ActualizarEstadoBotones()
        {
            using (var db = new AppDbContext())
            {
                var asistencia = db.Asistencias
                    .FirstOrDefault(a => a.NombreEmpleado == _usuarioActual.Nombre &&
                                       a.Fecha == _fechaHoy);

                // Habilitar botones según el flujo lógico
                btnEntrada.IsEnabled = asistencia?.Entrada == null;
                btnIdaComer.IsEnabled = asistencia?.Entrada != null && asistencia?.IdaComer == null;
                btnVueltaComer.IsEnabled = asistencia?.IdaComer != null && asistencia?.VueltaComer == null;
                btnSalida.IsEnabled = asistencia?.VueltaComer != null && asistencia?.Salida == null;

                // Cambiar colores según el estado
                if (!btnEntrada.IsEnabled)
                {
                    btnEntrada.Background = Brushes.LightGreen;
                    btnEntrada.Foreground = Brushes.DarkGreen;
                }
                else
                {
                    btnEntrada.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                    btnEntrada.Foreground = Brushes.White;
                }

                if (!btnIdaComer.IsEnabled && asistencia?.IdaComer != null)
                {
                    btnIdaComer.Background = Brushes.LightYellow;
                    btnIdaComer.Foreground = Brushes.DarkGoldenrod;
                }
                else if (btnIdaComer.IsEnabled)
                {
                    btnIdaComer.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                    btnIdaComer.Foreground = Brushes.Black;
                }

                if (!btnVueltaComer.IsEnabled && asistencia?.VueltaComer != null)
                {
                    btnVueltaComer.Background = Brushes.LightBlue;
                    btnVueltaComer.Foreground = Brushes.DarkBlue;
                }
                else if (btnVueltaComer.IsEnabled)
                {
                    btnVueltaComer.Background = new SolidColorBrush(Color.FromRgb(23, 162, 184));
                    btnVueltaComer.Foreground = Brushes.White;
                }

                if (!btnSalida.IsEnabled && asistencia?.Salida != null)
                {
                    btnSalida.Background = Brushes.LightCoral;
                    btnSalida.Foreground = Brushes.DarkRed;
                }
                else if (btnSalida.IsEnabled)
                {
                    btnSalida.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                    btnSalida.Foreground = Brushes.White;
                }
            }
        }

        private void Registrar(Action<Asistencia> accion, string tipo)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var asistencia = db.Asistencias
                        .FirstOrDefault(a => a.NombreEmpleado == _usuarioActual.Nombre &&
                                           a.Fecha == _fechaHoy);

                    if (asistencia == null)
                    {
                        asistencia = new Asistencia
                        {
                            NombreEmpleado = _usuarioActual.Nombre,
                            Fecha = _fechaHoy
                        };
                        db.Asistencias.Add(asistencia);
                    }

                    accion(asistencia);
                    db.SaveChanges();
                }

                lblMensaje.Text = $"{tipo} registrada correctamente a las {DateTime.Now:HH:mm:ss}";
                lblMensaje.Foreground = Brushes.Green;

                ActualizarHorario();
                ActualizarEstadoBotones();

                // Limpiar mensaje después de 3 segundos
                Task.Delay(3000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => lblMensaje.Text = "");
                });
            }
            catch (Exception ex)
            {
                lblMensaje.Text = $"Error al registrar {tipo}: {ex.Message}";
                lblMensaje.Foreground = Brushes.Red;
            }
        }

        private void BtnEntrada_Click(object sender, RoutedEventArgs e)
        {
            Registrar(a => a.Entrada = DateTime.Now, "Entrada");
        }

        private void BtnIdaComer_Click(object sender, RoutedEventArgs e)
        {
            Registrar(a => a.IdaComer = DateTime.Now, "Ida a comer");
        }

        private void BtnVueltaComer_Click(object sender, RoutedEventArgs e)
        {
            Registrar(a => a.VueltaComer = DateTime.Now, "Vuelta de comer");
        }

        private void BtnSalida_Click(object sender, RoutedEventArgs e)
        {
            Registrar(a => a.Salida = DateTime.Now, "Salida");
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show("¿Desea cerrar sesión?", "Cerrar Sesión",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                new LoginWindow().Show();
                this.Close();
            }
        }
    }
}