# Kriter dili

Sonuç kuralları, diyalog şablonları ve eylem ön koşulları **aynı** yapıyı kullanır (plan bölüm 5.9, [ADR-003](adr/0003-birlesik-kriter-dili.md)).
Tek ayrıştırıcı, tek doğrulayıcı, tek kapsama aracı üç kullanım için de çalışır.

## Biçim

Bir kriter JSON nesnesidir:

```json
{ "alan": "fikir(dinleyen)", "op": "<", "deger": -10, "agirlik": 2 }
{ "alan": "inanc(borclu(Mehmet,*,*))", "op": ">=", "deger": 0.5, "zorunlu": true }
{ "alan": "yay(kasaba_gerginligi)", "op": "aralik", "deger": [2, 4] }
```

- **alan:** zihin, dünya, hafıza ve yay değişkenleri üzerinde tanımlı bir sorgu fonksiyonu.
- **op / deger:** karşılaştırma.
- **agirlik:** kriter tutarsa puana eklenen değer.
- **zorunlu:** tutmazsa aday elenir.

**Puan** = tutan kriterlerin ağırlıklarının toplamı.

## Roller ve operatörler

- Roller: `ben` (kuralı/şablonu değerlendiren NPC), `oyuncu`, diyalogda `konusan` ve `dinleyen`, sonuç kurallarında `olay.fail` ve `olay.hedef`.
- Operatörler: `<`, `<=`, `>`, `>=`, `==`, `!=`, `aralik [a, b]`.
- `*` joker karakterdir: `borclu(Mehmet, *, *)` = "Mehmet'in herhangi birine borçlu olduğu önerme".
- Fonksiyon adları ASCII'dir (Türkçe karakter yok), böylece JSON ve C# tarafında kodlama sorunu çıkmaz.
- Fonksiyon adları ve JSON anahtarları ASCII'dir; Türkçe karakter yalnızca açıklama metinlerinde ve diyalog metinlerinde kullanılır.

## Sorgu fonksiyonu sözlüğü

v0.1'de sabit, belgelenmiş küçük bir set (yaklaşık 15 fonksiyon). **Bu sözlük Yusuf'un tasarım alanıdır; v0.1 listesi Yusuf tarafından onaylandı. Değişiklikler onun onayıyla yapılır.**
İpuçları: ruh hali, stres, mekan, zaman, kimden duydu, kaç kişi biliyor, yalan söylendi mi, bir eylemi en son ne zaman yaptı.

| # | Fonksiyon | Ne sorar | Döndürdüğü | Örnek kriter |
|---|---|---|---|---|
| 1 | `kisilik(ozellik)` | Bu NPC'nin kişilik özelliği ne kadar güçlü | sayı 0..1 | `kisilik(acgozluluk) > 0.7` |
| 2 | `eksen(hedef, guven\|sevgi\|korku)` | Hedefe karşı kalıcı ilişki eksenlerinden biri | sayı −100..100 | `eksen(oyuncu, guven) < -20` |
| 3 | `fikir(hedef)` | Hedef hakkındaki toplam fikir (eksenler + aktif modifier'lar) | sayı −100..100 | `fikir(dinleyen) < -10` |
| 4 | `fikir_bandi(hedef)` | Fikrin bandı | `dusman` / `soguk` / `notr` / `yakin` / `dost` | `fikir_bandi(oyuncu) == soguk` |
| 5 | `modifier_var(hedef, tur)` | Hedefe karşı bu türde aktif bir fikir modifier'ı var mı | doğru/yanlış | `modifier_var(oyuncu, yalanini_yakaladim)` |
| 6 | `duygu(tur)` | Baskın duygu bu türse şiddeti, değilse 0 | sayı 0..1 | `duygu(ofke) >= 0.5` |
| 7 | `duygu_hedefi()` | Baskın duygunun yöneldiği varlık | varlık kimliği | `duygu_hedefi() == oyuncu` |
| 8 | `stres()` | Kişiliğe aykırı davranışlardan biriken stres | sayı 0..100 | `stres() >= 50` |
| 9 | `inanc(onerme)` | Önermeye inanma derecesi (kanıt listesinden hesaplanır) | sayı 0..1 | `inanc(borclu(Mehmet, *, *)) >= 0.5` |
| 10 | `kaynak(onerme, kisi)` | Bu önerme için bu kişiden gelen bir kanıt var mı ("kimden duydu?") | doğru/yanlış | `kaynak(borclu(Mehmet, *, *), Ayse)` |
| 11 | `hafiza_var(olay_turu, fail, son_n_tick)` | Son n tick içinde bu kişinin bu türde bir eylemini hatırlıyor mu | doğru/yanlış | `hafiza_var(yardim_etti, oyuncu, 168)` |
| 12 | `yalan_soyledi(kime)` | Bu NPC o kişiye yalan söylediğini biliyor mu (kendi yalan kaydı) | doğru/yanlış | `yalan_soyledi(olay.hedef)` |
| 13 | `ayni_mekanda(hedef)` | Hedefle aynı mekanda mı | doğru/yanlış | `ayni_mekanda(oyuncu)` |
| 14 | `gercek(onerme)` | Önerme dünyada nesnel olarak doğru mu (yalnızca sonuç kuralları ve yazar koşulları için; NPC zihni bunu göremez) | doğru/yanlış | `gercek(konum!(Ali, han))` |
| 15 | `yay(degisken)` | Senaryo düzeyindeki dramatik yay değişkeninin değeri | sayı | `yay(kasaba_gerginligi) aralik [2, 4]` |

## Bağlama durumu (3. hafta)

`ZihinSorguCozucusu` 15 fonksiyonun 13'ünü gerçek veriye bağlar: zihne dayanan 11 (`kisilik`, `eksen`, `fikir`, `fikir_bandi`, `modifier_var`, `duygu`,
`duygu_hedefi`, `stres`, `inanc`, `kaynak`, `hafiza_var`) ve dünyaya dayanan 2 (`gercek`, `ayni_mekanda`). `yalan_soyledi` (yalan kaydı, 6. hafta) ve
`yay` (yay değişkenleri, 7. hafta) henüz bağlı değildir. Sözlüğe yeni fonksiyon eklenmedi. `inanc` çelişkide kaybeden önerme için 0 döner;
`degil(p)` biçimli inançlar sözlükte (iç içe önerme yasağı) sorgulanamaz; ayrıntı için `docs/notes/zihin.md` açık noktalar.

