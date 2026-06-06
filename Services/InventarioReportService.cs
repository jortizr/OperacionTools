using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PuppeteerSharp;
using System.Net.Http;
using OperacionTools.Models;
using System.Windows;

namespace OperacionTools.Services
{
    /// <summary>
    /// Servicio encargado de la compilación de la plantilla HTML corporativa con la identidad de marca Envía
    /// y su exportación limpia a documentos PDF de auditoría.
    /// </summary>
    internal class InventarioReportService
    {
        private readonly RegionalService _regionalService = new();

        /// <summary>
        /// Compila las lecturas y datos del sistema mapeados en un documento PDF profesional con los colores de la marca.
        /// </summary>
        /// <param name="datos">Lista consolidada de registros de inventario.</param>
        /// <param name="rutaDestinoPdf">Ruta absoluta donde se escribirá el PDF físico.</param>
        public async Task GenerarReporteAuditoriaAsync(List<RegistroInventario> datos, string rutaDestinoPdf, string observaciones = "",
            IProgress<double> progresoDescarga = null, Action<string> reportarEstado = null)
        {
            // 1. Resolver el nombre dinámico de la Regional y la Bodega
            var regionales = _regionalService.ObtenerRegionales();

            string codRegRaw = datos.FirstOrDefault(x => !string.IsNullOrEmpty(x.RegionalMaestro))?.RegionalMaestro ?? "0";

            // Limpiamos ceros iniciales para que coincida con las llaves del JSON ("1", "8", "11", etc.)
            string codRegKey = codRegRaw.TrimStart('0');
            if (string.IsNullOrEmpty(codRegKey)) codRegKey = "0"; // Fallback

            string codRegTrimmed = codRegRaw.TrimStart('0');
            if (string.IsNullOrEmpty(codRegTrimmed)) codRegTrimmed = "1";


            if (string.IsNullOrEmpty(codRegKey)) codRegKey = "no encontrada"; // Fallback de seguridad

            string nombreRegional = regionales.ContainsKey(codRegKey) ? regionales[codRegKey] : $"Regional {codRegKey}";

            string nombreBodega = datos.FirstOrDefault()?.Bodega ?? "Malla - Bodega no encontrada";

            // 2. Procesar el logo corporativo (logo-envia.png) en Base64 para evitar enlaces rotos
            string srcLogo = string.Empty;
            string rutaLogo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo-envia.png");

            if (File.Exists(rutaLogo))
            {
                byte[] imageBytes = File.ReadAllBytes(rutaLogo);
                string base64String = Convert.ToBase64String(imageBytes);
                srcLogo = $"data:image/png;base64,{base64String}";
            }
            else
            {
                // Alerta de diagnóstico temporal por si el nombre del archivo tiene alguna diferencia
                MessageBox.Show(
                    $"No se encontró el logo en la ruta esperada:\n{rutaLogo}\n\nEl reporte se generará sin logo.",
                    "⚠️ Advertencia de Recursos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            // 3. Compilación de la Estructura HTML y Estilos CSS Oficiales de Envía
            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append($@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    :root {{ 
                        --rojo-envia: #ba0020; 
                        --gris-envia: #4e4e4e; 
                        --gris-claro: #f5f5f5; 
                        --borde-linea: #e0e0e0;
                    }}
                    body {{ 
                        font-family: 'Segoe UI', Arial, sans-serif; 
                        margin: 0; 
                        padding: 30px; 
                        color: var(--gris-envia); 
                        background-color: #ffffff;
                    }}
                    
                    /* Contenedor del Encabezado Corporativo */
                    .header-container {{
                        display: flex;
                        justify-content: space-between;
                        align-items: center;
                        border-bottom: 3px solid var(--rojo-envia);
                        padding-bottom: 15px;
                        margin-bottom: 25px;
                    }}
                    .header-text {{
                        border-left: 6px solid var(--rojo-envia);
                        padding-left: 15px;
                    }}
                    .title {{ 
                        font-size: 24px; 
                        font-weight: bold; 
                        color: var(--gris-envia); 
                        margin: 0; 
                    }}
                    .subtitle {{ 
                        font-size: 15px; 
                        color: var(--rojo-envia); 
                        margin: 5px 0 0 0; 
                        text-transform: uppercase; 
                        font-weight: 600;
                        letter-spacing: 0.5px;
                    }}
                    .logo-container img {{
                        max-height: 55px;
                        object-fit: contain;
                    }}
                    .logo-fallback {{
                        font-size: 26px;
                        font-weight: bold;
                        color: var(--rojo-envia);
                        letter-spacing: -1px;
                    }}
                    
                    /* Bloque de Información General */
                    .info-bar {{ 
                        background: var(--gris-claro); 
                        padding: 12px 20px; 
                        border-radius: 6px; 
                        margin-bottom: 25px; 
                        font-size: 13px; 
                        display: flex; 
                        justify-content: space-between;
                        border: 1px solid #e8e8e8;
                    }}
                    .info-item strong {{
                        color: var(--gris-envia);
                    }}
                    
                    /* Estilos de la Tabla de Auditoría */
                    table {{ 
                        width: 100%; 
                        border-collapse: collapse; 
                        margin-top: 15px; 
                        font-size: 10px; 
                    }}
                    th {{ 
                        background-color: var(--rojo-envia); 
                        color: white; 
                        padding: 10px 6px; 
                        text-align: left; 
                        text-transform: uppercase; 
                        font-weight: 600;
                        font-size: 9px;
                        letter-spacing: 0.3px;
                    }}
                    td {{ 
                        padding: 9px 6px; 
                        border-bottom: 1px solid var(--borde-linea); 
                        color: #333333;
                    }}
                    tr:nth-child(even) {{ 
                        background-color: #fbfbfb; 
                    }}
                    
                    /* Píldoras de Estado (Badges) */
                    .badge {{ 
                        padding: 3px 6px; 
                        border-radius: 4px; 
                        font-weight: bold; 
                        font-size: 9px;
                        display: inline-block;
                        text-align: center;
                    }}
                    .status-ok {{ 
                        background-color: #e8f8f0;
                        color: #1e7e34; 
                    }} 
                    .status-error {{ 
                        background-color: #fde8ec;
                        color: var(--rojo-envia); 
                    }}
                    .status-warn {{
                        background-color: #fff3cd;
                        color: #856404;
                    }}
                </style>
            </head>
            <body>
                <div class='header-container'>
                    <div class='header-text'>
                        <h1 class='title'>Inventario Malla Regional - {nombreRegional}</h1>
                        <div class='subtitle'>Reporte de Control y Cuadrante de Mercancía</div>
                    </div>
                    <div class='logo-container'>");

            if (!string.IsNullOrEmpty(srcLogo))
            {
                htmlBuilder.Append($"<img src='{srcLogo}' alt='Logo Envía' />");
            }
            else
            {
                htmlBuilder.Append("<span class='logo-fallback'>envía</span>");
            }

            htmlBuilder.Append($@"
                    </div>
                </div>

                <div class='info-bar'>
                    <div class='info-item'><strong>Área / Bodega:</strong> {nombreBodega}</div>
                    <div class='info-item'><strong>Fecha de Emisión:</strong> {DateTime.Now:dd/MM/yyyy hh:mm tt}</div>
                </div>

                <table>
                    <thead>
                        <tr>
                            <th style='width: 4%;'>Reg</th>
                            <th style='width: 4%;'>Serv</th>
                            <th style='width: 12%;'>Consecutivo</th>
                            <th style='width: 16%;'>Novedad</th>
                            <th>Remitente</th>
                            <th style='width: 12%;'>Estado</th>
                            <th style='width: 8%;'>Rack</th>
                            <th style='width: 6%;'>CodNom</th>
                            <th style='width: 6%; text-align: center;'>Leídas</th>
                            <th style='width: 6%; text-align: center;'>Unidades</th>
                            <th style='width: 6%; text-align: center;'>Saldo</th>
                            <th style='width: 14%; text-align: center;'>Observacion</th>
                        </tr>
                    </thead>
                    <tbody>");

            foreach (var item in datos)
            {
                // Determinar el estilo visual de la fila según la conciliación
                string cssClass = "status-error";
                if (item.EstadoConciliacion.Contains("✅OK") || item.EstadoConciliacion.ToUpper().Contains("OK"))
                {
                    cssClass = "status-ok";
                }
                else if (item.EstadoConciliacion.Contains("Pendiente") || item.EstadoConciliacion.Contains("Leído"))
                {
                    cssClass = "status-warn";
                }

                htmlBuilder.Append($@"
                    <tr>
                        <td>{item.Reg}</td>
                        <td>{item.Serv}</td>
                        <td><strong>{item.Consecutivo}</strong></td>
                        <td>{item.Novedad}</td>
                        <td>{item.Remitente}</td>
                        <td>{item.Estado}</td>
                        <td>{item.Rack}</td>
                        <td>{item.CodEntr}</td>
                        <td style='text-align: center; font-weight: bold;'>{item.UnidadesLeidas}</td>
                        <td style='text-align: center;'>{item.UnidadesEsperadas}</td>
                        <td style='text-align: center;'>{item.Saldo}</td>
                        <td style='text-align: center;'><span class='badge {cssClass}'>{item.EstadoConciliacion}</span></td>
                    </tr>");
            }

            htmlBuilder.Append(@"
                    </tbody>
                </table>");

            if (!string.IsNullOrWhiteSpace(observaciones))
            {
                htmlBuilder.Append($@"
                <div style='margin-top: 35px; padding: 15px; border: 1px solid #e0e0e0; background-color: #fcfcfc; border-radius: 6px;'>
                    <h3 style='margin-top: 0; color: #2c3e50; font-size: 16px; border-bottom: 2px solid #f39c12; padding-bottom: 5px;'>
                        📝 Observaciones y Hallazgos de la Auditoría
                    </h3>
                    <p style='white-space: pre-wrap; font-size: 14px; color: #444; line-height: 1.5; margin: 8px 0 0 0;'>
                        {observaciones}
                    </p>
                </div>");
                    }
                    else
                    {
                        // Fallback por si va vacío, para dejar constancia legal de que no hubo novedades extras
                        htmlBuilder.Append(@"
                <div style='margin-top: 35px; padding: 12px; border: 1px dashed #ccc; background-color: #fafafa; border-radius: 6px; text-align: center;'>
                    <p style='font-size: 13px; color: #777; font-style: italic; margin: 0;'>
                        No se registraron observaciones adicionales durante esta jornada de auditoría del inventario.
                    </p>
                </div>");
                    }

                    // Continuación normal del cierre del HTML
                    htmlBuilder.Append(@"
                        </body>
                        </html>");


            // 4. Inicializar y lanzar el navegador Headless de Puppeteer Sharp
            reportarEstado?.Invoke("⏳ Verificando componentes de renderizado... \n (Si es la primera vez, esto puede tardar unos minutos)");

            // Configuramos las opciones para capturar el progreso de los Bytes
            var opciones = new BrowserFetcherOptions
            {
                Browser = SupportedBrowser.Chrome,
                CustomFileDownload = async (url, archivePath) =>
                {
                    using var client = new HttpClient();
                    // Enviamos la petición leyendo los encabezados primero para saber el tamaño total del archivo
                    using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength;
                    using var downloadStream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = File.Create(archivePath);

                    var buffer = new byte[8192]; // Buffer de lectura de 8KB
                    long totalBytesLeidos = 0;
                    int bytesLeidos;

                    while ((bytesLeidos = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesLeidos);
                        totalBytesLeidos += bytesLeidos;

                        if (totalBytes.HasValue)
                        {
                            // Cálculo matemático del porcentaje de progreso
                            double porcentaje = (double)totalBytesLeidos / totalBytes.Value * 100;
                            progresoDescarga?.Report(porcentaje);
                        }
                    }
                }
            };

            var browserFetcher = new BrowserFetcher(opciones);
            await browserFetcher.DownloadAsync();

            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            using var page = await browser.NewPageAsync();

            reportarEstado?.Invoke("⏳ Generando y formateando el documento PDF...");
        

            await page.SetContentAsync(htmlBuilder.ToString());

            await page.PdfAsync(rutaDestinoPdf, new PdfOptions
            {
                Format = PuppeteerSharp.Media.PaperFormat.Letter,
                PrintBackground = true,
                MarginOptions = new PuppeteerSharp.Media.MarginOptions
                {
                    Top = "15mm",
                    Bottom = "15mm",
                    Left = "15mm",
                    Right = "15mm"
                }
            });
        }
    }
}
