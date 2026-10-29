# Events

Olay kaydı: yalnızca sonuna eklenen, değişmez günlük. Her olay `Id`, `Tick`, `NedenOlaylar[]` ve `KuralId` taşır (plan 5.1, [ADR-001](../../../docs/adr/0001-olay-kaynakli-cekirdek.md)).
Durum bu günlükten türetilir; "bu neden oldu?" sorusu `NedenOlaylar` zinciri geriye izlenerek cevaplanır.

**Durum:** 2. hafta teslimi tamam. Tick sonu hash'i ve kayıt/yükleme sonraki haftalarda (4. ve 7. hafta).

## Tipler

| Tip           | Ne yapar                                                                                                                           |
| ------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| `Olay`        | Değişmez kayıt: `Id` (1'den), `Tick`, `Sira` (tick içi, 0'dan), `Tur`, `Fail`, `Hedef`, `Mekan`, `Yuk`, `NedenOlaylar`, `KuralId`. |
| `OlayGunlugu` | Yalnızca `Ekle` ile büyür. Eklerken doğrular. `Neden(id)` nedensellik zincirini döndürür.                                          |

## Kurallar

- `Fail`, `Hedef`, `Mekan` boş olabilir (`""`; `null` verilirse `""` olur). `Tur` ve `KuralId` boş olamaz; oyuncu eylemlerinde `KuralId` = `"player"`.
- `Yuk`: anahtara göre **ordinal sıralı**, anahtarlar tekil. `NedenOlaylar`: artan sıralı, tekrarsız. İkisi de oluşturulurken kopyalanır; sonradan değiştirilemez.
- `OlayGunlugu.Ekle` şunları denetler ve ihlalde açık mesajlı `InvalidOperationException` fırlatır:
  - `Id` ardışık (`SonrakiId`),
  - `Tick` geriye gidemez,
  - `Sira` yeni tick'te 0, aynı tick'te bir öncekinin tam bir fazlası,
  - `NedenOlaylar` içindeki her kimlik günlükte zaten var (yani yeni olaydan önce).
- `Neden(olayId)`: olaya doğrudan ya da dolaylı yol açan tüm olaylar, **Id azalan sırada**, tekrarsız; olayın kendisi hariç. (Nedenler her zaman daha küçük `Id`'lidir; bu sıra geçerli bir ters topolojik sıradır.)

## Örnek

```csharp
var gunluk = new OlayGunlugu();
gunluk.Ekle(new Olay(1, 0, 0, "oyuncu_yalan_soyledi", "Oyuncu1", "Ali", "han",
    new KeyValuePair<string, string>[0], new long[0], "player"));
gunluk.Ekle(new Olay(2, 0, 1, "fikir_dustu", "Ali", "Oyuncu1", "han",
    new KeyValuePair<string, string>[0], new long[] { 1 }, "yalan_tepkisi"));

var nedenler = gunluk.Neden(2); // [#1]
```

## Bu hafta kapsam dışı

Tick döngüsü, olay yayını, durum hash'inin tick sonunda alınması, kayıt/yükleme, olay türlerinin kataloğu (sonuç kuralları 4. haftada).
