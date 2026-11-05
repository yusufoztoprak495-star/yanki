using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xunit;
using Yanki.Core.Simulation;

namespace Yanki.Dialogue.Tests
{
    /// <summary>
    /// IDialogueRenderer ve diyalog hash'i (8. hafta, H ve I kararları): girdi beyaz listedir (reflection testiyle kilitli), çıktı tek yönlüdür, <c>diyalog_hash</c> her zaman varsayılan
    /// renderer'ın çıktısından hesaplanır; aynı seed ve girdi aynı satırları, farklı seed ya da farklı sıra ölçülebilir şekilde farklı satırları verir.
    /// </summary>
    public class RendererVeHashTests
    {
        private const string Sablonlar = @"{ 'dil': 'tr',
 'adlar': { 'Ali': 'Ali', 'Veli': 'Veli', 'oyuncu': { 'ad': 'yabancı', 'cins': true } },
 'esleme': { 'yardim_etti': { 'niyet': 'tesekkur', 'konusan': 'hedef' } },
 'gramer': { 'selam': ['Merhaba', 'Selam', 'Günaydın', 'İyi günler'], 'dua': ['Sağ ol', 'Eyvallah', 'Teşekkürler'] },
 'sablonlar': [
  { 'id': 'genel', 'concept': 'tesekkur', 'genel': true, 'metin': '#selam# #dinleyen#, #dua#.' },
  { 'id': 'a', 'concept': 'tesekkur', 'kriterler': [ { 'alan': 'kisilik(sosyallik)', 'op': '>=', 'deger': 0, 'zorunlu': true, 'agirlik': 1 } ], 'metin': 'A: #dua# #dinleyen.yonelme#.' },
  { 'id': 'b', 'concept': 'tesekkur', 'kriterler': [ { 'alan': 'kisilik(sosyallik)', 'op': '>=', 'deger': 0, 'zorunlu': true, 'agirlik': 1 } ], 'metin': 'B: #dua# #dinleyen.belirtme#.' }
 ] }";

        private static (Simulasyon Sim, DiyalogMotoru Motor) Oyna(ulong seed, IEnumerable<(int Tick, string Hedef)> yardimlar, int tick = 20, IDialogueRenderer? renderer = null)
        {
            var sim = DiyalogFikstur.Sim(null, seed);
            var motor = DiyalogMotoru.Baglan(sim, DiyalogFikstur.Sablon(Sablonlar), renderer: renderer);
            var takvim = yardimlar.ToLookup(y => y.Tick, y => y.Hedef);
            for (var t = 1; t <= tick; t++)
            {
                foreach (var h in takvim[t])
                {
                    sim.Submit(new PlayerAction("yardim_etti", h));
                }

                sim.Tick(1);
            }

            return (sim, motor);
        }

        private static readonly (int, string)[] Girdi = Enumerable.Range(1, 16).Select(i => (i, i % 2 == 0 ? "Ali" : "Veli")).ToArray();

        // ---- H: renderer girdisi beyaz liste ----

        [Fact]
        public void Render_girdisi_yalnizca_niyet_sablon_metni_secilmis_gercekler_ve_gerekce_tasir()
        {
            var props = typeof(RenderGirdisi).GetProperties().Select(p => p.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray();

            Assert.Equal(new[] { "Gerekce", "Niyet", "SablonMetni", "SecilmisGercekler" }, props);
        }

        [Fact]
        public void Render_girdisinin_erisilebilir_tip_grafinda_Core_tipi_yoktur_Simulasyon_Zihin_Olay_KararIzi_gecmez()
        {
            var gorulen = new HashSet<Type>();
            var kuyruk = new Queue<Type>();
            kuyruk.Enqueue(typeof(RenderGirdisi));
            while (kuyruk.Count > 0)
            {
                var t = kuyruk.Dequeue();
                if (!gorulen.Add(t))
                {
                    continue;
                }

                foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    AltTipler(p.PropertyType).ToList().ForEach(kuyruk.Enqueue);
                }

                foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    AltTipler(f.FieldType).ToList().ForEach(kuyruk.Enqueue);
                }
            }

