using System;
using System.IO;

namespace Yanki.Core.Tests
{
    /// <summary>Testlerin repo köküne göre dosya aramasını sağlar (kök: Yanki.slnx'in bulunduğu klasör).</summary>
    internal static class RepoRoot
    {
        public static string Find()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "Yanki.slnx")))
            {
                current = current.Parent;
            }

            return current?.FullName ?? throw new InvalidOperationException("Repo kökü (Yanki.slnx) bulunamadı.");
        }

        public static string Combine(params string[] parts) =>
            Path.Combine(Find(), Path.Combine(parts));
    }
}
