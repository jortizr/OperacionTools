using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Shapes;
using System.Linq;

namespace OperacionTools.Helpers
{

    public static class GuiaHelper
    {
        /// <summary>
        /// Normaliza una cadena de entrada al formato de 12 digitos (2-1-9)
        /// </summary>
        /// <param name="input">Cadena con guiones, espacios o solo numeros.</param>
        /// <return>Tupla con las 3 partes de la guia: (Parte1, Parte2, Parte3).</return>
        public static (string reg, string ser, string cons)? ProcesarLinea(string linea) 
        {
            if (string.IsNullOrWhiteSpace(linea)) return null;

            // Filtrar encabezados comunes para evitar procesar basura del portapapeles
            string lower = linea.ToLower();
            if (lower.Contains("reg") || lower.Contains("serv") || lower.Contains("consecutivo") || lower.Contains("guia"))
                return null;

            char[] separadores = new char[] { '-', '\t', ' ' };
            string[] partes = linea.Split(separadores, StringSplitOptions.RemoveEmptyEntries);

            string r1 = "", r2 = "", r3 = "";

            if (partes.Length >= 3)
            {
                // CASO CON SEPARADORES: "1-4-12345"
                r1 = LimpiarNumeros(partes[0]).PadLeft(2, '0'); // P1 -> 2 dígitos (Reg)
                r2 = LimpiarNumeros(partes[1]);                 // P2 -> 1 dígito (Serv)
                r3 = LimpiarNumeros(partes[2]).PadLeft(9, '0'); // P3 -> 9 dígitos (Consecutivo)
            }
            else
            {
                // CASO CADENA PLANA: "1412345" o "014123456789"
                string soloNumeros = LimpiarNumeros(linea);
                if (soloNumeros.Length < 3) return null;

                // Tomamos los primeros 2 como P1, el 3ero como P2 y el resto como P3
                r1 = soloNumeros.Substring(0, 2).PadLeft(2, '0');
                r2 = soloNumeros.Substring(2, 1);
                r3 = soloNumeros.Substring(3).PadLeft(9, '0');
            }

            // Validamos que el resultado final tenga sentido (solo si r3 no excede los 9 dígitos lógicos)
            if (r3.Length > 9) r3 = r3.Substring(r3.Length - 9);

            return (r1, r2, r3);
        }

        private static string LimpiarNumeros(string texto) => new string(texto.Where(char.IsDigit).ToArray());
    }
}
