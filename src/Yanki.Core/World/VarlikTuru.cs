namespace Yanki.Core.World
{
    /// <summary>Dünyadaki varlıkların türü (plan 5.2).</summary>
    public enum VarlikTuru
    {
        /// <summary>Derin simüle edilen karakter.</summary>
        Npc,

        /// <summary>Bağlantı grafiğinde bir düğüm.</summary>
        Mekan,

        /// <summary>Taşınabilir ya da yerinde duran nesne.</summary>
        Esya,

        /// <summary>Oyuncu: <c>konum!</c> gerçeği, fail/hedef ve fikir hedefi olabilir; zihni ve algısı yoktur, eşya sorgularında görünmez.</summary>
        Oyuncu,
    }
}
