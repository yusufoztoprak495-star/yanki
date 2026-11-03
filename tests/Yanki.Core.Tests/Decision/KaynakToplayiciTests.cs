using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Criteria;
using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.World;

namespace Yanki.Core.Tests.Decision
{
    /// <summary>
    /// Karar izinin kaynakları: seçilen adayın girdilerine katkı veren zihin olaylarının kimlikleri. Kaynağı bulunamayan girdi
    /// uydurulmaz, <c>Kaynaksiz</c> listesinde raporlanır. Gerçek <see cref="Zihin"/> ve <see cref="Dunya"/> nesneleriyle sınanır.
    /// </summary>
    public class KaynakToplayiciTests
    {
        private const long Tick = 10;

        private sealed class Duzen
        {
            public Duzen()
            {
                Zihin = new Zihin("Ali", new Kisilik(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, 0.5m));
                Depo = new ZihinDeposu();
                Depo.Ekle(Zihin);
            }

            public Zihin Zihin { get; }

            public ZihinDeposu Depo { get; }

            public Dunya Dunya { get; } = new Dunya();

            public OlayGunlugu Gunluk { get; } = new OlayGunlugu();

            public void Uygula(Olay olay) => Zihin.Uygula(olay);

            public KararBaglami Baglam() =>
                new KararBaglami("Ali", "oyuncu", 1, Tick, Dunya, Zihin, Gunluk, new ZihinSorguCozucusu(Dunya, Depo, Gunluk, Tick));
        }

        private static (IReadOnlyList<long> Olaylar, IReadOnlyList<string> Kaynaksiz) Topla(Duzen d, string eylemJson, string hedef = "")
        {
            var eylem = KararFikstur.Yukle(eylemJson).Single();
            var sonuc = KaynakToplayici.Topla(d.Baglam(), new Aday(eylem, hedef));
            return (sonuc.Olaylar, sonuc.Kaynaksiz);
        }

        private static string Olcutlu(string alan, string hedef = "yok", string ek = "") =>
            KararFikstur.Eylem("e", hedef, "\"olcutler\": [ { \"alan\": \"" + alan + "\" } ]" + (ek.Length > 0 ? ", " + ek : string.Empty));

        private static string Kosullu(string alan, string op, string deger, string hedef = "yok") =>
            KararFikstur.Eylem("e", hedef, "\"on_kosullar\": [ { \"alan\": \"" + alan + "\", \"op\": \"" + op + "\", \"deger\": " + deger + " } ]");

        private static Olay Modifier(long id, string hedef, string tur, decimal deger, long sure = 100, long tick = 5) =>
            ZihinOlaylari.ModifierEklendi(id, tick, 0, "Ali", hedef, tur, deger, sure, new long[0], "kural");

        // ---- dünya sorguları (7. hafta, I-b) ----

        [Fact]
        public void Dunya_sorgulari_kaynak_olay_gostermez_kaynaksiz_olarak_raporlanir()
        {
            var d = new Duzen();
            var (olaylar, kaynaksiz) = Topla(d, Kosullu("ayni_mekanda(eylem.hedef)", "==", "true", "npc"), "Can");

            Assert.Empty(olaylar);                                           // uydurulmaz
            var not = Assert.Single(kaynaksiz);
            Assert.Contains("ayni_mekanda", not);
            Assert.Contains("dünya", not);

        }

        // ---- fikir ----

        [Fact]
        public void Fikir_aktif_modifierlarin_neden_olaylarini_gosterir()
        {
            var d = new Duzen();
            d.Uygula(Modifier(7, "Can", "minnet", 15));
            d.Uygula(Modifier(9, "Can", "hakaret", -5));
            d.Uygula(Modifier(11, "Veli", "minnet", 15));

            var (olaylar, kaynaksiz) = Topla(d, Olcutlu("fikir(eylem.hedef)", "npc"), "Can");

            Assert.Equal(new long[] { 7, 9 }, olaylar.ToArray());
            Assert.Empty(kaynaksiz);
        }

        [Fact]
        public void Suresi_dolmus_modifier_kaynak_degildir()
        {
            var d = new Duzen();
            d.Uygula(Modifier(7, "Can", "eski", 15, sure: 3, tick: 1)); // [1, 4) → tick 10'da bitti
            d.Uygula(Modifier(9, "Can", "yeni", 15));

            var (olaylar, _) = Topla(d, Olcutlu("fikir(eylem.hedef)", "npc"), "Can");

            Assert.Equal(new long[] { 9 }, olaylar.ToArray());
        }

