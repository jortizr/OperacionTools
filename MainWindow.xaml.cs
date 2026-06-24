using OperacionTools.Interfaz;
using OperacionTools.Services;
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

namespace OperacionTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                var regionalService = new RegionalService();
            }
            catch (Exception ex)
            {
                // Si por algún motivo extraño falla, este mensaje te dirá el porqué exacto en pantalla
                MessageBox.Show($"Error al inicializar el archivo de Regionales: {ex.Message}",
                                "Diagnóstico Inicial", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Manejador de eventos que captura el clic en el Hyperlink y abre el navegador web predeterminado.
        /// </summary>
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri)
                {
                    UseShellExecute = true // Requerido en .NET Core / .NET 5+ para lanzar URLs externas
                });

                // Marcamos el evento como manejado para que no cause excepciones internas
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir el perfil de GitHub: {ex.Message}", "Aviso del Sistema", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
           // Permite arrastrar la ventana sin bordes por toda la pantalla
            this.DragMove();
        }

        private void btnDigitarModSop_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Content = new AutoDigitadorModSopView();
        }

        private void BtnGenerarGuia_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Content = new GenerarGuiaView();
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            // Cierra de forma segura todas las ventanas y finaliza el proceso
            Application.Current.Shutdown();
        }

        private void BtnConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Content = new ConfiguracionView();
        }

        private void BtnInventarioMalla_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Content = new InventarioView();
        }

        private void BtnHistorialInventario_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.Content = new HistorialInventariosView();
        }

    }
}