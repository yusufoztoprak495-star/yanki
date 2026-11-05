using System;
using System.Collections.Generic;
using Yanki.Core.Criteria;
using Yanki.Core.Json;

namespace Yanki.Core.Brakes
{
    /// <summary>
    /// Korumalı bayraklar (kapalı küme, plan 5.2: üç bayrak). <c>mekandan_ayrilamaz</c> yerleşik harekete (ve <c>konum!</c> gerçeğinin yazımına)
    /// motor tarafından uygulanır, yazar bildirmez. <c>oldurulemez</c> ve <c>calinamaz</c> YALNIZCA <c>ihlal_bayraklari</c> bildiren eylem ve
    /// kurallarda uygulanır: motorda yerleşik öldürme/çalma eylemi yoktur, bildirmeyi unutan yazarı motor yakalayamaz (ADR-013).
    /// </summary>
    public static class KorumaBayraklari
    {
        /// <summary>Öldürülemez.</summary>
        public const string Oldurulemez = "oldurulemez";

        /// <summary>Çalınamaz.</summary>
        public const string Calinamaz = "calinamaz";

        /// <summary>Bulunduğu mekandan ayrılamaz.</summary>
        public const string MekandanAyrilamaz = "mekandan_ayrilamaz";

        /// <summary>Kapalı kümede mi.</summary>
        public static bool Gecerli(string bayrak) => bayrak == Oldurulemez || bayrak == Calinamaz || bayrak == MekandanAyrilamaz;

        /// <summary>Yazarın <c>ihlal_bayraklari</c> ile bildirebileceği bayrak mı (<c>mekandan_ayrilamaz</c> motorundur).</summary>
        public static bool YazarBildirebilir(string bayrak) => bayrak == Oldurulemez || bayrak == Calinamaz;
    }

    /// <summary>Senaryodaki korumalı varlıklar (<c>korumali</c>): varlık kimliği → bayraklar.</summary>
    public sealed class Korumalilik
    {
        /// <summary>Hiçbir varlık korumalı değil.</summary>
        public static Korumalilik Yok { get; } = new Korumalilik(new KeyValuePair<string, IEnumerable<string>>[0]);

        private readonly SortedDictionary<string, SortedSet<string>> _harita = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        /// <summary>Kurar; bayraklar geçerli olmalıdır (denetim yükleyicidedir).</summary>
        public Korumalilik(IEnumerable<KeyValuePair<string, IEnumerable<string>>> girdi)
        {
            foreach (var kv in girdi ?? throw new ArgumentNullException(nameof(girdi)))
            {
                _harita[kv.Key] = new SortedSet<string>(kv.Value, StringComparer.Ordinal);
            }
        }

        /// <summary>Korumalı varlık kimlikleri, ordinal sıralı.</summary>
        public IEnumerable<string> Varliklar => _harita.Keys;

        /// <summary>Varlığın bayrakları, ordinal sıralı; korumalı değilse boş.</summary>
        public IEnumerable<string> Bayraklar(string varlik) => _harita.TryGetValue(varlik, out var b) ? b : (IEnumerable<string>)new string[0];

        /// <summary>Varlık bu bayrakla korumalı mı.</summary>
        public bool Korunuyor(string varlik, string bayrak) => varlik != null && _harita.TryGetValue(varlik, out var b) && b.Contains(bayrak);

        /// <summary>Korumalı varlık yok mu.</summary>
        public bool Bos => _harita.Count == 0;
    }

    /// <summary>
    /// Eylemin, kuralın ya da oyuncu eyleminin hangi bayrağı hangi varlık için ihlal edebileceği (<c>ihlal_bayraklari</c>).
    /// <c>Varlik</c>: bağlamdaki bir rol (<c>eylem.hedef</c>, <c>ben</c>, <c>olay.hedef</c>, <c>olay.fail</c>) ya da sabit kimlik.
    /// </summary>
    public sealed class IhlalBayragi
    {
        /// <summary><see cref="KorumaBayraklari"/> değerlerinden biri.</summary>
        public string Bayrak { get; }

        /// <summary>Rol adı ya da sabit varlık kimliği.</summary>
        public string Varlik { get; }

        /// <summary>Kaynak dosyadaki satır; bilinmiyorsa 0.</summary>
        public int Satir { get; }

        /// <summary>Kurar.</summary>
        public IhlalBayragi(string bayrak, string varlik, int satir = 0)
        {
            Bayrak = bayrak;
            Varlik = varlik;
            Satir = satir;
        }

