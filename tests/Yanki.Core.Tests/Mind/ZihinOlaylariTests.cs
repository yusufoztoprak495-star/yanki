using System;
using System.Collections.Generic;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Tests.World;
using Yanki.Core.World;

namespace Yanki.Core.Tests.Mind
{
    public class ZihinOlaylariTests
    {
        private static Kisilik K() => new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m);

        [Fact]
        public void Olay_turleri_plandaki_adlardadir()
        {
            Assert.Equal("modifier_eklendi", ZihinOlaylari.ModifierEklendiTuru);
            Assert.Equal("eksen_degisti", ZihinOlaylari.EksenDegistiTuru);
            Assert.Equal("duygu_degisti", ZihinOlaylari.DuyguDegistiTuru);
            Assert.Equal("stres_degisti", ZihinOlaylari.StresDegistiTuru);
            Assert.Equal("kanit_eklendi", ZihinOlaylari.KanitEklendiTuru);
            Assert.Equal("hafiza_eklendi", ZihinOlaylari.HafizaEklendiTuru);
        }

        [Fact]
        public void Uretilen_olay_neden_ve_kural_kimligi_tasir()
        {
            var olay = ZihinOlaylari.StresDegisti(5, 3, 0, "Can", 10m, new long[] { 2, 1 }, "kural_x");

            Assert.Equal(new long[] { 1, 2 }, olay.NedenOlaylar);
            Assert.Equal("kural_x", olay.KuralId);
            Assert.Equal("stres_degisti", olay.Tur);
        }

        [Fact]
        public void Zihin_dunya_olayini_ve_baskasinin_olayini_yoksayar_ya_da_reddeder()
        {
            var z = new Zihin("Can", K());
            var dunyaOlayi = new OlayUretici().Varlik("Ali", "npc");
            var u = new ZihinOlayUretici();

            Assert.False(z.Uygula(dunyaOlayi));
            Assert.True(z.Uygula(u.Stres("Can", 5m)));
            Assert.Throws<ArgumentException>(() => z.Uygula(u.Stres("Ayse", 5m)));
        }

        [Fact]
        public void Deposu_olayi_sahibinin_zihnine_yonlendirir()
        {
            var depo = new ZihinDeposu();
            depo.Ekle(new Zihin("Can", K()));
            depo.Ekle(new Zihin("Ayse", K()));
            var u = new ZihinOlayUretici();

            Assert.True(depo.Uygula(u.Stres("Ayse", 30m)));

            Assert.Equal(30m, depo["Ayse"].Stres(0));
            Assert.Equal(0m, depo["Can"].Stres(0));
            Assert.False(depo.Uygula(new OlayUretici().Varlik("Ali", "npc")));
            Assert.Throws<InvalidOperationException>(() => depo.Uygula(u.Stres("Yok", 1m)));
        }

        [Fact]
        public void Deposu_zihinleri_ordinal_sirayla_verir_ve_tekrar_eklemeyi_reddeder()
        {
            var depo = new ZihinDeposu();
            depo.Ekle(new Zihin("Veli", K()));
            depo.Ekle(new Zihin("Ayse", K()));

            Assert.Equal(new[] { "Ayse", "Veli" }, depo.Kimlikler);
            Assert.Throws<InvalidOperationException>(() => depo.Ekle(new Zihin("Ayse", K())));
        }

        [Fact]
        public void Deposu_fikir_a_b_tick_ile_sorulur()
        {
            var depo = new ZihinDeposu();
            depo.Ekle(new Zihin("Can", K()));
            var u = new ZihinOlayUretici();
            depo.Uygula(u.Modifier("Can", "oyuncu", "aileme_iftira", -22m, 18));

            Assert.Equal(-22m, depo.Fikir("Can", "oyuncu", 0));
            Assert.Equal(-22m, depo.FikirDokumu("Can", "oyuncu", 0).Toplam);
        }

        [Fact]
        public void Bos_zihin_deposu_dunya_hashini_degistirmez()
        {
            var dunya = OlayUretici.SabitDunya();

            Assert.Equal(dunya.DurumHash(), DurumHashu.Hesapla(dunya, new ZihinDeposu()));
            Assert.Equal("b37a372bab89d9b4", DurumHashu.Hesapla(dunya, new ZihinDeposu()));
        }

