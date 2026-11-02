using System;
using System.Text;

namespace Yanki.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            return KomutCalistirici.Calistir(args, Console.Out, Console.Error);
        }
    }
}