        [Fact]
        public void Eksen_degeri_son_degisim_olayiyla_gosterilir_ve_birikim_kaynaksiz_olarak_raporlanir()
        {
            var d = new Duzen();
            d.Uygula(ZihinOlaylari.EksenDegisti(4, 2, 0, "Ali", "Can", "guven", 10m, new long[0], "kural"));
            d.Uygula(ZihinOlaylari.EksenDegisti(6, 3, 0, "Ali", "Can", "guven", 5m, new long[0], "kural"));

            var (olaylar, kaynaksiz) = Topla(d, Olcutlu("fikir(eylem.hedef)", "npc"), "Can");

            Assert.Equal(new long[] { 6 }, olaylar.ToArray());
            var not = Assert.Single(kaynaksiz);
            Assert.Contains("eksen", not);
            Assert.Contains("guven", not);
            Assert.Contains("Can", not);
            Assert.Contains("#6", not);
        }

        [Fact]
        public void Degismemis_eksen_kaynak_ya_da_rapor_uretmez()
        {
            var d = new Duzen();

            var (olaylar, kaynaksiz) = Topla(d, Olcutlu("fikir(eylem.hedef)", "npc"), "Can");

            Assert.Empty(olaylar);
            Assert.Empty(kaynaksiz);
        }

        [Fact]
        public void Eksen_sorgusu_yalniz_o_ekseni_gosterir()
        {
            var d = new Duzen();
            d.Uygula(ZihinOlaylari.EksenDegisti(4, 2, 0, "Ali", "Can", "guven", 10m, new long[0], "kural"));
            d.Uygula(ZihinOlaylari.EksenDegisti(6, 3, 0, "Ali", "Can", "korku", 5m, new long[0], "kural"));

            var (olaylar, _) = Topla(d, Olcutlu("eksen(eylem.hedef, korku)", "npc"), "Can");

            Assert.Equal(new long[] { 6 }, olaylar.ToArray());
        }

        [Fact]
        public void Modifier_var_yalniz_o_turdeki_aktif_modifierlari_gosterir()
        {
            var d = new Duzen();
            d.Uygula(Modifier(7, "Can", "minnet", 15));
            d.Uygula(Modifier(9, "Can", "hakaret", -5));

            var (olaylar, _) = Topla(d, Olcutlu("modifier_var(eylem.hedef, minnet)", "npc"), "Can");

            Assert.Equal(new long[] { 7 }, olaylar.ToArray());
        }

        [Fact]
        public void On_kosul_sorgulari_da_kaynak_toplar()
        {
            var d = new Duzen();
            d.Uygula(Modifier(7, "Can", "minnet", 15));

            var (olaylar, _) = Topla(d, Kosullu("modifier_var(eylem.hedef, minnet)", "==", "true", "npc"), "Can");

            Assert.Equal(new long[] { 7 }, olaylar.ToArray());
        }

        [Fact]
        public void Fikir_carpani_hedefe_karsi_modifierlari_kaynak_gosterir()
        {
            var d = new Duzen();
            d.Uygula(Modifier(7, "Can", "minnet", 15));
            d.Uygula(Modifier(9, "Veli", "minnet", 15));

            var (olaylar, _) = Topla(d, KararFikstur.Eylem("e", "npc", "\"fikir_carpani\": { \"yon\": \"artan\" }"), "Can");

            Assert.Equal(new long[] { 7 }, olaylar.ToArray());
        }

        [Fact]
        public void Ayni_olay_iki_girdiden_gelse_de_bir_kez_ve_artan_sirada_listelenir()
        {
            var d = new Duzen();
            d.Uygula(Modifier(9, "Can", "minnet", 15));
            d.Uygula(Modifier(3, "Can", "hakaret", -5));
            var json = KararFikstur.Eylem("e", "npc",
                "\"olcutler\": [ { \"alan\": \"fikir(eylem.hedef)\" }, { \"alan\": \"modifier_var(eylem.hedef, minnet)\" } ], \"fikir_carpani\": { \"yon\": \"artan\" }");

            var (olaylar, _) = Topla(d, json, "Can");

            Assert.Equal(new long[] { 3, 9 }, olaylar.ToArray());
        }

        // ---- ruh hali ----

        [Fact]
        public void Duygu_sorgusu_baskin_duygunun_neden_olayini_gosterir()
        {
            var d = new Duzen();
            d.Uygula(ZihinOlaylari.DuyguDegisti(8, 9, 0, "Ali", "korku", 0.8m, "Can", new long[0], "kural"));

            Assert.Equal(new long[] { 8 }, Topla(d, Olcutlu("duygu(korku)")).Olaylar.ToArray());
            Assert.Empty(Topla(d, Olcutlu("duygu(ofke)")).Olaylar);
            Assert.Equal(new long[] { 8 }, Topla(d, Kosullu("duygu_hedefi()", "==", "\"Can\"")).Olaylar.ToArray());
        }

        [Fact]
        public void Ruh_hali_carpani_listelenen_baskin_duygunun_nedenini_gosterir()
        {
            var d = new Duzen();
            d.Uygula(ZihinOlaylari.DuyguDegisti(8, 9, 0, "Ali", "korku", 0.8m, "Can", new long[0], "kural"));

            var ilgili = KararFikstur.Eylem("e", "yok", "\"ruh_hali_carpani\": { \"duygular\": [ { \"duygu\": \"korku\" } ] }");
            var ilgisiz = KararFikstur.Eylem("e", "yok", "\"ruh_hali_carpani\": { \"duygular\": [ { \"duygu\": \"ofke\" } ] }");

            Assert.Equal(new long[] { 8 }, Topla(d, ilgili).Olaylar.ToArray());
            Assert.Empty(Topla(d, ilgisiz).Olaylar);
        }

