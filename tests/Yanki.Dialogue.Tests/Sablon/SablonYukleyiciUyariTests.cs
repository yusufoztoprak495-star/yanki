using System.Linq;
using Xunit;

namespace Yanki.Dialogue.Tests.Sablon
{
    /// <summary>
    /// Doğrulayıcı uyarıları (8. hafta düzeltme turu, madde 5): anlatma/anlatim_cevabi şablonunda BELİRLİ bir önermeyi okuyan kriter (inanc/kaynak) var ama
    /// konu_deseni yoksa şablon her konuda aday olur ve metni yanlış konuya çıkabilir. Hata değildir (dosya yüklenir), uyarıdır.
    /// </summary>
    public class SablonYukleyiciUyariTests
    {
        private static string J(string s) => s.Replace('\'', '"');

        private static string Dosya(string sablon) => @"{
 'dil': 'tr',
 'adlar': { 'Ayse': 'Ayşe', 'Mehmet': 'Mehmet', 'Can': 'Can', 'oyuncu': { 'ad': 'yabancı', 'cins': true } },
 'onerme_metinleri': { 'kardes': { 'metin': '{0} ile {1} kardeş', 'olumsuz': '{0} ile {1} kardeş değil', 'ozne': [0, 1],
   'ben': { '0': { 'metin': 'ben ile {1} kardeşiz', 'olumsuz': 'ben ile {1} kardeş değiliz' }, '1': { 'metin': '{0} ile ben kardeşiz', 'olumsuz': '{0} ile ben kardeş değiliz' } },
   'sen': { '0': { 'metin': 'sen ile {1} kardeşsiniz', 'olumsuz': 'sen ile {1} kardeş değilsiniz' }, '1': { 'metin': '{0} ile sen kardeşsiniz', 'olumsuz': '{0} ile sen kardeş değilsiniz' } } } },
 'esleme': [ { 'olay_turu': 'anlatti', 'kaynak': 'npc', 'niyet': 'anlatma', 'konusan': 'fail' }, { 'olay_turu': 'anlatti', 'kaynak': 'oyuncu', 'niyet': 'anlatim_cevabi', 'konusan': 'hedef' },
   { 'olay_turu': 'yardim_etti', 'niyet': 'tesekkur', 'konusan': 'hedef' } ],
 'sablonlar': [
  { 'id': 'anlat_genel', 'concept': 'anlatma', 'genel': true, 'metin': 'Dinle: #konu#.' },
  { 'id': 'cevap_genel', 'concept': 'anlatim_cevabi', 'genel': true, 'metin': 'Öyle mi? #konu#, demek.' },
  { 'id': 'tesekkur_genel', 'concept': 'tesekkur', 'genel': true, 'metin': 'Sağ ol.' },
  " + sablon + @"
 ]
}";

        private static SablonYuklemeSonucu Yukle(string sablon) => SablonYukleyici.Yukle(J(Dosya(sablon)), "sablon.json");

        private const string InananDesensiz =
            "{ 'id': 'cevap_inanan', 'concept': 'anlatim_cevabi', 'kriterler': [ { 'alan': 'inanc(kardes(Mehmet, Can))', 'op': '>', 'deger': 0.5, 'zorunlu': true } ], 'metin': 'Ben de öyle duymuştum.' }";

        [Fact]
        public void Anlatim_cevabinda_belirli_onerme_okuyan_kriter_konu_deseni_yoksa_uyarir()
        {
            var sonuc = Yukle(InananDesensiz);

            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            var uyari = Assert.Single(sonuc.Uyarilar);
            Assert.Contains("cevap_inanan", uyari);
            Assert.Contains("konu_deseni", uyari);
            Assert.Contains("sablon.json:", uyari);
        }

        [Fact]
        public void Konu_deseni_varsa_uyari_yok()
        {
            var sonuc = Yukle(InananDesensiz.Replace("'metin'", "'konu_deseni': 'kardes(Mehmet,Can)', 'metin'"));

            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            Assert.Empty(sonuc.Uyarilar);
        }

        [Fact]
        public void Anlatma_niyetinde_inanc_hata_olarak_kalir_uyari_uretmez()
        {
            // NPC anlatımında (anlatma) inanc/kaynak zaten yalan gizliliği HATASIdır; hata varken uyarı eklenmez.
            var sonuc = Yukle(InananDesensiz.Replace("anlatim_cevabi", "anlatma"));

            Assert.False(sonuc.Basarili);   // yalan gizliliği hatası: uyarıdan önce gelir
            Assert.Empty(sonuc.Uyarilar);
        }

        [Fact]
        public void Belirli_onerme_okumayan_kriter_uyarmaz()
        {
            var sonuc = Yukle("{ 'id': 'cevap_supheli', 'concept': 'anlatim_cevabi', 'kriterler': [ { 'alan': 'kisilik(durustluk)', 'op': '>=', 'deger': 0.7, 'zorunlu': true } ], 'metin': 'İnanması zor.' }");

            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            Assert.Empty(sonuc.Uyarilar);
        }

        [Fact]
        public void Anlatti_ile_eslesmeyen_niyette_uyarmaz()
        {
            var sonuc = Yukle("{ 'id': 'tesekkur_inanan', 'concept': 'tesekkur', 'kriterler': [ { 'alan': 'inanc(kardes(Mehmet, Can))', 'op': '>', 'deger': 0.5, 'zorunlu': true } ], 'metin': 'Sağ ol.' }");

            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            Assert.Empty(sonuc.Uyarilar);
        }

        [Fact]
        public void Hatasiz_ve_uyarisiz_dosyada_uyarilar_bos_liste()
        {
            var sonuc = Yukle("{ 'id': 'cevap_x', 'concept': 'anlatim_cevabi', 'kriterler': [ { 'alan': 'kisilik(durustluk)', 'op': '>=', 'deger': 0.7, 'zorunlu': true } ], 'metin': 'Hmm.' }");

            Assert.NotNull(sonuc.Uyarilar);
            Assert.Empty(sonuc.Uyarilar);
        }
    }
}
