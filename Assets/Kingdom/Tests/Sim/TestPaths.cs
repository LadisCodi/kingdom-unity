using System;
using System.IO;

namespace Kingdom.Tests.Sim
{
    // Where the project's files are, from the Unity editor (cwd = project root) or from `dotnet test`.
    public static class TestPaths
    {
        private static string _root;

        public static string Root => _root ??= FindRoot();

        public static string Data => Path.Combine(Root, "Assets", "Kingdom", "Data");

        public static string Golden => Path.Combine(Root, "Assets", "Kingdom", "Tests", "Golden");

        private static string FindRoot()
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                var dir = new DirectoryInfo(start);
                while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "Kingdom"))) dir = dir.Parent;
                if (dir != null) return dir.FullName;
            }

            throw new InvalidOperationException("The Unity project root was not found above the working directory.");
        }
    }
}
