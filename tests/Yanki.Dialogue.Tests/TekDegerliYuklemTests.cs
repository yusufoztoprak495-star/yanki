using System;
using Xunit;
using Yanki.Dialogue.Gramer;

namespace Yanki.Dialogue.Tests
{
    /// <summary>9. hafta (Yusuf, DURAK 2 madde 3): <c>onerme_metinleri</c> tek değerli (<c>!</c> ile biten) yüklemi kabul eder; anahtar <c>konum!</c>. Çekirdek ve hash değişmez.</summary>
    public class TekDegerliYuklemTests
    {
        private const string Dosya = @"{ 'dil': 'tr',
 'adlar': { 'muhur': { 'ad': 'mühür', 'cins': true, 'yumusak': 'mühr' }, 'depo': { 'ad': 'depo', 'cins': true }, 'han': { 'ad': 'han', 'cins': true }, 'Can': 'Can' },
 'onerme_metinleri': { 'konum!': { 'metin': '{0.tamlayan} yeri {1.bulunma}', 'olumsuz': '{0.tamlayan} yeri {1.bulunma} değil' } },
 'esleme': { 'anlatti': { 'niyet': 'anlatma', 'konusan': 'fail' } },
 'sablonlar': [ { 'id': 'g', 'concept': 'anlatma', 'genel': true, 'metin': 'x' } ] }";

        [Theory]
        [InlineData("konum!(muhur,depo)", "mührün yeri depoda")]
        [InlineData("konum!(muhur,han)", "mührün yeri handa")]
        [InlineData("degil(konum!(muhur,depo))", "mührün yeri depoda değil")]
        public void Tek_degerli_yuklem_cumleye_cevrilir(string onerme, string beklenen)
        {
            var sablon = DiyalogFikstur.Sablon(Dosya);

            Assert.Equal(beklenen, OnermeMetni.Uret(onerme, sablon, "Can", "Can"), StringComparer.Ordinal);
        }

        [Theory]
        [InlineData("tamlayan", "mührün")]
        [InlineData("yonelme", "mühre")]
        [InlineData("iyelik", "mührü")]
        [InlineData("bulunma", "mühürde")]       // ünsüzle başlayan ek: gövde düşmez
        public void Unlu_dusmesi_yumusak_govde_kaydiyla_yazilir_ek_tablosu_sessizce_yanlis_uretmez(string ek, string beklenen)
        {
            Assert.Equal(beklenen, TurkceEkler.Uygula("mühür", false, ek, false, "mühr"));
            // Gövde yazılmadıysa düşmesiz biçim üretilir (yazarın sorumluluğu, README/authoring-guide'da belgeli): hata değil.
            Assert.Equal("mühürün", TurkceEkler.Uygula("mühür", false, "tamlayan"));
        }

        [Fact]
        public void Yuklem_adi_yine_ASCII_olmali_yalnizca_sondaki_unlem_kabul_edilir()
        {
            var yukle = SablonYukleyici.Yukle(Dosya.Replace("'konum!'", "'ko!num'").Replace('\'', '"'), "s.json");

            Assert.False(yukle.Basarili);
            Assert.Contains(yukle.Hatalar, h => h.Contains("ASCII", StringComparison.Ordinal));
            Assert.True(SablonYukleyici.Yukle(Dosya.Replace('\'', '"'), "s.json").Basarili);
        }
    }
}
