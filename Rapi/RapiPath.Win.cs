using System;
using System.Text;

namespace Rapi
{
    public partial class RapiPath
    {
        string GetPathRootWin(string path)
        {
            if (IsEffectivelyEmpty(path.AsSpan()))
                return null;

            ReadOnlySpan<char> result = GetPathRootWin(path.AsSpan());
            if (path.Length == result.Length)
                return NormalizeDirectorySeparatorsWin(path);

            return NormalizeDirectorySeparatorsWin(result.ToString());
        }
        
        string NormalizeDirectorySeparatorsWin(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            char current;

            // Make a pass to see if we need to normalize so we can potentially skip allocating
            bool normalized = true;

            for (int i = 0; i < path.Length; i++)
            {
                current = path[i];
                if (IsDirectorySeparator(current)
                    && (current != DirectorySeparatorChar
                        // Check for sequential separators past the first position (we need to keep initial two for UNC/extended)
                        || (i > 0 && i + 1 < path.Length && IsDirectorySeparator(path[i + 1]))))
                {
                    normalized = false;
                    break;
                }
            }

            if (normalized)
                return path;

            var builder = new StringBuilder();

            int start = 0;
            if (IsDirectorySeparator(path[start]))
            {
                start++;
                builder.Append(DirectorySeparatorChar);
            }

            for (int i = start; i < path.Length; i++)
            {
                current = path[i];

                // If we have a separator
                if (IsDirectorySeparator(current))
                {
                    // If the next is a separator, skip adding this
                    if (i + 1 < path.Length && IsDirectorySeparator(path[i + 1]))
                    {
                        continue;
                    }

                    // Ensure it is the primary separator
                    current = DirectorySeparatorChar;
                }

                builder.Append(current);
            }

            return builder.ToString();
        }


        /// <remarks>
        /// Unlike the string overload, this method will not normalize directory separators.
        /// </remarks>
        ReadOnlySpan<char> GetPathRootWin(ReadOnlySpan<char> path)
        {
            if (IsEffectivelyEmpty(path))
                return ReadOnlySpan<char>.Empty;

            int pathRoot = GetRootLengthWin(path);
            return pathRoot <= 0 ? ReadOnlySpan<char>.Empty : path.Slice(0, pathRoot);
        }
        
        static bool IsEffectivelyEmptyWin(ReadOnlySpan<char> path)
        {
            if (path.IsEmpty)
                return true;

            foreach (char c in path)
            {
                if (c != ' ')
                    return false;
            }
            return true;
        }
        
        internal static bool IsExtendedWin(ReadOnlySpan<char> path)
        {
            // While paths like "//?/C:/" will work, they're treated the same as "\\.\" paths.
            // Skipping of normalization will *only* occur if back slashes ('\') are used.
            return path.Length >= WinDevicePrefixLength
                   && path[0] == '\\'
                   && (path[1] == '\\' || path[1] == '?')
                   && path[2] == '?'
                   && path[3] == '\\';
        }
        
        const int WinDevicePrefixLength = 4;
        bool IsDeviceWin(ReadOnlySpan<char> path)
        {
            // If the path begins with any two separators is will be recognized and normalized and prepped with
            // "\??\" for internal usage correctly. "\??\" is recognized and handled, "/??/" is not.
            return IsExtendedWin(path)
                   ||
                   (
                       path.Length >= WinDevicePrefixLength
                       && IsDirectorySeparator(path[0])
                       && IsDirectorySeparator(path[1])
                       && (path[2] == '.' || path[2] == '?')
                       && IsDirectorySeparator(path[3])
                   );
        }
        const int WinUncExtendedPrefixLength = 8;
        const int WinUncPrefixLength = 2;
        bool IsDeviceUNCWin(ReadOnlySpan<char> path)
        {
            return path.Length >= WinUncExtendedPrefixLength
                   && IsDeviceWin(path)
                   && IsDirectorySeparator(path[7])
                   && path[4] == 'U'
                   && path[5] == 'N'
                   && path[6] == 'C';
        }

        private const char WinVolumeSeparatorChar = ':';
        bool IsValidDriveCharWin(char value)
        {
            return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
        }
        int GetRootLengthWin(ReadOnlySpan<char> path)
        {
            int pathLength = path.Length;
            int i = 0;

            bool deviceSyntax = IsDeviceWin(path);
            bool deviceUnc = deviceSyntax && IsDeviceUNCWin(path);

            if ((!deviceSyntax || deviceUnc) && pathLength > 0 && IsDirectorySeparator(path[0]))
            {
                // UNC or simple rooted path (e.g. "\foo", NOT "\\?\C:\foo")
                if (deviceUnc || (pathLength > 1 && IsDirectorySeparator(path[1])))
                {
                    // UNC (\\?\UNC\ or \\), scan past server\share

                    // Start past the prefix ("\\" or "\\?\UNC\")
                    i = deviceUnc ? WinUncExtendedPrefixLength : WinUncPrefixLength;

                    // Skip two separators at most
                    int n = 2;
                    while (i < pathLength && (!IsDirectorySeparator(path[i]) || --n > 0))
                        i++;
                }
                else
                {
                    // Current drive rooted (e.g. "\foo")
                    i = 1;
                }
            }
            else if (deviceSyntax)
            {
                // Device path (e.g. "\\?\.", "\\.\")
                // Skip any characters following the prefix that aren't a separator
                i = WinDevicePrefixLength;
                while (i < pathLength && !IsDirectorySeparator(path[i]))
                    i++;

                // If there is another separator take it, as long as we have had at least one
                // non-separator after the prefix (e.g. don't take "\\?\\", but take "\\?\a\")
                if (i < pathLength && i > WinDevicePrefixLength && IsDirectorySeparator(path[i]))
                    i++;
            }
            else if (pathLength >= 2
                && path[1] == WinVolumeSeparatorChar
                && IsValidDriveCharWin(path[0]))
            {
                // Valid drive specified path ("C:", "D:", etc.)
                i = 2;

                // If the colon is followed by a directory separator, move past it (e.g "C:\")
                if (pathLength > 2 && IsDirectorySeparator(path[2]))
                    i++;
            }

            return i;
        }
    }
}