using System;
using System.Linq;
using System.Windows;
using MarcaAsistencia.Data;
using MarcaAsistencia.Models;

namespace MarcaAsistencia
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Iniciando aplicación...");

                // Inicializar base de datos
                using (var db = new AppDbContext())
                {
                    System.Diagnostics.Debug.WriteLine("Verificando base de datos...");
                    db.Database.EnsureCreated();

                    // Verificar usuarios
                    var totalUsuarios = db.Usuarios.Count();
                    System.Diagnostics.Debug.WriteLine($"Usuarios encontrados: {totalUsuarios}");

                    if (totalUsuarios == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("Creando usuarios por defecto...");
                        db.Usuarios.AddRange(
                            new Usuario
                            {
                                Username = "admin",
                                Password = "admin123",
                                Nombre = "Administrador",
                                EsAdministrador = true,
                                FechaCreacion = DateTime.Now
                            },
                            new Usuario
                            {
                                Username = "empleado",
                                Password = "123456",
                                Nombre = "Empleado Ejemplo",
                                EsAdministrador = false,
                                FechaCreacion = DateTime.Now
                            }
                        );
                        db.SaveChanges();
                        System.Diagnostics.Debug.WriteLine("Usuarios creados exitosamente.");
                    }
                }

                System.Diagnostics.Debug.WriteLine("Mostrando ventana de login...");
                new LoginWindow().Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error crítico al iniciar: {ex.Message}");
                MessageBox.Show($"Error crítico: {ex.Message}", "Error Fatal",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            base.OnStartup(e);
        }
    }
}