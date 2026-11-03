using System;
using System.IO;
using Xunit;
using Yanki.Cli;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// Satır sonu (CRLF/LF) senaryo okumayı ve durum hash'ini etkilemez: Windows'ta <c>core.autocrlf</c> ile çıkan CRLF'li dosyalar
    /// LF'lilerle aynı hash'i verir (altın hash'ler çalışma kopyasının satır sonundan bağımsızdır).
    /// </summary>
    public sealed class SatirSonuTests : IDisposable
    {
        private readonly string _dizin = Path.Combine(Path.GetTempPath(), "yanki-satirsonu-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            Directory.Delete(_dizin, true);
        }

        private static string Kok()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "Yanki.slnx")))
            {
                d = d.Parent;
            }

            return d!.FullName;
        }

        // Örnek senaryo klasörünü, tüm JSON dosyalarının satır sonu `crlf` ile yeniden yazılmış olarak kopyalar.
        private string Kopyala(string ad, bool crlf)
        {
            var hedef = Path.Combine(_dizin, ad + (crlf ? "-crlf" : "-lf"));
            Directory.CreateDirectory(hedef);
            foreach (var dosya in Directory.GetFiles(Path.Combine(Kok(), "samples", ad), "*.json"))
            {
                var metin = File.ReadAllText(dosya).Replace("\r\n", "\n");
                File.WriteAllText(Path.Combine(hedef, Path.GetFileName(dosya)), crlf ? metin.Replace("\n", "\r\n") : metin);
            }

            return Path.Combine(hedef, "scenario.json");
        }

        [Theory]
        [InlineData("MiniHan", 42UL, 12)]
        [InlineData("MiniKarar", 42UL, 24)]
        public void Crlf_ve_lf_ayni_hash_verir(string ad, ulong seed, int tick)
        {
            string Hash(bool crlf)
            {
                var hata = new StringWriter();
                var sim = SenaryoDosyasi.Yukle(Kopyala(ad, crlf), seed, hata);
                Assert.True(sim != null, hata.ToString());
                sim!.Tick(tick);
                return sim.SonHash;
            }

            Assert.Equal(Hash(false), Hash(true));
        }
    }
}
