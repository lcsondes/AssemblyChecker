// Copyright © 2021 László Csöndes
//
// This file is part of AssemblyChecker, a program that verifies the contents
// of a directory against a whitelist based on various criteria.
//
// AssemblyChecker is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of
// the License, or (at your option) any later version.
//
// AssemblyChecker is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with AssemblyChecker. If not, see <https://www.gnu.org/licenses/>.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace AssemblyChecker;

public readonly record struct Rule
{
    public readonly FileInfo Path;
    public readonly string? Version;
    public readonly string? FileVersion;
    public readonly byte[]? Sha256Hash;

    /// <summary>
    /// Initializes a new instance of the <see cref="Rule" /> structure
    /// from the specified XML element.
    /// </summary>
    /// <param name="xml"><c>&lt;Assembly&gt;</c> element.</param>
    /// <exception cref="ArgumentException"></exception>
    public Rule(XElement xml)
    {
        Path = new FileInfo(xml.Attribute("Path")!.Value);
        Version = xml.Attribute("Version")?.Value;
        FileVersion = xml.Attribute("FileVersion")?.Value;
        Sha256Hash = xml.Attribute("SHA256")?.Value switch
        {
            { } value => Convert.FromHexString(value),
            null => null,
        };

        if ((Version ?? FileVersion ?? (object?)Sha256Hash) == null)
            throw new ArgumentException("At least one rule must be defined");
    }

    /// <returns>Errors collected into an array.</returns>
    public IEnumerable<string> Validate(FileInfo file)
    {
        Debug.Assert(file.FullName == Path.FullName);

        if (Version != null || FileVersion != null)
        {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(file.FullName);

            if (Version != null && info.ProductVersion != Version)
                yield return $"{file} has version {info.ProductVersion}, " +
                             $"{Version} expected";

            if (FileVersion != null && info.FileVersion != FileVersion)
                yield return $"{file} has file version {info.FileVersion}, " +
                             $"{FileVersion} expected";
        }

        if (Sha256Hash != null)
        {
            SHA256 sha = SHA256.Create();
            using FileStream stream = file.OpenRead();
            byte[] hash = sha.ComputeHash(stream);

            if (!hash.SequenceEqual(Sha256Hash))
                yield return $"{file} has SHA-256 hash " +
                             $"{Convert.ToHexString(hash)}, " +
                             $"{Convert.ToHexString(Sha256Hash)} expected";
        }
    }
}
