using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using OperacionTools.Models;
using OperacionTools.Services;

namespace OperacionTools.Interfaz
{
    /// <summary>
    /// Lógica de interacción y comportamiento visual para <see cref="InventarioView"/>.
    /// Maneja capturas de foco físico de periféricos y eventos de ventanas de diálogo.
    /// </summary>
    public partial class InventarioView : UserControl
    {
        private readonly InventarioService _inventarioService;
        private readonly InventarioReportService _reportService;

        /// <summary>
        /// Inicializa los componentes visuales y conecta las dependencias de los servicios.
        /// </summary>
        public InventarioView()
        {
            InitializeComponent();
            TxtReg.Focus();

            // Inyección manual de dependencias de infraestructura
            var storageService = new InventarioStorageService();
            _inventarioService = new InventarioService(storageService);
            _reportService = new InventarioReportService();

            GridInventario.ItemsSource = _inventarioService.LecturasFisicas;

            ProcesarRecuperacionBackup();
        }

        /// <summary>
        /// Evalúa la existencia de una sesión previa colgada y pregunta al usuario si desea restaurarla.
        /// </summary>
        private void ProcesarRecuperacionBackup()
        {
            var listaRecuperada = _inventarioService.CargarBackupSiExiste();

            if (listaRecuperada != null && listaRecuperada.Any())
            {
                var resultado = MessageBox.Show(
                    "Detectamos que el programa se cerró de forma inesperada o hubo un corte de energía.\n\n¿Deseas recuperar las guías que ya habías escaneado hasta ese momento?",
                    "Respaldo Encontrado",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    _inventarioService.LecturasFisicas.AddRange(listaRecuperada);
                    ActualizarGrilla(_inventarioService.LecturasFisicas);

                    LblStatus.Text = $"♻️ Se recuperaron {listaRecuperada.Count} registros de la sesión anterior.";
                    LblStatus.Foreground = Brushes.LightGreen;
                }
                else
                {
                    _inventarioService.EliminarBackup();
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

        /// <summary>
        /// Captura el disparo final de la pistola, procesa la guía completa y reinicia los focos de captura.
        /// </summary>
        private void TxtConsecutivo_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter || e.Key == Key.Tab) && !string.IsNullOrWhiteSpace(TxtConsecutivo.Text))
            {
                string reg = TxtReg.Text.Trim().PadLeft(2, '0');
                string serv = TxtServ.Text.Trim().PadLeft(1, '0');
                string consecutivo = TxtConsecutivo.Text.Trim().PadLeft(9, '0');

                // Envía la concatenación limpia de datos al motor lógico
                if (_inventarioService.ProcesarLecturaFisica(reg + serv + consecutivo))
                {
                    ActualizarGrilla(_inventarioService.LecturasFisicas);
                }

                // Reset de cajas de texto visuales
                TxtReg.Clear();
                TxtServ.Clear();
                TxtConsecutivo.Clear();
                TxtReg.Focus();
                e.Handled = true;
            }
        }

        #endregion

        /// <summary>
        /// Evento gatillo para seleccionar y cargar el archivo de Excel del sistema al Grid.
        /// </summary>
        private void BtnCargarExcel_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Archivos de Excel (*.xlsx)|*.xlsx" };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    int total = _inventarioService.CargarExcelSistema(openFileDialog.FileName);

                    LblStatus.Text = $"Archivo del sistema cargado de forma exitosa.\nSe encontraron {total} registros.";
                    LblStatus.Foreground = Brushes.LightGreen;
                }
                catch (System.IO.IOException)
                {
                    MessageBox.Show("El archivo de Excel está siendo utilizado por otro proceso (ej. Microsoft Excel). Ciérralo e intenta de nuevo.", "Archivo Bloqueado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error crítico al procesar el Excel: {ex.Message}", "Error de Carga", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Ejecuta el cruce de datos final y refresca los indicadores visuales informando el resultado.
        /// </summary>
        private void BtnConciliar_Click(object sender, RoutedEventArgs e)
        {
            if (!_inventarioService.SistemaExcel.Any())
            {
                MessageBox.Show("Por favor, cargue el reporte de Excel del sistema primero.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var consolidado = _inventarioService.Conciliar();
            ActualizarGrilla(consolidado);

            int okCount = consolidado.Count(x => x.EstadoConciliacion == "✅OK");
            LblStatus.Text = $"Conciliación finalizada. {okCount} registros Ok de {consolidado.Count} evaluados.";
        }

        /// <summary>
        /// Genera de forma asíncrona mediante un motor headless el informe PDF corporativo de auditoría.
        /// </summary>
        private async void BtnExportarPDF_Click(object sender, RoutedEventArgs e)
        {

            var datosReporte = GridInventario.ItemsSource as List<RegistroInventario>;
            if (datosReporte == null || !datosReporte.Any())
            {
                MessageBox.Show("No hay datos conciliados disponibles en la tabla para exportar.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            string observacionesUsuario = TxtObservaciones.Text.Trim();

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Documento PDF (*.pdf)|*.pdf",
                FileName = $"Informe_Auditoria_{DateTime.Now:yyyyMMdd}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    LblStatus.Text = "⏳ Generando reporte PDF con Chrome, Espera por favor...";
                    LblStatus.Foreground = Brushes.Orange;


                    // Definimos el reportero de progreso
                    var progreso = new Progress<double>(porcentaje =>
                    {
                        LblStatus.Text = $"⏳ Descargando componentes necesarios: {porcentaje:F1}%";

                        // Opcional: Si pones un <ProgressBar x:Name="MiProgressBar"/> en tu XAML puedes hacer:
                        ProgBarStatus.Visibility = Visibility.Visible;
                        ProgBarStatus.Value = porcentaje;
                    });

                    // Invocar el motor de renderizado asíncrono
                    await _reportService.GenerarReporteAuditoriaAsync(datosReporte, saveFileDialog.FileName, 
                        observacionesUsuario, progreso, 
                        (mensaje) =>{
                        LblStatus.Text = mensaje;}
                    );

                    LblStatus.Text = "✅ PDF generado y exportado exitosamente.";
                    LblStatus.Foreground = Brushes.LightGreen;

                    ProgBarStatus.Value = 0;
                    ProgBarStatus.Visibility = Visibility.Collapsed;

                    MessageBox.Show("El informe PDF de auditoría ha sido generado de forma exitosa y está listo.", "Reporte Creado", MessageBoxButton.OK, MessageBoxImage.Information);


                    //abrir automáticamente el PDF generado con el programa predeterminado del sistema operativo
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(saveFileDialog.FileName)
                        {
                            UseShellExecute = true // Requerido en .NET Core / .NET 5+ para abrir archivos con el programa nativo de Windows
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"No se pudo abrir el PDF automáticamente: {ex.Message}");
                    }

                    // Finalizado con éxito, limpiamos backups obsoletos de memoria física
                    _inventarioService.EliminarBackup();
                }
                catch (Exception ex)
                {
                    LblStatus.Text = "❌ Error al exportar el PDF.";
                    LblStatus.Foreground = Brushes.Red;
                    MessageBox.Show($"Ocurrió un error al escribir el archivo: {ex.Message}", "Error de Exportación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Fuerza el refresco físico de los punteros de datos del componente DataGrid de WPF.
        /// </summary>
        /// <param name="datos">Lista actualizada de registros a emparejar.</param>
        private void ActualizarGrilla(List<RegistroInventario> datos)
        {
            GridInventario.ItemsSource = null;
            GridInventario.ItemsSource = datos;
        }
    }
}