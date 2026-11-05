using Yanki.Core.Simulation;

namespace Yanki.Dialogue
{
    /// <summary>Konuşanın hatırladığı, şablonun gönderme yapacağı gerçek bir olay.</summary>
    internal sealed class HafizaSonucu
    {
        public HafizaSonucu(long olayId, string tur, string fail, long tick)
        {
            OlayId = olayId;
            Tur = tur;
            Fail = fail;
            Tick = tick;
        }

        public long OlayId { get; }

        public string Tur { get; }

        public string Fail { get; }

        public long Tick { get; }
    }

    /// <summary>
    /// Hafıza göndermeleri (E kararı): eşleşen kayıt KONUŞANIN <c>Hafiza(tick)</c> listesinden seçilir, dünya günlüğünden değil. Silik kayıt görünmez; başkasının hafızası okunmaz; konuşanın
    /// görmediği/duymadığı olaya gönderme yapılmaz. Birden çok kayıt varsa en yeni (olay tick'i), eşitse en düşük olay kimliği. Niyeti doğuran olayın KENDİSİ gönderme sayılmaz (geçmişe gönderme).
    /// </summary>
    internal static class HafizaGondermesi
    {
        public static string FailCoz(string fail, Niyet niyet, Simulasyon sim)
        {
            switch (fail)
            {
                case "oyuncu": return sim.OyuncuId;
                case "dinleyen": return niyet.Dinleyen;
                case "konusan":
                case "ben": return niyet.Konusan;
                default: return fail;
            }
        }

        public static HafizaSonucu? Bul(Simulasyon sim, Niyet niyet, HafizaBildirimi bildirim)
        {
            var fail = FailCoz(bildirim.Fail, niyet, sim);
            var simdi = sim.SimdikiTick;
            HafizaSonucu? en = null;
            foreach (var kayit in sim.Zihinler[niyet.Konusan].Hafiza(simdi))
            {
                if (kayit.OlayId == niyet.OlayId || !sim.Gunluk.Var(kayit.OlayId))
                {
                    continue;
                }

                var olay = sim.Gunluk[kayit.OlayId];
                if (olay.Tur != bildirim.Tur || olay.Fail != fail || simdi - olay.Tick > bildirim.SonNTick)
                {
                    continue;
                }

                if (en == null || olay.Tick > en.Tick || (olay.Tick == en.Tick && olay.Id < en.OlayId))
                {
                    en = new HafizaSonucu(olay.Id, olay.Tur, olay.Fail, olay.Tick);
                }
            }

            return en;
        }
    }
}
