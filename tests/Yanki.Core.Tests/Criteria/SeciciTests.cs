using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Criteria;
using Yanki.Core.World;

namespace Yanki.Core.Tests.Criteria
{
    public class SeciciTests
    {
        private static readonly SorguBaglami Baglam = new SorguBaglami("Ali");

        // Sahte dünya: stres()=10, yay(a)=5, kisilik(x)=1
        private static FakeSorguCozucu Cozucu() => new FakeSorguCozucu()
            .Ayarla("stres()", SorguDegeri.Sayi(10))
            .Ayarla("yay(a)", SorguDegeri.Sayi(5))
            .Ayarla("kisilik(x)", SorguDegeri.Sayi(1));

        private static Kriter Buyuk(string alan, int esik, decimal agirlik = 1m, bool zorunlu = false) =>
            Kriter.Olustur(alan, KriterOperatoru.Buyuk, SorguDegeri.Sayi(esik), agirlik, zorunlu);

        private static Aday<string> Aday(string id, params Kriter[] kriterler) => new Aday<string>(id, "eylem:" + id, kriterler);

        [Fact]
        public void Puan_tutan_kriterlerin_agirlik_toplamidir()
        {
            var iz = Secici.Puanla(
                Aday("a", Buyuk("stres()", 0, 2m), Buyuk("yay(a)", 100, 7m), Buyuk("kisilik(x)", 0, 0.5m)),
                Cozucu(), Baglam);

            Assert.Equal(2.5m, iz.Puan);
            Assert.False(iz.Elendi);
            Assert.Equal(new[] { true, false, true }, iz.Sonuclar.Select(s => s.Tuttu).ToArray());
        }

        [Fact]
        public void Zorunlu_tutmazsa_elenir_ve_butun_kriterler_yine_de_degerlendirilir()
        {
            var cozucu = Cozucu();
            var iz = Secici.Puanla(
                Aday("a", Buyuk("kisilik(x)", 100, 1m, zorunlu: true), Buyuk("stres()", 0, 4m), Buyuk("yay(a)", 0, 1m)),
                cozucu, Baglam);

            Assert.True(iz.Elendi);
            Assert.Equal(5m, iz.Puan);
            Assert.Equal(3, iz.Sonuclar.Count);
            Assert.Equal(3, cozucu.CagriSayisi);
        }

        [Fact]
        public void Zorunlu_tutarsa_elenmez()
        {
            var iz = Secici.Puanla(Aday("a", Buyuk("stres()", 0, 1m, zorunlu: true)), Cozucu(), Baglam);

            Assert.False(iz.Elendi);
        }

        [Fact]
        public void En_yuksek_puanli_kazanir()
        {
            var adaylar = new[]
            {
                Aday("a", Buyuk("stres()", 0, 2m)),
                Aday("b", Buyuk("stres()", 0, 3m), Buyuk("yay(a)", 0, 1m)),
                Aday("c", Buyuk("stres()", 100, 9m)),
            };

            var sonuc = Secici.EnIyiSec(adaylar, Cozucu(), Baglam, AnahtarliRng.Icin(1UL, 0, "sec"));

            Assert.True(sonuc.Bulundu);
            Assert.Equal("b", sonuc.Secilen!.Id);
            Assert.False(sonuc.EsitlikVardi);
        }

        [Fact]
        public void Zorunlu_tutmazsa_puani_yuksek_olsa_bile_elenir()
        {
            var adaylar = new[]
            {
                Aday("a", Buyuk("stres()", 0, 50m), Buyuk("kisilik(x)", 100, 1m, zorunlu: true)),
                Aday("b", Buyuk("stres()", 0, 1m)),
            };

            var sonuc = Secici.EnIyiSec(adaylar, Cozucu(), Baglam, AnahtarliRng.Icin(1UL, 0, "sec"));

            Assert.Equal("b", sonuc.Secilen!.Id);
            Assert.True(sonuc.Izler.Single(i => i.AdayId == "a").Elendi);
        }

