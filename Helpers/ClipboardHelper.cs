using System;
using System.Collections.Generic;
using System.Text;

namespace OperacionTools.Helpers
{
    internal class ClipboardHelper
    {
        /// <summary>
        /// Obtiene el texto del portapapeles y lo divide en líneas eliminando las vacías.
        /// </summary>
        public static IEnumerable<string> ObtenerLineasDesdePortapapeles()
        {
            string texto = System.Windows.Clipboard.GetText();
            if(string.IsNullOrEmpty(texto))
                return Array.Empty<string>();

            return texto.Split(new[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
