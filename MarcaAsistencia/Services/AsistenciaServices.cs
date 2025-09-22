using MarcaAsistencia.Data;
using MarcaAsistencia.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarcaAsistencia.Services
{
    public class AsistenciaService
    {
        public List<Asistencia> GetAsistenciasMes(int month, int year)
        {
            using (var db = new AppDbContext())
            {
                DateTime start = new DateTime(year, month, 1);
                DateTime end = start.AddMonths(1);

                return db.Asistencias
                         .Where(a => a.Fecha >= start && a.Fecha < end)
                         .OrderBy(a => a.NombreEmpleado)
                         .ThenBy(a => a.Fecha)
                         .ToList();
            }
        }
    }
}
