namespace Yanki.Dialogue
{
    /// <summary>
    /// Bir niyetin gerekçesi (B kararı): konuşanın GÖSTERMEYE İZİNLİ verilerinden oluşur, karar izinden DEĞİL (karar izi seçilen eylemi içerir; doğru ve yalan anlatım farklı
    /// eylemdir, yani sızıntı olurdu). Yusuf'un düzeltmesi: <c>anlatti</c> gerekçesi yalnızca fikir bandı ve baskın duygudur; hafıza olayı ve kaynak gerekçeye girmez,
    /// çünkü gören kişinin hafızası vardır, yalancının yoktur.
    /// </summary>
    public sealed class NiyetGerekcesi
    {
        internal NiyetGerekcesi(string fikirBandi, string baskinDuygu)
        {
            FikirBandi = fikirBandi;
            BaskinDuygu = baskinDuygu;
        }

        /// <summary>Konuşanın dinleyene karşı fikir bandı (dusman, soguk, notr, yakin, dost).</summary>
        public string FikirBandi { get; }

        /// <summary>Konuşanın baskın duygusunun türü; duygu yoksa ya da söndüyse <c>yok</c>.</summary>
        public string BaskinDuygu { get; }
    }

    /// <summary>
    /// Bir olaydan türeyen niyet (A ve B kararları): konuşan, dinleyen, niyet (concept), konu (önerme, varsa), ton, fikir bandı, gerekçe ve olay kimliği. Niyet çekirdeğin KARAR
    /// modülünden değil, karar sonucu OLAYDAN türer (plandan sapma, ADR-014). Yalan gizliliği (ADR-012): <c>KuralId</c>, eylem kimliği, kök, karar izi ve aktarım kaydı niyette YOKTUR;
    /// doğru ve yalan anlatım alan alan aynı biçimdedir.
    /// </summary>
    public sealed class Niyet
    {
        internal Niyet(
            string konusan, string dinleyen, string ad, string konu, string ton, string fikirBandi, NiyetGerekcesi gerekce, long olayId, long tick, bool followup)
        {
            Konusan = konusan;
            Dinleyen = dinleyen;
            Ad = ad;
            Konu = konu;
            Ton = ton;
            FikirBandi = fikirBandi;
            Gerekce = gerekce;
            OlayId = olayId;
            Tick = tick;
            Followup = followup;
        }

        /// <summary>Konuşan NPC.</summary>
        public string Konusan { get; }

        /// <summary>Dinleyen (NPC ya da oyuncu).</summary>
        public string Dinleyen { get; }

        /// <summary>Niyet adı (concept).</summary>
        public string Ad { get; }

        /// <summary>Konu önermesi (ör. <c>borclu(Mehmet,Ayse,500)</c>); konusuz niyette boş.</summary>
        public string Konu { get; }

        /// <summary>Baskın duygudan türeyen ton (tablo <see cref="DiyalogAyarlari.TonTablosu"/>).</summary>
        public string Ton { get; }

        /// <summary>Konuşanın dinleyene karşı fikir bandı (mevcut <c>fikir_bandi</c> sorgusu).</summary>
        public string FikirBandi { get; }

        /// <summary>Gösterilmesine izin verilen gerekçe.</summary>
        public NiyetGerekcesi Gerekce { get; }

        /// <summary>Niyeti doğuran olayın günlük kimliği.</summary>
        public long OlayId { get; }

        /// <summary>Niyeti doğuran olayın tick'i.</summary>
        public long Tick { get; }

        /// <summary>Ana satırın followup'ından (cevap) türeyen niyet mi.</summary>
        public bool Followup { get; }
    }
}
