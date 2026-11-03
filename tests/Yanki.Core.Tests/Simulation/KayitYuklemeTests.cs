using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Decision;
using Yanki.Core.Rules;
using Yanki.Core.Simulation;
using Yanki.Core.Tests.Decision;

namespace Yanki.Core.Tests.Simulation
{
    /// <summary>
    /// 7. hafta (F): kayıt sürüm 2 (<c>gunluk_hash</c>, <c>senaryo_hash</c>, <c>bekleyen_eylemler</c>) ve yükleme. Yükleme = baştan oynatma
    /// (<see cref="OturumOynatici.Oynat"/> tek yoldur). Fikstür: SimFikstur senaryosu, yardım kuralı ve iki eşdeğer hedefsiz eylem.
    /// </summary>
    public class KayitYuklemeTests
    {
        private static string[] Eylemler => new[] { KararFikstur.Eylem("selam", "yok", string.Empty, 1m), KararFikstur.Eylem("merhaba", "yok", string.Empty, 0.5m) };

        private static Simulasyon Kur(IActionSelector? secici = null, ulong seed = 42) =>
            Simulasyon.Olustur(SimFikstur.Senaryo(), SimFikstur.Kurallar(SimFikstur.YardimKurali), seed, eylemler: KararFikstur.Yukle(Eylemler), secici: secici);

        private static string Ozet(Simulasyon s) =>
            string.Join("\n", s.Gunluk.Olaylar.Select(o => o.Id + "|" + o.Tick + "|" + o.Sira + "|" + o.Tur + "|" + o.Fail + "|" + o.Hedef + "|" + o.KuralId + "|"
                + string.Join(",", o.Yuk.Select(kv => kv.Key + "=" + kv.Value)) + "|" + string.Join(",", o.NedenOlaylar)));

        private static PlayerAction Yardim(string hedef = "Ali") => new PlayerAction("yardim_etti", hedef);

        private static OturumKaydi Gidis(OturumKaydi k) => OturumKaydi.Oku(OturumKaydi.Yaz(k), "kayit.json").Kayit!;

        [Fact]
        public void N_tick_kos_kaydet_yukle_M_tick_kos_ile_N_arti_M_tick_ayni_sonucu_verir()
        {
            var kesintisiz = Kur();
            kesintisiz.Tick(2);
            kesintisiz.Submit(Yardim());
            kesintisiz.Tick(3);
            kesintisiz.Submit(Yardim("Veli"));
            kesintisiz.Tick(5);

            var ilk = Kur();
            ilk.Tick(2);
            ilk.Submit(Yardim());
            ilk.Tick(3);
            var kayit = Gidis(OturumKaydi.Uret(ilk, "senaryo.json", "00000000000000aa"));

            var yuklenen = Kur();
            var sonuc = OturumOynatici.Oynat(yuklenen, kayit, "00000000000000aa");
            Assert.True(sonuc.HashTutuyor);
            Assert.True(sonuc.GunlukHashTutuyor);
            yuklenen.Submit(Yardim("Veli"));
            yuklenen.Tick(5);

            Assert.Equal(Ozet(kesintisiz), Ozet(yuklenen));                       // olay sırası, kimlikler, yük, nedenler
            Assert.Equal(kesintisiz.SonHash, yuklenen.SonHash);
            Assert.Equal(GunlukHashu.Hesapla(kesintisiz.Gunluk), GunlukHashu.Hesapla(yuklenen.Gunluk));
        }

