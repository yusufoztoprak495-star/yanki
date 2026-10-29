using System.Collections.Generic;
using Yanki.Core.Events;

namespace Yanki.Core.Tests.Events
{
    /// <summary>Olay testlerinde kısa olay kurmak için yardımcılar.</summary>
    internal static class EventTestYardimcilari
    {
        public static readonly KeyValuePair<string, string>[] BosYuk = new KeyValuePair<string, string>[0];

        public static Olay Yap(long id, long tick, int sira, params long[] nedenler) =>
            new Olay(id, tick, sira, "test", "Ali", "", "han", BosYuk, nedenler, "kural_x");

        public static Olay YapTur(long id, long tick, int sira, string tur, params KeyValuePair<string, string>[] yuk) =>
            new Olay(id, tick, sira, tur, "", "", "", yuk, new long[0], "kural_x");

        public static KeyValuePair<string, string> Y(string anahtar, string deger) =>
            new KeyValuePair<string, string>(anahtar, deger);
    }
}
