using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static OperacionTools.Interfaz.GenerarGuiaView;

namespace OperacionTools.Interfaz
{
    /// <summary>
    /// Lógica de interacción para CodigosBarrasWindow.xaml donde se muestran los códigos de barras de las guías generadas.
    /// </summary>
    public partial class CodigosBarrasWindow : Window, INotifyPropertyChanged
    {
        public class ItemCodigoView
        {
            public string? TextoPlano { get; set; }
            public string? TextoCodigo39 { get; set; }
        }

        //lista de fuentes para los códigos de barras, se pueden agregar más fuentes si es necesario
        public List<KeyValuePair<string, string>> FuentesDisponibles { get; set; }

        private string _fuenteSeleccionada;
        public string FuenteSeleccionada
        {
            get => _fuenteSeleccionada;
            set
            {
                _fuenteSeleccionada = value;
                OnPropertyChanged();
            }
        }
        public CodigosBarrasWindow(List<GuiaData> guias)
        {
            InitializeComponent();
            //definicion de las fuentes disponibles
            FuentesDisponibles = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Codigo 39 - Estandar", "pack://application:,,,/resources/#C39HrP24DhTt"),
                new KeyValuePair<string, string>("Codigo 39 - Digital", "pack://application:,,,/resources/#Code39-Digits")
            };

            //seleccionar la primera fuente por defecto
            FuenteSeleccionada = FuentesDisponibles[1].Value;

            //establecer el DataContext para habilitar el enlace de datos
            this.DataContext = this;


            ConfigurarListado(guias);
        }

        private void ConfigurarListado(List<GuiaData> guias)
        {
            var listaFormateada = new List<ItemCodigoView>();

            foreach (var guia in guias)
            {
                string textoPlano = $"{guia.P1}{guia.P2}{guia.P3}";
                string textoCodigo39 = $"*{guia.P1}{guia.P2}{guia.P3}*";

                listaFormateada.Add(new ItemCodigoView
                {
                    TextoPlano = textoPlano,
                    TextoCodigo39 = textoCodigo39
                });

            }
            // Asignar la lista formateada al ItemsSource del ListView
            ItemsCodigos.ItemsSource = listaFormateada;
        }

        private void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(ItemsCodigos, "Impresión de Códigos de Barras");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //notificar cambios en las propiedades para actualizar la interfaz
        public event PropertyChangedEventHandler ?PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }
    }
}
