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

- Roller: `ben` (kuralı/şablonu değerlendiren NPC), `oyuncu`, diyalogda `konusan` ve `dinleyen`, sonuç kurallarında `olay.fail` ve `olay.hedef`, eylemlerde `eylem.hedef` (bkz. "Eylem bağlamı" bölümü).
- Operatörler: `<`, `<=`, `>`, `>=`, `==`, `!=`, `aralik [a, b]`.
- `*` joker karakterdir: `borclu(Mehmet, *, *)` = "Mehmet'in herhangi birine borçlu olduğu önerme".
- Fonksiyon adları ASCII'dir (Türkçe karakter yok), böylece JSON ve C# tarafında kodlama sorunu çıkmaz.
- Fonksiyon adları ve JSON anahtarları ASCII'dir; Türkçe karakter yalnızca açıklama metinlerinde ve diyalog metinlerinde kullanılır.

## Sorgu fonksiyonu sözlüğü

v0.1'de sabit, belgelenmiş küçük bir set (18 fonksiyon; 15'i v0.1 listesi, `olay_rolu` 4. haftada, `konu_puani` ve `olay_konusu` 6. haftada Yusuf'un kararıyla eklendi). **Bu sözlük Yusuf'un tasarım alanıdır; v0.1 listesi Yusuf tarafından onaylandı. Değişiklikler onun onayıyla yapılır.**
İpuçları: ruh hali, stres, mekan, zaman, kimden duydu, kaç kişi biliyor, yalan söylendi mi, bir eylemi en son ne zaman yaptı.

