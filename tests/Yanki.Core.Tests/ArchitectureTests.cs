using System.Linq;
using Xunit;
using Yanki.Core;
using Yanki.Dialogue;

namespace Yanki.Core.Tests
{
    /// <summary>
    /// Mimari sınırları koruyan testler. Bunlar iskeletin "boş ama kurallı" olmasını sağlar:
    /// ileride biri yanlışlıkla çekirdeğe bağımlılık eklerse CI kırmızıya döner.
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
        public void Dialogue_katmani_yuklenebilir()
        {
            // Not: Dialogue → Core bağımlılığı csproj'da tanımlı, ama Dialogue Core'dan tip kullanmadıkça
            // derleyici bu referansı DLL'e yazmaz. O yüzden burada yalnızca katmanın var olduğunu doğruluyoruz.
            Assert.Equal("Yanki.Dialogue", typeof(DialogueInfo).Assembly.GetName().Name);
        }

        [Fact]
        public void Olay_gunlugu_bicim_surumu_tanimlidir()
        {
            Assert.True(YankiInfo.EventLogFormatVersion >= 1);
        }
    }
}
