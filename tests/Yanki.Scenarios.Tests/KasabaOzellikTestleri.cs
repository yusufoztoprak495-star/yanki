using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsCheck;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Simulation;
using Yanki.Dialogue;

namespace Yanki.Scenarios.Tests
{
    /// <summary>
    /// Kasaba Hanı için özellik tabanlı testler (CsCheck, 9. hafta): <see cref="OzellikTestleri"/> ile aynı sabit tohum ve örnek sayısı, rastgele GEÇERLİ oyuncu eylem dizileri.
    /// Değişmezler: sınırlar, aynı seed + girdi → aynı hash, N tick koş-kaydet-yükle-M tick koş = N+M, korumalı varlık, NedenOlaylar, yalan gizliliği, diyalog açık/kapalı hash eşitliği.
    /// </summary>
    public class KasabaOzellikTestleri
    {
        private const int KasabaTick = 48;

        private static readonly string[] Kisiler = { "Riza", "Mehmet", "Ayse", "Kerem", "Osman", "Can", "Fatma", "Dursun" };

        private static readonly string[] Onermeler = { "borclu(Mehmet,Riza,500)", "kardes(Mehmet,Can)", "konum!(muhur,depo)", "kese_calindi(Kerem,Ayse)" };

        internal static readonly PlayerAction[] Havuz = Kisiler
            .SelectMany(k => Onermeler.Select(o => new PlayerAction("anlatti", k, new[] { new KeyValuePair<string, string>("onerme", o) })))
            .Concat(Kisiler.SelectMany(k => new[] { "yardim_etti", "hakaret_et", "sakladi", "selam_oldu" }.Select(t => new PlayerAction(t, k))))
            .Concat(new[] { new PlayerAction("ele_verdirme_istendi", "Can"), new PlayerAction("borc_odendi", "Riza") })
            .ToArray();

        private static Gen<OzellikTestleri.Vaka> Girdi() =>
            Gen.Select(
                Gen.Int[1, KasabaTick].Select(Gen.Int[0, Havuz.Length - 1]).List[0, 8], Gen.Int[1, 60], Gen.Int[1, KasabaTick - 1], Gen.Bool,
                (l, seed, n, bekleyen) => new OzellikTestleri.Vaka(l.ToArray(), seed, n, bekleyen));

        private static void Korumali(Simulasyon sim)
        {
            // Riza han'dan ayrılamaz; kasa korumalı (calinamaz): kasa_acmak hiç seçilmez; kese yalnızca Kerem tarafından ve Ayşe'den çalınır.
            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => (o.Tur == "ayrildi" || o.Tur == "geldi") && o.Fail == "Riza");
            Assert.Equal("han", sim.Dunya.TekDeger("konum!", "Riza"));
            Assert.DoesNotContain(sim.Gunluk.Olaylar, o => o.Tur == "kasa_acildi");
            Assert.All(sim.Gunluk.Olaylar.Where(o => o.Tur == "kese_calindi"), o => Assert.Equal(("Kerem", "Ayse"), (o.Fail, o.Hedef)));
        }

        [Fact]
        public void Kasaba_degismezleri_rastgele_eylem_dizilerinde_korunur()
        {
            var yalanli = 0;
            Girdi().Sample(
                g =>
                {
                    var takvim = OzellikTestleri.Takvim(g.Eylemler, Havuz);
                    var sim = OzellikTestleri.Kos("KasabaHani", g.Seed, takvim, KasabaTick, OzellikTestleri.SinirlarIcinde);

                    OzellikTestleri.SinirlarIcinde(sim);
                    OzellikTestleri.NedenleriGunluktedir(sim);
                    OzellikTestleri.ZincirSiniriAsilmaz(sim);
                    OzellikTestleri.YalanGizliligi(sim);
                    Korumali(sim);
                    if (sim.Zihinler.Kimlikler.Any(k => sim.Zihinler[k].Aktarimlar.Any(a => a.Yalan)))
                    {
                        yalanli++;
                    }
                },
                iter: OzellikTestleri.Ornek, seed: OzellikTestleri.Tohum, threads: 1);

            Assert.True(yalanli > 0, "hiçbir örnekte yalan söylenmedi: yalan gizliliği değişmezi anlamsız kaldı");
        }

        [Fact]
        public void Kasaba_ayni_seed_ve_girdi_ayni_hash_N_arti_M_denkligi_ve_diyalog_acik_kapali_hash_esitligi()
        {
            var sablon = SablonYukleyici.Yukle(File.ReadAllText(Path.Combine(KokDizin(), "samples", "KasabaHani", "sablonlar.json")), "sablonlar.json").Dosya!;
            Girdi().Sample(
                x =>
                {
                    var (eylemler, seed, n) = (x.Eylemler, x.Seed, x.N);
                    var takvim = OzellikTestleri.Takvim(eylemler, Havuz);
                    var a = OzellikTestleri.Kos("KasabaHani", seed, takvim, KasabaTick);
                    var b = OzellikTestleri.Kos("KasabaHani", seed, takvim, KasabaTick);
                    Assert.Equal(a.SonHash, b.SonHash);
                    Assert.Equal(GunlukHashu.Hesapla(a.Gunluk), GunlukHashu.Hesapla(b.Gunluk));

                    // N + M
                    var ilk = OzellikTestleri.Kos("KasabaHani", seed, takvim, n);
                    var kayit = OturumKaydi.Oku(OturumKaydi.Yaz(OturumKaydi.Uret(ilk, "../../samples/KasabaHani/scenario.json")), "kayit.json").Kayit!;
                    var yuklenen = OzellikTestleri.Yukle("KasabaHani", seed);
                    Assert.True(OturumOynatici.Oynat(yuklenen, kayit).GunlukHashTutuyor);
                    for (var t = n + 1; t <= KasabaTick; t++)
                    {
                        OzellikTestleri.Gonder(yuklenen, takvim[t]);
                        yuklenen.Tick(1);
                    }

                    Assert.Equal(OzellikTestleri.Ozet(a), OzellikTestleri.Ozet(yuklenen));
                    Assert.Equal(a.SonHash, yuklenen.SonHash);

                    // diyalog açık/kapalı: son_hash ve gunluk_hash eşit
                    var acik = OzellikTestleri.Yukle("KasabaHani", seed);
                    using (DiyalogMotoru.Baglan(acik, sablon))
                    {
                        for (var t = 1; t <= KasabaTick; t++)
                        {
                            OzellikTestleri.Gonder(acik, takvim[t]);
                            acik.Tick(1);
                        }
                    }

                    Assert.Equal(a.SonHash, acik.SonHash);
                    Assert.Equal(GunlukHashu.Hesapla(a.Gunluk), GunlukHashu.Hesapla(acik.Gunluk));
                },
                iter: OzellikTestleri.Ornek, seed: OzellikTestleri.Tohum, threads: 1);
        }

        private static string KokDizin()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "Yanki.slnx")))
            {
                d = d.Parent;
            }

            return d!.FullName;
        }
    }
}
