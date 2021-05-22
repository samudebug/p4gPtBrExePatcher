using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace P4G_EXE_PTBR.Utilities
{
    class TextEncoding
    {
        public char character { get; set; }
        public String encoded { get; set; }
    }
    class Root
    {
        public TextEncoding[] encondings { get; set; }
    }
    
    class TextEncoder
    {
        private TextEncoding[] encondings;
        public TextEncoder()
        {
            String jsonString;
            jsonString = File.ReadAllText(@"mods/exePatches/_charEnconding.json");
            Root rootObject = JsonConvert.DeserializeObject<Root>(jsonString);
            encondings = rootObject.encondings;
        }

        private bool IsSpecialEncode(char test)
        {
            bool result = false;
            foreach (TextEncoding enconding in encondings)
            {
                if (enconding.character == test) result = true;
            }
            return result;
        }

        private byte[] GetEncodeForChar(char c)
        {
            List<byte> result = new List<byte>();
            foreach (TextEncoding enconding in encondings)
            {
                if (enconding.character == c) {
                    string encoded = enconding.encoded;
                    string[] split = encoded.Split(" ");
                    foreach(string s in split)
                    {
                        result.Add((byte)Convert.ToInt32(s, 16));
                    }
                }
            }
            return result.ToArray();
        }

        public byte[] EncodeString(String strToEncode)
        {
            List<byte> result = new List<byte>();

            foreach(char c in strToEncode)
            {
                if (IsSpecialEncode(c)) result.AddRange(GetEncodeForChar(c));
                else result.Add((byte) c);
            }
            result.Add((byte)Convert.ToInt32("00", 16));
            return result.ToArray();
        }

    }
}

