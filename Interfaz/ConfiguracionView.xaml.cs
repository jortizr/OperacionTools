using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using System.Windows.Data;
using System.IO;
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
    /// Lógica de interacción para ConfiguracionView.xaml
    /// </summary>
    public partial class ConfiguracionView : UserControl
    {
        private string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "networkConfig.json");
        public ConfiguracionView()
        {
            InitializeComponent();
            CargarConfiguracion();
        }

        private void CargarConfiguracion()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<ConfiguracionRed>(json);
                    if(config != null)
                    {
                        ChkUtilizarRed.IsChecked = config.UtilizarRutaRed;
                        TxtRutaRed.Text = config.RutaServidor;
                        TxtUsuarioRed.Text = config.UsuarioRed;
                        TxtPasswordRed.Password = config.ContrasenaRed;
                    }
                }
                catch
                {
                    /* Manejar error de lectura silencioso */
                }
            }

        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e) 
        {
            var config = new ConfiguracionRed
            {
                UtilizarRutaRed = ChkUtilizarRed.IsChecked ?? false,
                RutaServidor = TxtRutaRed.Text.Trim(),
                UsuarioRed = TxtUsuarioRed.Text.Trim(),
                ContrasenaRed = TxtPasswordRed.Password
            };

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(configPath, json);
            MessageBox.Show("Configuración guardada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ChkUtilizarRed_Changed(object sender, RoutedEventArgs e) 
        {
            if(PanelCredencialesRed != null)
            {
                PanelCredencialesRed.IsEnabled = ChkUtilizarRed.IsChecked ?? false;
            }
        }

    }
}