        [Fact]
        public void Hepsi_elendiyse_Bulundu_false()
        {
            var adaylar = new[]
            {
                Aday("a", Buyuk("kisilik(x)", 100, 1m, zorunlu: true)),
                Aday("b", Buyuk("kisilik(x)", 100, 1m, zorunlu: true)),
            };

            var sonuc = Secici.EnIyiSec(adaylar, Cozucu(), Baglam, AnahtarliRng.Icin(1UL, 0, "sec"));

            Assert.False(sonuc.Bulundu);
            Assert.Null(sonuc.Secilen);
            Assert.Equal(2, sonuc.Izler.Count);
        }

        [Fact]
        public void Bos_liste_Bulundu_false()
        {
            var sonuc = Secici.EnIyiSec(new Aday<string>[0], Cozucu(), Baglam, AnahtarliRng.Icin(1UL, 0, "sec"));

            Assert.False(sonuc.Bulundu);
            Assert.Empty(sonuc.Izler);
        }

        private static Aday<string>[] UcEsitAday() => new[]
        {
            Aday("c", Buyuk("stres()", 0, 1m)),
            Aday("a", Buyuk("stres()", 0, 1m)),
            Aday("b", Buyuk("stres()", 0, 1m)),
        };

        [Fact]
        public void Esitlikte_ayni_rng_anahtari_ayni_secim()
        {
            var ilk = Secici.EnIyiSec(UcEsitAday(), Cozucu(), Baglam, AnahtarliRng.Icin(7UL, 3, "Ali", "sec"));
            var ikinci = Secici.EnIyiSec(UcEsitAday(), Cozucu(), Baglam, AnahtarliRng.Icin(7UL, 3, "Ali", "sec"));

            Assert.Equal(ilk.Secilen!.Id, ikinci.Secilen!.Id);
            Assert.True(ilk.EsitlikVardi);
        }

        [Fact]
        public void Esitlikte_girdi_sirasi_sonucu_degistirmez()
        {
            for (var tick = 0; tick < 20; tick++)
            {
                var duz = Secici.EnIyiSec(UcEsitAday(), Cozucu(), Baglam, AnahtarliRng.Icin(7UL, tick, "sec"));
                var ters = Secici.EnIyiSec(UcEsitAday().Reverse().ToArray(), Cozucu(), Baglam, AnahtarliRng.Icin(7UL, tick, "sec"));

                Assert.Equal(duz.Secilen!.Id, ters.Secilen!.Id);
            }
        }

        [Fact]
        public void Esitlikte_secim_adaylar_arasinda_dagilir()
        {
            var secilenler = new SortedSet<string>(StringComparer.Ordinal);
            for (var tick = 0; tick < 60; tick++)
            {
                var sonuc = Secici.EnIyiSec(UcEsitAday(), Cozucu(), Baglam, AnahtarliRng.Icin(7UL, tick, "sec"));
                secilenler.Add(sonuc.Secilen!.Id);
            }

            Assert.Equal(new[] { "a", "b", "c" }, secilenler.ToArray());
        }

        // Karar (Yusuf): puan eşitse daha çok kriteri tutan (daha özgül) aday kazanır; o da eşitse anahtarlı RNG (ADR-003).
        [Fact]
        public void Ayni_puan_farkli_kriter_sayisi_ozgul_olan_kazanir()
        {
            for (var tick = 0; tick < 20; tick++)
            {
                var adaylar = new[]
                {
                    Aday("genel", Buyuk("stres()", 0, 2m)),                                   // puan 2, 1 kriter
                    Aday("ozgul", Buyuk("stres()", 0, 1m), Buyuk("yay(a)", 0, 1m)),           // puan 2, 2 kriter
                };

                var sonuc = Secici.EnIyiSec(adaylar, Cozucu(), Baglam, AnahtarliRng.Icin(3UL, tick, "sec"));

                Assert.Equal("ozgul", sonuc.Secilen!.Id);
                Assert.False(sonuc.EsitlikVardi);   // RNG gerekmedi
            }
        }

