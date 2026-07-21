using System;
using System.IO;
using DLSS_Swapper.Core.Interfaces;

namespace DLSS_Swapper.Core.Services;

public class LinuxSignatureVerifier : ISignatureVerifier
{
    public bool IsFileSignedAndValid(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        try
        {
            // Basic PE header verification for Windows DLL files on Linux
            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            if (stream.Length < 0x40)
            {
                return false;
            }

            // Verify 'MZ' header
            if (reader.ReadUInt16() != 0x5A4D)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
