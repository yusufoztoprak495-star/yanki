using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Simulation;
using Yanki.Dialogue.Gramer;

namespace Yanki.Dialogue.Tests
{
    /// <summary>
    /// DURAK 2 kararları (motor): kişi zamirleri (önerme metninde konuşan/dinleyen için ben/sen ve ekli hâlleri; özne varyantları), eşlemenin kaynağa göre ayrılması, dinleyensiz niyet
    /// (satır mekanı taşır), ton kuralları ve satır başı büyük harf.
    /// </summary>
    public class DurakIkiMotorTests
    {
        private static readonly SablonDosyasi Metinler = DiyalogFikstur.Sablon(@"{ 'dil': 'tr',
 'adlar': { 'Ali': 'Ali', 'Can': 'Can', 'Mehmet': 'Mehmet', 'Ayse': 'Ayşe', 'Veli': 'Veli', 'oyuncu': { 'ad': 'yabancı', 'cins': true } },
 'onerme_metinleri': {
  'borclu': { 'metin': '{0} {1.yonelme} {2} lira borçlu', 'olumsuz': '{0} {1.yonelme} {2} lira borçlu değil', 'sayilar': [2], 'ozne': [0],
    'ben': { '0': { 'metin': 'ben {1.yonelme} {2} lira borçluyum', 'olumsuz': 'ben {1.yonelme} {2} lira borçlu değilim' } },
    'sen': { '0': { 'metin': 'sen {1.yonelme} {2} lira borçlusun', 'olumsuz': 'sen {1.yonelme} {2} lira borçlu değilsin' } } },
  'kardes': { 'metin': '{0} ile {1} kardeş', 'olumsuz': '{0} ile {1} kardeş değil', 'ozne': [0, 1],
    'ben': { '0': { 'metin': 'ben ile {1} kardeşiz', 'olumsuz': 'ben ile {1} kardeş değiliz' }, '1': { 'metin': '{0} ile ben kardeşiz', 'olumsuz': '{0} ile ben kardeş değiliz' } },
    'sen': { '0': { 'metin': 'sen ile {1} kardeşsiniz', 'olumsuz': 'sen ile {1} kardeş değilsiniz' }, '1': { 'metin': '{0} ile sen kardeşsiniz', 'olumsuz': '{0} ile sen kardeş değilsiniz' } } },
  'tanir': { 'metin': '{0} {1.belirtme} tanır', 'olumsuz': '{0} {1.belirtme} tanımaz' },
  'evi': { 'metin': '{0} {1.iyelik} evi', 'olumsuz': '{0} {1.iyelik} evi değil' },
  'sahibi': { 'metin': '{1.tamlayan} sahibi {0}', 'olumsuz': '{1.tamlayan} sahibi {0} değil' }
 },
 'esleme': { 'anlatti': { 'niyet': 'anlatma', 'konusan': 'fail' } },
 'sablonlar': [ { 'id': 'g', 'concept': 'anlatma', 'genel': true, 'metin': 'x' } ] }");

        // ---- kişi zamirleri ----

        [Theory]
        [InlineData("borclu(Mehmet,Ali,500)", "Ali", "Can", "Mehmet bana 500 lira borçlu")]                        // Ayşe-benzeri: konuşan nesne konumunda → bana
        [InlineData("borclu(Mehmet,Can,500)", "Ali", "Can", "Mehmet sana 500 lira borçlu")]                        // dinleyen nesne konumunda → sana
        [InlineData("borclu(Mehmet,Ayse,500)", "Can", "Veli", "Mehmet Ayşe'ye 500 lira borçlu")]                   // kimse konuşan/dinleyen değil → isim
        [InlineData("borclu(Ali,Mehmet,500)", "Ali", "Can", "ben Mehmet'e 500 lira borçluyum")]                     // özne konuşan → ben varyantı
        [InlineData("borclu(Can,Mehmet,500)", "Ali", "Can", "sen Mehmet'e 500 lira borçlusun")]                     // özne dinleyen → sen varyantı
        [InlineData("degil(borclu(Ali,Mehmet,500))", "Ali", "Can", "ben Mehmet'e 500 lira borçlu değilim")]         // olumsuz varyant
        [InlineData("kardes(Mehmet,Can)", "Ali", "Can", "Mehmet ile sen kardeşsiniz")]
        [InlineData("kardes(Ali,Mehmet)", "Ali", "Can", "ben ile Mehmet kardeşiz")]
        [InlineData("kardes(Mehmet,Ali)", "Ali", "Can", "Mehmet ile ben kardeşiz")]
        [InlineData("tanir(Mehmet,Can)", "Ali", "Can", "Mehmet seni tanır")]
        [InlineData("tanir(Mehmet,Ali)", "Ali", "Can", "Mehmet beni tanır")]
        [InlineData("sahibi(Mehmet,Ali)", "Ali", "Can", "benim sahibi Mehmet")]
        [InlineData("sahibi(Mehmet,Can)", "Ali", "Can", "senin sahibi Mehmet")]
        [InlineData("sahibi(Mehmet,Veli)", "Ali", "Can", "Veli'nin sahibi Mehmet")]
        public void Konusan_ve_dinleyen_icin_isim_yerine_zamir_kullanilir(string onerme, string konusan, string dinleyen, string beklenen)
        {
            Assert.Equal(beklenen, OnermeMetni.Uret(onerme, Metinler, konusan, dinleyen));
        }

