using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Yanki.Core.Mind;
using Yanki.Core.Simulation;
using Yanki.Core.World;

namespace Yanki.Cli
{
    /// <summary>
    /// <c>yanki query</c> çıktısı: bir NPC'nin durumu, ilişkileri (fikir dökümü ve modifier'lar), inançları ve hafızası.
    /// Her katkının yanında neden olayının kimliği yazar; <c>yanki why &lt;olayId&gt;</c> ile zincire inilir.
    /// </summary>
    internal static class NpcDurumu
    {
        public static void Yaz(Simulasyon sim, string npc, TextWriter w)
        {
            var zihin = sim.Zihinler[npc];
            var tick = sim.SimdikiTick;
            var konum = sim.Dunya.TekDeger("konum!", npc) ?? "?";

            w.WriteLine($"NPC: {npc} (konum: {konum}, tick {Sayi(tick)})");

            if (zihin.Duygu != null && zihin.BaskinDuyguSiddeti(tick) > 0m)
            {
                var hedef = zihin.Duygu.Hedef.Length > 0 ? ", hedef: " + zihin.Duygu.Hedef : string.Empty;
                w.WriteLine($"ruh hali: {zihin.Duygu.Tur} {Sayi(zihin.BaskinDuyguSiddeti(tick))}{hedef} (#{Sayi(zihin.Duygu.NedenOlayId)})");
            }
            else
            {
                w.WriteLine("ruh hali: yok");
            }

            w.WriteLine($"stres: {Sayi(zihin.Stres(tick))}");

            var kisilik = new List<string>();
            foreach (var ozellik in Kisilik.Ozellikler)
            {
                kisilik.Add(ozellik + "=" + Sayi(zihin.Kisilik.Deger(ozellik)));
            }

            w.WriteLine("kişilik: " + string.Join(" ", kisilik));

            w.WriteLine("-- ilişkiler --");
            var hedefler = new List<string>(sim.Dunya.Varliklar(VarlikTuru.Npc));
            hedefler.AddRange(sim.Dunya.Varliklar(VarlikTuru.Oyuncu));
            hedefler.Sort(string.CompareOrdinal);
            foreach (var hedef in hedefler)
            {
                if (hedef == npc)
                {
                    continue;
                }

                var dokum = zihin.FikirDokumu(hedef, tick);
                w.WriteLine($"fikir {hedef}: {Sayi(dokum.Toplam)} ({zihin.FikirBandi(hedef, tick)})");
                foreach (var k in dokum.Katkilar)
                {
                    if (k.Deger == 0m)
                    {
                        continue;
                    }

                    var neden = k.NedenOlayId > 0 ? " ← #" + Sayi(k.NedenOlayId) : string.Empty;
                    var kalan = k.KalanTick >= 0 ? ", kalan " + Sayi(k.KalanTick) + " tick" : string.Empty;
                    w.WriteLine($"  {k.Kaynak} {Isaretli(k.Deger)}{neden}{kalan}");
                }

                if (!string.IsNullOrEmpty(dokum.KirpmaMetni))
                {
                    w.WriteLine("  " + dokum.KirpmaMetni);
                }
            }

            w.WriteLine("-- inançlar --");
            if (zihin.Onermeler.Count == 0)
            {
                w.WriteLine("(yok)");
            }

            foreach (var onerme in zihin.Onermeler)
            {
                var durum = zihin.Inanilan(onerme, tick) ? "inanılıyor" : "inanılmıyor";
                w.WriteLine($"{onerme}: güven {Sayi(zihin.Guven(onerme, tick))}, {durum}");
                foreach (var kanit in zihin.Kanitlar(onerme))
                {
                    w.WriteLine($"  {KanitTurleri.Ad(kanit.Tur)} ← {kanit.Kaynak} (#{Sayi(kanit.OlayId)}, tick {Sayi(kanit.Tick)}, güç {Sayi(kanit.Guc)})");
                }
            }

            w.WriteLine("-- hafıza --");
            var hafiza = zihin.Hafiza(tick);
            if (hafiza.Count == 0)
            {
                w.WriteLine("(yok)");
            }

            foreach (var h in hafiza)
            {
                var tur = sim.Gunluk.Var(h.OlayId) ? sim.Gunluk[h.OlayId].Tur : "?";
                w.WriteLine($"#{Sayi(h.OlayId)} {tur} (önem {Sayi(h.Onem)}, tick {Sayi(h.Tick)})");
            }
        }

        private static string Sayi(long n) => n.ToString(CultureInfo.InvariantCulture);

        private static string Sayi(decimal n) => KanonikYazici.Sayi(n);

        private static string Isaretli(decimal n) => (n > 0m ? "+" : string.Empty) + KanonikYazici.Sayi(n);
    }
}
