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
using OperacionTools.Interfaz;

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
    }
}