using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ExcelDataReader;
using OperacionTools.Helpers;
using OperacionTools.Models;

namespace OperacionTools.Services
{
    /// <summary>
    /// Servicio orquestador de la lógica de negocio para el control de inventarios de mercancía.
    /// Desacoplado por completo de la interfaz gráfica (WPF).
    /// </summary>
    public class InventarioService
    {
        private readonly IInventarioStorageService _storageService;

        /// <summary>
        /// Obtiene la lista de registros capturados físicamente a través de la pistola de barras.
        /// </summary>
        public List<RegistroInventario> LecturasFisicas { get; private set; } = new();

        /// <summary>
        /// Obtiene la lista de registros cargados desde el archivo maestro de Excel del sistema.
        /// </summary>
        public List<RegistroInventario> SistemaExcel { get; private set; } = new();

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="InventarioService"/>.
        /// </summary>
        /// <param name="storageService">Servicio de persistencia de datos (Local/Red).</param>
        public InventarioService(IInventarioStorageService storageService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        /// <summary>
        /// Recupera los datos de lecturas físicas guardados localmente si la sesión previa falló o se interrumpió.
        /// </summary>
        /// <returns>Una lista de <see cref="RegistroInventario"/> con los datos respaldados.</returns>
        public List<RegistroInventario> CargarBackupSiExiste()
        {
            return _storageService.CargarBackupSesion();
        }

        /// <summary>
        /// Elimina de forma permanente el archivo de respaldo temporal de la sesión actual.
        /// </summary>
        public void EliminarBackup()
        {
            _storageService.EliminarBackupSesion();
        }

        /// <summary>
        /// Procesa, valida y agrega una cadena de texto cruda proveniente de un lector de códigos de barras.
        /// </summary>
        /// <param name="textoCelda">Cadena de caracteres leída (ej. "014000123456").</param>
        /// <returns><c>true</c> si el texto correspondía a una guía válida y fue procesado; de lo contrario, <c>false</c>.</returns>
        public bool ProcesarLecturaFisica(string textoCelda)
        {
            var partes = GuiaHelper.ProcesarLinea(textoCelda);
            if (partes == null) return false;

            var (reg, ser, cons) = partes.Value;

            // Buscar si la guía ya fue escaneada previamente en esta sesión
            var existente = LecturasFisicas.FirstOrDefault(x => x.Reg == reg && x.Serv == ser && x.Consecutivo == cons);

            if (existente != null)
            {
                existente.UnidadesLeidas++;
            }
            else
            {
                LecturasFisicas.Add(new RegistroInventario
                {
                    Reg = reg,
                    Serv = ser,
                    Consecutivo = cons,
                    UnidadesLeidas = 1,
                    UnidadesEsperadas = 0,
                    EstadoConciliacion = "Leído (Pendiente Validación)"
                });
            }

            // Guardado automático preventivo ante pérdidas de energía
            _storageService.GuardarBackupSesion(LecturasFisicas);
            return true;
        }

        /// <summary>
        /// Lee y parsea el archivo de Excel del sistema extrayendo las columnas requeridas para el cruce.
        /// </summary>
        /// <param name="rutaArchivo">Ruta absoluta del archivo .xlsx en el disco.</param>
        /// <returns>La cantidad total de registros válidos encontrados en el archivo.</returns>
        /// <exception cref="IOException">Se genera si el archivo está abierto o bloqueado por otro proceso.</exception>
        public int CargarExcelSistema(string rutaArchivo)
        {
            SistemaExcel.Clear();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(rutaArchivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                });

                var tabla = result.Tables[0];

                foreach (DataRow row in tabla.Rows)
                {
                    if (row["cod_regional"] == DBNull.Value) continue;

                    // Saltar filas vacías o corruptas obligatorias
                    if (row["cod_regional"] == null || row["cod_regional"] == DBNull.Value ||
                        row["cons_guiasu"] == null || row["cons_guiasu"] == DBNull.Value)
                    {
                        continue;
                    }

                    // Normalización de cadenas de texto (Ceros a la izquierda)
                    string reg = row["cod_regional"]?.ToString()?.Trim().PadLeft(2, '0') ?? "00";
                    string serv = row["cod_formapago"]?.ToString()?.Trim() ?? "";
                    string consecutivo = row["cons_guiasu"]?.ToString()?.Trim().PadLeft(9, '0') ?? "000000000";


                    int unidades = 1;
                    if (tabla.Columns.Contains("Unidades") && row["Unidades"] != DBNull.Value)
                    {
                        int.TryParse(row["Unidades"].ToString(), out unidades);
                    }

                    SistemaExcel.Add(new RegistroInventario
                    {
                        Reg = reg,
                        Serv = serv,
                        Consecutivo = consecutivo,
                        UnidadesEsperadas = unidades,
                        UnidadesLeidas = 0,
                        EstadoConciliacion = "No Registrado / Faltante",

                        Novedad = row["Novedad"]?.ToString() ?? "",
                        Saldo = row["Saldo"]?.ToString() ?? "0",
                        Remitente = row["REMITENTE"]?.ToString() ?? "",
                        Estado = row["ESTADO"]?.ToString() ?? "",
                        Rack = row["RACK"]?.ToString() ?? "",
                        CodEntr = row["CodEntr"]?.ToString() ?? "",
                        Bodega = row["BODEGA"]?.ToString() ?? "Malla Documentos"
                    });
                }
            }

