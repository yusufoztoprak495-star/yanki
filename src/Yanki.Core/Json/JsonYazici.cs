using System;
using System.Globalization;
using System.Text;

namespace Yanki.Core.Json
{
    /// <summary>
    /// JSON ağacını kanonik biçimde yazar: 2 boşluk girinti, özellikler verilen sırayla, sayılar kültürden bağımsız
    /// ve sondaki sıfırsız, metin sonu tek <c>\n</c>. Aynı ağaç her zaman aynı metni verir (golden dosyaları için).
    /// </summary>
    public static class JsonYazici
    {
        /// <summary>Değeri yazar; sonuna satır sonu eklenir.</summary>
        public static string Yaz(JsonDegeri deger)
        {
            if (deger == null)
            {
                throw new ArgumentNullException(nameof(deger));
            }

            var sb = new StringBuilder();
            Yaz(sb, deger, 0);
            sb.Append('\n');
            return sb.ToString();
        }

        private static void Yaz(StringBuilder sb, JsonDegeri d, int girinti)
        {
            switch (d.Tur)
            {
                case JsonTuru.Bos:
                    sb.Append("null");
                    break;
                case JsonTuru.Mantiksal:
                    sb.Append(d.MantiksalDeger ? "true" : "false");
                    break;
                case JsonTuru.Sayi:
                    sb.Append(SayiMetni(d.SayiDeger));
                    break;
                case JsonTuru.Metin:
                    MetinYaz(sb, d.MetinDeger);
                    break;
                case JsonTuru.Dizi:
                    if (d.Elemanlar.Count == 0)
                    {
                        sb.Append("[]");
                        break;
                    }

                    sb.Append("[\n");
                    for (var i = 0; i < d.Elemanlar.Count; i++)
                    {
                        sb.Append(' ', (girinti + 1) * 2);
                        Yaz(sb, d.Elemanlar[i], girinti + 1);
                        sb.Append(i + 1 < d.Elemanlar.Count ? ",\n" : "\n");
                    }

                    sb.Append(' ', girinti * 2).Append(']');
                    break;
                default:
                    if (d.Ozellikler.Count == 0)
                    {
                        sb.Append("{}");
                        break;
                    }

                    sb.Append("{\n");
                    for (var i = 0; i < d.Ozellikler.Count; i++)
                    {
                        sb.Append(' ', (girinti + 1) * 2);
                        MetinYaz(sb, d.Ozellikler[i].Key);
                        sb.Append(": ");
                        Yaz(sb, d.Ozellikler[i].Value, girinti + 1);
                        sb.Append(i + 1 < d.Ozellikler.Count ? ",\n" : "\n");
                    }

                    sb.Append(' ', girinti * 2).Append('}');
                    break;
            }
        }

        private static string SayiMetni(decimal deger)
        {
            // Çarpmak ondalık ölçeği değiştirmeden değeri korur; bölmek sondaki sıfırları atar.
            var yalin = deger / 1.0000000000000000000000000000m;
            return yalin == 0m ? "0" : yalin.ToString(CultureInfo.InvariantCulture);
        }

        private static void MetinYaz(StringBuilder sb, string metin)
        {
            sb.Append('"');
            foreach (var c in metin)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            sb.Append('"');
        }
    }
}
