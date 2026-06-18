using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OperacionTools.Helpers;

namespace OperacionTools.Services
{
    public class ColaboradorService
    {
        private readonly string _rutaLocal;
        private readonly string _configPath;

        public ColaboradorService() 
        {
            //apunta a la carpeta de documentos del usuario AppData\Roaming\OperacionTools
            _rutaLocal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "OperacionTools",
                "colaboradores.json"
                );

            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "networkConfig.json"
                );

            AsegurarDirectorioLocal();
        }

        private void AsegurarDirectorioLocal()
        {
            string? carpeta = Path.GetDirectoryName(_rutaLocal);
            if(!string.IsNullOrEmpty(carpeta) && !Directory.Exists(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }
        }

        /// <summary>
        /// Lee y combina los colaboradores locales y de red (si aplica) para garantizar la sincronización.
        /// </summary>
        /// 
        public Dictionary<string, string> ObtenerColaboradores()
        {
            var colaboradores = new Dictionary<string, string>();

            // Cargar los colaboradores locales del almacenamiento de contingencia
            if (File.Exists(_rutaLocal))
            {
                try
                {
                    string jsonLocal = File.ReadAllText(_rutaLocal);

                    var localDict = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonLocal);
                    if (localDict != null) colaboradores = localDict;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al leer colaboradores locales: {ex.Message}");
                }
            }

            // sincronizar con la ruta central UNC si la configuración remota esta habilitada
            if (File.Exists(_configPath))
            {
                try
                {
                    string jsonConfig = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<ConfiguracionRed>(jsonConfig);

                    if (config != null && config.UtilizarRutaRed && !string.IsNullOrEmpty(config.RutaServidor))
                    {
                        string rutaRedColab = Path.Combine(config.RutaServidor, "colaboradores.json");
                        if(File.Exists(rutaRedColab))
                        {
                            string jsonRed = File.ReadAllText(rutaRedColab);
                            var redDict = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonRed);

                            if (redDict != null)
                            {
                                foreach (var kvp in redDict)
                                {
                                    colaboradores[kvp.Key] = kvp.Value; // Sobrescribe o agrega colaboradores de la red
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al leer configuración de red: {ex.Message}");
                }
            }
            return colaboradores;
        }

        /// <summary>
        /// Registra o actualiza un auditor indexado por su código de nómina.
        /// </summary>
        /// 
        public void GuardarColaborador(string codigo, string nombre)
        {
            if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre)) return;

            var colaboradores = ObtenerColaboradores();
            colaboradores[codigo.Trim()] = nombre.Trim();

            string jsonFinal = JsonSerializer.Serialize(colaboradores, new JsonSerializerOptions { WriteIndented = true });

            // Guardar en la ruta local de contingencia
            try
            {
                File.WriteAllText(_rutaLocal, jsonFinal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar colaboradores localmente: {ex.Message}");
            }

            //guardar en el servidor si la configuración remota esta habilitada
            if (File.Exists(_configPath))
            {
                try
                {
                    string jsonConfig = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<ConfiguracionRed>(jsonConfig);
                    if (config != null && config.UtilizarRutaRed && !string.IsNullOrEmpty(config.RutaServidor))
                    {
                        string rutaRedColab = Path.Combine(config.RutaServidor, "colaboradores.json");
                        File.WriteAllText(rutaRedColab, jsonFinal);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al guardar colaboradores en red: {ex.Message}");

                }
            }
        }
    }
}
