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
using System.Windows.Navigation;
using System.Windows.Shapes;
using OperacionTools.Services;

namespace OperacionTools.Interfaz
{
    /// <summary>
    /// Lógica de interacción para HistorialInventariosView.xaml
    /// </summary>
    public partial class HistorialInventariosView : UserControl
    {
        private readonly HistorialService _historialService;
        public HistorialInventariosView()
        {
            InitializeComponent();
            _historialService = new HistorialService();
            CargarTabla();
        }

        private void CargarTabla()
        {
            var items = _historialService.ObtenerHistorial(DpDesde.SelectedDate, DpHasta.SelectedDate);
            GridHistorial.ItemsSource = items;
        }

        private void BtnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            CargarTabla();
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            DpDesde.SelectedDate = null;
            DpHasta.SelectedDate = null;
            CargarTabla();
        }

        private void BtnVerReporte_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button boton && boton.Tag is ItemHistorial itemSeleccionado)
            {
                //Instaciar el visualizador interactivo inyectando directamente los registros cacheados en memoria
                var visor = new VisualizadorReporteWindow(itemSeleccionado.DatosInternos, itemSeleccionado.Bodega)
                {
                    Owner = Window.GetWindow(this) // Establecer la ventana padre para el modal
                };

                visor.ShowDialog();
            }
            else
            {
                MessageBox.Show("Seleccione un registro para ver el reporte.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnTutorial_Click(object sender, RoutedEventArgs e)
        {
            var tutorial = new TutorialModalWindow("historial inventario");
            tutorial.Owner = Window.GetWindow(this);
            tutorial.Show();
        }
    }
}
