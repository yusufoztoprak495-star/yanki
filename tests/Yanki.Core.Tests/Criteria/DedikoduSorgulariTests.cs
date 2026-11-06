using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Criteria;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Rules;
using Yanki.Core.Tests.Decision;
using Yanki.Core.Tests.World;

namespace Yanki.Core.Tests.Criteria
{
    /// <summary>6. hafta sözlük eklemeleri: <c>konu_puani(kime)</c> ve <c>olay_konusu(desen)</c> (18 fonksiyon).</summary>
    public class DedikoduSorgulariTests
    {
        private const string Borc = "borclu(Mehmet,Ayse,500)";

        private sealed class Duzen
        {
            public readonly ZihinDeposu Zihinler = new ZihinDeposu();
            public readonly OlayGunlugu Gunluk = new OlayGunlugu();

            public Duzen()
            {
                var ayar = new ZihinAyarlari { KanitSonumuTickBasina = 0m };
                foreach (var id in new[] { "Ayse", "Can" })
                {
                    Zihinler.Ekle(new Zihin(id, new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m), ayar));
                }

                var anlatti = Ekle(new Olay(1, 10, 0, "anlatti", "oyuncu", "Ayse", "han", new[] { new KeyValuePair<string, string>("onerme", Borc) }, new long[0], "player"));
                Ekle(ZihinOlaylari.HafizaEklendi(2, 10, 1, "Ayse", anlatti.Id, 0.5m, new[] { anlatti.Id }, "algi"));
                Ekle(ZihinOlaylari.KanitEklendi(3, 10, 2, "Ayse", Borc, "duydu", "oyuncu", 0.7m, new[] { anlatti.Id }, "player", "han", "oyuncu"));
            }

            private Olay Ekle(Olay o)
            {
                Gunluk.Ekle(o);
                Zihinler.Uygula(o);
                return o;
            }
        }

        private static SorguDegeri Coz(Duzen d, string alan, SorguBaglami baglam, long tick = 10)
        {
            var cozucu = new ZihinSorguCozucusu(OlayUretici.SabitDunya(), d.Zihinler, d.Gunluk, tick);
            return cozucu.Coz(IfadeAyristirici.Ayristir(alan), baglam);
        }

        private static OnermeDeseni[] Desenler(params string[] d) => d.Select(OnermeDeseni.Ayristir).ToArray();

        [Fact]
        public void Sozluk_19_fonksiyondur_ve_yeni_ikisi_tanimlidir()
        {
            Assert.Equal(19, SorguSozlugu.Hepsi.Count);
            var kp = SorguSozlugu.Bul("konu_puani")!;
            Assert.Equal(1, kp.ArgumanSayisi);
            Assert.Equal(DonusTuru.Sayi, kp.Donus);
            var ok = SorguSozlugu.Bul("olay_konusu")!;
            Assert.Equal(1, ok.ArgumanSayisi);
            Assert.Equal(DonusTuru.Mantiksal, ok.Donus);
            Assert.Equal(0, ok.OnermeIndeksi);
        }

        [Fact]
        public void Konu_puani_eylemin_konu_desenlerinden_en_yuksek_puani_verir()
        {
            var d = new Duzen();
            var baglam = new SorguBaglami("Ayse", "oyuncu", eylemHedef: "Can", konuDesenleri: Desenler("borclu(*,*,*)"));

            // onem 0.5 × yenilik 1 × ilgi 0.5 (yalnızca Ayşe önermede geçiyor)
            Assert.Equal(0.25m, Coz(d, "konu_puani(eylem.hedef)", baglam).SayiDegeri);
        }

        [Fact]
        public void Konu_puani_desene_uyan_konu_yoksa_sifirdir()
        {
            var d = new Duzen();
            var baglam = new SorguBaglami("Ayse", "oyuncu", eylemHedef: "Can", konuDesenleri: Desenler("sevdi(*,*)"));

            Assert.Equal(0m, Coz(d, "konu_puani(eylem.hedef)", baglam).SayiDegeri);
        }

        [Fact]
        public void Konu_puani_konu_havuzu_olmayan_baglamda_cozulemez()
        {
            var d = new Duzen();
            var baglam = new SorguBaglami("Ayse", "oyuncu", eylemHedef: "Can");

            Assert.Throws<InvalidOperationException>(() => Coz(d, "konu_puani(eylem.hedef)", baglam));
        }

        [Theory]
        [InlineData("olay_konusu(borclu(Mehmet, *, *))", "Ayse", true)]     // dinleyen (olay.hedef), desen uyar
        [InlineData("olay_konusu(borclu(*, *, *))", "Ayse", true)]
        [InlineData("olay_konusu(borclu(Zeki, *, *))", "Ayse", false)]      // desen uymaz
        [InlineData("olay_konusu(degil(borclu(*, *, *)))", "Ayse", false)]  // olumsuzluk uymaz
        [InlineData("olay_konusu(borclu(Mehmet, *, *))", "oyuncu", false)]  // konuşan içeriği "duyan" değildir
        [InlineData("olay_konusu(borclu(Mehmet, *, *))", "Can", false)]     // tanık yalnızca konuşmayı görür
        public void Olay_konusu_yalnizca_icerigi_duyan_dinleyen_icin_dogrudur(string alan, string ben, bool beklenen)
        {
            var d = new Duzen();
            var baglam = new SorguBaglami(ben, "oyuncu", olayFail: "oyuncu", olayHedef: "Ayse", olayOnermesi: Borc);

            Assert.Equal(beklenen, Coz(d, alan, baglam).MantiksalDegeri);
        }

