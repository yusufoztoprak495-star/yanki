# Kriter dili

Sonuç kuralları, diyalog şablonları ve eylem ön koşulları **aynı** yapıyı kullanır (plan bölüm 5.9, [ADR-003](adr/0003-birlesik-kriter-dili.md)).
Tek ayrıştırıcı, tek doğrulayıcı, tek kapsama aracı üç kullanım için de çalışır.

## Biçim

Bir kriter JSON nesnesidir:

```json
{ "alan": "fikir(konuşan, dinleyen)", "op": "<", "değer": -10, "ağırlık": 2 }
{ "alan": "inanç(dinleyen, borçlu(Mehmet,*,*))", "op": "güven>=", "değer": 0.5, "zorunlu": true }
{ "alan": "yay.kasaba_gerginliği", "op": "aralık", "değer": [2, 4] }
```

- **alan:** zihin, dünya, hafıza ve yay değişkenleri üzerinde tanımlı bir sorgu fonksiyonu.
- **op / değer:** karşılaştırma.
- **ağırlık:** kriter tutarsa puana eklenen değer.
- **zorunlu:** tutmazsa aday elenir.

**Puan** = tutan kriterlerin ağırlıklarının toplamı.

## Sorgu fonksiyonu sözlüğü

v0.1'de sabit, belgelenmiş küçük bir set (yaklaşık 15 fonksiyon). **Bu tablo Yusuf'un tasarım alanıdır; boş bırakıldı.**
İpuçları: ruh hali, stres, mekan, zaman, kimden duydu, kaç kişi biliyor, yalan söylendi mi, bir eylemi en son ne zaman yaptı.

| # | Fonksiyon | Ne sorar | Döndürdüğü | Örnek kriter |
|---|---|---|---|---|
| 1 |  |  |  |  |
| 2 |  |  |  |  |
| 3 |  |  |  |  |
| 4 |  |  |  |  |
| 5 |  |  |  |  |
| 6 |  |  |  |  |
| 7 |  |  |  |  |
| 8 |  |  |  |  |
| 9 |  |  |  |  |
| 10 |  |  |  |  |
| 11 |  |  |  |  |
| 12 |  |  |  |  |
| 13 |  |  |  |  |
| 14 |  |  |  |  |
| 15 |  |  |  |  |
