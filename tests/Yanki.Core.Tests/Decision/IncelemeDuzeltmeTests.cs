using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Criteria;
using Yanki.Core.Decision;
using Yanki.Core.Tests.Simulation;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>Kod incelemesi (5. hafta) bulgularının düzeltmeleri: ayrıştırıcı gerilemesi, zar aralığı, seçici sonucunun doğrulanması.</summary>
    public class IncelemeDuzeltmeTests
    {
        // ---- eylem adı yalnızca "eylem." ile rol sayılır ----

        [Fact]
        public void Eylem_adli_onerme_ve_varlik_5_hafta_oncesi_gibi_ayristirilir()
        {
            var onerme = IfadeAyristirici.Ayristir("inanc(eylem(Ali, *))");
            var o = Assert.IsType<OnermeArgumani>(Assert.Single(onerme.Argumanlar)).Cagri;
            Assert.Equal("eylem", o.Ad);

            var varlik = IfadeAyristirici.Ayristir("kaynak(x(1), eylem)");
            Assert.Equal("eylem", Assert.IsType<TanimlayiciArgumani>(varlik.Argumanlar[1]).Ad);
        }

        [Fact]
        public void Eylem_hedef_hala_rol_ve_eksik_biçimler_hala_hatadir()
        {
            var c = IfadeAyristirici.Ayristir("fikir(eylem.hedef)");
            Assert.Equal(Rol.EylemHedef, Assert.IsType<RolArgumani>(Assert.Single(c.Argumanlar)).Rol);
            Assert.Throws<KriterHatasi>(() => IfadeAyristirici.Ayristir("fikir(eylem.fail)"));
            Assert.Throws<KriterHatasi>(() => IfadeAyristirici.Ayristir("fikir(eylem.)"));
        }

        // ---- zar [0, 1) aralığında kalır ----

        [Theory]
        [InlineData("0.9999996", "0.999999")]
        [InlineData("0.9999999999", "0.999999")]
        [InlineData("0.5", "0.5")]
        [InlineData("0.0000004", "0")]
        [InlineData("0.2500005", "0.250000")]
        public void Zar_yuvarlama_1e_ulasmaz(string ham, string beklenen)
        {
            var s = UtilitySelector.ZarYuvarla(decimal.Parse(ham, System.Globalization.CultureInfo.InvariantCulture));

            Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), s);
            Assert.True(s < 1m);
        }

        // ---- seçici sonucu aday listesinde olmalı ----

        private sealed class HileliSecici : IActionSelector
        {
            private readonly Func<IReadOnlyList<Aday>, EylemTanimi, Aday> _sec;
            private readonly IReadOnlyList<EylemTanimi> _eylemler;

            public HileliSecici(IReadOnlyList<EylemTanimi> eylemler, Func<IReadOnlyList<Aday>, EylemTanimi, Aday> sec)
            {
                _eylemler = eylemler;
                _sec = sec;
            }

            public SecimSonucu Sec(KararBaglami baglam, IReadOnlyList<Aday> adaylar, IReadOnlyList<ElenenAday> elenenler)
            {
                var aday = _sec(adaylar, _eylemler[0]);
                return new SecimSonucu(aday, KararIzi.Basit(baglam, aday));
            }
        }

        [Fact]
        public void Adaylar_arasinda_olmayan_secim_acik_hatadir()
        {
            var eylemler = KararFikstur.Yukle(KararFikstur.Eylem("yardim", "npc"));
            var sim = Yanki.Core.Simulation.Simulasyon.Olustur(
                SimFikstur.Senaryo(), new Yanki.Core.Rules.KuralTanimi[0], 1, eylemler: eylemler,
                secici: new HileliSecici(eylemler, (adaylar, e) => new Aday(e, "Hayalet")));

            var ex = Assert.Throws<InvalidOperationException>(() => sim.Tick(1));

            Assert.Contains("Hayalet", ex.Message);
            Assert.Contains("aday", ex.Message);
        }

        [Fact]
        public void Adaylardan_biri_secilirse_hata_yoktur()
        {
            var eylemler = KararFikstur.Yukle(KararFikstur.Eylem("yardim", "npc"));
            var sim = Yanki.Core.Simulation.Simulasyon.Olustur(
                SimFikstur.Senaryo(), new Yanki.Core.Rules.KuralTanimi[0], 1, eylemler: eylemler,
                secici: new HileliSecici(eylemler, (adaylar, e) => adaylar[0]));

            sim.Tick(2);

            Assert.Contains(sim.Gunluk.Olaylar, o => o.Tur == "yardim_oldu");
        }
    }
}
