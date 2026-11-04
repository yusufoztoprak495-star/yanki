using System;
using System.Collections.Generic;
using Xunit;
using Yanki.Core.World;
using Yanki.Dialogue.Gramer;

namespace Yanki.Dialogue.Tests.Gramer
{
    /// <summary>
    /// Tracery benzeri mini gramer (8. hafta, F kararı): <c>#kural#</c> açılımı, kural başına seçenekler, en fazla 3 iç içe seviye, döngü yükleme hatası.
    /// Rastgelelik yalnızca anahtarlı RNG'den; kültüre bağlı dönüşüm yok.
    /// </summary>
    public class GramerAcilimiTests
    {
        private static GramerTablosu Tablo(params (string Kural, string[] Secenekler)[] kurallar)
        {
            var t = new GramerTablosu();
            foreach (var (k, s) in kurallar)
            {
                t.Ekle(k, s);
            }

            return t;
        }

        private static SortedDictionary<string, MetinOlgusu> Olgular() => new SortedDictionary<string, MetinOlgusu>(StringComparer.Ordinal)
        {
            ["konusan"] = MetinOlgusu.Ad("Hatice", ozelAd: true),
            ["dinleyen"] = MetinOlgusu.Ad("Ayşe", ozelAd: true),
            ["hafiza.fail"] = MetinOlgusu.Ad("hancı", ozelAd: false),
            ["hafiza.zaman"] = MetinOlgusu.Metin("dün"),
        };

        private static AnahtarliRng Rng(string anahtar = "a") => AnahtarliRng.Icin(42, 1, anahtar);

        [Fact]
        public void Kural_acar_ve_ad_olgusuna_ek_getirir()
        {
            var tablo = Tablo(("selam", new[] { "Merhaba" }));

            Assert.Equal("Merhaba Ayşe'ye", GramerAcilimi.Ac("#selam# #dinleyen.yonelme#", tablo, Olgular(), Rng()));
        }

        [Fact]
        public void Metin_olgusu_ve_ad_olgusu_birlikte_dogru_cumle_kurar()
        {
            var cumle = GramerAcilimi.Ac("#hafiza.zaman# #hafiza.fail.belirtme# gördün, #dinleyen.bulunma# da konuştum.", Tablo(), Olgular(), Rng());

            Assert.Equal("dün hancıyı gördün, Ayşe'de da konuştum.", cumle);
        }

        [Fact]
        public void Ic_ice_kural_uc_seviyeye_kadar_acilir()
        {
            var tablo = Tablo(("a", new[] { "x #b#" }), ("b", new[] { "y #c#" }), ("c", new[] { "z" }));

            Assert.Equal("[x y z]", "[" + GramerAcilimi.Ac("#a#", tablo, Olgular(), Rng()) + "]");
            Assert.Empty(tablo.Analiz(new[] { "a" }));
        }

        [Fact]
        public void Dort_seviye_ic_ice_yukleme_hatasidir()
        {
            var tablo = Tablo(("a", new[] { "#b#" }), ("b", new[] { "#c#" }), ("c", new[] { "#d#" }), ("d", new[] { "son" }));

            var hatalar = tablo.Analiz(new[] { "a" });

            Assert.Contains(hatalar, h => h.Contains("3 seviye", StringComparison.Ordinal) && h.Contains("\"a\"", StringComparison.Ordinal));
        }

        [Fact]
        public void Dongu_yukleme_hatasidir_ve_zinciri_gosterir()
        {
            var tablo = Tablo(("a", new[] { "#b#" }), ("b", new[] { "#a#" }));

            var hatalar = tablo.Analiz(new[] { "a" });

            Assert.Contains(hatalar, h => h.Contains("döngü", StringComparison.Ordinal) && h.Contains("a → b → a", StringComparison.Ordinal));
        }

        [Fact]
        public void Kendine_gonderme_de_dongudur()
        {
            var hatalar = Tablo(("a", new[] { "bir #a#" })).Analiz(new[] { "a" });

            Assert.Contains(hatalar, h => h.Contains("döngü", StringComparison.Ordinal));
        }

        [Fact]
        public void Tanimsiz_sembol_statik_analizde_ve_calisma_aninda_hatadir()
        {
            var tablo = Tablo(("a", new[] { "#yok#" }));

            Assert.Contains(tablo.Analiz(new[] { "a" }), h => h.Contains("yok", StringComparison.Ordinal));
            Assert.Throws<InvalidOperationException>(() => GramerAcilimi.Ac("#yok#", Tablo(), Olgular(), Rng()));
        }

        [Fact]
        public void Metin_olgusuna_ve_kurala_ek_getirilemez()
        {
            var tablo = Tablo(("selam", new[] { "Merhaba" }));

            Assert.Throws<InvalidOperationException>(() => GramerAcilimi.Ac("#hafiza.zaman.yonelme#", tablo, Olgular(), Rng()));
            Assert.Throws<InvalidOperationException>(() => GramerAcilimi.Ac("#selam.yonelme#", tablo, Olgular(), Rng()));
            Assert.Throws<InvalidOperationException>(() => GramerAcilimi.Ac("#dinleyen.tamlayan#", tablo, Olgular(), Rng()));
        }

        [Fact]
        public void Rakamla_biten_ad_olgusuna_ek_getirilmez_sessiz_yanlis_ek_uretilmez()
        {
            var olgular = Olgular();
            olgular["dinleyen"] = MetinOlgusu.Ad("500", ozelAd: false);

            var hata = Assert.Throws<InvalidOperationException>(() => GramerAcilimi.Ac("#dinleyen.yonelme#", Tablo(), olgular, Rng()));
            Assert.Contains("500", hata.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Ayni_anahtar_ayni_secimi_farkli_anahtar_farkli_secimleri_verir()
        {
            var tablo = Tablo(("selam", new[] { "Merhaba", "Selam", "Günaydın", "İyi günler" }));
            var farkli = new SortedSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < 40; i++)
            {
                var anahtar = "olay:" + i;
                var ilk = GramerAcilimi.Ac("#selam#", tablo, Olgular(), Rng(anahtar));
                Assert.Equal(ilk, GramerAcilimi.Ac("#selam#", tablo, Olgular(), Rng(anahtar)));
                farkli.Add(ilk);
            }

            Assert.Equal(4, farkli.Count);   // dört seçeneğin hepsi 40 anahtarda en az bir kez seçilir
        }

        [Fact]
        public void Acilim_kultur_bagimsizdir()
        {
            var onceki = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                var tablo = Tablo(("selam", new[] { "Işık", "İpek", "Iğdır" }));
                var referans = new List<string>();
                for (var i = 0; i < 12; i++)
                {
                    referans.Add(GramerAcilimi.Ac("#selam# #dinleyen.yonelme#", tablo, Olgular(), Rng("k" + i)));
                }

                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
                for (var i = 0; i < 12; i++)
                {
                    Assert.Equal(referans[i], GramerAcilimi.Ac("#selam# #dinleyen.yonelme#", tablo, Olgular(), Rng("k" + i)));
                }
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = onceki;
            }
        }

        [Fact]
        public void Etiket_ayirici_kapanmamis_isareti_hata_sayar()
        {
            Assert.Throws<FormatException>(() => GramerAcilimi.Etiketler("selam #dinleyen"));
            var etiketler = GramerAcilimi.Etiketler("a #x# b #y.z.w# c");
            Assert.Equal(new[] { "x", "y.z.w" }, new[] { etiketler[0].Ham, etiketler[1].Ham });
        }
    }
}
