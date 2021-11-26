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

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace AssemblyChecker;

/// <summary>
/// Class holding the main entry point.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point of the program.
    /// </summary>
    /// <param name="verbose">
    /// Print files that were successfully checked.
    /// </param>
    /// <param name="teamcity">
    /// Produce TeamCity-compatible output.
    /// </param>
    /// <param name="root">
    /// The root directory to scan, defaults to the current working directory.
    /// </param>
    /// <param name="list">
    /// A file to use as the whitelist, defaults to <c>filelist.xml</c> in the current directory.
    /// </param>
    private static int Main(bool verbose,
                            bool teamcity,
                            string root = ".",
                            string list = "filelist.xml")
    {
        Stream xsd = typeof(Program).Assembly.GetManifestResourceStream(
            "AssemblyChecker.whitelist.xsd")!;

        XmlSchemaSet schemaSet = new();
        schemaSet.Add(XmlSchema.Read(xsd, null)!);

        XmlReaderSettings settings = new()
        {
            ConformanceLevel = ConformanceLevel.Document,
            ValidationType = ValidationType.Schema,
            Schemas = schemaSet,
        };

        int errorCount = 0;
        void PrintError(string msg)
        {
            ++errorCount;
            if (teamcity)
                TeamCity.WriteErrorMessage(msg);
            else
                Console.Error.WriteLine(msg);
        }

        try
        {
            using XmlReader reader = XmlReader.Create(list, settings);
            XDocument doc = XDocument.Load(reader);

            Regex[] ignore = (from i in doc.Root!.Elements("Ignore")
                              select new Regex(i.Value)).ToArray();

            Dictionary<string, Rule> fileRules = new();
            foreach (XElement xml in doc.Root.Elements("File"))
            {
                string name = xml.Attribute("Path")!.Value;
                string path = Path.Combine(root, name);

                // Check that if something is whitelisted it does exist.
                // The contents will be checked in the main pass.
                if (!File.Exists(path))
                    PrintError($"File {name} doesn't exist");

                fileRules.Add(name, new Rule(xml));
            }

            // Run a DFS from the current working directory
            Stack<DirectoryInfo> stack = new();
            stack.Push(new DirectoryInfo(root));
            while (stack.TryPop(out DirectoryInfo? dir))
            {
                foreach (DirectoryInfo subdir in dir.EnumerateDirectories())
                    stack.Push(subdir);

                foreach (FileInfo file in dir.EnumerateFiles())
                {
                    string path = Path.GetRelativePath(root, file.FullName);

                    if (ignore.Any(r => r.IsMatch(path)))
                        continue;

                    if (!fileRules.TryGetValue(path, out Rule rule))
                        PrintError($"{path} not on whitelist");
                    else
                    {
                        string[] errors = rule.Validate(file).ToArray();

                        foreach (string err in errors)
                            PrintError(err);

                        if (verbose && errors.Length == 0)
                            Console.WriteLine($"{path} passed");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            PrintError($"{ex.GetType().Name}: {ex.Message}");
        }

        // Patch error count for various OSes

        // On Windows, 259 is STILL_ACTIVE
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (errorCount == 259)
                ++errorCount;
        }
        // Most *nix OSes only return the lowest byte
        else if (errorCount > 0 && (errorCount & 0xFF) == 0)
            ++errorCount;

        return errorCount;
    }
}
