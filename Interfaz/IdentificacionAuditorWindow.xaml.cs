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
using OperacionTools.Services;

namespace OperacionTools.Interfaz
{
    /// <summary>
    /// Lógica de interacción para IdentificacionAuditorWindow.xaml
    /// </summary>
    public partial class IdentificacionAuditorWindow : Window
    {
        private readonly ColaboradorService _colaboradorService;
        public string NombreCompleto { get; private set; } = string.Empty;


        public IdentificacionAuditorWindow()
        {
            InitializeComponent();
            _colaboradorService = new ColaboradorService();
            TxtCodigoNomina.Focus();
        }

        // Logica de Autocompletado para el textbox de codigo de nomina
        private void TxtCodigoNomina_TextChanged(object sender, TextChangedEventArgs e)
        {
            string codigo = TxtCodigoNomina.Text.Trim();
            if (!string.IsNullOrEmpty(codigo))
            {
                var colaboradores = _colaboradorService.ObtenerColaboradores();
                if (colaboradores.TryGetValue(codigo, out string? nombre))
                {
                    TxtNombreCompleto.Text = nombre;
                }
            }
        }

        private void TxtCodigoNomina_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(TxtCodigoNomina.Text))
            {
                TxtNombreCompleto.Focus();
                e.Handled = true;
            }
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCodigoNomina.Text))
            {
                MessageBox.Show("El código de nómina es requerido para continuar.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCodigoNomina.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtNombreCompleto.Text))
            {
                MessageBox.Show("Por favor, digite el nombre completo del auditor.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNombreCompleto.Focus();
                return;
            }

            NombreCompleto = TxtNombreCompleto.Text.Trim();

            // Guardar o actualizar la base de datos de manera automatizada
            _colaboradorService.GuardarColaborador(TxtCodigoNomina.Text, NombreCompleto);

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
