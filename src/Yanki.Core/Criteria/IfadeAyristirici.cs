using System;
using System.Collections.Generic;
using System.Globalization;

namespace Yanki.Core.Criteria
{
    /// <summary>
    /// Kriterin <c>alan</c> ifadesini ayrıştırır. Gramer:
    /// <code>
    /// cagri := ad ['!'] '(' [arg {',' arg}] ')'
    /// arg   := '*' | sayi | rol | tanimlayici | cagri
    /// rol   := ben | oyuncu | konusan | dinleyen | olay.fail | olay.hedef
    /// </code>
    /// Boşluklar anlamsızdır. Yalnızca söz dizimine bakar; sözlük denetimi <see cref="SorguSozlugu.Dogrula"/>'dadır.
    /// </summary>
    public static class IfadeAyristirici
    {
        /// <summary>İfadeyi ayrıştırır.</summary>
        /// <exception cref="KriterHatasi">Söz dizimi hatalıysa; hata karakter konumunu taşır.</exception>
        public static SorguCagrisi Ayristir(string ifade)
        {
            if (ifade == null)
            {
                throw new ArgumentNullException(nameof(ifade));
            }

            return new Okuyucu(ifade).Calistir();
        }

        private sealed class Okuyucu
        {
            private readonly string _s;
            private int _i;

            public Okuyucu(string s)
            {
                _s = s;
            }

            public SorguCagrisi Calistir()
            {
                BoslukAtla();
                var konum = _i;
                var ad = KimlikOku() ?? throw Hata("Fonksiyon adı bekleniyor", _i);
                var cagri = CagriGovdesi(ad, konum);
                BoslukAtla();
                if (_i < _s.Length)
                {
                    throw Hata($"İfadenin sonunda beklenmeyen karakter '{_s[_i]}'", _i);
                }

                return cagri;
            }

            // Ad okunmuş olarak, '!' ve '(' ... ')' kısmını okur.
            private SorguCagrisi CagriGovdesi(string ad, int konum)
            {
                BoslukAtla();
                var tekDegerli = false;
                if (Bak() == '!')
                {
                    tekDegerli = true;
                    _i++;
                    BoslukAtla();
                }

                if (Bak() != '(')
                {
                    throw Hata("'(' bekleniyor", _i);
                }

                _i++;
                var argumanlar = new List<Arguman>();
                BoslukAtla();
                if (Bak() == ')')
                {
                    _i++;
                    return new SorguCagrisi(ad, tekDegerli, argumanlar, konum);
                }

                while (true)
                {
                    argumanlar.Add(ArgumanOku());
                    BoslukAtla();
                    if (Bak() == ',')
                    {
                        _i++;
                        continue;
                    }

                    if (Bak() == ')')
                    {
                        _i++;
                        return new SorguCagrisi(ad, tekDegerli, argumanlar, konum);
                    }

                    throw Hata("',' ya da ')' bekleniyor", _i);
                }
            }

            private Arguman ArgumanOku()
            {
                BoslukAtla();
                var konum = _i;
                var c = Bak();
                if (c == '\0' && _i >= _s.Length)
                {
                    throw Hata("Argüman bekleniyor", _i);
                }

                if (c == '*')
                {
                    _i++;
                    return new JokerArgumani(konum);
                }

                if (RakamMi(c) || c == '-')
                {
                    return SayiOku();
                }

                if (HarfMi(c) || c == '_')
                {
                    var ad = KimlikOku()!;
                    if (ad == "olay")
                    {
                        return OlayRolu(konum);
                    }

                    if (CagriBasliyorMu())
                    {
                        return new OnermeArgumani(CagriGovdesi(ad, konum));
                    }

                    switch (ad)
                    {
                        case "ben": return new RolArgumani(Rol.Ben, konum);
                        case "oyuncu": return new RolArgumani(Rol.Oyuncu, konum);
                        case "konusan": return new RolArgumani(Rol.Konusan, konum);
                        case "dinleyen": return new RolArgumani(Rol.Dinleyen, konum);
                        default: return new TanimlayiciArgumani(ad, konum);
                    }
                }

                throw Hata($"Geçersiz karakter '{c}'", _i);
            }

            // "olay" okundu; olay.fail ya da olay.hedef bekleniyor.
            private Arguman OlayRolu(int konum)
            {
                if (Bak() != '.')
                {
                    throw Hata("'olay' tek başına kullanılamaz; olay.fail ya da olay.hedef bekleniyor", _i);
                }

                _i++;
                var altKonum = _i;
                var alt = KimlikOku();
                switch (alt)
                {
                    case "fail": return new RolArgumani(Rol.OlayFail, konum);
                    case "hedef": return new RolArgumani(Rol.OlayHedef, konum);
                    default: throw Hata("'olay.' sonrasında 'fail' ya da 'hedef' bekleniyor", altKonum);
                }
            }

            private Arguman SayiOku()
            {
                var baslangic = _i;
                if (Bak() == '-')
                {
                    _i++;
                }

                RakamlariOku();
                if (Bak() == '.')
                {
                    _i++;
                    RakamlariOku();
                }

                var metin = _s.Substring(baslangic, _i - baslangic);
                if (!decimal.TryParse(metin, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var deger))
                {
                    throw Hata($"Sayı okunamadı ya da çok büyük: '{metin}'", baslangic);
                }

                return new SayiArgumani(deger, baslangic);
            }

            private void RakamlariOku()
            {
                if (!RakamMi(Bak()))
                {
                    throw Hata("Rakam bekleniyor", _i);
                }

                while (RakamMi(Bak()))
                {
                    _i++;
                }
            }

            // Kimlikten sonra (boşluklar atlanarak) '(' ya da '!' var mı; tüketmez.
            private bool CagriBasliyorMu()
            {
                var j = _i;
                while (j < _s.Length && char.IsWhiteSpace(_s[j]))
                {
                    j++;
                }

                return j < _s.Length && (_s[j] == '(' || _s[j] == '!');
            }

            private string? KimlikOku()
            {
                if (!(HarfMi(Bak()) || Bak() == '_'))
                {
                    return null;
                }

                var baslangic = _i;
                while (HarfMi(Bak()) || RakamMi(Bak()) || Bak() == '_')
                {
                    _i++;
                }

                return _s.Substring(baslangic, _i - baslangic);
            }

            private char Bak() => _i < _s.Length ? _s[_i] : '\0';

            private void BoslukAtla()
            {
                while (_i < _s.Length && char.IsWhiteSpace(_s[_i]))
                {
                    _i++;
                }
            }

            private static bool HarfMi(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

            private static bool RakamMi(char c) => c >= '0' && c <= '9';

            private static KriterHatasi Hata(string mesaj, int konum) => new KriterHatasi(mesaj, konum);
        }
    }
}
