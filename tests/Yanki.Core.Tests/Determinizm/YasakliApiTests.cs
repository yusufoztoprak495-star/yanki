using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Yanki.Core.Tests.Determinizm
{
    /// <summary>
    /// <c>src/Yanki.Core</c> altındaki kaynak dosyalarda determinizmi bozan API'lerin geçmediğini denetler
    /// (CLAUDE.md değişmez karar 3, ADR-007). Kararsız API'ler: süreçten sürece değişen hash'ler, sistem saati,
    /// sistem rastgeleliği, yüzer nokta ve sırasız koleksiyonlar (yerine SortedDictionary/SortedSet kullanılır).
    /// </summary>
    public class YasakliApiTests
    {
        internal sealed class Kural
        {
            public Kural(string ad, string kalip, string neden)
            {
                Ad = ad;
                Kalip = new Regex(kalip, RegexOptions.CultureInvariant);
                Neden = neden;
            }

            public string Ad { get; }

            public Regex Kalip { get; }

            public string Neden { get; }
        }

        internal static readonly Kural[] Kurallar =
        {
            new Kural("System.Random", @"System\.Random\b", "tek RNG kaynağı AnahtarliRng"),
            new Kural("new Random(", @"\bnew\s+Random\s*\(", "tek RNG kaynağı AnahtarliRng"),
            new Kural("DateTime.Now", @"\bDateTime\.Now\b", "simülasyon zamanı tick'tir"),
            new Kural("DateTime.UtcNow", @"\bDateTime\.UtcNow\b", "simülasyon zamanı tick'tir"),
            new Kural("DateTimeOffset.Now/UtcNow", @"\bDateTimeOffset\.(Utc)?Now\b", "simülasyon zamanı tick'tir"),
            new Kural("Guid.NewGuid", @"\bGuid\.NewGuid\b", "kimlikler sıralı ya da anahtarlı üretilir"),
            new Kural("GetHashCode(", @"\bGetHashCode\s*\(", "süreçten sürece değişir; Hash64 kullan"),
            new Kural("HashCode.", @"\bHashCode\.", "süreçten sürece değişir; Hash64 kullan"),
            new Kural("double", @"\bdouble\b", "yüzer nokta yasak; decimal kullan (ADR-007)"),
            new Kural("float", @"\bfloat\b", "yüzer nokta yasak; decimal kullan (ADR-007)"),
            new Kural("Dictionary<", @"\bDictionary\s*<", "sırasız gezinti yasak; SortedDictionary(StringComparer.Ordinal)"),
            new Kural("HashSet<", @"\bHashSet\s*<", "sırasız gezinti yasak; SortedSet(StringComparer.Ordinal)"),
            new Kural(
                "Math.Pow/Exp/Log/Sqrt/Trig", @"\bMath\.(Pow|Exp|Log|Log10|Log2|Sqrt|Cbrt|Sin|Cos|Tan|Asin|Acos|Atan|Atan2|Sinh|Cosh|Tanh)\b",
                "kayan noktalı matematik yasak; elle yazılmış decimal serisi kullan (DecimalMat, ADR-007)"),
        };

        // Bilinçli istisnalar: (src/Yanki.Core'a göre göreli yol, kural adı, GEREKÇE). Boş tutmak hedeftir.
        // Örnek: ("Polyfills/Ornek.cs", "HashSet<", "netstandard2.1 polyfill'i; hiçbir yerde gezilmez.")
        internal static readonly (string Dosya, string KuralAdi, string Gerekce)[] Izinler =
        {
        };

        // 8. hafta: Yanki.Dialogue için EK kurallar (Core'a uygulanmaz). Metin işleyen kod kültüre bağlı çağrılardan kaçınmalı: Türkçe I/ı ve İ/i farkı tr-TR makinede
        // sonucu değiştirir (ToLower, ToUpper, kültürlü karşılaştırma ve biçimlendirme). Karşılaştırma ordinal, biçimlendirme InvariantCulture, harf dönüşümü elle.
        internal static readonly Kural[] KulturKurallari =
        {
            new Kural("ToLower/ToUpper", @"\.(ToLower|ToUpper)(Invariant)?\s*\(", "harf dönüşümü kültüre bağlı (Türkçe I/ı); elle tablo kullan"),
            new Kural("string.Format", @"\b[Ss]tring\.Format\s*\(", "kültürlü biçimlendirme; InvariantCulture ile birleştir"),
            new Kural("CurrentCulture", @"\bCurrent(UI)?Culture\b", "ortam kültürüne bağlı; yasak"),
            new Kural("string.Compare", @"\b[Ss]tring\.Compare\s*\(", "kültürlü karşılaştırma; string.CompareOrdinal"),
            new Kural("StartsWith/EndsWith/IndexOf(string)", @"\.(StartsWith|EndsWith|IndexOf)\s*\(\s*""[^""]*""\s*\)", "kültürlü karşılaştırma; StringComparison.Ordinal ver"),
        };

        internal static IReadOnlyList<(int Satir, string KuralAdi)> Tara(string kaynak, IEnumerable<Kural>? kurallar = null)
        {
            var temiz = YorumlariTemizle(kaynak);
            var bulunan = new List<(int, string)>();
            var satirlar = temiz.Split('\n');
            var tum = kurallar ?? Kurallar;
            for (var i = 0; i < satirlar.Length; i++)
            {
                foreach (var kural in tum)
                {
                    if (kural.Kalip.IsMatch(satirlar[i]))
                    {
                        bulunan.Add((i + 1, kural.Ad));
                    }
                }
            }

            return bulunan;
        }

        // // ve /* */ yorumlarını (satır sayısını bozmadan) boşlukla değiştirir; metin sabitlerine dokunmaz.
        internal static string YorumlariTemizle(string kaynak)
        {
            var sb = new StringBuilder(kaynak.Length);
            var i = 0;
            while (i < kaynak.Length)
            {
                var c = kaynak[i];
                var sonraki = i + 1 < kaynak.Length ? kaynak[i + 1] : '\0';
                if (c == '"')
                {
                    // Tırnaklı metin: kaçışlı \" ve (verbatim için) "" atlanır.
                    sb.Append(c);
                    i++;
                    while (i < kaynak.Length)
                    {
                        sb.Append(kaynak[i]);
                        if (kaynak[i] == '\\' && i + 1 < kaynak.Length)
                        {
                            sb.Append(kaynak[i + 1]);
                            i += 2;
                            continue;
                        }

                        if (kaynak[i] == '"')
                        {
                            i++;
                            break;
                        }

                        i++;
                    }

                    continue;
                }

                if (c == '/' && sonraki == '/')
                {
                    while (i < kaynak.Length && kaynak[i] != '\n')
                    {
                        i++;
                    }

                    continue;
                }

                if (c == '/' && sonraki == '*')
                {
                    i += 2;
                    while (i < kaynak.Length && !(kaynak[i] == '*' && i + 1 < kaynak.Length && kaynak[i + 1] == '/'))
                    {
                        sb.Append(kaynak[i] == '\n' ? '\n' : ' ');
                        i++;
                    }

                    i += 2;
                    continue;
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }

        [Fact]
        public void Core_kaynaginda_yasakli_api_yoktur()
        {
            var kok = RepoRoot.Combine("src", "Yanki.Core");
            var ihlaller = new List<string>();
            var dosyalar = Directory.EnumerateFiles(kok, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToList();
            Assert.NotEmpty(dosyalar);

            foreach (var dosya in dosyalar)
            {
                var goreli = Path.GetRelativePath(kok, dosya).Replace('\\', '/');
                foreach (var (satir, kuralAdi) in Tara(File.ReadAllText(dosya)))
                {
                    if (Izinler.Any(i => i.Dosya == goreli && i.KuralAdi == kuralAdi))
                    {
                        continue;
                    }

                    var kural = Kurallar.First(k => k.Ad == kuralAdi);
                    ihlaller.Add($"{goreli}:{satir}: '{kuralAdi}' yasak ({kural.Neden}).");
                }
            }

            Assert.True(ihlaller.Count == 0, "Yasaklı API kullanımı:\n" + string.Join("\n", ihlaller));
        }

        [Fact]
        public void Dialogue_kaynaginda_yasakli_ve_kulture_bagli_api_yoktur()
        {
            // 8. hafta: determinizm kuralları Yanki.Dialogue için de geçerlidir; ek olarak kültüre bağlı çağrılar yasaktır.
            var kok = RepoRoot.Combine("src", "Yanki.Dialogue");
            var tum = Kurallar.Concat(KulturKurallari).ToList();
            var ihlaller = new List<string>();
            var dosyalar = Directory.EnumerateFiles(kok, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                         && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToList();
            Assert.NotEmpty(dosyalar);

            foreach (var dosya in dosyalar)
            {
                var goreli = Path.GetRelativePath(kok, dosya).Replace('\\', '/');
                foreach (var (satir, kuralAdi) in Tara(File.ReadAllText(dosya), tum))
                {
                    ihlaller.Add($"{goreli}:{satir}: '{kuralAdi}' yasak ({tum.First(k => k.Ad == kuralAdi).Neden}).");
                }
            }

            Assert.True(ihlaller.Count == 0, "Yasaklı API kullanımı (Yanki.Dialogue):\n" + string.Join("\n", ihlaller));
        }

        [Theory]
        [InlineData("var s = ad.ToLower();", "ToLower/ToUpper")]
        [InlineData("var s = ad.ToUpperInvariant();", "ToLower/ToUpper")]
        [InlineData("var s = string.Format(\"{0}\", x);", "string.Format")]
        [InlineData("var c = CultureInfo.CurrentCulture;", "CurrentCulture")]
        [InlineData("var c = string.Compare(a, b);", "string.Compare")]
        [InlineData("if (ad.StartsWith(\"a\")) { }", "StartsWith/EndsWith/IndexOf(string)")]
        [InlineData("var i = ad.IndexOf(\"ş\");", "StartsWith/EndsWith/IndexOf(string)")]
        public void Kultur_tarayicisi_kulture_bagli_kalibi_yakalar(string kod, string beklenenKural)
        {
            Assert.Contains(Tara(kod, KulturKurallari), b => b.KuralAdi == beklenenKural);
        }

        [Theory]
        [InlineData("if (ad.StartsWith(\"a\", StringComparison.Ordinal)) { }")]
        [InlineData("var s = string.Concat(a, b); var t = ad.Replace(\"a\", \"b\");")]
        [InlineData("var c = string.CompareOrdinal(a, b); var e = string.Equals(a, b, StringComparison.Ordinal);")]
        public void Kultur_tarayicisi_ordinal_kullanimi_yakalamaz(string kod)
        {
            Assert.Empty(Tara(kod, KulturKurallari));
        }

        [Theory]
        [InlineData("Decision/KonuHesabi.cs")]
        [InlineData("Mind/DedikoduAyarlari.cs")]
        [InlineData("Mind/StresAyarlari.cs")]
        [InlineData("Simulation/KrizTablosu.cs")]
        [InlineData("Simulation/Simulasyon.cs")]
        public void Tarama_6_hafta_dosyalarini_kapsar(string goreli)
        {
            var kok = RepoRoot.Combine("src", "Yanki.Core");
            var taranan = Directory.EnumerateFiles(kok, "*.cs", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(kok, f).Replace('\\', '/'))
                .ToList();

            Assert.Contains(goreli, taranan);
        }

        [Fact]
        public void Her_izin_gerekce_tasir_ve_var_olan_bir_dosya_ile_kurala_isaret_eder()
        {
            var kok = RepoRoot.Combine("src", "Yanki.Core");
            foreach (var (dosya, kuralAdi, gerekce) in Izinler)
            {
                Assert.False(string.IsNullOrWhiteSpace(gerekce), $"{dosya}/{kuralAdi} izninin gerekçesi yok.");
                Assert.True(File.Exists(Path.Combine(kok, dosya.Replace('/', Path.DirectorySeparatorChar))), $"İzin listesindeki dosya yok: {dosya}");
                Assert.Contains(Kurallar, k => k.Ad == kuralAdi);
            }
        }

        [Theory]
        [InlineData("var r = new System.Random();", "System.Random")]
        [InlineData("var r = new Random(5);", "new Random(")]
        [InlineData("var t = DateTime.Now;", "DateTime.Now")]
        [InlineData("var t = DateTime.UtcNow;", "DateTime.UtcNow")]
        [InlineData("var t = DateTimeOffset.UtcNow;", "DateTimeOffset.Now/UtcNow")]
        [InlineData("var g = Guid.NewGuid();", "Guid.NewGuid")]
        [InlineData("var h = \"x\".GetHashCode();", "GetHashCode(")]
        [InlineData("public override int GetHashCode () => 1;", "GetHashCode(")]
        [InlineData("var h = HashCode.Combine(1, 2);", "HashCode.")]
        [InlineData("double x = 1;", "double")]
        [InlineData("float x = 1f;", "float")]
        [InlineData("var d = new Dictionary<string, int>();", "Dictionary<")]
        [InlineData("var s = new HashSet<string>();", "HashSet<")]
        [InlineData("var p = Math.Pow(2, 3);", "Math.Pow/Exp/Log/Sqrt/Trig")]
        [InlineData("var e = System.Math.Exp(1);", "Math.Pow/Exp/Log/Sqrt/Trig")]
        [InlineData("var l = Math.Log(2);", "Math.Pow/Exp/Log/Sqrt/Trig")]
        [InlineData("var r = Math.Sqrt(2);", "Math.Pow/Exp/Log/Sqrt/Trig")]
        [InlineData("var t = Math.Tanh(2);", "Math.Pow/Exp/Log/Sqrt/Trig")]
        public void Tarayici_yasakli_kalibi_yakalar(string kod, string beklenenKural)
        {
            var bulunan = Tara(kod);

            Assert.Contains(bulunan, b => b.KuralAdi == beklenenKural);
        }

        [Fact]
        public void Tarama_karar_klasorunu_ve_5_hafta_dosyalarini_kapsar()
        {
            var kok = RepoRoot.Combine("src", "Yanki.Core");
            var taranan = Directory.EnumerateFiles(kok, "*.cs", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(kok, f).Replace('\\', '/'))
                .Where(f => !f.StartsWith("obj/", StringComparison.Ordinal) && !f.StartsWith("bin/", StringComparison.Ordinal))
                .ToList();

            foreach (var beklenen in new[]
            {
                "Decision/UtilitySelector.cs", "Decision/Egriler.cs", "Decision/DecimalMat.cs", "Decision/Puanlayici.cs",
                "Decision/AdayUretici.cs", "Decision/EylemYukleyici.cs", "Decision/KararIziYazici.cs", "Simulation/Simulasyon.cs",
            })
            {
                Assert.Contains(beklenen, taranan);
            }
        }

        [Theory]
        [InlineData("var x = Math.Round(1.5m, 6, MidpointRounding.ToEven);")]
        [InlineData("var x = Math.Max(1m, 2m); var y = Math.Min(1m, 2m); var z = Math.Abs(-1m);")]
        [InlineData("var d = new SortedDictionary<string, int>(StringComparer.Ordinal);")]
        [InlineData("var s = new SortedSet<string>(StringComparer.Ordinal);")]
        [InlineData("decimal x = 1m; // double ve float burada yalnızca yorum")]
        [InlineData("/* DateTime.Now bu blok yorumunda */ var x = 1;")]
        [InlineData("/// <summary>System.Random yerine AnahtarliRng.</summary>")]
        [InlineData("var doubled = 2; var floating = 3;")]
        public void Tarayici_gecerli_kodu_ve_yorumlari_yakalamaz(string kod)
        {
            Assert.Empty(Tara(kod));
        }

        [Fact]
        public void Tarayici_cok_satirli_blok_yorumda_satir_numarasini_korur()
        {
            var bulunan = Tara("/* a\nb\nc */\nvar t = DateTime.Now;");

            Assert.Equal(new[] { (4, "DateTime.Now") }, bulunan.ToArray());
        }
    }
}
