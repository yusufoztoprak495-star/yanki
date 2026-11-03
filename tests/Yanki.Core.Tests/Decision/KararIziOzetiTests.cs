using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Events;

namespace Yanki.Core.Tests.Decision
{
    /// <summary><c>yanki why</c> ve <c>yanki query</c> için karar izinin okunur satırları: adaylar, puan bileşenleri, havuz, seçim, kaynaklar.</summary>
    public class KararIziOzetiTests
    {
        // a|Veli: taban 2 × lojistik(cesaret) = 1.0; b: taban 1.4. N=1 → b seçilir.
        private static KararIzi Tam(int n = 1, string aEk = "")
        {
            var eylemler = KararFikstur.Yukle(
                KararFikstur.Eylem("a", "npc", "\"olcutler\": [ { \"alan\": \"kisilik(cesaret)\", \"egri\": { \"tur\": \"lojistik\", \"k\": 10, \"m\": 0.5 } } ]" + aEk, 2m),
                KararFikstur.Eylem("b", taban: 1.4m));
            var sim = KararFikstur.Sim(eylemler);
            var adaylar = new[] { new Aday(eylemler[0], "Veli"), new Aday(eylemler[1], string.Empty) };
            var elenen = new[] { new ElenenAday(eylemler[0], "Can", "on_kosul #1 tutmadi: ayni_mekanda(eylem.hedef) == true") };
            return new UtilitySelector(new SecimAyarlari(n, 0.7m)).Sec(KararFikstur.Baglam(sim, "Ali"), adaylar, elenen).Iz;
        }

        // Tam iz: replay ile yeniden üretilmiş izin görünümü (bileşenler dahil).
        private static string[] Satirlar(KararIzi iz) => KararIziOzeti.Satirlar(KararOlayi.Yap(5, iz.Tick, 0, iz, "han"), iz).ToArray();

        // Kısa iz: yalnızca günlükteki olaydan.
        private static string[] KisaSatirlar(KararIzi iz) => KararIziOzeti.Satirlar(KararOlayi.Yap(5, iz.Tick, 0, iz, "han")).ToArray();

        private static string[] Satirlar(Olay olay) => KararIziOzeti.Satirlar(olay).ToArray();

        [Fact]
        public void Baslik_npc_tick_ve_secim_ayarini_gosterir()
        {
            var satirlar = Satirlar(Tam());

            Assert.StartsWith("karar izi: Ali, tick 1", satirlar[0]);
            Assert.Contains("ilk 1", satirlar[0]);
            Assert.Contains("eşik 0.7", satirlar[0]);
        }

        [Fact]
        public void Secilen_ve_havuz_gorunur()
        {
            var satirlar = Satirlar(Tam());

            Assert.Contains(satirlar, s => s.Trim().StartsWith("seçilen: b", System.StringComparison.Ordinal));
            Assert.Contains(satirlar, s => s.Contains("havuz: b (1.4)"));
        }

        [Fact]
        public void Puanlanan_adayin_butun_bilesenleri_okunur_satirdadir()
        {
            var satir = Satirlar(Tam()).Single(s => s.Contains("aday a → Veli"));

            Assert.Contains("puan 1", satir);
            Assert.Contains("taban 2", satir);
            Assert.Contains("kisilik(cesaret)", satir);
            Assert.Contains("girdi 0.5", satir);
            Assert.Contains("normal 0.5", satir);
            Assert.Contains("lojistik(k=10, m=0.5)", satir);
            Assert.Contains("çıktı 0.5", satir);
            Assert.Contains("kişilik 1", satir);
            Assert.Contains("fikir 1", satir);
            Assert.Contains("ruh hali 1", satir);
            Assert.Contains("sıra 2", satir);
        }

        [Fact]
        public void Hedefsiz_aday_hedef_oku_olmadan_yazilir()
        {
            var satir = Satirlar(Tam()).Single(s => s.Contains("aday b"));

            Assert.DoesNotContain("→", satir);
            Assert.Contains("puan 1.4", satir);
            Assert.Contains("havuzda", satir);
        }

        [Fact]
        public void Elenen_aday_nedeniyle_yazilir()
        {
            var satir = Satirlar(Tam()).Single(s => s.Contains("aday a → Can"));

            Assert.Contains("elendi", satir);
            Assert.Contains("on_kosul #1 tutmadi: ayni_mekanda(eylem.hedef) == true", satir);
        }

        [Fact]
        public void Zar_atildiysa_gorunur()
        {
            var satirlar = Satirlar(Tam(n: 2));

            Assert.Contains(satirlar, s => s.Contains("zar "));
        }

        [Fact]
        public void Zar_atilmadiysa_zar_satiri_yoktur()
        {
            Assert.DoesNotContain(Satirlar(Tam(n: 1)), s => s.Contains("zar "));
        }

