using System;
using System.IO;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace DLSS_Swapper.Helpers;

public static class PeSignatureVerifier
{
    public static bool VerifyPeSignature(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);

            // 1. Read DOS Header e_lfanew
            if (stream.Length < 0x40) return false;
            stream.Position = 0x3C;
            uint peOffset = reader.ReadUInt32();
            if (peOffset >= stream.Length - 4) return false;

            // 2. Read PE Header Signature ("PE\0\0")
            stream.Position = peOffset;
            uint peSig = reader.ReadUInt32();
            if (peSig != 0x00004550) return false; // "PE\0\0"

            // 3. Read COFF Header (20 bytes)
            ushort machine = reader.ReadUInt16();
            ushort numberOfSections = reader.ReadUInt16();
            stream.Position += 12; // Skip TimeDateStamp, PointerToSymbolTable, NumberOfSymbols
            ushort sizeOfOptionalHeader = reader.ReadUInt16();
            ushort characteristics = reader.ReadUInt16();

            // 4. Read Optional Header Magic (0x10B = PE32, 0x20B = PE32+)
            long optHeaderStart = stream.Position;
            ushort magic = reader.ReadUInt16();
            bool is64Bit = (magic == 0x20B);

            // Security Directory is DataDirectory[4]
            // For PE32 (32-bit): DataDirectories start at optHeaderStart + 96 + (4 * 8) = optHeaderStart + 128
            // For PE32+ (64-bit): DataDirectories start at optHeaderStart + 112 + (4 * 8) = optHeaderStart + 144
            long secDirOffset = is64Bit ? optHeaderStart + 144 : optHeaderStart + 128;
            if (secDirOffset + 8 > stream.Length) return false;

            stream.Position = secDirOffset;
            uint certOffset = reader.ReadUInt32();
            uint certSize = reader.ReadUInt32();

            if (certOffset == 0 || certSize == 0 || certOffset + certSize > stream.Length)
            {
                return false;
            }

            // 5. Read WIN_CERTIFICATE Header
            stream.Position = certOffset;
            uint dwLength = reader.ReadUInt32();
            ushort wRevision = reader.ReadUInt16();
            ushort wCertificateType = reader.ReadUInt16();

            if (dwLength < 8 || dwLength > certSize) return false;

            byte[] certData = reader.ReadBytes((int)(dwLength - 8));
            if (certData == null || certData.Length == 0) return false;

            // 6. Parse PKCS#7 SignedData
            var signedCms = new SignedCms();
            signedCms.Decode(certData);

            // Check Certificates in the signature chain
            foreach (var cert in signedCms.Certificates)
            {
                var subject = cert.Subject;
                var issuer = cert.Issuer;

                if ((subject != null && subject.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) ||
                    (issuer != null && issuer.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
        }
        catch { }

        return false;
    }
}
