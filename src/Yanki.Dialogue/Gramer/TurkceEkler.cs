using System;

namespace Yanki.Dialogue.Gramer
{
    /// <summary>
    /// Türkçe ek değiştiricileri (8. hafta, F kararı): <c>yonelme</c> (-e/-a), <c>belirtme</c> (-i/-ı/-u/-ü), <c>bulunma</c> (-de/-da/-te/-ta),
    /// <c>ayrilma</c> (-den/-dan/-ten/-tan) ve <c>iyelik</c> (3. tekil kişi iyelik eki: -(s)i/-(s)ı/-(s)u/-(s)ü). Ünlü uyumu ve kaynaştırma elle tablodur;
    /// harf sınıflandırması kültürden bağımsızdır (<c>ToLower</c> yok; Türkçe I/ı ve İ/i tr-TR'de de aynı sonucu verir). Özel adlarda kesme işareti kullanılır
    /// (Ayşe'ye), cins isimlerde kullanılmaz (hancıya). Sayıya ek getirilmez: ek okunuşa bağlıdır. Yanlış ek sessizce üretilmez, açık hata fırlatılır.
    /// </summary>
    internal static class TurkceEkler
    {
        public static readonly string[] Ekler = { "yonelme", "belirtme", "bulunma", "ayrilma", "iyelik" };

