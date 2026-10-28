using System;
using System.Reflection;
using Yanki.Core;

namespace Yanki.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var version = typeof(YankiInfo).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? "bilinmiyor";

            if (args.Length == 1 && (args[0] == "--version" || args[0] == "-v"))
            {
                Console.WriteLine($"yanki {version}");
                return 0;
            }

            Console.WriteLine($"{YankiInfo.Name} {version}");
            Console.WriteLine("Henüz komut yok. Simülasyon komutları (run, why, diff, validate, coverage) ilerleyen haftalarda gelecek.");
            Console.WriteLine("Kullanım: yanki --version");
            return args.Length == 0 ? 0 : 1;
        }
    }
}
