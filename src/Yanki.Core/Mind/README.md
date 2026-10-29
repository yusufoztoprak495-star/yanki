# Mind

Zihin: kişilik, 3 kalıcı ilişki ekseni + süreli fikir modifier'ları, tek duygulu ruh hali, stres, hafıza ve kanıt listeli inançlar (plan 5.4).
Durum yalnızca zihin olaylarıyla ilerler; süreli değerler sorgu anında tick'ten hesaplanır ([ADR-008](../../../docs/adr/0008-zihin-olaylardan-turetilir.md)).

**Durum:** 3. hafta teslimi tamam. Tick döngüsüne bağlama 4. hafta; stres eylem bağlantısı ve kriz olayları 6. hafta; frenler, korumalı bayraklar ve yay 7. hafta.

## Tipler

| Tip | Ne yapar |
|---|---|
| `Zihin` | Bir NPC'nin zihni. `Uygula(olay)`, `Fikir`, `FikirDokumu`, `Eksen`, `Guven`, `Inanilan`, `InancDerecesi`, `KimdenDuydu`, `Hafiza`, `Stres`, `DuyguSiddeti`, `KanonikMetin`. |
| `ZihinDeposu` | Bütün zihinler (ordinal sıralı); olayları sahibine yönlendirir; `Fikir(a, b, tick)`, `FikirDokumu`, `KimdenDuydu(npc, onerme)`. |
| `Kisilik` | Altı özellik (0–1); kurulduktan sonra değişmez. |
| `ZihinAyarlari` | Eksen ağırlıkları, birikme sınırı/çarpanı, sönme oranları, hafıza eşikleri (varsayılanlarıyla). |
| `Sonumleme` | Saf sönme fonksiyonları `(durum, simdikiTick) → değer`: modifier, duygu, stres, kanıt gücü, hafıza. |
| `ZihinOlaylari` | Altı olay türü ve kurucuları (`modifier_eklendi`, `eksen_degisti`, `duygu_degisti`, `stres_degisti`, `kanit_eklendi`, `hafiza_eklendi`). |
| `Onerme`, `OnermeDeseni` | İnancın konusu (`p` ya da `degil(p)`) ve `*` jokerli desen; çelişki kuralı. |
| `FikirBandi`, `FikirDokumu` | `dusman < −40 ≤ soguk < −10 ≤ notr ≤ 10 < yakin ≤ 40 < dost`; "neden kızgın?" dökümü. |
| `DurumHashu` | Dünya + zihin birleşik durum hash'i. Zihin yokken dünya hash'iyle aynıdır. |

Kriter diliyle bağlantı: [`Criteria/ZihinSorguCozucusu`](../Criteria/ZihinSorguCozucusu.cs).
Taslak not ve açık noktalar: [`docs/notes/zihin.md`](../../../docs/notes/zihin.md).
