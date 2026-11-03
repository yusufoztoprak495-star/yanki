using System;
using Yanki.Core.Json;

namespace Yanki.Core.Brakes
{
    /// <summary>Cooldown ve tetiklenme sınırlarının sayıldığı kapsam.</summary>
    public enum SinirKapsami
    {
        /// <summary>Kuralın bütün NPC'leri için tek sayaç (yalnızca kurallarda).</summary>
        Kural,

        /// <summary>(kural ya da eylem, NPC) çifti başına sayaç.</summary>
        Npc,

        /// <summary>(eylem, NPC, hedef) üçlüsü başına sayaç (yalnızca eylemlerde).</summary>
        Hedef,
    }

    /// <summary>
    /// Bir kuralın ya da eylemin cooldown ve tetiklenme sınırları (7. hafta, ADR-013). "Tetiklendi": kural için bütün kriterler tuttu ve
    /// en az bir etki üretildi; eylem için karar başına bir (konuşmadaki çoklu <c>anlatti</c>, hareketin <c>ayrildi</c> + <c>geldi</c> olayları tek sayılır).
    /// <see cref="Kapsam"/> üç sınırın hepsi için geçerlidir. 0 = tanımsız.
    /// </summary>
    public sealed class TetikSiniri
    {
        /// <summary>Son tetiklemeden sonra kaç tick kapalı kalınacağı (t=10, cooldown 24: t=33 kapalı, t=34 açık); 0 tanımsız.</summary>
        public long Cooldown { get; }

        /// <summary>Sayma kapsamı.</summary>
        public SinirKapsami Kapsam { get; }

        /// <summary>Tick başına en fazla tetikleme (yalnızca kurallar); 0 tanımsız.</summary>
        public int TickBasinaEnFazla { get; }

        /// <summary>Toplam en fazla tetikleme; 0 tanımsız.</summary>
        public int EnFazla { get; }

        /// <summary>Sınır tanımlı mı.</summary>
        public bool Tanimli => Cooldown > 0 || TickBasinaEnFazla > 0 || EnFazla > 0;

        /// <summary>Sayacın kanonik metne girmesi gerekir mi (yalnızca cooldown ya da en_fazla tanımlıysa).</summary>
        public bool KanonikMetneGirer => Cooldown > 0 || EnFazla > 0;

        /// <summary>Sınır kurar.</summary>
        public TetikSiniri(long cooldown, SinirKapsami kapsam, int tickBasinaEnFazla, int enFazla)
        {
            if (cooldown < 0 || tickBasinaEnFazla < 0 || enFazla < 0)
            {
                throw new ArgumentException("Sınırlar negatif olamaz.");
            }

            Cooldown = cooldown;
            Kapsam = kapsam;
            TickBasinaEnFazla = tickBasinaEnFazla;
            EnFazla = enFazla;
        }

        /// <summary>
        /// Kural ya da eylem öğesinin <c>cooldown</c>, <c>cooldown_kapsam</c>, <c>tick_basina_en_fazla</c> (yalnızca kural) ve <c>en_fazla</c>
        /// alanlarını okur ve doğrular; hiçbiri yoksa <c>null</c> (fren alanı olmayan tanımın davranışı değişmez).
        /// </summary>
        public static TetikSiniri? Oku(JsonDegeri oge, bool eylem, Action<int, string> hata)
        {
            var cooldown = TamSayi(oge, "cooldown", hata);
            var tick = eylem ? null : TamSayi(oge, "tick_basina_en_fazla", hata);
            var enFazla = TamSayi(oge, "en_fazla", hata);
            var kapsamDugumu = oge.Al("cooldown_kapsam");
            var kapsam = eylem ? SinirKapsami.Hedef : SinirKapsami.Npc;
            if (kapsamDugumu != null)
            {
                var izinli = eylem ? "npc | hedef" : "kural | npc";
                if (kapsamDugumu.Tur != JsonTuru.Metin
                    || !(kapsamDugumu.MetinDeger == "npc" || (eylem ? kapsamDugumu.MetinDeger == "hedef" : kapsamDugumu.MetinDeger == "kural")))
                {
                    hata(kapsamDugumu.Satir, $"\"cooldown_kapsam\" {izinli} olmalı");
                }
                else
                {
                    kapsam = kapsamDugumu.MetinDeger == "kural" ? SinirKapsami.Kural : kapsamDugumu.MetinDeger == "hedef" ? SinirKapsami.Hedef : SinirKapsami.Npc;
                }
            }

            if (cooldown == null && tick == null && enFazla == null)
            {
                if (kapsamDugumu != null)
                {
                    hata(kapsamDugumu.Satir, "\"cooldown_kapsam\" tek başına anlamsız: cooldown, en_fazla" + (eylem ? string.Empty : " ya da tick_basina_en_fazla") + " gerekli");
                }

                return null;
            }

            return new TetikSiniri(cooldown ?? 0, kapsam, (int)(tick ?? 0), (int)(enFazla ?? 0));
        }

        private static long? TamSayi(JsonDegeri oge, string anahtar, Action<int, string> hata)
        {
            var d = oge.Al(anahtar);
            if (d == null)
            {
                return null;
            }

            if (d.Tur != JsonTuru.Sayi || d.SayiDeger < 1m || d.SayiDeger != decimal.Truncate(d.SayiDeger) || d.SayiDeger > 1000000m)
            {
                hata(d.Satir, $"\"{anahtar}\" 1 ile 1000000 arasında bir tam sayı olmalı");
                return null;
            }

            return (long)d.SayiDeger;
        }
    }

    /// <summary>Senaryo düzeyindeki genel frenler (<c>frenler</c> nesnesi).</summary>
    public sealed class GenelFrenler
    {
        /// <summary>Hiç fren yok.</summary>
        public static GenelFrenler Yok { get; } = new GenelFrenler(null);

        /// <summary>Tick başına en fazla sonuç kuralı çıktısı (M); <c>null</c> sınırsız.</summary>
        public int? TickBasinaEnFazlaSonuc { get; }

        /// <summary>Kurar.</summary>
        public GenelFrenler(int? tickBasinaEnFazlaSonuc)
        {
            if (tickBasinaEnFazlaSonuc is < 1)
            {
                throw new ArgumentException("tick_basina_en_fazla_sonuc en az 1 olmalı.", nameof(tickBasinaEnFazlaSonuc));
            }

            TickBasinaEnFazlaSonuc = tickBasinaEnFazlaSonuc;
        }
    }
}
