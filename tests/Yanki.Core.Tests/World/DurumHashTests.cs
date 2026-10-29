using System.Text.RegularExpressions;
using Xunit;
using Yanki.Core.World;

namespace Yanki.Core.Tests.World
{
    public class DurumHashTests
    {
        [Fact]
        public void Bos_dunya_hashi_16_karakter_kucuk_harf_hex_ve_kararli()
        {
            var a = new Dunya().DurumHash();
            var b = new Dunya().DurumHash();

            Assert.Matches(new Regex("^[0-9a-f]{16}$"), a);
            Assert.Equal(a, b);
        }

        [Fact]
        public void Ayni_olay_dizisi_iki_kez_uygulaninca_ayni_hash()
        {
            Assert.Equal(OlayUretici.SabitDunya().DurumHash(), OlayUretici.SabitDunya().DurumHash());
        }

        [Fact]
        public void Farkli_nesne_orneklerinden_kurulan_olaylar_ayni_hash()
        {
            var u1 = new OlayUretici();
            var u2 = new OlayUretici();
            var d1 = new Dunya();
            var d2 = new Dunya();

            // İki ayrı üretici, iki ayrı Olay nesnesi kümesi; aynı içerik.
            d1.Uygula(u1.Varlik("Ali", "npc"));
            d1.Uygula(u1.GercekEkle("borclu(Ali,Ali,5)"));
            d2.Uygula(u2.Varlik("Ali", "npc"));
            d2.Uygula(u2.GercekEkle("borclu(Ali,Ali,5)"));

            Assert.Equal(d1.DurumHash(), d2.DurumHash());
        }

        [Fact]
        public void Ekleme_sirasi_farkli_ama_son_durum_ayni_ise_hash_ayni()
        {
            var u1 = new OlayUretici();
            var d1 = new Dunya();
            d1.Uygula(u1.Varlik("han", "mekan"));
            d1.Uygula(u1.Varlik("pazar", "mekan"));
            d1.Uygula(u1.Varlik("Ali", "npc"));
            d1.Uygula(u1.Bagla("han", "pazar"));
            d1.Uygula(u1.GercekEkle("dost(Ali,Ali)"));
            d1.Uygula(u1.GercekEkle("konum!(Ali,han)"));

            var u2 = new OlayUretici();
            var d2 = new Dunya();
            d2.Uygula(u2.Varlik("Ali", "npc"));
            d2.Uygula(u2.Varlik("pazar", "mekan"));
            d2.Uygula(u2.Varlik("han", "mekan"));
            d2.Uygula(u2.GercekEkle("konum!(Ali,han)"));
            d2.Uygula(u2.GercekEkle("dost(Ali,Ali)"));
            d2.Uygula(u2.Bagla("pazar", "han"));

            Assert.Equal(d1.DurumHash(), d2.DurumHash());
        }

        [Fact]
        public void Durum_farkliysa_hash_farkli()
        {
            var tabani = OlayUretici.SabitDunya().DurumHash();

            var u = new OlayUretici();
            var farkli = OlayUretici.SabitDunya();
            farkli.Uygula(new OlayUretici().GercekEkle("dost(Ali,Ayse)"));
            Assert.NotEqual(tabani, farkli.DurumHash());

            var yeniVarlik = OlayUretici.SabitDunya();
            yeniVarlik.Uygula(u.Varlik("Veli", "npc"));
            Assert.NotEqual(tabani, yeniVarlik.DurumHash());

            var yeniBaglanti = OlayUretici.SabitDunya();
            var v = new OlayUretici();
            yeniBaglanti.Uygula(v.Varlik("kale", "mekan"));
            var onceki = yeniBaglanti.DurumHash();
            yeniBaglanti.Uygula(v.Bagla("kale", "han"));
            Assert.NotEqual(onceki, yeniBaglanti.DurumHash());
        }

        [Fact]
        public void Varlik_turu_hashi_etkiler()
        {
            var u1 = new OlayUretici();
            var d1 = new Dunya();
            d1.Uygula(u1.Varlik("x", "npc"));

            var u2 = new OlayUretici();
            var d2 = new Dunya();
            d2.Uygula(u2.Varlik("x", "esya"));

            Assert.NotEqual(d1.DurumHash(), d2.DurumHash());
        }

        [Fact]
        public void Tek_degerli_gercegin_gecmisi_hashi_etkilemez()
        {
            var u1 = new OlayUretici();
            var d1 = new Dunya();
            d1.Uygula(u1.GercekEkle("konum!(Ali,han)"));
            d1.Uygula(u1.GercekEkle("konum!(Ali,pazar)"));
            d1.Uygula(u1.GercekEkle("konum!(Ali,kale)"));

            var u2 = new OlayUretici();
            var d2 = new Dunya();
            d2.Uygula(u2.GercekEkle("konum!(Ali,kale)"));

            Assert.Equal(d1.DurumHash(), d2.DurumHash());
        }

        [Fact]
        public void Eklenip_kaldirilan_gercek_hashi_etkilemez()
        {
            var u1 = new OlayUretici();
            var d1 = new Dunya();
            d1.Uygula(u1.GercekEkle("dost(Ali,Ayse)"));
            d1.Uygula(u1.GercekKaldir("dost(Ali,Ayse)"));

            Assert.Equal(new Dunya().DurumHash(), d1.DurumHash());
        }
    }
}
