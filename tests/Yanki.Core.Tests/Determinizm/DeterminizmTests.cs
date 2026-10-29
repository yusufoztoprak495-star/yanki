using System.Collections.Generic;
using System.Linq;
using Xunit;
using Yanki.Core.Events;
using Yanki.Core.Tests.World;
using Yanki.Core.World;

namespace Yanki.Core.Tests.Determinizm
{
    public class DeterminizmTests
    {
        private static readonly string[] Npcler = { "Ali", "Ayse", "Mehmet", "Zeynep" };
        private static readonly string[] Mekanlar = { "han", "pazar", "kale" };

        // Her tick'te her NPC, kendi anahtarlı akışından bir mekan seçip oraya taşınır. Sonuç, günlük + dünya.
        private static (OlayGunlugu Gunluk, Dunya Dunya) Simule(ulong seed, IEnumerable<string> npcSirasi, int tickSayisi)
        {
            var gunluk = new OlayGunlugu();
            var dunya = new Dunya();

            void Ekle(long tick, string tur, params KeyValuePair<string, string>[] yuk)
            {
                var olay = new Olay(gunluk.SonrakiId, tick, gunluk.SonrakiSira(tick), tur, "", "", "", yuk, new long[0], "sim");
                gunluk.Ekle(olay);
                dunya.Uygula(olay);
            }

            foreach (var m in Mekanlar)
            {
                Ekle(0, "varlik_eklendi", new KeyValuePair<string, string>("id", m), new KeyValuePair<string, string>("tur", "mekan"));
            }

            foreach (var n in Npcler)
            {
                Ekle(0, "varlik_eklendi", new KeyValuePair<string, string>("id", n), new KeyValuePair<string, string>("tur", "npc"));
            }

            // Hareket: tick 1.. başlar; npc sırası değişse de her NPC'nin kararı yalnızca kendi anahtarına bağlı.
            var sirali = npcSirasi.ToArray();
            for (var tick = 1; tick <= tickSayisi; tick++)
            {
                foreach (var npc in sirali)
                {
                    var rng = AnahtarliRng.Icin(seed, tick, npc, "hareket");
                    var hedef = Mekanlar[rng.Aralik(0, Mekanlar.Length)];
                    Ekle(tick, "gercek_eklendi", new KeyValuePair<string, string>("gercek", $"konum!({npc},{hedef})"));
                }
            }

            return (gunluk, dunya);
        }

        [Fact]
        public void Ayni_seed_ayni_girdi_ayni_hash()
        {
            var a = Simule(42UL, Npcler, 40);
            var b = Simule(42UL, Npcler, 40);

            Assert.Equal(a.Dunya.DurumHash(), b.Dunya.DurumHash());
            Assert.Equal(a.Gunluk.Sayi, b.Gunluk.Sayi);
        }

        [Fact]
        public void NPC_isleme_sirasi_degisse_de_son_durum_ayni()
        {
            // Anahtarlı RNG: bir NPC'nin kararı, başka NPC'lerin ne zaman çağrıldığına bağlı değildir.
            var duz = Simule(42UL, Npcler, 40);
            var ters = Simule(42UL, Npcler.Reverse(), 40);

            Assert.Equal(duz.Dunya.DurumHash(), ters.Dunya.DurumHash());
        }

        [Fact]
        public void Farkli_seed_farkli_hash()
        {
            Assert.NotEqual(Simule(1UL, Npcler, 40).Dunya.DurumHash(), Simule(2UL, Npcler, 40).Dunya.DurumHash());
        }

        [Fact]
        public void Gunlugu_bastan_oynatmak_ayni_hashi_verir()
        {
            var canli = Simule(7UL, Npcler, 30);

            var yeniden = new Dunya();
            foreach (var olay in canli.Gunluk.Olaylar)
            {
                yeniden.Uygula(olay);
            }

            Assert.Equal(canli.Dunya.DurumHash(), yeniden.DurumHash());
        }

        [Fact]
        public void Farkli_nesne_orneklerinden_kurulan_olay_dizisi_ayni_hash()
        {
            // İki bağımsız simülasyon = iki ayrı Olay/OlayGunlugu/Dunya nesne kümesi.
            var a = Simule(99UL, Npcler, 25);
            var b = Simule(99UL, Npcler, 25);

            Assert.NotSame(a.Gunluk, b.Gunluk);
            Assert.NotSame(a.Gunluk[1], b.Gunluk[1]);
            Assert.Equal(a.Dunya.DurumHash(), b.Dunya.DurumHash());
        }

        [Fact]
        public void Rng_ayni_anahtar_ayni_degerler_cagri_sirasi_degisince_degismez()
        {
            var ilkSira = new List<ulong>();
            var a = AnahtarliRng.Icin(5UL, 1, "Ali");
            var b = AnahtarliRng.Icin(5UL, 1, "Ayse");
            ilkSira.Add(a.SonrakiUlong());
            ilkSira.Add(b.SonrakiUlong());
            ilkSira.Add(a.SonrakiUlong());

            var a2 = AnahtarliRng.Icin(5UL, 1, "Ali");
            var b2 = AnahtarliRng.Icin(5UL, 1, "Ayse");
            var ikinciSira = new List<ulong>();
            var bIlk = b2.SonrakiUlong();
            var aIlk = a2.SonrakiUlong();
            var aIkinci = a2.SonrakiUlong();
            ikinciSira.Add(aIlk);
            ikinciSira.Add(bIlk);
            ikinciSira.Add(aIkinci);

            Assert.Equal(ilkSira, ikinciSira);
        }

        [Fact]
        public void Sabit_dunya_ayni_olaylarla_her_seferinde_ayni_hash()
        {
            var hashler = Enumerable.Range(0, 5).Select(_ => OlayUretici.SabitDunya().DurumHash()).Distinct().ToArray();

            Assert.Single(hashler);
        }
    }
}
