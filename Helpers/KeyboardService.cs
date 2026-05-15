using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OperacionTools.Helpers
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    private const byte VK_TAB = 0x09;
    private const byte VK_RETURN = 0x0D;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    public static async Task SimularTexto(string texto, int delay)
    {
        foreach (char c in texto)
        {
            // Usamos el simulador de WPF para caracteres
            System.Windows.Forms.SendKeys.SendWait(c.ToString()); // Nota: SendKeys funciona internamente si el proyecto tiene <UseWindowsForms>true</UseWindowsForms>
                                                                  // Pero si quieres evitarlo totalmente, usamos P/Invoke:
            await Task.Delay(10);
        }
    }

    public static void PresionarTab()
    {
        keybd_event(VK_TAB, 0, 0, 0); // Presionar
        keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, 0); // Soltar
    }

    public static void PresionarEnter()
    {
        keybd_event(VK_RETURN, 0, 0, 0);
        keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, 0);
    }
}
