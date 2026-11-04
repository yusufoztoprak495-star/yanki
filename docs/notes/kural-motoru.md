# Not: Kural motoru ve JSON okuyucu

> **Yazar: Claude Code** (Yusuf'un isteğiyle). **Karar (Yusuf)** satırları Yusuf'un kararlarıdır.

## Ne yapıyor?

Bir olay olunca, onu algılayan her NPC için kuralları değerlendirir. Tutan kuralların etkileri **yeni olay** olur. `KuralMotoru` durumu değiştirmez, `UretilenEtki` listesi döner. `Simulasyon` bunları `KuralId` ve `NedenOlaylar` taşıyan olaylara çevirip günlüğe ekler.

Gerçek bir kural (`samples/MiniHan/kurallar.json`):

```json
{
  "id": "tehdit_cesur_ofkesi",
  "ne_zaman": "tehdit_etti",
  "kriterler": [
    { "alan": "olay_rolu(hedef)", "op": "==", "deger": true },
    { "alan": "kisilik(cesaret)", "op": ">=", "deger": 0.7 }
  ],
  "etkiler": [
    { "tur": "modifier", "hedef": "olay.fail", "modifier": "tehdit_edildi", "deger": -20, "sure": 168 },
    { "tur": "duygu", "duygu": "ofke", "siddet": 0.5, "hedef": "olay.fail" }
  ]
}
```

- **Kriterler** ikinci bir yol değil, mevcut kriter dilidir (`KriterOkuyucu` → `Kriter.Olustur`).
- **Roller:** `ben`, `oyuncu`, `olay.fail`, `olay.hedef`. `konusan`/`dinleyen` diyaloğa aittir, kuralda reddedilir.
- **Etkiler (v0):** `modifier`, `eksen`, `duygu`, `hafiza`, `gercek`. Zihin etkileri `ben`'in zihnine yazılır. `gercek` metnindeki `{ben}`, `{oyuncu}`, `{olay.fail}`, `{olay.hedef}` çalışma anında doldurulur.
- **Doğrulayıcı v0** (`KuralYukleyici`): şema, kriter, olay türü, boş etki, tanımsız rol, henüz bağlı olmayan fonksiyon ve tekrar eden kimlik hatalarını toplar. Biçim: `dosya:satır: kural "id": mesaj`. `agirlik` ve `zorunlu: false` uyarı verir.

## Neden böyle?

- **Yeni davranış = yeni JSON.** Motor yalnızca beş etki türünü bilir, kod değişmez.
- **Çekirdek JSON okur, dosya okumaz.** Paket eklemek ADR-004'e aykırı olurdu, bu yüzden `Yanki.Core.Json` yazıldı: sayılar `decimal`, nesne sırası korunur, satır bilgisi hatalara akar (ADR-009). Dosya okuma CLI'dadır. Sınır testleri sırasında eksik olan 8 MiB boyut sınırı eklendi.

## Yusuf'un kararları (PR #7)

- **Karar (Yusuf):** Kural yalnızca **tüm kriterleri** tutunca çalışır. Kriteri olmayan kural her tetikleyicide çalışır. `agirlik` etkilemez ve uyarı verir (ADR-010).
- **Karar (Yusuf):** Hedef/tanık ayrımı `olay_rolu(fail|hedef|tanik)` ile yapılır. Rol o olaydan belirlenir, geçmiş inançlara bakmaz.
- **Karar (Yusuf):** Tek durum görüntüsü: bir adımdaki bütün kurallar adım başındaki durumu görür, böylece sıra sonucu değiştirmez.
- **Karar (Yusuf):** Gerçek etkisi tekrarsız yazılır. Durumu değiştirmiyorsa üretilmez, aynı adımda aynı gerçek tek kez yazılır.
- **Karar (Yusuf):** Tanımsız rol gerektiren kural o olay için atlanır. 7. haftada bunun için fren olayı yazılacak.
- **Karar (Yusuf):** Algı yalnızca dış olayları alır. Kural çıktıları sonraki tick'te algıya (dış olaysa) ve kurallara girer. Zihin olayını yalnızca sahibi algılar.
- **Karar (Yusuf):** Aynı NPC ve aynı olay için tek hafıza kaydı. `hafiza` etkisi önemi `max(eski, yeni)` yapar, değişiklik yine `hafiza_eklendi` olayıyla kaydedilir.

## Sonraki haftalara kalanlar

`cooldown`, `tick_basina_en_fazla`, `yay` koşulları, zincir derinliği sınırı, fren olayları ve korumalı bayraklar 7. haftada.

## Açık

- `hafiza` etkisi her zaman tetikleyici olayı hatırlatır, başka olay seçilemez.
- Etki hedefi senaryoda olmayan bir sabit kimlikse `Simulasyon.Olustur` reddeder.

## 7. hafta

`KuralMotoru.DegerlendirFrenli` saf kalır ve etkilerin yanında fren bilgisini döndürür (`KuralDegerlendirmesi`); olayları `Simulasyon` yazar. Sıra: kriterler tuttu → korumalı bayrak → zincir derinliği → etkiler denenir (tetiklendi = en az bir etki üretildi) → cooldown, `en_fazla`, `tick_siniri`, `genel_sinir`. Sayaçlar `TetikSayaclari` (günlükten türer) ve adım içi `Gorunum` ile okunur. Yeni etki türü `yay`. Sınırlı kuralın `gercek` olayına `ben` yükü eklenir (NPC bilgisi sayaç için).