        [Fact]
        public void Ozgul_olan_listede_nerede_olursa_olsun_kazanir()
        {
            var ozgul = Aday("a", Buyuk("stres()", 0, 1m), Buyuk("yay(a)", 0, 1m), Buyuk("kisilik(x)", 0, 1m));
            var genel = Aday("z", Buyuk("stres()", 0, 3m));

            var ileri = Secici.EnIyiSec(new[] { ozgul, genel }, Cozucu(), Baglam, AnahtarliRng.Icin(3UL, 0, "sec"));
            var geri = Secici.EnIyiSec(new[] { genel, ozgul }, Cozucu(), Baglam, AnahtarliRng.Icin(3UL, 0, "sec"));

            Assert.Equal("a", ileri.Secilen!.Id);
            Assert.Equal("a", geri.Secilen!.Id);
        }

        [Fact]
        public void Puan_ozguluktan_once_gelir()
        {
            var adaylar = new[]
            {
                Aday("yuksek_puan", Buyuk("stres()", 0, 5m)),                                                         // puan 5, 1 kriter
                Aday("cok_kriter", Buyuk("stres()", 0, 1m), Buyuk("yay(a)", 0, 1m), Buyuk("kisilik(x)", 0, 1m)),      // puan 3, 3 kriter
            };

            var sonuc = Secici.EnIyiSec(adaylar, Cozucu(), Baglam, AnahtarliRng.Icin(3UL, 0, "sec"));

            Assert.Equal("yuksek_puan", sonuc.Secilen!.Id);
        }

        [Fact]
        public void Ozgulluk_yalnizca_tutan_kriterleri_sayar()
        {
            // "b" 3 kriter taşır ama yalnızca 1'i tutar; "a" 1 kriter taşır ve o tutar → özgüllük eşit, RNG devreye girer.
            var adaylar = new[]
            {
                Aday("a", Buyuk("stres()", 0, 2m)),
                Aday("b", Buyuk("stres()", 0, 2m), Buyuk("yay(a)", 100, 0m), Buyuk("kisilik(x)", 100, 0m)),
            };

            var secilenler = new SortedSet<string>(StringComparer.Ordinal);
            for (var tick = 0; tick < 60; tick++)
            {
                var sonuc = Secici.EnIyiSec(adaylar, Cozucu(), Baglam, AnahtarliRng.Icin(3UL, tick, "sec"));
                Assert.True(sonuc.EsitlikVardi);
                secilenler.Add(sonuc.Secilen!.Id);
            }

            Assert.Equal(new[] { "a", "b" }, secilenler.ToArray());
        }

        [Fact]
        public void Elenmis_aday_cok_kriter_tutsa_da_kazanmaz()
        {
            var adaylar = new[]
            {
                Aday("elenen", Buyuk("stres()", 0, 1m), Buyuk("yay(a)", 0, 1m), Buyuk("kisilik(x)", 100, 1m, zorunlu: true)),
                Aday("kalan", Buyuk("stres()", 0, 1m)),
            };

            var sonuc = Secici.EnIyiSec(adaylar, Cozucu(), Baglam, AnahtarliRng.Icin(3UL, 0, "sec"));

            Assert.Equal("kalan", sonuc.Secilen!.Id);
        }

        [Fact]
        public void Puan_ve_ozgulluk_ikisi_de_esitse_anahtarli_rng_secer()
        {
            // İki kriterli, eşit puanlı üç aday: farklı kriterlerle ama aynı puan ve aynı tutan sayısı.
            List<Aday<string>> Uc() => new List<Aday<string>>
            {
                Aday("c", Buyuk("stres()", 0, 1m), Buyuk("yay(a)", 0, 1m)),
                Aday("a", Buyuk("stres()", 0, 1m), Buyuk("kisilik(x)", 0, 1m)),
                Aday("b", Buyuk("yay(a)", 0, 1m), Buyuk("kisilik(x)", 0, 1m)),
            };

            var secilenler = new SortedSet<string>(StringComparer.Ordinal);
            for (var tick = 0; tick < 60; tick++)
            {
                var duz = Secici.EnIyiSec(Uc(), Cozucu(), Baglam, AnahtarliRng.Icin(3UL, tick, "sec"));
                var ters = Secici.EnIyiSec(Uc().AsEnumerable().Reverse().ToList(), Cozucu(), Baglam, AnahtarliRng.Icin(3UL, tick, "sec"));

                Assert.True(duz.EsitlikVardi);
                Assert.Equal(duz.Secilen!.Id, ters.Secilen!.Id);   // girdi sırasından bağımsız
                secilenler.Add(duz.Secilen.Id);
            }

            Assert.Equal(new[] { "a", "b", "c" }, secilenler.ToArray());
        }

