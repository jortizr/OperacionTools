using System;
using System.Collections.Generic;
using System.Text;

namespace OperacionTools.Models
{
    /// <summary>
    /// Representa un ítem de inventario dentro del flujo de conciliación.
    /// Mapea los datos tanto de las lecturas físicas como de los registros del sistema.
    /// </summary>
    public class RegistroInventario
    {
        /// <summary>
        /// Obtiene o establece el código de la Regional (ej. "01", "08").
        /// </summary>
        public string Reg { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece el tipo de Servicio o forma de pago (ej. "4", "1").
        /// </summary>
        public string Serv { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene o establece el número consecutivo de la guía, normalizado a 9 dígitos.
        /// </summary>
        public string Consecutivo { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene la representación unificada de la guía utilizando la estructura estándar (.Reg-Serv-Consecutivo).
        /// </summary>
        public string GuiaCompleta => $".{Reg}-{Serv}-{Consecutivo}";

        /// <summary>
        /// Obtiene o establece la cantidad de unidades registradas originalmente en el sistema (Excel).
        /// </summary>
        public int UnidadesEsperadas { get; set; } = 0;

        /// <summary>
        /// Obtiene o establece la cantidad de unidades físicas escaneadas por el colaborador.
        /// </summary>
        public int UnidadesLeidas { get; set; } = 0;

        /// <summary>
        /// Obtiene o establece el dictamen final del cruce (ej. "✅Conciliado Total", "Sobrante Físico", "Faltan Unidades").
        /// </summary>
        public string EstadoConciliacion { get; set; } = "Leído (Pendiente Validación)";

        /// <summary>
        /// Obtiene o establece la regional maestra de descarga del inventario (Columna 'Regional' del Excel).
        /// </summary>
        public string RegionalMaestro { get; set; } = string.Empty;

        public string Novedad { get; set; } = string.Empty;
        public string Saldo { get; set; } = string.Empty;
        public string Remitente { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Rack { get; set; } = string.Empty;
        public string CodEntr { get; set; } = string.Empty;
        public string Bodega { get; set; } = string.Empty;

    }
}
