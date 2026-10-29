using System.Collections.Generic;
using Yanki.Core.Events;
using Yanki.Core.World;

namespace Yanki.Core.Tests.World
{
    /// <summary>Dünya testlerinde dünya olaylarını (Id, Tick, Sira otomatik) kurar. Tüm olaylar tick 0'dadır.</summary>
    internal sealed class OlayUretici
    {
        private long _sonrakiId = 1;

        private Olay Yap(string tur, params KeyValuePair<string, string>[] yuk)
        {
            var id = _sonrakiId++;
            return new Olay(id, 0, (int)(id - 1), tur, "", "", "", yuk, new long[0], "test");
        }

        private static KeyValuePair<string, string> Y(string k, string v) => new KeyValuePair<string, string>(k, v);

        public Olay Varlik(string id, string tur) => Yap("varlik_eklendi", Y("id", id), Y("tur", tur));

        public Olay GercekEkle(string gercek) => Yap("gercek_eklendi", Y("gercek", gercek));

        public Olay GercekKaldir(string gercek) => Yap("gercek_kaldirildi", Y("gercek", gercek));

        public Olay Bagla(string a, string b) => Yap("mekan_baglandi", Y("a", a), Y("b", b));

        public Olay Baska() => Yap("oyuncu_konustu");

        /// <summary>Küçük, sabit dünya: 3 NPC, 2 mekan, birkaç gerçek (altın sabit testinin de kullandığı).</summary>
        public static Dunya SabitDunya()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();
            dunya.Uygula(u.Varlik("han", "mekan"));
            dunya.Uygula(u.Varlik("pazar", "mekan"));
            dunya.Uygula(u.Varlik("Ali", "npc"));
            dunya.Uygula(u.Varlik("Ayse", "npc"));
            dunya.Uygula(u.Varlik("Mehmet", "npc"));
            dunya.Uygula(u.Bagla("han", "pazar"));
            dunya.Uygula(u.GercekEkle("konum!(Ali,han)"));
            dunya.Uygula(u.GercekEkle("konum!(Ayse,han)"));
            dunya.Uygula(u.GercekEkle("konum!(Mehmet,pazar)"));
            dunya.Uygula(u.GercekEkle("borclu(Mehmet,Ayse,500)"));
            return dunya;
        }
    }
}
