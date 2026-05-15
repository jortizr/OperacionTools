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
    public partial class AutoDigitadorModSopView : System.Windows.Controls.UserControl
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
            string texto = System.Windows.Clipboard.GetText();
            if (string.IsNullOrEmpty(texto)) return;

            var lineas = texto.Split(new[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            var listaProcesada = new List<GuiaData>();

            foreach (var l in lineas)
            {
                var res = GuiaHelper.ProcesarLinea(l);
                if (res.HasValue)
                {
                    listaProcesada.Add(new GuiaData { P1 = res.Value.reg, P2 = res.Value.ser, P3 = res.Value.cons });
                }
            }

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
            await Task.Delay(3000);
            BtnIniciar.Content = "DIGITANDO...";

            foreach (var guia in items)
            {
                // P1 + TAB
                EnviarTexto(guia.P1);
                await Task.Delay(delay);
                System.Windows.Forms.SendKeys.SendWait("{TAB}");
                await Task.Delay(delay);

                // P2 + TAB
                EnviarTexto(guia.P2);
                await Task.Delay(delay);
                System.Windows.Forms.SendKeys.SendWait("{TAB}");
                await Task.Delay(delay);

                // P3 + ENTER
                EnviarTexto(guia.P3);
                await Task.Delay(delay);
                System.Windows.Forms.SendKeys.SendWait("{ENTER}");

                await Task.Delay(delay * 2);
            }

            BtnIniciar.Content = "▶ INICIAR DIGITACIÓN";
            System.Windows.MessageBox.Show("Completado");
        }

        private void EnviarTexto(string texto)
        {
            // SendWait es la forma más compatible en .NET moderno para enviar 
            // pulsaciones de teclas a procesos externos.
            System.Windows.Forms.SendKeys.SendWait(texto);
        }

    }
}
