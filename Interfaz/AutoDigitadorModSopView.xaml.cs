using OperacionTools.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OperacionTools.Interfaz
{
    /// <summary>
    /// Lógica de interacción para AutoDigitadorModSop.xaml
    /// </summary>
    public partial class AutoDigitadorModSopView : UserControl
    {
        public class GuiaData
        {
            public required string P1 { get; set; }
            public required string P2 { get; set; }
            public required string P3 { get; set; }
        }
        public AutoDigitadorModSopView()
        {
            InitializeComponent();
        }



        private void BtnPegar_Click(object sender, RoutedEventArgs e)
        {

            var lineas = ClipboardHelper.ObtenerLineasDesdePortapapeles();
            if (!lineas.Any()) return;

            //recuperacion de la lista existente para agregar los nuevos item, si esta vacia se crea una nueva
            var listaProcesada = GridGuias.ItemsSource as List<GuiaData> ?? new List<GuiaData>();

            foreach (var linea in lineas)
            {
                var res = GuiaHelper.ProcesarLinea(linea);
                if (res.HasValue)
                {
                    listaProcesada.Add(new GuiaData { 
                        P1 = res.Value.reg, 
                        P2 = res.Value.ser, 
                        P3 = res.Value.cons 
                    });
                }
            }
            GridGuias.ItemsSource = null;
            GridGuias.ItemsSource = listaProcesada;
            
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            GridGuias.ItemsSource = null;
        }

        private async void BtnIniciar_Click(object sender, RoutedEventArgs e)
        {
            var items = GridGuias.ItemsSource as List<GuiaData>;
            if (items == null || items.Count == 0) return;

            int delay = (int)SldDelay.Value;

            // Pequeña notificación visual en lugar de MessageBox para no perder el foco
            BtnIniciar.Content = "PREPARANDO...";
            await Task.Delay(5000);
            BtnIniciar.Content = "DIGITANDO...";

            foreach (var guia in items)
            {
                // P1 + TAB
                await keyboardService.SimularTexto(guia.P1, delay);
                await Task.Delay(delay);
                keyboardService.PresionarTab();
                await Task.Delay(delay);

                // P2 + TAB
                await keyboardService.SimularTexto(guia.P2, delay);
                await Task.Delay(delay);
                keyboardService.PresionarTab();
                await Task.Delay(delay);

                // P3 + ENTER
                await keyboardService.SimularTexto(guia.P3, delay);
                await Task.Delay(delay);
                keyboardService.PresionarEnter();

                await Task.Delay(delay * 2);
            }

            BtnIniciar.Content = "▶ INICIAR DIGITACIÓN";
            // Opcional: Limpiar la lista después de procesar
            GridGuias.ItemsSource = null;
        }

        private void BtnTutorial_Click(object sender, RoutedEventArgs e)
        {
            var tutorial = new TutorialModalWindow("digitador guias");
            tutorial.Owner = Window.GetWindow(this);
            tutorial.Show();
        }
    }
}
