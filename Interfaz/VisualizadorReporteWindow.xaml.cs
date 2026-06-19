using Microsoft.Web.WebView2.Core;
using OperacionTools.Models;
using OperacionTools.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OperacionTools.Interfaz
{
    /// <summary>
    /// Lógica de interacción para VisualizadorReporteWindow.xaml
    /// </summary>
    public partial class VisualizadorReporteWindow : Window
    {
        private readonly InventarioReportService _reportService;

        public VisualizadorReporteWindow(List<RegistroInventario> datos, string bodega)
        {
            InitializeComponent();
            _reportService = new InventarioReportService();
            InicializarYRenderizar(datos,bodega);
        }

        private async void InicializarYRenderizar(List<RegistroInventario> datos, string bodega)
        {
            //esperar a que el motor de Chromium local de WebView2 esté listo
            await WebVisualizador.EnsureCoreWebView2Async();

            //generamos el HTML usando el motor unificado de reportes
            string htmlCompleto = _reportService.ObtenerHtmlReporte(datos, "Visualización rápida desde el historial de auditoría local.", "Consulta de Historial");

            // Navegar directamente usando la cadena en memoria sin guardar archivos físicos
            WebVisualizador.CoreWebView2.NavigateToString(htmlCompleto);
        }

        /// <summary>
        /// Invoca de forma nativa el motor de impresión de Chromium.
        /// </summary>
        private void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WebVisualizador.CoreWebView2 != null)
                {
                    // Abre el diálogo interactivo que integra copias físicas y exportación PDF de Windows
                    WebVisualizador.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);
                }
                else
                {
                    MessageBox.Show("El visor web aún se está inicializando. Por favor espera un segundo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al intentar abrir la interfaz de impresión: {ex.Message}", "Error de Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
