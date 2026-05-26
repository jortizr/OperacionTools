using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OperacionTools.Helpers;
using static OperacionTools.Interfaz.InventarioView;
using System.Text;

namespace OperacionTools.Services
{
    /// <summary>
    /// Interfaz para el servicio de almacenamiento de inventarios.
    /// Permite el intercambio fácil de implementaciones (ej. bases de datos en el futuro).
    /// </summary>
    /// 

    public interface IInventarioStorageService
    {
        void GuardarBackupSesion(List<RegistroInventario> datos);
        List<RegistroInventario> CargarBackupSesion();
        void EliminarBackupSesion();
        bool PersistirDatosConciliados(List<RegistroInventario> datos, string configPath);
    }

    /// <summary>
    /// Servicio encargado de la persistencia en disco local y rutas de red UNC de la sesión del inventario.
    /// </summary>
    internal class InventarioStorageService : IInventarioStorageService
    {
        private readonly string _rutaBackupLocal = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OperacionTools", "inventario_backup.json"
        );

        public InventarioStorageService()
        {
            string carpeta = Path.GetDirectoryName(_rutaBackupLocal);
            if (!Directory.Exists(carpeta)) {
                Directory.CreateDirectory(carpeta);
            }
        }

        /// <summary>
        /// Guarda el estado actual de las lecturas físicas en un archivo JSON local de contingencia.
        /// </summary>
        /// 
        public void GuardarBackupSesion(List<RegistroInventario> datos) {
            try
            {
                string jsonText = JsonSerializer.Serialize(datos, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_rutaBackupLocal, jsonText);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Storage] Error al guardar backup en segundo plano: {ex.Message}");
            }
        }

        /// <summary>
        /// Recupera el estado de una sesión de inventario previa que no haya sido conciliada.
        /// </summary>
        public List<RegistroInventario> CargarBackupSesion()
        {
            if(!File.Exists(_rutaBackupLocal)) return new List<RegistroInventario>();

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
        /// Elimina el archivo de contingencia local una vez que el proceso se ha completado exitosamente.
        /// </summary>
        public void EliminarBackupSesion()
        {
            if (File.Exists(_rutaBackupLocal))
            {
                File.Delete(_rutaBackupLocal);
            }
        }

        /// <summary>
        /// Persiste la información consolidada final determinando si se guarda de forma local o remota según la configuración.
        /// </summary>
        public bool PersistirDatosConciliados(List<RegistroInventario> datos, string configPath)
        {
            string jsonFinal = JsonSerializer.Serialize(datos, new JsonSerializerOptions { WriteIndented = true });
            string nombreArchivo = $"Inventario_Consolidado_{DateTime.Now:yyyyMMdd_HHmmss}.json";

            ConfiguracionRed config = null;
            if (File.Exists(configPath))
            {
                try
                {
                    string jsonConfig = File.ReadAllText(configPath);
                    config = JsonSerializer.Deserialize<ConfiguracionRed>(jsonConfig);
                }
                catch { /* Ignorar error de carga de config y proceder a fallback */ }
            }

            // Validar si se requiere guardar en red externa o local
            if (config != null && config.UtilizarRutaRed && !string.IsNullOrWhiteSpace(config.RutaServidor))
            {
                try
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Storage] Falló almacenamiento en red, ejecutando fallback local: {ex.Message}");
                }
            }

            // Fallback: Almacenamiento Local de Datos Formales
            try
            {
                string rutaLocalBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InventariosGuardados");
                if (!Directory.Exists(rutaLocalBase)) Directory.CreateDirectory(rutaLocalBase);

                string rutaDestinoLocal = Path.Combine(rutaLocalBase, nombreArchivo);
                File.WriteAllText(rutaDestinoLocal, jsonFinal);
                return true;
            }
            catch (Exception ex)
            {
                throw new IOException($"No se pudo escribir en el almacenamiento de contingencia local: {ex.Message}", ex);
            }
        }
    }
}
