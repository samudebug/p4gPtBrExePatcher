using System;
using System.Collections.Generic;
using System.Diagnostics;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using Reloaded.Memory.Sigscan;
using Reloaded.Memory.Sigscan.Structs;
using System.IO;
using Newtonsoft.Json;

using P4G_EXE_PTBR.Utilities;
using System.Text;

namespace P4G_EXE_PTBR
{
    class Patch
    {
        public string type { get; set; }
        public string oldStr { get; set; }
        public string newStr { get; set; }

        public string originalFile { get; set; }

        public string newFile { get; set; }
    }
    class Root
    {
        public Patch[] patches { get; set; }
    }
    class Patcher
    {
        private static readonly string MODS_BASE_DIR = @"mods/exePatches";
        protected readonly ILogger logger;

        protected readonly IMemory memory;
        protected readonly Process process;
        protected readonly IntPtr baseAddress;
        protected readonly TextEncoder encoder;

        private Scanner scanner;
        private Patch[] patches;
        public Patcher(ILogger logger)
        {
            this.logger = logger;
            process = Process.GetCurrentProcess();
            baseAddress = process.MainModule.BaseAddress;
            memory = new Memory();
            encoder = new TextEncoder();
            scanner = new Scanner(process, process.MainModule);
        }

        public void Patch()
        {
            string jsonString = File.ReadAllText($@"{MODS_BASE_DIR}/patches.json");
            Root rootObject = JsonConvert.DeserializeObject<Root>(jsonString);
            patches = rootObject.patches;
            foreach(Patch patch in patches)
            {
                switch (patch.type)
                {
                    case "string":
                        ApplyStringPatch(patch.oldStr, patch.newStr);
                        break;
                    case "file":
                        ApplyFilePatch(patch.originalFile, patch.newFile);
                        break;
                }
            }
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2} ", b);
            
            return hex.ToString().Substring(0, hex.ToString().Length - 1);
        }

        private void ApplyStringPatch(String oldString, String newStr)
        {
            logger.WriteLine($"[P4G PC EXE PT-BR] Enconding Old String \"{oldString}\"");
            byte[] encodedOldStr = encoder.EncodeString(oldString);
            string oldStrBytes = ByteArrayToString(encodedOldStr);

            PatternScanResult oldStrOffset = scanner.CompiledFindPattern(oldStrBytes);
            logger.WriteLine($"[P4G PC EXE PT-BR] Finding old string pointer");
            byte[] oldStrAddressPattern = StringToByteArray((oldStrOffset.Offset + baseAddress.ToInt32()).ToString("X"));
            Array.Reverse(oldStrAddressPattern);
            string oldStrPointerBytes = ByteArrayToString(oldStrAddressPattern);

            PatternScanResult oldStrPointer = scanner.CompiledFindPattern(oldStrPointerBytes);
            logger.WriteLine($"[P4G PC EXE PT-BR] Enconding New String \"{newStr}\"");

            byte[] encodedStr = encoder.EncodeString(newStr);
            IntPtr newOffset = memory.Allocate(encodedStr.Length);
            logger.WriteLine($"[P4G PC EXE PT-BR] Patching String \"{newStr}\" at 0x{newOffset.ToInt32().ToString("X")}");
            memory.SafeWriteRaw(newOffset, encodedStr);
            string newOffsetStr = newOffset.ToInt32().ToString("X");
            byte[] newOffsetStringBytes = StringToByteArray(newOffsetStr);
            Array.Reverse(newOffsetStringBytes);
            IntPtr pointerAddress = IntPtr.Add(baseAddress, oldStrPointer.Offset);
            logger.WriteLine($"[P4G PC EXE PT-BR] Patching pointer at {pointerAddress.ToInt32().ToString("X")}");

            memory.SafeWriteRaw(pointerAddress, newOffsetStringBytes);


        }

        private void ApplyFilePatch(String oldFilePath, String newFilePath)
        {
            logger.WriteLine($"[P4G PC EXE PT-BR] Finding Old File");

            byte[] oldFileBytes = File.ReadAllBytes($@"{MODS_BASE_DIR}/{oldFilePath}");
            string oldFilePattern = ByteArrayToString(oldFileBytes);
            PatternScanResult oldFileOffset = scanner.CompiledFindPattern(oldFilePattern);
            logger.WriteLine($"[P4G PC EXE PT-BR] Finding old file pointer");
            byte[] oldFileAddressPattern = StringToByteArray((oldFileOffset.Offset + baseAddress.ToInt32()).ToString("X"));
            Array.Reverse(oldFileAddressPattern);
            string oldFilePointerBytes = ByteArrayToString(oldFileAddressPattern);

            PatternScanResult oldFilePointer = scanner.CompiledFindPattern(oldFilePointerBytes);
            logger.WriteLine($"[P4G PC EXE PT-BR] Patching new file");
            byte[] newFileBytes = File.ReadAllBytes($@"{MODS_BASE_DIR}/{newFilePath}");
            IntPtr newFileOffset = memory.Allocate(newFileBytes.Length);
            memory.SafeWriteRaw(newFileOffset, newFileBytes);
            string newOffsetStr = newFileOffset.ToInt32().ToString("X");
            byte[] newOffsetStringBytes = StringToByteArray(newOffsetStr);
            Array.Reverse(newOffsetStringBytes);
            IntPtr pointerAddress = IntPtr.Add(baseAddress, oldFilePointer.Offset);
            logger.WriteLine($"[P4G PC EXE PT-BR] Patching file pointer");
            memory.SafeWriteRaw(pointerAddress, newOffsetStringBytes);



        }

    }
}
