using System;

namespace Rapi
{
    public partial class RapiPath
    {
        string GetPathRootUnix(string path)
        {
            if (IsEffectivelyEmpty(path.AsSpan())) return null;

            return IsPathRooted(path) ? DirectorySeparatorCharAsString : string.Empty;
        }

        ReadOnlySpan<char> GetPathRootUnix(ReadOnlySpan<char> path)
        {
            return IsEffectivelyEmpty(path) && IsPathRooted(path) ? DirectorySeparatorCharAsString.AsSpan() : ReadOnlySpan<char>.Empty;
        }
        
        internal static bool IsEffectivelyEmptyUnix(ReadOnlySpan<char> path)
        {
            return path.IsEmpty;
        }
    }
}