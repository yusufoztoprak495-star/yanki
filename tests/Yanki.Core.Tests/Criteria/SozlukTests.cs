using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Yanki.Core.Criteria;

namespace Yanki.Core.Tests.Criteria
{
    public class SozlukTests
    {
        [Theory]
        [InlineData("kisilik", 1, DonusTuru.Sayi, -1)]
        [InlineData("eksen", 2, DonusTuru.Sayi, -1)]
        [InlineData("fikir", 1, DonusTuru.Sayi, -1)]
        [InlineData("fikir_bandi", 1, DonusTuru.Bant, -1)]
        [InlineData("modifier_var", 2, DonusTuru.Mantiksal, -1)]
        [InlineData("duygu", 1, DonusTuru.Sayi, -1)]
        [InlineData("duygu_hedefi", 0, DonusTuru.Varlik, -1)]
        [InlineData("stres", 0, DonusTuru.Sayi, -1)]
        [InlineData("inanc", 1, DonusTuru.Sayi, 0)]
        [InlineData("kaynak", 2, DonusTuru.Mantiksal, 0)]
        [InlineData("hafiza_var", 3, DonusTuru.Mantiksal, -1)]
        [InlineData("yalan_soyledi", 1, DonusTuru.Mantiksal, -1)]
        [InlineData("ayni_mekanda", 1, DonusTuru.Mantiksal, -1)]
        [InlineData("gercek", 1, DonusTuru.Mantiksal, 0)]
        [InlineData("yay", 1, DonusTuru.Sayi, -1)]
        public void Fonksiyon_tanimi_ad_arguman_sayisi_ve_donus_turu(string ad, int arguman, DonusTuru donus, int onermeIndeksi)
        {
            var t = SorguSozlugu.Bul(ad);

            Assert.NotNull(t);
            Assert.Equal(ad, t!.Ad);
            Assert.Equal(arguman, t.ArgumanSayisi);
            Assert.Equal(donus, t.Donus);
            Assert.Equal(onermeIndeksi, t.OnermeIndeksi);
        }

        [Fact]
        public void Sozluk_tam_15_fonksiyon_ve_ordinal_siralidir()
        {
            var adlar = SorguSozlugu.Hepsi.Select(t => t.Ad).ToArray();

            Assert.Equal(15, adlar.Length);
            Assert.Equal(adlar.OrderBy(a => a, StringComparer.Ordinal).ToArray(), adlar);
            Assert.Null(SorguSozlugu.Bul("yok_boyle_bir_sey"));
        }

