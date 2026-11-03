# Senaryo yazım rehberi

Bu rehber haftalar ilerledikçe dolar: **eylem yazımı** 5. haftada tamam; sonuç kuralı ve şablon biçimleri 4–8. haftalarda, `yanki validate` ile birlikte 9. haftada tamamlanır. Kuralların biçimi için `samples/MiniHan/kurallar.json` ve [`notes/kural-motoru.md`](notes/kural-motoru.md); çalışan bir örnek için [`samples/MiniKarar`](../samples/MiniKarar/README.md).

## Eylem yazımı (5. hafta)

NPC eylemleri JSON ile tanımlanır, kod değiştirmeden genişler. Senaryo dosyası `eylem_dosyalari` ile bağlar (yoksa NPC'ler hiçbir şey yapmaz):

```json
{
  "...": "...",
  "kural_dosyalari": ["kurallar.json"],
  "eylem_dosyalari": ["eylemler.json"]
}
```

Bir eylem dosyası `{ "eylemler": [ ... ] }` biçimindedir. Eylem alanları:

| Alan                                                   | Zorunlu                         | Anlam                                                                                                                                                                                                                                           |
| ------------------------------------------------------ | ------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `id`                                                   | evet                            | Kimlik (ASCII). Olayların `KuralId`'si `eylem:<id>` olur.                                                                                                                                                                                       |
| `hedef`                                                | evet                            | `yok`, `npc` (aynı mekandaki başka NPC), `oyuncu` (aynı mekandaysa), `komsu_mekan` (hareket).                                                                                                                                                   |
| `olay_turu`                                            | `komsu_mekan` dışında evet      | Eylemin dışarı görünen olayı; kurallar `ne_zaman` ile buna bağlanır. Çekirdeğe ayrılmış adlar (`ayrildi`, `geldi`, `karar_verildi`, `hareket`, zihin ve dünya olayları) kullanılamaz. Hareket eyleminde yazılmaz (çıktısı `ayrildi` + `geldi`). |
| `taban`                                                | evet                            | Puan tabanı, `0 < taban ≤ 1000`.                                                                                                                                                                                                                |
| `on_kosullar`                                          | hayır                           | Kriter listesi; **hepsi tutmalı** (ADR-010). Tutmayan aday elenir.                                                                                                                                                                              |
| `olcutler`                                             | hayır                           | Puanlama ölçütleri (aşağıda).                                                                                                                                                                                                                   |
| `kisilik_carpani`, `fikir_carpani`, `ruh_hali_carpani` | hayır                           | Üç çarpan tanımı; yoksa 1.0.                                                                                                                                                                                                                    |
| `celisen_ozellikler`                                   | hayır                           | Eylemin çeliştiği kişilik özellikleri. Varsa `stres_tabani` zorunlu: eylem seçilince stres artar (aşağıda, 6. hafta).                                                                                                                                              |
| `etkiler`                                              | `komsu_mekan` dışında evet, ≥ 1 | Kural etkisiyle aynı biçim (`modifier`, `eksen`, `duygu`, `hafiza`, `gercek`); eylem seçilince `eylem:<id>` kimliğiyle yazılır.                                                                                                                 |

**Puan:** `taban × Π(ölçüt eğrisi) × kişilik çarpanı × fikir çarpanı × ruh hali çarpanı`. Puanı 0 olan aday seçilemez.

- **Ölçüt:** `{ "alan": "kisilik(merhamet)", "min": 0, "max": 1, "egri": { "tur": "dogrusal" } }`. `alan` kriter sözlüğünden sayısal (ya da doğru/yanlış = 1/0) bir sorgudur; `min`/`max` (varsayılan 0..1) ile 0..1'e normalize edilir. Eğriler: `dogrusal`; `ustel` (`k` tam sayı 1..8); `lojistik` (`k` 0..100, `m` 0..1); `basamak` (`esik` 0..1).
- **Kişilik çarpanı:** `{ "ozellikler": [ { "ozellik": "merhamet", "agirlik": 1, "ters": false } ] }`. Ağırlıklı ortalama 0.25–2.0'a eşlenir (0.5 → 1.0).
- **Fikir çarpanı:** `{ "yon": "artan" }` (ya da `azalan`); yalnızca `npc`/`oyuncu` hedefli eylemde. `fikir(eylem.hedef)` 0.6–1.5'e eşlenir.
- **Ruh hali çarpanı:** `{ "duygular": [ { "duygu": "korku", "agirlik": 1, "ters": true } ] }`; 0.8–1.25.

Eylem bağlamındaki roller ve yasak sorgular: [`criteria-language.md`](criteria-language.md#eylem-bağlamı-5-hafta). Doğrulayıcı hataları `dosya:satir: eylem "id": mesaj` biçimindedir.

### Tam örnek

```json
{
  "id": "yardim_et",
  "olay_turu": "yardim_etti",
  "hedef": "npc",
  "taban": 1,
  "on_kosullar": [
    { "alan": "hafiza_var(yardim_etti, ben, 12)", "op": "==", "deger": false }
  ],
  "olcutler": [{ "alan": "kisilik(merhamet)" }],
  "kisilik_carpani": { "ozellikler": [{ "ozellik": "merhamet" }] },
  "fikir_carpani": { "yon": "artan" },
  "etkiler": [{ "tur": "hafiza", "onem": 0.6 }]
}
```

### Aynı eylemin her tick tekrarını sınırlamak

Cooldown 7. haftadadır. O zamana kadar `hafiza_var(olay_turu, ben, n)` ön koşulu kullanılır (örnek yukarıda): eylem son `n` tick içinde yapıldıysa aday elenir. Sınırları (ayrıntı: [`notes/karar.md`](notes/karar.md)):

- Eylem **türüne** göre işler, hedefe göre değil.
- Hafıza yaklaşık 200 tick sonra unutulur; daha büyük pencereler güvenilmez.
- "En fazla k kez" ya da tick başına sınır ifade edilemez.

### Mutlaka bir bekleme eylemi yazın (`dinlen`): şiddetle önerilir

Her senaryoya **puanı 0'dan büyük, hedefsiz, hiçbir ön koşulu olmayan** küçük bir bekleme eylemi ekleyin:

```json
{
  "id": "dinlen",
  "olay_turu": "dinlendi",
  "hedef": "yok",
  "taban": 0.1,
  "etkiler": [{ "tur": "hafiza", "onem": 0.1 }]
}
```

**Neden:** seçici yalnızca puanı **0'dan büyük** adaylardan seçer. Yardım ve selam gibi eylemler `hafiza_var` sınırına takılınca elenir; geriye puanı pozitif tek aday olarak `git` kalırsa (küçük de olsa pozitiftir) bütün NPC'ler her tick sürü halinde mekan değiştirir. 5. haftanın senaryo taslaklarında bu görüldü: bekleme eylemi olmadığında üç NPC de tick 3'ten itibaren her tick birlikte mekan değiştirdi (`samples/MiniKarar` `dinlen` içerir). `dinlen` düşük ama pozitif puanla "hiçbir şey yapma" seçeneğini yaratır, böylece sınırlı eylemler sustuğunda NPC boşta kalabilir.

Motor bunu **zorunlu kılmaz** ve yerleşik bir bekleme eylemi eklemez: bekleme, senaryonun kişiliğine ve dengesine ait bir yazar kararıdır (hangi olayı üretir, hafızaya yazar mı, ruh haline göre mi değişir). Hiç aday kalmayan NPC için olay yazılmaz; tüm adaylar 0 puanlıysa boş seçimli `karar_verildi` yazılır.

### Hareket eylemi

`hedef: komsu_mekan` olan eylem bir harekettir: `olay_turu` yazılmaz, `etkiler` boş olabilir. Çıktısı iki olaydır: `ayrildi` (`Mekan` = kalkış) ve `geldi` (`Mekan` = varış); ikisinin `Hedef` alanı karşı mekandır. `konum!` gerçeği tick'in en sonunda ayrı olayla değişir. Hareket eyleminde `eylem.hedef` bir mekandır (bkz. kriter dili).

## Konuşma eylemi: anlat ve yalan söyle (6. hafta)

Dedikodu için kod yazılmaz: eylemde `aktarim` alanı konuşma eylemi yapar. Hedef `npc` (aynı mekandaki başka NPC) ya da `oyuncu` olur (ikisine de anlatmak için iki eylem yazılır). `olay_turu` **`anlatti` olmak zorundadır**, hem doğru hem yalan için (başka tür yalanı ele verirdi); `etkiler` boş olabilir (anlatımın kendisi etkidir).

| Alan | Zorunlu | Açıklama |
| ---- | ------- | -------- |
| `aktarim` | evet | `dogru`: konuşanın inandığı konuları anlatır. `yalan`: konuşanın inandığıyla çelişen bir önerme söyler. |
| `konu_desenleri` | `dogru` için evet | Anlatılabilecek önerme desenleri, ör. `["borclu(*,*,*)", "kardes(*,*)"]`. Yüklem açık yazılır (joker yüklem yok); `anlatti` önermeleri desene uysa bile konu olamaz (uyarı). `yalan` eyleminde yazılamaz. |
| `yalan_onermeleri` | `yalan` için evet | Yazar listesi, `{eylem.hedef}` ve `{ben}` şablonu destekli: `["degil(borclu(Mehmet,{eylem.hedef},500))"]`. Konuşan listedekilerden **inandığı bir önermeyle çelişen** ve bu kişiye daha önce söylenmemiş olanını söyler (çok varsa anahtarlı RNG seçer); yoksa aday elenir. `dogru` eyleminde yazılamaz. |
| `gorunurluk` | hayır | `sessiz` (varsayılan; yalnızca konuşan ve dinleyen algılar) ya da `acik` (tanık konuşmanın olduğunu görür, içeriğini duymaz). |
| `stres_tabani` | `celisen_ozellikler` varsa evet | Aşağıda. |

```json
{ "id": "anlat", "olay_turu": "anlatti", "hedef": "npc", "taban": 1, "aktarim": "dogru",
  "konu_desenleri": ["borclu(*,*,*)"],
  "olcutler": [ { "alan": "kisilik(sosyallik)" }, { "alan": "konu_puani(eylem.hedef)" } ],
  "kisilik_carpani": { "ozellikler": [ { "ozellik": "sosyallik" } ] } },
{ "id": "yalan_soyle", "olay_turu": "anlatti", "hedef": "npc", "taban": 0.4, "aktarim": "yalan",
  "yalan_onermeleri": ["degil(borclu(Mehmet,{eylem.hedef},500))"],
  "celisen_ozellikler": ["durustluk"], "stres_tabani": 15 }
```

- **Ne anlatılır?** Seçilince konuşan, `konu_desenleri`ne uyan, inandığı ve ham güveni ≥ 0.3 olan önermelerden **konu puanına** göre azalan en fazla `n` tanesini anlatır; `n = 1 + ⌊sosyallik × 2⌋` (+1 fikir bandı `yakin`/`dost`). Her konu ayrı `anlatti` olayıdır, yani kural konu bazında yazılır. Puanı 0.1'in altındaki konu anlatılmaz; konu kalmazsa aday elenir (karar izinde `konu yok (...)`).
  Aynı önerme aynı kişiye iki kez anlatılmaz; konuşan kendisine kanıt veren kişiye ya da söylentinin kökü olan kişiye geri anlatmaz.
- **`konu_puani(kime)`** yalnızca `aktarim: dogru` eylemlerinin `on_kosullar`/`olcutler`'inde kullanılır ("anlatacak şeyi ne kadar ilginç?"). Başka yerde doğrulayıcı reddeder.
- **Kuralda konu:** `"olay_konusu(borclu(Mehmet, *, *))"` yalnızca sonuç kuralında kullanılır ve yalnızca içeriği duyan dinleyen (`olay.hedef`) için tutar. Konuşan ve tanık için tutmaz:

```json
{ "id": "aile_iftirasi", "ne_zaman": "anlatti",
  "kriterler": [ { "alan": "olay_konusu(borclu(Mehmet, *, *))", "op": "==", "deger": true } ],
  "etkiler": [ { "tur": "modifier", "hedef": "olay.fail", "modifier": "aileme_iftira", "deger": -22, "sure": 18 } ] }
```

- **Duyulan konunun önemi** varsayılan olarak 0.5'e iner; önemli bir haberi daha çok yaymak istiyorsanız `anlatti` kuralına `{ "tur": "hafiza", "onem": 0.9 }` ekleyin (11. hafta denge turunda ölçülecek).
- **Yalan** dışarıdan doğru anlatımdan ayırt edilemez; kural yazarı "bu yalandı" diyemez (`olay_konusu` ikisinde de aynı çalışır). Konuşan kendi yalanını `yalan_soyledi(kime)` ile bilir: `"alan": "yalan_soyledi(eylem.hedef)", "op": "==", "deger": false` ile aynı kişiye yalan söylemişse tekrar söylememeyi yazabilirsiniz.
- **Oyuncu anlatımı:** oyuncu `anlatti` türünde eylem gönderir (senaryoda bildirilmez): hedef aynı mekandaki bir NPC, yük `onerme` (+ isteğe bağlı `gorunurluk`). CLI: `yanki run senaryo.json --eylem "anlatti:Ayse:onerme=borclu(Mehmet,Ayse,500)" --tick 3`. `anlatti` adı senaryoda `olay_turleri`ne yazılmaz (çekirdeğe ayrılmıştır).

## Stres ve kriz yazımı (6. hafta)

**Stres:** çelişen eylemin bir maliyeti olur. `celisen_ozellikler` yazdığınızda `stres_tabani` (0 < taban ≤ 100) da yazmak zorundasınız (doğrulayıcı reddeder). Seçilince `stres += stres_tabani × Σ(özellik değeri)`: dürüstlüğü 0.8 olan NPC için `yalan_soyle` (taban 15) +12. Hareket dahil her eylem stres yazabilir.
Stres yavaş söner (0.2/tick); eşik 50 ve 80'dir (denge alanı).

**Kriz tablosu:** senaryo `"kriz_dosyalari": ["krizler.json"]` ile bağlar (yoksa kriz çıkmaz, stres çalışır). Eşik aşılınca (`stres ≥ eşik`, iki eşik birden aşılırsa üstteki) NPC o tick eylem seçmez; tablodan kişiliğine uygun bir satır seçilir, kriz olayı yazılır ve stres düşer (50 seviyesinde 30, 80 seviyesinde 60).

```json
{ "krizler": [
  { "id": "itiraf", "seviye": 1, "olay_turu": "itiraf_etti", "hedef": "yalan_kurbani",
    "kriterler": [ { "alan": "kisilik(durustluk)", "op": ">=", "deger": 0.7 } ],
    "etkiler": [ { "tur": "hafiza", "onem": 0.9 } ] },
  { "id": "sakinles", "seviye": 1, "olay_turu": "sakinlesti", "hedef": "yok" },
  { "id": "patla", "seviye": 2, "olay_turu": "patladi", "hedef": "en_soguk",
    "kriterler": [ { "alan": "kisilik(kin_tutma)", "op": ">=", "deger": 0.7 } ] },
  { "id": "kac", "seviye": 2, "olay_turu": "kacti", "hedef": "yok" }
] }
```

- `seviye`: **1 = alt eşik (50), 2 = üst eşik (80)**; doğrulayıcı başka değeri (0, 3, kesirli, metin) reddeder. `hedef`: `yok`, `yalan_kurbani` (en son yalan söylenen kişi), `en_yakin` / `en_soguk` (aynı mekandaki en yüksek / en düşük fikirli kişi; oyuncu dahil). Hedefi aynı mekanda olmayan satır elenir.
- **Her seviye için kriterli olmayan bir yedek satır zorunludur** (örnekte `sakinles`, `kac`); yedeğin hedefi `yok` olmalıdır. Kriterler **hepsi tutmalı**dır (ADR-010); birden çok satır uyuyorsa en özgül (ağırlık toplamı yüksek) kazanır, eşitlikte anahtarlı RNG.
- Kriz olayı (`olay_turu`) sıradaki tick'te sonuç kurallarını tetikler: `{ "ne_zaman": "itiraf_etti", "kriterler": [ { "alan": "olay_rolu(hedef)", ... } ], ... }`. Kurban yalanı **yalnızca** bu tür kurallarla öğrenir; itiraf otomatik olarak inancı düzeltmez.
- Kriz satırı kriterlerinde `olay_rolu`, `olay_konusu`, `konu_puani`, `gercek`, `yay` kullanılamaz. Hata biçimi: `dosya:satir: kriz "id": mesaj`.

## Sonuç kuralları, şablonlar, `yanki validate`

🚧 İlerleyen haftalarda (kural yazımı için şimdilik `samples/MiniHan/kurallar.json` ve `notes/kural-motoru.md`).
