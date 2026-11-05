using System;
using System.Linq;
using Xunit;

namespace Yanki.Dialogue.Tests.Sablon
{
    /// <summary>
    /// DURAK 2 kararları (yükleyici/doğrulayıcı): eşleme kaynağa göre ayrılır (oyuncu | npc), anlatıcı yasakları yalnızca konuşanın anlatıcı olduğu niyetlerde, dinleyensiz niyet,
    /// kişi zamiri varyantları (özne), ünsüz yumuşaması (yumuşak gövde zorunlu) ve yeni ekler.
    /// </summary>
    public class SablonYukleyiciDurakIkiTests
    {
        private static string J(string s) => s.Replace('\'', '"');

        private static string[] Hatalar(string json) => SablonYukleyici.Yukle(J(json), "sablon.json").Hatalar.ToArray();

        private static bool Basarili(string json) => SablonYukleyici.Yukle(J(json), "sablon.json").Basarili;

        private const string Genel = "{ 'id': 'a_genel', 'concept': 'anlatma', 'genel': true, 'metin': 'x' }, { 'id': 'c_genel', 'concept': 'anlatim_cevabi', 'genel': true, 'metin': 'y' }";

        private static string Dosya(string esleme, string sablonlar, string ek = "") =>
            "{ 'dil': 'tr', " + ek + " 'esleme': " + esleme + ", 'sablonlar': [ " + sablonlar + " ] }";

        private const string KaynakEsleme =
            "[ { 'olay_turu': 'anlatti', 'kaynak': 'npc', 'niyet': 'anlatma', 'konusan': 'fail' }, { 'olay_turu': 'anlatti', 'kaynak': 'oyuncu', 'niyet': 'anlatim_cevabi', 'konusan': 'hedef' } ]";

        // ---- eşleme: kaynak ----

        [Fact]
        public void Esleme_dizi_bicimi_kaynaga_gore_ayrilir_ve_yuklenir()
        {
            Assert.True(Basarili(Dosya(KaynakEsleme, Genel)), string.Join("\n", Hatalar(Dosya(KaynakEsleme, Genel))));
        }

        [Fact]
        public void Esleme_ayni_olay_ve_kaynak_iki_kez_ya_da_gecersiz_kaynak_hatadir()
        {
            var iki = "[ { 'olay_turu': 'anlatti', 'kaynak': 'npc', 'niyet': 'anlatma', 'konusan': 'fail' }, { 'olay_turu': 'anlatti', 'kaynak': 'npc', 'niyet': 'anlatma', 'konusan': 'hedef' } ]";
            Assert.Contains(Hatalar(Dosya(iki, Genel)), h => h.Contains("iki kez", StringComparison.Ordinal));

            var kotu = "[ { 'olay_turu': 'anlatti', 'kaynak': 'herkes', 'niyet': 'anlatma', 'konusan': 'fail' } ]";
            Assert.Contains(Hatalar(Dosya(kotu, Genel)), h => h.Contains("\"kaynak\" oyuncu ya da npc", StringComparison.Ordinal));
        }

        private static string Sablonlu(string ozelSablon) => Genel + ", " + ozelSablon;

        [Theory]
        [InlineData("inanc(borclu(*,*,*))", ">=", "0.5")]
        [InlineData("kaynak(borclu(*,*,*), oyuncu)", "==", "true")]
        [InlineData("hafiza_var(yardim_etti, oyuncu, 168)", "==", "true")]
        public void Anlatici_yasaklari_yalnizca_konusanin_anlatici_oldugu_niyette_gecerli_cevap_veren_kendi_inancini_kullanabilir(string alan, string op, string deger)
        {
            var kriter = "[ { 'alan': '" + alan + "', 'op': '" + op + "', 'deger': " + deger + ", 'zorunlu': true } ]";

            // anlatma (NPC anlatıcı): reddedilir
            Assert.Contains(
                Hatalar(Dosya(KaynakEsleme, Sablonlu("{ 'id': 'a_ozel', 'concept': 'anlatma', 'kriterler': " + kriter + ", 'metin': 'x' }"))),
                h => h.Contains("yalan gizliliği", StringComparison.Ordinal));

            // anlatim_cevabi (oyuncunun anlatımına cevap veren NPC): serbest
            var cevap = Dosya(KaynakEsleme, Sablonlu("{ 'id': 'c_ozel', 'concept': 'anlatim_cevabi', 'kriterler': " + kriter + ", 'metin': 'x' }"));
            Assert.True(Basarili(cevap), string.Join("\n", Hatalar(cevap)));
        }

