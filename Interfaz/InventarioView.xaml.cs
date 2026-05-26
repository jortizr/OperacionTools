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
using ExcelDataReader;
using PuppeteerSharp;

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

            _listaLecturasFisicas = new List<RegistroInventario>();
            _listaSistemaExcel = new List<RegistroInventario>();
            GridInventario.ItemsSource = _listaLecturasFisicas;

            //Recuperacion de backups ante fallas
            string rutaBackup = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Inventario_Backup_{DateTime.Now:yyyyMMdd}.json");

            if (File.Exists(rutaBackup))
            {
                var resultado = MessageBox.Show(
                    "Detectamos que el programa se cerró de forma inesperada o hubo un corte de energía.\n\n¿Deseas recuperar las guías que ya habías escaneado hasta ese momento?",
                    "Respaldo Encontrado",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    try
                    {
                        string jsonText = File.ReadAllText(rutaBackup);
                        var listaRecuperada = JsonSerializer.Deserialize<List<RegistroInventario>>(jsonText);

                        if (listaRecuperada != null)
                        {
                            _listaLecturasFisicas.AddRange(listaRecuperada);
                            GridInventario.ItemsSource = null;
                            GridInventario.ItemsSource = _listaLecturasFisicas;

                            LblStatus.Text = $"♻️ Se recuperaron {_listaLecturasFisicas.Count} registros de la sesión anterior.";
                            LblStatus.Foreground = Brushes.LightGreen;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"No se pudo procesar el archivo de respaldo: {ex.Message}");
                    }
                }
                else
                {
                    // Si el usuario decide iniciar de cero de forma voluntaria, borramos el archivo fantasma
                    File.Delete(rutaBackup);
                }

            }
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
            if ((e.Key == Key.Enter || e.Key == Key.Tab) && !string.IsNullOrWhiteSpace(TxtConsecutivo.Text))
            {
                // Concatenamos las cajas actuales limpiando espacios
                string reg = TxtReg.Text.Trim();
                string serv = TxtServ.Text.Trim();
                string consecutivo = TxtConsecutivo.Text.Trim();

                ProcesarLecturaFisica(reg + serv + consecutivo);

                // Limpiar y reiniciar ciclo de captura
                TxtReg.Clear();
                TxtServ.Clear();
                TxtConsecutivo.Clear();
                TxtReg.Focus();
                e.Handled = true;
            }
        }
        #endregion


        private void ProcesarLecturaFisica(string textoCelda)
        {
            var partes = GuiaHelper.ProcesarLinea(textoCelda);

            if (partes == null) return;

            var(reg, ser, cons) = partes.Value;

            var existente = _listaLecturasFisicas.FirstOrDefault(x => x.Reg == reg && x.Serv == ser && x.Consecutivo == cons);

            if (existente != null)
            {
                existente.UnidadesLeidas++;
            }
            else
            {
                _listaLecturasFisicas.Add(new RegistroInventario
                {
                    Reg = reg,
                    Serv = ser,
                    Consecutivo = cons,
                    UnidadesLeidas = 1,
                    UnidadesEsperadas = 0,
                    EstadoConciliacion = "Leído (Pendiente Validación)"
                });
            }
            //actualizar la tabla
            GridInventario.ItemsSource = null;
            GridInventario.ItemsSource = _listaLecturasFisicas;

            //Auto-guardado local de respaldo
            try
            {
                string rutaBackup = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Inventario_Backup_{DateTime.Now:yyyyMMdd}.json");
                
                string jsonText = JsonSerializer.Serialize(_listaLecturasFisicas, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(rutaBackup, jsonText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar backup local: {ex.Message}", "Error de Respaldo", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                    //Configucion del encodin para soportar caracteres antiguos
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                    using (var stream = File.Open(openFileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = ExcelReaderFactory.CreateReader(stream))
                        {

                            //conversion del excel a un dataset para poder manipularlo de forma mas sencilla
                            var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                            {
                                ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                            });

                            //seleccion de la primera hoja de Excel
                            var tabla = result.Tables[0];

                            foreach (System.Data.DataRow row in tabla.Rows)
                            {
                                //validacion de filas vacias
                                if (row["cod_regional"] == null || row["cod_regional"] == DBNull.Value ||
                                    row["cons_guiasu"] == null || row["cons_guiasu"] == DBNull.Value)
                                {
                                    continue;
                                }

                                //mapeo y validacion de datos del layout de Sigma
                                string reg = row["cod_regional"]?.ToString()?.Trim().PadLeft(2, '0') ?? "00";
                                string serv = row["cod_formapago"]?.ToString()?.Trim() ?? "";
                                string consecutivo = row["cons_guiasu"]?.ToString()?.Trim().PadLeft(9, '0') ?? "000000000";

                                //validar y parsear las unidades esperadas, si no es un numero se asigna 0
                                int unidades = 0;
                                if (tabla.Columns.Contains("Unidades") && row["Unidades"] != DBNull.Value)
                                {
                                    int.TryParse(row["Unidades"].ToString(), out unidades);
                                }
                                else
                                {
                                    unidades = 1; // Asumimos 1 unidad por defecto si no se especifica, esto puede ajustarse
                                                  // según el caso
                                }

                                //agregar el registro mapeado a la lista del sistema
                                _listaSistemaExcel.Add(new RegistroInventario
                                {
                                    Reg = reg,
                                    Serv = serv,
                                    Consecutivo = consecutivo,
                                    UnidadesEsperadas = unidades,
                                    UnidadesLeidas = 0,
                                    EstadoConciliacion = "No Registrado / Faltante"
                                });

                            }

                        }

                    }

                        LblStatus.Text = "Archivo del sistema cargado de forma exitosa. " +
                        $"\n Se encontraron {_listaSistemaExcel.Count} registros.";
                        LblStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                }
                catch (IOException)
                {
                    MessageBox.Show("El archivo de Excel está siendo utilizado por otro proceso (posiblemente Excel está " +
                        "abierto). Ciérralo e intenta de nuevo.", "Archivo Bloqueado", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error crítico al procesar el Excel: {ex.Message}", "Error de Carga", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
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
                    registro.EstadoConciliacion = "✅OK";         // Todo en Verde

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
            string nombreArchivo = $"Inventario_backup_{DateTime.Now:yyyyMMdd}.json";
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

        private async void BtnExportarPDF_Click(object sender, RoutedEventArgs e)
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
                FileName = $"Informe_Inventario_Malla_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    //notificacion al usuario de que se está generando el reporte
                    LblStatus.Text = "⏳ Preparando motor de renderizado...";
                    LblStatus.Foreground = System.Windows.Media.Brushes.Orange;


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


                    string executablePath = string.Empty;

                    //detectamos el navegador instalado usando la API de PuppeteerSharp para obtener la ruta del ejecutable del navegador, esto es necesario para garantizar compatibilidad y evitar errores de renderizado
                    var installations = new PuppeteerSharp.BrowserFetcher().GetInstalledBrowsers();


                    //buscamos el navegador Chrome, sino se encuentra buscar a Edge
                    var chrome = installations.FirstOrDefault(i => i.Browser == PuppeteerSharp.SupportedBrowser.Chrome);

                    if (chrome != null && !string.IsNullOrEmpty(chrome.GetExecutablePath()) && File.Exists(chrome.GetExecutablePath()))
                    {
                        executablePath = chrome.GetExecutablePath();
                    }
                    else
                    { 
                        //si no hay chrome, buscamos a Microsoft Edge
                        var edge = installations.FirstOrDefault(i => i.Browser == PuppeteerSharp.SupportedBrowser.Chromium);
                        if (edge != null && !string.IsNullOrEmpty(edge.GetExecutablePath()) && File.Exists(edge.GetExecutablePath())) 
                        {
                            executablePath = edge.GetExecutablePath();
                        }
                    }

                    //Estrategia de Respaldo: Si la PC es antigua y no tiene ninguno, descargamos el motor ligero
                    if (string.IsNullOrEmpty(executablePath))
                    {
                        Dispatcher.Invoke(() => {
                            LblStatus.Text = "⏳ No se detectó Chrome. \n Descargando motor de respaldo...";
                            LblStatus.Foreground = System.Windows.Media.Brushes.Orange;
                        });

                        var browserFetcher = new PuppeteerSharp.BrowserFetcher();

                        // Descarga directa simplificada
                        var installed = await browserFetcher.DownloadAsync();
                        executablePath = installed.GetExecutablePath();

                    }

                    //informacion del proceso de generación del PDF al usuario
                    LblStatus.Text = "⏳ Renderizando y escribiendo PDF...";
                    LblStatus.Foreground = System.Windows.Media.Brushes.Yellow;

                    //configurar las opciones de lanzamiento inyectando explicitamente la ruta del ejecutable
                    var launchOptions = new PuppeteerSharp.LaunchOptions
                    {
                        Headless = true,
                        ExecutablePath = executablePath
                    };

                    //lanzar el navegador en segundo plano para renderizar el PDF en un hilo segundario
                    using (var browser = await Task.Run(() => Puppeteer.LaunchAsync(launchOptions)))
                    {
                        using (var page = await browser.NewPageAsync())
                        {
                            //cargar el HTML generado dinámicamente dentro de la pagina virtual
                            await page.SetContentAsync(htmlFinal);

                            //configuracion de opciones de impresion a PDF (Formato A4 y margenes de impresion)
                            var pdfOptions = new PuppeteerSharp.PdfOptions
                            {
                                Format = PuppeteerSharp.Media.PaperFormat.Letter,
                                PrintBackground = true,
                                MarginOptions = new PuppeteerSharp.Media.MarginOptions
                                {
                                    Top = "20mm", Bottom = "20mm", Left = "15mm", Right = "15mm"
                                }
                            };

                            //guardar en disco el PDF generado a partir del HTML renderizado
                            await page.PdfAsync(saveFileDialog.FileName, pdfOptions);
                            
                        }
                    }

                    LblStatus.Text = "✅ PDF generado y exportado exitosamente.";
                    LblStatus.Foreground = System.Windows.Media.Brushes.LightGreen;

                    MessageBox.Show("El informe PDF de auditoría ha sido generado de forma exitosa y está listo para envío por correo.", "Reporte Creado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LblStatus.Text = "❌ Error al exportar el PDF.";
                    LblStatus.Foreground = System.Windows.Media.Brushes.Red;
                    MessageBox.Show($"Ocurrió un error al escribir el archivo: {ex.Message}", "Error de Exportación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
            }

            //borrado de backup temporal de la sesión para evitar confusiones en futuros arranques, ya que el proceso se completó de forma exitosa
            string rutaBackup = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Inventario_backup_{DateTime.Now:yyyyMMdd}.json");
            if (File.Exists(rutaBackup))
            {
                File.Delete(rutaBackup);
            }
        }

    }
}
