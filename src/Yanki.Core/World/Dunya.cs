using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.Events;

namespace Yanki.Core.World
{
    /// <summary>
    /// Dünya durumu: varlıklar, mekan grafiği ve gerçekler (plan 5.2). Durum yalnızca olaylar
    /// <see cref="Uygula"/> ile işlenerek ilerler (ADR-001). Bu hafta yalnızca dünya olayları vardır:
    /// <c>varlik_eklendi</c> (<c>id</c>, <c>tur</c>: npc|mekan|esya|oyuncu), <c>gercek_eklendi</c> / <c>gercek_kaldirildi</c>
    /// (<c>gercek</c>: kanonik metin) ve <c>mekan_baglandi</c> (<c>a</c>, <c>b</c>).
    /// Kimin ne bildiği burada tutulmaz.
    /// </summary>
    public sealed class Dunya
    {
        /// <summary>Varlık eklenir. Yük: <c>id</c>, <c>tur</c>.</summary>
        public const string VarlikEklendi = "varlik_eklendi";

        /// <summary>Gerçek eklenir. Yük: <c>gercek</c>.</summary>
        public const string GercekEklendi = "gercek_eklendi";

        /// <summary>Gerçek kaldırılır. Yük: <c>gercek</c>.</summary>
        public const string GercekKaldirildi = "gercek_kaldirildi";

        /// <summary>İki mekan bağlanır. Yük: <c>a</c>, <c>b</c>.</summary>
        public const string MekanBaglandi = "mekan_baglandi";

        private readonly SortedDictionary<string, VarlikTuru> _varliklar =
            new SortedDictionary<string, VarlikTuru>(StringComparer.Ordinal);

        private readonly SortedDictionary<string, SortedSet<string>> _komsular =
            new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        // Çok değerli gerçekler kendi metniyle, tek değerliler TekDegerAnahtari ile saklanır.
        private readonly SortedDictionary<string, Gercek> _gercekler =
            new SortedDictionary<string, Gercek>(StringComparer.Ordinal);

