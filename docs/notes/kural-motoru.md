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
        {
          "alan": "fikir(olay.fail)",
          "op": "<",
          "deger": 50,
          "agirlik": 1,
          "zorunlu": false
        }
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

- **Kriterler** mevcut kriter dilidir (`docs/criteria-language.md`): `KriterOkuyucu` JSON'u `Kriter.Olustur`'a verir, `Secici.Puanla` puanlar. İkinci bir kriter yolu yoktur.
- **Roller:** kural bağlamında `ben` (kuralı değerlendiren NPC), `oyuncu`, `olay.fail`, `olay.hedef`. `konusan`/`dinleyen` diyalog içindir, kuralda reddedilir.
- **Etkiler (v0, genişletilmedi):** `modifier`, `eksen`, `duygu`, `hafiza`, `gercek`. Zihin etkileri `ben`'in zihnine yazılır; `hedef` ilişkinin karşı tarafıdır. `hafiza` tetikleyici olayı hatırlatır.
  `gercek` metnindeki `{ben}`, `{oyuncu}`, `{olay.fail}`, `{olay.hedef}` çalışma anında kimlikle doldurulur.
- **Doğrulayıcı v0** (`KuralYukleyici`): şema/bilinmeyen alan, geçersiz kriter (sözdizimi, fonksiyon, operatör-değer türü), bilinmeyen olay türü, etkisi boş kural, kuralda tanımsız rol,
  henüz bağlı olmayan fonksiyon (`yalan_soyledi`, `yay`), geçersiz/tekrar eden kimlik, bozuk etki alanları. Tüm hatalar toplanır; her biri `dosya:satır: kural "id": mesaj` biçimindedir.

## Neden böyle?

- **Kural dosyası kodu değiştirmeden genişler:** yeni davranış = yeni JSON; motor yalnızca beş etki türünü bilir.
- **Çekirdek JSON okur, dosya okumaz.** Çekirdekte JSON okuyucu yoktu; paket eklemek ADR-004'e aykırı olurdu. Küçük, bağımlılıksız bir okuyucu yazıldı (`Yanki.Core.Json`: sayılar `decimal`,
  nesne sırası korunur, satır/sütun bilgisi doğrulayıcı hatalarına akar). Bkz. ADR-009. Dosya okuma CLI'dadır (`SenaryoDosyasi`).
- **Tek durum görüntüsü:** bir adımdaki bütün kurallar adım başındaki durumu görür. Sıra (tetikleyici → kural kimliği → NPC) sonucu değiştirmesin diye, bir kuralın ürettiği olay aynı adımda başka kuralın kriterine yansımaz.
- **Gerçek etkisi değişiklik yapmıyorsa üretilmez** (var olanı eklemek, olmayanı kaldırmak) ve aynı adımda iki NPC için aynı gerçek tek kez yazılır; böylece günlükte kopya olay birikmez.

## Bu hafta yazılmayanlar

`cooldown`, `tick_basina_en_fazla`, `yay` koşulları, zincir derinliği sınırı, fren olayları, korumalı bayraklar (hepsi 7. hafta). Sözlüğe yeni fonksiyon eklenmedi.

## Açık noktalar (Yusuf'un kararı)

1. **Kural ne zaman tetiklenir?** Şu an: hiçbir `zorunlu` kriter elenmemiş **ve** toplam puan 0'dan büyük (tutan en az bir kriter); kriteri olmayan kural olay türü tutunca tetiklenir.
   Alternatif: tüm kriterler tutmalı ("hepsi"), ya da kuralda `esik` puanı. ADR-003'ün ağırlık/puan modeline en yakın olan şimdiki seçim.
2. **Olayda tanımsız rol:** kural `olay.hedef` kullanıyor ama eylemin hedefi yoksa kural o olay için sessizce atlanır (hata değil).
3. **`hafiza` etkisi** her zaman tetikleyici olayı hatırlatır; başka bir olayı seçme yok.
4. **Algı olayları kural tetiklemez** (`kanit_eklendi`/`hafiza_eklendi` `KuralId: algi`); yalnızca oyuncu eylemleri ve kural çıktıları tetikler.
   Kural çıktıları sonraki tick'te hem algıya (dış olaysa) hem kurallara girer; bir zihin olayını yalnızca sahibi algılar.
5. **Etki hedefi** senaryoda bulunmayan bir sabit kimlikse `Simulasyon.Olustur` reddeder.
