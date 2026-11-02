using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Json;
using Yanki.Core.Tests.Simulation;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>Karar izinin kanonik (kompakt) JSON yazımı ve <c>karar_verildi</c> olayı (ADR-011).</summary>
    public class KararIziYaziciTests
    {
        // Ali, tick 1'de bir yardım (a: taban 2 × cesaret ölçütü) ve bir selam (b: taban 1) arasında; N=2.
        private static (KararIzi Iz, Yanki.Core.Simulation.Simulasyon Sim) Ornek()
        {
            var eylemler = KararFikstur.Yukle(
                KararFikstur.Eylem("a", "npc", "\"olcutler\": [ { \"alan\": \"kisilik(cesaret)\", \"egri\": { \"tur\": \"lojistik\", \"k\": 10, \"m\": 0.5 } } ], \"celisen_ozellikler\": [ \"durustluk\" ]", 2m),
                KararFikstur.Eylem("b", taban: 1.4m));
            var sim = KararFikstur.Sim(eylemler);
            var adaylar = new[] { new Aday(eylemler[0], "Veli"), new Aday(eylemler[1], string.Empty) };
            var elenen = new[] { new ElenenAday(eylemler[0], "Can", "on_kosul #1 tutmadi: ayni_mekanda(eylem.hedef) == true") };
            var sonuc = new UtilitySelector(new SecimAyarlari(2, 0.7m)).Sec(KararFikstur.Baglam(sim, "Ali"), adaylar, elenen);
            return (sonuc.Iz, sim);
        }

        private static JsonDegeri Oku(KararIzi iz) => JsonAyristirici.Ayristir(KararIziYazici.Yaz(iz));

        [Fact]
        public void Yazim_kompakttir_tek_satir_ve_bosluksuz()
        {
            var metin = KararIziYazici.Yaz(Ornek().Iz);

            var serbestMetinsiz = metin.Replace("on_kosul #1 tutmadi: ayni_mekanda(eylem.hedef) == true", string.Empty);

            Assert.DoesNotContain("\n", metin);
            Assert.DoesNotContain(": ", serbestMetinsiz);
            Assert.DoesNotContain(", ", serbestMetinsiz);
        }

        [Fact]
        public void Yazim_deterministiktir_ayni_iz_ayni_metin()
        {
            var iz = Ornek().Iz;

            Assert.Equal(KararIziYazici.Yaz(iz), KararIziYazici.Yaz(iz));
            Assert.Equal(KararIziYazici.Yaz(Ornek().Iz), KararIziYazici.Yaz(Ornek().Iz));
        }

        [Fact]
        public void Basit_iz_kesin_metinle_yazilir()
        {
            var eylemler = KararFikstur.Yukle(KararFikstur.Eylem("selam"));
            var sim = KararFikstur.Sim(eylemler);
            var iz = KararIzi.Basit(KararFikstur.Baglam(sim, "Ali"), new Aday(eylemler[0], string.Empty));

            Assert.Equal(
                "{\"npc\":\"Ali\",\"tick\":1,\"ayarlar\":{\"en_iyi_n\":3,\"esik\":0.7},\"adaylar\":[{\"eylem\":\"selam\",\"hedef\":\"\",\"durum\":\"puansiz\",\"sira\":1,\"havuz\":true}],"
                + "\"havuz\":[\"selam|\"],\"zar\":null,\"secilen\":{\"eylem\":\"selam\",\"hedef\":\"\",\"celisen_ozellikler\":[]},\"kaynak_olaylar\":[],\"kaynaksiz\":[]}",
                KararIziYazici.Yaz(iz));
        }

        [Fact]
        public void Iz_ust_duzey_alanlari_yazilir()
        {
            var iz = Ornek().Iz;
            var k = Oku(iz);

            Assert.Equal("Ali", k.Al("npc")!.MetinDeger);
            Assert.Equal(iz.Tick, (long)k.Al("tick")!.SayiDeger);
            Assert.Equal(2m, k.Al("ayarlar")!.Al("en_iyi_n")!.SayiDeger);
            Assert.Equal(0.7m, k.Al("ayarlar")!.Al("esik")!.SayiDeger);
            Assert.Equal(iz.Havuz.ToArray(), k.Al("havuz")!.Elemanlar.Select(e => e.MetinDeger).ToArray());
            Assert.Equal(JsonTuru.Sayi, k.Al("zar")!.Tur);
            Assert.Equal(iz.Zar, k.Al("zar")!.SayiDeger);
        }

        [Fact]
        public void Adaylar_siralidir_elenenler_neden_ile_gorunur()
        {
            var k = Oku(Ornek().Iz);

            var anahtarlar = k.Al("adaylar")!.Elemanlar.Select(a => a.Al("eylem")!.MetinDeger + "|" + a.Al("hedef")!.MetinDeger).ToArray();
            Assert.Equal(new[] { "a|Can", "a|Veli", "b|" }, anahtarlar);

            var elenen = k.Al("adaylar")!.Elemanlar[0];
            Assert.Equal("elendi", elenen.Al("durum")!.MetinDeger);
            Assert.Equal("on_kosul #1 tutmadi: ayni_mekanda(eylem.hedef) == true", elenen.Al("neden")!.MetinDeger);
            Assert.Null(elenen.Al("puan"));
        }

        [Fact]
        public void Puanlanan_adayin_butun_bilesenleri_yazilir()
        {
            var iz = Ornek().Iz;
            var puanli = iz.Adaylar.Single(a => a.Aday.Anahtar == "a|Veli");
            var j = Oku(iz).Al("adaylar")!.Elemanlar[1];

            Assert.Equal("puanlandi", j.Al("durum")!.MetinDeger);
            Assert.Equal(2m, j.Al("taban")!.SayiDeger);
            var olcut = Assert.Single(j.Al("olcutler")!.Elemanlar);
            Assert.Equal("kisilik(cesaret)", olcut.Al("alan")!.MetinDeger);
            Assert.Equal(puanli.Puan!.Olcutler[0].Girdi, olcut.Al("girdi")!.SayiDeger);
            Assert.Equal(puanli.Puan.Olcutler[0].Normal, olcut.Al("normal")!.SayiDeger);
            Assert.Equal(puanli.Puan.Olcutler[0].Cikti, olcut.Al("cikti")!.SayiDeger);
            Assert.Equal("lojistik", olcut.Al("egri")!.Al("tur")!.MetinDeger);
            Assert.Equal(10m, olcut.Al("egri")!.Al("k")!.SayiDeger);
            Assert.Equal(0.5m, olcut.Al("egri")!.Al("m")!.SayiDeger);
            Assert.Equal(puanli.Puan.KisilikCarpani, j.Al("kisilik")!.SayiDeger);
            Assert.Equal(puanli.Puan.FikirCarpani, j.Al("fikir")!.SayiDeger);
            Assert.Equal(puanli.Puan.RuhHaliCarpani, j.Al("ruh_hali")!.SayiDeger);
            Assert.Equal(puanli.Puan.Puan, j.Al("puan")!.SayiDeger);
            Assert.Equal(puanli.Sira, (int)j.Al("sira")!.SayiDeger);
            Assert.Equal(puanli.HavuzdaMi, j.Al("havuz")!.MantiksalDeger);
        }

        [Fact]
        public void Egri_parametreleri_turune_gore_yazilir()
        {
            var eylemler = KararFikstur.Yukle(
                KararFikstur.Eylem("d", ek: "\"olcutler\": [ { \"alan\": \"kisilik(cesaret)\" }, { \"alan\": \"kisilik(cesaret)\", \"egri\": { \"tur\": \"ustel\", \"k\": 3 } }, { \"alan\": \"kisilik(cesaret)\", \"egri\": { \"tur\": \"basamak\", \"esik\": 0.4 } } ]"));
            var sim = KararFikstur.Sim(eylemler);
            var iz = new UtilitySelector().Sec(KararFikstur.Baglam(sim, "Ali"), new[] { new Aday(eylemler[0], string.Empty) }, new ElenenAday[0]).Iz;

            var olcutler = Oku(iz).Al("adaylar")!.Elemanlar[0].Al("olcutler")!.Elemanlar;

            Assert.Equal("{\"tur\":\"dogrusal\"}", JsonYazici.YazKisa(olcutler[0].Al("egri")!));
            Assert.Equal("{\"tur\":\"ustel\",\"k\":3}", JsonYazici.YazKisa(olcutler[1].Al("egri")!));
            Assert.Equal("{\"tur\":\"basamak\",\"esik\":0.4}", JsonYazici.YazKisa(olcutler[2].Al("egri")!));
        }

        [Fact]
        public void Secilen_celisen_ozellikler_kaynaklar_ve_kaynaksizlar_yazilir()
        {
            var iz = Ornek().Iz;
            var k = Oku(iz);

            Assert.Equal(iz.Secilen!.Eylem.Id, k.Al("secilen")!.Al("eylem")!.MetinDeger);
            Assert.Equal(iz.Secilen.Hedef, k.Al("secilen")!.Al("hedef")!.MetinDeger);
            Assert.Equal(iz.CelisenOzellikler.ToArray(), k.Al("secilen")!.Al("celisen_ozellikler")!.Elemanlar.Select(e => e.MetinDeger).ToArray());
            Assert.Empty(k.Al("kaynak_olaylar")!.Elemanlar);
            Assert.Empty(k.Al("kaynaksiz")!.Elemanlar);
        }

        [Fact]
        public void Secilen_yoksa_secilen_null_ve_zar_null_yazilir()
        {
            var eylemler = KararFikstur.Yukle(KararFikstur.Eylem("z", ek: "\"olcutler\": [ { \"alan\": \"kisilik(cesaret)\", \"egri\": { \"tur\": \"basamak\", \"esik\": 0.9 } } ]"));
            var sim = KararFikstur.Sim(eylemler);
            var iz = new UtilitySelector().Sec(KararFikstur.Baglam(sim, "Ali"), new[] { new Aday(eylemler[0], string.Empty) }, new ElenenAday[0]).Iz;

            var k = Oku(iz);

            Assert.Equal(JsonTuru.Bos, k.Al("secilen")!.Tur);
            Assert.Equal(JsonTuru.Bos, k.Al("zar")!.Tur);
            Assert.Empty(k.Al("havuz")!.Elemanlar);
            Assert.Equal(0m, k.Al("adaylar")!.Elemanlar[0].Al("puan")!.SayiDeger);
        }

        // ---- olay ----

        [Fact]
        public void Karar_olayi_alanlari()
        {
            var (iz, _) = Ornek();

            var olay = KararOlayi.Yap(7, iz.Tick, 3, iz, "han");

            Assert.Equal(7, olay.Id);
            Assert.Equal(iz.Tick, olay.Tick);
            Assert.Equal(3, olay.Sira);
            Assert.Equal("karar_verildi", olay.Tur);
            Assert.Equal(YerlesikTurler.KararVerildi, olay.Tur);
            Assert.Equal("Ali", olay.Fail);
            Assert.Equal(string.Empty, olay.Hedef);
            Assert.Equal("han", olay.Mekan);
            Assert.Equal("karar", olay.KuralId);
            Assert.Equal(KararIziYazici.YazKisa(iz), olay.Deger("iz"));
            Assert.Equal(iz.Secilen!.Eylem.Id, olay.Deger("secilen_eylem"));
            Assert.Equal(iz.Secilen.Hedef, olay.Deger("secilen_hedef"));
        }

        [Fact]
        public void Karar_olayi_nedenleri_iz_kaynaklaridir()
        {
            var iz = new KararIzi("Ali", 4, new SecimAyarlari(), new AdayIzi[0], new string[0], null, null, new string[0], new long[] { 9, 3, 9 }, new string[0]);

            var olay = KararOlayi.Yap(12, 4, 0, iz, "han");

            Assert.Equal(new long[] { 3, 9 }, olay.NedenOlaylar.ToArray());
        }

        [Fact]
        public void Secilen_yoksa_olay_yuku_bos_secim_tasir()
        {
            var iz = new KararIzi("Ali", 4, new SecimAyarlari(), new AdayIzi[0], new string[0], null, null, new string[0], new long[0], new string[0]);

            var olay = KararOlayi.Yap(1, 4, 0, iz, "han");

            Assert.Equal(string.Empty, olay.Deger("secilen_eylem"));
            Assert.Equal(string.Empty, olay.Deger("secilen_hedef"));
        }

        [Fact]
        public void Olay_gunluge_eklenir_ve_dunyayi_zihni_degistirmez()
        {
            var (iz, sim) = Ornek();
            var hash = sim.DurumHash();
            var olay = KararOlayi.Yap(sim.Gunluk.SonrakiId, iz.Tick, sim.Gunluk.SonrakiSira(iz.Tick), iz, "han");

            sim.Gunluk.Ekle(olay);

            Assert.Equal(hash, sim.DurumHash());
            Assert.False(sim.Dunya.Uygula(olay));
            Assert.False(sim.Zihinler.Uygula(olay));
        }
    }
}
