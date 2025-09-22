using System;

namespace MarcaAsistencia.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Nombre { get; set; }
        public bool EsAdministrador { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}