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
    /// Lógica de interacción para TutorialModalWindow.xaml
    /// </summary>
    public partial class TutorialModalWindow : Window
    {
        public class PasoTutorial
        {
            public int Numero { get; set; }
            public string Titulo { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
        }

        public TutorialModalWindow(string moduloKey)
        {
            InitializeComponent();
            CargarPasos(moduloKey);
        }

        private void CargarPasos(string moduloKey)
        {
            var pasos = new List<PasoTutorial>();

            switch (moduloKey.ToLower())
            {
                case "inventario":
                    TxtIcono.Text = "📦";
                    TxtTituloModulo.Text = "Inventario Malla";
                    TxtDescripcionModulo.Text = "Este módulo sirve para cruzar la informacion de las unidades fisicas con el inventario que reposa en la plataforma Sigma(Mallan) por cada bodega, al cruzar la informacion genera un reporte para ser compartido con " +
                        "gerencia y/o auditor. En caso de cerrarse inesperadamente cuenta con recuperación automatica de las guias ingresadas.";
                    pasos.Add(new PasoTutorial { Numero = 1, Titulo = "Paso 1: Ingresar guias", Descripcion = "En los 3 campos, selecciona el primero campo e inicie la lectura de las unidades fiscas mediante su codigo de barras hasta finalizar todo el rack." });
                    pasos.Add(new PasoTutorial { Numero = 2, Titulo = "Paso 2: Agregar los datos de Sigma", Descripcion = "Haz clic en 'Cargar Inventario General' luego se busca el excel descargado de la plataforma Mallan en la opcion 'Informes'->'Reportes'->'Inventario general' y la bodega a la que se va a realizar el inventario. "});
                    pasos.Add(new PasoTutorial { Numero = 3, Titulo = "Paso 3: Conciliar la información", Descripcion = "Selecciona 'Conciliar Datos' para que la aplicacion cruce la informacion del excel con lo ingresado fisicamente, en la lista se vera reflejadas las inconsistencias." });
                    pasos.Add(new PasoTutorial { Numero = 4, Titulo = "Paso 4: Agregar una observación", Descripcion = "Selecciona el campo 'Observaciones' para escribir alguna nota sobre alguna inconsistencia encontrada y/o solución." });
                    pasos.Add(new PasoTutorial { Numero = 4, Titulo = "Paso 4: Generar Reporte", Descripcion = "Seleccionar 'Guardar e Imprimir PDF', automaticamente se genera el reporte en pdf, sin embargo, se descargara los complementos solo la primera vez. Al finalizar se muestra el reporte en el navegador." });
                    break;

                case "configuracion":
                    TxtIcono.Text = "⚙️";
                    TxtTituloModulo.Text = "Configuración de Red";
                    pasos.Add(new PasoTutorial { Numero = 1, Titulo = "Activa el Almacenamiento", Descripcion = "Marca la casilla para habilitar las copias automáticas en el servidor corporativo." });
                    pasos.Add(new PasoTutorial { Numero = 2, Titulo = "Ruta UNC", Descripcion = "Valida que la dirección IP y la carpeta compartida apunten al servidor correcto de Envía." });
                    pasos.Add(new PasoTutorial { Numero = 3, Titulo = "Credenciales", Descripcion = "Digita el usuario de dominio y contraseña asignados para que el software tenga permisos de escritura." });
                    break;

                default:
                    TxtTituloModulo.Text = "Ayuda General";
                    pasos.Add(new PasoTutorial { Numero = 1, Titulo = "Navegación", Descripcion = "Usa el menú del panel izquierdo para alternar entre las diferentes herramientas operativas." });
                    break;
            }

            ListaPasos.ItemsSource = pasos;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
