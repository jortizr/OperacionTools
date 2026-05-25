using System;
using System.Collections.Generic;
using System.Text;

namespace OperacionTools.Helpers
{
    internal class ConfiguracionRed
    {
        public string RutaServidor { get; set; } = @"\\SERVIDOR-PC\Compartido\InventarioData\";
        public string UsuarioRed { get; set; } = "";
        public string ContrasenaRed { get; set; } = "";
        public bool UtilizarRutaRed { get; set; } = true;
    }
}