        /// <summary>
        /// Olayı uygular. Dünya olayıysa durumu ilerletir ve <c>true</c> döner; başka türdeki olay yoksayılır
        /// ve <c>false</c> döner (tüm günlük verilebilsin diye).
        /// </summary>
        /// <exception cref="ArgumentException">Yük geçersizse (kimlik, tür, gerçek metni).</exception>
        /// <exception cref="InvalidOperationException">Durum kuralı ihlal edilirse (var olan varlığı eklemek, olmayan gerçeği kaldırmak vb.).</exception>
        public bool Uygula(Olay olay)
        {
            if (olay == null)
            {
                throw new ArgumentNullException(nameof(olay));
            }

            switch (olay.Tur)
            {
                case VarlikEklendi:
                    VarlikEkle(olay.Deger("id"), olay.Deger("tur"));
                    return true;
                case GercekEklendi:
                    GercekEkle(Gercek.Ayristir(olay.Deger("gercek")));
                    return true;
                case GercekKaldirildi:
                    GercekKaldir(Gercek.Ayristir(olay.Deger("gercek")));
                    return true;
                case MekanBaglandi:
                    MekanBagla(olay.Deger("a"), olay.Deger("b"));
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Varlık var mı.</summary>
        public bool VarlikVar(string id) => id != null && _varliklar.ContainsKey(id);

        /// <summary>Varlığın türü; varlık yoksa <c>null</c>.</summary>
        public VarlikTuru? TurOf(string id) =>
            id != null && _varliklar.TryGetValue(id, out var tur) ? tur : (VarlikTuru?)null;

        /// <summary>Verilen türdeki varlık kimlikleri, ordinal sıralı.</summary>
        public IReadOnlyList<string> Varliklar(VarlikTuru tur)
        {
            var sonuc = new List<string>();
            foreach (var kv in _varliklar)
            {
                if (kv.Value == tur)
                {
                    sonuc.Add(kv.Key);
                }
            }

            return new ReadOnlyCollection<string>(sonuc);
        }

        /// <summary>Mekanın komşuları, ordinal sıralı; bilinmeyen mekan için boş.</summary>
        public IReadOnlyList<string> Komsular(string mekan)
        {
            if (mekan != null && _komsular.TryGetValue(mekan, out var set))
            {
                return new ReadOnlyCollection<string>(new List<string>(set));
            }

            return new ReadOnlyCollection<string>(new List<string>());
        }

        /// <summary>İki mekan doğrudan bağlı mı (yönsüz).</summary>
        public bool BagliMi(string a, string b) =>
            a != null && b != null && _komsular.TryGetValue(a, out var set) && set.Contains(b);

        /// <summary>Gerçek şu anda doğru mu.</summary>
        public bool Var(Gercek gercek)
        {
            if (gercek == null)
            {
                throw new ArgumentNullException(nameof(gercek));
            }

            return _gercekler.TryGetValue(Anahtar(gercek), out var kayitli)
                && string.Equals(kayitli.ToString(), gercek.ToString(), StringComparison.Ordinal);
        }

        /// <summary>Desene uyan gerçekler, kanonik metne göre ordinal sıralı.</summary>
        public IReadOnlyList<Gercek> Eslesen(GercekDeseni desen)
        {
            if (desen == null)
            {
                throw new ArgumentNullException(nameof(desen));
            }

            var sonuc = new List<Gercek>();
            foreach (var g in _gercekler.Values)
            {
                if (desen.Uyar(g))
                {
                    sonuc.Add(g);
                }
            }

            sonuc.Sort((x, y) => string.CompareOrdinal(x.ToString(), y.ToString()));
            return new ReadOnlyCollection<Gercek>(sonuc);
        }

        /// <summary>
        /// Tek değerli bir gerçeğin güncel değeri (son argüman); yoksa <c>null</c>.
        /// Örn. <c>TekDeger("konum!", "Ali")</c> → <c>"han"</c>.
        /// </summary>
        /// <exception cref="ArgumentException">Yüklem <c>!</c> ile bitmiyorsa.</exception>
        public string? TekDeger(string yuklem, params string[] anahtarArgumanlari)
        {
            if (yuklem == null || yuklem.Length == 0 || yuklem[yuklem.Length - 1] != '!')
            {
                throw new ArgumentException($"TekDeger yalnızca tek değerli (\"!\" ile biten) yüklemlerde kullanılır: \"{yuklem}\".", nameof(yuklem));
            }

            var anahtar = yuklem + "(" + string.Join(",", anahtarArgumanlari ?? new string[0]);
            if (!_gercekler.TryGetValue(anahtar, out var g))
            {
                return null;
            }

            return g.Argumanlar[g.Argumanlar.Count - 1];
        }

        /// <summary>
        /// Dünya durumunun 64 bit hash'i, 16 karakterlik küçük harfli onaltılık metin olarak (plan 5.1).
        /// Kanonik metin: sürüm başlığı, ardından <c>V|id|tur</c>, <c>M|a|b</c> (a &lt; b) ve <c>G|gercek</c> satırları;
        /// her grup ordinal sıralıdır. Aynı durum, nasıl oluştuğundan bağımsız olarak aynı hash'i verir.
        /// </summary>
        public string DurumHash() => Hash64.Hex(Hash64.Hesapla(KanonikMetin()));

        /// <summary>Hash'in girdisi olan kanonik metin (hata ayıklama ve sınama için).</summary>
        internal string KanonikMetin()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("YANKI-DURUM|").Append(YankiInfo.EventLogFormatVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n');

            foreach (var kv in _varliklar)
            {
                sb.Append("V|").Append(kv.Key).Append('|').Append(TurMetni(kv.Value)).Append('\n');
            }

            foreach (var kv in _komsular)
            {
                foreach (var komsu in kv.Value)
                {
                    if (string.CompareOrdinal(kv.Key, komsu) < 0)
                    {
                        sb.Append("M|").Append(kv.Key).Append('|').Append(komsu).Append('\n');
                    }
                }
            }

            var gercekler = new List<string>();
            foreach (var g in _gercekler.Values)
            {
                gercekler.Add(g.ToString());
            }

            gercekler.Sort(StringComparer.Ordinal);
            foreach (var g in gercekler)
            {
                sb.Append("G|").Append(g).Append('\n');
            }

            return sb.ToString();
        }

        private static string TurMetni(VarlikTuru tur)
        {
            switch (tur)
            {
                case VarlikTuru.Npc: return "npc";
                case VarlikTuru.Mekan: return "mekan";
                case VarlikTuru.Oyuncu: return "oyuncu";
                default: return "esya";
            }
        }

        private static string Anahtar(Gercek g) => g.TekDegerli ? g.TekDegerAnahtari : g.ToString();

        private void VarlikEkle(string id, string tur)
        {
            if (!Kimlik.GecerliKimlikMi(id))
            {
                throw new ArgumentException($"Geçersiz varlık kimliği: \"{id}\" (ASCII kimlik olmalı: harf/rakam/_, harf ya da _ ile başlar).");
            }

            VarlikTuru turu;
            switch (tur)
            {
                case "npc": turu = VarlikTuru.Npc; break;
                case "mekan": turu = VarlikTuru.Mekan; break;
                case "esya": turu = VarlikTuru.Esya; break;
                case "oyuncu": turu = VarlikTuru.Oyuncu; break;
                default:
                    throw new ArgumentException($"Bilinmeyen varlık türü: \"{tur}\" (npc, mekan, esya ya da oyuncu olmalı).");
            }

            if (_varliklar.ContainsKey(id))
            {
                throw new InvalidOperationException($"\"{id}\" varlığı zaten var.");
            }

            _varliklar.Add(id, turu);
            if (turu == VarlikTuru.Mekan)
            {
                _komsular.Add(id, new SortedSet<string>(StringComparer.Ordinal));
            }
        }

        private void GercekEkle(Gercek g)
        {
            _gercekler[Anahtar(g)] = g;
        }

        private void GercekKaldir(Gercek g)
        {
            if (!Var(g))
            {
                throw new InvalidOperationException($"Kaldırılacak gerçek yok: {g}.");
            }

            _gercekler.Remove(Anahtar(g));
        }

        private void MekanBagla(string a, string b)
        {
            if (TurOf(a) != VarlikTuru.Mekan)
            {
                throw new InvalidOperationException($"\"{a}\" bir mekan değil ya da yok; yalnızca mekanlar bağlanabilir.");
            }

            if (TurOf(b) != VarlikTuru.Mekan)
            {
                throw new InvalidOperationException($"\"{b}\" bir mekan değil ya da yok; yalnızca mekanlar bağlanabilir.");
            }

            if (string.Equals(a, b, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Mekan kendisine bağlanamaz: \"{a}\".");
            }

            _komsular[a].Add(b);
            _komsular[b].Add(a);
        }
    }
}
