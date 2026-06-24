using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OperacionTools.Models;
using OperacionTools.Helpers;
using System.Numerics;

namespace OperacionTools.Services
{
    internal class HistorialService
    {
        private readonly string _rutaLocal;
        private readonly string _configPath;

        public HistorialService()
        {
            _rutaLocal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OperacionTools");
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "networkConfig.json");
        }

        public List<ItemHistorial> ObtenerHistorial(DateTime? desde = null, DateTime? hasta = null)
        {
            var lista = new List<ItemHistorial>();
            string carpetaObjetivo = _rutaLocal;

            //evalua si el usuario ha configurado una ruta personalizada de red UNC
            if (File.Exists(_configPath))
            {
                try
                {
                    string jsonConfig = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<ConfiguracionRed>(jsonConfig);
                    if (config != null && config.UtilizarRutaRed && !string.IsNullOrEmpty(config.RutaServidor) && Directory.Exists(config.RutaServidor))
                    {
                        carpetaObjetivo = config.RutaServidor;
                    }
                }
                catch
                {
                    Console.WriteLine("Error al leer la configuración de red. Se usará la ruta local por defecto.");
                }

            }

            if (!Directory.Exists(carpetaObjetivo)) return lista;

            //buscar todos los archivos conciliados que coincidan con el patrón "Inventario_Conciliado_*.json"
            var archivos = Directory.GetFiles(carpetaObjetivo, "Inventario_Conciliado_*.json");

            foreach (var archivo in archivos)
            {
                try
                {
                    var infoArchivo = new FileInfo(archivo);
                    DateTime fechaCreacion = infoArchivo.CreationTime;

                    //aplicar filtros de fecha si lo definió el usuario
                    if (desde.HasValue && fechaCreacion.Date < desde.Value.Date) continue;
                    if (hasta.HasValue && fechaCreacion.Date > hasta.Value.Date) continue;

                    //mapeo de metadatos del archivo
                    string contenido = File.ReadAllText(archivo);
                    var datos = JsonSerializer.Deserialize<List<RegistroInventario>>(contenido);

                    if (datos != null && datos.Any())
                    {
                        var primerItem = datos.First();
                        lista.Add(new ItemHistorial
                        {
                            NombreArchivo = infoArchivo.Name,
                            RutaCompleta = archivo,
                            Fecha = fechaCreacion,
                            TotalRegistros = datos.Count,
                            Bodega = datos.FirstOrDefault(x => !string.IsNullOrEmpty(x.Bodega))?.Bodega ?? "Malla General",
                            DatosInternos = datos
                        });
                    }
                }
                catch
                {
                    Console.WriteLine($"Error al procesar el archivo: {archivo}. Se omitirá este archivo del historial.");
                }
            }

            return lista.OrderByDescending(x => x.Fecha).ToList();
        }
    }

    public class ItemHistorial
    {
        public string NombreArchivo { get; set; } = string.Empty;
        public string RutaCompleta { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public int TotalRegistros { get; set; }
        public string Bodega { get; set; } = string.Empty;
        public List<RegistroInventario> DatosInternos { get; set; } = new();
    }
}
