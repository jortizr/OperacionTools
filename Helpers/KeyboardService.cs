using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OperacionTools.Helpers
{
    public static class keyboardService
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        private const byte VK_TAB = 0x09;
        private const byte VK_RETURN = 0x0D;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_SHIFT = 0x10;

        public static async Task SimularTexto(string texto, int delay) 
        {
            if (string.IsNullOrEmpty(texto)) return;

            foreach (char c in texto) {
                //traducir el caracter a una tecla virtual VK
                short vkScan = VkKeyScan(c);
                byte vk = (byte)(vkScan & 0xFF);
                bool shift = (vkScan >> 8 & 1) == 1;

                //si el caracter requiere SHIFT (mayusculas, algunos simbolos)
                if (shift) keybd_event(VK_SHIFT, 0, 0, UIntPtr.Zero);

                //presionar y soltar la tecla del caracter
                keybd_event(vk, 0, 0, UIntPtr.Zero);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                if (shift) keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                //pequeña pausa entre caracteres para simular tipeo humano
                await Task.Delay(20);
            }
        }

        public static void PresionarTab() 
        {
            keybd_event(VK_TAB, 0, 0, UIntPtr.Zero);//presionar
            keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); //soltar
        }

        public static void PresionarEnter()
        {
            keybd_event(VK_RETURN, 0, 0, UIntPtr.Zero);//presionar
            keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); //soltar
        }
    }
}