        [Fact]
        public void Bekleyen_eylemler_kaydedilir_ve_yuklemede_geri_Submit_edilir()
        {
            var kesintisiz = Kur();
            kesintisiz.Tick(4);
            kesintisiz.Submit(Yardim());
            kesintisiz.Submit(Yardim("Veli"));
            kesintisiz.Tick(4);

            var ilk = Kur();
            ilk.Tick(4);
            ilk.Submit(Yardim());
            ilk.Submit(Yardim("Veli"));                                              // Tick görmedi: kayıtta bekliyor
            var kayit = Gidis(OturumKaydi.Uret(ilk, "senaryo.json"));
            Assert.Equal(new[] { "Ali", "Veli" }, kayit.BekleyenEylemler.Select(e => e.Hedef).ToArray());
            Assert.Empty(kayit.Eylemler);
            Assert.Contains("bekleyen_eylemler", OturumKaydi.Yaz(kayit));

            var yuklenen = Kur();
            OturumOynatici.Oynat(yuklenen, kayit);
            Assert.Equal(2, yuklenen.BekleyenEylemler.Count);
            yuklenen.Tick(4);

            Assert.Equal(Ozet(kesintisiz), Ozet(yuklenen));
            Assert.Equal(kesintisiz.SonHash, yuklenen.SonHash);
            Assert.Equal(GunlukHashu.Hesapla(kesintisiz.Gunluk), GunlukHashu.Hesapla(yuklenen.Gunluk));
        }

        [Fact]
        public void Senaryo_degistiyse_yukleme_acik_hatayla_durur_hash_yoksa_denetim_atlanir()
        {
            var ilk = Kur();
            ilk.Tick(2);
            var kayit = Gidis(OturumKaydi.Uret(ilk, "senaryo.json", "00000000000000aa"));

            var hata = Assert.Throws<SenaryoDegistiHatasi>(() => OturumOynatici.Oynat(Kur(), kayit, "00000000000000bb"));
            Assert.Contains("00000000000000aa", hata.Message);
            Assert.Contains("00000000000000bb", hata.Message);

            var hashsiz = Gidis(OturumKaydi.Uret(ilk, "senaryo.json"));              // senaryo_hash yok (CLI vermedi)
            Assert.Null(hashsiz.SenaryoHash);
            OturumOynatici.Oynat(Kur(), hashsiz, "00000000000000bb");                 // atlanır
        }

        [Fact]
        public void Surum_1_dosya_okunur_gunluk_denetimi_atlanir_ve_ayni_surumle_yazilir()
        {
            var ilk = Kur();
            ilk.Tick(3);
            var json = "{ \"surum\": 1, \"seed\": 42, \"senaryo\": \"s.json\", \"tick_sayisi\": 3, \"eylemler\": [], \"son_hash\": \"" + ilk.SonHash + "\" }";

            var okuma = OturumKaydi.Oku(json, "eski.json");
            Assert.True(okuma.Basarili, string.Join("\n", okuma.Hatalar));
            var kayit = okuma.Kayit!;
            Assert.Equal(1, kayit.Surum);
            Assert.Null(kayit.GunlukHash);
            Assert.Empty(kayit.BekleyenEylemler);

            var sonuc = OturumOynatici.Oynat(Kur(), kayit);
            Assert.True(sonuc.HashTutuyor);
            Assert.True(sonuc.GunlukHashTutuyor);
            Assert.Contains("\"surum\": 1", OturumKaydi.Yaz(kayit));
        }

        [Fact]
        public void Surum_1_dosyada_surum_2_alanlari_reddedilir()
        {
            var json = "{ \"surum\": 1, \"seed\": 1, \"senaryo\": \"s.json\", \"tick_sayisi\": 1, \"eylemler\": [], \"son_hash\": \"0000000000000000\", \"gunluk_hash\": \"0000000000000000\" }";
            Assert.False(OturumKaydi.Oku(json, "x.json").Basarili);
        }

        [Fact]
        public void Bozuk_gunluk_hash_replay_hatasi_verir_son_hash_tutsa_bile()
        {
            var ilk = Kur();
            ilk.Tick(3);
            var gercek = OturumKaydi.Uret(ilk, "senaryo.json", "00000000000000aa");
            var bozuk = new OturumKaydi(gercek.Seed, gercek.Senaryo, gercek.TickSayisi, gercek.Eylemler, gercek.SonHash, "deadbeefdeadbeef", gercek.SenaryoHash, gercek.BekleyenEylemler);

            var sonuc = OturumOynatici.Oynat(Kur(), bozuk);

            Assert.True(sonuc.HashTutuyor);
            Assert.False(sonuc.GunlukHashTutuyor);
            Assert.Equal("deadbeefdeadbeef", sonuc.BeklenenGunlukHash);
            Assert.Equal(gercek.GunlukHash, sonuc.GercekGunlukHash);
        }

