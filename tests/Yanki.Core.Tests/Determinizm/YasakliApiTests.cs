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
        };

        // Bilinçli istisnalar: (src/Yanki.Core'a göre göreli yol, kural adı, GEREKÇE). Boş tutmak hedeftir.
        // Örnek: ("Polyfills/Ornek.cs", "HashSet<", "netstandard2.1 polyfill'i; hiçbir yerde gezilmez.")
        internal static readonly (string Dosya, string KuralAdi, string Gerekce)[] Izinler =
        {
        };

        internal static IReadOnlyList<(int Satir, string KuralAdi)> Tara(string kaynak)
        {
            var temiz = YorumlariTemizle(kaynak);
            var bulunan = new List<(int, string)>();
            var satirlar = temiz.Split('\n');
            for (var i = 0; i < satirlar.Length; i++)
            {
                foreach (var kural in Kurallar)
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
        public void Tarayici_yasakli_kalibi_yakalar(string kod, string beklenenKural)
        {
            var bulunan = Tara(kod);

            Assert.Contains(bulunan, b => b.KuralAdi == beklenenKural);
        }

        [Theory]
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
