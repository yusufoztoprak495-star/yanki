# Not: Frenler (7. hafta, TASLAK)

> **Yazar: Claude Code** (Yusuf'un isteğiyle). Bu not 7. hafta boyunca doldurulur; T16'da tamamlanır. **Karar (Yusuf)** satırları Yusuf'un kararlarıdır.

## HanGerilim: `hafiza_var(...)` ön koşulları cooldown'a çevrilmeli mi? (T11)

**Karar (Yusuf):** Hayır. `samples/HanGerilim` eylemlerinin `hafiza_var(itiraf_etti | sakinlesti | patladi | kacti, ben, 24)` ön koşulları değişmez; cooldown bu koşulların yerini tutmaz.
Golden #3 (`han-gerilim-1.json`) bu karardan etkilenmez.

**Neden:** İki mekanizma farklı şeyi ifade eder. `hafiza_var(kriz, ben, 24)` krizden SONRA 24 tick eylemi engeller (toparlanma penceresi). Cooldown eylemin KENDİ son tetiklemesinden sayar (tekrar sınırı).
Aynı oyuncu eylemleri, seed 42, 24 tick ile üç varyant koşuldu (`eylemler.json` bir kopyada değiştirildi, `yalan_soyle` ve `tehdit_et`):

|                                          | bugün (`hafiza_var`) | `hafiza_var` yerine `cooldown: 24` | `hafiza_var` yerine `cooldown: 3` |
| ---------------------------------------- | -------------------- | ---------------------------------- | --------------------------------- |
| Hatice yalanı                            | 2                    | 1                                  | 7                                 |
| `itiraf_etti` / `sakinlesti` / `patladi` | 1 / 1 / 1            | **0 / 0 / 0**                      | 5 / 8 / 3                         |
| Tehdit (Can / Selim)                     | 2 / 2                | 1 / 1                              | 8 / 5                             |
| Fren olayı                               | 0                    | 6                                  | 32                                |
| `son_hash`                               | `d98b50f39fc358dd`   | `47d73a8cc99b4a9f`                 | `69669ea2cb4bd8ba`                |

- `cooldown: 24`: eylemler tek seferlik olur, stres hiçbir zaman kriz eşiğine ulaşmaz, HanGerilim'in ana değişmezleri (iki yalan, bir itiraf, Selim'in patlaması) bozulur.
- `cooldown: 3`: kriz tekrar tekrar çıkar; krizden sonraki toparlanma modellenmez.
- Ön koşulların yanına `cooldown` eklemek de (ör. `tehdit_et`'e `cooldown: 2`) Can'ın t2 ve t3'teki iki tehdidini bire indirir ve hash'i değiştirir.

Cooldown'ın doğru kullanımı `samples/FrenliHan`'da gösterilir (kural tekrarı). Krizden sonra bekleme `hafiza_var` ile kalır.

## Ölçüm (7. hafta, T13; `tools/olcum`, benchmark değil)

Stres fikstürü (her NPC her tick eylem seçer), 168 tick, 3 çalıştırmanın ortancası, bu makinede. Önceki değerler 6. hafta sonundaki ölçümdür; Z3 (zihin kanonik metin önbelleği) ve 7. haftanın diğer işlerinden sonra:

| | Önceki (ms/tick) | Şimdi (ms/tick) | Değişim | 168 tick yükleme (baştan oynatma) |
|---|---|---|---|---|
| 8 NPC dedikodusuz | 2.52 | **2.08** | −17% | 0.35 sn |
| 8 NPC dedikodulu | 4.62 | **3.79** | −18% | 0.64 sn |
| 50 NPC dedikodusuz | 79.5 | **32.4** | −59% | 5.4 sn |
| 50 NPC dedikodulu | 523.1 | **183.7** | −65% | 30.9 sn (önceden ≈ 88 sn) |

- **5 ms hedefi:** 8 NPC'de TUTULDU (2.08 / 3.79). 50 NPC'de tutmuyor: dedikodusuz 32.4 ms (6.5×), dedikodulu 183.7 ms (37×).
- **Tick başına olay:** 8 NPC ≈ 96 (dedikodusuz), 50 NPC ≈ 1738 (dedikodusuz) ve ≈ 1698 (dedikodulu). Dedikodulu 50 NPC'de kanıtların %99.7'si `gordu` (126946 / 127268), NPC başına en çok 328 önerme; `karar_verildi` günlük boyutunun %10.7'si.
- **Fren olayı oranı:** stres fikstüründe 0 (fren alanı yok). Gerçek frenli koşular: `frenli-han-1` 8/675 = %1.19, `han-gerilim-1` 3/1693 = %0.18 (üç `en_fazla` freni).
- **Z3'ün kazancı:** önbelleksiz referans hesapla zihin hash'i 50 NPC dedikodusuzda tick başına 27.7 ms (tick süresinin ≈ %85'i), dedikodulu 63.1 ms; önbellek (bölüm önbellekleri + hafıza satır önbelleği) bu payın büyük kısmını kaldırdı. Kalan maliyet bu fikstürde her zihin her tick değiştiği için tam metni yeniden birleştirmek ve FNV ile hash'lemektir.
- **Kalan fark (50 NPC):** dedikodusuz 32.4 → 5 ms için ≈ 6.5×; dedikodulu 183.7 → 5 ms için ≈ 37×. Dedikodu maliyeti ≈ 151 ms/tick (183.7 − 32.4): konuşan başına O(n²) inanılan taraması (NPC başına 328 önerme) ve algı hacmi (≈ 1700 olay/tick).

**Sonraki optimizasyon adayları (VERİYLE sunulur, SEÇİLMEDİ; karar Yusuf'un):**
1. *Gossip: önerme dizini / yüklem ön filtresi.* `anlat` eyleminin `konu_desenleri`ne uymayan önermeler tarama dışına çıkar. Dedikodu maliyetinin (≈ 151 ms/tick) çoğu bu taramadadır; olay biçimi ve hash değişmez. Beklenen kazanç: dedikodulu 50 NPC'de büyük (tahmin: ≥ 2×), ölçülmeden söz verilmez.
2. *Z4: kanıt + hafıza olaylarının birleştirilmesi.* Algı başına iki olay yerine biri: olay sayısı ≈ %45 azalır (kanıt + hafıza ≈ olayların %89'u). **Olay biçimini değiştirir, bütün hash'leri kaydırır**; Yusuf'un onayı gerekir.
3. *Z4: silik hafızanın durumdan düşürülmesi / önem eşiği.* Hafıza büyümesini keser, davranışı ve hash'i değiştirebilir (algı hafızası önem 0.2).
4. *Hash'i artımlı yapmak.* FNV zincirleme olduğu için ön ekin yeniden kullanımı ancak ön ekteki zihinler değişmediğinde mümkündür; stres fikstüründe her zihin her tick değiştiğinden kazanç yok, gerçek senaryolarda küçük.
