# Criteria

Ortak kriter dili: Valve tarzı ağırlıklı kriterlerin ayrıştırıcısı, doğrulayıcısı ve puanlayıcısı. Sonuç kuralları, diyalog şablonları ve eylem ön koşulları aynı yapıyı kullanır (plan 5.9, [ADR-003](../../../docs/adr/0003-birlesik-kriter-dili.md)). Dil referansı ve 15 fonksiyonluk sözlük: [`docs/criteria-language.md`](../../../docs/criteria-language.md).

**Durum:** 2. hafta teslimi tamam. Kriterler bu hafta **C# ile kurulur**; JSON okuma yok. Kural motoru 4. hafta.

## Akış

```
"gercek(konum!(Ali, han))"  --IfadeAyristirici-->  SorguCagrisi  --SorguSozlugu.Dogrula-->  Kriter.Olustur(op, deger, agirlik, zorunlu)
Kriter + ISorguCozucu + SorguBaglami  --KriterDegerlendirici-->  KriterSonucu (tuttu mu, gözlenen, katkı)
Aday<T>[] + çözücü + bağlam + AnahtarliRng  --Secici.EnIyiSec-->  SecimSonucu<T> (seçilen + eşleşme izleri)
```

## Tipler

| Tip                         | Ne yapar                                                                                                                                                                                                                                                                   |
| --------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IfadeAyristirici.Ayristir` | `fonksiyon(arg, ...)` ifadesini ayrıştırır. Hata `KriterHatasi` olur ve karakter konumunu (`konum N`) taşır.                                                                                                                                                               |
| `SorguSozlugu`              | 15 fonksiyonun adı, argüman sayısı, dönüş türü (sayı / mantıksal / varlık / bant) ve önerme alan argümanı. `Dogrula` bilinmeyen fonksiyonu, yanlış argüman sayısını, yanlış yerdeki önermeyi/jokeri reddeder.                                                              |
| `Kriter`                    | `alan`, `op`, `deger`, `agirlik` (varsayılan 1), `zorunlu` (varsayılan `false`). `Olustur` ifadeyi doğrular; operatör ve değer dönüş türüne uymuyorsa (örn. mantıksal değerde `<`) hata verir.                                                                             |
| `SorguDegeri`               | Sayı (`decimal`), mantıksal, metin (varlık/bant) ya da aralık.                                                                                                                                                                                                             |
| `SorguBaglami`              | `ben`, `oyuncu`, `konusan`, `dinleyen`, `olay.fail`, `olay.hedef` rollerinin varlık kimlikleri. Tanımsız rol kullanılırsa açık hata.                                                                                                                                       |
| `ISorguCozucu`              | Sorgu çağrılarını gerçek değerlere çözer.                                                                                                                                                                                                                                  |
| `DunyaSorguCozucusu`        | Bu hafta yalnızca dünyaya dayanan `gercek` ve `ayni_mekanda`'yı çözer (`konum!` gerçeklerinden). Diğer 13 fonksiyon `NotSupportedException`; onları zihin gelince (3. hafta) başka çözücüler sağlar. Testlerde sahte çözücü kullanılır.                                    |
| `Secici`                    | `Puanla`: puan = tutan kriterlerin ağırlık toplamı; tutmayan `zorunlu` kriter adayı eler (tüm kriterler yine de değerlendirilir). `EnIyiSec`: en yüksek puan kazanır; puan eşitse daha çok kriteri tutan (daha özgül) aday kazanır (ADR-003); o da eşitse adaylar kimliğe göre sıralanıp anahtarlı RNG ile biri seçilir (girdi sırasından bağımsız). |
| `EslesmeIzi`                | Her adayda hangi kriter tuttu, hangisi tutmadı; ileride `why` aracı kullanır.                                                                                                                                                                                              |

## Kurallar

- Fonksiyon adları ve JSON anahtarları ASCII'dir (`docs/criteria-language.md`).
- `aralik` uçları dahildir. Mantıksal, varlık ve bant dönüşlerinde yalnızca `==` ve `!=` geçerlidir. `fikir_bandi` değeri `dusman`, `soguk`, `notr`, `yakin`, `dost` olmalıdır.
- Önerme (`borclu(Mehmet, *, *)`, `konum!(Ali, han)`) yalnızca `inanc`, `kaynak` (ilk argüman) ve `gercek` içinde kullanılır; önerme içinde önerme ve önerme dışında `*` yoktur.
- Kod sözlüğü `docs/criteria-language.md` tablosuyla birebir olmalıdır: bir test tabloyu okuyup ad, argüman sayısı, dönüş türü ve örnek kriteri koddaki tanımla karşılaştırır.

## Örnek

```csharp
var kriterler = new[]
{
    Kriter.Olustur("ayni_mekanda(oyuncu)", KriterOperatoru.Esit, SorguDegeri.Mantiksal(true), agirlik: 2m, zorunlu: true),
    Kriter.Olustur("gercek(borclu(Mehmet, *, *))", KriterOperatoru.Esit, SorguDegeri.Mantiksal(true)),
};
var adaylar = new[] { new Aday<string>("sorgula", "sorgula", kriterler) };

var sonuc = Secici.EnIyiSec(adaylar, new DunyaSorguCozucusu(dunya),
    new SorguBaglami("Ali", oyuncu: "Oyuncu1"), AnahtarliRng.Icin(42UL, 7, "Ali", "karar"));
```

## Bu hafta kapsam dışı

JSON okuma, sonuç kuralı motoru, zihin/hafıza/yay tabanlı 13 fonksiyonun çözümü, kapsama aracı, `yanki validate`.
