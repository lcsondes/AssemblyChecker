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

namespace AssemblyChecker;

internal static class TeamCity
{
    public static void WriteErrorMessage(string msg)
    {
        msg = msg.Replace("|", "||").Replace("'", "|'")
                 .Replace("\n", "|n").Replace("\r", "|r")
                 .Replace("[", "|[").Replace("]", "|]");
        Console.WriteLine($"##teamcity[message status='ERROR' text='{msg}']");
    }
}
