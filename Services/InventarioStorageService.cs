using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OperacionTools.Helpers;
using OperacionTools.Models;

namespace OperacionTools.Services
{
    /// <summary>
    /// Define las operaciones de persistencia necesarias para salvaguardar el flujo de inventarios.
    /// </summary>
    public interface IInventarioStorageService
    {
        void GuardarBackupSesion(List<RegistroInventario> datos);
        List<RegistroInventario> CargarBackupSesion();
        void EliminarBackupSesion();
        bool PersistirDatosConciliados(List<RegistroInventario> datos, string configPath);
    }

    /// <summary>
    /// Servicio encargado de la persistencia física en disco local (AppData) y rutas UNC de red corporativas.
    /// </summary>
    internal class InventarioStorageService : IInventarioStorageService
    {
        private readonly string _rutaBackupLocal = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OperacionTools", "inventario_backup.json"
        );

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="InventarioStorageService"/> asegurando directorios base.
        /// </summary>
        public InventarioStorageService()
        {
            string? carpeta = Path.GetDirectoryName(_rutaBackupLocal);
            if (!string.IsNullOrEmpty(carpeta))
            {
                Directory.CreateDirectory(carpeta);
            }
        }

        /// <summary>
        /// Guarda de forma asíncrona un respaldo plano de la sesión en curso.
        /// </summary>
        /// <param name="datos">Lista de lecturas actuales.</param>
        public void GuardarBackupSesion(List<RegistroInventario> datos)
        {
            try
            {
                string json = JsonSerializer.Serialize(datos, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_rutaBackupLocal, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Storage] Error al guardar backup: {ex.Message}");
            }
        }

        /// <summary>
        /// Carga el respaldo local si el programa sufrió un cierre forzoso.
        /// </summary>
        /// <returns>Colección de registros respaldados, o una lista vacía si no existía el archivo.</returns>
        public List<RegistroInventario> CargarBackupSesion()
        {
            if (!File.Exists(_rutaBackupLocal)) return new List<RegistroInventario>();

            try
            {
                string json = File.ReadAllText(_rutaBackupLocal);
                return JsonSerializer.Deserialize<List<RegistroInventario>>(json) ?? new List<RegistroInventario>();
            }
            catch
            {
                return new List<RegistroInventario>();
            }
        }

        /// <summary>
        /// Elimina el archivo físico de respaldo temporal una vez concluida la conciliación.
        /// </summary>
        public void EliminarBackupSesion()
        {
            if (File.Exists(_rutaBackupLocal))
            {
                File.Delete(_rutaBackupLocal);
            }
        }

        /// <summary>
        /// Exporta el JSON final consolidado. Intenta guardarlo en la red UNC compartida configurada; 
        /// si falla o no está disponible, realiza un fallback guardándolo localmente en la carpeta de ejecución.
        /// </summary>
        /// <param name="datos">Lista final de auditoría conciliada.</param>
        /// <param name="configPath">Ruta física del archivo de configuración de red.</param>
        /// <returns><c>true</c> si el guardado en red fue exitoso; <c>false</c> si recurrió al fallback local.</returns>
        public bool PersistirDatosConciliados(List<RegistroInventario> datos, string configPath)
        {
            string nombreArchivo = $"Inventario_Conciliado_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string jsonFinal = JsonSerializer.Serialize(datos, new JsonSerializerOptions { WriteIndented = true });

            // 1. Intento de Almacenamiento Remoto mediante Credenciales de Red
            if (File.Exists(configPath))
            {
                try
                {
                    string jsonConfig = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<ConfiguracionRed>(jsonConfig);

                    if (config != null && config.UtilizarRutaRed && !string.IsNullOrEmpty(config.RutaServidor))
                    {
                        bool autenticado = RedHelper.AutenticarCarpetaRed(config.RutaServidor, config.UsuarioRed, config.ContrasenaRed);
                        if (autenticado)
                        {
                            if (!Directory.Exists(config.RutaServidor)) Directory.CreateDirectory(config.RutaServidor);
                            string rutaDestinoRed = Path.Combine(config.RutaServidor, nombreArchivo);
                            File.WriteAllText(rutaDestinoRed, jsonFinal);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Storage] Falló almacenamiento en red, ejecutando fallback local: {ex.Message}");
                }
            }

            // 2. Contingencia Local (Si el servidor principal está fuera de línea)
            try
            {
                string rutaLocalBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InventariosGuardados");
                if (!Directory.Exists(rutaLocalBase)) Directory.CreateDirectory(rutaLocalBase);

                string rutaDestinoLocal = Path.Combine(rutaLocalBase, nombreArchivo);
                File.WriteAllText(rutaDestinoLocal, jsonFinal);
                return false;
            }
            catch (Exception ex)
            {
                throw new IOException($"No se pudo escribir en el almacenamiento de contingencia local: {ex.Message}", ex);
            }
        }
    }
}