        /// <summary>Sözcük yalnızca rakamlardan (ve başta isteğe bağlı eksi işaretinden) oluşuyorsa doğru.</summary>
        public static bool SayiMi(string sozcuk)
        {
            if (string.IsNullOrEmpty(sozcuk))
            {
                return false;
            }

            var i = sozcuk[0] == '-' ? 1 : 0;
            if (i == sozcuk.Length)
            {
                return false;
            }

            for (; i < sozcuk.Length; i++)
            {
                if (sozcuk[i] < '0' || sozcuk[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }

        public static string Uygula(string ad, bool ozelAd, string ek, bool ince = false)
        {
            if (ad == null)
            {
                throw new ArgumentNullException(nameof(ad));
            }

            var sozcuk = ad.Trim();
            if (sozcuk.Length == 0)
            {
                throw new ArgumentException("Boş sözcüğe ek getirilmez.", nameof(ad));
            }

            if (SayiMi(sozcuk))
            {
                throw new ArgumentException($"Sayıya ek getirilmez (\"{sozcuk}\"): ek okunuşa bağlıdır; sayıyı ekle kullanma ya da sözcükle yaz.", nameof(ad));
            }

            foreach (var c in sozcuk)
            {
                if (c >= '0' && c <= '9')
                {
                    throw new ArgumentException($"Rakam içeren sözcüğe ek getirilmez (\"{sozcuk}\"): ek okunuşa bağlıdır.", nameof(ad));
                }
            }

            var sonUnlu = SonUnlu(sozcuk);
            if (sonUnlu == '\0')
            {
                throw new ArgumentException($"\"{sozcuk}\" ünlü içermiyor; ek getirilemez.", nameof(ad));
            }

            var sonHarf = sozcuk[sozcuk.Length - 1];
            var unluyleBitiyor = Unlu(sonHarf);
            // Yumuşak (damaksıl) l: Arapça kökenli adlar (Kemal, Cemal ...) arka ünlüyle yazılsa da ince ünlü eki alır (Kemal'e). Yazar `ince` ile belirtir; küçük yerleşik tablo en yaygın adları kapsar.
            var yumusakL = ince || (ozelAd && YumusakLAdi(sozcuk));
            var on = Ince(sonUnlu) || yumusakL;
            string sonek;
            switch (ek)
            {
                case "yonelme":
                    sonek = (unluyleBitiyor ? "y" : string.Empty) + (on ? "e" : "a");
                    break;
                case "belirtme":
                    sonek = (unluyleBitiyor ? "y" : string.Empty) + Dortlu(sonUnlu, yumusakL);
                    break;
                case "bulunma":
                    sonek = (Sert(sonHarf) ? "t" : "d") + (on ? "e" : "a");
                    break;
                case "ayrilma":
                    sonek = (Sert(sonHarf) ? "t" : "d") + (on ? "e" : "a") + "n";
                    break;
                case "iyelik":
                    sonek = (unluyleBitiyor ? "s" : string.Empty) + Dortlu(sonUnlu, yumusakL);
                    break;
                default:
                    throw new ArgumentException($"Bilinmeyen ek \"{ek}\" (yonelme, belirtme, bulunma, ayrilma, iyelik).", nameof(ek));
            }

            return sozcuk + (ozelAd ? "'" : string.Empty) + sonek;
        }

        // Yumuşak l ile biten, ince ünlü eki alan yaygın özel adlar (i/İ/ı/I ayrımı yapılmadan karşılaştırılır).
        private static readonly string[] YumusakLAdlari = { "kemal", "cemal", "celal", "bilal", "nihal", "resul" };

        private static bool YumusakLAdi(string sozcuk)
        {
            var katla = new System.Text.StringBuilder(sozcuk.Length);
            foreach (var c in sozcuk)
            {
                katla.Append(c >= 'A' && c <= 'Z' && c != 'I' ? (char)(c + 32) : (c == 'I' || c == 'İ' || c == 'ı' ? 'i' : c));
            }

            return Array.IndexOf(YumusakLAdlari, katla.ToString()) >= 0;
        }

        // Kültürden bağımsız harf sınıfları. Büyük I (noktasız) arka, büyük İ (noktalı) ince ünlüdür.
        private static bool Unlu(char c)
        {
            switch (c)
            {
                case 'a': case 'e': case 'ı': case 'i': case 'o': case 'ö': case 'u': case 'ü':
                case 'A': case 'E': case 'I': case 'İ': case 'O': case 'Ö': case 'U': case 'Ü':
                case 'â': case 'î': case 'û': case 'Â': case 'Î': case 'Û':
                    return true;
                default:
                    return false;
            }
        }

        private static char SonUnlu(string sozcuk)
        {
            for (var i = sozcuk.Length - 1; i >= 0; i--)
            {
                if (Unlu(sozcuk[i]))
                {
                    return sozcuk[i];
                }
            }

            return '\0';
        }

        private static bool Ince(char unlu)
        {
            switch (unlu)
            {
                case 'e': case 'i': case 'ö': case 'ü': case 'E': case 'İ': case 'Ö': case 'Ü': case 'î': case 'Î':
                    return true;
                default:
                    return false;
            }
        }

        private static string Dortlu(char unlu, bool yumusakL)
        {
            if (yumusakL)
            {
                switch (unlu)
                {
                    case 'a': case 'ı': case 'A': case 'I': case 'â': case 'Â':
                        return "i";
                    case 'o': case 'u': case 'O': case 'U': case 'û': case 'Û':
                        return "ü";
                }
            }

            switch (unlu)
            {
                case 'a': case 'ı': case 'A': case 'I': case 'â': case 'Â':
                    return "ı";
                case 'e': case 'i': case 'E': case 'İ': case 'î': case 'Î':
                    return "i";
                case 'o': case 'u': case 'O': case 'U': case 'û': case 'Û':
                    return "u";
                default:
                    return "ü";   // ö, ü, Ö, Ü
            }
        }

        // Sert ünsüzler: ç f h k p s ş t (sonra -te/-ta/-ten/-tan).
        private static bool Sert(char c)
        {
            switch (c)
            {
                case 'ç': case 'f': case 'h': case 'k': case 'p': case 's': case 'ş': case 't':
                case 'Ç': case 'F': case 'H': case 'K': case 'P': case 'S': case 'Ş': case 'T':
                    return true;
                default:
                    return false;
            }
        }
    }
}