        [Fact]
        public void Zihin_durumu_hashe_girer()
        {
            var dunya = OlayUretici.SabitDunya();
            var bos = new ZihinDeposu();
            var tek = new ZihinDeposu();
            tek.Ekle(new Zihin("Can", K()));

            Assert.NotEqual(DurumHashu.Hesapla(dunya, bos), DurumHashu.Hesapla(dunya, tek));

            var once = DurumHashu.Hesapla(dunya, tek);
            tek.Uygula(new ZihinOlayUretici().Stres("Can", 1m));
            Assert.NotEqual(once, DurumHashu.Hesapla(dunya, tek));
        }

        [Fact]
        public void Kisilik_hashe_girer()
        {
            var a = new ZihinDeposu();
            a.Ekle(new Zihin("Can", new Kisilik(0.1m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m)));
            var b = new ZihinDeposu();
            b.Ekle(new Zihin("Can", new Kisilik(0.2m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m)));

            Assert.NotEqual(a.DurumHash(), b.DurumHash());
        }

        [Fact]
        public void Ayni_olaylar_ayni_hashi_verir_tick_sorgusu_hashi_degistirmez()
        {
            ZihinDeposu Kur()
            {
                var d = new ZihinDeposu();
                d.Ekle(new Zihin("Can", K()));
                d.Ekle(new Zihin("Ayse", K()));
                var u = new ZihinOlayUretici();
                d.Uygula(u.Eksen("Can", "oyuncu", "guven", -20m));
                d.Uygula(u.Modifier("Can", "oyuncu", "minnet", 20m, 50));
                d.Uygula(u.Duygu("Ayse", "ofke", 0.5m, "Can"));
                d.Uygula(u.Stres("Ayse", 12.3456789m));
                d.Uygula(u.Kanit("Can", "borclu(Mehmet,Ayse,500)", "duydu", "Ayse", 0.7m));
                d.Uygula(u.Hafiza("Can", 3, 0.6m));
                return d;
            }

            var x = Kur();
            var y = Kur();
            var h = x.DurumHash();

            Assert.Equal(y.DurumHash(), h);
            x["Can"].Fikir("oyuncu", 999);
            x["Can"].Guven("borclu(Mehmet,Ayse,500)", 999);
            Assert.Equal(h, x.DurumHash());
            Assert.Equal(16, h.Length);
        }

        [Fact]
        public void Zihin_kanonik_metni_kalici_durumu_sirali_yazar()
        {
            var z = new Zihin("Can", new Kisilik(0.1m, 0.2m, 0.3m, 0.4m, 0.5m, 0.6m));
            var u = new ZihinOlayUretici();
            z.Uygula(u.Eksen("Can", "oyuncu", "guven", 10m));

            var metin = z.KanonikMetin();

            Assert.Contains("Z|Can\n", metin);
            Assert.Contains("P|acgozluluk|0.1\n", metin);
            Assert.Contains("P|sosyallik|0.4\n", metin);
            Assert.True(metin.IndexOf("P|acgozluluk", StringComparison.Ordinal) < metin.IndexOf("P|cesaret", StringComparison.Ordinal));
            Assert.Contains("E|oyuncu|guven|10\n", metin);
        }

        [Fact]
        public void Zihin_olaylari_gunlukten_bastan_oynatilinca_ayni_durum_olusur()
        {
            var gunluk = new OlayGunlugu();
            gunluk.Ekle(ZihinOlaylari.EksenDegisti(1, 0, 0, "Can", "oyuncu", "guven", -30m, new long[0], "kural_a"));
            gunluk.Ekle(ZihinOlaylari.ModifierEklendi(2, 0, 1, "Can", "oyuncu", "hakaret_etti", -15m, 20, new long[] { 1 }, "kural_b"));

            var z = new Zihin("Can", K());
            foreach (var o in gunluk.Olaylar)
            {
                z.Uygula(o);
            }

            Assert.Equal(-12m - 15m, z.Fikir("oyuncu", 0));
            Assert.Equal(new long[] { 1 }, gunluk[2].NedenOlaylar);
        }

        [Fact]
        public void Yuk_anahtarlari_ASCII_dir()
        {
            var olay = ZihinOlaylari.ModifierEklendi(1, 0, 0, "Can", "oyuncu", "minnet", 20m, 5, new long[0], "k");

            foreach (KeyValuePair<string, string> kv in olay.Yuk)
            {
                foreach (var c in kv.Key)
                {
                    Assert.True(c < 128);
                }
            }
        }
    }
}
