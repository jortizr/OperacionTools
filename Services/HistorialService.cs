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
            _rutaLocal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InventariosGuardados");
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "networkConfig.json");
        }

        public List<ItemHistorial> ObtenerHistorial(DateTime? desde = null, DateTime? hasta = null)
        {
            var lista = new List<ItemHistorial>();
            var carpetasAProcesar = new HashSet<string>();

            // Se incluye la carpeta local por defecto
            if (Directory.Exists(_rutaLocal))
            {
                carpetasAProcesar.Add(_rutaLocal);
            }

            //evalua si el usuario ha configurado una ruta personalizada de red UNC
            if (File.Exists(_configPath))
            {
                try
                {
                    string jsonConfig = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<ConfiguracionRed>(jsonConfig);
                    if (config != null && config.UtilizarRutaRed && !string.IsNullOrEmpty(config.RutaServidor) && Directory.Exists(config.RutaServidor))
                    {
                        // Se efectúa la autenticación UNC mediante RedHelper
                        bool autenticado = RedHelper.AutenticarCarpetaRed(config.RutaServidor, config.UsuarioRed, config.ContrasenaRed);
                        if (autenticado && Directory.Exists(config.RutaServidor))
                        {
                            carpetasAProcesar.Add(config.RutaServidor);
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Error al leer la configuración de red. Se usará la ruta local por defecto.");
                }

            }

            // 3. Procesar las carpetas disponibles (local y/o servidor)
            foreach (var carpeta in carpetasAProcesar)
            {
                var archivos = Directory.GetFiles(carpeta, "Inventario_Conciliado_*.json");

                foreach (var archivo in archivos)
                {
                    try
                    {
                        var infoArchivo = new FileInfo(archivo);

                        // Evitar procesar archivos repetidos si existen tanto en local como en red
                        if (lista.Any(x => x.NombreArchivo == infoArchivo.Name)) continue;

                        DateTime fechaCreacion = infoArchivo.CreationTime;

                        if (desde.HasValue && fechaCreacion.Date < desde.Value.Date) continue;
                        if (hasta.HasValue && fechaCreacion.Date > hasta.Value.Date) continue;

                        string contenido = File.ReadAllText(archivo);
                        var datos = JsonSerializer.Deserialize<List<RegistroInventario>>(contenido);

                        if (datos != null && datos.Any())
                        {
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
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al procesar el archivo {archivo}: {ex.Message}");
                    }
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
