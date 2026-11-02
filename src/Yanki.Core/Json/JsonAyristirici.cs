using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Yanki.Core.Json
{
    /// <summary>JSON metni geçersizse fırlatılır; mesaj 1 tabanlı satır ve sütunu içerir.</summary>
    public sealed class JsonHatasi : Exception
    {
        /// <summary>Hatanın 1 tabanlı satırı.</summary>
        public int Satir { get; }

        /// <summary>Hatanın 1 tabanlı sütunu.</summary>
        public int Sutun { get; }

        /// <summary>Hata kurar.</summary>
        public JsonHatasi(string mesaj, int satir, int sutun)
            : base($"{mesaj} (satır {satir}, sütun {sutun})")
        {
            Satir = satir;
            Sutun = sutun;
        }
    }

    /// <summary>
    /// Bağımlılıksız, katı JSON (RFC 8259) okuyucusu. Sayılar <see cref="decimal"/> okunur (ADR-007), nesne özellikleri
    /// dosyadaki sırayla tutulur, tekrar eden anahtar hatadır. Yorum, sondaki virgül ve tırnaksız anahtar yoktur.
    /// Çekirdek dosya sistemine dokunmaz; metin dışarıdan verilir.
    /// </summary>
    public static class JsonAyristirici
    {
        private const int EnFazlaDerinlik = 64;

        /// <summary>Okunabilecek en büyük metin uzunluğu (karakter). Kural, senaryo ve oturum dosyaları bunun çok altındadır.</summary>
        public const int EnFazlaBoyut = 8 * 1024 * 1024;

        /// <summary>Metni ayrıştırır.</summary>
        /// <exception cref="JsonHatasi">Metin geçerli JSON değilse.</exception>
        public static JsonDegeri Ayristir(string metin)
        {
            if (metin == null)
            {
                throw new ArgumentNullException(nameof(metin));
            }

            if (metin.Length > EnFazlaBoyut)
            {
                throw new JsonHatasi($"JSON boyutu en fazla {EnFazlaBoyut} karakter olabilir, {metin.Length} verildi", 1, 1);
            }

            var okuyucu = new Okuyucu(metin);
            return okuyucu.Calistir();
        }

        private sealed class Okuyucu
        {
            private readonly string _s;
            private int _i;
            private int _satir = 1;
            private int _sutun = 1;

            public Okuyucu(string s)
            {
                _s = s;
                if (_s.Length > 0 && _s[0] == '﻿')
                {
                    _i = 1;
                }
            }

            public JsonDegeri Calistir()
            {
                BosluklariAtla();
                var deger = Deger(0);
                BosluklariAtla();
                if (_i < _s.Length)
                {
                    throw Hata("Değerden sonra beklenmeyen karakter");
                }

                return deger;
            }

            private JsonDegeri Deger(int derinlik)
            {
                if (_i >= _s.Length)
                {
                    throw Hata("Değer beklenirken metin bitti");
                }

                var satir = _satir;
                var sutun = _sutun;
                var c = _s[_i];
                switch (c)
                {
                    case '{': return Nesne(derinlik, satir, sutun);
                    case '[': return Dizi(derinlik, satir, sutun);
                    case '"': return JsonDegeri.OkunanMetin(Metin(), satir, sutun);
                    case 't': Sozcuk("true"); return JsonDegeri.OkunanMantiksal(true, satir, sutun);
                    case 'f': Sozcuk("false"); return JsonDegeri.OkunanMantiksal(false, satir, sutun);
                    case 'n': Sozcuk("null"); return JsonDegeri.OkunanBos(satir, sutun);
                    default:
                        if (c == '-' || (c >= '0' && c <= '9'))
                        {
                            return JsonDegeri.OkunanSayi(Sayi(), satir, sutun);
                        }

                        throw Hata($"Beklenmeyen karakter '{c}'");
                }
            }

            private JsonDegeri Nesne(int derinlik, int satir, int sutun)
            {
                DerinlikDenetle(derinlik);
                Ilerle();
                var liste = new List<KeyValuePair<string, JsonDegeri>>();
                var gorulen = new SortedSet<string>(StringComparer.Ordinal);
                BosluklariAtla();
                if (Bak() == '}')
                {
                    Ilerle();
                    return JsonDegeri.OkunanNesne(liste, satir, sutun);
                }

                while (true)
                {
                    BosluklariAtla();
                    if (Bak() != '"')
                    {
                        throw Hata("Nesne anahtarı (tırnaklı metin) bekleniyordu");
                    }

                    var anahtarSatiri = _satir;
                    var anahtarSutunu = _sutun;
                    var anahtar = Metin();
                    if (!gorulen.Add(anahtar))
                    {
                        throw new JsonHatasi($"\"{anahtar}\" anahtarı nesnede birden fazla kez geçiyor", anahtarSatiri, anahtarSutunu);
                    }

                    BosluklariAtla();
                    if (Bak() != ':')
                    {
                        throw Hata("':' bekleniyordu");
                    }

                    Ilerle();
                    BosluklariAtla();
                    liste.Add(new KeyValuePair<string, JsonDegeri>(anahtar, Deger(derinlik + 1)));
                    BosluklariAtla();
                    var c = Bak();
                    if (c == ',')
                    {
                        Ilerle();
                        continue;
                    }

                    if (c == '}')
                    {
                        Ilerle();
                        return JsonDegeri.OkunanNesne(liste, satir, sutun);
                    }

                    throw Hata("',' ya da '}' bekleniyordu");
                }
            }

            private JsonDegeri Dizi(int derinlik, int satir, int sutun)
            {
                DerinlikDenetle(derinlik);
                Ilerle();
                var liste = new List<JsonDegeri>();
                BosluklariAtla();
                if (Bak() == ']')
                {
                    Ilerle();
                    return JsonDegeri.OkunanDizi(liste, satir, sutun);
                }

                while (true)
                {
                    BosluklariAtla();
                    liste.Add(Deger(derinlik + 1));
                    BosluklariAtla();
                    var c = Bak();
                    if (c == ',')
                    {
                        Ilerle();
                        continue;
                    }

                    if (c == ']')
                    {
                        Ilerle();
                        return JsonDegeri.OkunanDizi(liste, satir, sutun);
                    }

                    throw Hata("',' ya da ']' bekleniyordu");
                }
            }

            private string Metin()
            {
                Ilerle(); // açılış tırnağı
                var sb = new StringBuilder();
                while (true)
                {
                    if (_i >= _s.Length)
                    {
                        throw Hata("Metin kapanmadan bitti");
                    }

                    var c = _s[_i];
                    if (c == '"')
                    {
                        Ilerle();
                        return sb.ToString();
                    }

                    if (c < ' ')
                    {
                        throw Hata("Metin içinde kaçışsız denetim karakteri");
                    }

                    if (c != '\\')
                    {
                        sb.Append(c);
                        Ilerle();
                        continue;
                    }

                    Ilerle();
                    if (_i >= _s.Length)
                    {
                        throw Hata("Kaçış dizisi yarım kaldı");
                    }

                    var k = _s[_i];
                    switch (k)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            sb.Append(Unicode());
                            continue;
                        default:
                            throw Hata($"Geçersiz kaçış dizisi '\\{k}'");
                    }

                    Ilerle();
                }
            }

            // Çağrıldığında _i 'u' harfinde; dört onaltılık basamağı okur ve sonrasına geçer.
            private char Unicode()
            {
                Ilerle();
                var deger = 0;
                for (var n = 0; n < 4; n++)
                {
                    if (_i >= _s.Length)
                    {
                        throw Hata("\\u kaçışı yarım kaldı");
                    }

                    var h = OnaltilikDeger(_s[_i]);
                    if (h < 0)
                    {
                        throw Hata("\\u kaçışında onaltılık basamak bekleniyordu");
                    }

                    deger = (deger * 16) + h;
                    Ilerle();
                }

                return (char)deger;
            }

            private decimal Sayi()
            {
                var bas = _i;
                var satir = _satir;
                var sutun = _sutun;
                if (Bak() == '-')
                {
                    Ilerle();
                }

                if (Bak() == '0')
                {
                    Ilerle();
                    if (RakamMi(Bak()))
                    {
                        throw Hata("Sayı sıfırla başlayıp rakamla devam edemez");
                    }
                }
                else
                {
                    RakamlariOku();
                }

                if (Bak() == '.')
                {
                    Ilerle();
                    RakamlariOku();
                }

                if (Bak() == 'e' || Bak() == 'E')
                {
                    Ilerle();
                    if (Bak() == '+' || Bak() == '-')
                    {
                        Ilerle();
                    }

                    RakamlariOku();
                }

                var parca = _s.Substring(bas, _i - bas);
                if (!decimal.TryParse(parca, NumberStyles.Float, CultureInfo.InvariantCulture, out var sonuc))
                {
                    throw new JsonHatasi($"Sayı decimal aralığına sığmıyor: {parca}", satir, sutun);
                }

                return sonuc;
            }

            private void RakamlariOku()
            {
                if (!RakamMi(Bak()))
                {
                    throw Hata("Rakam bekleniyordu");
                }

                while (RakamMi(Bak()))
                {
                    Ilerle();
                }
            }

            private void Sozcuk(string sozcuk)
            {
                for (var n = 0; n < sozcuk.Length; n++)
                {
                    if (_i >= _s.Length || _s[_i] != sozcuk[n])
                    {
                        throw Hata($"'{sozcuk}' bekleniyordu");
                    }

                    Ilerle();
                }
            }

            private void BosluklariAtla()
            {
                while (_i < _s.Length)
                {
                    var c = _s[_i];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                    {
                        Ilerle();
                    }
                    else
                    {
                        break;
                    }
                }
            }

            private void DerinlikDenetle(int derinlik)
            {
                if (derinlik >= EnFazlaDerinlik)
                {
                    throw Hata($"İç içe geçme en fazla {EnFazlaDerinlik} olabilir");
                }
            }

            private char Bak() => _i < _s.Length ? _s[_i] : '\0';

            private void Ilerle()
            {
                if (_s[_i] == '\n')
                {
                    _satir++;
                    _sutun = 1;
                }
                else
                {
                    _sutun++;
                }

                _i++;
            }

            private JsonHatasi Hata(string mesaj) => new JsonHatasi(mesaj, _satir, _sutun);

            private static bool RakamMi(char c) => c >= '0' && c <= '9';

            private static int OnaltilikDeger(char c)
            {
                if (c >= '0' && c <= '9')
                {
                    return c - '0';
                }

                if (c >= 'a' && c <= 'f')
                {
                    return c - 'a' + 10;
                }

                if (c >= 'A' && c <= 'F')
                {
                    return c - 'A' + 10;
                }

                return -1;
            }
        }
    }
}
