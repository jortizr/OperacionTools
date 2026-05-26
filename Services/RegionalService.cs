using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace OperacionTools.Services
{
    /// <summary>
    /// Administra de forma local el catálogo de códigos de Regionales y nombres de ciudades.
    /// </summary>
    /// 
    internal class RegionalService
    {
        private readonly string _rutaArchivo;

        public RegionalService()
        {
            _rutaArchivo = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OperacionTools",
                "regionales.json"
            );
            AsegurarCatalogoInicial();
        }

        private void AsegurarCatalogoInicial()
        {
            string carpeta = Path.GetDirectoryName(_rutaArchivo);
            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);

            if (!File.Exists(_rutaArchivo))
            {
                var baseInicial = new Dictionary<string, string>
                {
                    { "1", "Bogota D.C." },
                    { "3", "Medellin" },
                    { "2", "Cali" },
                    { "4", "Barranquilla" },
                    { "5", "Pereira" },
                    { "8", "Ibagué" },
                    { "10", "Sincelejo" },
                    { "86", "Monteria" },
                    { "12", "Neiva" },
                    { "11", "Cucuta" }

                };
                string json = JsonSerializer.Serialize(baseInicial, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_rutaArchivo, json);
            }

        }

        public Dictionary<string, string> ObtenerRegionales()
        {
            try
            {
                if (!File.Exists(_rutaArchivo)) return new Dictionary<string, string>();
                string json = File.ReadAllText(_rutaArchivo);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch { return new Dictionary<string, string>(); }
        }
    }
}
