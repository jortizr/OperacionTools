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
using OperacionTools.Helpers;

namespace OperacionTools.Interfaz
{
    /// <summary>
    /// Lógica de interacción para GenerarGuiaView.xaml
    /// </summary>
    public partial class GenerarGuiaView : UserControl
    {
        public class GuiaData
        {
            public required string P1 { get; set; }
            public required string P2 { get; set; }
            public required string P3 { get; set; }
        }
        public GenerarGuiaView()
        {
            InitializeComponent();
        }

        private void BtnGenerarCodBar_Click(object sender, RoutedEventArgs e)
        {
            // Obtener la lista de guías desde el DataGrid
            var items = GridGuiasCodBar.ItemsSource as List<GuiaData>;

            if(items == null || items.Count == 0)
            { 
                MessageBox.Show("No hay guías para generar códigos de barra.", "Lista Vacia", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //instanciar la ventana de generación de códigos de barra y pasarle la lista de guías
            CodigosBarrasWindow ventanaCodigos = new CodigosBarrasWindow(items);

            //establecer la ventana principal como propietario de la ventana de códigos de barra
            ventanaCodigos.Owner = Window.GetWindow(this);

            //mostrar la ventana de códigos de barra
            ventanaCodigos.ShowDialog();
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            GridGuiasCodBar.ItemsSource = null;
        }

        private void BtnPegar_Click(object sender, RoutedEventArgs e)
        {
            var lineas = ClipboardHelper.ObtenerLineasDesdePortapapeles();
            if (!lineas.Any()) return;

            var ListaGuias = GridGuiasCodBar.ItemsSource as List<GuiaData> ?? new List<GuiaData>();

            foreach (var linea in lineas)
            { 
                var res = GuiaHelper.ProcesarLinea(linea);
                if (res.HasValue)
                {
                    ListaGuias.Add( new GuiaData {
                        P1 = res.Value.reg,
                        P2 = res.Value.ser,
                        P3 = res.Value.cons,
                    });
                }
            }

            GridGuiasCodBar.ItemsSource = null;
            GridGuiasCodBar.ItemsSource = ListaGuias;
        }
    }
}
