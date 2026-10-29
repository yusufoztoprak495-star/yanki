using Yanki.Core.Events;
using Yanki.Core.Mind;

namespace Yanki.Core.Tests.Mind
{
    /// <summary>Zihin testlerinde zihin olaylarını (Id, Sira otomatik) kurar. <see cref="Tick"/> elle ilerletilir.</summary>
    internal sealed class ZihinOlayUretici
    {
        private long _sonrakiId = 1;
        private int _sira;
        private long _sonTick;

        public long Tick { get; set; }

        private (long Id, int Sira) Sonraki()
        {
            if (Tick != _sonTick)
            {
                _sonTick = Tick;
                _sira = 0;
            }

            return (_sonrakiId++, _sira++);
        }

        public Olay Modifier(string npc, string hedef, string tur, decimal deger, long sure)
        {
            var (id, sira) = Sonraki();
            return ZihinOlaylari.ModifierEklendi(id, Tick, sira, npc, hedef, tur, deger, sure, new long[0], "test");
        }

        public Olay Eksen(string npc, string hedef, string eksen, decimal delta)
        {
            var (id, sira) = Sonraki();
            return ZihinOlaylari.EksenDegisti(id, Tick, sira, npc, hedef, eksen, delta, new long[0], "test");
        }

        public Olay Duygu(string npc, string tur, decimal siddet, string hedef = "")
        {
            var (id, sira) = Sonraki();
            return ZihinOlaylari.DuyguDegisti(id, Tick, sira, npc, tur, siddet, hedef, new long[0], "test");
        }

        public Olay Stres(string npc, decimal delta)
        {
            var (id, sira) = Sonraki();
            return ZihinOlaylari.StresDegisti(id, Tick, sira, npc, delta, new long[0], "test");
        }

        public Olay Kanit(string npc, string onerme, string tur, string kaynak, decimal guc)
        {
            var (id, sira) = Sonraki();
            return ZihinOlaylari.KanitEklendi(id, Tick, sira, npc, onerme, tur, kaynak, guc, new long[0], "test");
        }

        public Olay Hafiza(string npc, long olayId, decimal onem)
        {
            var (id, sira) = Sonraki();
            return ZihinOlaylari.HafizaEklendi(id, Tick, sira, npc, olayId, onem, new long[0], "test");
        }
    }
}
