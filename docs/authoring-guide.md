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
| `celisen_ozellikler`                                   | hayır                           | Eylemin çeliştiği kişilik özellikleri. 5. haftada yalnızca karar izinde görünür, stres 6. haftada.                                                                                                                                              |
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

## Sonuç kuralları, şablonlar, `yanki validate`

🚧 İlerleyen haftalarda (kural yazımı için şimdilik `samples/MiniHan/kurallar.json` ve `notes/kural-motoru.md`).
