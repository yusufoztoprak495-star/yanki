using System;
using System.Linq;
using Xunit;

namespace Yanki.Dialogue.Tests.Sablon
{
    /// <summary>
    /// Şablon dosyası yükleyici ve doğrulayıcı v0 (8. hafta, C/F/I kararları + Yusuf'un düzeltmeleri). Hatalar <c>dosya:satir: sablon "id": mesaj</c> biçimindedir ve hepsi birlikte döner.
    /// Testlerde JSON tek tırnakla yazılır ve çift tırnağa çevrilir.
    /// </summary>
    public class SablonYukleyiciTests
    {
        private static string TemelLf => Temel.Replace("\r\n", "\n");

        private static string J(string s) => s.Replace('\'', '"');

        // Geçerli çekirdek dosya: anlatma (anlatti'ya eşli) ve tesekkur (yardim_etti'ye eşli) niyetleri.
        private const string Temel = @"{
 'dil': 'tr',
 'adlar': { 'Ayse': 'Ayşe', 'Mehmet': 'Mehmet', 'Hanci': { 'ad': 'hancı', 'cins': true }, 'oyuncu': { 'ad': 'yabancı', 'cins': true } },
 'onerme_metinleri': { 'borclu': { 'metin': '{0}, {1.yonelme} {2} lira borçlu', 'olumsuz': '{0}, {1.yonelme} {2} lira borçlu değil', 'sayilar': [2] } },
 'esleme': { 'anlatti': { 'niyet': 'anlatma', 'konusan': 'fail' }, 'yardim_etti': { 'niyet': 'tesekkur', 'konusan': 'hedef' } },
 'gramer': { 'selam': ['Merhaba', 'Selam'] },
 'sablonlar': [
  { 'id': 'anlat_genel', 'concept': 'anlatma', 'genel': true, 'metin': '#selam#, #dinleyen.yonelme# bir şey söyleyeceğim: #konu#.' },
  { 'id': 'tesekkur_genel', 'concept': 'tesekkur', 'genel': true, 'metin': 'Sağ ol #dinleyen#.' },
  { 'id': 'tesekkur_dost', 'concept': 'tesekkur', 'kriterler': [ { 'alan': 'fikir_bandi(dinleyen)', 'op': '==', 'deger': 'dost', 'zorunlu': true } ], 'metin': 'Sen benim dostumsun #dinleyen#.' }
 ]
}";

        private static SablonYuklemeSonucu Yukle(string json) => SablonYukleyici.Yukle(J(json), "sablon.json");

        private static string[] Hatalar(string json) => Yukle(json).Hatalar.ToArray();

        // Temel dosyada tek bir parçayı değiştirir.
        private static string Degistir(string eski, string yeni)
        {
            Assert.Contains(eski, TemelLf, StringComparison.Ordinal);
            return TemelLf.Replace(eski, yeni);
        }

        [Fact]
        public void Gecerli_dosya_yuklenir()
        {
            var sonuc = Yukle(Temel);

            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            Assert.Equal("tr", sonuc.Dosya!.Dil);
            Assert.Equal(3, sonuc.Dosya.SablonSayisi);
        }

        [Fact]
        public void Sablon_hash_CRLF_ve_LF_icin_ayni_icerik_degisince_farkli()
        {
            var lf = J(Temel).Replace("\r\n", "\n");
            var crlf = lf.Replace("\n", "\r\n");
            var degismis = lf.Replace("Sağ ol", "Teşekkürler");

            var a = SablonYukleyici.Yukle(lf, "a.json").Dosya!.SablonHash;
            var b = SablonYukleyici.Yukle(crlf, "b.json").Dosya!.SablonHash;
            var c = SablonYukleyici.Yukle(degismis, "c.json").Dosya!.SablonHash;

            Assert.Equal(a, b);
            Assert.NotEqual(a, c);
            Assert.Equal(16, a.Length);
        }

