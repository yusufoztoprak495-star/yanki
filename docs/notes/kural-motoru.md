# Not: Sonuç kuralları (kural motoru) ve JSON okuyucu

> **Taslak (Claude Code).** Yusuf bunu kendi cümleleriyle yeniden yazacak. Aşağıdakiler "ne yaptım, neden" iskeletidir.

## Bu modül ne yapıyor?

Bir olay olduğunda, onu algılayan her NPC için kuralları değerlendirir; tutan kuralların **etkilerini yeni olaylar olarak** üretir (plan 5.6).
Etki doğrudan durumu değiştirmez: `KuralMotoru` saf bir değerlendiricidir ve `UretilenEtki` listesi döndürür; `Simulasyon` bunları `KuralId` = kural kimliği,
`NedenOlaylar` = tetikleyici olay (+ o NPC'nin algı olayı) olan olaylara çevirip günlüğe ekler.

Kural dosyası (`kurallar.json`):

```json
{
  "kurallar": [
    {
      "id": "yardim_minneti",
      "ne_zaman": "yardim_etti",
      "kriterler": [
        { "alan": "olay_rolu(hedef)", "op": "==", "deger": true },
        { "alan": "fikir(olay.fail)", "op": "<", "deger": 50 }
      ],
      "etkiler": [
        {
          "tur": "modifier",
          "hedef": "olay.fail",
          "modifier": "minnet",
          "deger": 15,
          "sure": 336
        },
        { "tur": "eksen", "hedef": "olay.fail", "eksen": "guven", "delta": 5 },
        {
          "tur": "duygu",
          "duygu": "minnet",
          "siddet": 0.5,
          "hedef": "olay.fail"
        },
        { "tur": "hafiza", "onem": 0.8 },
        {
          "tur": "gercek",
          "islem": "ekle",
          "gercek": "yardim_gordu({ben}, {olay.fail})"
        }
      ]
    }
  ]
}
```

- **Kriterler** mevcut kriter dilidir (`docs/criteria-language.md`): `KriterOkuyucu` JSON'u `Kriter.Olustur`'a verir, `KriterDegerlendirici` her kriteri değerlendirir; kural yalnızca hepsi tutunca çalışır (ADR-010). İkinci bir kriter yolu yoktur.
- **Roller:** kural bağlamında `ben` (kuralı değerlendiren NPC), `oyuncu`, `olay.fail`, `olay.hedef`. `konusan`/`dinleyen` diyalog içindir, kuralda reddedilir.
- **Etkiler (v0, genişletilmedi):** `modifier`, `eksen`, `duygu`, `hafiza`, `gercek`. Zihin etkileri `ben`'in zihnine yazılır; `hedef` ilişkinin karşı tarafıdır. `hafiza` tetikleyici olayı hatırlatır.
  `gercek` metnindeki `{ben}`, `{oyuncu}`, `{olay.fail}`, `{olay.hedef}` çalışma anında kimlikle doldurulur.
- **Doğrulayıcı v0** (`KuralYukleyici`): şema/bilinmeyen alan, geçersiz kriter (sözdizimi, fonksiyon, operatör-değer türü), bilinmeyen olay türü, etkisi boş kural, kuralda tanımsız rol,
  henüz bağlı olmayan fonksiyon (`yalan_soyledi`, `yay`), geçersiz/tekrar eden kimlik, bozuk etki alanları. Tüm hatalar toplanır; her biri `dosya:satır: kural "id": mesaj` biçimindedir.

## Neden böyle?

- **Kural dosyası kodu değiştirmeden genişler:** yeni davranış = yeni JSON; motor yalnızca beş etki türünü bilir.
- **Çekirdek JSON okur, dosya okumaz.** Çekirdekte JSON okuyucu yoktu; paket eklemek ADR-004'e aykırı olurdu. Küçük, bağımlılıksız bir okuyucu yazıldı (`Yanki.Core.Json`: sayılar `decimal`,
  nesne sırası korunur, satır/sütun bilgisi doğrulayıcı hatalarına akar). Bkz. ADR-009. Dosya okuma CLI'dadır (`SenaryoDosyasi`).
- **Karar (Yusuf):** Tek durum görüntüsü kalır: bir adımdaki bütün kurallar adım başındaki durumu görür. Sıra (tetikleyici → kural kimliği → NPC) sonucu değiştirmesin diye, bir kuralın ürettiği olay aynı adımda başka kuralın kriterine yansımaz.
- **Karar (Yusuf):** Gerçek etkisi tekrarsız yazılır: **değişiklik yapmıyorsa üretilmez** (var olanı eklemek, olmayanı kaldırmak) ve aynı adımda iki NPC için aynı gerçek tek kez yazılır; böylece günlükte kopya olay birikmez.

## Bu hafta yazılmayanlar

`cooldown`, `tick_basina_en_fazla`, `yay` koşulları, zincir derinliği sınırı, fren olayları, korumalı bayraklar (hepsi 7. hafta). Sözlüğe yeni fonksiyon eklenmedi.

## Kararlar (Yusuf, PR #7 incelemesi) ve açık noktalar

- **Karar (Yusuf):** Kural yalnızca **tüm kriterleri** tutunca çalışır; kriteri olmayan kural her tetikleyici olayda çalışır; `agirlik` sonucu etkilemez ve doğrulayıcı uyarı verir (ADR-010).
- **Karar (Yusuf):** Hedef/tanık ayrımı `olay_rolu(fail|hedef|tanik)` ile yapılır (sözlüğe eklendi). Rol, o olaydaki fail/hedef alanlarından belirlenir; geçmiş inançlara bakmaz.
- **Karar (Yusuf):** Tanımsız rol gerektiren kural o olay için atlanır; 7. haftada bu durum için fren olayı yazılacak.
- **Karar (Yusuf):** Algı yalnızca dış olayları alır; algı olayları kural tetiklemez. Kural çıktıları sonraki tick'te hem algıya (dış olaysa) hem kurallara girer; bir zihin olayını yalnızca sahibi algılar.
- **Karar (Yusuf):** Aynı NPC ve aynı olay için tek hafıza kaydı; `hafiza` etkisi önemi `max(eski, yeni)` yapar (yalnızca artarsa kayıt tazelenir); her çağrı yine `hafiza_eklendi` olayıyla kaydedilir.
- **Açık:** `hafiza` etkisi her zaman tetikleyici olayı hatırlatır; başka bir olayı seçme yok.
- **Açık:** Etki hedefi senaryoda bulunmayan bir sabit kimlikse `Simulasyon.Olustur` reddeder.
