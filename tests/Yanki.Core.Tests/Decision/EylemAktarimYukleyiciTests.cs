using System.Linq;
using Xunit;
using Yanki.Core.Decision;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>6. hafta eylem şeması: <c>aktarim</c>, <c>konu_desenleri</c>, <c>yalan_onermeleri</c>, <c>gorunurluk</c>, <c>stres_tabani</c> (ADR-012).</summary>
    public class EylemAktarimYukleyiciTests
    {
        private const string DogruEk = "\"aktarim\": \"dogru\", \"konu_desenleri\": [\"borclu(*,*,*)\"]";
        private const string YalanEk = "\"aktarim\": \"yalan\", \"yalan_onermeleri\": [\"degil(borclu(Mehmet,{eylem.hedef},500))\"]";

        private static string Eylem(string ek, string id = "anlat", string olay = "anlatti", string hedef = "npc") =>
            "{ \"id\": \"" + id + "\", " + (olay.Length > 0 ? "\"olay_turu\": \"" + olay + "\", " : string.Empty)
            + "\"hedef\": \"" + hedef + "\", \"taban\": 1" + (ek.Length > 0 ? ", " + ek : string.Empty) + " }";

        private static EylemYuklemeSonucu Yukle(string eylem) => EylemYukleyici.Yukle(KararFikstur.Dosya(eylem), "e.json");

        private static void Gecer(string eylem)
        {
            var s = Yukle(eylem);
            Assert.True(s.Basarili, string.Join("\n", s.Hatalar));
        }

        private static void HataVar(string eylem, string parca)
        {
            var s = Yukle(eylem);
            Assert.False(s.Basarili, "Hata bekleniyordu: " + parca);
            Assert.Contains(s.Hatalar, h => h.ToString().Contains(parca));
        }

        [Fact]
        public void Dogru_aktarim_etkisiz_olabilir_ve_varsayilan_gorunurluk_sessizdir()
        {
            var s = Yukle(Eylem(DogruEk));
            Assert.True(s.Basarili, string.Join("\n", s.Hatalar));

            var e = s.Eylemler.Single();
            Assert.Equal(AktarimTuru.Dogru, e.Aktarim);
            Assert.Equal("sessiz", e.Gorunurluk);
            Assert.Equal("anlatti", e.OlayTuru);
            Assert.Single(e.KonuDesenleri);
            Assert.Empty(e.Etkiler);
            Assert.Null(e.StresTabani);
        }

        [Fact]
        public void Yalan_aktarimi_sablonlu_yalan_onermeleri_alir()
        {
            var s = Yukle(Eylem(YalanEk, "yalan_soyle"));
            Assert.True(s.Basarili, string.Join("\n", s.Hatalar));

            var e = s.Eylemler.Single();
            Assert.Equal(AktarimTuru.Yalan, e.Aktarim);
            Assert.Equal(new[] { "degil(borclu(Mehmet,{eylem.hedef},500))" }, e.YalanOnermeleri);
            Assert.Equal(new[] { "degil(borclu(Mehmet,Can,500))" }, e.YalanOnermeleriniAc("Ayse", "Can"));
        }

        [Fact]
        public void Aktarim_etkileri_de_alabilir() =>
            Gecer(Eylem(DogruEk + ", \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.5 } ]"));

        [Theory]
        [InlineData("acik")]
        [InlineData("sessiz")]
        public void Gorunurluk_acik_ya_da_sessiz(string g) =>
            Gecer(Eylem(DogruEk + ", \"gorunurluk\": \"" + g + "\""));

        [Fact]
        public void Gizli_gorunurluk_aktarimda_anlamsizdir() =>
            HataVar(Eylem(DogruEk + ", \"gorunurluk\": \"gizli\""), "gorunurluk");

        [Fact]
        public void Gorunurluk_yalnizca_aktarim_eyleminde_yazilir() =>
            HataVar(Eylem("\"gorunurluk\": \"acik\", \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.5 } ]", "selam", "selam_oldu"), "gorunurluk");

        [Fact]
        public void Gecersiz_aktarim_degeri_reddedilir() =>
            HataVar(Eylem("\"aktarim\": \"kandir\""), "aktarim");

        [Theory]
        [InlineData("selam_oldu")]
        [InlineData("yalan_soyledi_oldu")]
        public void Aktarim_eyleminin_olay_turu_anlatti_olmak_zorundadir(string olay) =>
            HataVar(Eylem(YalanEk, "yalan_soyle", olay), "anlatti");

        [Fact]
        public void Dogru_aktarim_da_anlatti_olmak_zorundadir_yoksa_tur_yalani_ele_verir() =>
            HataVar(Eylem(DogruEk, "anlat", "dedikodu"), "anlatti");

        [Fact]
        public void Aktarimsiz_eylem_anlatti_turunu_kullanamaz() =>
            HataVar(Eylem("\"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.5 } ]", "selam", "anlatti"), "anlatti");

        [Fact]
        public void Aktarim_eyleminde_olay_turu_zorunludur() =>
            HataVar(Eylem(DogruEk, "anlat", string.Empty), "olay_turu");

        [Theory]
        [InlineData("yok")]
        [InlineData("komsu_mekan")]
        public void Aktarim_npc_ya_da_oyuncu_hedef_ister(string hedef) =>
            HataVar(Eylem(DogruEk, "anlat", "anlatti", hedef), "hedef");

        [Fact]
        public void Aktarim_oyuncu_hedefli_olabilir() =>
            Gecer(Eylem(DogruEk, "anlat_oyuncuya", "anlatti", "oyuncu"));

        [Fact]
        public void Dogru_aktarimda_konu_desenleri_zorunludur() =>
            HataVar(Eylem("\"aktarim\": \"dogru\""), "konu_desenleri");

        [Theory]
        [InlineData("[]")]
        [InlineData("[\"borclu(\"]")]
        [InlineData("[\"\"]")]
        [InlineData("[5]")]
        [InlineData("[\"*(a,b)\"]")]
        [InlineData("[\"degil(*(a,b))\"]")]
        [InlineData("\"borclu(*,*,*)\"")]
        public void Gecersiz_konu_desenleri_reddedilir(string desenler) =>
            HataVar(Eylem("\"aktarim\": \"dogru\", \"konu_desenleri\": " + desenler), "konu_desenleri");

        [Fact]
        public void Konu_desenlerinde_anlatti_uyari_verir_ama_gecerlidir()
        {
            var s = Yukle(Eylem("\"aktarim\": \"dogru\", \"konu_desenleri\": [\"anlatti(*,*)\", \"borclu(*,*,*)\"]"));
            Assert.True(s.Basarili);
            Assert.Contains(s.Uyarilar, u => u.ToString().Contains("anlatti"));
        }

        [Fact]
        public void Dogru_aktarimda_yalan_onermeleri_yazilamaz() =>
            HataVar(Eylem(DogruEk + ", \"yalan_onermeleri\": [\"x(a)\"]"), "yalan_onermeleri");

        [Fact]
        public void Yalan_aktarimda_konu_desenleri_yazilamaz() =>
            HataVar(Eylem(YalanEk + ", \"konu_desenleri\": [\"borclu(*,*,*)\"]"), "konu_desenleri");

        [Fact]
        public void Yalan_aktarimda_yalan_onermeleri_zorunludur() =>
            HataVar(Eylem("\"aktarim\": \"yalan\""), "yalan_onermeleri");

        [Theory]
        [InlineData("[]")]
        [InlineData("[\"\"]")]
        [InlineData("[\"borclu(\"]")]
        [InlineData("[\"degil(borclu(M,{bilinmeyen},5))\"]")]
        [InlineData("[7]")]
        public void Bos_ya_da_gecersiz_yalan_onermesi_reddedilir(string liste) =>
            HataVar(Eylem("\"aktarim\": \"yalan\", \"yalan_onermeleri\": " + liste), "yalan_onermeleri");

        [Fact]
        public void Yalan_onermesinde_ben_ve_eylem_hedef_sablonlari_gecerlidir()
        {
            var s = Yukle(Eylem("\"aktarim\": \"yalan\", \"yalan_onermeleri\": [\"degil(borclu({ben},{eylem.hedef},5))\"]"));
            Assert.True(s.Basarili, string.Join("\n", s.Hatalar));
            Assert.Equal(new[] { "degil(borclu(Ayse,Can,5))" }, s.Eylemler.Single().YalanOnermeleriniAc("Ayse", "Can"));
        }

        [Fact]
        public void Aktarimsiz_eylemde_konu_ve_yalan_alanlari_reddedilir()
        {
            const string etki = ", \"etkiler\": [ { \"tur\": \"hafiza\", \"onem\": 0.5 } ]";
            HataVar(Eylem("\"konu_desenleri\": [\"a(*)\"]" + etki, "selam", "selam_oldu"), "konu_desenleri");
            HataVar(Eylem("\"yalan_onermeleri\": [\"a(b)\"]" + etki, "selam", "selam_oldu"), "yalan_onermeleri");
        }

        // ---- stres ----

        [Fact]
        public void Celisen_ozellik_varsa_stres_tabani_zorunludur() =>
            HataVar(Eylem(YalanEk + ", \"celisen_ozellikler\": [\"durustluk\"]", "yalan_soyle"), "stres_tabani");

        [Fact]
        public void Celisen_ozellik_ve_stres_tabani_birlikte_gecerlidir()
        {
            var s = Yukle(Eylem(YalanEk + ", \"celisen_ozellikler\": [\"durustluk\"], \"stres_tabani\": 15", "yalan_soyle"));
            Assert.True(s.Basarili, string.Join("\n", s.Hatalar));
            Assert.Equal(15m, s.Eylemler.Single().StresTabani);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-5")]
        [InlineData("101")]
        [InlineData("\"15\"")]
        public void Stres_tabani_0_ile_100_arasinda_bir_sayi_olmali(string deger) =>
            HataVar(Eylem(YalanEk + ", \"celisen_ozellikler\": [\"durustluk\"], \"stres_tabani\": " + deger, "yalan_soyle"), "stres_tabani");

        [Fact]
        public void Celisensiz_stres_tabani_uyari_verir()
        {
            var s = Yukle(Eylem(YalanEk + ", \"stres_tabani\": 15", "yalan_soyle"));
            Assert.True(s.Basarili);
            Assert.Contains(s.Uyarilar, u => u.ToString().Contains("stres_tabani"));
        }

        [Fact]
        public void Hata_biclimi_dosya_satir_eylem_id_mesajdir()
        {
            var s = Yukle(Eylem("\"aktarim\": \"kandir\"", "anlat"));
            Assert.Contains(s.Hatalar, h => h.ToString().StartsWith("e.json:") && h.ToString().Contains("eylem \"anlat\":"));
        }

        [Fact]
        public void Mevcut_eylemler_degismeden_yuklenir()
        {
            var s = EylemYukleyici.Yukle(KararFikstur.Dosya(KararFikstur.Eylem("selam", "yok")), "e.json");
            Assert.True(s.Basarili);
            var e = s.Eylemler.Single();
            Assert.Equal(AktarimTuru.Yok, e.Aktarim);
            Assert.Null(e.StresTabani);
            Assert.Empty(e.KonuDesenleri);
            Assert.Empty(e.YalanOnermeleri);
        }
    }
}
