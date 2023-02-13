using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using File = Pri.LongPath.File;

namespace AemulusModManager.Utilities
{
    public static class FileIOWrapper
    {
        public static void Copy(string input, string output, bool overwrite = false)
        {
            File.Copy($@"\\?\{input}", $@"\\?\{output}", overwrite);
        }
        public static void Delete(string input)
        {
            File.Delete($@"\\?\{input}");
        }
        public static void Move(string input, string output)
        {
            File.Move($@"\\?\{input}", $@"\\?\{output}");
        }
        public static bool Exists(string input)
        {
            return File.Exists($@"\\?\{input}");
        }
        public static void WriteAllBytes(string output, byte[] bytes)
        {
            File.WriteAllBytes($@"\\?\{output}", bytes);
        }
        public static byte[] ReadAllBytes(string input)
        {
            return File.ReadAllBytes($@"\\?\{input}");
        }
        public static void WriteAllText(string output, string contents)
        {
            File.WriteAllText($@"\\?\{output}", contents);
        }
        public static string ReadAllText(string input)
        {
            return File.ReadAllText($@"\\?\{input}");
        }
        public static string[] ReadAllLines(string input)
        {
            return File.ReadAllLines($@"\\?\{input}");
        }
        public static FileStream Create(string input)
        {
            return File.Create($@"\\?\{input}");
        }
        public static FileStream Open(string input, FileMode mode)
        {
            return File.Open($@"\\?\{input}", mode);
        }
        public static FileStream OpenRead(string input)
        {
            return File.OpenRead($@"\\?\{input}");
        }
        public static DateTime GetLastWriteTime(string input)
        {
            return File.GetLastWriteTime($@"\\?\{input}");
        }
    }
}
