using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Yanki.Core.Simulation
{
    /// <summary>
    /// Bir tick'te yazılan algı olaylarının haritası: olay kimliği → (algılayan NPC → o NPC için yazılan algı olaylarının kimlikleri).
    /// Algılayanlar algı SONUCUNDAN türetilir (ayrı bir bilgi kaynağı yok): her algılayan için en az bir algı olayı yazılır.
    /// NPC eylemleri ve hareketleri için bu harita bir sonraki tick'in kural değerlendirmesinde kullanılır, sonra atılır; böylece
    /// kurallar algılayanları o anki konumdan yeniden hesaplamaz (ADR-011).
    /// </summary>
    internal sealed class AlgiHaritasi
    {
        private readonly SortedDictionary<long, SortedDictionary<string, List<long>>> _harita =
            new SortedDictionary<long, SortedDictionary<string, List<long>>>();

        /// <summary>Olay için bir algı olayı kaydeder.</summary>
        public void Ekle(long olayId, string npc, long algiOlayId)
        {
            if (!_harita.TryGetValue(olayId, out var npcler))
            {
                npcler = new SortedDictionary<string, List<long>>(StringComparer.Ordinal);
                _harita.Add(olayId, npcler);
            }

            if (!npcler.TryGetValue(npc, out var liste))
            {
                liste = new List<long>();
                npcler.Add(npc, liste);
            }

            liste.Add(algiOlayId);
        }

        /// <summary>Başka bir haritadaki kayıtları ekler.</summary>
        public void Birlestir(AlgiHaritasi diger)
        {
            foreach (var olay in diger._harita)
            {
                foreach (var npc in olay.Value)
                {
                    foreach (var id in npc.Value)
                    {
                        Ekle(olay.Key, npc.Key, id);
                    }
                }
            }
        }

        /// <summary>Olayı algılayan NPC'ler, ordinal sıralı; kimse algılamadıysa boş.</summary>
        public IReadOnlyList<string> Algilayanlar(long olayId)
        {
            if (!_harita.TryGetValue(olayId, out var npcler))
            {
                return new ReadOnlyCollection<string>(new List<string>());
            }

            return new ReadOnlyCollection<string>(new List<string>(npcler.Keys));
        }

        /// <summary>NPC'nin bu olayı algılamasından doğan algı olaylarının kimlikleri.</summary>
        public IReadOnlyList<long> Nedenler(long olayId, string npc)
        {
            if (_harita.TryGetValue(olayId, out var npcler) && npcler.TryGetValue(npc, out var liste))
            {
                return new ReadOnlyCollection<long>(new List<long>(liste));
            }

            return new ReadOnlyCollection<long>(new List<long>());
        }
    }
}