        [Fact]
        public void Hatalar_dosya_satir_sablon_id_biciminde_ve_hepsi_birlikte_doner()
        {
            var json = "{\n 'dil': 'tr',\n 'sablonlar': [\n  { 'id': 'a', 'concept': 'x', 'genel': true, 'metin': '#yok#' },\n  { 'id': 'b', 'concept': 'x', 'metin': 'tamam' },\n  { 'id': 'c', 'concept': 'x', 'genel': true, 'metin': 'ok', 'bilinmeyen': 1 }\n ]\n}";

            var hatalar = Hatalar(json);

            Assert.Contains(hatalar, h => h.StartsWith("sablon.json:4: sablon \"a\": ", StringComparison.Ordinal));
            Assert.Contains(hatalar, h => h.StartsWith("sablon.json:5: sablon \"b\": ", StringComparison.Ordinal) && h.Contains("zorunlu kriter", StringComparison.Ordinal));
            Assert.Contains(hatalar, h => h.StartsWith("sablon.json:6: sablon \"c\": ", StringComparison.Ordinal) && h.Contains("bilinmeyen", StringComparison.Ordinal));
            Assert.True(hatalar.Length >= 3);
        }

        [Fact]
        public void Bilinmeyen_ust_duzey_alan_reddedilir_sablon_dosyalari_senaryoya_eklenmez_ama_burada_da_yok()
        {
            var hatalar = Hatalar(Degistir("'dil': 'tr',", "'dil': 'tr', 'sablon_dosyalari': ['x.json'],"));

            Assert.Contains(hatalar, h => h.Contains("bilinmeyen alan \"sablon_dosyalari\"", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("olay_rolu(hedef)", "==", "true")]
        [InlineData("olay_konusu(borclu(*,*,*))", "==", "true")]
        [InlineData("konu_puani(dinleyen)", ">", "0")]
        [InlineData("gercek(konum!(Ali,han))", "==", "true")]
        [InlineData("yalan_soyledi(dinleyen)", "==", "true")]
        public void Reddedilen_sorgular_sablon_baglaminda_hata_verir(string alan, string op, string deger)
        {
            var hatalar = Hatalar(Degistir("'alan': 'fikir_bandi(dinleyen)', 'op': '==', 'deger': 'dost'", "'alan': '" + alan + "', 'op': '" + op + "', 'deger': " + deger));

            Assert.Contains(hatalar, h => h.Contains("reddedilir", StringComparison.Ordinal));
            Assert.Contains(hatalar, h => h.Contains("sablon \"tesekkur_dost\"", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("fikir(olay.fail)")]
        [InlineData("fikir(olay.hedef)")]
        [InlineData("fikir(eylem.hedef)")]
        public void Olay_ve_eylem_rolleri_sablon_baglaminda_yoktur(string alan)
        {
            var hatalar = Hatalar(Degistir("'alan': 'fikir_bandi(dinleyen)', 'op': '==', 'deger': 'dost'", "'alan': '" + alan + "', 'op': '>', 'deger': 0"));

            Assert.Contains(hatalar, h => h.Contains("rolü şablon bağlamında yok", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("inanc(borclu(*,*,*))", ">=", "0.5")]
        [InlineData("kaynak(borclu(*,*,*), dinleyen)", "==", "true")]
        [InlineData("hafiza_var(yardim_etti, oyuncu, 168)", "==", "true")]
        public void Anlatti_niyetine_eslenen_sablonda_inanc_kaynak_ve_hafiza_var_reddedilir(string alan, string op, string deger)
        {
            var anlatEk = ", { 'id': 'anlat_ozel', 'concept': 'anlatma', 'kriterler': [ { 'alan': '" + alan + "', 'op': '" + op + "', 'deger': " + deger + ", 'zorunlu': true } ], 'metin': 'x' } ]";
            var json = Degistir("'dost'", "'dost'").Replace("\n ]\n}", anlatEk + "\n}");

            var hatalar = Hatalar(json);

            Assert.Contains(hatalar, h => h.Contains("anlatti niyetine eslenen".Replace("eslenen", "eşlenen"), StringComparison.Ordinal) && h.Contains("yalan gizliliği", StringComparison.Ordinal));
        }

        [Fact]
        public void Ayni_sorgular_anlatti_disindaki_niyette_serbesttir_stres_de_serbesttir()
        {
            var ek = ", { 'id': 'tesekkur_stresli', 'concept': 'tesekkur', 'kriterler': [ { 'alan': 'stres()', 'op': '>=', 'deger': 30, 'zorunlu': true }, { 'alan': 'inanc(borclu(*,*,*))', 'op': '>=', 'deger': 0.5, 'agirlik': 2 } ], 'metin': 'Gergin ama sağ ol.' } ]";
            var json = Temel.Replace("\n ]\n}", ek + "\n}");

            var sonuc = Yukle(json);

            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
        }

        [Fact]
        public void Anlatti_niyetinde_hafiza_alani_reddedilir()
        {
            var ek = ", { 'id': 'anlat_hafiza', 'concept': 'anlatma', 'kriterler': [ { 'alan': 'fikir(dinleyen)', 'op': '>', 'deger': 0, 'zorunlu': true } ], 'hafiza': { 'tur': 'yardim_etti', 'fail': 'oyuncu', 'son_n_tick': 168 }, 'metin': 'x' } ]";

            var hatalar = Hatalar(Temel.Replace("\n ]\n}", ek + "\n}"));

            Assert.Contains(hatalar, h => h.Contains("hafiza alanı reddedilir", StringComparison.Ordinal));
        }

        [Fact]
        public void Genel_olmayan_sablonda_zorunlu_kriter_yoksa_hata_agirlikli_kriter_puan_katar()
        {
            var zorunluYok = Degistir("'zorunlu': true }", "'agirlik': 2 }");
            Assert.Contains(Hatalar(zorunluYok), h => h.Contains("en az bir zorunlu kriter", StringComparison.Ordinal));

            var kismiEslesme = Degistir(
                "'kriterler': [ { 'alan': 'fikir_bandi(dinleyen)', 'op': '==', 'deger': 'dost', 'zorunlu': true } ]",
                "'kriterler': [ { 'alan': 'fikir_bandi(dinleyen)', 'op': '==', 'deger': 'dost', 'zorunlu': true }, { 'alan': 'stres()', 'op': '>=', 'deger': 10, 'agirlik': 3 } ]");
            Assert.True(Yukle(kismiEslesme).Basarili, "zorunlu dışındaki kriterler puan katmaya devam eder (ağırlıklı kısmi eşleşme)");
        }

        [Fact]
        public void Genel_sablon_zorunlu_kriter_tekrar_sinirı_hafiza_ve_konu_deseni_tasiyamaz()
        {
            var kotu = Degistir(
                "{ 'id': 'tesekkur_genel', 'concept': 'tesekkur', 'genel': true,",
                "{ 'id': 'tesekkur_genel', 'concept': 'tesekkur', 'genel': true, 'kriterler': [ { 'alan': 'stres()', 'op': '>', 'deger': 0, 'zorunlu': true } ], 'tekrar': 'bir_kez', 'konu_deseni': 'borclu(*,*,*)',");

            var hatalar = Hatalar(kotu);

            Assert.Contains(hatalar, h => h.Contains("genel şablon zorunlu kriter taşıyamaz", StringComparison.Ordinal));
            Assert.Contains(hatalar, h => h.Contains("genel şablon tekrar sınırı taşıyamaz", StringComparison.Ordinal));
            Assert.Contains(hatalar, h => h.Contains("genel şablon konu_deseni taşıyamaz", StringComparison.Ordinal));
        }

        [Fact]
        public void Her_concept_ve_her_esleme_icin_genel_sablon_zorunlu()
        {
            var json = Degistir("{ 'id': 'tesekkur_genel', 'concept': 'tesekkur', 'genel': true, 'metin': 'Sağ ol #dinleyen#.' },", string.Empty);

            var hatalar = Hatalar(json);

            Assert.Contains(hatalar, h => h.Contains("niyet \"tesekkur\" için genel", StringComparison.Ordinal));
            Assert.Contains(hatalar, h => h.Contains("concept \"tesekkur\" için genel şablon yok", StringComparison.Ordinal));
        }

        private const string HafizaEk =
            ", { 'id': 'tesekkur_gecmis', 'concept': 'tesekkur', 'kriterler': [ { 'alan': 'hafiza_var(yardim_etti, oyuncu, 168)', 'op': '==', 'deger': true, 'zorunlu': true } ], "
            + "'hafiza': { 'tur': 'yardim_etti', 'fail': 'oyuncu', 'son_n_tick': 168 }, 'metin': '#hafiza.zaman# bana yardım ettin.' } ]";

        [Fact]
        public void Hafiza_bildirimi_zorunlu_hafiza_var_ile_ayni_tur_fail_ve_aralikta_olmali()
        {
            Assert.True(Yukle(Temel.Replace("\n ]\n}", HafizaEk + "\n}")).Basarili);

            foreach (var (eski, yeni) in new[] { ("'son_n_tick': 168", "'son_n_tick': 100"), ("'fail': 'oyuncu', 'son_n", "'fail': 'dinleyen', 'son_n"), ("'tur': 'yardim_etti', 'fail'", "'tur': 'tehdit_etti', 'fail'"), ("'zorunlu': true } ], 'hafiza'", "'agirlik': 2 } ], 'hafiza'") })
            {
                var hatalar = Hatalar(Temel.Replace("\n ]\n}", HafizaEk.Replace(eski, yeni) + "\n}"));
                Assert.Contains(hatalar, h => h.Contains("uyuşmuyor", StringComparison.Ordinal) || h.Contains("zorunlu kriter", StringComparison.Ordinal));
            }
        }

        [Fact]
        public void Hafiza_yuvasi_hafiza_bildirimi_olmadan_kullanilamaz()
        {
            var json = Degistir("'metin': 'Sen benim dostumsun #dinleyen#.'", "'metin': '#hafiza.zaman# dostumsun.'");

            Assert.Contains(Hatalar(json), h => h.Contains("hafiza bildirimi yok", StringComparison.Ordinal));
        }

        [Fact]
        public void Followup_hedefi_olan_concept_sonra_alani_tasiyamaz_ve_genel_sablonu_olmali()
        {
            var ek = ", { 'id': 'tesekkur_sonra', 'concept': 'tesekkur', 'kriterler': [ { 'alan': 'stres()', 'op': '>', 'deger': 5, 'zorunlu': true } ], 'sonra': { 'hedef': 'dinleyen', 'concept': 'cevap' }, 'metin': 'x' } ]";

            var hatalar = Hatalar(Temel.Replace("\n ]\n}", ek + "\n}"));
            Assert.Contains(hatalar, h => h.Contains("followup concept'i \"cevap\" için genel şablon yok", StringComparison.Ordinal));

            var cevapluEk = ek.TrimEnd(']', ' ') + ", { 'id': 'cevap_genel', 'concept': 'cevap', 'genel': true, 'sonra': { 'hedef': 'dinleyen', 'concept': 'tesekkur' }, 'metin': 'Rica ederim.' } ]";
            var zincir = Hatalar(Temel.Replace("\n ]\n}", cevapluEk + "\n}"));
            Assert.Contains(zincir, h => h.Contains("followup zinciri yok", StringComparison.Ordinal));
        }

        [Fact]
        public void Gramer_dongusu_ve_dort_seviye_yukleme_hatasidir_hata_gramer_satirini_gosterir()
        {
            var json = Degistir("'gramer': { 'selam': ['Merhaba', 'Selam'] },", "'gramer': { 'a': ['#b#'], 'b': ['#a#'], 'p': ['#q#'], 'q': ['#r#'], 'r': ['#s#'], 's': ['son'] },");

            var hatalar = Hatalar(json);

            Assert.Contains(hatalar, h => h.Contains("döngü: a → b → a", StringComparison.Ordinal) && h.StartsWith("sablon.json:", StringComparison.Ordinal));
            Assert.Contains(hatalar, h => h.Contains("4 seviye iç içe", StringComparison.Ordinal));
        }

        [Fact]
        public void Onerme_metni_olumsuz_metin_zorunlu_ve_sayiya_ek_hatadir()
        {
            var olumsuzYok = Degistir(", 'olumsuz': '{0}, {1.yonelme} {2} lira borçlu değil'", string.Empty);
            Assert.Contains(Hatalar(olumsuzYok), h => h.Contains("degil(...) için ayrı metin zorunludur", StringComparison.Ordinal));

            var sayiyaEk = Degistir("{2} lira borçlu', 'olumsuz'", "{2.yonelme} lira borçlu', 'olumsuz'");
            Assert.Contains(Hatalar(sayiyaEk), h => h.Contains("sayıya ek yok", StringComparison.Ordinal));
        }

        [Fact]
        public void Tanimsiz_yuklem_konu_deseni_ve_konu_yuvasi_hatadir()
        {
            var ek = ", { 'id': 'anlat_kardes', 'concept': 'anlatma', 'konu_deseni': 'kardes(*,*)', 'kriterler': [ { 'alan': 'fikir(dinleyen)', 'op': '>', 'deger': 0, 'zorunlu': true } ], 'metin': 'Bak: #konu#' } ]";

            var hatalar = Hatalar(Temel.Replace("\n ]\n}", ek + "\n}"));

            Assert.Contains(hatalar, h => h.Contains("tanımsız yüklem \"kardes\"", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("aktarim_kaydedildi")]
        [InlineData("karar_verildi")]
        [InlineData("kanit_eklendi")]
        [InlineData("fren_uygulandi")]
        [InlineData("gercek_eklendi")]
        public void Ic_olay_turleri_eslenemez_aktarim_kaydi_yalan_bilgisi_tasir(string tur)
        {
            var hatalar = Hatalar(Degistir("'yardim_etti': {", "'" + tur + "': { 'niyet': 'tesekkur', 'konusan': 'fail' }, 'yardim_etti': {"));

            Assert.Contains(hatalar, h => h.Contains("iç olay türü eşlenemez", StringComparison.Ordinal));
        }

        [Fact]
        public void Esleme_eylem_kimligi_ya_da_bilinmeyen_alan_kabul_etmez()
        {
            var hatalar = Hatalar(Degistir("'konusan': 'hedef' } }", "'konusan': 'hedef', 'eylem': 'yardim' } }"));

            Assert.Contains(hatalar, h => h.Contains("eylem kimliği kullanılmaz", StringComparison.Ordinal));
        }

        [Fact]
        public void Adlar_ince_alani_yazar_isaretidir_bool_olmali()
        {
            Assert.True(Yukle(Degistir("'Ayse': 'Ayşe'", "'Rasul': { 'ad': 'Rasul', 'ince': true }")).Basarili);
            Assert.Contains(Hatalar(Degistir("'Ayse': 'Ayşe'", "'Rasul': { 'ad': 'Rasul', 'ince': 'evet' }")), h => h.Contains("\"ince\" true ya da false", StringComparison.Ordinal));
        }

        [Fact]
        public void Adlar_kimligi_ASCII_olmali_gorunen_ad_Turkce_olabilir()
        {
            var hatalar = Hatalar(Degistir("'Ayse': 'Ayşe'", "'Ayşe': 'Ayşe'"));

            Assert.Contains(hatalar, h => h.Contains("kimlik ASCII olmalı", StringComparison.Ordinal));
        }

        [Fact]
        public void Ek_degistiricileri_yalnizca_tr_dosyalarinda_ve_yalnizca_ad_olgularinda_gecerli()
        {
            var en = Degistir("'dil': 'tr'", "'dil': 'en'");
            Assert.Contains(Hatalar(en), h => h.Contains("yalnızca Türkçe", StringComparison.Ordinal));

            var metinOlgusunaEk = Degistir("Sağ ol #dinleyen#.", "Sağ ol #konu.yonelme#.");
            Assert.Contains(Hatalar(metinOlgusunaEk), h => h.Contains("düz metindir, ek getirilmez", StringComparison.Ordinal));

            var bilinmeyenEk = Degistir("Sağ ol #dinleyen#.", "Sağ ol #dinleyen.tamlayan#.");
            Assert.Contains(Hatalar(bilinmeyenEk), h => h.Contains("bilinmeyen ya da birden çok ek", StringComparison.Ordinal));
        }

        [Fact]
        public void JSON_hatasi_satir_bilgisiyle_doner()
        {
            var sonuc = SablonYukleyici.Yukle("{\n 'dil': \n}".Replace('\'', '"'), "bozuk.json");

            Assert.False(sonuc.Basarili);
            Assert.StartsWith("bozuk.json:", sonuc.Hatalar[0], StringComparison.Ordinal);
        }
    }
}