        [Fact]
        public void Ayse_Can_a_Mehmet_bana_500_lira_borclu_der_Can_a_Mehmet_ile_sen_kardessiniz_der()
        {
            Assert.Equal("Mehmet bana 500 lira borçlu", OnermeMetni.Uret("borclu(Mehmet,Ayse,500)", Metinler, "Ayse", "Can"));
            Assert.Equal("Mehmet ile sen kardeşsiniz", OnermeMetni.Uret("kardes(Mehmet,Can)", Metinler, "Ayse", "Can"));
        }

        [Fact]
        public void Ozne_bildirilmemis_yuklemde_konusan_ozne_konumundaysa_sessiz_yanlis_cumle_uretilmez()
        {
            Assert.Throws<InvalidOperationException>(() => OnermeMetni.Uret("tanir(Ali,Mehmet)", Metinler, "Ali", "Can"));   // 'tanir' ozne bildirmedi: tanir fiili 'tanırım' olmalıydı
            Assert.Throws<InvalidOperationException>(() => OnermeMetni.Uret("kardes(Ali,Can)", Metinler, "Ali", "Can"));    // hem konuşan hem dinleyen özne: kombinasyon varyantı yok
            Assert.Throws<InvalidOperationException>(() => OnermeMetni.Uret("evi(Mehmet,Ali)", Metinler, "Ali", "Can"));    // zamire iyelik eki getirilmez
        }

        // ---- eşleme: kaynak, dinleyensiz ----

        private static readonly SablonDosyasi Kaynakli = DiyalogFikstur.Sablon(@"{ 'dil': 'tr',
 'esleme': [
  { 'olay_turu': 'anlatti', 'kaynak': 'npc', 'niyet': 'anlatma', 'konusan': 'fail' },
  { 'olay_turu': 'anlatti', 'kaynak': 'oyuncu', 'niyet': 'anlatim_cevabi', 'konusan': 'hedef' },
  { 'olay_turu': 'kacti', 'niyet': 'kacis', 'konusan': 'fail', 'dinleyensiz': true },
  { 'olay_turu': 'sakinlesti', 'niyet': 'sakinlik', 'konusan': 'fail' }
 ],
 'sablonlar': [
  { 'id': 'a', 'concept': 'anlatma', 'genel': true, 'metin': 'Anlat.' },
  { 'id': 'c', 'concept': 'anlatim_cevabi', 'genel': true, 'metin': 'Cevap.' },
  { 'id': 'k', 'concept': 'kacis', 'genel': true, 'metin': 'Gidiyorum.' },
  { 'id': 's', 'concept': 'sakinlik', 'genel': true, 'metin': 'Sakinim.' }
 ] }");

        private static Olay Olayi(string tur, string fail, string hedef, string mekan = "han") =>
            new Olay(
                70, 4, 0, tur, fail, hedef, mekan,
                tur == "anlatti" ? new[] { new KeyValuePair<string, string>("onerme", "borclu(Mehmet,Ayse,500)") } : new KeyValuePair<string, string>[0], new long[0], "x");

        [Fact]
        public void Oyuncu_kaynakli_anlatimda_hedef_NPC_konusur_dinleyen_oyuncudur_niyet_ayridir()
        {
            var sim = DiyalogFikstur.Sim();

            var cevap = NiyetUretici.Uret(Olayi("anlatti", "oyuncu", "Ali"), sim, Kaynakli, new DiyalogAyarlari())!;
            var anlatim = NiyetUretici.Uret(Olayi("anlatti", "Ali", "Veli"), sim, Kaynakli, new DiyalogAyarlari())!;

            Assert.Equal(("Ali", "oyuncu", "anlatim_cevabi", "borclu(Mehmet,Ayse,500)"), (cevap.Konusan, cevap.Dinleyen, cevap.Ad, cevap.Konu));
            Assert.Equal(("Ali", "Veli", "anlatma"), (anlatim.Konusan, anlatim.Dinleyen, anlatim.Ad));
        }