        /// <summary>Varlık bir rolse rolü; sabit kimlikse <c>null</c>.</summary>
        public Rol? Rolu => RolAdlari.TryCoz(Varlik, out var r) ? r : (Rol?)null;

        /// <summary>Varlık kimliğini bağlama göre çözer; çözülemeyen rol için boş döner.</summary>
        public string Coz(string ben, string eylemHedef, string olayFail, string olayHedef)
        {
            switch (Rolu)
            {
                case Rol.Ben: return ben;
                case Rol.EylemHedef: return eylemHedef;
                case Rol.OlayFail: return olayFail;
                case Rol.OlayHedef: return olayHedef;
                case null: return Varlik;   // sabit kimlik
                default:
                    // Hiçbir yükleyici bu rollere (oyuncu, konusan, dinleyen) izin vermez; elle kurulan nesnede boş kimlik dönmek korumayı SESSİZCE kapatırdı.
                    throw new InvalidOperationException($"ihlal_bayraklari varlığı \"{Varlik}\" bu bağlamda çözülemeyen bir rol (geçerli roller: ben, eylem.hedef, olay.fail, olay.hedef).");
            }
        }

        /// <summary>
        /// <c>ihlal_bayraklari</c> dizisini okur ve doğrular. <paramref name="izinliRoller"/> bu bağlamda geçerli rol adlarıdır; başka yazım sabit kimlik sayılır.
        /// Dizi yoksa boş liste döner.
        /// </summary>
        public static IReadOnlyList<IhlalBayragi> OkuDizi(JsonDegeri oge, string anahtar, ICollection<string> izinliRoller, Action<int, string> hata)
        {
            var sonuc = new List<IhlalBayragi>();
            var dugum = oge.Al(anahtar);
            if (dugum == null)
            {
                return sonuc;
            }

            if (dugum.Tur != JsonTuru.Dizi)
            {
                hata(dugum.Satir, $"\"{anahtar}\" bir dizi olmalı (her eleman {{ \"bayrak\", \"varlik\" }})");
                return sonuc;
            }

            for (var i = 0; i < dugum.Elemanlar.Count; i++)
            {
                var e = dugum.Elemanlar[i];
                var onek = $"{anahtar} #{i + 1}: ";
                if (e.Tur != JsonTuru.Nesne)
                {
                    hata(e.Satir, onek + "{ \"bayrak\", \"varlik\" } nesnesi bekleniyor");
                    continue;
                }

                var tamam = true;
                foreach (var kv in e.Ozellikler)
                {
                    if (kv.Key != "bayrak" && kv.Key != "varlik")
                    {
                        hata(kv.Value.Satir, onek + $"bilinmeyen alan \"{kv.Key}\" (bayrak, varlik)");
                        tamam = false;
                    }
                }

                var b = e.Al("bayrak");
                var v = e.Al("varlik");
                if (b == null || b.Tur != JsonTuru.Metin || !KorumaBayraklari.Gecerli(b.MetinDeger))
                {
                    hata(b?.Satir ?? e.Satir, onek + "\"bayrak\" oldurulemez, calinamaz ya da mekandan_ayrilamaz olmalı");
                    tamam = false;
                }
                else if (!KorumaBayraklari.YazarBildirebilir(b.MetinDeger))
                {
                    hata(b.Satir, onek + $"\"{b.MetinDeger}\" yerleşik harekete motor tarafından uygulanır, yazar bildirmez (oldurulemez, calinamaz)");
                    tamam = false;
                }

                if (v == null || v.Tur != JsonTuru.Metin || v.MetinDeger.Length == 0)
                {
                    hata(v?.Satir ?? e.Satir, onek + "\"varlik\" gerekli (rol ya da varlık kimliği)");
                    tamam = false;
                }
                else if (RolAdlari.TryCoz(v.MetinDeger, out _))
                {
                    if (!izinliRoller.Contains(v.MetinDeger))
                    {
                        hata(v.Satir, onek + $"\"{v.MetinDeger}\" bu bağlamda geçersiz (geçerli roller: {string.Join(", ", izinliRoller)}; ya da sabit kimlik)");
                        tamam = false;
                    }
                }
                else if (!Yanki.Core.World.Kimlik.GecerliKimlikMi(v.MetinDeger))
                {
                    hata(v.Satir, onek + $"\"varlik\" geçersiz kimlik: \"{v.MetinDeger}\"");
                    tamam = false;
                }

                if (tamam)
                {
                    sonuc.Add(new IhlalBayragi(b!.MetinDeger, v!.MetinDeger, e.Satir));
                }
            }

            return sonuc;
        }
    }
}
