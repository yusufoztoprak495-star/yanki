# Not: Algı (Perception)

> **Yazar: Claude Code** (Yusuf'un isteğiyle). **Karar (Yusuf)** satırları Yusuf'un kararlarıdır.

## Ne yapıyor?

Şimdilik yalnızca **doğrudan algı** var. Olay olunca görünürlüğüne uyan NPC'ler onu algılar:

| Görünürlük (`Yuk["gorunurluk"]`) | Kim algılar |
| --- | --- |
| `acik` (varsayılan) | Olayın `Mekan`'ındaki bütün NPC'ler + `Fail` |
| `sessiz` | `Fail` + olayın `Mekan`'ındaysa `Hedef` |
| `gizli` | Yalnızca `Fail` |

`DogrudanAlgi.Uret` her algılayan için iki olay üretir: `kanit_eklendi` (tür `gordu`, güç `0.9`) ve `hafiza_eklendi` (önem `0.5`). İkisi de `KuralId: algi` taşır ve algılanan olayı neden gösterir. Dünyaya ve günlüğe dokunmaz; olayları çağıran ekler.

Önerme şöyle türetilir: `gercek_eklendi` → o gerçek, `gercek_kaldirildi` → `degil(gercek)`, diğerleri → `<tur>(fail, hedef)`.

## Neden böyle?

- **Algı da olay üretir** (ADR-001). "Ayşe bunu neden biliyor?" sorusu `Neden(olayId)` ile `algi` kuralına ve gözlenen olaya iner. `yanki why` çıktısında bu zincir görünür.
- **Görünürlük olayın yükünde.** `Olay` tipine yeni alan gerekmedi.
- **Algılayan NPC olmalı.** Oyuncu, eşya ve mekanın zihni yok.
- **Saf fonksiyon.** `Uret` durumu değiştirmediği için çağrı sırası sonucu etkilemez.

## Yusuf'un kararları

- **Karar (Yusuf):** `gordu` gücü `0.9`, hafıza önemi herkese `0.5`.
- **Karar (Yusuf):** `sessiz` olayda `Hedef` olayın `Mekan`'ında olmalı; `Mekan` boşsa hedef algılamaz.
- **Karar (Yusuf):** Fail, her görünürlükte ve `Mekan` boş olsa bile kendi olayını algılar.
- **Karar (Yusuf):** Oyuncu NPC değildir, algılamaz.
- **Karar (Yusuf):** Bilinmeyen görünürlük değeri hatadır.
- **Karar (Yusuf, PR #7):** Algı yalnızca dış (zihin olmayan) olayları alır. Algı olayları kural tetiklemez.

## Sonraki haftalara kalanlar

Dedikodu, `duydu` aktarımı, konu sınırı ve yalan kaydı 6. haftada.

## Açık

- Önerme türetmede boş fail/hedef atlanır (`selamladi(Ali)`). İkisi de boşsa kanıt üretilmez, yalnızca hafıza yazılır.
- Önerme adı olay türünden gelir, bu yüzden olay türleri ASCII kimlik olmalı.
- Gözlenen kanıt çelişen eski inancı ezebilir (`degil(p)` 0.9, eski `duydu` 0.4). Dengesi dedikoduyla birlikte 6. haftada sınanacak.
