using System.Linq;
using Xunit;
using Yanki.Core.Criteria;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Tests.World;

namespace Yanki.Core.Tests.Mind
{
    /// <summary>
    /// 9. hafta (S kararı, Yusuf): <c>kok(onerme, kisi)</c>, <c>kaynak(onerme, kisi)</c> ile simetrik 19. sözlük fonksiyonu:
    /// ben'in o önerme için etkin kökü <c>kisi</c> olan herhangi bir kanıtı varsa doğru. Kök <i>kimin</i> söylediğini gösterir,
    /// söylentinin doğru ya da yalan olduğunu göstermez (yalan gizliliği değişmez).
    /// </summary>
    public class KokSorgusuTests
    {
        private const string Borc = "borclu(Mehmet,Ayse,500)";

        private static Zihin Yeni(string id = "Can") =>
            new Zihin(id, new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m), new ZihinAyarlari { KanitSonumuTickBasina = 0m });

        private static Olay Duydu(long id, string npc, string kaynak, string kok, string onerme = Borc) =>
            ZihinOlaylari.KanitEklendi(id, 1, 0, npc, onerme, "duydu", kaynak, 0.5m, new long[0], "test", string.Empty, kok);

        [Fact]
        public void Sozlukte_kok_iki_arguman_mantiksal_birinci_arguman_onerme()
        {
            var t = SorguSozlugu.Bul("kok");

            Assert.NotNull(t);
            Assert.Equal(2, t!.ArgumanSayisi);
            Assert.Equal(DonusTuru.Mantiksal, t.Donus);
            Assert.Equal(0, t.OnermeIndeksi);
            Assert.Equal(19, SorguSozlugu.Hepsi.Count);
        }

        [Fact]
        public void Zihin_KokVar_duydu_kanitinin_kokunu_kaynaktan_ayirir()
        {
            var z = Yeni();
            z.Uygula(Duydu(1, "Can", kaynak: "Ayse", kok: "oyuncu"));

            Assert.True(z.KokVar(OnermeDeseni.Ayristir("borclu(Mehmet,*,*)"), "oyuncu"));
            Assert.False(z.KokVar(OnermeDeseni.Ayristir("borclu(Mehmet,*,*)"), "Ayse"));   // doğrudan kaynak kök değil
            Assert.True(z.KaynakVar(OnermeDeseni.Ayristir("borclu(Mehmet,*,*)"), "Ayse"));
            Assert.False(z.KaynakVar(OnermeDeseni.Ayristir("borclu(Mehmet,*,*)"), "oyuncu"));
        }

        [Fact]
        public void Birden_cok_kanit_varken_herhangi_birinin_koku_yeter_ve_hicbiri_silinmez()
        {
            var z = Yeni();
            z.Uygula(Duydu(1, "Can", kaynak: "Ayse", kok: "oyuncu"));
            z.Uygula(Duydu(2, "Can", kaynak: "Osman", kok: "Kerem"));

            var desen = OnermeDeseni.Ayristir("borclu(Mehmet,*,*)");
            Assert.True(z.KokVar(desen, "oyuncu"));
            Assert.True(z.KokVar(desen, "Kerem"));
            Assert.False(z.KokVar(desen, "Veli"));
            Assert.False(z.KokVar(OnermeDeseni.Ayristir("borclu(Ali,*,*)"), "oyuncu"));
        }

        [Fact]
        public void Kokun_kendisi_ya_da_gordugu_onermede_kok_zihnin_sahibidir()
        {
            var z = Yeni();
            z.Uygula(ZihinOlaylari.KanitEklendi(1, 1, 0, "Can", "konum!(Mehmet,pazar)", "gordu", "Can", 0.9m, new long[0], "test"));
            // Kök yazılmamış eski duydu kanıtında anlatan kök sayılır (EtkinKok).
            z.Uygula(ZihinOlaylari.KanitEklendi(2, 1, 1, "Can", Borc, "duydu", "Ayse", 0.5m, new long[0], "test"));

            Assert.True(z.KokVar(OnermeDeseni.Ayristir("konum!(Mehmet,*)"), "Can"));
            Assert.False(z.KokVar(OnermeDeseni.Ayristir("konum!(Mehmet,*)"), "Ayse"));
            Assert.True(z.KokVar(OnermeDeseni.Ayristir("borclu(Mehmet,*,*)"), "Ayse"));
        }

        [Fact]
        public void Kok_kriter_dilinde_cozulur_ve_degil_onermesini_kabul_eder()
        {
            var zihinler = new ZihinDeposu();
            zihinler.Ekle(Yeni());
            zihinler.Uygula(Duydu(1, "Can", kaynak: "Ayse", kok: "oyuncu", onerme: "degil(" + Borc + ")"));
            var cozucu = new ZihinSorguCozucusu(OlayUretici.SabitDunya(), zihinler, new OlayGunlugu(), 0);
            var baglam = new SorguBaglami("Can", oyuncu: "oyuncu", konusan: "Can", dinleyen: "Ayse", olayFail: "Ali", olayHedef: "Mehmet");

            bool Dogru(string alan) => cozucu.Coz(IfadeAyristirici.Ayristir(alan), baglam).MantiksalDegeri;

            Assert.True(Dogru("kok(degil(borclu(Mehmet, *, *)), oyuncu)"));
            Assert.False(Dogru("kok(degil(borclu(Mehmet, *, *)), dinleyen)"));
            Assert.False(Dogru("kok(borclu(Mehmet, *, *), oyuncu)"));
        }

        [Fact]
        public void Kok_dogru_ve_yalan_anlatimda_ayni_gorunur_yalan_gizliligi()
        {
            // Aynı kök, aynı kaynak: önerme doğru da olsa yalan da olsa kanıt yapısı (ve kok cevabı) aynıdır.
            var dogru = Yeni();
            dogru.Uygula(Duydu(1, "Can", kaynak: "Ayse", kok: "Ayse", onerme: Borc));
            var yalan = Yeni();
            yalan.Uygula(Duydu(1, "Can", kaynak: "Ayse", kok: "Ayse", onerme: "degil(" + Borc + ")"));

            Assert.Equal(
                dogru.KokVar(OnermeDeseni.Ayristir("borclu(Mehmet,*,*)"), "Ayse"),
                yalan.KokVar(OnermeDeseni.Ayristir("degil(borclu(Mehmet,*,*))"), "Ayse"));
            Assert.Equal(
                dogru.Kanitlar(Borc).Select(k => (k.Kaynak, k.Kok, k.Guc)).ToArray(),
                yalan.Kanitlar("degil(" + Borc + ")").Select(k => (k.Kaynak, k.Kok, k.Guc)).ToArray());
        }
    }
}
