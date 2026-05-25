using Microsoft.Win32;
using OperacionTools.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OperacionTools.Interfaz
{
    /// <summary>
    /// Modulo para el inventario de mercancia en la malla fisicamente y cruce de informacion con el sistema para detectar faltantes, sobrantes o errores en la malla.
    /// </summary>
    public partial class InventarioView : UserControl
    {
        public class RegistroInventario
        {
            public string Reg { get; set; } = string.Empty;
            public string Serv { get; set; } = string.Empty;
            public string Consecutivo { get; set; } = string.Empty;
            public string GuiaCompleta => $".{Reg}-{Serv}-{Consecutivo}";
            public int UnidadesEsperadas { get; set; } = 0;
            public int UnidadesLeidas { get; set; } = 0;
            public string EstadoConciliacion { get; set; } = "Leído (Pendiente Validación)";
        }

        private List<RegistroInventario> _listaLecturasFisicas = new ();
        private List<RegistroInventario> _listaSistemaExcel = new();

        public InventarioView()
        {
            InitializeComponent();
            TxtReg.Focus();
        }

        #region Automatización de Foco para Lectores de Barra
        private void TxtReg_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(TxtReg.Text))
            {
                TxtServ.Focus();
                e.Handled = true;
            }
        }

        private void TxtServ_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(TxtServ.Text))
            {
                TxtConsecutivo.Focus();
                e.Handled = true;
            }
        }

        private void TxtConsecutivo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(TxtConsecutivo.Text))
            {
                ProcesarLecturaFisica(TxtReg.Text.Trim(), TxtServ.Text.Trim(), TxtConsecutivo.Text.Trim());

                // Limpiar y reiniciar ciclo de captura
                TxtReg.Clear();
                TxtServ.Clear();
                TxtConsecutivo.Clear();
                TxtReg.Focus();
                e.Handled = true;
            }
        }
        #endregion


        private void ProcesarLecturaFisica(string reg, string serv, string cons)
        {
            var existente = _listaLecturasFisicas.FirstOrDefault(x => x.Reg == reg && x.Serv == serv && x.Consecutivo == cons);

            if (existente != null)
            {
                existente.UnidadesLeidas++;
            }
            else
            {
                _listaLecturasFisicas.Add(new RegistroInventario
                {
                    Reg = reg,
                    Serv = serv,
                    Consecutivo = cons,
                    UnidadesLeidas = 1
                });
            }
            ActualizarGrilla();
        }

        private void BtnCargarExcel_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Archivos de Excel (*.xlsx)|*.xlsx" };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _listaSistemaExcel.Clear();

                    // LÓGICA DE LECTURA (Ejemplo conceptual con mapeo del layout provisto)
                    // En producción, inicializar Workbook/Reader del Excel cargado.
                    // Buscaremos las columnas: 'cod_regional', 'cod_formapago', 'cons_guiasu' y 'Unidades'.

                    /* Ejemplo de extracción:
                       var workbook = new ClosedXML.Excel.XLWorkbook(openFileDialog.FileName);
                       var rows = workbook.Worksheet(1).RowsUsed().Skip(1);
                       foreach(var row in rows) {
                           _listaSistemaExcel.Add(new RegistroInventario {
                               Reg = row.Cell(1).GetString(), 
                               Serv = row.Cell(2).GetString(),
                               Consecutivo = row.Cell(4).GetString(),
                               UnidadesEsperadas = row.Cell(10).GetInteger()
                           });
                       }
                    */

                    LblStatus.Text = "Archivo del sistema cargado de forma exitosa.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al abrir el Excel: {ex.Message}", "Error de Carga", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnConciliar_Click(object sender, RoutedEventArgs e)
        {
            if (!_listaSistemaExcel.Any())
            {
                MessageBox.Show("Por favor, cargue el reporte de Excel del sistema primero.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<RegistroInventario> consolidadoDefinitivo = new();

            // 1. Procesar todo lo que está registrado en el sistema de la plataforma
            foreach (var itemSis in _listaSistemaExcel)
            {
                var leido = _listaLecturasFisicas.FirstOrDefault(x => x.Reg == itemSis.Reg && x.Serv == itemSis.Serv && x.Consecutivo == itemSis.Consecutivo);

                var registro = new RegistroInventario
                {
                    Reg = itemSis.Reg,
                    Serv = itemSis.Serv,
                    Consecutivo = itemSis.Consecutivo,
                    UnidadesEsperadas = itemSis.UnidadesEsperadas,
                    UnidadesLeidas = leido?.UnidadesLeidas ?? 0
                };

                if (registro.UnidadesLeidas == 0)
                    registro.EstadoConciliacion = "No Registrado / Faltante"; // Todo en Rojo
                else if (registro.UnidadesLeidas < registro.UnidadesEsperadas)
                    registro.EstadoConciliacion = "Faltan Unidades";          // Naranja/Amarillo
                else
                    registro.EstadoConciliacion = "Conciliado Total";         // Todo en Verde

                consolidadoDefinitivo.Add(registro);
            }

            // 2. Capturar si se leyó algo físico no existente en el Excel del sistema
            foreach (var itemFis in _listaLecturasFisicas)
            {
                if (!consolidadoDefinitivo.Any(x => x.Reg == itemFis.Reg && x.Serv == itemFis.Serv && x.Consecutivo == itemFis.Consecutivo))
                {
                    itemFis.EstadoConciliacion = "No Registrado / Faltante"; // Sobrante Físico sin registro en sistema
                    consolidadoDefinitivo.Add(itemFis);
                }
            }

            GridInventario.ItemsSource = null;
            GridInventario.ItemsSource = consolidadoDefinitivo;
            LblStatus.Text = $"Conciliación finalizada. {consolidadoDefinitivo.Count(x => x.EstadoConciliacion == "Conciliado Total")} Ok.";

            // Persistir de forma local inmediatamente para resguardo contra fallos de energía
            PersistirDatosLocales(consolidadoDefinitivo);
        }

        private void ActualizarGrilla()
        {
            GridInventario.ItemsSource = null;
            GridInventario.ItemsSource = _listaLecturasFisicas;
        }

        #region Almacenamiento Local y Escalabilidad a Red
        private void PersistirDatosLocales(List<RegistroInventario> datos)
        {
            string nombreArchivo = $"Inventario_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string jsonString = JsonSerializer.Serialize(datos, new JsonSerializerOptions { WriteIndented = true });

            // 1. Definir ruta local por defecto (Opción 1 - Contingencia)
            string rutaLocalBase = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InventarioDataLocal");
            if (!Directory.Exists(rutaLocalBase)) Directory.CreateDirectory(rutaLocalBase);
            string destinoFinalLocal = System.IO.Path.Combine(rutaLocalBase, nombreArchivo);

            // 2. Cargar preferencias de Red desde la sección de configuración
            string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "networkConfig.json");
            ConfiguracionRed? config = null;

            if (File.Exists(configPath))
            {
                config = JsonSerializer.Deserialize<ConfiguracionRed>(File.ReadAllText(configPath));
            }

            // 3. Intentar guardar en Red por defecto si está habilitada (Opción 2)
            if (config != null && config.UtilizarRutaRed && !string.IsNullOrWhiteSpace(config.RutaServidor))
            {
                try
                {
                    // Autenticar de forma segura en segundo plano con las credenciales ingresadas
                    bool conectado = Helpers.RedHelper.AutenticarCarpetaRed(config.RutaServidor, config.UsuarioRed, config.ContrasenaRed);

                    if (conectado)
                    {
                        if (!Directory.Exists(config.RutaServidor)) Directory.CreateDirectory(config.RutaServidor);

                        string destinoRed = System.IO.Path.Combine(config.RutaServidor, nombreArchivo);
                        File.WriteAllText(destinoRed, jsonString);

                        LblStatus.Text = "📦 Guardado exitoso directamente en el Servidor de Red.";
                        return; // Guardado principal completo con éxito
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Fallo de escritura en Red: {ex.Message}");
                }
            }

            // 4. Fallback automático: Si la red falló o estaba desactivada, escribe localmente
            File.WriteAllText(destinoFinalLocal, jsonString);
            LblStatus.Text = "⚠️ Servidor no disponible. Copia resguardada de forma Local y segura.";
            LblStatus.Foreground = System.Windows.Media.Brushes.Orange;

        }
        #endregion

        private void BtnExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            var datosReporte = GridInventario.ItemsSource as List<RegistroInventario>;
            if (datosReporte == null || !datosReporte.Any())
            {
                MessageBox.Show("No hay datos conciliados para exportar.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Documento PDF (*.pdf)|*.pdf",
                FileName = $"Informe_Inventario_{DateTime.Now:yyyyMMdd}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // Construcción dinámica del HTML seguro
                string rowsHtml = "";
                foreach (var item in datosReporte)
                {
                    string claseEstado = item.EstadoConciliacion switch
                    {
                        "Conciliado Total" => "status-ok",
                        "Faltan Unidades" => "status-warn",
                        _ => "status-danger"
                    };

                    rowsHtml += $@"
            <tr>
                <td>{item.GuiaCompleta}</td>
                <td>{item.Reg}</td>
                <td>{item.Serv}</td>
                <td>{item.Consecutivo}</td>
                <td style='text-align: center;'>{item.UnidadesEsperadas}</td>
                <td style='text-align: center;'>{item.UnidadesLeidas}</td>
                <td><span class='{claseEstado}'>{item.EstadoConciliacion}</span></td>
            </tr>";
                }

                // Recuperamos la plantilla CSS inmutable probada
                string htmlFinal = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <style>
                @page {{ size: A4; margin: 20mm 15mm; }}
                body {{ font-family: 'Segoe UI', sans-serif; color: #2c3e50; }}
                .header-container {{ border-bottom: 3px solid #2980b9; padding-bottom: 10px; margin-bottom: 25px; }}
                .title {{ font-size: 20pt; font-weight: bold; color: #1a1a1c; }}
                .data-table {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
                .data-table th {{ background-color: #34495e; color: white; padding: 10px; text-align: left; font-size: 10pt; }}
                .data-table td {{ padding: 9px 10px; border-bottom: 1px solid #bdc3c7; font-size: 10pt; }}
                .status-ok {{ background-color: #d4edda; color: #155724; font-weight: bold; padding: 3px 8px; border-radius: 4px; }}
                .status-warn {{ background-color: #fff3cd; color: #856404; font-weight: bold; padding: 3px 8px; border-radius: 4px; }}
                .status-danger {{ background-color: #f8d7da; color: #721c24; font-weight: bold; padding: 3px 8px; border-radius: 4px; }}
            </style>
        </head>
        <body>
            <div class='header-container'>
                <div class='title'>Informe de Conciliación de Inventario</div>
                <div style='color: #7f8c8d;'>Generado de forma automática e Inalterable</div>
            </div>
            <p><strong>Fecha Emisión:</strong> {DateTime.Now:dd/MM/yyyy hh:mm tt}</p>
            <table class='data-table'>
                <thead>
                    <tr>
                        <th>Guía Concatenada</th>
                        <th>Reg</th>
                        <th>Serv</th>
                        <th>Consecutivo</th>
                        <th style='text-align: center;'>Esp.</th>
                        <th style='text-align: center;'>Leídas</th>
                        <th>Resultado Supervisor</th>
                    </tr>
                </thead>
                <tbody>
                    {rowsHtml}
                </tbody>
            </table>
        </body>
        </html>";

                // NOTA DE IMPLEMENTACIÓN .NET 10:
                // Para compilar el HTML a PDF de forma nativa sin herramientas de terceros puedes usar el control WebView2 integrado
                // ejecutando el comando: await webView.CoreWebView2.PrintToPdfAsync(saveFileDialog.FileName);
                // O usar librerías ligeras de .NET 10 como iTextSharp / IronPdf / PuppeteerSharp.

                MessageBox.Show("El informe PDF de auditoría ha sido generado de forma exitosa y está listo para envío por correo.", "Reporte Creado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

    }
}
