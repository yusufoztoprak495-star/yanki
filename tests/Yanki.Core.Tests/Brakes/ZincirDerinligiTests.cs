using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Brakes;
using Yanki.Core.Events;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Simulation;

namespace Yanki.Core.Tests.Brakes
{
    /// <summary>
    /// 7. hafta (C, Seçenek 1): zincir derinliği tick'ler arası ölçülür ve günlükten türer. Kuralın ürettiği olay 1 + nedenlerindeki en büyük
    /// derinliği alır; algı olayları derinlik eklemez; oyuncu, NPC eylemi ve kriz olayları 0 başlar. D'yi (varsayılan 4) aşacak kural çalışmaz,
    /// <c>zincir</c> freni yazılır (etki düşer; plandaki "ertelenir" yerine, ADR-013 sapması).
    /// </summary>
    public class ZincirDerinligiTests
    {
        // A: yardim_etti → modifier. B: modifier_eklendi → eksen. C: eksen_degisti → modifier. B ve C birbirini sürer (rule→rule döngüsü).
        private const string DongulukKurallar = @"{ ""kurallar"": [
  { ""id"": ""a"", ""ne_zaman"": ""yardim_etti"", ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""olay.fail"", ""modifier"": ""minnet"", ""deger"": 1, ""sure"": 1000 } ] },
  { ""id"": ""b"", ""ne_zaman"": ""modifier_eklendi"", ""etkiler"": [ { ""tur"": ""eksen"", ""hedef"": ""oyuncu"", ""eksen"": ""guven"", ""delta"": 1 } ] },
  { ""id"": ""c"", ""ne_zaman"": ""eksen_degisti"", ""etkiler"": [ { ""tur"": ""modifier"", ""hedef"": ""oyuncu"", ""modifier"": ""minnet"", ""deger"": 1, ""sure"": 1000 } ] }
] }";

        private static Simulasyon Yap(int? d = null)
        {
            var senaryo = SimFikstur.Senaryo();
            var sonuc = KuralYukleyici.Yukle(DongulukKurallar, "k.json", senaryo.OlayTurleri);
            Assert.True(sonuc.Basarili, string.Join("\n", sonuc.Hatalar));
            return Simulasyon.Olustur(senaryo, sonuc.Kurallar, 42, frenAyarlari: d == null ? null : new FrenAyarlari(d.Value));
        }

        private static Olay[] Zincir(Simulasyon s) => s.Frenler("zincir").ToArray();

        [Fact]
        public void Kural_kural_dongusu_D_4te_durur_zincir_freni_yazilir()
        {
            var s = Yap();
            s.Submit(new PlayerAction("yardim_etti", "Ali"));
            s.Tick(12);

            // Zincir: yardim_etti(0) → a(1) → b(2) → c(3) → b(4) → c(5 > 4: düşer). Her NPC için ayrı zincir.
            var ruleIds = new[] { "a", "b", "c" };
            var tumDerinlikler = s.Gunluk.Olaylar.Where(o => ruleIds.Contains(o.KuralId)).Select(o => s.ZincirDerinligi(o.Id)).ToArray();
            Assert.NotEmpty(tumDerinlikler);
            Assert.True(tumDerinlikler.Max() <= 4, "derinlik " + tumDerinlikler.Max());
            Assert.Equal(4, tumDerinlikler.Max());

            var fren = Zincir(s);
            Assert.NotEmpty(fren);
            Assert.All(fren, f =>
            {
                Assert.Equal("c", f.Deger("kimlik"));
                Assert.Equal("fren:zincir", f.KuralId);
                Assert.Equal(5, f.Tick);                 // her tick bir halka: a=1, b=2, c=3, b=4, c=5
            });
            Assert.Equal(s.Gunluk.Sayi, s.Gunluk.Olaylar.Last().Id);
            Assert.DoesNotContain(s.Gunluk.Olaylar, o => o.Tick > 5 && ruleIds.Contains(o.KuralId));   // zincir durdu, sonsuza gitmedi
        }

        [Fact]
        public void Fren_olayi_why_ile_zincirin_koklerine_iner()
        {
            var s = Yap();
            s.Submit(new PlayerAction("yardim_etti", "Ali"));
            s.Tick(6);

            var fren = Zincir(s).First();
            var neden = s.Neden(fren.Id);
            Assert.Contains(neden, o => o.Tur == "yardim_etti" && o.KuralId == "player");
            Assert.Contains(neden, o => o.KuralId == "a");
            Assert.Contains(neden, o => o.KuralId == "b");
        }

        [Fact]
        public void D_ayarlanabilir_kucuk_D_zinciri_daha_erken_keser()
        {
            var s = Yap(2);
            s.Submit(new PlayerAction("yardim_etti", "Ali"));
            s.Tick(8);

            var tumDerinlikler = s.Gunluk.Olaylar.Where(o => new[] { "a", "b", "c" }.Contains(o.KuralId)).Select(o => s.ZincirDerinligi(o.Id)).ToArray();
            Assert.Equal(2, tumDerinlikler.Max());
            Assert.NotEmpty(Zincir(s));
            Assert.All(Zincir(s), f =>
            {
                Assert.Equal("c", f.Deger("kimlik"));
                Assert.Equal(3, f.Tick);
            });
        }

        [Fact]
        public void Algi_olaylari_derinlik_eklemez_oyuncu_olayi_ve_kural_cikisi_dogru_derinlikte()
        {
            var s = Yap();
            s.Submit(new PlayerAction("yardim_etti", "Ali"));
            s.Tick(1);

            var oyuncu = s.Gunluk.Olaylar.Single(o => o.KuralId == "player");
            var algi = s.Gunluk.Olaylar.Where(o => o.KuralId == "algi").ToArray();
            var a = s.Gunluk.Olaylar.Where(o => o.KuralId == "a").ToArray();
            Assert.Equal(0, s.ZincirDerinligi(oyuncu.Id));
            Assert.NotEmpty(algi);
            Assert.All(algi, o => Assert.Equal(0, s.ZincirDerinligi(o.Id)));
            Assert.All(a, o => Assert.Equal(1, s.ZincirDerinligi(o.Id)));
        }

        [Fact]
        public void Derinlik_gunlukten_yeniden_kurulur()
        {
            var s = Yap();
            s.Submit(new PlayerAction("yardim_etti", "Ali"));
            s.Tick(6);

            var yeni = new ZincirDerinligi(s.Kurallar.Select(k => k.Id));
            foreach (var o in s.Gunluk.Olaylar)
            {
                yeni.Isle(o);
            }

            Assert.All(s.Gunluk.Olaylar, o => Assert.Equal(yeni.Derinlik(o.Id), s.ZincirDerinligi(o.Id)));
        }

        [Fact]
        public void NPC_eylemi_ve_kriz_olaylari_derinlik_0dan_baslar()
        {
            var z = new ZincirDerinligi(new[] { "r" });
            var deriv = new List<Olay>
            {
                new Olay(1, 1, 0, "x", "oyuncu", "", "", new KeyValuePair<string, string>[0], new long[0], "player"),
                new Olay(2, 1, 1, "y", "", "", "", new KeyValuePair<string, string>[0], new[] { 1L }, "r"),                 // kural çıktısı: 1
                new Olay(3, 2, 0, "selam_oldu", "Ali", "", "", new KeyValuePair<string, string>[0], new[] { 2L }, "eylem:selam"),   // NPC eylemi: 0
                new Olay(4, 2, 1, "itiraf_etti", "Ali", "", "", new KeyValuePair<string, string>[0], new[] { 2L }, "kriz:itiraf"),   // kriz: 0
                new Olay(5, 3, 0, "z", "", "", "", new KeyValuePair<string, string>[0], new[] { 3L }, "r"),                          // eylemden doğan kural çıktısı: 1
            };
            foreach (var o in deriv)
            {
                z.Isle(o);
            }

            Assert.Equal(new[] { 0, 1, 0, 0, 1 }, deriv.Select(o => z.Derinlik(o.Id)).ToArray());
        }

        [Fact]
        public void Varsayilan_D_4tur_ve_gecersiz_D_reddedilir()
        {
            Assert.Equal(4, new FrenAyarlari().ZincirDerinligi);
            Assert.Throws<System.ArgumentException>(() => new FrenAyarlari(0));
        }
    }
}
