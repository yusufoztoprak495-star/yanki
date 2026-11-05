using System;
using System.Linq;
using Xunit;

namespace Yanki.Dialogue.Tests.Sablon
{
    /// <summary>
    /// SablonYukleyici'nin yalan gizliliği ve zorunlu kriter dallarındaki kalan boşluklar (8. hafta düzeltme turu, madde 10). Her test bir doğrulayıcı dalını HATA ya da kabul sonucuyla sınar;
    /// yüzde için yazılmadı. Diğer dallar <c>SablonYukleyiciTests</c> ve <c>SablonYukleyiciDurakIkiTests</c> içindedir.
    /// </summary>
    public class SablonYukleyiciKapsamTests
    {
        private static string J(string s) => s.Replace('\'', '"');

        private static string Dosya(string sablon) => @"{
 'dil': 'tr',
 'adlar': { 'Ayse': 'Ayşe', 'Mehmet': 'Mehmet', 'Can': 'Can', 'oyuncu': { 'ad': 'yabancı', 'cins': true } },
 'onerme_metinleri': { 'kardes': { 'metin': '{0} ile {1} kardeş', 'olumsuz': '{0} ile {1} kardeş değil' } },
 'esleme': [ { 'olay_turu': 'anlatti', 'kaynak': 'npc', 'niyet': 'anlatma', 'konusan': 'fail' }, { 'olay_turu': 'anlatti', 'kaynak': 'oyuncu', 'niyet': 'anlatim_cevabi', 'konusan': 'hedef' },
   { 'olay_turu': 'yardim_etti', 'niyet': 'tesekkur', 'konusan': 'hedef' }, { 'olay_turu': 'kacti', 'niyet': 'kacis', 'konusan': 'fail', 'dinleyensiz': true } ],
 'sablonlar': [
  { 'id': 'anlat_genel', 'concept': 'anlatma', 'genel': true, 'metin': 'Dinle: #konu#.' },
  { 'id': 'cevap_genel', 'concept': 'anlatim_cevabi', 'genel': true, 'metin': 'Öyle mi? #konu#, demek.' },
  { 'id': 'tesekkur_genel', 'concept': 'tesekkur', 'genel': true, 'metin': 'Sağ ol.' },
  { 'id': 'kacis_genel', 'concept': 'kacis', 'genel': true, 'metin': 'Gidiyorum.' },
  " + sablon + @"
 ]
}";

        private static string[] Hatalar(string sablon) => SablonYukleyici.Yukle(J(Dosya(sablon)), "sablon.json").Hatalar.ToArray();

        private static string Hafizali(string kriterFail, string bildirimFail) =>
            "{ 'id': 'tesekkur_gecmis', 'concept': 'tesekkur', 'kriterler': [ { 'alan': 'hafiza_var(yardim_etti, " + kriterFail + ", 24)', 'op': '==', 'deger': true, 'zorunlu': true } ], "
            + "'hafiza': { 'tur': 'yardim_etti', 'fail': '" + bildirimFail + "', 'son_n_tick': 24 }, 'metin': '#hafiza.zaman# yardım etmiştin.' }";

        [Fact]
        public void Genel_sablon_hafiza_gondermesi_tasiyamaz()
        {
            var hatalar = Hatalar("{ 'id': 'tesekkur_genel2', 'concept': 'tesekkur', 'genel': true, 'hafiza': { 'tur': 'yardim_etti', 'fail': 'dinleyen', 'son_n_tick': 24 }, 'metin': '#hafiza.zaman# yardım etmiştin.' }");

            Assert.Contains(hatalar, h => h.Contains("tesekkur_genel2", StringComparison.Ordinal) && h.Contains("genel şablon hafiza göndermesi taşıyamaz", StringComparison.Ordinal));
        }

        // Rol, önerme argümanının İÇİNDE de denetlenir: olay.* rolü şablon bağlamında yoktur (yalan gizliliği: şablon yalnızca konuşanın zihnini görür).
        [Fact]
        public void Ic_ice_onerme_argumaninda_olay_rolu_reddedilir()
        {
            var hatalar = Hatalar("{ 'id': 'cevap_x', 'concept': 'anlatim_cevabi', 'konu_deseni': 'kardes(*,*)', 'kriterler': [ { 'alan': 'inanc(kardes(olay.fail, Can))', 'op': '>', 'deger': 0.5, 'zorunlu': true } ], 'metin': 'Hmm.' }");

            Assert.Contains(hatalar, h => h.Contains("cevap_x", StringComparison.Ordinal) && h.Contains("rolü şablon bağlamında yok", StringComparison.Ordinal));
        }

        [Fact]
        public void Dinleyensiz_niyette_onerme_icindeki_dinleyen_rolu_reddedilir()
        {
            var hatalar = Hatalar("{ 'id': 'kacis_x', 'concept': 'kacis', 'kriterler': [ { 'alan': 'inanc(kardes(dinleyen, Can))', 'op': '>', 'deger': 0.5, 'zorunlu': true } ], 'metin': 'Gitmeliyim.' }");

            Assert.Contains(hatalar, h => h.Contains("kacis_x", StringComparison.Ordinal) && h.Contains("dinleyensiz niyette \"dinleyen\" rolü", StringComparison.Ordinal));
        }

        // Hafıza göndermesi ↔ zorunlu hafiza_var uyumu: fail sabit kimlikse de aynı olmalı.
        [Fact]
        public void Hafiza_bildirimi_ile_zorunlu_hafiza_var_ayni_sabit_kimlikte_uyar()
        {
            Assert.Empty(Hatalar(Hafizali("Mehmet", "Mehmet")));
        }

        [Fact]
        public void Hafiza_bildirimi_ile_zorunlu_hafiza_var_farkli_sabit_kimlikte_uyusmaz()
        {
            var hatalar = Hatalar(Hafizali("Mehmet", "Can"));

            Assert.Contains(hatalar, h => h.Contains("tesekkur_gecmis", StringComparison.Ordinal) && h.Contains("uyuşmuyor", StringComparison.Ordinal));
        }

        // Zorunlu kriterin kendisi okunamıyorsa (bilinmeyen sorgu) hata sıra numarasıyla bildirilir; şablon sessizce kriteri olmayan şablona dönüşmez.
        [Fact]
        public void Okunamayan_kriter_sira_numarasiyla_hata_verir()
        {
            var hatalar = Hatalar("{ 'id': 'tesekkur_bozuk', 'concept': 'tesekkur', 'kriterler': [ { 'alan': 'kisilik(cesaret)', 'op': '>=', 'deger': 0.5, 'zorunlu': true }, { 'alan': 'olmayan_sorgu(dinleyen)', 'op': '==', 'deger': true, 'zorunlu': true } ], 'metin': 'Sağ ol.' }");

            Assert.Contains(hatalar, h => h.Contains("tesekkur_bozuk", StringComparison.Ordinal) && h.Contains("kriter #2:", StringComparison.Ordinal));
        }
    }
}
