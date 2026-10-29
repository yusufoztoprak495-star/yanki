using System;

namespace Yanki.Core.Mind
{
    /// <summary>
    /// Bütün sönmeler saf fonksiyondur: <c>(durum, simdikiTick) → değer</c> (ADR-008). Hiçbir şey saklanmaz ya da değiştirilmez;
    /// değer her sorguda tick'ten hesaplanır. Oranlar <see cref="ZihinAyarlari"/>'ndandır. Tick döngüsüne bağlamak 4. haftanın işi.
    /// </summary>
    public sealed class Sonumleme
    {
        private readonly ZihinAyarlari _ayar;

        /// <summary>Verilen ayarlarla sönme hesaplayıcısı kurar.</summary>
        public Sonumleme(ZihinAyarlari ayar)
        {
            _ayar = ayar ?? throw new ArgumentNullException(nameof(ayar));
        }

        /// <summary>Modifier bu tick'te aktif mi: <c>[başlangıç, başlangıç + süre)</c>.</summary>
        public bool ModifierAktifMi(FikirModifier modifier, long simdikiTick)
        {
            Bos(modifier, nameof(modifier));
            return simdikiTick >= modifier.BaslangicTick && simdikiTick < modifier.BaslangicTick + modifier.Sure;
        }

        /// <summary>Modifier'ın bitmesine kalan tick; bittiyse 0.</summary>
        public long ModifierKalan(FikirModifier modifier, long simdikiTick)
        {
            Bos(modifier, nameof(modifier));
            return Math.Max(0L, modifier.BaslangicTick + modifier.Sure - simdikiTick);
        }

        /// <summary>Duygunun bu tick'teki şiddeti: doğrusal söner, 0'ın altına inmez.</summary>
        public decimal DuyguSiddeti(Duygu duygu, long simdikiTick)
        {
            Bos(duygu, nameof(duygu));
            return Azalt(duygu.Siddet, _ayar.DuyguSonumuTickBasina, duygu.BaslangicTick, simdikiTick);
        }

        /// <summary>Stresin bu tick'teki değeri: doğrusal söner, 0'ın altına inmez.</summary>
        public decimal Stres(StresDurumu durum, long simdikiTick)
        {
            Bos(durum, nameof(durum));
            return Azalt(durum.Deger, _ayar.StresSonumuTickBasina, durum.Tick, simdikiTick);
        }

        /// <summary>Kanıtın bu tick'teki gücü. Kanıt silinmez; yalnızca gücü azalır.</summary>
        public decimal KanitGucu(Kanit kanit, long simdikiTick)
        {
            Bos(kanit, nameof(kanit));
            return Azalt(kanit.Guc, _ayar.KanitSonumuTickBasina, kanit.Tick, simdikiTick);
        }

        /// <summary>Hafıza kalıcı mı: önemi kalıcılık eşiğinin üstünde (eşit değil).</summary>
        public bool HafizaKaliciMi(HafizaKaydi kayit)
        {
            Bos(kayit, nameof(kayit));
            return kayit.Onem > _ayar.HafizaKaliciEsigi;
        }

        /// <summary>Hafızanın bu tick'teki önemi; kalıcıysa değişmez, değilse doğrusal silikleşir.</summary>
        public decimal HafizaOnemi(HafizaKaydi kayit, long simdikiTick)
        {
            Bos(kayit, nameof(kayit));
            return HafizaKaliciMi(kayit)
                ? kayit.Onem
                : Azalt(kayit.Onem, _ayar.HafizaSonumuTickBasina, kayit.Tick, simdikiTick);
        }

        /// <summary>Hafıza sorgularda görünür mü: kalıcıysa her zaman; değilse önemi unutma eşiğinin üstünde.</summary>
        public bool HafizaAktifMi(HafizaKaydi kayit, long simdikiTick) =>
            HafizaKaliciMi(kayit) || HafizaOnemi(kayit, simdikiTick) > _ayar.HafizaUnutmaEsigi;

        private static decimal Azalt(decimal baslangic, decimal oran, long baslangicTick, long simdikiTick)
        {
            var gecen = Math.Max(0L, simdikiTick - baslangicTick);
            return Math.Max(0m, baslangic - oran * gecen);
        }

        private static void Bos(object? nesne, string ad)
        {
            if (nesne == null)
            {
                throw new ArgumentNullException(ad);
            }
        }
    }
}