        // ---- inanç ----

        private static Olay Kanit(long id, string onerme, string tur, string kaynak) =>
            ZihinOlaylari.KanitEklendi(id, 2, 0, "Ali", onerme, tur, kaynak, 0.8m, new long[0], "algi");

        [Fact]
        public void Inanc_sorgusu_desene_uyan_inanilan_onermelerin_kanit_olaylarini_gosterir()
        {
            var d = new Duzen();
            d.Uygula(Kanit(20, "borclu(Mehmet,Ayse,500)", "gordu", "Ali"));
            d.Uygula(Kanit(21, "borclu(Mehmet,Ayse,500)", "duydu", "Veli"));
            d.Uygula(Kanit(22, "borclu(Can,Ayse,100)", "gordu", "Ali"));

            var (olaylar, _) = Topla(d, Olcutlu("inanc(borclu(Mehmet, *, *))"));

            Assert.Equal(new long[] { 20, 21 }, olaylar.ToArray());
        }

        [Fact]
        public void Kaynak_sorgusu_yalniz_o_kisiden_gelen_kanitlari_gosterir()
        {
            var d = new Duzen();
            d.Uygula(Kanit(20, "borclu(Mehmet,Ayse,500)", "gordu", "Ali"));
            d.Uygula(Kanit(21, "borclu(Mehmet,Ayse,500)", "duydu", "Veli"));

            var (olaylar, _) = Topla(d, Olcutlu("kaynak(borclu(Mehmet, *, *), Veli)"));

            Assert.Equal(new long[] { 21 }, olaylar.ToArray());
        }

        // ---- hafıza ----

        [Fact]
        public void Hafiza_var_hatirlanan_eslesen_olayi_gosterir()
        {
            var d = new Duzen();
            d.Gunluk.Ekle(new Olay(1, 8, 0, "yardim_oldu", "Can", string.Empty, "han", new KeyValuePair<string, string>[0], new long[0], "x"));
            d.Gunluk.Ekle(new Olay(2, 9, 0, "baska_oldu", "Can", string.Empty, "han", new KeyValuePair<string, string>[0], new long[0], "x"));
            d.Uygula(ZihinOlaylari.HafizaEklendi(3, 9, 0, "Ali", 1, 0.9m, new long[] { 1 }, "algi"));
            d.Uygula(ZihinOlaylari.HafizaEklendi(4, 9, 1, "Ali", 2, 0.9m, new long[] { 2 }, "algi"));

            var (olaylar, _) = Topla(d, Olcutlu("hafiza_var(yardim_oldu, Can, 24)"));

            Assert.Equal(new long[] { 1 }, olaylar.ToArray());
        }

        [Fact]
        public void Hatirlanmayan_olay_kaynak_degildir()
        {
            var d = new Duzen();

            Assert.Empty(Topla(d, Olcutlu("hafiza_var(yardim_oldu, Can, 24)")).Olaylar);
        }

        // ---- stres ve kaynağı olmayanlar ----

        [Fact]
        public void Stres_sifirdan_buyukse_kaynaksiz_olarak_raporlanir()
        {
            var d = new Duzen();
            d.Uygula(ZihinOlaylari.StresDegisti(5, 9, 0, "Ali", 30m, new long[0], "kural"));

            var (olaylar, kaynaksiz) = Topla(d, Olcutlu("stres()"));

            Assert.Empty(olaylar);
            Assert.Contains(kaynaksiz, k => k.Contains("stres"));
        }

        [Fact]
        public void Stres_sifirsa_rapor_yoktur() =>
            Assert.Empty(Topla(new Duzen(), Olcutlu("stres()")).Kaynaksiz);

        [Fact]
        public void Kisilik_ve_dunya_sorgulari_zihin_olayi_gerektirmez()
        {
            var d = new Duzen();

            var (olaylar, kaynaksiz) = Topla(d, Olcutlu("kisilik(cesaret)"));

            Assert.Empty(olaylar);
            Assert.Empty(kaynaksiz);
        }

        [Fact]
        public void Kaynaksiz_notlar_tekrarsiz_ve_ordinal_siralidir()
        {
            var d = new Duzen();
            d.Uygula(ZihinOlaylari.StresDegisti(5, 9, 0, "Ali", 30m, new long[0], "kural"));
            var json = KararFikstur.Eylem("e", "yok", "\"olcutler\": [ { \"alan\": \"stres()\" }, { \"alan\": \"stres()\", \"max\": 50 } ]");

            var (_, kaynaksiz) = Topla(d, json);

            Assert.Single(kaynaksiz);
        }
    }
}