        [Fact]
        public void Kaynaksiz_esleme_her_iki_kaynagi_kapsar_kaynakli_olmayan_kaynak_icin_satir_uretmez()
        {
            var sim = DiyalogFikstur.Sim();
            var yalnizNpc = DiyalogFikstur.Sablon(@"{ 'dil': 'tr', 'esleme': [ { 'olay_turu': 'anlatti', 'kaynak': 'npc', 'niyet': 'anlatma', 'konusan': 'fail' } ],
 'sablonlar': [ { 'id': 'a', 'concept': 'anlatma', 'genel': true, 'metin': 'x' } ] }");

            Assert.NotNull(NiyetUretici.Uret(Olayi("anlatti", "Ali", "Veli"), sim, yalnizNpc, new DiyalogAyarlari()));
            Assert.Null(NiyetUretici.Uret(Olayi("anlatti", "oyuncu", "Ali"), sim, yalnizNpc, new DiyalogAyarlari()));
            Assert.NotNull(NiyetUretici.Uret(Olayi("anlatti", "oyuncu", "Ali"), sim, DiyalogFikstur.Sablon(@"{ 'dil': 'tr', 'esleme': { 'anlatti': { 'niyet': 'anlatma', 'konusan': 'hedef' } },
 'sablonlar': [ { 'id': 'a', 'concept': 'anlatma', 'genel': true, 'metin': 'x' } ] }"), new DiyalogAyarlari()));
        }

        [Fact]
        public void Dinleyensiz_olay_satir_uretir_satir_mekani_tasir_dinleyen_bos()
        {
            var sim = DiyalogFikstur.Sim();

            var kacti = NiyetUretici.Uret(Olayi("kacti", "Ali", string.Empty), sim, Kaynakli, new DiyalogAyarlari())!;
            var sakinlesti = NiyetUretici.Uret(Olayi("sakinlesti", "Ali", string.Empty), sim, Kaynakli, new DiyalogAyarlari());

            Assert.Equal(("Ali", string.Empty, "kacis", "han"), (kacti.Konusan, kacti.Dinleyen, kacti.Ad, kacti.Mekan));
            Assert.Equal("yok", kacti.FikirBandi);
            Assert.Null(sakinlesti);   // dinleyensiz bildirilmeyen niyet hedefsiz olayda satır üretmez
        }

        [Fact]
        public void Dinleyensiz_satir_motor_uzerinden_uretilir_ve_mekan_tasir()
        {
            var sablon = DiyalogFikstur.Sablon(@"{ 'dil': 'tr', 'esleme': [ { 'olay_turu': 'yardim_etti', 'niyet': 'ic_ses', 'konusan': 'hedef', 'dinleyensiz': true } ],
 'sablonlar': [ { 'id': 'g', 'concept': 'ic_ses', 'genel': true, 'metin': 'hmm, bu da ne.' } ] }");
            var sim = DiyalogFikstur.Sim();
            var motor = DiyalogMotoru.Baglan(sim, sablon);
            sim.Submit(new PlayerAction("yardim_etti", "Ali"));
            sim.Tick(1);

            var satir = Assert.Single(motor.Satirlar);
            Assert.Equal(("Ali", string.Empty, "han", "Hmm, bu da ne."), (satir.Konusan, satir.Dinleyen, satir.Mekan, satir.Metin));
        }

        // ---- ton kuralları ----

        [Theory]
        [InlineData("ofke", 0.9, "ofkeli")]
        [InlineData("ofke", 0.6, "ofkeli")]
        [InlineData("ofke", 0.59, "gergin")]
        [InlineData("ofke", 0.2, "gergin")]
        [InlineData("ofke", 0.19, "notr")]
        [InlineData("korku", 0.2, "urkek")]
        [InlineData("korku", 0.19, "notr")]
        [InlineData("sevinc", 0.9, "notr")]
        public void Ton_kurallari_esikle_secilir_stres_ton_tablosuna_girmez(string duygu, double siddet, string beklenen)
        {
            Assert.Equal(beklenen, new DiyalogAyarlari().TonSec(duygu, (decimal)siddet));
        }

        // ---- satır başı büyük harf ----

        [Fact]
        public void Satirin_ilk_harfi_otomatik_buyur_noktali_i_ve_noktasiz_i_elle_tablo()
        {
            var sablon = DiyalogFikstur.Sablon(@"{ 'dil': 'tr', 'gramer': { 'cevap': ['ışık yok', 'iyi günler'] }, 'esleme': { 'yardim_etti': { 'niyet': 't', 'konusan': 'hedef' } },
 'sablonlar': [ { 'id': 'g', 'concept': 't', 'genel': true, 'metin': '#cevap#, sağ ol.' } ] }");
            var sim = DiyalogFikstur.Sim();
            var motor = DiyalogMotoru.Baglan(sim, sablon);
            for (var i = 0; i < 12; i++)
            {
                sim.Submit(new PlayerAction("yardim_etti", "Ali"));
                sim.Tick(1);
            }

            var metinler = motor.Satirlar.Select(s => s.Metin).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(new[] { "Işık yok, sağ ol.", "İyi günler, sağ ol." }, metinler);
        }
    }
}
