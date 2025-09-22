using System;
using System.Linq;
using System.Windows;
using MarcaAsistencia.Data;
using MarcaAsistencia.Models;
using MarcaAsistencia.Views;

namespace MarcaAsistencia
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text?.Trim();
            string password = txtPassword.Password;

            // Debug: Mostrar lo que se está intentando
            System.Diagnostics.Debug.WriteLine($"Intentando login - Username: '{username}', Password: '{password}'");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MostrarMensaje("Por favor, complete todos los campos.", "Error");
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    // Debug: Verificar conexión
                    System.Diagnostics.Debug.WriteLine("Conexión a BD exitosa. Verificando usuarios...");

                    // Verificar cuántos usuarios hay
                    var totalUsuarios = db.Usuarios.Count();
                    System.Diagnostics.Debug.WriteLine($"Total de usuarios en BD: {totalUsuarios}");

                    // Buscar usuario específico
                    var usuario = db.Usuarios
                        .FirstOrDefault(u => u.Username == username && u.Password == password);

                    System.Diagnostics.Debug.WriteLine($"Usuario encontrado: {(usuario != null ? "SÍ" : "NO")}");

                    if (usuario == null)
                    {
                        // Debug: Mostrar usuarios disponibles
                        var usuarios = db.Usuarios.Select(u => u.Username).ToList();
                        System.Diagnostics.Debug.WriteLine($"Usuarios disponibles: {string.Join(", ", usuarios)}");

                        MostrarMensaje("Usuario o contraseña incorrectos.", "Error");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"Usuario autenticado: {usuario.Nombre} (Admin: {usuario.EsAdministrador})");

                    // NO cerrar la ventana de login todavía - abrir nueva ventana primero
                    Window nuevaVentana = null;

                    if (usuario.EsAdministrador)
                    {
                        System.Diagnostics.Debug.WriteLine("Abriendo MainWindow para administrador...");
                        nuevaVentana = new MainWindow();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Abriendo MarcaAsistenciaWindow para empleado...");
                        nuevaVentana = new MarcaAsistenciaWindow(usuario);
                    }

                    // Mostrar nueva ventana
                    nuevaVentana.Show();

                    // Ahora cerrar login
                    System.Diagnostics.Debug.WriteLine("Cerrando ventana de login...");
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EXCEPCIÓN en login: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                MostrarMensaje($"Error al iniciar sesión: {ex.Message}", "Error");
            }
        }

        private void MostrarMensaje(string mensaje, string tipo)
        {
            lblMensaje.Text = mensaje;
            lblMensaje.Foreground = tipo == "Error" ?
                System.Windows.Media.Brushes.Red :
                System.Windows.Media.Brushes.Green;

            System.Diagnostics.Debug.WriteLine($"Mensaje mostrado: {mensaje} (Tipo: {tipo})");
        }

        // Evento para cuando se cierra la ventana
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Ventana de login cerrándose...");
        }
    }
}