        [Fact]
        public void Olay_konusu_olumsuz_yalan_onermesini_de_esler()
        {
            var d = new Duzen();
            var baglam = new SorguBaglami("Ayse", "oyuncu", olayFail: "Can", olayHedef: "Ayse", olayOnermesi: "degil(" + Borc + ")");

            Assert.True(Coz(d, "olay_konusu(degil(borclu(Mehmet, *, *)))", baglam).MantiksalDegeri);
            Assert.False(Coz(d, "olay_konusu(borclu(Mehmet, *, *))", baglam).MantiksalDegeri);
        }

        [Fact]
        public void Olay_konusu_olay_onermesi_yoksa_yanlistir()
        {
            var d = new Duzen();
            var baglam = new SorguBaglami("Ayse", "oyuncu", olayFail: "Can", olayHedef: "Ayse");

            Assert.False(Coz(d, "olay_konusu(borclu(*, *, *))", baglam).MantiksalDegeri);
        }

        // ---- doğrulayıcı bağlamları ----

        private static string Kural(string alan) => @"{ ""kurallar"": [ {
  ""id"": ""r"", ""ne_zaman"": ""anlatti"",
  ""kriterler"": [ { ""alan"": """ + alan + @""", ""op"": ""=="", ""deger"": true } ],
  ""etkiler"": [ { ""tur"": ""hafiza"", ""onem"": 0.9 } ] } ] }";

        [Fact]
        public void Olay_konusu_sonuc_kuralinda_gecerlidir()
        {
            var s = KuralYukleyici.Yukle(Kural("olay_konusu(borclu(Mehmet, *, *))"), "k.json");
            Assert.True(s.Basarili, string.Join("\n", s.Hatalar));
        }

        [Fact]
        public void Konu_puani_sonuc_kuralinda_reddedilir()
        {
            var json = Kural("konu_puani(olay.hedef)").Replace("\"op\": \"==\", \"deger\": true", "\"op\": \">\", \"deger\": 0.1");
            var s = KuralYukleyici.Yukle(json, "k.json");
            Assert.False(s.Basarili);
            Assert.Contains(s.Hatalar, h => h.ToString().Contains("konu_puani"));
        }

        private static string Eylem(string ek, string aktarim) =>
            KararFikstur.Dosya("{ \"id\": \"anlat\", \"olay_turu\": \"anlatti\", \"hedef\": \"npc\", \"taban\": 1, \"aktarim\": \"" + aktarim + "\", " + ek + " }");

        [Fact]
        public void Konu_puani_dogru_aktarimin_on_kosul_ve_olcutunde_gecerlidir()
        {
            const string desen = "\"konu_desenleri\": [\"borclu(*,*,*)\"]";
            var onKosul = Eylem(desen + ", \"on_kosullar\": [ { \"alan\": \"konu_puani(eylem.hedef)\", \"op\": \">\", \"deger\": 0.2 } ]", "dogru");
            var olcut = Eylem(desen + ", \"olcutler\": [ { \"alan\": \"konu_puani(eylem.hedef)\", \"min\": 0, \"max\": 1, \"egri\": { \"tur\": \"dogrusal\" } } ]", "dogru");

            foreach (var json in new[] { onKosul, olcut })
            {
                var s = EylemYukleyici.Yukle(json, "e.json");
                Assert.True(s.Basarili, string.Join("\n", s.Hatalar));
            }
        }

        [Fact]
        public void Konu_puani_yalan_aktarimda_ve_aktarimsiz_eylemde_reddedilir()
        {
            var yalan = Eylem("\"yalan_onermeleri\": [\"degil(a(b))\"], \"on_kosullar\": [ { \"alan\": \"konu_puani(eylem.hedef)\", \"op\": \">\", \"deger\": 0.2 } ]", "yalan");
            var yok = KararFikstur.Dosya(KararFikstur.Eylem("selam", "npc", "\"on_kosullar\": [ { \"alan\": \"konu_puani(eylem.hedef)\", \"op\": \">\", \"deger\": 0.2 } ]"));

            foreach (var json in new[] { yalan, yok })
            {
                var s = EylemYukleyici.Yukle(json, "e.json");
                Assert.False(s.Basarili);
                Assert.Contains(s.Hatalar, h => h.ToString().Contains("konu_puani"));
            }
        }

        [Fact]
        public void Olay_konusu_eylem_baglaminda_reddedilir()
        {
            var json = KararFikstur.Dosya(KararFikstur.Eylem("selam", "npc", "\"on_kosullar\": [ { \"alan\": \"olay_konusu(borclu(*,*,*))\", \"op\": \"==\", \"deger\": true } ]"));

            var s = EylemYukleyici.Yukle(json, "e.json");

            Assert.False(s.Basarili);
            Assert.Contains(s.Hatalar, h => h.ToString().Contains("olay_konusu"));
        }
    }
}
