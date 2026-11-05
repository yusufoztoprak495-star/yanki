using System;
using System.IO;
using System.Linq;
using Xunit;
using Yanki.Cli;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// CLI: <c>yanki diyalog &lt;oturum.json&gt; --sablon &lt;dosya&gt; [--neden &lt;satir_no&gt;]</c> (8. hafta, I kararı). Senaryo <c>samples/HanGerilim</c> (geçici klasöre kopyalanır); şablon dosyası
    /// bir test fikstürüdür (Kasaba Hanı ya da HanGerilim şablon içeriği DEĞİL; içerik Yusuf'un alanıdır, DURAK 2). Normal satır çıktısı KuralId vermez, --neden çıktısı why ile aynı dili konuşur.
    /// </summary>
    public sealed class DiyalogCliTests : IDisposable
    {
        private const string Anlat = "anlatti:Ayse:onerme=borclu(Mehmet,Ayse,500)";

        private const string Sablon = @"{ 'dil': 'tr',
 'adlar': { 'Ayse': 'Ayşe', 'Can': 'Can', 'Hatice': 'Hatice', 'Kemal': 'Kemal', 'Selim': 'Selim', 'Mehmet': 'Mehmet', 'oyuncu': { 'ad': 'yabancı', 'cins': true } },
 'onerme_metinleri': {
  'borclu': { 'metin': '{0}, {1.yonelme} {2} lira borçlu', 'olumsuz': '{0}, {1.yonelme} {2} lira borçlu değil', 'sayilar': [2] },
  'kardes': { 'metin': '{0} ile {1} kardeş', 'olumsuz': '{0} ile {1} kardeş değil' },
  'sir_sakliyor': { 'metin': '{0}, {1.ayrilma} bir şey saklıyor', 'olumsuz': '{0}, {1.ayrilma} bir şey saklamıyor' }
 },
 'esleme': { 'anlatti': { 'niyet': 'anlatma', 'konusan': 'fail' } },
 'sablonlar': [ { 'id': 'anlat_genel', 'concept': 'anlatma', 'genel': true, 'metin': '#dinleyen.yonelme# bir şey söyleyeceğim: #konu#.' } ] }";

        private readonly string _dizin;
        private readonly string _senaryo;

        public DiyalogCliTests()
        {
            _dizin = Path.Combine(Path.GetTempPath(), "yanki-diyalog-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dizin);
            var hedef = Path.Combine(_dizin, "senaryo");
            Directory.CreateDirectory(hedef);
            foreach (var f in Directory.GetFiles(Path.Combine(RepoKoku(), "samples", "HanGerilim"), "*.json"))
            {
                File.Copy(f, Path.Combine(hedef, Path.GetFileName(f)));
            }

            _senaryo = Path.Combine(hedef, "scenario.json");
        }

        public void Dispose() => Directory.Delete(_dizin, true);

        private string Yol(string ad) => Path.Combine(_dizin, ad);

        private static string RepoKoku()
        {
            var dizin = new DirectoryInfo(AppContext.BaseDirectory);
            while (dizin != null && !File.Exists(Path.Combine(dizin.FullName, "Yanki.slnx")))
            {
                dizin = dizin.Parent;
            }

            return dizin?.FullName ?? throw new InvalidOperationException("Repo kökü bulunamadı.");
        }

        private static (int Kod, string Cikti, string Hata) Calistir(params string[] args)
        {
            var cikti = new StringWriter();
            var hata = new StringWriter();
            var kod = KomutCalistirici.Calistir(args, cikti, hata);
            return (kod, cikti.ToString(), hata.ToString());
        }

        private string SablonYaz(string icerik = Sablon, string ad = "sablon.json")
        {
            var yol = Yol(ad);
            File.WriteAllText(yol, icerik.Replace('\'', '"'));
            return yol;
        }

        private string Oturum()
        {
            var yol = Yol("oturum.json");
            var (kod, _, hata) = Calistir("run", _senaryo, "--seed", "42", "--eylem", Anlat, "--tick", "24", "--kaydet", yol);
            Assert.True(kod == 0, hata);
            return yol;
        }

        [Fact]
        public void Diyalog_satirlari_yazar_hash_basliklariyla_ve_KuralId_vermez()
        {
            var (kod, cikti, hata) = Calistir("diyalog", Oturum(), "--sablon", SablonYaz());

            Assert.True(kod == 0, hata);
            Assert.Contains("sablon_hash: ", cikti, StringComparison.Ordinal);
            Assert.Contains("diyalog_hash: ", cikti, StringComparison.Ordinal);
            Assert.Matches(@"satir: \d+", cikti);
            Assert.Contains("#1 [t", cikti, StringComparison.Ordinal);
            Assert.DoesNotContain("eylem:", cikti, StringComparison.Ordinal);
            Assert.DoesNotContain("KuralId", cikti, StringComparison.Ordinal);
            Assert.DoesNotContain("kriz:", cikti, StringComparison.Ordinal);
        }

        [Fact]
        public void Yumusak_l_ile_biten_ad_ince_ek_alir_Kemale_Kemala_degil()
        {
            var (kod, cikti, hata) = Calistir("diyalog", Oturum(), "--sablon", SablonYaz());

            Assert.True(kod == 0, hata);
            Assert.Contains("Kemal'e", cikti, StringComparison.Ordinal);
            Assert.DoesNotContain("Kemal'a", cikti, StringComparison.Ordinal);
        }

        [Fact]
        public void Diyalog_ayni_oturum_ve_sablonla_birebir_ayni_ciktiyi_verir()
        {
            var oturum = Oturum();
            var sablon = SablonYaz();

            Assert.Equal(Calistir("diyalog", oturum, "--sablon", sablon).Cikti, Calistir("diyalog", oturum, "--sablon", sablon).Cikti);
        }

        [Fact]
        public void Neden_secilen_sablonu_kriterleri_ve_olayin_why_zincirini_gosterir()
        {
            var oturum = Oturum();
            var (kod, cikti, hata) = Calistir("diyalog", oturum, "--sablon", SablonYaz(), "--neden", "1");

            Assert.True(kod == 0, hata);
            Assert.Contains("-- neden (diyalog) --", cikti, StringComparison.Ordinal);
            Assert.Contains("seçilen şablon: anlat_genel", cikti, StringComparison.Ordinal);
            Assert.Contains("şablon anlat_genel: puan 0 SEÇİLDİ", cikti, StringComparison.Ordinal);
            Assert.Contains("-- olayın why zinciri (yanki why ", cikti, StringComparison.Ordinal);

            // why ile AYNI dil: aynı olay için `yanki why` çıktısı, --neden çıktısının sonunda aynen bulunur.
            var olayId = System.Text.RegularExpressions.Regex.Match(cikti, @"yanki why \S+ (\d+)\)").Groups[1].Value;
            var why = Calistir("why", oturum, olayId);
            Assert.Equal(0, why.Kod);
            Assert.EndsWith(why.Cikti, cikti, StringComparison.Ordinal);
        }

        [Fact]
        public void Neden_olmayan_satir_numarasi_ve_eksik_sablon_hata_kodu_verir()
        {
            var oturum = Oturum();

            Assert.Equal(2, Calistir("diyalog", oturum, "--sablon", SablonYaz(), "--neden", "9999").Kod);
            Assert.Equal(1, Calistir("diyalog", oturum).Kod);
            Assert.Equal(1, Calistir("diyalog", oturum, "--sablon", SablonYaz(), "--neden", "0").Kod);
            Assert.Equal(2, Calistir("diyalog", oturum, "--sablon", Yol("yok.json")).Kod);
        }

        [Fact]
        public void Bozuk_sablon_dosya_satir_hatalarini_birlikte_yazar_ve_2_doner()
        {
            var bozuk = SablonYaz("{\n 'dil': 'tr',\n 'sablonlar': [\n  { 'id': 'a', 'concept': 'x', 'genel': true, 'metin': '#yok#' },\n  { 'id': 'b', 'concept': 'x', 'metin': 'tamam' }\n ] }", "bozuk.json");

            var (kod, _, hata) = Calistir("diyalog", Oturum(), "--sablon", bozuk);

            Assert.Equal(2, kod);
            var satirlar = hata.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(s => s.TrimEnd('\r')).ToArray();
            Assert.Contains(satirlar, s => s.Contains("bozuk.json:4: sablon \"a\": ", StringComparison.Ordinal));
            Assert.Contains(satirlar, s => s.Contains("bozuk.json:5: sablon \"b\": ", StringComparison.Ordinal));
        }

        // 8. hafta kontrol noktası: diyalog açıkken dört golden'ın son_hash ve gunluk_hash değerleri birebir tutar (replay + diyalog aboneliği; çıkış kodları 3 ve 5 yok).
        [Theory]
        [InlineData("mini-han-1")]
        [InlineData("mini-karar-1")]
        [InlineData("han-gerilim-1")]
        [InlineData("frenli-han-1")]
        public void Dort_golden_diyalog_acikken_son_hash_ve_gunluk_hash_ayni(string golden)
        {
            var yol = Path.Combine(RepoKoku(), "tests", "golden", golden + ".json");
            var (kod, cikti, hata) = Calistir("diyalog", yol, "--sablon", SablonYaz());

            Assert.True(kod == 0, $"{golden}: çıkış kodu {kod}\n{hata}");
            Assert.Contains("diyalog_hash: ", cikti, StringComparison.Ordinal);
            Assert.Equal(0, Calistir("replay", yol).Kod);   // aynı dosya diyalog olmadan da tutuyor
        }
    }
}
