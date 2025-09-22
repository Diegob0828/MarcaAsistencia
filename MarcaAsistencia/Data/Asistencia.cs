using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarcaAsistencia.Models
{
    public class Asistencia
    {
        public int Id { get; set; }
        public string NombreEmpleado { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? Entrada { get; set; }
        public DateTime? IdaComer { get; set; }
        public DateTime? VueltaComer { get; set; }
        public DateTime? Salida { get; set; }
    }

}
