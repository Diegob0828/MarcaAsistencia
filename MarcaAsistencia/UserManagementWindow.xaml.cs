using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MarcaAsistencia.Data;
using MarcaAsistencia.Models;

namespace MarcaAsistencia.Views
{
    public partial class UserManagementWindow : Window
    {
        private Usuario _usuarioSeleccionado;

        public UserManagementWindow()
        {
            InitializeComponent();
            CargarUsuarios();
        }

        private void CargarUsuarios()
        {
            using (var db = new AppDbContext())
            {
                dgUsuarios.ItemsSource = db.Usuarios
                    .Where(u => !u.EsAdministrador) // Solo empleados
                    .ToList();
            }
        }

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (ValidarCampos())
            {
                using (var db = new AppDbContext())
                {
                    var nuevo = new Usuario
                    {
                        Username = txtUsername.Text.Trim(),
                        Password = txtPassword.Password, // En producción, hashear la contraseña
                        Nombre = txtNombre.Text.Trim(),
                        EsAdministrador = false
                    };
                    db.Usuarios.Add(nuevo);
                    db.SaveChanges();
                }
                MostrarMensaje("Empleado agregado correctamente.", Brushes.Green);
                LimpiarCampos();
                CargarUsuarios();
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (_usuarioSeleccionado == null)
            {
                MostrarMensaje("Seleccione un empleado para editar.", Brushes.Red);
                return;
            }
            if (ValidarCampos())
            {
                using (var db = new AppDbContext())
                {
                    var usuario = db.Usuarios.Find(_usuarioSeleccionado.Id);
                    if (usuario != null)
                    {
                        usuario.Username = txtUsername.Text.Trim();
                        if (!string.IsNullOrEmpty(txtPassword.Password))
                        {
                            usuario.Password = txtPassword.Password; // Hashear en producción
                        }
                        usuario.Nombre = txtNombre.Text.Trim();
                        db.SaveChanges();
                    }
                }
                MostrarMensaje("Empleado editado correctamente.", Brushes.Green);
                LimpiarCampos();
                CargarUsuarios();
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_usuarioSeleccionado == null)
            {
                MostrarMensaje("Seleccione un empleado para eliminar.", Brushes.Red);
                return;
            }
            var confirm = MessageBox.Show($"¿Eliminar a {_usuarioSeleccionado.Nombre}?", "Confirmar", MessageBoxButton.YesNo);
            if (confirm == MessageBoxResult.Yes)
            {
                using (var db = new AppDbContext())
                {
                    var usuario = db.Usuarios.Find(_usuarioSeleccionado.Id);
                    if (usuario != null)
                    {
                        db.Usuarios.Remove(usuario);
                        db.SaveChanges();
                    }
                }
                MostrarMensaje("Empleado eliminado correctamente.", Brushes.Green);
                LimpiarCampos();
                CargarUsuarios();
            }
        }

        private bool ValidarCampos()
        {
            if (string.IsNullOrEmpty(txtUsername.Text.Trim()))
            {
                MostrarMensaje("Ingrese un username.", Brushes.Red);
                return false;
            }
            if (string.IsNullOrEmpty(txtPassword.Password) && _usuarioSeleccionado == null) // Para edición, contraseña opcional
            {
                MostrarMensaje("Ingrese una contraseña.", Brushes.Red);
                return false;
            }
            if (string.IsNullOrEmpty(txtNombre.Text.Trim()))
            {
                MostrarMensaje("Ingrese un nombre.", Brushes.Red);
                return false;
            }
            return true;
        }

        private void MostrarMensaje(string mensaje, SolidColorBrush color)
        {
            lblMensaje.Text = mensaje;
            lblMensaje.Foreground = color;
        }

        private void LimpiarCampos()
        {
            txtUsername.Text = "";
            txtPassword.Password = "";
            txtNombre.Text = "";
            _usuarioSeleccionado = null;
        }

        private void DgUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _usuarioSeleccionado = dgUsuarios.SelectedItem as Usuario;
            if (_usuarioSeleccionado != null)
            {
                txtUsername.Text = _usuarioSeleccionado.Username;
                txtNombre.Text = _usuarioSeleccionado.Nombre;
                txtPassword.Password = ""; // No mostrar contraseña por seguridad
            }
        }
    }
}