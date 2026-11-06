using System;

namespace Yanki.Dialogue.Gramer
{
    /// <summary>
    /// Türkçe ek değiştiricileri (8. hafta, F kararı + DURAK 2 kararları): <c>yonelme</c> (-e/-a), <c>belirtme</c> (-i/-ı/-u/-ü), <c>bulunma</c> (-de/-da/-te/-ta), <c>ayrilma</c> (-den/-dan/-ten/-tan),
    /// <c>iyelik</c> (3. tekil kişi iyelik eki: -(s)i/-(s)ı/-(s)u/-(s)ü) ve <c>tamlayan</c> (-(n)in/-(n)ın/-(n)un/-(n)ün). Ünlü uyumu ve kaynaştırma elle tablodur; harf sınıflandırması ve büyük harf
    /// kültürden bağımsızdır (<c>ToLower</c>/<c>ToUpper</c> yok; Türkçe I/ı ve İ/i tr-TR'de de aynı sonucu verir). Özel adlarda kesme işareti kullanılır (Ayşe'ye), cins isimlerde kullanılmaz (hancıya).
    /// Ünsüz yumuşaması: p, ç, t, k ile biten CİNS isimde ünlüyle başlayan ek (yonelme, belirtme, iyelik, tamlayan) için yazarın verdiği yumuşak gövde ZORUNLUDUR (borç → borc); özel adda yumuşama yoktur.
    /// Sayıya ek getirilmez: ek okunuşa bağlıdır. Yanlış ek sessizce üretilmez, açık hata fırlatılır.
    /// </summary>
    internal static class TurkceEkler
    {
        public static readonly string[] Ekler = { "yonelme", "belirtme", "bulunma", "ayrilma", "iyelik", "tamlayan" };

        // Yumuşak l: Arapça kökenli adlar (Kemal, Cemal ...) arka ünlüyle yazılsa da ince ünlü eki alır. i/İ/ı/I ayrımı yapılmadan karşılaştırılır.
        private static readonly string[] YumusakLAdlari = { "kemal", "cemal", "celal", "bilal", "nihal", "resul" };

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

        /// <summary>Cins isim p, ç, t ya da k ile bitiyor mu (ünlüyle başlayan ekte yumuşak gövde gerekir).</summary>
        public static bool YumusamaAdayi(string sozcuk)
        {
            if (string.IsNullOrEmpty(sozcuk))
            {
                return false;
            }

            switch (sozcuk[sozcuk.Length - 1])
            {
                case 'p': case 'ç': case 't': case 'k': case 'P': case 'Ç': case 'T': case 'K':
                    return true;
                default:
                    return false;
            }
        }

        public static string Uygula(string ad, bool ozelAd, string ek, bool ince = false, string? yumusakGovde = null)
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

            if (Array.IndexOf(Ekler, ek) < 0)
            {
                throw new ArgumentException($"Bilinmeyen ek \"{ek}\" (yonelme, belirtme, bulunma, ayrilma, iyelik, tamlayan).", nameof(ek));
            }

            // Ünsüz yumuşaması: yalnızca cins isim ve yalnızca ünlüyle başlayan ek (bulunma ve ayrılma ünsüzle başlar).
            var govde = sozcuk;
            var unluylaBaslayanEk = ek == "yonelme" || ek == "belirtme" || ek == "iyelik" || ek == "tamlayan";
            // 9. hafta: aynı "ünlüyle başlayan ekte kullanılacak gövde" kaydı ünlü düşmesi için de kullanılır (mühür → "mühr": mührün, mühre; motor düşmeyi kendiliğinden bilmez,
            // yazar gövdeyi elle yazar; yazmazsa mühürün gibi düşmesiz biçim üretilir).
            if (!ozelAd && unluylaBaslayanEk)
            {
                if (YumusamaAdayi(sozcuk) && string.IsNullOrWhiteSpace(yumusakGovde))
                {
                    throw new ArgumentException(
                        $"\"{sozcuk}\" p, ç, t ya da k ile biten bir cins isim: .{ek} için adlar kaydında yumuşak gövde zorunlu (ör. borç → borc; yumuşamayan sözcükte gövde aynen yazılır).", nameof(yumusakGovde));
                }

                if (!string.IsNullOrWhiteSpace(yumusakGovde))
                {
                    govde = yumusakGovde!.Trim();
                }
            }

            var sonHarf = govde[govde.Length - 1];
            var unluyleBitiyor = Unlu(sonHarf);
            // Yumuşak l: Arapça kökenli adlar arka ünlüyle yazılsa da ince ünlü eki alır (Kemal'e). Yazar `ince` ile belirtir; küçük yerleşik tablo en yaygın adları kapsar.
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
                default: // tamlayan
                    sonek = (unluyleBitiyor ? "n" : string.Empty) + Dortlu(sonUnlu, yumusakL) + "n";
                    break;
            }

            return govde + (ozelAd ? "'" : string.Empty) + sonek;
        }

        /// <summary>
        /// Kişi zamiri (ben/sen) ve ekli hâlleri; elle tablo: ben, bana, beni, bende, benden, benim / sen, sana, seni, sende, senden, senin. Zamire iyelik eki getirilmez.
        /// </summary>
        public static string Zamir(string kisi, string? ek)
        {
            if (kisi != "ben" && kisi != "sen")
            {
                throw new ArgumentException($"Bilinmeyen kişi zamiri \"{kisi}\" (ben, sen).", nameof(kisi));
            }

            var ben = kisi == "ben";
            switch (ek)
            {
                case null: return ben ? "ben" : "sen";
                case "yonelme": return ben ? "bana" : "sana";
                case "belirtme": return ben ? "beni" : "seni";
                case "bulunma": return ben ? "bende" : "sende";
                case "ayrilma": return ben ? "benden" : "senden";
                case "tamlayan": return ben ? "benim" : "senin";
                default: throw new ArgumentException($"Zamire \"{ek}\" eki getirilmez (yonelme, belirtme, bulunma, ayrilma, tamlayan).", nameof(ek));
            }
        }

        /// <summary>
        /// Metnin ilk harfini büyütür. Kültürden bağımsız, elle tablo: Türkçede i → İ ve ı → I (ç, ğ, ö, ş, ü ve a–z de); Türkçe olmayan dosyada i → I. İlk harf harf değilse metin aynen kalır.
        /// </summary>
        public static string BasHarfBuyut(string metin, bool turkce)
        {
            if (string.IsNullOrEmpty(metin))
            {
                return metin;
            }

            return Buyuk(metin[0], turkce) + metin.Substring(1);
        }

        private static string Buyuk(char c, bool turkce)
        {
            switch (c)
            {
                case 'i': return turkce ? "İ" : "I";
                case 'ı': return "I";
                case 'ç': return "Ç";
                case 'ğ': return "Ğ";
                case 'ö': return "Ö";
                case 'ş': return "Ş";
                case 'ü': return "Ü";
                default:
                    return c >= 'a' && c <= 'z' ? ((char)(c - 32)).ToString() : c.ToString();
            }
        }

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