            return SistemaExcel.Count;
        }

        /// <summary>
        /// Realiza el algoritmo de conciliación cruzando la información del sistema vs las lecturas de la bodega.
        /// </summary>
        /// <returns>Una lista consolidada con los estados finales de auditoría e inconsistencias.</returns>
        public List<RegistroInventario> Conciliar()
        {
            List<RegistroInventario> consolidadoDefinitivo = new();

            // 1. Evaluar lo que el sistema esperaba recibir contra lo leído físicamente
            foreach (var itemSis in SistemaExcel)
            {
                var leido = LecturasFisicas.FirstOrDefault(x => x.Reg == itemSis.Reg && x.Serv == itemSis.Serv && x.Consecutivo == itemSis.Consecutivo);

                var registro = new RegistroInventario
                {
                    Reg = itemSis.Reg,
                    Serv = itemSis.Serv,
                    Consecutivo = itemSis.Consecutivo,
                    UnidadesEsperadas = itemSis.UnidadesEsperadas,
                    UnidadesLeidas = leido?.UnidadesLeidas ?? 0,

                    Novedad = itemSis.Novedad,
                    Saldo = itemSis.Saldo,
                    Remitente = itemSis.Remitente,
                    Estado = itemSis.Estado,
                    Rack = itemSis.Rack,
                    CodEntr = itemSis.CodEntr,
                    Bodega = itemSis.Bodega
                };

                if (registro.UnidadesLeidas == 0)
                    registro.EstadoConciliacion = "No Registrado / Faltante";
                else if (registro.UnidadesLeidas < registro.UnidadesEsperadas)
                    registro.EstadoConciliacion = "Faltan Unidades";
                else
                    registro.EstadoConciliacion = "✅ OK";

                consolidadoDefinitivo.Add(registro);
            }

            // 2. Identificar Sobrantes Físicos (Cajas escaneadas que NO estaban en el Excel)
            foreach (var itemFis in LecturasFisicas)
            {
                if (!consolidadoDefinitivo.Any(x => x.Reg == itemFis.Reg && x.Serv == itemFis.Serv && x.Consecutivo == itemFis.Consecutivo))
                {
                    var registroSobrante = new RegistroInventario
                    {
                        Reg = itemFis.Reg,
                        Serv = itemFis.Serv,
                        Consecutivo = itemFis.Consecutivo,
                        UnidadesEsperadas = 0,
                        UnidadesLeidas = itemFis.UnidadesLeidas,
                        EstadoConciliacion = "Sobrante Físico",
                        Novedad = "Sobrante - No reportado en Excel",
                        Bodega = "Malla General" // Fallback por defecto
                    };
                    consolidadoDefinitivo.Add(registroSobrante);
                }
            }

            // Intentar persistencia definitiva delegando en la capa de datos
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "networkConfig.json");
            _storageService.PersistirDatosConciliados(consolidadoDefinitivo, configPath);

            return consolidadoDefinitivo;
        }
    }
}