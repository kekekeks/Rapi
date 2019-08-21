using System;
using System.Text;

namespace Rapi
{
    public class RapiPath
    {
        private readonly bool _isUnix;
        
        public char[] InvalidFileNameChars { get; }
        public char[] InvalidPathChars { get; }
        public char DirectorySeparatorChar { get; }
        public char AltDirectorySeparatorChar { get; }
        
        public RapiPath(RapiPlatformInfo platformInfo)
        {
            if (platformInfo.IsUnix)
            {
                InvalidFileNameChars = new[] {'\0', '/'};
                InvalidPathChars = new[] {'\0'};
                DirectorySeparatorChar = AltDirectorySeparatorChar = '/';
                _isUnix = true;
            }
            else
            {
                InvalidFileNameChars = new[]
                {
                    '\"', '<', '>', '|', '\0',
                    (char) 1, (char) 2, (char) 3, (char) 4, (char) 5, (char) 6, (char) 7, (char) 8, (char) 9, (char) 10,
                    (char) 11, (char) 12, (char) 13, (char) 14, (char) 15, (char) 16, (char) 17, (char) 18, (char) 19,
                    (char) 20,
                    (char) 21, (char) 22, (char) 23, (char) 24, (char) 25, (char) 26, (char) 27, (char) 28, (char) 29,
                    (char) 30,
                    (char) 31, ':', '*', '?', '\\', '/'
                };
                InvalidPathChars = new[]
                {
                    '|', '\0',
                    (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
                    (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
                    (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
                    (char)31
                };
                DirectorySeparatorChar = '\\';
                AltDirectorySeparatorChar = '/';
            }
        }
        
        public bool IsPathRooted(string path)
        {
            if (_isUnix)
                return path.Length > 0 && path[0] == DirectorySeparatorChar;
            int length = path.Length;
            return length >= 1 && IsDirectorySeparator(path[0]) || 
                   length >= 2 && IsLatin(path[0]) && path[1] == ':';
        }
        
        public string Combine(params string[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            int maxSize = 0;
            int firstComponent = 0;

            // We have two passes, the first calculates how large a buffer to allocate and does some precondition
            // checks on the paths passed in. The second actually does the combination.

            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i] == null)
                {
                    throw new ArgumentNullException(nameof(paths));
                }

                if (paths[i].Length == 0)
                {
                    continue;
                }

                if (IsPathRooted(paths[i]))
                {
                    firstComponent = i;
                    maxSize = paths[i].Length;
                }
                else
                {
                    maxSize += paths[i].Length;
                }

                char ch = paths[i][paths[i].Length - 1];
                if (!IsDirectorySeparator(ch))
                    maxSize++;
            }

            var builder = new StringBuilder();
            builder.EnsureCapacity(maxSize);

            for (int i = firstComponent; i < paths.Length; i++)
            {
                if (paths[i].Length == 0)
                {
                    continue;
                }

                if (builder.Length == 0)
                {
                    builder.Append(paths[i]);
                }
                else
                {
                    char ch = builder[builder.Length - 1];
                    if (!IsDirectorySeparator(ch))
                    {
                        builder.Append(DirectorySeparatorChar);
                    }

                    builder.Append(paths[i]);
                }
            }

            return builder.ToString();
        }
        
        private bool IsDirectorySeparator(char ch) => ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar;

        private static bool IsLatin(char ch) => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'); 
    }
}