        [Fact]
        public void Secici_degisince_durum_ayni_kalsa_bile_gunluk_hash_replay_hatasi_verir()
        {
            Simulasyon TekEylemli(IActionSelector? secici) =>
                Simulasyon.Olustur(
                    SimFikstur.Senaryo(), SimFikstur.Kurallar(SimFikstur.YardimKurali), 42,
                    eylemler: KararFikstur.Yukle(KararFikstur.Eylem("selam", "yok", string.Empty, 1m)), secici: secici);

            var varsayilan = TekEylemli(null);                                      // UtilitySelector: tek aday, tam karar izi
            varsayilan.Tick(3);
            var kayit = Gidis(OturumKaydi.Uret(varsayilan, "senaryo.json"));

            // Aynı eylemi seçen ama farklı iz yazan başka bir seçici: durum aynı kalır, karar_verildi olayları farklı olur.
            var baska = new SabitSecici((npc, tick) => "selam|");
            var sonuc = OturumOynatici.Oynat(TekEylemli(baska), kayit);

            Assert.True(sonuc.HashTutuyor);                // durum hash'i bunu yakalayamaz
            Assert.False(sonuc.GunlukHashTutuyor);         // gunluk_hash yakalar
        }

        [Fact]
        public void Senaryo_hash_satir_sonundan_bagimsizdir_icerik_ve_sira_degisince_degisir()
        {
            var lf = SenaryoHashu.Hesapla(new[] { "a\nb\n", "c" });
            Assert.Equal(lf, SenaryoHashu.Hesapla(new[] { "a\r\nb\r\n", "c" }));
            Assert.NotEqual(lf, SenaryoHashu.Hesapla(new[] { "a\nb\n", "d" }));
            Assert.NotEqual(lf, SenaryoHashu.Hesapla(new[] { "c", "a\nb\n" }));
            Assert.NotEqual(SenaryoHashu.Hesapla(new[] { "ab", "c" }), SenaryoHashu.Hesapla(new[] { "a", "bc" }));
            Assert.Equal(16, lf.Length);
        }

        [Fact]
        public void Gunluk_hash_deterministiktir_ve_her_olayi_kapsar()
        {
            var a = Kur();
            a.Tick(3);
            var b = Kur();
            b.Tick(3);
            Assert.Equal(GunlukHashu.Hesapla(a.Gunluk), GunlukHashu.Hesapla(b.Gunluk));

            b.Tick(1);
            Assert.NotEqual(GunlukHashu.Hesapla(a.Gunluk), GunlukHashu.Hesapla(b.Gunluk));
        }

        [Fact]
        public void Bekleyen_eylem_kaydinda_tick_alani_yazilmaz_okunurken_kabul_edilmez()
        {
            var json = "{ \"surum\": 2, \"seed\": 1, \"senaryo\": \"s.json\", \"tick_sayisi\": 1, \"eylemler\": [], \"son_hash\": \"0000000000000000\", "
                + "\"bekleyen_eylemler\": [ { \"tick\": 1, \"tur\": \"a\" } ] }";
            Assert.False(OturumKaydi.Oku(json, "x.json").Basarili);

            var ok = json.Replace("{ \"tick\": 1, \"tur\": \"a\" }", "{ \"tur\": \"a\", \"hedef\": \"Ali\" }");
            var kayit = OturumKaydi.Oku(ok, "x.json").Kayit!;
            Assert.Equal(("a", "Ali"), (kayit.BekleyenEylemler[0].Tur, kayit.BekleyenEylemler[0].Hedef));
        }
    }
}
