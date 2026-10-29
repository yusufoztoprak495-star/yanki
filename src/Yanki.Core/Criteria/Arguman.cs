using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Yanki.Core.World;

namespace Yanki.Core.Criteria
{
    /// <summary>Sorgu çağrılarında kullanılabilen roller; bağlamdan bir varlık kimliğine çözülür.</summary>
    public enum Rol
    {
        /// <summary>Kuralı ya da şablonu değerlendiren NPC.</summary>
        Ben,

        /// <summary>Oyuncu.</summary>
        Oyuncu,

        /// <summary>Diyalogda konuşan.</summary>
        Konusan,

        /// <summary>Diyalogda dinleyen.</summary>
        Dinleyen,

        /// <summary>Sonuç kurallarında olayın failı (<c>olay.fail</c>).</summary>
        OlayFail,

        /// <summary>Sonuç kurallarında olayın hedefi (<c>olay.hedef</c>).</summary>
        OlayHedef,
    }

    /// <summary>Rol adları ile <see cref="Rol"/> değerleri arasında çeviri.</summary>
    public static class RolAdlari
    {
        /// <summary>Rolün kriter dilindeki yazımı (<c>olay.fail</c> gibi).</summary>
        public static string Ad(Rol rol)
        {
            switch (rol)
            {
                case Rol.Ben: return "ben";
                case Rol.Oyuncu: return "oyuncu";
                case Rol.Konusan: return "konusan";
                case Rol.Dinleyen: return "dinleyen";
                case Rol.OlayFail: return "olay.fail";
                default: return "olay.hedef";
            }
        }

        /// <summary>Rol adını (<c>ben</c>, <c>olay.fail</c> ...) <see cref="Rol"/> değerine çevirir.</summary>
        public static bool TryCoz(string ad, out Rol rol)
        {
            switch (ad)
            {
                case "ben": rol = Rol.Ben; return true;
                case "oyuncu": rol = Rol.Oyuncu; return true;
                case "konusan": rol = Rol.Konusan; return true;
                case "dinleyen": rol = Rol.Dinleyen; return true;
                case "olay.fail": rol = Rol.OlayFail; return true;
                case "olay.hedef": rol = Rol.OlayHedef; return true;
                default: rol = Rol.Ben; return false;
            }
        }
    }

    /// <summary>Bir sorgu çağrısının argümanı (soyut taban).</summary>
    public abstract class Arguman
    {
        /// <summary>Argümanın kaynak ifadedeki 0 tabanlı karakter indeksi.</summary>
        public int Konum { get; }

        /// <summary>Taban kurucu.</summary>
        protected Arguman(int konum)
        {
            Konum = konum;
        }
    }

    /// <summary>Tanımlayıcı argüman: bir varlık, özellik ya da tür adı (<c>Mehmet</c>, <c>guven</c>).</summary>
    public sealed class TanimlayiciArgumani : Arguman
    {
        /// <summary>Tanımlayıcı.</summary>
        public string Ad { get; }

        /// <summary>Yeni tanımlayıcı argüman.</summary>
        public TanimlayiciArgumani(string ad, int konum)
            : base(konum)
        {
            Ad = ad;
        }
    }

    /// <summary>Sayı argümanı.</summary>
    public sealed class SayiArgumani : Arguman
    {
        /// <summary>Sayının değeri.</summary>
        public decimal Deger { get; }

        /// <summary>Yeni sayı argümanı.</summary>
        public SayiArgumani(decimal deger, int konum)
            : base(konum)
        {
            Deger = deger;
        }
    }

    /// <summary>Joker argüman (<c>*</c>): yalnızca önermelerde, "herhangi biri" demektir.</summary>
    public sealed class JokerArgumani : Arguman
    {
        /// <summary>Yeni joker argüman.</summary>
        public JokerArgumani(int konum)
            : base(konum)
        {
        }
    }

    /// <summary>Rol argümanı (<c>ben</c>, <c>olay.hedef</c> ...).</summary>
    public sealed class RolArgumani : Arguman
    {
        /// <summary>Rol.</summary>
        public Rol Rol { get; }

        /// <summary>Yeni rol argümanı.</summary>
        public RolArgumani(Rol rol, int konum)
            : base(konum)
        {
            Rol = rol;
        }
    }

    /// <summary>Önerme argümanı (<c>borclu(Mehmet, *, *)</c>, <c>konum!(Ali, han)</c>).</summary>
    public sealed class OnermeArgumani : Arguman
    {
        /// <summary>Önermenin kendisi (yüklem + argümanlar).</summary>
        public SorguCagrisi Cagri { get; }

        /// <summary>Yeni önerme argümanı.</summary>
        public OnermeArgumani(SorguCagrisi cagri)
            : base(cagri?.Konum ?? 0)
        {
            Cagri = cagri ?? throw new ArgumentNullException(nameof(cagri));
        }
    }

    /// <summary>
    /// Ayrıştırılmış bir çağrı: <c>fonksiyon(arg, ...)</c> ya da önerme olarak <c>yuklem!(arg, ...)</c>.
    /// </summary>
    public sealed class SorguCagrisi
    {
        /// <summary>Fonksiyon ya da yüklem adı (sondaki <c>!</c> hariç).</summary>
        public string Ad { get; }

        /// <summary>Ad <c>!</c> ile mi yazıldı (tek değerli yüklem).</summary>
        public bool TekDegerli { get; }

        /// <summary>Argümanlar.</summary>
        public IReadOnlyList<Arguman> Argumanlar { get; }

        /// <summary>Adın kaynak ifadedeki 0 tabanlı karakter indeksi.</summary>
        public int Konum { get; }

        /// <summary>Yeni çağrı.</summary>
        public SorguCagrisi(string ad, bool tekDegerli, IEnumerable<Arguman> argumanlar, int konum)
        {
            Ad = ad ?? throw new ArgumentNullException(nameof(ad));
            TekDegerli = tekDegerli;
            Argumanlar = new ReadOnlyCollection<Arguman>(new List<Arguman>(argumanlar ?? throw new ArgumentNullException(nameof(argumanlar))));
            Konum = konum;
        }

        /// <summary>Kanonik yazım: boşluksuz, <c>ad[!](arg,arg)</c>.</summary>
        public override string ToString()
        {
            var parcalar = new List<string>(Argumanlar.Count);
            foreach (var a in Argumanlar)
            {
                parcalar.Add(ArgumanMetni(a));
            }

            return Ad + (TekDegerli ? "!" : string.Empty) + "(" + string.Join(",", parcalar) + ")";
        }

        private static string ArgumanMetni(Arguman a)
        {
            switch (a)
            {
                case TanimlayiciArgumani t: return t.Ad;
                case SayiArgumani s: return KanonikYazici.Sayi(s.Deger);
                case JokerArgumani _: return "*";
                case RolArgumani r: return RolAdlari.Ad(r.Rol);
                case OnermeArgumani o: return o.Cagri.ToString();
                default: throw new InvalidOperationException("Bilinmeyen argüman türü: " + a.GetType().Name);
            }
        }
    }
}