        [Fact]
        public void Celisen_ozellikler_secilen_eylem_icin_gosterilir()
        {
            var eylemler = KararFikstur.Yukle(KararFikstur.Eylem("c", ek: "\"celisen_ozellikler\": [ \"durustluk\" ], \"stres_tabani\": 10"));
            var sim = KararFikstur.Sim(eylemler);
            var iz = new UtilitySelector().Sec(KararFikstur.Baglam(sim, "Ali"), new[] { new Aday(eylemler[0], string.Empty) }, new ElenenAday[0]).Iz;

            var satirlar = Satirlar(iz);

            Assert.Contains(satirlar, s => s.Contains("çelişen özellikler: durustluk"));
        }

        [Fact]
        public void Kaynak_olaylar_ve_kaynaksiz_girdiler_gosterilir()
        {
            var iz = new KararIzi(
                "Ali", 4, new SecimAyarlari(), new AdayIzi[0], new string[0], null, null, new string[0],
                new long[] { 9, 3 }, new[] { "stres(): kaynak yok" });

            var satirlar = Satirlar(iz);

            Assert.Contains(satirlar, s => s.Contains("kaynak olaylar: #3, #9"));
            Assert.Contains(satirlar, s => s.Contains("kaynaksız girdi: stres(): kaynak yok"));
        }

        [Fact]
        public void Secim_yoksa_nedeni_yazilir()
        {
            var iz = new KararIzi("Ali", 4, new SecimAyarlari(), new AdayIzi[0], new string[0], null, null, new string[0], new long[0], new string[0]);

            var satirlar = Satirlar(iz);

            Assert.Contains(satirlar, s => s.Contains("seçilen: yok"));
        }

        [Fact]
        public void Bozuk_yuk_okunamadi_satiri_verir()
        {
            var bozuk = new Olay(3, 1, 0, "karar_verildi", "Ali", string.Empty, "han", new[] { new KeyValuePair<string, string>("iz", "{bozuk") }, new long[0], "karar");

            var satirlar = Satirlar(bozuk);

            Assert.Single(satirlar);
            Assert.Contains("okunamadı", satirlar[0]);
        }

        // ---- kısa iz görünümü (günlükten) ----

        [Fact]
        public void Kisa_iz_basligi_havuzu_puanlari_zari_ve_sayilari_gosterir()
        {
            var satirlar = KisaSatirlar(Tam(n: 2));

            Assert.StartsWith("karar izi (kısa): Ali, tick 1", satirlar[0]);
            Assert.Contains(satirlar, s => s.Contains("havuz: b (1.4), a → Veli (1)"));
            Assert.Contains(satirlar, s => s.Contains("adaylar: 2 puanlandı, 1 elendi"));
            Assert.Contains(satirlar, s => s.Contains("seçilen: ") && s.Contains("(zar "));
        }

        [Fact]
        public void Kisa_iz_bilesenleri_ve_elenme_nedenlerini_yazmaz_ama_tam_izin_replayle_uretilecegini_soyler()
        {
            var satirlar = KisaSatirlar(Tam());

            Assert.DoesNotContain(satirlar, s => s.Contains("aday a → Veli:"));
            Assert.DoesNotContain(satirlar, s => s.Contains("on_kosul"));
            Assert.Contains(satirlar, s => s.Contains("tam iz replay ile yeniden üretilir"));
        }

        [Fact]
        public void Kisa_iz_kaynaklari_ve_celisen_ozellikleri_yazar()
        {
            var eylemler = KararFikstur.Yukle(KararFikstur.Eylem("c", ek: "\"celisen_ozellikler\": [ \"durustluk\" ], \"stres_tabani\": 10"));
            var sim = KararFikstur.Sim(eylemler);
            var iz = new UtilitySelector().Sec(KararFikstur.Baglam(sim, "Ali"), new[] { new Aday(eylemler[0], string.Empty) }, new ElenenAday[0]).Iz;
            var kaynakli = new KararIzi(iz.Npc, iz.Tick, iz.Ayarlar, iz.Adaylar, iz.Havuz, iz.Zar, iz.Secilen, iz.CelisenOzellikler, new long[] { 9, 3 }, new[] { "stres(): kaynak yok" });

            var satirlar = KisaSatirlar(kaynakli);

            Assert.Contains(satirlar, s => s.Contains("çelişen özellikler: durustluk"));
            Assert.Contains(satirlar, s => s.Contains("kaynak olaylar: #3, #9"));
            Assert.Contains(satirlar, s => s.Contains("kaynaksız girdi: stres(): kaynak yok"));
        }

        [Fact]
        public void Karar_olmayan_olay_icin_bos_liste_doner()
        {
            var baska = new Olay(3, 1, 0, "selam_oldu", "Ali", string.Empty, "han", new KeyValuePair<string, string>[0], new long[0], "eylem:selam");

            Assert.Empty(KararIziOzeti.Satirlar(baska));
        }
    }
}
