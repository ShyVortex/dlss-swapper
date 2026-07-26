using System;
using System.IO;
using System.Text;

namespace DLSS_Swapper.Core.Helpers;

/// <summary>
/// Reads the VS_FIXEDFILEINFO version resource from a Windows PE (.dll/.exe) file on Linux,
/// where System.Diagnostics.FileVersionInfo.GetVersionInfo() cannot parse PE version resources.
/// </summary>
public static class PeVersionReader
{
    /// <summary>
    /// Attempts to read the file version from a PE file's VS_FIXEDFILEINFO resource.
    /// Returns null if the file is not a valid PE or has no version resource.
    /// </summary>
    public static Version? GetFileVersion(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);

            // Read DOS header - check MZ signature
            if (stream.Length < 64) return null;
            var dosSignature = reader.ReadUInt16();
            if (dosSignature != 0x5A4D) return null; // "MZ"

            // Read PE offset from DOS header at offset 0x3C
            stream.Seek(0x3C, SeekOrigin.Begin);
            var peOffset = reader.ReadUInt32();
            if (peOffset + 4 > stream.Length) return null;

            // Read PE signature
            stream.Seek(peOffset, SeekOrigin.Begin);
            var peSignature = reader.ReadUInt32();
            if (peSignature != 0x00004550) return null; // "PE\0\0"

            // Read COFF header
            var machine = reader.ReadUInt16();
            var numberOfSections = reader.ReadUInt16();
            reader.ReadUInt32(); // TimeDateStamp
            reader.ReadUInt32(); // PointerToSymbolTable
            reader.ReadUInt32(); // NumberOfSymbols
            var sizeOfOptionalHeader = reader.ReadUInt16();
            reader.ReadUInt16(); // Characteristics

            // Skip optional header to get to section headers
            var optionalHeaderStart = stream.Position;
            stream.Seek(optionalHeaderStart + sizeOfOptionalHeader, SeekOrigin.Begin);

            // Find .rsrc section
            uint rsrcVirtualAddress = 0;
            uint rsrcRawDataPointer = 0;
            uint rsrcVirtualSize = 0;

            for (int i = 0; i < numberOfSections; i++)
            {
                var sectionNameBytes = reader.ReadBytes(8);
                var sectionName = Encoding.ASCII.GetString(sectionNameBytes).TrimEnd('\0');
                var virtualSize = reader.ReadUInt32();
                var virtualAddress = reader.ReadUInt32();
                var sizeOfRawData = reader.ReadUInt32();
                var pointerToRawData = reader.ReadUInt32();
                reader.ReadBytes(16); // Skip remaining section header fields

                if (sectionName == ".rsrc")
                {
                    rsrcVirtualAddress = virtualAddress;
                    rsrcRawDataPointer = pointerToRawData;
                    rsrcVirtualSize = virtualSize;
                    break;
                }
            }

            if (rsrcRawDataPointer == 0) return null;

            // Search for VS_FIXEDFILEINFO signature (0xFEEF04BD) in the .rsrc section
            stream.Seek(rsrcRawDataPointer, SeekOrigin.Begin);
            var rsrcData = reader.ReadBytes((int)Math.Min(rsrcVirtualSize, stream.Length - rsrcRawDataPointer));

            var signatureBytes = new byte[] { 0xBD, 0x04, 0xEF, 0xFE }; // VS_FIXEDFILEINFO signature (little-endian)
            int signatureIndex = FindPattern(rsrcData, signatureBytes);
            if (signatureIndex < 0) return null;

            // VS_FIXEDFILEINFO structure layout after the signature:
            // DWORD dwSignature;        // 0xFEEF04BD (already found)
            // DWORD dwStrucVersion;
            // DWORD dwFileVersionMS;    // Major.Minor
            // DWORD dwFileVersionLS;    // Build.Revision
            if (signatureIndex + 16 > rsrcData.Length) return null;

            var fileVersionMS = BitConverter.ToUInt32(rsrcData, signatureIndex + 8);
            var fileVersionLS = BitConverter.ToUInt32(rsrcData, signatureIndex + 12);

            var major = (int)(fileVersionMS >> 16);
            var minor = (int)(fileVersionMS & 0xFFFF);
            var build = (int)(fileVersionLS >> 16);
            var revision = (int)(fileVersionLS & 0xFFFF);

            return new Version(major, minor, build, revision);
        }
        catch
        {
            return null;
        }
    }

    private static int FindPattern(byte[] data, byte[] pattern)
    {
        for (int i = 0; i <= data.Length - pattern.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (data[i + j] != pattern[j])
                {
                    found = false;
                    break;
                }
            }
            if (found) return i;
        }
        return -1;
    }
}