        [Fact]
        public void Cevap_veren_NPCnin_sablonunda_hafiza_alani_serbest_anlaticida_reddedilir()
        {
            var hafiza = "'kriterler': [ { 'alan': 'hafiza_var(yardim_etti, oyuncu, 100)', 'op': '==', 'deger': true, 'zorunlu': true } ], 'hafiza': { 'tur': 'yardim_etti', 'fail': 'oyuncu', 'son_n_tick': 100 }";

            Assert.True(Basarili(Dosya(KaynakEsleme, Sablonlu("{ 'id': 'c_hafiza', 'concept': 'anlatim_cevabi', " + hafiza + ", 'metin': '#hafiza.zaman# yardım ettin.' }"))));
            Assert.Contains(
                Hatalar(Dosya(KaynakEsleme, Sablonlu("{ 'id': 'a_hafiza', 'concept': 'anlatma', " + hafiza + ", 'metin': '#hafiza.zaman# yardım ettin.' }"))),
                h => h.Contains("hafiza alanı reddedilir", StringComparison.Ordinal));
        }

        [Fact]
        public void Yalan_soyledi_her_baglamda_reddedilir_cevap_veren_niyette_de()
        {
            var kriter = "[ { 'alan': 'yalan_soyledi(oyuncu)', 'op': '==', 'deger': true, 'zorunlu': true } ]";

            var hatalar = Hatalar(Dosya(KaynakEsleme, Sablonlu("{ 'id': 'c_ozel', 'concept': 'anlatim_cevabi', 'kriterler': " + kriter + ", 'metin': 'x' }")));

            Assert.Contains(hatalar, h => h.Contains("\"yalan_soyledi\" şablon bağlamında reddedilir", StringComparison.Ordinal));
        }

        // ---- dinleyensiz niyet ----

        private const string DinleyensizEsleme =
            "[ { 'olay_turu': 'kacti', 'niyet': 'kacis', 'konusan': 'fail', 'dinleyensiz': true }, { 'olay_turu': 'yardim_etti', 'niyet': 'tesekkur', 'konusan': 'hedef' } ]";

        private const string DinleyensizGenel = "{ 'id': 'k_genel', 'concept': 'kacis', 'genel': true, 'metin': 'Gidiyorum.' }, { 'id': 't_genel', 'concept': 'tesekkur', 'genel': true, 'metin': 'Sağ ol.' }";

        [Fact]
        public void Dinleyensiz_niyette_dinleyen_rolu_yuvasi_ve_followup_hedefi_hatadir()
        {
            Assert.True(Basarili(Dosya(DinleyensizEsleme, DinleyensizGenel)));

            var kriter = DinleyensizGenel + ", { 'id': 'k_kriter', 'concept': 'kacis', 'kriterler': [ { 'alan': 'fikir(dinleyen)', 'op': '>', 'deger': 0, 'zorunlu': true } ], 'metin': 'x' }";
            Assert.Contains(Hatalar(Dosya(DinleyensizEsleme, kriter)), h => h.Contains("dinleyensiz niyette \"dinleyen\" rolü", StringComparison.Ordinal));

            var yuva = DinleyensizGenel + ", { 'id': 'k_yuva', 'concept': 'kacis', 'kriterler': [ { 'alan': 'stres()', 'op': '>', 'deger': 0, 'zorunlu': true } ], 'metin': 'Elveda #dinleyen#.' }";
            Assert.Contains(Hatalar(Dosya(DinleyensizEsleme, yuva)), h => h.Contains("dinleyensiz niyette #dinleyen#", StringComparison.Ordinal));

            var sonra = "{ 'id': 'k_genel', 'concept': 'kacis', 'genel': true, 'metin': 'Gidiyorum.', 'sonra': { 'hedef': 'dinleyen', 'concept': 'tesekkur' } }, { 'id': 't_genel', 'concept': 'tesekkur', 'genel': true, 'metin': 'Sağ ol.' }";
            Assert.Contains(Hatalar(Dosya(DinleyensizEsleme, sonra)), h => h.Contains("dinleyensiz niyette followup hedefi dinleyen olamaz", StringComparison.Ordinal));
        }

        [Fact]
        public void Ayni_niyet_hem_dinleyenli_hem_dinleyensiz_eslenemez()
        {
            var cakisma = "[ { 'olay_turu': 'kacti', 'niyet': 'x', 'konusan': 'fail', 'dinleyensiz': true }, { 'olay_turu': 'yardim_etti', 'niyet': 'x', 'konusan': 'hedef' } ]";

            Assert.Contains(Hatalar(Dosya(cakisma, "{ 'id': 'g', 'concept': 'x', 'genel': true, 'metin': 'x' }")), h => h.Contains("hem dinleyenli hem dinleyensiz", StringComparison.Ordinal));
        }

        // ---- ünsüz yumuşaması: adlar kaydında yumuşak gövde zorunlu ----

