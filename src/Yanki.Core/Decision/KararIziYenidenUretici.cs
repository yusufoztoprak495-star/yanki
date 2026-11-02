using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.Events;
using Yanki.Core.Simulation;

namespace Yanki.Core.Decision
{
    /// <summary>Yeniden üretilen tam iz, günlükteki kısa izle uyuşmadığında fırlatılır (determinizm bozulmuş ya da girdi değişmiştir).</summary>
    public sealed class KararIziUyusmazligiException : InvalidOperationException
    {
        /// <summary>Uyuşmayan <c>karar_verildi</c> olayının kimliği.</summary>
        public long OlayId { get; }

        /// <summary>Kurar.</summary>
        public KararIziUyusmazligiException(long olayId, string npc, long tick, string ayrinti)
            : base($"Karar izi uyuşmuyor (#{olayId.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {npc}, tick {tick.ToString(System.Globalization.CultureInfo.InvariantCulture)}): {ayrinti}. "
                + "Replay günlükle aynı sonucu vermedi; senaryo/kural/eylem dosyaları, seed ya da seçici değişmiş olabilir ya da determinizm bozulmuştur.")
        {
            OlayId = olayId;
        }
    }

    /// <summary>
    /// Geçmiş kararların TAM izini yeniden üretir (K3, ADR-011): günlük yalnızca kısa izi taşır, simülasyon geçmiş tam izleri tutmaz.
    /// Oturum, yeni kurulmuş bir simülasyonla o karara kadar replay ile oynatılır; yalnızca istenen (NPC, tick) çiftlerinin izi yakalanır.
    /// Yeniden üretilen iz günlükteki kısa izle karşılaştırılır; uyuşmazlık sessizce geçmez, <see cref="KararIziUyusmazligiException"/> fırlatır.
    /// Orijinal simülasyona dokunulmaz; hesap durum hash'ini ve olay günlüğünü değiştirmez.
    /// </summary>
    public static class KararIziYenidenUretici
    {
        /// <summary>Tek kararın tam izini üretir.</summary>
        /// <param name="yeniSimulasyon">Her çağrıda yeni kurulmuş (tick 0) ve orijinal oturumla aynı seed/senaryo/kural/eylem/seçiciyle kurulmuş simülasyon üretir.</param>
        /// <param name="kayit">Oturum kaydı (seed + eylem günlüğü).</param>
        /// <param name="kararOlayi">Günlükteki <c>karar_verildi</c> olayı.</param>
        /// <exception cref="ArgumentException">Olay karar olayı değilse ya da kaydın tick'inden sonraysa.</exception>
        /// <exception cref="KararIziUyusmazligiException">Yeniden üretilen iz günlükteki kısa izle uyuşmuyorsa.</exception>
        public static KararIzi Uret(Func<Simulasyon> yeniSimulasyon, OturumKaydi kayit, Olay kararOlayi)
        {
            if (kararOlayi == null)
            {
                throw new ArgumentNullException(nameof(kararOlayi));
            }

            return UretCoklu(yeniSimulasyon, kayit, new[] { kararOlayi })[kararOlayi.Id];
        }

        /// <summary>Birden çok kararın tam izini TEK replay ile üretir (olay kimliği → tam iz).</summary>
        /// <exception cref="ArgumentException">Bir olay karar olayı değilse ya da kaydın tick'inden sonraysa.</exception>
        /// <exception cref="KararIziUyusmazligiException">Yeniden üretilen bir iz günlükteki kısa izle uyuşmuyorsa.</exception>
        public static IReadOnlyDictionary<long, KararIzi> UretCoklu(Func<Simulasyon> yeniSimulasyon, OturumKaydi kayit, IEnumerable<Olay> kararOlaylari)
        {
            if (yeniSimulasyon == null)
            {
                throw new ArgumentNullException(nameof(yeniSimulasyon));
            }

            if (kayit == null)
            {
                throw new ArgumentNullException(nameof(kayit));
            }

            if (kararOlaylari == null)
            {
                throw new ArgumentNullException(nameof(kararOlaylari));
            }

            var olaylar = new List<Olay>(kararOlaylari);
            long enGecTick = 0;
            foreach (var o in olaylar)
            {
                if (o.Tur != YerlesikTurler.KararVerildi)
                {
                    throw new ArgumentException($"Olay #{o.Id} bir karar_verildi olayı değil ({o.Tur}).", nameof(kararOlaylari));
                }

                if (o.Tick > kayit.TickSayisi)
                {
                    throw new ArgumentException($"Olay #{o.Id} tick {o.Tick}'te, kayıt yalnızca {kayit.TickSayisi} tick içeriyor.", nameof(kararOlaylari));
                }

                enGecTick = Math.Max(enGecTick, o.Tick);
            }

            var sim = yeniSimulasyon();
            foreach (var o in olaylar)
            {
                sim.TamIziIste(o.Fail, o.Tick);
            }

            OturumOynatici.TickeKadarOynat(sim, kayit, enGecTick);

            var sonuc = new SortedDictionary<long, KararIzi>();
            foreach (var o in olaylar)
            {
                var iz = sim.YakalananIz(o.Fail, o.Tick)
                    ?? throw new KararIziUyusmazligiException(o.Id, o.Fail, o.Tick, "replay'de bu NPC bu tick'te karar vermedi");
                var fark = KisaIz.Oku(o).Fark(iz);
                if (fark != null)
                {
                    throw new KararIziUyusmazligiException(o.Id, o.Fail, o.Tick, fark);
                }

                sonuc[o.Id] = iz;
            }

            return new ReadOnlyDictionary<long, KararIzi>(sonuc);
        }
    }
}
