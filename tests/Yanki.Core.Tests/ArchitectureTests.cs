using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Xunit;
using Yanki.Core;

namespace Yanki.Core.Tests
{
    /// <summary>
    /// Mimari sınırları koruyan testler: çekirdek hiçbir Yanki katmanını (Dialogue dahil) tanımaz (8. hafta, A kararı); Dialogue simülasyon durumunu
    /// değiştiren hiçbir API'yi çağırmaz. İleride biri yanlışlıkla bağımlılık ya da yazma çağrısı eklerse CI kırmızıya döner.
    /// </summary>
    public class ArchitectureTests
    {
        [Fact]
        public void Core_hicbir_Yanki_katmanina_bagimli_degildir()
        {
            var references = typeof(YankiInfo).Assembly
                .GetReferencedAssemblies()
                .Select(a => a.Name)
                .ToList();

            Assert.DoesNotContain(references, name => name != null && name.StartsWith("Yanki.", System.StringComparison.Ordinal));
        }

        [Fact]
        public void Core_Dialogue_katmanini_tanimaz_ne_derleme_basvurusu_ne_tip_adi()
        {
            var core = typeof(YankiInfo).Assembly;

            Assert.DoesNotContain(core.GetReferencedAssemblies(), a => a.Name == "Yanki.Dialogue");
            Assert.DoesNotContain(core.GetTypes(), t => (t.Namespace ?? string.Empty).Contains("Dialogue", StringComparison.Ordinal));
            var csproj = File.ReadAllText(RepoRoot.Combine("src", "Yanki.Core", "Yanki.Core.csproj"));
            Assert.DoesNotContain("Yanki.Dialogue", csproj.Replace("InternalsVisibleTo Include=\"Yanki.Dialogue.Tests\"", string.Empty), StringComparison.Ordinal);
        }

        [Fact]
        public void Core_yalnizca_cerceve_kutuphanelerine_baglidir()
        {
            // ADR-004: çekirdek dış pakete bağımlı olmaz, Unity'ye sorunsuz girer.
            var references = typeof(YankiInfo).Assembly
                .GetReferencedAssemblies()
                .Select(a => a.Name ?? string.Empty);

            Assert.All(references, name =>
                Assert.True(
                    name.StartsWith("System", System.StringComparison.Ordinal)
                    || name == "netstandard"
                    || name == "mscorlib",
                    $"Beklenmeyen bağımlılık: {name}"));
        }

        [Fact]
        public void Dialogue_katmani_yuklenebilir_ve_yalnizca_cerceve_ile_cekirdege_baglidir()
        {
            var dialogue = typeof(Yanki.Dialogue.DiyalogAyarlari).Assembly;

            Assert.Equal("Yanki.Dialogue", dialogue.GetName().Name);
            Assert.All(dialogue.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty), name =>
                Assert.True(
                    name.StartsWith("System", StringComparison.Ordinal) || name == "netstandard" || name == "mscorlib" || name == "Yanki.Core",
                    $"Beklenmeyen bağımlılık: {name}"));
        }

        // Dialogue'un çekirdekten başvurduğu üyelerden, simülasyon durumunu değiştirenler. Metadata'daki üye başvuruları (MemberRef) taranır.
        private static readonly (string Tip, string Uye)[] Yasak =
        {
            ("Simulasyon", "Submit"), ("Simulasyon", "Tick"), ("Simulasyon", "TamIziIste"),
            ("Zihin", "Uygula"), ("ZihinDeposu", "Uygula"), ("ZihinDeposu", "Ekle"),
            ("OlayGunlugu", "Ekle"), ("Dunya", "Ekle"), ("Dunya", "Kaldir"), ("Dunya", "Uygula"),
        };

        internal static IReadOnlyList<string> CekirdektenYazmaBasvurulari(string dllYolu)
        {
            using var stream = File.OpenRead(dllYolu);
            using var pe = new PEReader(stream);
            var md = pe.GetMetadataReader();
            var bulunan = new List<string>();
            foreach (var h in md.MemberReferences)
            {
                var m = md.GetMemberReference(h);
                if (m.Parent.Kind != HandleKind.TypeReference)
                {
                    continue;
                }

                var tip = md.GetTypeReference((TypeReferenceHandle)m.Parent);
                var tipAdi = md.GetString(tip.Name);
                var uye = md.GetString(m.Name);
                if (Yasak.Any(y => y.Tip == tipAdi && y.Uye == uye))
                {
                    bulunan.Add(tipAdi + "." + uye);
                }
            }

            return bulunan;
        }

        [Fact]
        public void Dialogue_simulasyon_durumunu_degistiren_hicbir_cekirdek_apisini_cagirmaz()
        {
            var yol = typeof(Yanki.Dialogue.DiyalogAyarlari).Assembly.Location;

            Assert.Empty(CekirdektenYazmaBasvurulari(yol));
        }

        [Fact]
        public void Yazma_taramasi_yazma_cagrisi_yakalar_kendi_dll_ornegiyle()
        {
            // Çekirdek assembly'si kendi içinde başvuru değil tanım içerir; test projesinin kendisi Simulasyon.Tick çağırıyor: tarayıcı bunu yakalamalı.
            var yol = typeof(ArchitectureTests).Assembly.Location;

            var bulunan = CekirdektenYazmaBasvurulari(yol);

            Assert.Contains("Simulasyon.Tick", bulunan);
            Assert.Contains("Simulasyon.Submit", bulunan);
        }

        [Fact]
        public void Olay_gunlugu_bicim_surumu_tanimlidir()
        {
            Assert.True(YankiInfo.EventLogFormatVersion >= 1);
        }
    }
}
