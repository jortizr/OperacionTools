using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PuppeteerSharp;
using OperacionTools.Models;

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
        public async Task GenerarReporteAuditoriaAsync(List<RegistroInventario> datos, string rutaDestinoPdf)
        {
            // 1. Resolver el nombre dinámico de la Regional y la Bodega
            var regionales = _regionalService.ObtenerRegionales();
            string codRegRaw = datos.FirstOrDefault()?.Reg ?? "00";
            string codRegKey = codRegRaw.TrimStart('0');

            string codRegTrimmed = codRegRaw.TrimStart('0');
            if (string.IsNullOrEmpty(codRegTrimmed)) codRegTrimmed = "1";


            if (string.IsNullOrEmpty(codRegKey)) codRegKey = "1"; // Fallback de seguridad

            string nombreRegional = regionales.ContainsKey(codRegKey) ? regionales[codRegKey] : "Desconocida";
            string nombreBodega = datos.FirstOrDefault()?.Bodega ?? "Malla Documentos";

            // 2. Procesar el logo corporativo (logo-envia.png) en Base64 para evitar enlaces rotos
            string srcLogo = string.Empty;
            string rutaLogo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo-envia.png");

            if (File.Exists(rutaLogo))
            {
                byte[] imageBytes = File.ReadAllBytes(rutaLogo);
                string base64String = Convert.ToBase64String(imageBytes);
                srcLogo = $"data:image/png;base64,{base64String}";
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
                        <h1 class='title'>Inventario Malla Regional {nombreRegional}</h1>
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
                            <th style='width: 12%;'>Nº Guía</th>
                            <th style='width: 16%;'>Novedad</th>
                            <th style='width: 6%; text-align: center;'>Saldo</th>
                            <th>Remitente</th>
                            <th style='width: 12%;'>Estado</th>
                            <th style='width: 8%;'>Rack</th>
                            <th style='width: 6%;'>CodEntr</th>
                            <th style='width: 6%; text-align: center;'>Leídas</th>
                            <th style='width: 14%; text-align: center;'>Dictamen</th>
                        </tr>
                    </thead>
                    <tbody>");

            foreach (var item in datos)
            {
                // Determinar el estilo visual de la fila según la conciliación
                string cssClass = "status-error";
                if (item.EstadoConciliacion.Contains("✅") || item.EstadoConciliacion.ToUpper().Contains("OK"))
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
                        <td style='text-align: center;'>{item.Saldo}</td>
                        <td>{item.Remitente}</td>
                        <td>{item.Estado}</td>
                        <td>{item.Rack}</td>
                        <td>{item.CodEntr}</td>
                        <td style='text-align: center; font-weight: bold;'>{item.UnidadesLeidas}</td>
                        <td style='text-align: center;'><span class='badge {cssClass}'>{item.EstadoConciliacion}</span></td>
                    </tr>");
            }

            htmlBuilder.Append(@"
                    </tbody>
                </table>
            </body>
            </html>");

            // 4. Inicializar y lanzar el navegador Headless de Puppeteer Sharp
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            using var page = await browser.NewPageAsync();

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