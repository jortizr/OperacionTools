using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.Collections.Generic;
using System.Text;
using static OperacionTools.Interfaz.InventarioView;

namespace OperacionTools.Services
{
    /// <summary>
    /// Servicio enfocado puramente en la compilación de plantillas HTML y su exportación a PDF para auditorías.
    /// </summary>
    internal class InventarioReportService
    {
        /// <summary>
         /// Compila las lecturas del inventario en un formato HTML limpio y corporativo y genera un archivo PDF físico.
         /// </summary>
         /// <param name="datos">Lista de registros conciliados.</param>
         /// <param name="rutaDestinoPdf">Ruta absoluta donde se escribirá el PDF.</param>
        public async Task GenerarReporteAuditoriaAsync(List<RegistroInventario> datos, string rutaDestinoPdf)
        {
            //descargar y/o verificar el motod de Chromium en segun plano
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            //Construcción de la plantilla HTML
            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append(@"
                <html>
                <head>
                    <style>
                        body { font-family: 'Segoe UI', Arial, sans-serif; margin: 30px; color: #333; }
                        h2 { color: #2c3e50; border-bottom: 2px solid #34495e; padding-bottom: 8px; }
                        .meta-box { background: #f8f9fa; padding: 15px; border-left: 4px solid #f39c12; margin-bottom: 20px; border-radius: 4px; }
                        table { width: 100%; border-collapse: collapse; margin-top: 15px; font-size: 13px; }
                        th { background-color: #2c3e50; color: white; padding: 10px; text-align: left; }
                        td { padding: 8px; border-bottom: 1px solid #ddd; }
                        tr:nth-child(even) { background-color: #f2f2f2; }
                        .badge { padding: 4px 8px; border-radius: 4px; font-weight: bold; font-size: 11px; }
                        .badge-ok { background-color: #d4edda; color: #155724; }
                        .badge-error { background-color: #f8d7da; color: #721c24; }
                        .badge-warn { background-color: #fff3cd; color: #856404; }
                    </style>
                </head>
                <body>
                    <h2>📋 Informe de Inventario de Malla</h2>
                    <div class='meta-box'>
                        <strong>Fecha de Emisión:</strong> " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + @"<br/>
                        <strong>Dispositivo Evaluador:</strong> Control Operativo Malla General
                    </div>
                    <table>
                        <thead>
                            <tr>
                                <th>Guía Completa</th>
                                <th>Reg</th>
                                <th>Serv</th>
                                <th>Consecutivo</th>
                                <th>Esperadas (Sist.)</th>
                                <th>Leídas (Físico)</th>
                                <th>Estado de Conciliación</th>
                            </tr>
                        </thead>
                        <tbody>");

            foreach (var item in datos)
            {
                string cssBadge = item.EstadoConciliacion.Contains("Correcto") ? "badge-ok" :
                                  item.EstadoConciliacion.Contains("Faltante") || item.EstadoConciliacion.Contains("Sobrante") ? "badge-error" : "badge-warn";

                htmlBuilder.Append($@"
                    <tr>
                        <td><strong>{item.GuiaCompleta}</strong></td>
                        <td>{item.Reg}</td>
                        <td>{item.Serv}</td>
                        <td>{item.Consecutivo}</td>
                        <td>{item.UnidadesEsperadas}</td>
                        <td>{item.UnidadesLeidas}</td>
                        <td><span class='badge {cssBadge}'>{item.EstadoConciliacion}</span></td>
                    </tr>");
            }

            htmlBuilder.Append(@"
                        </tbody>
                    </table>
                </body>
                </html>");

            //lanzamiento del proceso Headless para renderizar el HTML y generar el PDF
            using (var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true }))
            using (var page = await browser.NewPageAsync())
            {
                await page.SetContentAsync(htmlBuilder.ToString());

                var pdfOptions = new PdfOptions
                {
                    Format = PaperFormat.Letter,
                    PrintBackground = true,
                    MarginOptions = new MarginOptions { Top = "20px", Bottom = "20px", Left = "20px", Right = "20px" }
                };

                await page.PdfAsync(rutaDestinoPdf, pdfOptions);
            }

        }

    }
}
