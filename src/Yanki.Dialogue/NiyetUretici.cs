using Yanki.Core.Decision;
using Yanki.Core.Events;
using Yanki.Core.Mind;
using Yanki.Core.Simulation;
using Yanki.Core.World;

namespace Yanki.Dialogue
{
    /// <summary>
    /// Olaydan niyet üretir (A ve B kararları). Yalnızca olay türü, fail/hedef, <c>onerme</c> yükü ve konuşanın zihninden fikir bandı ile baskın duyguyu okur;
    /// <c>KuralId</c>, <c>NedenOlaylar</c>, <c>kok</c> ya da başka yük alanını OKUMAZ (yalan gizliliği). Eşlemesi olmayan olay niyet üretmez.
    /// </summary>
    internal static class NiyetUretici
    {
        public static Niyet? Uret(Olay olay, Simulasyon sim, SablonDosyasi dosya, DiyalogAyarlari ayar)
        {
            var kaynak = olay.Fail == sim.OyuncuId ? "oyuncu" : "npc";
            var kural = dosya.Esleme.Bul(olay.Tur, kaynak);
            if (kural == null)
            {
                return null;
            }

            var konusan = kural.KonusanFail ? olay.Fail : olay.Hedef;
            var dinleyen = kural.KonusanFail ? olay.Hedef : olay.Fail;
            if (kural.Dinleyensiz)
            {
                if (!sim.Zihinler.Var(konusan))
                {
                    return null;
                }

                return Olustur(sim, ayar, konusan, string.Empty, kural.Niyet, string.Empty, olay.Id, olay.Tick, false, olay.Mekan);
            }

            if (!sim.Zihinler.Var(konusan) || !KisiMi(sim, dinleyen) || string.Equals(konusan, dinleyen, System.StringComparison.Ordinal))
            {
                return null;   // yalnızca NPC'ler konuşur; dinleyen bir kişi (NPC ya da oyuncu) olmalı (mekan hedefli olaylar satır üretmez)
            }

            var konu = olay.Tur == YerlesikTurler.Anlatti ? olay.Deger("onerme") : string.Empty;
            return Olustur(sim, ayar, konusan, dinleyen, kural.Niyet, konu, olay.Id, olay.Tick, false, string.Empty);
        }

        public static Niyet Followup(Niyet ana, string konusan, string concept, Simulasyon sim, DiyalogAyarlari ayar) =>
            Olustur(sim, ayar, konusan, ana.Konusan, concept, ana.Konu, ana.OlayId, ana.Tick, true, string.Empty);

        public static bool KisiMi(Simulasyon sim, string kimlik)
        {
            if (string.IsNullOrEmpty(kimlik))
            {
                return false;
            }

            var tur = sim.Dunya.TurOf(kimlik);
            return tur == VarlikTuru.Npc || tur == VarlikTuru.Oyuncu;
        }

        private static Niyet Olustur(Simulasyon sim, DiyalogAyarlari ayar, string konusan, string dinleyen, string ad, string konu, long olayId, long tick, bool followup, string mekan)
        {
            var zihin = sim.Zihinler[konusan];
            var simdi = sim.SimdikiTick;
            var bant = dinleyen.Length == 0 ? "yok" : zihin.FikirBandi(dinleyen, simdi);
            var duygu = zihin.Duygu != null && zihin.BaskinDuyguSiddeti(simdi) > 0m ? zihin.Duygu.Tur : "yok";
            var ton = duygu == "yok" ? ayar.VarsayilanTon : ayar.TonSec(duygu, zihin.BaskinDuyguSiddeti(simdi));
            return new Niyet(konusan, dinleyen, ad, konu, ton, bant, new NiyetGerekcesi(bant, duygu), olayId, tick, followup, mekan);
        }
    }
}