            Assert.DoesNotContain(gorulen, t => (t.Namespace ?? string.Empty).StartsWith("Yanki.Core", StringComparison.Ordinal));
            Assert.DoesNotContain(gorulen, t => t.Name.Contains("KuralId", StringComparison.Ordinal) || t.Name == "Olay" || t.Name == "Zihin" || t.Name == "Simulasyon");
            var alanAdlari = gorulen.SelectMany(t => t.GetProperties().Select(p => p.Name)).ToList();
            Assert.DoesNotContain(alanAdlari, a => a.Contains("KuralId", StringComparison.OrdinalIgnoreCase) || a.Contains("KararIzi", StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<Type> AltTipler(Type t)
        {
            if (t.IsGenericType)
            {
                foreach (var a in t.GetGenericArguments())
                {
                    foreach (var x in AltTipler(a))
                    {
                        yield return x;
                    }
                }
            }

            if (t.IsArray)
            {
                yield return t.GetElementType()!;
            }

            if (!t.IsGenericType)
            {
                yield return t;
            }
        }

        private sealed class BuyukHarfRenderer : IDialogueRenderer
        {
            public int Cagri { get; private set; }

            public string Render(RenderGirdisi girdi)
            {
                Cagri++;
                return "[eklenti] " + girdi.SablonMetni.Length.ToString(CultureInfo.InvariantCulture) + " " + girdi.Niyet.Ad;
            }
        }

        private sealed class TekrarYazanRenderer : IDialogueRenderer
        {
            // Deterministik olmayan bir eklenti: her çağrıda farklı metin (Environment.TickCount64 yerine sayaç; test deterministik kalsın).
            private int _n;

            public string Render(RenderGirdisi girdi) => "rastgele-" + (_n++).ToString(CultureInfo.InvariantCulture);
        }

        [Fact]
        public void Diyalog_hash_her_zaman_varsayilan_renderer_ciktisindan_eklenti_replayi_bozmaz()
        {
            var (_, varsayilan) = Oyna(42, Girdi);
            var eklenti = new TekrarYazanRenderer();
            var (_, ozel) = Oyna(42, Girdi, renderer: eklenti);

            Assert.Equal(varsayilan.DiyalogHash(), ozel.DiyalogHash());
            Assert.All(ozel.Satirlar, s => Assert.StartsWith("rastgele-", s.Metin, StringComparison.Ordinal));
            Assert.Equal(varsayilan.Satirlar.Select(s => s.Metin), ozel.Satirlar.Select(s => s.VarsayilanMetin));
        }

        [Fact]
        public void Renderer_cikti_simulasyona_ve_tekrar_durumuna_geri_yazilmaz()
        {
            var (simVarsayilan, varsayilan) = Oyna(42, Girdi);
            var (simOzel, ozel) = Oyna(42, Girdi, renderer: new BuyukHarfRenderer());

            Assert.Equal(simVarsayilan.DurumHash(), simOzel.DurumHash());
            Assert.Equal(simVarsayilan.Gunluk.Sayi, simOzel.Gunluk.Sayi);
            Assert.Equal(varsayilan.Satirlar.Select(s => s.SablonId), ozel.Satirlar.Select(s => s.SablonId));   // seçim renderer'dan etkilenmez
        }

        private sealed class AtanRenderer : IDialogueRenderer
        {
            private readonly Func<Exception?> _at;

            public AtanRenderer(Func<Exception?> at) => _at = at;

            public string Render(RenderGirdisi girdi) => throw _at()!;
        }

        private sealed class NullDonenRenderer : IDialogueRenderer
        {
#pragma warning disable CS8603
            public string Render(RenderGirdisi girdi) => null;
#pragma warning restore CS8603
        }

        // H kararı (düzeltme): özel renderer'ın HİÇBİR hatası seçimi, tekrar durumunu, diyalog_hash'i ya da Tick'i etkilemez; satır varsayılan metinle üretilir ve nota yazılır.
        [Theory]
        [InlineData("InvalidOperationException")]
        [InlineData("ArgumentException")]
        [InlineData("NullReferenceException")]
        [InlineData("ApplicationException")]
        [InlineData("null")]
        public void Ozel_renderer_hatasi_secimi_tekrari_hashi_ve_ticki_etkilemez_satir_varsayilan_metinle_uretilir(string tur)
        {
            IDialogueRenderer ozel = tur switch
            {
                "InvalidOperationException" => new AtanRenderer(() => new InvalidOperationException("yuva bağlanamadı (eklenti)")),
                "ArgumentException" => new AtanRenderer(() => new ArgumentException("eklenti")),
                "NullReferenceException" => new AtanRenderer(() => new NullReferenceException()),
                "ApplicationException" => new AtanRenderer(() => new ApplicationException("eklenti")),
                _ => new NullDonenRenderer(),
            };
            var (simV, varsayilan) = Oyna(42, Girdi);

            var (simO, motor) = Oyna(42, Girdi, renderer: ozel);

            Assert.Equal(varsayilan.DiyalogHash(), motor.DiyalogHash());
            Assert.Equal(varsayilan.Satirlar.Select(s => s.SablonId), motor.Satirlar.Select(s => s.SablonId));
            Assert.Equal(varsayilan.Satirlar.Select(s => (s.Tick, s.Konusan, s.Dinleyen)), motor.Satirlar.Select(s => (s.Tick, s.Konusan, s.Dinleyen)));
            Assert.Equal(varsayilan.Satirlar.Select(s => s.Metin), motor.Satirlar.Select(s => s.Metin));   // varsayılan metin
            Assert.All(motor.Satirlar, s => Assert.Contains(s.NedenSatirlari, n => n.Contains("renderer", StringComparison.Ordinal) && n.Contains("varsayılan metin", StringComparison.Ordinal)));
            Assert.Equal(simV.SimdikiTick, simO.SimdikiTick);
            Assert.Equal(simV.DurumHash(), simO.DurumHash());
        }

        [Fact]
        public void Varsayilan_renderer_yuva_hatasi_hala_sablonu_eler()
        {
            // yuva bağlanamayan şablon aday olamaz (genel şablon kalır); özel renderer hatası ise elemez (yukarıdaki test).
            var dosya = DiyalogFikstur.Sablon(@"{ 'dil': 'tr', 'esleme': { 'yardim_etti': { 'niyet': 'tesekkur', 'konusan': 'hedef' } },
 'sablonlar': [ { 'id': 'genel', 'concept': 'tesekkur', 'genel': true, 'metin': 'Sağ ol.' },
  { 'id': 'konulu', 'concept': 'tesekkur', 'kriterler': [ { 'alan': 'kisilik(sosyallik)', 'op': '>=', 'deger': 0, 'zorunlu': true, 'agirlik': 5 } ], 'metin': 'Sağ ol, #konu#.' } ] }");
            var sim = DiyalogFikstur.Sim(null, 42);
            var motor = DiyalogMotoru.Baglan(sim, dosya);
            sim.Submit(new PlayerAction("yardim_etti", "Ali"));
            sim.Tick(1);

            Assert.Equal("genel", Assert.Single(motor.Satirlar).SablonId);
        }

        // ---- I: diyalog hash'i ----

        [Fact]
        public void Diyalog_hash_satir_sonu_CRLF_ve_LF_icin_ayni()
        {
            DiyalogSatiri Satir(string metin) => new DiyalogSatiri(1, 1, 5, "Ali", "oyuncu", "genel", metin, metin, false, new List<string>());

            var lf = DiyalogHashu.Hesapla(new[] { Satir("Sağ ol.\nGörüşürüz.") });
            var crlf = DiyalogHashu.Hesapla(new[] { Satir("Sağ ol.\r\nGörüşürüz.") });
            var degisik = DiyalogHashu.Hesapla(new[] { Satir("Sağ ol.\nGörüşürüz!") });

            Assert.Equal(lf, crlf);
            Assert.NotEqual(lf, degisik);
            Assert.Equal(16, lf.Length);
        }

        [Fact]
        public void Diyalog_hash_olay_konusan_dinleyen_sablon_ve_metni_kapsar_tick_ve_neden_kapsamaz()
        {
            DiyalogSatiri Satir(long olay = 5, string k = "Ali", string d = "oyuncu", string s = "genel", string m = "x", long tick = 1, string neden = "n") =>
                new DiyalogSatiri(1, tick, olay, k, d, s, m, m, false, new List<string> { neden });

            var temel = DiyalogHashu.Hesapla(new[] { Satir() });

            Assert.NotEqual(temel, DiyalogHashu.Hesapla(new[] { Satir(olay: 6) }));
            Assert.NotEqual(temel, DiyalogHashu.Hesapla(new[] { Satir(k: "Veli") }));
            Assert.NotEqual(temel, DiyalogHashu.Hesapla(new[] { Satir(d: "Ali") }));
            Assert.NotEqual(temel, DiyalogHashu.Hesapla(new[] { Satir(s: "ozel") }));
            Assert.NotEqual(temel, DiyalogHashu.Hesapla(new[] { Satir(m: "y") }));
            Assert.Equal(temel, DiyalogHashu.Hesapla(new[] { Satir(tick: 99, neden: "başka") }));
        }

        // ---- determinizm ----

        [Fact]
        public void Ayni_seed_ve_girdi_ayni_satirlar_ve_diyalog_hash()
        {
            var (_, a) = Oyna(42, Girdi);
            var (_, b) = Oyna(42, Girdi);

            Assert.True(a.Satirlar.Count >= 16);
            Assert.Equal(a.Satirlar.Select(s => s.SablonId + "|" + s.Metin), b.Satirlar.Select(s => s.SablonId + "|" + s.Metin));
            Assert.Equal(a.DiyalogHash(), b.DiyalogHash());
        }

        [Fact]
        public void Farkli_seed_olculebilir_sekilde_farkli_satirlar_verir()
        {
            var hashler = new HashSet<string>();
            for (ulong seed = 1; seed <= 8; seed++)
            {
                hashler.Add(Oyna(seed, Girdi).Motor.DiyalogHash());
            }

            Assert.True(hashler.Count >= 6, $"8 farklı seed yalnızca {hashler.Count} farklı diyalog hash'i verdi");
        }

        [Fact]
        public void Farkli_girdi_sirasi_farkli_satirlar_verir()
        {
            var ters = Girdi.Select(g => (g.Item1, g.Item2 == "Ali" ? "Veli" : "Ali")).ToArray();

            Assert.NotEqual(Oyna(42, Girdi).Motor.DiyalogHash(), Oyna(42, ters).Motor.DiyalogHash());
        }

        [Fact]
        public void Diyalog_hash_tr_TR_ve_invariant_kulturde_ayni()
        {
            var onceki = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                var inv = Oyna(42, Girdi).Motor.DiyalogHash();
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                var tr = Oyna(42, Girdi).Motor.DiyalogHash();

                Assert.Equal(inv, tr);
            }
            finally
            {
                CultureInfo.CurrentCulture = onceki;
            }
        }
    }
}