        [Fact]
        public void Cins_isim_p_c_t_k_ile_bitiyorsa_yumusak_govde_zorunlu_ozel_adda_degil()
        {
            const string esleme = "[ { 'olay_turu': 'yardim_etti', 'niyet': 'tesekkur', 'konusan': 'hedef' } ]";
            const string genel = "{ 'id': 'g', 'concept': 'tesekkur', 'genel': true, 'metin': 'x' }";

            Assert.Contains(
                Hatalar(Dosya(esleme, genel, "'adlar': { 'borc': { 'ad': 'borç', 'cins': true } },")),
                h => h.Contains("yumuşak gövde zorunlu", StringComparison.Ordinal) && h.Contains("sablon.json:", StringComparison.Ordinal));
            Assert.True(Basarili(Dosya(esleme, genel, "'adlar': { 'borc': { 'ad': 'borç', 'cins': true, 'yumusak': 'borc' }, 'Burak': 'Burak', 'hanci': { 'ad': 'hancı', 'cins': true } },")));
            Assert.Contains(
                Hatalar(Dosya(esleme, genel, "'adlar': { 'borc': { 'ad': 'borç', 'cins': true, 'yumusak': 5 } },")),
                h => h.Contains("\"yumusak\" metin olmalı", StringComparison.Ordinal));
        }

        // ---- kişi zamiri: özne varyantları ----

        private static string OnermeEk(string kayit) =>
            "'onerme_metinleri': { 'kardes': " + kayit + " },";

        [Fact]
        public void Ozne_konusan_ya_da_dinleyen_olabiliyorsa_ben_ve_sen_varyanti_zorunlu_eksikse_hata()
        {
            const string esleme = "[ { 'olay_turu': 'yardim_etti', 'niyet': 'tesekkur', 'konusan': 'hedef' } ]";
            const string genel = "{ 'id': 'g', 'concept': 'tesekkur', 'genel': true, 'metin': 'x' }";

            var eksik = OnermeEk("{ 'metin': '{0} ile {1} kardeş', 'olumsuz': '{0} ile {1} kardeş değil', 'ozne': [0, 1], 'ben': { '0': { 'metin': 'ben ile {1} kardeşiz', 'olumsuz': 'ben ile {1} kardeş değiliz' } } }");
            var hatalar = Hatalar(Dosya(esleme, genel, eksik));
            Assert.Contains(hatalar, h => h.Contains("ozne 0 için sen varyantı yok", StringComparison.Ordinal));
            Assert.Contains(hatalar, h => h.Contains("ozne 1 için ben varyantı yok", StringComparison.Ordinal));

            var tam = OnermeEk(
                "{ 'metin': '{0} ile {1} kardeş', 'olumsuz': '{0} ile {1} kardeş değil', 'ozne': [0, 1], "
                + "'ben': { '0': { 'metin': 'ben ile {1} kardeşiz', 'olumsuz': 'ben ile {1} kardeş değiliz' }, '1': { 'metin': '{0} ile ben kardeşiz', 'olumsuz': '{0} ile ben kardeş değiliz' } }, "
                + "'sen': { '0': { 'metin': 'sen ile {1} kardeşsiniz', 'olumsuz': 'sen ile {1} kardeş değilsiniz' }, '1': { 'metin': '{0} ile sen kardeşsiniz', 'olumsuz': '{0} ile sen kardeş değilsiniz' } } }");
            Assert.True(Basarili(Dosya(esleme, genel, tam)), string.Join("\n", Hatalar(Dosya(esleme, genel, tam))));
        }

        [Fact]
        public void Varyantta_olumsuz_metin_zorunlu_bilinmeyen_pozisyon_ve_alan_hatadir()
        {
            const string esleme = "[ { 'olay_turu': 'yardim_etti', 'niyet': 'tesekkur', 'konusan': 'hedef' } ]";
            const string genel = "{ 'id': 'g', 'concept': 'tesekkur', 'genel': true, 'metin': 'x' }";
            var kotu = OnermeEk("{ 'metin': 'a {0}', 'olumsuz': 'b {0}', 'ozne': [0], 'ben': { '0': { 'metin': 'ben' }, '7': { 'metin': 'x', 'olumsuz': 'y' } }, 'sen': { '0': { 'metin': 's', 'olumsuz': 't', 'x': 1 } } }");

            var hatalar = Hatalar(Dosya(esleme, genel, kotu));

            Assert.Contains(hatalar, h => h.Contains("ben varyantı 0: \"olumsuz\" gerekli", StringComparison.Ordinal));
            Assert.Contains(hatalar, h => h.Contains("ben varyantı 7: ozne listesinde yok", StringComparison.Ordinal));
            Assert.Contains(hatalar, h => h.Contains("bilinmeyen alan \"x\"", StringComparison.Ordinal));
        }

        // ---- yeni ekler ----

        [Fact]
        public void Tamlayan_ve_baslik_degistiricileri_gecerlidir_iyelik_ve_baslik_siralari_denetlenir()
        {
            const string esleme = "[ { 'olay_turu': 'yardim_etti', 'niyet': 'tesekkur', 'konusan': 'hedef' } ]";
            string Tek(string metin) => "{ 'id': 'g', 'concept': 'tesekkur', 'genel': true, 'metin': '" + metin + "' }";

            Assert.True(Basarili(Dosya(esleme, Tek("#dinleyen.tamlayan# borcu. #dinleyen.yonelme.baslik# bak."))));
            Assert.Contains(Hatalar(Dosya(esleme, Tek("#dinleyen.baslik.yonelme#"))), h => h.Contains("baslik en sonda olmalı", StringComparison.Ordinal));
        }
    }
}
