using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Threading;

// The static HashStat class contains utilities for generating and comparing hashes of files.
// The purpose of the hashing is to discourage tampering and vandalism.
// For the course.xml file, SHA256 hashing is used.
// For image and sound files, which are much larger, SHA256 is too slow, so
//    a simpler method of reading 32 evenly spaced bytes from the file is used.
// In either case, the hash is further encrypted by xor with the key given below. 

namespace Learning
{
    static class HashStat
    {
        // Encryption key for xor with hash value.
        private static byte[] key = { 062, 082, 087, 220, 060, 176, 125, 069, 149, 060, 039, 153, 101, 036, 054, 226, 247, 033, 142, 139, 084, 178, 213, 046, 157, 185, 088, 196, 040, 088, 149, 235, 173, 085, 055, 199, 176, 066, 017, 228, 091, 115, 051, 020, 152, 029, 124, 220, 072, 099, 185, 109, 028, 137, 178, 177, 062, 076, 105, 169, 242, 120, 119, 221, 101, 234, 006, 066, 043, 049, 098, 124, 209, 139, 236, 068, 030, 161, 078, 075, 014, 253, 169, 100, 091, 203, 016, 151, 103, 000, 137, 150, 072, 171, 012, 057, 026, 107, 018, 085, 049, 200, 006, 178, 150, 213, 055, 173, 041, 136, 091, 230, 104, 191, 027, 125, 055, 097, 032, 103, 013, 196, 241, 098, 196, 061, 238, 074, 195, 000, 200, 066, 144, 136, 017, 030, 084, 036, 105, 159, 251, 175, 107, 141, 245, 105, 134, 158, 108, 108, 143, 104, 083, 090, 064, 008, 110, 019, 081, 229, 072, 122, 182, 175, 248, 012, 181, 016, 202, 189, 160, 099, 168, 233, 052, 152, 053, 082, 104, 193, 252, 175, 177, 059, 227, 147, 083, 171, 255, 139, 007, 131, 189, 042, 173, 035, 189, 032, 184, 206, 010, 071, 094, 112, 116, 244, 105, 052, 232, 114, 183, 106, 122, 248, 114, 000, 008, 070, 090, 130, 086, 062, 248, 167, 202, 064, 070, 243, 117, 103, 079, 149, 093, 217, 085, 088, 165, 214, 094, 104, 219, 200, 133, 022, 164, 045, 210, 015, 110, 101, 023, 123, 067, 119, 008, 148, 185, 033, 254, 127, 018, 233, 038, 241, 147, 145, 146, 009, 063, 168, 014, 021, 035, 095, 095, 007, 000, 125, 154, 009, 002, 180, 083, 216, 179, 069, 227, 073, 126, 120, 004, 238, 064, 077, 188, 066, 003, 048, 190 };
        // Method to check the SHA256 hash of file f against the hash code h
        public static bool CheckHash(string f, byte[] h){
            byte[] hash = ReadHash(f);
            bool r = true;
            for (int i = 0; i < hash.Length; i++)
                r = r && hash[i] == h[i];
            return r;
        }
        // Method to generate the SHA256 hash of file f
        public static byte[] ReadHash(string f){
            HashAlgorithm hasher = new SHA256Managed();
            FileStream fs = null;
            fs = new FileStream(f, FileMode.Open,FileAccess.Read);
            CryptoStream chash = new CryptoStream(fs, hasher, CryptoStreamMode.Read);
            byte[] h = hasher.ComputeHash(chash);
            fs.Close();
            for (int i = 0; i < h.Length; i++)
                h[i] = (byte)(key[i] ^ h[i]);
            return h;
        }
        // Method to check the Steve hash of file f against hash code h.
        // Used for image and sound files that are too large for SHA256 hash.
        public static bool CheckHash2(string f, byte[] h){
            byte[] hash = ReadHash2(f);
            bool r = true;
            for (int i = 0; i < hash.Length; i++)
                r = r && hash[i] == h[i];
            return r;
        }
        // Method to generate the Steve hash of file f
        public static byte[] ReadHash2(string f){
            FileStream fs = null;
            fs = new FileStream(f, FileMode.Open, FileAccess.Read);
            int count = (int)fs.Length;
            int offs = (int)count / 32;
            byte[] h = new byte[32];
            h[0] = (byte)(key[0] ^ fs.ReadByte());
            for (int i = 1; i < 31; i++){
                fs.Position += offs;
                h[i] = (byte)(key[i] ^ fs.ReadByte());
            }
            fs.Position = count - 1;
            h[31] = (byte)(key[31] ^ fs.ReadByte());
            fs.Close();
            return h;
        }
    }
}
