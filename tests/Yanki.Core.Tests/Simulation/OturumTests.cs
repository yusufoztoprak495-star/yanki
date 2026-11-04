using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Simulation;

namespace Yanki.Core.Tests.Simulation
{
    public class OturumTests
    {
        private static Simulasyon Calistir()
        {
            var s = SimFikstur.Yap(seed: 99);
            s.Submit(new PlayerAction("yardim_etti", "Ali"));
            s.Tick(2);
            s.Submit(new PlayerAction("tehdit_etti", "Veli", new[] { new KeyValuePair<string, string>("gorunurluk", "sessiz") }));
            s.Tick(3);
            return s;
        }

        [Fact]
        public void Kayit_simulasyondan_uretilir()
        {
            var s = Calistir();

            var kayit = OturumKaydi.Uret(s, "senaryo.json");

            Assert.Equal(2, kayit.Surum);
            Assert.Equal(99UL, kayit.Seed);
            Assert.Equal("senaryo.json", kayit.Senaryo);
            Assert.Equal(5, kayit.TickSayisi);
            Assert.Equal(s.SonHash, kayit.SonHash);
            Assert.Equal(new long[] { 1, 3 }, kayit.Eylemler.Select(e => e.Tick).ToArray());
        }

        [Fact]
        public void Yaz_ve_oku_gidis_donus_aynidir()
        {
            var kayit = OturumKaydi.Uret(Calistir(), "senaryo.json");

            var metin = OturumKaydi.Yaz(kayit);
            var oku = OturumKaydi.Oku(metin, "oturum.json");

            Assert.True(oku.Basarili, string.Join("\n", oku.Hatalar));
            Assert.Equal(metin, OturumKaydi.Yaz(oku.Kayit!));
            Assert.Equal("sessiz", oku.Kayit!.Eylemler[1].Eylem.Yuk.Single(k => k.Key == "gorunurluk").Value);
            Assert.Equal("Veli", oku.Kayit.Eylemler[1].Eylem.Hedef);
        }

        [Fact]
        public void Oynat_ayni_hash_uretir()
        {
            var s = Calistir();
            var kayit = OturumKaydi.Uret(s, "senaryo.json");

            var yeni = SimFikstur.Yap(seed: kayit.Seed);
            var sonuc = OturumOynatici.Oynat(yeni, kayit);

            Assert.True(sonuc.HashTutuyor);
            Assert.Equal(kayit.SonHash, sonuc.GercekHash);
            Assert.Equal(5, yeni.SimdikiTick);
            Assert.Equal(s.DurumHash(), yeni.DurumHash());
        }

        [Fact]
        public void Oynat_hash_tutmazsa_bunu_bildirir()
        {
            var kayit = OturumKaydi.Uret(Calistir(), "senaryo.json");
            var bozuk = new OturumKaydi(kayit.Seed, kayit.Senaryo, kayit.TickSayisi, kayit.Eylemler, "0000000000000000");

            var sonuc = OturumOynatici.Oynat(SimFikstur.Yap(seed: kayit.Seed), bozuk);

            Assert.False(sonuc.HashTutuyor);
            Assert.Equal("0000000000000000", sonuc.BeklenenHash);
            Assert.NotEqual("0000000000000000", sonuc.GercekHash);
        }

        [Fact]
        public void Oynat_seed_uyusmazsa_reddeder()
        {
            var kayit = OturumKaydi.Uret(Calistir(), "senaryo.json");
            Assert.Throws<ArgumentException>(() => OturumOynatici.Oynat(SimFikstur.Yap(seed: 1), kayit));
        }

        [Fact]
        public void Oynat_dolu_simulasyonu_reddeder()
        {
            var kayit = OturumKaydi.Uret(Calistir(), "senaryo.json");
            var s = SimFikstur.Yap(seed: kayit.Seed);
            s.Tick(1);
            Assert.Throws<InvalidOperationException>(() => OturumOynatici.Oynat(s, kayit));
        }

