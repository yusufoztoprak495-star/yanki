using System;
using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Json;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>
    /// K3: <c>karar_verildi</c> olayı günlükte KISA izi taşır: seçilen, top-N havuzu ve puanlar, zar, kaynak olaylar, kaynaksız girdiler,
    /// aday/elenen sayıları. Bütün adaylar ve puan bileşenleri günlükte tutulmaz; replay ile yeniden üretilir.
    /// </summary>
    public class KisaIzTests
    {
        // a|Veli puan 1.0, b puan 1.4 (ikisi de havuzda), c puan 0.5 (havuz dışı), elenen: a|Can.
        private static KararIzi Iz(int n = 3)
        {
            var eylemler = KararFikstur.Yukle(
                KararFikstur.Eylem("a", "npc", "\"olcutler\": [ { \"alan\": \"kisilik(cesaret)\", \"egri\": { \"tur\": \"lojistik\", \"k\": 10, \"m\": 0.5 } } ], \"celisen_ozellikler\": [ \"durustluk\" ]", 2m),
                KararFikstur.Eylem("b", taban: 1.4m),
                KararFikstur.Eylem("c", taban: 0.5m));
            var sim = KararFikstur.Sim(eylemler);
            var adaylar = new[] { new Aday(eylemler[0], "Veli"), new Aday(eylemler[1], string.Empty), new Aday(eylemler[2], string.Empty) };
            var elenen = new[] { new ElenenAday(eylemler[0], "Can", "on_kosul #1 tutmadi: ayni_mekanda(eylem.hedef) == true") };
            return new UtilitySelector(new SecimAyarlari(n, 0.7m)).Sec(KararFikstur.Baglam(sim, "Ali"), adaylar, elenen).Iz;
        }

        [Fact]
        public void Basit_iz_kesin_kisa_metinle_yazilir()
        {
            var eylemler = KararFikstur.Yukle(KararFikstur.Eylem("selam"));
            var sim = KararFikstur.Sim(eylemler);
            var iz = KararIzi.Basit(KararFikstur.Baglam(sim, "Ali"), new Aday(eylemler[0], string.Empty));

            Assert.Equal(
                "{\"surum\":\"kisa\",\"npc\":\"Ali\",\"tick\":1,\"ayarlar\":{\"en_iyi_n\":3,\"esik\":0.7},\"puanlanan\":1,\"elenen\":0,"
                + "\"havuz\":[{\"aday\":\"selam|\",\"puan\":null}],\"zar\":null,\"secilen\":{\"eylem\":\"selam\",\"hedef\":\"\",\"celisen_ozellikler\":[]},"
                + "\"kaynak_olaylar\":[],\"kaynaksiz\":[]}",
                KararIziYazici.YazKisa(iz));
        }

        [Fact]
        public void Kisa_iz_havuz_puanlari_zar_secilen_ve_sayilari_tasir()
        {
            var iz = Iz();
            var k = JsonAyristirici.Ayristir(KararIziYazici.YazKisa(iz));

            Assert.Equal("kisa", k.Al("surum")!.MetinDeger);
            Assert.Equal(3m, k.Al("puanlanan")!.SayiDeger);
            Assert.Equal(1m, k.Al("elenen")!.SayiDeger);
            var havuz = k.Al("havuz")!.Elemanlar;
            Assert.Equal(iz.Havuz.ToArray(), havuz.Select(h => h.Al("aday")!.MetinDeger).ToArray());
            var beklenenPuanlar = iz.Havuz.Select(a => iz.Adaylar.Single(x => x.Aday.Anahtar == a).Puan!.Puan).ToArray();
            Assert.Equal(beklenenPuanlar, havuz.Select(h => h.Al("puan")!.SayiDeger).ToArray());
            Assert.Equal(iz.Zar, k.Al("zar")!.SayiDeger);
            Assert.Equal(iz.Secilen!.Eylem.Id, k.Al("secilen")!.Al("eylem")!.MetinDeger);
        }

        [Fact]
        public void Kisa_iz_bilesenleri_elenenleri_ve_havuz_disi_adaylari_icermez()
        {
            var metin = KararIziYazici.YazKisa(Iz());

            Assert.DoesNotContain("olcutler", metin);
            Assert.DoesNotContain("kisilik", metin.Replace("kisilik_carpani", string.Empty));
            Assert.DoesNotContain("on_kosul", metin);
            Assert.DoesNotContain("c|", metin); // havuz dışı aday adı yok
            Assert.DoesNotContain("a|Can", metin);
        }

        [Fact]
        public void Kisa_iz_tam_izden_belirgin_kucuktur_ve_aday_sayisindan_bagimsizdir()
        {
            var eylemler = KararFikstur.Yukle(Enumerable.Range(0, 40).Select(i => KararFikstur.Eylem("e" + i.ToString("D2"), taban: 1m + (i / 100m), ek: "\"olcutler\": [ { \"alan\": \"kisilik(cesaret)\" } ]")).ToArray());
            var sim = KararFikstur.Sim(eylemler);
            var iz = new UtilitySelector().Sec(KararFikstur.Baglam(sim, "Ali"), eylemler.Select(e => new Aday(e, string.Empty)).ToList(), new ElenenAday[0]).Iz;

            var kisa = KararIziYazici.YazKisa(iz).Length;
            var tam = KararIziYazici.Yaz(iz).Length;

            Assert.True(kisa * 10 < tam, $"kısa {kisa}, tam {tam}");
            Assert.True(kisa < 500, $"kısa iz havuz boyutuyla sınırlı kalmalı: {kisa}");
        }

        [Fact]
        public void Karar_olayi_yuku_kisa_izi_tasir()
        {
            var iz = Iz();

            var olay = KararOlayi.Yap(7, iz.Tick, 3, iz, "han");

            Assert.Equal(KararIziYazici.YazKisa(iz), olay.Deger("iz"));
            Assert.DoesNotContain("adaylar", olay.Deger("iz"));
        }

        // ---- KisaIz okuma ----

        [Fact]
        public void KisaIz_olaydan_okunur()
        {
            var iz = Iz();
            var olay = KararOlayi.Yap(7, iz.Tick, 3, iz, "han");

            var k = KisaIz.Oku(olay);

            Assert.Equal("Ali", k.Npc);
            Assert.Equal(iz.Tick, k.Tick);
            Assert.Equal(3, k.EnIyiN);
            Assert.Equal(0.7m, k.Esik);
            Assert.Equal(3, k.PuanlananSayisi);
            Assert.Equal(1, k.ElenenSayisi);
            Assert.Equal(iz.Havuz.ToArray(), k.Havuz.Select(h => h.Anahtar).ToArray());
            Assert.Equal(iz.Zar, k.Zar);
            Assert.Equal(iz.Secilen!.Eylem.Id, k.SecilenEylem);
            Assert.Equal(iz.Secilen.Hedef, k.SecilenHedef);
            Assert.Equal(iz.CelisenOzellikler.ToArray(), k.CelisenOzellikler.ToArray());
        }

        [Fact]
        public void KisaIz_tam_izle_uyusur_ve_uyusmayan_alani_adiyla_bildirir()
        {
            var iz = Iz();
            var k = KisaIz.Oku(KararOlayi.Yap(7, iz.Tick, 3, iz, "han"));

            Assert.Null(k.Fark(iz));

            var baska = Iz(n: 1);
            var fark = k.Fark(baska);
            Assert.NotNull(fark);
        }

        [Fact]
        public void KisaIz_karar_olmayan_ya_da_bozuk_olay_icin_acik_hata_verir()
        {
            var baska = new Olay(1, 1, 0, "selam_oldu", "Ali", string.Empty, "han", new System.Collections.Generic.KeyValuePair<string, string>[0], new long[0], "eylem:selam");
            var bozuk = new Olay(2, 1, 0, "karar_verildi", "Ali", string.Empty, "han", new[] { new System.Collections.Generic.KeyValuePair<string, string>("iz", "{bozuk") }, new long[0], "karar");

            Assert.Throws<ArgumentException>(() => KisaIz.Oku(baska));
            Assert.Throws<FormatException>(() => KisaIz.Oku(bozuk));
        }
    }
}
