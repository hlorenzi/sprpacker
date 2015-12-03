using System;
using System.IO;


public static class PathUtil
{
    public static String MakeRelativePath(String fromPath, String toPath)
    {
        if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
        if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

        Uri fromUri = new Uri(fromPath);
        Uri toUri = new Uri(toPath);

        if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

        Uri relativeUri = fromUri.MakeRelativeUri(toUri);
        String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (toUri.Scheme.ToUpperInvariant() == "FILE")
        {
            relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
        return relativePath;
    }


    public static string MakeAbsolutePath(string absPath, string relPath)
    {
        return Path.Combine(absPath, relPath);
    }
}
