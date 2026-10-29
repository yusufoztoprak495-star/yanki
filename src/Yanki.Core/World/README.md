# World

Dünya: varlıklar (NPC, mekan, eşya), mekan grafiği ve gerçekler (tek değerli `!` gerçekler dahil). Ayrıca durum hash'i ve tek, anahtarlı RNG (plan 5.2, 5.1).
Kimin ne bildiği burada değil, Zihin'de tutulur (3. hafta).

**Durum:** 2. hafta teslimi tamam. Korumalı bayraklar ve yay değişkenleri sonra (7. hafta); zihin 3. hafta.

## Kimlikler ASCII'dir

Varlık kimlikleri, yüklem adları ve gerçek argümanları `[A-Za-z_][A-Za-z0-9_]*` biçimindedir (sayı argümanları hariç, aşağıya bak).
Türkçe karakter, boşluk ve `( ) , ! * |` kimliklerde yoktur; bu, kanonik serileştirmenin ve hash'in kodlama ya da ayraç sorunu
yaşamamasını sağlar. **Oyuncuya görünen ad (Türkçe karakterli, ör. "Ayşe") ileride ayrı bir alan olacak; kimlik ile görünen ad ayrıdır.**
Bu hafta o alan yoktur ve eklenmedi.

## Tipler

| Tip             | Ne yapar                                                                                                                                                           |
| --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `Dunya`         | Durum. `Uygula(olay)` ile ilerler; `DurumHash()` hex hash verir. Sorgular: `VarlikVar`, `TurOf`, `Varliklar`, `Komsular`, `BagliMi`, `Var`, `Eslesen`, `TekDeger`. |
| `Gercek`        | `yuklem(arg, ...)`. Yüklem `!` ile bitiyorsa tek değerlidir.                                                                                                       |
| `GercekDeseni`  | Joker (`null` argüman) destekli eşleştirme: `borclu(Mehmet, *, *)`.                                                                                                |
| `VarlikTuru`    | `Npc`, `Mekan`, `Esya`.                                                                                                                                            |
| `AnahtarliRng`  | `Icin(seed, tick, anahtarlar...)`; `SonrakiUlong`, `Aralik(min, maxHaric)`, `SonrakiDecimal01`.                                                                    |
| `Hash64`        | Kendi 64 bit hash'imiz: FNV-1a + SplitMix64 bitirme karıştırıcısı.                                                                                                 |
| `KanonikYazici` | `decimal` için tek kanonik yazım (aşağıya bak).                                                                                                                    |

## Dünya olayları

`Dunya.Uygula(olay)` yalnızca şu `Tur`'leri işler; başkasını yoksayar ve `false` döner (tüm günlük verilebilsin diye):

| `Tur`               | `Yuk`                                  | Etki                                                                                    |
| ------------------- | -------------------------------------- | --------------------------------------------------------------------------------------- |
| `varlik_eklendi`    | `id`, `tur` (`npc` / `mekan` / `esya`) | Varlık eklenir; var olanı eklemek hata.                                                 |
| `gercek_eklendi`    | `gercek` (örn. `konum!(Ali,han)`)      | Gerçek eklenir (aynısı tekrar eklenirse etkisiz). Tek değerliyse eski değer düşer.      |
| `gercek_kaldirildi` | `gercek`                               | Gerçek kaldırılır; olmayanı kaldırmak hata.                                             |
| `mekan_baglandi`    | `a`, `b`                               | İki **mekan** yönsüz bağlanır (tekrar etkisiz; kendine bağlantı ve mekan olmayan hata). |

**Tek değerli gerçek:** anahtar = yüklem + son argüman hariç tüm argümanlar. `konum!(Ali, han)` yazılınca `konum!(Ali, pazar)` otomatik düşer.

## Determinizm

- Koleksiyonlar `SortedDictionary`/`SortedSet` + `StringComparer.Ordinal`; sırasız `Dictionary`/`HashSet` yok (test denetler).
- Sayılar `decimal` ([ADR-007](../../../docs/adr/0007-sayisal-temsil-decimal.md)). Kanonik yazım: `MidpointRounding.ToEven` ile 6 ondalık basamak, sondaki sıfırlar kırpılır, `InvariantCulture` (`0.0000025` → `0.000002`, `500` → `500`).
- **Durum hash'i:** kanonik metin (`YANKI-DURUM|<sürüm>`, sonra `V|id|tur`, `M|a|b` (a<b), `G|gercek` satırları; her grup ordinal sıralı) → UTF-8 → `Hash64`. Aynı durum, nasıl oluştuğundan bağımsız aynı hash'i verir; tek değerli gerçeğin geçmiş değerleri hash'i etkilemez.
- **Anahtarlı RNG:** tohum = `Hash64(seed(8 bayt) + tick(8 bayt) + her anahtar için uzunluk(4 bayt) + UTF-8)`; akış SplitMix64. Aynı girdi aynı akış; başka akışların çağrı sırası sonucu etkilemez. `Aralik` red örneklemesi kullanır (modulo yanlılığı yok). `SonrakiDecimal01` = 53 bit / 2^53, `[0, 1)`.

## Örnek

```csharp
var dunya = new Dunya();
dunya.Uygula(new Olay(1, 0, 0, "varlik_eklendi", "", "", "",
    new[] { new KeyValuePair<string, string>("id", "Ali"), new KeyValuePair<string, string>("tur", "npc") },
    new long[0], "kurulum"));
var hash = dunya.DurumHash(); // 16 karakter, küçük harf hex

var rng = AnahtarliRng.Icin(seed: 42UL, tick: 7, "Ali", "karar");
int secim = rng.Aralik(0, 3);
```

## Bu hafta kapsam dışı

Korumalı bayraklar, yay değişkenleri, zihin (kişilik, ilişki, inanç, hafıza), algı, görünen ad alanı.
