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
    public class RegionalService
    {
        private readonly string _rutaArchivo;

        public RegionalService()
        {

            // 1. Creamos o apuntamos a la carpeta 'Settings' junto al ejecutable (.exe)
            string rutaLocalBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");

            if (!Directory.Exists(rutaLocalBase))
                Directory.CreateDirectory(rutaLocalBase);

            //definicion de la ruta completa del archivo JSON de Regionales
            _rutaArchivo = Path.Combine(rutaLocalBase, "regionales.json");
            AsegurarCatálogoInicial();
        }

        private void AsegurarCatálogoInicial()
        {
            if (!File.Exists(_rutaArchivo))
            {
                var baseInicial = new Dictionary<string, string>
                {
                    { "0", "No registrada" },
                    { "1", "Bogotá D.C." },
                    { "2", "Cali" },
                    { "3", "Medellín" },
                    { "4", "Barranquilla" },
                    { "5", "Pereira" },
                    { "6", "Bucaramanga" },
                    { "7", "Manizales" },
                    { "8", "Ibagué" },
                    { "9", "Pasto" },
                    { "10", "Sincelejo" },
                    { "86", "Monteria" }
                };

                string json = JsonSerializer.Serialize(baseInicial, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_rutaArchivo, json, Encoding.UTF8);
            }
        }

        public Dictionary<string, string> ObtenerRegionales()
        {
            try
            {
                if (!File.Exists(_rutaArchivo)) return new Dictionary<string, string>();
                string json = File.ReadAllText(_rutaArchivo, Encoding.UTF8);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch { 
                return new Dictionary<string, string>(); 
            }
        }
    }
}