| #   | Fonksiyon                                 | Ne sorar                                                                                                                                                                                   | Döndürdüğü                                     | Örnek kriter                               |
| --- | ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------- | ------------------------------------------ |
| 1   | `kisilik(ozellik)`                        | Bu NPC'nin kişilik özelliği ne kadar güçlü                                                                                                                                                 | sayı 0..1                                      | `kisilik(acgozluluk) > 0.7`                |
| 2   | `eksen(hedef, guven\|sevgi\|korku)`       | Hedefe karşı kalıcı ilişki eksenlerinden biri                                                                                                                                              | sayı −100..100                                 | `eksen(oyuncu, guven) < -20`               |
| 3   | `fikir(hedef)`                            | Hedef hakkındaki toplam fikir (eksenler + aktif modifier'lar)                                                                                                                              | sayı −100..100                                 | `fikir(dinleyen) < -10`                    |
| 4   | `fikir_bandi(hedef)`                      | Fikrin bandı                                                                                                                                                                               | `dusman` / `soguk` / `notr` / `yakin` / `dost` | `fikir_bandi(oyuncu) == soguk`             |
| 5   | `modifier_var(hedef, tur)`                | Hedefe karşı bu türde aktif bir fikir modifier'ı var mı                                                                                                                                    | doğru/yanlış                                   | `modifier_var(oyuncu, yalanini_yakaladim)` |
| 6   | `duygu(tur)`                              | Baskın duygu bu türse şiddeti, değilse 0                                                                                                                                                   | sayı 0..1                                      | `duygu(ofke) >= 0.5`                       |
| 7   | `duygu_hedefi()`                          | Baskın duygunun yöneldiği varlık                                                                                                                                                           | varlık kimliği                                 | `duygu_hedefi() == oyuncu`                 |
| 8   | `stres()`                                 | Kişiliğe aykırı davranışlardan biriken stres                                                                                                                                               | sayı 0..100                                    | `stres() >= 50`                            |
| 9   | `inanc(onerme)`                           | Önermeye inanma derecesi (kanıt listesinden hesaplanır)                                                                                                                                    | sayı 0..1                                      | `inanc(borclu(Mehmet, *, *)) >= 0.5`       |
| 10  | `kaynak(onerme, kisi)`                    | Bu önerme için bu kişiden gelen bir kanıt var mı ("kimden duydu?")                                                                                                                         | doğru/yanlış                                   | `kaynak(borclu(Mehmet, *, *), Ayse)`       |
| 11  | `hafiza_var(olay_turu, fail, son_n_tick)` | Son n tick içinde bu kişinin bu türde bir eylemini hatırlıyor mu                                                                                                                           | doğru/yanlış                                   | `hafiza_var(yardim_etti, oyuncu, 168)`     |
| 12  | `yalan_soyledi(kime)`                     | Bu NPC o kişiye yalan söylediğini biliyor mu (kendi yalan kaydı)                                                                                                                           | doğru/yanlış                                   | `yalan_soyledi(olay.hedef)`                |
| 13  | `ayni_mekanda(hedef)`                     | Hedefle aynı mekanda mı                                                                                                                                                                    | doğru/yanlış                                   | `ayni_mekanda(oyuncu)`                     |
| 14  | `gercek(onerme)`                          | Önerme dünyada nesnel olarak doğru mu (yalnızca sonuç kuralları ve yazar koşulları için; NPC zihni bunu göremez)                                                                           | doğru/yanlış                                   | `gercek(konum!(Ali, han))`                 |
| 15  | `olay_rolu(rol)`                          | Sonuç kuralında mevcut olay bağlamında bu NPC `fail` (olayı yapan), `hedef` (olayın yöneldiği) ya da `tanik` (olayı algılayan, ama ikisi de olmayan) mı. Olay bağlamı yoksa her rol yanlış | doğru/yanlış                                   | `olay_rolu(hedef)`                         |
| 16  | `yay(degisken)`                           | Senaryo düzeyindeki dramatik yay değişkeninin değeri                                                                                                                                       | sayı                                           | `yay(kasaba_gerginligi) aralik [2, 4]`     |
| 17 | `konu_puani(kime)` | Bu NPC'nin o kişiye anlatabileceği en yüksek konu puanı (yalnızca `aktarim: dogru` eylemlerinin ön koşul ve ölçütlerinde; konu havuzu eylemin `konu_desenleri`) | sayı 0..1 | `konu_puani(eylem.hedef) >= 0.2` |
| 18 | `olay_konusu(onerme)` | Sonuç kuralında mevcut olay `anlatti` ise ve anlatılan önerme desene uyuyorsa; yalnızca içeriği duyan dinleyen (`olay.hedef`) için doğru, konuşan ve tanık için yanlış | doğru/yanlış | `olay_konusu(borclu(Mehmet, *, *))` |

## Bağlama durumu (3. hafta)

`ZihinSorguCozucusu` 3. haftada 16 fonksiyonun 14'ünü gerçek veriye bağlıyordu (6. haftada `yalan_soyledi`, `konu_puani` ve `olay_konusu` da bağlandı, `yay` 7. haftada bağlandı; aşağıda): zihne dayanan 12 (`kisilik`, `eksen`, `fikir`, `fikir_bandi`, `modifier_var`, `olay_rolu`, `duygu`,
`duygu_hedefi`, `stres`, `inanc`, `kaynak`, `hafiza_var`) ve dünyaya dayanan 2 (`gercek`, `ayni_mekanda`). `yalan_soyledi` (yalan kaydı) 6. haftada bağlandı;
`yay` (yay değişkenleri, 7. hafta) bağlandı: değer `yay_degisti` olaylarından türeyen depodan okunur (tanımsız değişken `Simulasyon.Olustur`'da reddedilir, çözücüde `KriterHatasi`). Sözlüğe yeni fonksiyon eklenmedi, sözlük 18 fonksiyon. `inanc` çelişkide kaybeden önerme için 0 döner;
`inanc(...)` ve `kaynak(...)` önerme argümanında `degil(yuklem(...))` kabul eder (ör. `inanc(degil(konum!(Ali, han)))` = "Ali handa değil" inancı); `degil` başka yerde ve iç içe geçersizdir. Yeni fonksiyon eklenmedi. Ayrıntı: `docs/notes/zihin.md`.

## JSON'dan okuma ve kural bağlamı (4. hafta)

Kriterler artık JSON nesnesinden okunur (`KriterOkuyucu`; `alan`, `op`, `deger`, isteğe bağlı `agirlik` ve `zorunlu`) ve sonuç kurallarında kullanılır (`docs/notes/kural-motoru.md`).
Sözlüğe **yeni fonksiyon eklenmedi**. Sonuç kurallarında geçerli roller `ben`, `oyuncu`, `olay.fail`, `olay.hedef`'tir; `konusan` ve `dinleyen` diyalog içindir.
Çözücüsü bağlı olmayan fonksiyon kalmadı (`BagliOlmayanFonksiyonlar` boş): `yalan_soyledi` 6., `yay` 7. haftada bağlandı.

`olay_rolu(rol)` (4. hafta, Yusuf'un kararı) hedef/tanık ayrımını yapar: önceki geçici `inanc(<tür>(olay.fail, ben))` yöntemi kaldırıldı, çünkü NPC'nin geçmişte benzer bir olayın hedefi olması
onu sonraki olaylarda yanlışlıkla hedef sayıyordu. Rol yalnızca o olaydaki fail/hedef alanlarından belirlenir; olay bağlamı (`olay.fail`) yoksa her rol yanlıştır.
Kural kriterleri hakkında: bir kural yalnızca **tüm** kriterleri tutunca çalışır; `agirlik` kural tetiklenmesini etkilemez (doğrulayıcı uyarı verir), bkz. ADR-010.

## Eylem bağlamı (5. hafta)

NPC eylemlerinin `on_kosullar`, `olcutler` ve `etkiler` alanları aynı kriter dilini kullanır (ADR-003, ADR-011); ikinci bir sorgu yolu yoktur. Sözlüğe **yeni fonksiyon eklenmedi** (16 fonksiyon), yalnızca yeni bir rol eklendi.

**Roller:** `ben` (karar veren NPC), `oyuncu` ve `eylem.hedef` (adayın hedefi). `eylem.hedef` yalnızca eylem bağlamında geçerlidir; kural ve diyalog bağlamında doğrulayıcı reddeder, hedefsiz (`hedef: yok`) eylemde de reddedilir. Hedef türüne göre anlamı:

| `hedef`       | `eylem.hedef`                                                                                                                                                                                                                                                                                          |
| ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `npc`         | aynı mekandaki başka bir NPC                                                                                                                                                                                                                                                                           |
| `oyuncu`      | oyuncu (aynı mekandaysa)                                                                                                                                                                                                                                                                               |
| `komsu_mekan` | bağlı bir komşu **mekan**; kişi bekleyen sorgularda (`fikir`, `eksen`, `modifier_var`, `ayni_mekanda`, ...) ve kişi bekleyen etkilerde (modifier, eksen, duygu) reddedilir; yalnızca önerme içinde (`inanc(tehlikeli(eylem.hedef))`) ve `gercek` etkisinin şablonunda (`{eylem.hedef}`) kullanılabilir |

**Eylem bağlamında yasak olanlar** (doğrulayıcı `dosya:satir: eylem "id": mesaj` ile reddeder):

| Yasak                                                    | Neden                                                                                     |
| -------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| `olay.fail`, `olay.hedef`, `konusan`, `dinleyen` rolleri | Eylemin tetikleyici olayı yoktur; diyalog 8. haftada.                                     |
| `olay_rolu(...)`                                         | Sonuç kuralı bağlamıdır (olay.fail / hedef / tanık).                                      |
| `gercek(...)`                                            | Nesnel dünya sorgusudur, NPC'nin bilgisini aşar; NPC'nin inancı için `inanc(...)` kullan. |
| `yay(ad)`                                                | Kural kriterinde ve eylem ön koşul/ölçütünde geçerlidir; kriz satırında reddedilir. Değişken senaryoda `yay_degiskenleri` ile tanımlı olmalıdır. |

**Ölçütler** (`olcutler[].alan`) yalnızca sayısal ya da doğru/yanlış (1/0) sonuç veren fonksiyonlardır; `fikir_bandi` ve `duygu_hedefi` ölçüt olamaz (ön koşul olarak kullanılabilir).

**Sonuç kuralları** NPC eylemlerinin `olay_turu` türüne `ne_zaman` ile bağlanabilir; `ayrildi` ve `geldi` yerleşik türlerdir ve olayın `Hedef` alanı karşı **mekandır** (kural `olay.hedef`'e modifier/eksen/duygu yazamaz). `karar_verildi` `ne_zaman` olamaz. Ayrıntı: [ADR-011](adr/0011-karar-modeli.md), [`notes/karar.md`](notes/karar.md).

## Dedikodu bağlamı (6. hafta)

Sözlük 16'dan **18 fonksiyona** çıktı (Yusuf'un kararı, ADR-012); `yalan_soyledi(kime)` bağlandı. Yeni fonksiyonlar ve bağlam kuralları:

| Fonksiyon                  | Geçerli bağlam                                                                 | Anlam                                                                                                                                                                                                                  |
| -------------------------- | ------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `konu_puani(kime)`         | Yalnızca `aktarim: dogru` eylemlerinin `on_kosullar` ve `olcutler`'i           | Bu NPC'nin (`ben`) o kişiye anlatabileceği en yüksek konu puanı (0..1); konu havuzu eylemin `konu_desenleri`'dir. Başka bağlamda (kural, kriz, yalan ve aktarımsız eylem) doğrulayıcı reddeder; çalışma anında konu havuzu yoksa çözülemez. |
| `olay_konusu(desen)`       | Yalnızca sonuç kuralı                                                          | Mevcut olay `anlatti` ise, `ben` olayın hedefi (içeriği duyan dinleyen) ise ve anlatılan önerme desene uyuyorsa doğru; konuşan ve tanık için her zaman yanlış. Desen `degil(...)` kabul eder (yalan söylenen olumsuz önerme için). Eylem bağlamında doğrulayıcı reddeder. |
| `yalan_soyledi(kime)`      | Kural, eylem ve kriz kriterleri                                                | NPC'nin kendi aktarım kaydında o kişiye yalan söylediği bir kayıt var mı. Dinleyen bunu bilmez; `ben` hep konuşandır.                                                                                              |

Kriz satırı kriterlerinde roller `ben`, `oyuncu` ve (hedefli satırda) `eylem.hedef`'tir; `olay_rolu`, `olay_konusu`, `konu_puani`, `gercek` ve `yay` reddedilir (`yay` etkisi de kriz satırında yazılamaz). Kriz satırında kriterler ADR-010 gibi **hepsi tutmalıdır**; `agirlik` yalnızca tutan satırlar arasında özgüllük sıralaması içindir.
Ayrıntı: `docs/notes/dedikodu.md`, `docs/notes/stres.md`.

## Yay değişkenleri (7. hafta)

Senaryoda `"yay_degiskenleri": { "kasaba_gerginligi": { "baslangic": 0, "min": 0, "max": 10 } }` ile tanımlanır. Etki (kural ve eylem): `{ "tur": "yay", "degisken": "kasaba_gerginligi", "delta": 1 }`; değer aralığa kırpılır, kırpılırsa `yay_siniri` freni yazılır. `yay_degisti` olayı algılanmaz ve kural tetiklemez (`ne_zaman: yay_degisti` reddedilir); `KuralId` ve `NedenOlaylar` taşır, `why` zincire iner. Kriterler değişkeni kuralların aynı adımda tuttuğu **tek görüntüden** okur; aynı adımdaki birden çok delta sabit sırayla toplanır. Anlatısal "sonuç bandı" kapısı yoktur: aralık için `aralik` operatörünü kullanın.

## Diyalog şablonu bağlamı (8. hafta)

Şablon kriterleri aynı dili kullanır; roller `ben` (konuşan), `konusan`, `dinleyen`, `oyuncu`. Reddedilen fonksiyonlar: `olay_rolu`, `olay_konusu`, `konu_puani`, `gercek`, `yalan_soyledi`; konuşanın anlatıcı olduğu
niyetlerde `inanc`, `kaynak`, `hafiza_var` da reddedilir (yalan gizliliği). Ayrıntı: [authoring-guide](authoring-guide.md#diyalog-şablonları-8-hafta), [ADR-014](adr/0014-diyalog-katmani.md).
