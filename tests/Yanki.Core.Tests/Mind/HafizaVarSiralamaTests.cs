using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.World;

namespace Yanki.Core.Tests.Mind
{
    /// <summary>
    /// <c>Zihin.HafizaVar</c> erken durdurma (Z1). Pencere, hatırlanan OLAYIN tick'ine göre uygulanır; hafıza kayıtlarının EKLENME sırası bu
    /// anahtara göre sıralı DEĞİLDİR (kural <c>hafiza</c> etkisi tetikleyici, yani bir önceki tick'in olayını yeni kayıt olarak sona ekler;
    /// mevcut kayıt yerinde güncellenir). Bu yüzden erken durdurma eklenme sırasına dayanamaz: olay kimliğine göre sıralı ayrı bir dizin gerekir
    /// (günlük kuralı gereği olay tick'i kimlikle azalmaz). Bu dosya hem tersliği hem yeni uygulamanın eski tam taramayla birebir aynı
    /// sonucu verdiğini kanıtlar.
    /// </summary>
    public class HafizaVarSiralamaTests
    {
        private static Kisilik K() => new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m);

        private static Olay Dis(OlayGunlugu g, long tick, string tur, string fail)
        {
            var o = new Olay(g.SonrakiId, tick, g.SonrakiSira(tick), tur, fail, string.Empty, "han", new KeyValuePair<string, string>[0], new long[0], "x");
            g.Ekle(o);
            return o;
        }

        private static void Hatirla(Zihin z, long olayId, decimal onem, long tick) =>
            z.Uygula(ZihinOlaylari.HafizaEklendi(1000 + olayId, tick, 0, z.Id, olayId, onem, new long[0], "algi"));

        // Referans: Z1'den ÖNCEKİ uygulamanın birebir kopyası (tam tarama, olay kimliğine göre sıralı Hafiza(tick) üzerinden).
        private static bool EskiHafizaVar(Zihin z, OlayGunlugu gunluk, string olayTuru, string fail, long sonNTick, long simdikiTick)
        {
            foreach (var h in z.Hafiza(simdikiTick))
            {
                if (!gunluk.Var(h.OlayId))
                {
                    continue;
                }

                var olay = gunluk[h.OlayId];
                if (olay.Tur == olayTuru && olay.Fail == fail && simdikiTick - olay.Tick <= sonNTick)
                {
                    return true;
                }
            }

            return false;
        }

        [Fact]
        public void Ekleme_sirasi_olay_tickine_gore_sirali_olmayabilir()
        {
            // Tick 5'te önce o tick'in olayı (2), sonra bir önceki tick'in olayı (1) hatırlanır: kural `hafiza` etkisi tetikleyiciyi böyle ekler.
            var gunluk = new OlayGunlugu();
            var eski = Dis(gunluk, 4, "baska", "X");
            var yeni = Dis(gunluk, 5, "selam", "X");
            var z = new Zihin("Ali", K());
            Hatirla(z, yeni.Id, 0.9m, 5);
            Hatirla(z, eski.Id, 0.9m, 5);

            var sira = z.HafizaKayitlari.Select(h => gunluk[h.OlayId].Tick).ToArray();

            Assert.Equal(new long[] { 5, 4 }, sira);
        }

        [Fact]
        public void Yerinde_guncellenen_kayit_eklenme_sirasinda_yeni_tickle_en_basta_kalir()
        {
            var gunluk = new OlayGunlugu();
            var a = Dis(gunluk, 1, "a", "X");
            var b = Dis(gunluk, 2, "a", "X");
            var z = new Zihin("Ali", K());
            Hatirla(z, a.Id, 0.5m, 1);
            Hatirla(z, b.Id, 0.5m, 2);
            Hatirla(z, a.Id, 0.9m, 9); // önem artar → kayıt tazelenir

            Assert.Equal(new long[] { 9, 2 }, z.HafizaKayitlari.Select(h => h.Tick).ToArray());
        }

        [Fact]
        public void Pencere_icindeki_eslesme_ekleme_sirasindan_bagimsiz_bulunur()
        {
            // Ters sırada eklenmiş kayıtlar: taramanın sonundaki kayıt pencere dışındadır, ama öndeki eşleşir (fark 0).
            var gunluk = new OlayGunlugu();
            var eski = Dis(gunluk, 4, "baska", "X");
            var yeni = Dis(gunluk, 5, "selam", "X");
            var z = new Zihin("Ali", K());
            Hatirla(z, yeni.Id, 0.9m, 5);
            Hatirla(z, eski.Id, 0.9m, 5);

            Assert.True(z.HafizaVar(gunluk, "selam", "X", 0, 5));
            Assert.False(z.HafizaVar(gunluk, "baska", "X", 0, 5));
            Assert.True(z.HafizaVar(gunluk, "baska", "X", 1, 5));
        }

