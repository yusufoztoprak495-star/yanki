using System;
using System.Collections.Generic;

namespace Yanki.Dialogue
{
    /// <summary>
    /// Tekrar kontrolleri durumu (D kararı): yalnızca Yanki.Dialogue içindedir, simülasyona yazılmaz; ÜRETİLEN SATIR DİZİSİNDEN türer ve baştan oynatmayla aynen yeniden kurulur.
    /// Kapsam konuşan başınadır. <c>tekrar_bekleme N</c>: son kullanımdan beri <c>tick − son ≥ N</c> ise serbest (<c>cooldown</c> ile aynı sınır). <c>bir_kez</c>: konuşan başına bir kez.
    /// <c>tekrarsiz</c>: grup içindeki kullanılabilir üyelerin hepsi kullanılana kadar tekrar yok, tükenince döngü başlar.
    /// </summary>
    internal sealed class TekrarDurumu
    {
        private readonly SortedDictionary<string, long> _sonTick = new SortedDictionary<string, long>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, SortedSet<string>> _grupKullanimi = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        private static string Anahtar(string konusan, string sablonId) => konusan + "|" + sablonId;

        private static string GrupAnahtari(string konusan, string grup) => konusan + "|" + grup;

        /// <summary>
        /// Şablon bu konuşan için şimdi engelli mi. <paramref name="grupUyeleri"/>: bu şablonla aynı gruptaki, tekrarsiz işaretli ve diğer filtreleri geçmiş şablon kimlikleri
        /// (tükenme hesabı yalnızca bunlara bakar; kullanılamayan üye grubu sonsuza dek tıkamasın). Tükenmiş grup döngüye girer (kullanım kümesi temizlenir).
        /// </summary>
        public bool Engelli(Sablon s, string konusan, long tick, ICollection<string> grupUyeleri)
        {
            switch (s.Tekrar.Tur)
            {
                case TekrarTuru.Bekleme:
                    return _sonTick.TryGetValue(Anahtar(konusan, s.Id), out var son) && tick - son < s.Tekrar.Bekleme;
                case TekrarTuru.BirKez:
                    return _sonTick.ContainsKey(Anahtar(konusan, s.Id));
                case TekrarTuru.Tekrarsiz:
                    if (!_grupKullanimi.TryGetValue(GrupAnahtari(konusan, s.Grup), out var kullanilan))
                    {
                        return false;
                    }

                    var tukendi = true;
                    foreach (var uye in grupUyeleri)
                    {
                        if (!kullanilan.Contains(uye))
                        {
                            tukendi = false;
                            break;
                        }
                    }

                    if (tukendi)
                    {
                        kullanilan.Clear();
                        return false;
                    }

                    return kullanilan.Contains(s.Id);
                default:
                    return false;
            }
        }

        /// <summary>Üretilen satırın şablonunu kaydeder.</summary>
        public void Kaydet(Sablon s, string konusan, long tick)
        {
            _sonTick[Anahtar(konusan, s.Id)] = tick;
            if (s.Tekrar.Tur == TekrarTuru.Tekrarsiz)
            {
                if (!_grupKullanimi.TryGetValue(GrupAnahtari(konusan, s.Grup), out var kullanilan))
                {
                    kullanilan = new SortedSet<string>(StringComparer.Ordinal);
                    _grupKullanimi[GrupAnahtari(konusan, s.Grup)] = kullanilan;
                }

                kullanilan.Add(s.Id);
            }
        }
    }
}
