using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Repositories;

namespace ControlClientesRestaurante.Services
{
    public class ResenaEstadisticasDto
    {
        public decimal Promedio { get; set; } = 5.0m;
        public int TotalResenas { get; set; } = 0;
        public int Estrellas5 { get; set; } = 0;
        public int Estrellas4 { get; set; } = 0;
        public int Estrellas3 { get; set; } = 0;
        public int Estrellas2 { get; set; } = 0;
        public int Estrellas1 { get; set; } = 0;
        public int Porcentaje5 => TotalResenas > 0 ? (int)Math.Round((double)Estrellas5 / TotalResenas * 100) : 0;
        public int Porcentaje4 => TotalResenas > 0 ? (int)Math.Round((double)Estrellas4 / TotalResenas * 100) : 0;
        public int Porcentaje3 => TotalResenas > 0 ? (int)Math.Round((double)Estrellas3 / TotalResenas * 100) : 0;
        public int Porcentaje2 => TotalResenas > 0 ? (int)Math.Round((double)Estrellas2 / TotalResenas * 100) : 0;
        public int Porcentaje1 => TotalResenas > 0 ? (int)Math.Round((double)Estrellas1 / TotalResenas * 100) : 0;
    }

    public class ResenaService
    {
        private readonly IResenaRepository _repository;

        public ResenaService(IResenaRepository repository)
        {
            _repository = repository;
        }

        public List<Resena> ObtenerAprobadas() => _repository.ObtenerAprobadas();

        public List<Resena> ObtenerTodas() => _repository.ObtenerTodas();

        public (bool ok, string mensaje, Resena? resena) CrearResena(CrearResenaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NombreCliente) || dto.NombreCliente.Trim().Length < 2)
            {
                return (false, "Por favor ingresa tu nombre completo (mínimo 2 caracteres).", null);
            }

            if (dto.Calificacion < 1 || dto.Calificacion > 5)
            {
                return (false, "La calificación debe ser entre 1 y 5 estrellas.", null);
            }

            if (string.IsNullOrWhiteSpace(dto.Comentario) || dto.Comentario.Trim().Length < 5)
            {
                return (false, "Tu reseña debe contener al menos 5 caracteres para compartir tu experiencia.", null);
            }

            // Límites de longitud: el servidor no confía en los atributos maxlength del navegador
            if (dto.NombreCliente.Trim().Length > 80)
                return (false, "El nombre no puede superar los 80 caracteres.", null);
            if (dto.Comentario.Trim().Length > 1000)
                return (false, "La reseña no puede superar los 1000 caracteres.", null);
            if ((dto.PlatoRecomendado?.Trim().Length ?? 0) > 100 || (dto.TipoExperiencia?.Trim().Length ?? 0) > 50 || (dto.Correo?.Trim().Length ?? 0) > 150)
                return (false, "Alguno de los datos enviados es demasiado largo.", null);

            string tipoExp = string.IsNullOrWhiteSpace(dto.TipoExperiencia) ? "En Restaurante" : dto.TipoExperiencia.Trim();
            string? plato = string.IsNullOrWhiteSpace(dto.PlatoRecomendado) ? null : dto.PlatoRecomendado.Trim();

            var resena = new Resena
            {
                NombreCliente = dto.NombreCliente.Trim(),
                Correo = string.IsNullOrWhiteSpace(dto.Correo) ? null : dto.Correo.Trim(),
                Calificacion = dto.Calificacion,
                Comentario = dto.Comentario.Trim(),
                PlatoRecomendado = plato,
                TipoExperiencia = tipoExp,
                // Cualquiera puede enviar reseñas sin cuenta: quedan pendientes y las aprueba el administrador
                Aprobada = false,
                Verificado = false,
                FechaCreacion = DateTime.Now
            };

            try
            {
                _repository.Crear(resena);
                _repository.GuardarCambios();
                return (true, "¡Gracias por tu reseña! Será publicada en cuanto el equipo la revise.", resena);
            }
            catch (Exception)
            {
                return (false, "No pudimos guardar tu opinión. Inténtalo de nuevo en unos minutos.", null);
            }
        }

        public ResenaEstadisticasDto ObtenerEstadisticas()
        {
            var aprobadas = _repository.ObtenerAprobadas();
            var stats = new ResenaEstadisticasDto
            {
                TotalResenas = aprobadas.Count
            };

            if (stats.TotalResenas == 0)
            {
                stats.Promedio = 5.0m;
                return stats;
            }

            stats.Estrellas5 = aprobadas.Count(r => r.Calificacion == 5);
            stats.Estrellas4 = aprobadas.Count(r => r.Calificacion == 4);
            stats.Estrellas3 = aprobadas.Count(r => r.Calificacion == 3);
            stats.Estrellas2 = aprobadas.Count(r => r.Calificacion == 2);
            stats.Estrellas1 = aprobadas.Count(r => r.Calificacion == 1);

            double prom = aprobadas.Average(r => r.Calificacion);
            stats.Promedio = Math.Round((decimal)prom, 1);

            return stats;
        }

        public (bool ok, string mensaje) CambiarAprobacion(int id, bool aprobada)
        {
            var r = _repository.ObtenerPorId(id);
            if (r == null) return (false, "Reseña no encontrada.");

            r.Aprobada = aprobada;
            _repository.Actualizar(r);
            _repository.GuardarCambios();
            return (true, $"Reseña {(aprobada ? "aprobada" : "ocultada")} correctamente.");
        }

        public (bool ok, string mensaje) Eliminar(int id)
        {
            var r = _repository.ObtenerPorId(id);
            if (r == null) return (false, "Reseña no encontrada.");

            _repository.Eliminar(id);
            _repository.GuardarCambios();
            return (true, "Reseña eliminada correctamente.");
        }
    }
}
