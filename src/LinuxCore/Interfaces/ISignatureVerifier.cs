using System;

namespace DLSS_Swapper.Core.Interfaces;

public interface ISignatureVerifier
{
    /// <summary>
    /// Verifies if a given file is digitally signed and valid.
    /// </summary>
    /// <param name="filePath">Path to the file on disk.</param>
    /// <returns>True if signed and valid; otherwise false.</returns>
    bool IsFileSignedAndValid(string filePath);
}
