using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace OperacionTools.Helpers
{
    internal class RedHelper
    {
        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NetResource netResource, string password, string username, int flags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string name, int flags, bool force);

        [StructLayout(LayoutKind.Sequential)]
        private class NetResource
        {
            public int Scope = 2; // RESOURCE_GLOBALNET
            public int Type = 1;  // RESOURCETYPE_DISK
            public int DisplayType = 0;
            public int Usage = 1; // RESOURCEUSAGE_CONNECTABLE
            public string LocalName = null!;
            public string RemoteName = null!;
            public string Comment = null!;
            public string Provider = null!;
        }

        public static bool AutenticarCarpetaRed(string rutaRemote, string usuario, string password)
        {
            if (string.IsNullOrEmpty(usuario)) return true; // Si no requiere credenciales, omitir autenticación forzada

            NetResource nr = new NetResource { RemoteName = rutaRemote.TrimEnd('\\') };

            // Cancelar conexiones previas colgadas para evitar conflictos
            WNetCancelConnection2(nr.RemoteName, 0, true);

            int resultado = WNetAddConnection2(nr, password, usuario, 0);
            return resultado == 0; // 0 significa conexión exitosa
        }


    }
}