        [Fact]
        public void Kod_sozlugu_docs_criteria_language_tablosuyla_birebir_uyumlu()
        {
            var satirlar = File.ReadAllLines(RepoRoot.Combine("docs", "criteria-language.md"))
                .Where(s => s.StartsWith("| ", StringComparison.Ordinal) && char.IsDigit(s[2]))
                .ToList();
            Assert.Equal(15, satirlar.Count);

            var gorulen = new List<string>();
            foreach (var satir in satirlar)
            {
                // Kaçışlı '|' (eksen(hedef, guven\|sevgi\|korku)) hücre ayracı sayılmasın.
                var hucreler = satir.Replace("\\|", "\u0001").Split('|')
                    .Select(h => h.Replace("\u0001", "|").Trim()).ToArray();
                // hucreler: ["", "#", "`imza`", "ne sorar", "döndürdüğü", "`örnek`", ""]
                var imza = hucreler[2].Trim('`');
                var ad = imza.Substring(0, imza.IndexOf('('));
                var parametreler = imza.Substring(imza.IndexOf('(') + 1, imza.Length - imza.IndexOf('(') - 2).Trim();
                var arguman = parametreler.Length == 0 ? 0 : parametreler.Split(',').Length;
                var donusMetni = hucreler[4];
                var donus = donusMetni.StartsWith("sayı", StringComparison.Ordinal) ? DonusTuru.Sayi
                    : donusMetni.StartsWith("doğru/yanlış", StringComparison.Ordinal) ? DonusTuru.Mantiksal
                    : donusMetni.StartsWith("varlık", StringComparison.Ordinal) ? DonusTuru.Varlik
                    : donusMetni.Contains("dusman") ? DonusTuru.Bant
                    : throw new InvalidOperationException("Tanınmayan dönüş türü: " + donusMetni);

                var tanim = SorguSozlugu.Bul(ad);
                Assert.True(tanim != null, $"docs'ta olan '{ad}' kodda yok.");
                Assert.Equal(arguman, tanim!.ArgumanSayisi);
                Assert.Equal(donus, tanim.Donus);
                gorulen.Add(ad);

                // Örnek kriterin alan kısmı (ilk çağrı) ayrıştırılıp doğrulanmalı ve bu fonksiyonu çağırmalı.
                var ornek = hucreler[5].Trim('`');
                var alan = AlanKismi(ornek);
                var cagri = IfadeAyristirici.Ayristir(alan);
                SorguSozlugu.Dogrula(cagri);
                Assert.Equal(ad, cagri.Ad);
            }

            Assert.Equal(
                SorguSozlugu.Hepsi.Select(t => t.Ad).OrderBy(a => a, StringComparer.Ordinal).ToArray(),
                gorulen.OrderBy(a => a, StringComparer.Ordinal).ToArray());
        }

        // "kisilik(acgozluluk) > 0.7" → "kisilik(acgozluluk)": ilk '(' ile eşleşen ')' sonuna kadar.
        private static string AlanKismi(string ornek)
        {
            var derinlik = 0;
            for (var i = 0; i < ornek.Length; i++)
            {
                if (ornek[i] == '(')
                {
                    derinlik++;
                }
                else if (ornek[i] == ')' && --derinlik == 0)
                {
                    return ornek.Substring(0, i + 1);
                }
            }

            throw new InvalidOperationException("Dengesiz örnek: " + ornek);
        }

        [Theory]
        [InlineData("foo(a)", 0, "foo")]
        [InlineData("fikir()", 0, "fikir")]
        [InlineData("fikir(a, b)", 0, "fikir")]
        [InlineData("stres(a)", 0, "stres")]
        [InlineData("eksen(a)", 0, "eksen")]
        [InlineData("gercek(Ali)", 7, "önerme")]
        [InlineData("gercek(oyuncu)", 7, "önerme")]
        [InlineData("fikir(borclu(a))", 6, "önerme")]
        [InlineData("fikir(*)", 6, "joker")]
        [InlineData("gercek(konum!(Ali, borclu(a)))", 19, "iç içe")]
        [InlineData("gercek(konum!())", 7, "argüman")]
        [InlineData("fikir!(a)", 0, "'!'")]
        public void Dogrulama_hatalari_konum_ve_aciklama_icerir(string ifade, int konum, string parca)
        {
            var cagri = IfadeAyristirici.Ayristir(ifade);

            var hata = Assert.Throws<KriterHatasi>(() => SorguSozlugu.Dogrula(cagri));

            Assert.Equal(konum, hata.Konum);
            Assert.Contains(parca, hata.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("gercek(konum!(ben, han))")]
        [InlineData("gercek(borclu(Mehmet, *, *))")]
        [InlineData("inanc(borclu(olay.hedef, oyuncu, 500))")]
        [InlineData("kaynak(borclu(Mehmet, *, *), Ayse)")]
        [InlineData("hafiza_var(yardim_etti, oyuncu, 168)")]
        [InlineData("eksen(oyuncu, guven)")]
        [InlineData("yalan_soyledi(olay.hedef)")]
        public void Gecerli_ifadeler_dogrulanir(string ifade)
        {
            SorguSozlugu.Dogrula(IfadeAyristirici.Ayristir(ifade));
        }
    }
}
