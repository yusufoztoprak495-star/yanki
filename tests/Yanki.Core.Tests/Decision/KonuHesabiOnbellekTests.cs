using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>
    /// Konuşan başına önbellek (7. haftaya kalan Z3/Z4'ten bağımsız algoritma düzeltmesi): önbellekli hesap önbelleksizle birebir aynı
    /// konuları ve puanları verir; sonuç NPC/önerme ekleme sırasına bağlı değildir; önbellek durum hash'ine girmez.
    /// </summary>
    public class KonuHesabiOnbellekTests
    {
        private static readonly string[] Npcler = { "Ayse", "Can", "Veli", "Zeki", "Mehmet" };

        // Her NPC için ~40 önerme: borclu (tellable), degil(borclu) çelişkileri, kardes bağları ve anlatti gözlemleri.
        private static (ZihinDeposu Zihinler, OlayGunlugu Gunluk) Kur(IEnumerable<string> npcSirasi, bool onermeTers = false)
        {
            var zihinler = new ZihinDeposu();
            var gunluk = new OlayGunlugu();
            foreach (var id in npcSirasi)
            {
                var sosyallik = 0.2m + 0.2m * Array.IndexOf(Npcler, id);
                zihinler.Ekle(new Zihin(id, new Kisilik(0.5m, 0.5m, 0.5m, sosyallik, 0.5m, 0.5m), new ZihinAyarlari { KanitSonumuTickBasina = 0m }));
            }

            foreach (var id in Npcler)
            {
                var onermeler = new List<string>();
                for (var i = 0; i < 14; i++)
                {
                    onermeler.Add("borclu(" + Npcler[i % 5] + "," + Npcler[(i + 2) % 5] + "," + (100 + i) + ")");
                }

                onermeler.Add("degil(borclu(" + Npcler[0] + "," + Npcler[2] + ",100))");   // çelişki
                onermeler.Add("kardes(Mehmet,Can)");
                onermeler.Add("kardes(Ayse,Zeki)");
                for (var i = 0; i < 8; i++)
                {
                    onermeler.Add("anlatti(" + Npcler[i % 5] + "," + Npcler[(i + 1) % 5] + ")");
                    onermeler.Add("geldi(" + Npcler[i % 5] + ",han" + i + ")");
                }

                // Kanıt gücü ve türü önerme sırasına (indeks) bağlıdır, ekleme sırasına değil; tüm olaylar aynı tick'te (günlük tick'i geriye gitmez).
                var ciftler = onermeler.Select((o, i) => (O: o, I: i)).ToList();
                if (onermeTers)
                {
                    ciftler.Reverse();
                }

                foreach (var (o, i) in ciftler)
                {
                    const int tick = 5;
                    var olay = new Olay(gunluk.SonrakiId, tick, gunluk.SonrakiSira(tick), "gozlem", "x", id, "han", new KeyValuePair<string, string>[0], new long[0], "player");
                    gunluk.Ekle(olay);
                    Uygula(zihinler, gunluk, ZihinOlaylari.HafizaEklendi(gunluk.SonrakiId, tick, gunluk.SonrakiSira(tick), id, olay.Id, 0.5m, new[] { olay.Id }, "algi"));
                    var tur = i % 3 == 0 ? "duydu" : "gordu";
                    var kaynak = tur == "duydu" ? Npcler[(i + 1) % 5] : id;
                    Uygula(zihinler, gunluk, ZihinOlaylari.KanitEklendi(
                        gunluk.SonrakiId, tick, gunluk.SonrakiSira(tick), id, o, tur, kaynak, 0.4m + 0.01m * (i % 50), new[] { olay.Id }, "algi", "han",
                        tur == "duydu" ? Npcler[(i + 3) % 5] : string.Empty));
                }
            }

            return (zihinler, gunluk);
        }

        private static void Uygula(ZihinDeposu zihinler, OlayGunlugu gunluk, Olay olay)
        {
            gunluk.Ekle(olay);
            zihinler.Uygula(olay);
        }

        private static readonly OnermeDeseni[] Desenler =
        {
            OnermeDeseni.Ayristir("borclu(*,*,*)"), OnermeDeseni.Ayristir("degil(borclu(*,*,*))"), OnermeDeseni.Ayristir("kardes(*,*)"),
        };

        private static readonly string[] Yalanlar =
        {
            "degil(borclu(Ayse,Veli,100))", "borclu(Ayse,Veli,100)", "degil(borclu(Can,Zeki,101))", "kardes(Mehmet,Zeki)", "degil(kardes(Mehmet,Can))",
        };

        private static string Imza(ZihinDeposu zihinler, OlayGunlugu gunluk, bool onbellekli, IEnumerable<string> sira, bool kimlikli = true)
        {
            var hesap = new KonuHesabi(zihinler, gunluk, 60, onbellekli: onbellekli);
            var sb = new StringBuilder();
            foreach (var konusan in sira.OrderBy(x => x, StringComparer.Ordinal))
            {
                foreach (var dinleyen in Npcler.Concat(new[] { "oyuncu" }).Where(d => d != konusan))
                {
                    var dogru = hesap.Dogru(konusan, dinleyen, Desenler);
                    sb.Append(konusan).Append('>').Append(dinleyen).Append(" sinir=").Append(dogru.Sinir).Append(" maks=")
                        .Append(hesap.EnYuksekKonuPuani(konusan, dinleyen, Desenler)).Append('\n');
                    foreach (var k in dogru.Konular)
                    {
                        sb.Append("  ").Append(k.Onerme).Append('|').Append(k.Puan).Append('|').Append(k.Onem).Append('|').Append(k.Yenilik).Append('|')
                            .Append(k.Ilgi).Append('|').Append(k.Kok).Append(kimlikli ? "|" + k.Kanit.OlayId : string.Empty).Append('\n');
                    }

                    foreach (var e in dogru.Elenenler)
                    {
                        sb.Append("  x ").Append(e.Onerme).Append(':').Append(e.Neden).Append('\n');
                    }

                    for (ulong seed = 1; seed <= 3; seed++)
                    {
                        var yalan = hesap.Yalan(konusan, dinleyen, Yalanlar, seed);
                        sb.Append("  y").Append(seed).Append(' ').Append(string.Join(",", yalan.Konular.Select(k => k.Onerme + (kimlikli ? "@" + k.Kanit.OlayId : string.Empty))))
                            .Append(" x=").Append(yalan.Elenenler.Count).Append('\n');
                    }
                }
            }

            return sb.ToString();
        }

        [Fact]
        public void Onbellekli_hesap_onbelleksizle_ayni_konulari_ve_puanlari_verir()
        {
            var (zihinler, gunluk) = Kur(Npcler);

            var referans = Imza(zihinler, gunluk, false, Npcler);
            var onbellekli = Imza(zihinler, gunluk, true, Npcler);

            Assert.Contains("Ayse>Can", referans);
            Assert.True(referans.Contains("borclu("), "fikstür konu üretmeli");
            Assert.Equal(referans, onbellekli);
        }

        [Fact]
        public void Sonuc_npc_ve_onerme_ekleme_sirasindan_bagimsizdir()
        {
            var (z1, g1) = Kur(Npcler);
            var (z2, g2) = Kur(Npcler.Reverse(), onermeTers: true);

            // Olay kimlikleri ekleme sırasına göre değişir; imza kimliksiz üretilir (önerme, puan, önem, yenilik, ilgi, kök).
            var a = Imza(z1, g1, true, Npcler, kimlikli: false);
            var b = Imza(z2, g2, true, Npcler, kimlikli: false);

            Assert.Equal(a, b);
        }

        [Fact]
        public void Onbellek_zihin_durumunu_degistirmez()
        {
            var (zihinler, gunluk) = Kur(Npcler);
            var once = zihinler.DurumHash();
            var sayi = gunluk.Sayi;

            Imza(zihinler, gunluk, true, Npcler);

            Assert.Equal(once, zihinler.DurumHash());
            Assert.Equal(sayi, gunluk.Sayi);
        }

        [Fact]
        public void Her_hesap_ornegi_kendi_onbellegini_tasir_tick_degisince_sifirdan_kurulur()
        {
            var (zihinler, gunluk) = Kur(Npcler);
            var hesap1 = new KonuHesabi(zihinler, gunluk, 60, onbellekli: true);
            var ilk = hesap1.Dogru("Ayse", "Can", Desenler).Konular.Select(k => k.Onerme + k.Puan).ToList();

            // Yeni zihin olayı + yeni örnek: eski önbellek kullanılmaz, yeni durum görülür.
            Uygula(zihinler, gunluk, ZihinOlaylari.KanitEklendi(gunluk.SonrakiId, 61, gunluk.SonrakiSira(61), "Ayse", "borclu(Ayse,Can,999)", "gordu", "Ayse", 0.9m, new long[0], "algi"));
            var yeni = new KonuHesabi(zihinler, gunluk, 61, onbellekli: true).Dogru("Ayse", "Can", Desenler).Konular.Select(k => k.Onerme + k.Puan).ToList();

            Assert.DoesNotContain(ilk, x => x.StartsWith("borclu(Ayse,Can,999)", StringComparison.Ordinal));
            Assert.Contains(yeni, x => x.StartsWith("borclu(Ayse,Can,999)", StringComparison.Ordinal));
        }
    }
}