        [Theory]
        [InlineData("{")]
        [InlineData("[]")]
        [InlineData("{ \"surum\": 3, \"seed\": 1, \"senaryo\": \"s.json\", \"tick_sayisi\": 1, \"eylemler\": [], \"son_hash\": \"0000000000000000\" }")]
        [InlineData("{ \"surum\": 1, \"seed\": -1, \"senaryo\": \"s.json\", \"tick_sayisi\": 1, \"eylemler\": [], \"son_hash\": \"0000000000000000\" }")]
        [InlineData("{ \"surum\": 1, \"seed\": 1, \"senaryo\": \"s.json\", \"tick_sayisi\": -1, \"eylemler\": [], \"son_hash\": \"0000000000000000\" }")]
        [InlineData("{ \"surum\": 1, \"seed\": 1, \"senaryo\": \"s.json\", \"tick_sayisi\": 1, \"eylemler\": [], \"son_hash\": \"XYZ\" }")]
        [InlineData("{ \"surum\": 1, \"seed\": 1, \"senaryo\": \"s.json\", \"tick_sayisi\": 1, \"eylemler\": [ { \"tick\": 5, \"tur\": \"a\" } ], \"son_hash\": \"0000000000000000\" }")]
        [InlineData("{ \"surum\": 1, \"seed\": 1, \"senaryo\": \"s.json\", \"tick_sayisi\": 3, \"eylemler\": [ { \"tick\": 2, \"tur\": \"a\" }, { \"tick\": 1, \"tur\": \"a\" } ], \"son_hash\": \"0000000000000000\" }")]
        [InlineData("{ \"surum\": 1, \"seed\": 1, \"tick_sayisi\": 1, \"eylemler\": [], \"son_hash\": \"0000000000000000\" }")]
        [InlineData("{ \"surum\": 1, \"seed\": 1, \"senaryo\": \"s.json\", \"tick_sayisi\": 1, \"eylemler\": [], \"son_hash\": \"0000000000000000\", \"fazla\": 1 }")]
        public void Gecersiz_oturum_dosyasi_reddedilir(string json)
        {
            var sonuc = OturumKaydi.Oku(json, "oturum.json");
            Assert.False(sonuc.Basarili);
            Assert.All(sonuc.Hatalar, h => Assert.Equal("oturum.json", h.Kaynak));
        }

        [Fact]
        public void Buyuk_seed_kayipsiz_gidip_gelir()
        {
            var kayit = new OturumKaydi(ulong.MaxValue, "s.json", 0, new EylemKaydi[0], "0123456789abcdef");
            var oku = OturumKaydi.Oku(OturumKaydi.Yaz(kayit), "o.json");
            Assert.Equal(ulong.MaxValue, oku.Kayit!.Seed);
        }

        [Fact]
        public void Neden_yazici_olayi_ve_zincirini_okunur_satirlarla_yazar()
        {
            var s = SimFikstur.Yap();
            s.Submit(new PlayerAction("yardim_etti", "Ali"));
            s.Tick(1);
            var modifier = s.Gunluk.Olaylar.First(o => o.Tur == "modifier_eklendi" && o.Deger("npc") == "Ali");

            var satirlar = NedenYazici.Yaz(s.Gunluk, modifier.Id);

            Assert.StartsWith("#" + modifier.Id + " modifier_eklendi", satirlar[0]);
            Assert.Contains("yardim_minneti", satirlar[0]);
            Assert.Contains(satirlar, l => l.Contains("kanit_eklendi") && l.Contains("algi"));
            Assert.Contains(satirlar, l => l.Contains("yardim_etti") && l.Contains("player") && l.Contains("oyuncu"));
            // Girinti derinlikle artar; kök neden en içte.
            Assert.StartsWith(" ", satirlar.Last());
        }

        [Fact]
        public void Neden_yazici_tekrar_eden_dugumu_bir_kez_acar()
        {
            var s = SimFikstur.Yap();
            s.Submit(new PlayerAction("yardim_etti", "Ali"));
            s.Tick(1);
            var modifier = s.Gunluk.Olaylar.First(o => o.Tur == "modifier_eklendi" && o.Deger("npc") == "Ali");
            var eylem = s.Gunluk.Olaylar.First(o => o.Tur == "yardim_etti");

            var satirlar = NedenYazici.Yaz(s.Gunluk, modifier.Id);

            // Eylem hem modifier'ın hem kanıtın nedeni: ikinci görünüm "yukarıda" diye işaretlenir.
            Assert.Equal(1, satirlar.Count(l => l.Contains("#" + eylem.Id + " yardim_etti") && !l.Contains("yukarıda")));
        }

        [Fact]
        public void Neden_yazici_olmayan_olay_icin_hata_verir()
        {
            var s = SimFikstur.Yap();
            Assert.Throws<ArgumentOutOfRangeException>(() => NedenYazici.Yaz(s.Gunluk, 9999));
        }
    }
}
