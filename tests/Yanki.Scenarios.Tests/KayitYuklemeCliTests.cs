using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Yanki.Cli;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// CLI: <c>yanki devam</c> (kayıttan devam), <c>gunluk_hash</c> ve <c>senaryo_hash</c> denetimi (çıkış kodları 5 ve 6) ve üç golden'ın sürüm 2 alanları.
    /// Senaryo olarak <c>samples/HanGerilim</c> kullanılır; geçici klasöre kopyalanır ki dosyaları değiştirmek repoyu etkilemesin.
    /// </summary>
    public sealed class KayitYuklemeCliTests : IDisposable
    {
        private const string Anlat = "anlatti:Ayse:onerme=borclu(Mehmet,Ayse,500)";
        private readonly string _dizin;
        private readonly string _senaryo;

        public KayitYuklemeCliTests()
        {
            _dizin = Path.Combine(Path.GetTempPath(), "yanki-devam-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dizin);
            var kaynak = Path.Combine(RepoKoku(), "samples", "HanGerilim");
            var hedef = Path.Combine(_dizin, "senaryo");
            Directory.CreateDirectory(hedef);
            foreach (var f in Directory.GetFiles(kaynak, "*.json"))
            {
                File.Copy(f, Path.Combine(hedef, Path.GetFileName(f)));
            }

            _senaryo = Path.Combine(hedef, "scenario.json");
        }

        public void Dispose() => Directory.Delete(_dizin, true);

        private string Yol(string ad) => Path.Combine(_dizin, ad);

        private static (int Kod, string Cikti, string Hata) Calistir(params string[] args)
        {
            var cikti = new StringWriter();
            var hata = new StringWriter();
            var kod = KomutCalistirici.Calistir(args, cikti, hata);
            return (kod, cikti.ToString(), hata.ToString());
        }

        private static string RepoKoku()
        {
            var dizin = new DirectoryInfo(AppContext.BaseDirectory);
            while (dizin != null && !File.Exists(Path.Combine(dizin.FullName, "Yanki.slnx")))
            {
                dizin = dizin.Parent;
            }

            return dizin?.FullName ?? throw new InvalidOperationException("Repo kökü bulunamadı.");
        }

        [Fact]
        public void Devam_N_tick_kaydet_yukle_M_tick_ile_N_arti_M_tick_kesintisiz_kaydi_birebir_uretir()
        {
            Assert.Equal(0, Calistir("run", _senaryo, "--seed", "42", "--eylem", Anlat, "--tick", "3", "--kaydet", Yol("a.json")).Kod);
            var (kod, cikti, hata) = Calistir("devam", Yol("a.json"), "--tick", "5", "--kaydet", Yol("b.json"));
            Assert.True(kod == 0, hata);
            Assert.Contains("oturum kaydedildi", cikti);

            Assert.Equal(0, Calistir("run", _senaryo, "--seed", "42", "--eylem", Anlat, "--tick", "3", "--tick", "5", "--kaydet", Yol("c.json")).Kod);

            Assert.Equal(File.ReadAllText(Yol("c.json")), File.ReadAllText(Yol("b.json")));   // eylemler, tick, son_hash, gunluk_hash, senaryo_hash
            Assert.Equal(0, Calistir("replay", Yol("b.json")).Kod);
        }

        [Fact]
        public void Devam_bekleyen_eylemi_kaydeder_ve_geri_Submit_eder()
        {
            Assert.Equal(0, Calistir("run", _senaryo, "--seed", "42", "--tick", "2", "--kaydet", Yol("a.json")).Kod);
            var (kod, _, hata) = Calistir("devam", Yol("a.json"), "--eylem", Anlat, "--kaydet", Yol("bekleyen.json"));    // tick yok: eylem bekliyor
            Assert.True(kod == 0, hata);
            Assert.Contains("bekleyen_eylemler", File.ReadAllText(Yol("bekleyen.json")));
            Assert.Contains("borclu(Mehmet,Ayse,500)", File.ReadAllText(Yol("bekleyen.json")));

            Assert.Equal(0, Calistir("devam", Yol("bekleyen.json"), "--tick", "4", "--kaydet", Yol("c.json")).Kod);
            Assert.Equal(0, Calistir("devam", Yol("a.json"), "--eylem", Anlat, "--tick", "4", "--kaydet", Yol("d.json")).Kod);

            Assert.Equal(File.ReadAllText(Yol("d.json")), File.ReadAllText(Yol("c.json")));
        }

        [Fact]
        public void Devam_kaydet_ister_ve_eski_kaydi_degistirmez()
        {
            Assert.Equal(0, Calistir("run", _senaryo, "--tick", "2", "--kaydet", Yol("a.json")).Kod);
            var once = File.ReadAllText(Yol("a.json"));

            Assert.Equal(KomutCalistirici.KullanimHatasi, Calistir("devam", Yol("a.json"), "--tick", "2").Kod);
            Assert.Equal(0, Calistir("devam", Yol("a.json"), "--tick", "2", "--kaydet", Yol("b.json")).Kod);
            Assert.Equal(once, File.ReadAllText(Yol("a.json")));
        }

        [Fact]
        public void Bozuk_gunluk_hash_5_koduyla_cikar_guncelle_duzeltir_devam_durur()
        {
            Assert.Equal(0, Calistir("run", _senaryo, "--eylem", Anlat, "--tick", "4", "--kaydet", Yol("a.json")).Kod);
            var icerik = File.ReadAllText(Yol("a.json"));
            File.WriteAllText(Yol("a.json"), Regex.Replace(icerik, "\"gunluk_hash\": \"[0-9a-f]{16}\"", "\"gunluk_hash\": \"0000000000000000\""));

            var (kod, cikti, _) = Calistir("replay", Yol("a.json"));
            Assert.Equal(KomutCalistirici.GunlukHashUyusmadi, kod);
            Assert.Equal(5, kod);
            Assert.Contains("TUTMUYOR", cikti);

            var (devamKod, _, devamHata) = Calistir("devam", Yol("a.json"), "--tick", "1", "--kaydet", Yol("x.json"));
            Assert.Equal(5, devamKod);
            Assert.Contains("tutmuyor", devamHata);
            Assert.False(File.Exists(Yol("x.json")));

            Assert.Equal(0, Calistir("replay", Yol("a.json"), "--guncelle").Kod);
            Assert.Equal(icerik, File.ReadAllText(Yol("a.json")));
        }

        [Fact]
        public void Senaryo_dosyalari_degistiyse_yukleme_6_koduyla_acik_hatayla_durur()
        {
            Assert.Equal(0, Calistir("run", _senaryo, "--eylem", Anlat, "--tick", "3", "--kaydet", Yol("a.json")).Kod);
            Assert.Equal(0, Calistir("replay", Yol("a.json")).Kod);

            var kurallar = Path.Combine(Path.GetDirectoryName(_senaryo)!, "kurallar.json");
            File.AppendAllText(kurallar, "\n");
            var (kod, _, hata) = Calistir("replay", Yol("a.json"));

            Assert.Equal(KomutCalistirici.SenaryoDegisti, kod);
            Assert.Equal(6, kod);
            Assert.Contains("senaryo_hash", hata);
            Assert.Equal(6, Calistir("devam", Yol("a.json"), "--tick", "1", "--kaydet", Yol("b.json")).Kod);
        }

        [Fact]
        public void Senaryo_hash_satir_sonundan_bagimsizdir_CRLF_ile_yeniden_yazilan_dosya_ayni_degeri_verir()
        {
            Assert.Equal(0, Calistir("run", _senaryo, "--tick", "2", "--kaydet", Yol("a.json")).Kod);
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(_senaryo)!, "*.json"))
            {
                File.WriteAllText(f, File.ReadAllText(f).Replace("\r\n", "\n").Replace("\n", "\r\n"));
            }

            Assert.Equal(0, Calistir("replay", Yol("a.json")).Kod);
        }

        [Theory]
        [InlineData("mini-han-1.json")]
        [InlineData("mini-karar-1.json")]
        [InlineData("han-gerilim-1.json")]
        public void Golden_oturumlar_surum_2_alanlarini_tasir(string ad)
        {
            var json = File.ReadAllText(Path.Combine(RepoKoku(), "tests", "golden", ad));
            Assert.Contains("\"surum\": 2", json);
            Assert.Matches("\"gunluk_hash\": \"[0-9a-f]{16}\"", json);
            Assert.Matches("\"senaryo_hash\": \"[0-9a-f]{16}\"", json);
            Assert.Contains("\"bekleyen_eylemler\": []", json);
        }
    }
}
