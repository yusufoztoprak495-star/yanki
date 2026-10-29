using System;

namespace Yanki.Core.Criteria
{
    /// <summary>
    /// Bir kriterin hangi varlıklar için değerlendirildiği: <c>ben</c>, <c>oyuncu</c>, <c>konusan</c>, <c>dinleyen</c>,
    /// <c>olay.fail</c>, <c>olay.hedef</c> rollerinin varlık kimlikleri. Tanımsız roller boş bırakılır.
    /// </summary>
    public sealed class SorguBaglami
    {
        private readonly string _ben;
        private readonly string _oyuncu;
        private readonly string _konusan;
        private readonly string _dinleyen;
        private readonly string _olayFail;
        private readonly string _olayHedef;

        /// <summary>Bağlam kurar. <paramref name="ben"/> zorunludur; diğer roller yoksa boş bırakılır.</summary>
        /// <exception cref="ArgumentException"><paramref name="ben"/> boşsa.</exception>
        public SorguBaglami(
            string ben,
            string oyuncu = "",
            string konusan = "",
            string dinleyen = "",
            string olayFail = "",
            string olayHedef = "")
        {
            if (string.IsNullOrEmpty(ben))
            {
                throw new ArgumentException("'ben' rolü (değerlendiren NPC) boş olamaz.", nameof(ben));
            }

            _ben = ben;
            _oyuncu = oyuncu ?? string.Empty;
            _konusan = konusan ?? string.Empty;
            _dinleyen = dinleyen ?? string.Empty;
            _olayFail = olayFail ?? string.Empty;
            _olayHedef = olayHedef ?? string.Empty;
        }

        /// <summary>Rolün varlık kimliği.</summary>
        /// <exception cref="InvalidOperationException">Rol bu bağlamda tanımlı değilse.</exception>
        public string Coz(Rol rol)
        {
            string kimlik;
            switch (rol)
            {
                case Rol.Ben: kimlik = _ben; break;
                case Rol.Oyuncu: kimlik = _oyuncu; break;
                case Rol.Konusan: kimlik = _konusan; break;
                case Rol.Dinleyen: kimlik = _dinleyen; break;
                case Rol.OlayFail: kimlik = _olayFail; break;
                default: kimlik = _olayHedef; break;
            }

            if (kimlik.Length == 0)
            {
                throw new InvalidOperationException($"Rol '{RolAdlari.Ad(rol)}' bağlamda tanımlı değil.");
            }

            return kimlik;
        }
    }
}
