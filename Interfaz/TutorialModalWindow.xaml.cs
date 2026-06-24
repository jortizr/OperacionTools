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
                    pasos.Add(new PasoTutorial { Numero = 4, Titulo = "Paso 5: Generar Reporte", Descripcion = "Seleccionar 'Guardar e Imprimir PDF', automaticamente se genera el reporte en pdf, sin embargo, se descargara los complementos solo la primera vez. Al finalizar se muestra el reporte en el navegador." });
                    break;

                case "configuracion":
                    TxtIcono.Text = "⚙️";
                    TxtTituloModulo.Text = "Configuración de Red";
                    TxtDescripcionModulo.Text = "Este módulo sirve para configurar una carpeta compartida centralizada, donde se almacenaran los registros de los inventarios para consultas posteriores y almacenamiento de autoguardado. Por defecto se almacena en el disco duro.";
                    pasos.Add(new PasoTutorial { Numero = 1, Titulo = "Paso 1: Activar el Almacenamiento", Descripcion = "Marca la casilla para habilitar las copias automáticas en el servidor corporativo LAN." });
                    pasos.Add(new PasoTutorial { Numero = 2, Titulo = "Paso 2: Agregar Ruta UNC", Descripcion = "Valida que la dirección IP y la carpeta compartida apunten al servidor correcto de Envía y/o PC dedicado." });
                    pasos.Add(new PasoTutorial { Numero = 3, Titulo = "Paso 3: Agregar Credenciales", Descripcion = "Digita el usuario de dominio y contraseña del equipo donde se creo la carpeta compartida." });
                    break;

                case "generador codigo barras":
                    TxtIcono.Text = "🧾";
                    TxtTituloModulo.Text = "Generador Cod. Barras";
                    TxtDescripcionModulo.Text = "Este módulo sirve para generar los codigos de barras en base a las guias copiadas en formato tabulacion o la guia unida. Util para leer desde la pantalla los codigos de barras.";
                    pasos.Add(new PasoTutorial { Numero = 1, Titulo = "Paso 1: Copiado de guias", Descripcion = "Copia las guias y pegalas con el boton 'Pegar Portapapeles' ya sea en formato (1-1-1) o (012345678901)." });
                    pasos.Add(new PasoTutorial { Numero = 2, Titulo = "Paso 2: Generar Cod. de barras", Descripcion = "Presiona el boton 'Generar Cod. Barras' el cual genera la lista con las guias copiadas en una ventana segundaria." });
                    pasos.Add(new PasoTutorial { Numero = 3, Titulo = "Paso 3: Imprimir Cod. Barras", Descripcion = "Presiona el boton 'Imprimir listado' donde podra visualizar la ventana de impresion donde selecciona la impresora a usaar." });
                    break;

                case "digitador guias":
                    TxtIcono.Text = "⚙️";
                    TxtTituloModulo.Text = "Digitador de guias";
                    TxtDescripcionModulo.Text = "Este módulo sirve para transcribir un listado de guias a una aplicacion externa que tenga 3 campos donde se ingrese este informacion. Util para replanillar guias de contratistas y/o contingencia para planillar desde una lista de excel.";
                    pasos.Add(new PasoTutorial { Numero = 1, Titulo = "Paso 1: Copiar guias", Descripcion = "Copiar el listado de guias y presionar el boton 'Pegar Portapapeles' se almacenaran las guias en un listado, ya formateadas, en caso de inconsistente seleccionar y presionar 'supr' desde el teclado." });
                    pasos.Add(new PasoTutorial { Numero = 2, Titulo = "Paso 2: Ajustar tiempo", Descripcion = "Selecciona la barra de tiempo en milisegundos para ajustar la velocidad de digitación." });
                    pasos.Add(new PasoTutorial { Numero = 3, Titulo = "Paso 3: Ejecutar digitación", Descripcion = "Al presionar el boton 'Iniciar Digitación' tendra 5 segundo para seleccionar la aplicacion y seleccionar el campo donde se ingresan las guias, se debe dejar quieto mientras finaliza el proceso." });
                    break;

                case "historial inventario":
                    TxtIcono.Text = "📒";
                    TxtTituloModulo.Text = "Historial de inventarios";
                    TxtDescripcionModulo.Text = "Este módulo muestra el listado de los inventarios realizaados en ese equipo, en caso de ver los almacenados en el servidor local, debe configurar primero el servidor en la pestaña configuración. Se podra filtrar y visualizar los inventarios realizados e imprimirlos nuevamente.";
                    pasos.Add(new PasoTutorial { Numero = 1, Titulo = "Paso 1: Seleccionar rango de fecha", Descripcion = "Selecciona el rango de fecha desde los dos campos para determinar un rango y poder filtar el historial deseado." });
                    pasos.Add(new PasoTutorial { Numero = 2, Titulo = "Paso 2: Visualizar un registro", Descripcion = "En el listado de registros presionar el boton 'Ver Reporte', aparecera una ventana donde se podra visualizar el reporte generado." });
                    pasos.Add(new PasoTutorial { Numero = 3, Titulo = "Paso 3: Imprimir o Guardar", Descripcion = "Seleccione el boton de 'Imprimir' donde podra reimprimir el reporte o guardarlo como PDF." });
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
