using System;
using System.Linq;
using Xunit;
using Yanki.Core.World;

namespace Yanki.Core.Tests.World
{
    public class DunyaTests
    {
        [Fact]
        public void Varlik_eklenir_ve_tekrar_eklemek_hata_verir()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();

            Assert.True(dunya.Uygula(u.Varlik("Ali", "npc")));
            Assert.True(dunya.VarlikVar("Ali"));
            Assert.Equal(VarlikTuru.Npc, dunya.TurOf("Ali"));
            Assert.Null(dunya.TurOf("Yok"));
            Assert.Throws<InvalidOperationException>(() => dunya.Uygula(u.Varlik("Ali", "npc")));
        }

        [Fact]
        public void Varlik_turleri_ve_listeleme_ordinal_sirali()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();
            dunya.Uygula(u.Varlik("b", "npc"));
            dunya.Uygula(u.Varlik("B", "npc"));
            dunya.Uygula(u.Varlik("a", "npc"));
            dunya.Uygula(u.Varlik("han", "mekan"));
            dunya.Uygula(u.Varlik("kilic", "esya"));

            Assert.Equal(new[] { "B", "a", "b" }, dunya.Varliklar(VarlikTuru.Npc).ToArray());
            Assert.Equal(new[] { "han" }, dunya.Varliklar(VarlikTuru.Mekan).ToArray());
            Assert.Equal(new[] { "kilic" }, dunya.Varliklar(VarlikTuru.Esya).ToArray());
        }

        [Fact]
        public void Oyuncu_ayri_bir_varlik_turudur_ve_esya_npc_mekan_listelerinde_gorunmez()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();
            dunya.Uygula(u.Varlik("han", "mekan"));
            dunya.Uygula(u.Varlik("Ali", "npc"));
            dunya.Uygula(u.Varlik("kilic", "esya"));
            dunya.Uygula(u.Varlik("oyuncu", "oyuncu"));
            dunya.Uygula(u.GercekEkle("konum!(oyuncu,han)"));

            Assert.Equal(VarlikTuru.Oyuncu, dunya.TurOf("oyuncu"));
            Assert.Equal(new[] { "oyuncu" }, dunya.Varliklar(VarlikTuru.Oyuncu).ToArray());
            Assert.Equal(new[] { "kilic" }, dunya.Varliklar(VarlikTuru.Esya).ToArray());
            Assert.Equal(new[] { "Ali" }, dunya.Varliklar(VarlikTuru.Npc).ToArray());
            Assert.Equal(new[] { "han" }, dunya.Varliklar(VarlikTuru.Mekan).ToArray());
            Assert.Equal("han", dunya.TekDeger("konum!", "oyuncu"));
        }

        [Fact]
        public void Oyuncu_mekanlara_baglanamaz_ve_kimligi_tekrar_eklenemez()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();
            dunya.Uygula(u.Varlik("han", "mekan"));
            dunya.Uygula(u.Varlik("oyuncu", "oyuncu"));

            Assert.Throws<InvalidOperationException>(() => dunya.Uygula(u.Bagla("han", "oyuncu")));
            Assert.Throws<InvalidOperationException>(() => dunya.Uygula(u.Varlik("oyuncu", "esya")));
        }

        [Fact]
        public void Oyuncu_kanonik_metinde_ve_hashte_kendi_turuyle_yazilir()
        {
            var u = new OlayUretici();
            var oyuncu = new Dunya();
            oyuncu.Uygula(u.Varlik("x", "oyuncu"));
            var esya = new Dunya();
            esya.Uygula(new OlayUretici().Varlik("x", "esya"));

            Assert.Contains("V|x|oyuncu\n", oyuncu.KanonikMetin());
            Assert.NotEqual(esya.DurumHash(), oyuncu.DurumHash());
        }

        [Fact]
        public void Bilinmeyen_varlik_turu_hata_verir()
        {
            var u = new OlayUretici();

            Assert.Throws<ArgumentException>(() => new Dunya().Uygula(u.Varlik("Ali", "ejderha")));
        }

        [Theory]
        [InlineData("Ali Veli")]
        [InlineData("a,b")]
        [InlineData("a(b)")]
        [InlineData("ğ")]
        [InlineData("")]
        [InlineData("1a")]
        [InlineData("*")]
        [InlineData("a!")]
        public void Gecersiz_kimlik_hata_verir(string kimlik)
        {
            var u = new OlayUretici();

            Assert.Throws<ArgumentException>(() => new Dunya().Uygula(u.Varlik(kimlik, "npc")));
        }

        [Fact]
        public void Gercek_eklenir_ve_tekrar_eklemek_idempotent()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();

            dunya.Uygula(u.GercekEkle("borclu(Mehmet,Ayse,500)"));
            dunya.Uygula(u.GercekEkle("borclu(Mehmet,Ayse,500)"));

            Assert.True(dunya.Var(Gercek.Ayristir("borclu(Mehmet,Ayse,500)")));
            Assert.Single(dunya.Eslesen(new GercekDeseni("borclu", new string?[] { null, null, null })));
        }

        [Fact]
        public void Cok_degerli_gercekler_birikir()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();

            dunya.Uygula(u.GercekEkle("dost(Ali,Ayse)"));
            dunya.Uygula(u.GercekEkle("dost(Ali,Mehmet)"));

            Assert.Equal(2, dunya.Eslesen(new GercekDeseni("dost", new string?[] { "Ali", null })).Count);
        }

        [Fact]
        public void TekDegerli_yeni_deger_eskiyi_dusurur()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();

            dunya.Uygula(u.GercekEkle("konum!(Ali,han)"));
            dunya.Uygula(u.GercekEkle("konum!(Ali,pazar)"));

            Assert.False(dunya.Var(Gercek.Ayristir("konum!(Ali,han)")));
            Assert.True(dunya.Var(Gercek.Ayristir("konum!(Ali,pazar)")));
            Assert.Equal("pazar", dunya.TekDeger("konum!", "Ali"));
            Assert.Single(dunya.Eslesen(new GercekDeseni("konum!", new string?[] { "Ali", null })));
        }

        [Fact]
        public void TekDegerli_farkli_anahtarlar_birbirini_etkilemez()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();

            dunya.Uygula(u.GercekEkle("konum!(Ali,han)"));
            dunya.Uygula(u.GercekEkle("konum!(Ayse,pazar)"));
            dunya.Uygula(u.GercekEkle("hava!(han,yagmurlu)"));

            Assert.Equal("han", dunya.TekDeger("konum!", "Ali"));
            Assert.Equal("pazar", dunya.TekDeger("konum!", "Ayse"));
            Assert.Equal("yagmurlu", dunya.TekDeger("hava!", "han"));
            Assert.Null(dunya.TekDeger("konum!", "Mehmet"));
        }

        [Fact]
        public void TekDeger_tekdegerli_olmayan_yuklemde_hata_verir()
        {
            Assert.Throws<ArgumentException>(() => new Dunya().TekDeger("konum", "Ali"));
        }

        [Fact]
        public void Gercek_kaldirilir_olmayani_kaldirmak_hata_verir()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();
            dunya.Uygula(u.GercekEkle("dost(Ali,Ayse)"));
            dunya.Uygula(u.GercekEkle("konum!(Ali,han)"));

            dunya.Uygula(u.GercekKaldir("dost(Ali,Ayse)"));
            dunya.Uygula(u.GercekKaldir("konum!(Ali,han)"));

            Assert.False(dunya.Var(Gercek.Ayristir("dost(Ali,Ayse)")));
            Assert.Null(dunya.TekDeger("konum!", "Ali"));
            Assert.Throws<InvalidOperationException>(() => dunya.Uygula(u.GercekKaldir("dost(Ali,Ayse)")));
        }

        [Fact]
        public void TekDegerli_kaldirirken_guncel_deger_eslesmeli()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();
            dunya.Uygula(u.GercekEkle("konum!(Ali,pazar)"));

            // Eski değer zaten düşmüştü; onu kaldırmaya çalışmak olmayan bir gerçeği kaldırmaktır.
            Assert.Throws<InvalidOperationException>(() => dunya.Uygula(u.GercekKaldir("konum!(Ali,han)")));
            Assert.Equal("pazar", dunya.TekDeger("konum!", "Ali"));
        }

        [Fact]
        public void Mekan_baglantisi_yonsuz_ve_komsular_sirali()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();
            foreach (var m in new[] { "han", "pazar", "kale", "degirmen" })
            {
                dunya.Uygula(u.Varlik(m, "mekan"));
            }

            dunya.Uygula(u.Bagla("han", "pazar"));
            dunya.Uygula(u.Bagla("kale", "han"));
            dunya.Uygula(u.Bagla("han", "degirmen"));
            dunya.Uygula(u.Bagla("pazar", "han"));   // tekrar: idempotent

            Assert.Equal(new[] { "degirmen", "kale", "pazar" }, dunya.Komsular("han").ToArray());
            Assert.Equal(new[] { "han" }, dunya.Komsular("pazar").ToArray());
            Assert.True(dunya.BagliMi("han", "pazar"));
            Assert.True(dunya.BagliMi("pazar", "han"));
            Assert.False(dunya.BagliMi("pazar", "kale"));
            Assert.Empty(new Dunya().Komsular("yok"));
        }

        [Fact]
        public void Mekan_olmayan_varliga_ya_da_bilinmeyene_baglamak_hata_verir()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();
            dunya.Uygula(u.Varlik("han", "mekan"));
            dunya.Uygula(u.Varlik("Ali", "npc"));

            Assert.Throws<InvalidOperationException>(() => dunya.Uygula(u.Bagla("han", "Ali")));
            Assert.Throws<InvalidOperationException>(() => dunya.Uygula(u.Bagla("han", "yok")));
        }

        [Fact]
        public void Kendine_baglanti_hata_verir()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();
            dunya.Uygula(u.Varlik("han", "mekan"));

            Assert.Throws<InvalidOperationException>(() => dunya.Uygula(u.Bagla("han", "han")));
        }

        [Fact]
        public void Dunya_disi_olay_yoksayilir_ve_false_doner()
        {
            var u = new OlayUretici();
            var dunya = new Dunya();

            Assert.False(dunya.Uygula(u.Baska()));
            Assert.Empty(dunya.Varliklar(VarlikTuru.Npc));
        }

        [Fact]
        public void Eslesen_sirasi_ekleme_sirasindan_bagimsiz()
        {
            var a = new OlayUretici();
            var d1 = new Dunya();
            d1.Uygula(a.GercekEkle("dost(Ali,Mehmet)"));
            d1.Uygula(a.GercekEkle("dost(Ali,Ayse)"));

            var b = new OlayUretici();
            var d2 = new Dunya();
            d2.Uygula(b.GercekEkle("dost(Ali,Ayse)"));
            d2.Uygula(b.GercekEkle("dost(Ali,Mehmet)"));

            var desen = new GercekDeseni("dost", new string?[] { null, null });
            Assert.Equal(new[] { "dost(Ali,Ayse)", "dost(Ali,Mehmet)" }, d1.Eslesen(desen).Select(g => g.ToString()).ToArray());
            Assert.Equal(d1.Eslesen(desen).Select(g => g.ToString()), d2.Eslesen(desen).Select(g => g.ToString()));
        }
    }
}