        [Fact]
        public void Ozgulluk_esitligi_cozerse_rng_cekilmez()
        {
            var rng = AnahtarliRng.Icin(3UL, 0, "sec");
            var adaylar = new[] { Aday("genel", Buyuk("stres()", 0, 2m)), Aday("ozgul", Buyuk("stres()", 0, 1m), Buyuk("yay(a)", 0, 1m)) };

            Secici.EnIyiSec(adaylar, Cozucu(), Baglam, rng);

            Assert.Equal(AnahtarliRng.Icin(3UL, 0, "sec").SonrakiUlong(), rng.SonrakiUlong());
        }

        [Fact]
        public void Iz_tutan_kriter_sayisini_verir()
        {
            var iz = Secici.Puanla(Aday("a", Buyuk("stres()", 0), Buyuk("yay(a)", 100), Buyuk("kisilik(x)", 0)), Cozucu(), Baglam);

            Assert.Equal(2, iz.TutanKriterSayisi);
        }

        [Fact]
        public void Esitlik_yoksa_rng_cekilmez()
        {
            var rng = AnahtarliRng.Icin(7UL, 3, "sec");
            var adaylar = new[] { Aday("a", Buyuk("stres()", 0, 2m)), Aday("b", Buyuk("stres()", 0, 1m)) };

            Secici.EnIyiSec(adaylar, Cozucu(), Baglam, rng);

            Assert.Equal(AnahtarliRng.Icin(7UL, 3, "sec").SonrakiUlong(), rng.SonrakiUlong());
        }

        [Fact]
        public void Esitlik_yalnizca_elenmemis_adaylar_arasinda_aranir()
        {
            var adaylar = new[]
            {
                Aday("a", Buyuk("stres()", 0, 3m)),
                Aday("b", Buyuk("stres()", 0, 3m), Buyuk("kisilik(x)", 100, 1m, zorunlu: true)),   // elenir
            };

            var sonuc = Secici.EnIyiSec(adaylar, Cozucu(), Baglam, AnahtarliRng.Icin(7UL, 0, "sec"));

            Assert.Equal("a", sonuc.Secilen!.Id);
            Assert.False(sonuc.EsitlikVardi);
        }

        [Fact]
        public void Iz_tutan_ve_tutmayan_kriterleri_listeler_ve_adaya_gore_siralidir()
        {
            var adaylar = new[]
            {
                Aday("z", Buyuk("stres()", 0, 1m)),
                Aday("m", Buyuk("stres()", 0, 1m), Buyuk("yay(a)", 100, 2m)),
            };

            var sonuc = Secici.EnIyiSec(adaylar, Cozucu(), Baglam, AnahtarliRng.Icin(7UL, 0, "sec"));

            Assert.Equal(new[] { "m", "z" }, sonuc.Izler.Select(i => i.AdayId).ToArray());
            var m = sonuc.Izler[0];
            Assert.Equal(new[] { true, false }, m.Sonuclar.Select(s => s.Tuttu).ToArray());
            Assert.Equal(new[] { 1m, 0m }, m.Sonuclar.Select(s => s.Katki).ToArray());
            Assert.Equal(1m, m.Puan);
        }

        [Fact]
        public void Tekrar_eden_aday_id_hata()
        {
            var adaylar = new[] { Aday("a", Buyuk("stres()", 0)), Aday("a", Buyuk("stres()", 0)) };

            Assert.Throws<ArgumentException>(() => Secici.EnIyiSec(adaylar, Cozucu(), Baglam, AnahtarliRng.Icin(1UL, 0, "x")));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Bos_aday_id_hata(string? id)
        {
            Assert.Throws<ArgumentException>(() => new Aday<string>(id!, "x", new List<Kriter>()));
        }

        [Fact]
        public void Kriteri_olmayan_aday_puani_sifir_ve_elenmez()
        {
            var iz = Secici.Puanla(Aday("a"), Cozucu(), Baglam);

            Assert.Equal(0m, iz.Puan);
            Assert.False(iz.Elendi);
        }
    }
}
