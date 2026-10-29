using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Yanki.Core.Events;
using Yanki.Core.World;

namespace Yanki.Core.Mind
{
    /// <summary>Bütün NPC zihinleri: kimliğe göre ordinal sıralı, olayları sahibinin zihnine yönlendirir.</summary>
    public sealed class ZihinDeposu
    {
        private readonly SortedDictionary<string, Zihin> _zihinler = new SortedDictionary<string, Zihin>(StringComparer.Ordinal);

        /// <summary>Zihin sahiplerinin kimlikleri, ordinal sıralı.</summary>
        public IReadOnlyList<string> Kimlikler => new ReadOnlyCollection<string>(new List<string>(_zihinler.Keys));

        /// <summary>Zihin eklenir.</summary>
        /// <exception cref="InvalidOperationException">Aynı kimlikli zihin zaten varsa.</exception>
        public void Ekle(Zihin zihin)
        {
            if (zihin == null)
            {
                throw new ArgumentNullException(nameof(zihin));
            }

            if (_zihinler.ContainsKey(zihin.Id))
            {
                throw new InvalidOperationException($"\"{zihin.Id}\" zihni zaten var.");
            }

            _zihinler.Add(zihin.Id, zihin);
        }

        /// <summary>NPC'nin zihni var mı.</summary>
        public bool Var(string id) => id != null && _zihinler.ContainsKey(id);

        /// <summary>NPC'nin zihni.</summary>
        /// <exception cref="InvalidOperationException">Zihin yoksa.</exception>
        public Zihin this[string id]
        {
            get
            {
                if (id == null || !_zihinler.TryGetValue(id, out var z))
                {
                    throw new InvalidOperationException($"\"{id}\" kimlikli NPC'nin zihni yok.");
                }

                return z;
            }
        }

        /// <summary>
        /// Zihin olayını sahibinin zihnine uygular ve <c>true</c> döner; zihin olayı olmayan olay yoksayılır (<c>false</c>).
        /// </summary>
        /// <exception cref="InvalidOperationException">Sahibin zihni yoksa.</exception>
        public bool Uygula(Olay olay)
        {
            if (olay == null)
            {
                throw new ArgumentNullException(nameof(olay));
            }

            if (!ZihinOlaylari.ZihinOlayiMi(olay.Tur))
            {
                return false;
            }

            return this[olay.Deger("npc")].Uygula(olay);
        }

        /// <summary><c>Fikir(a, b, tick)</c>: a'nın b hakkındaki fikri.</summary>
        public decimal Fikir(string a, string b, long simdikiTick) => this[a].Fikir(b, simdikiTick);

        /// <summary>a'nın b hakkındaki fikrinin dökümü.</summary>
        public FikirDokumu FikirDokumu(string a, string b, long simdikiTick) => this[a].FikirDokumu(b, simdikiTick);

        /// <summary><c>KimdenDuydu(npc, onerme)</c>: NPC'nin önerme desenine uyan <c>duydu</c> kanıtları, sıralı.</summary>
        public IReadOnlyList<Kanit> KimdenDuydu(string npc, string onerme) => this[npc].KimdenDuydu(onerme);

        /// <summary>Bütün zihinlerin kanonik metni; zihin yoksa boş metin.</summary>
        public string KanonikMetin()
        {
            if (_zihinler.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.Append("ZIHIN|1\n");
            foreach (var z in _zihinler.Values)
            {
                sb.Append(z.KanonikMetin());
            }

            return sb.ToString();
        }

        /// <summary>Yalnızca zihin durumunun 64 bit hash'i (16 karakter onaltılık).</summary>
        public string DurumHash() => Hash64.Hex(Hash64.Hesapla(KanonikMetin()));
    }

    /// <summary>
    /// Birleşik durum hash'i: dünya + zihinler (plan 5.1). Zihin yoksa dünya hash'iyle aynıdır, yani 2. haftanın
    /// altın hash'i değişmez; zihin eklenince kanonik metne zihin bölümü eklenir.
    /// </summary>
    public static class DurumHashu
    {
        /// <summary>Dünya ve zihinlerin birleşik durum hash'i.</summary>
        public static string Hesapla(Dunya dunya, ZihinDeposu zihinler)
        {
            if (dunya == null)
            {
                throw new ArgumentNullException(nameof(dunya));
            }

            if (zihinler == null)
            {
                throw new ArgumentNullException(nameof(zihinler));
            }

            return Hash64.Hex(Hash64.Hesapla(dunya.KanonikMetin() + zihinler.KanonikMetin()));
        }
    }
}