        [Fact]
        public void Pencere_siniri_dahildir_ve_disindaki_bulunmaz()
        {
            var gunluk = new OlayGunlugu();
            var o = Dis(gunluk, 3, "selam", "X");
            var z = new Zihin("Ali", K());
            Hatirla(z, o.Id, 0.9m, 3);

            Assert.True(z.HafizaVar(gunluk, "selam", "X", 7, 10));
            Assert.False(z.HafizaVar(gunluk, "selam", "X", 6, 10));
        }

        [Fact]
        public void Gunlukte_olmayan_olaya_isaret_eden_kayit_atlanir()
        {
            var gunluk = new OlayGunlugu();
            Dis(gunluk, 1, "selam", "X");
            var z = new Zihin("Ali", K());
            Hatirla(z, 99, 0.9m, 1);

            Assert.False(z.HafizaVar(gunluk, "selam", "X", 10, 2));
        }

        [Fact]
        public void Yeni_uygulama_eski_tam_taramayla_rastgele_ama_sabit_seedle_birebir_ayni_sonucu_verir()
        {
            var dogru = 0;
            var yanlis = 0;
            for (ulong seed = 1; seed <= 60; seed++)
            {
                var rng = AnahtarliRng.Icin(seed, 0, "hafiza_var_karsilastirma");
                var gunluk = new OlayGunlugu();
                string[] turler = { "a", "b", "c" };
                string[] failler = { "X", "Y" };
                long tick = 0;
                const int olaySayisi = 70;
                for (var i = 0; i < olaySayisi; i++)
                {
                    tick += rng.Aralik(0, 3);
                    Dis(gunluk, tick, turler[rng.Aralik(0, 3)], failler[rng.Aralik(0, 2)]);
                }

                var z = new Zihin("Ali", K());
                decimal[] onemler = { 0.3m, 0.6m, 0.9m, 0.95m };
                var hafizaTicki = 0L;
                for (var i = 0; i < 90; i++)
                {
                    var olayId = rng.Aralik(1, olaySayisi + 3); // bazıları günlükte yok
                    var tazelemeTicki = olayId <= olaySayisi ? gunluk[olayId].Tick : tick;
                    hafizaTicki = System.Math.Max(hafizaTicki, tazelemeTicki);
                    Hatirla(z, olayId, onemler[rng.Aralik(0, 4)], hafizaTicki);
                }

                for (var q = 0; q < 250; q++)
                {
                    var simdi = tick + rng.Aralik(0, 12);
                    var tur = turler[rng.Aralik(0, 3)];
                    var fail = failler[rng.Aralik(0, 2)];
                    var pencere = rng.Aralik(0, 40);

                    var beklenen = EskiHafizaVar(z, gunluk, tur, fail, pencere, simdi);
                    var gercek = z.HafizaVar(gunluk, tur, fail, pencere, simdi);

                    Assert.True(beklenen == gercek, $"seed {seed}, sorgu {q}: tur={tur} fail={fail} pencere={pencere} tick={simdi}: eski {beklenen}, yeni {gercek}");
                    if (beklenen)
                    {
                        dogru++;
                    }
                    else
                    {
                        yanlis++;
                    }
                }
            }

            // Karşılaştırma boş değil: hem doğru hem yanlış sonuçlar sınandı.
            Assert.True(dogru > 500 && yanlis > 500, $"doğru {dogru}, yanlış {yanlis}");
        }

        [Fact]
        public void Kanonik_metin_ekleme_sirasindan_bagimsizdir_dizin_hashi_degistirmez()
        {
            // Dizin yalnızca sorguyu hızlandırır; zihnin kanonik metni (durum hash'i) olay kimliğine göre sıralı yazılmaya devam eder.
            var gunluk = new OlayGunlugu();
            var a = Dis(gunluk, 1, "a", "X");
            var b = Dis(gunluk, 2, "a", "X");
            var ileri = new Zihin("Ali", K());
            Hatirla(ileri, a.Id, 0.5m, 2);
            Hatirla(ileri, b.Id, 0.5m, 2);
            var ters = new Zihin("Ali", K());
            Hatirla(ters, b.Id, 0.5m, 2);
            Hatirla(ters, a.Id, 0.5m, 2);

            Assert.Equal(ileri.KanonikMetin(), ters.KanonikMetin());
            Assert.Equal(new long[] { b.Id, a.Id }, ters.HafizaKayitlari.Select(h => h.OlayId).ToArray());
        }
    }
}
