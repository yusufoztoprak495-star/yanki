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

## 5. hafta: algının zamanı, hareket ve NPC eylemleri

- **NPC eylemleri ve hareketleri de dış olaydır** ve aynı doğrudan algıdan geçer. `karar_verildi` olayı ise algılanmaz (zihinsel bir iz, dünyada görünen bir şey değil) ve kural tetiklemez; `konum!` değişimi olayları da algılanmaz (hareketin görünen yüzü `ayrildi`/`geldi`).
- **Algı zamanı.** NPC eylemlerinin ve hareketlerinin algısı tick adım 6.2'de, `konum!` değişimlerinden ÖNCE hesaplanır; yani algılayan kümesi adım 5 başındaki konum görüntüsüyle belirlenir. Aynı tick'te mekandan ayrılan tanık olayı görür, aynı tick'te varan görmez. `DogrudanAlgi`'ye özel durum eklenmedi: yazım sırası bunu kendiliğinden sağlıyor. Yer değiştiren iki NPC birbirinin `geldi` olayını görür.
- **Önerme mekanı taşır.** `ayrildi` ve `geldi` olaylarının `Hedef` alanı karşı mekandır; `OnermeUret` bunu `tur(fail, hedef)` olarak yazar: `ayrildi(Can,pazar)` (Can pazara gitti), `geldi(Can,han)` (Can han'dan geldi). Hareket olayları varsayılan olarak `acik` görünürlüktedir; oyuncu hareketine `gorunurluk` yükü verilebilir.
- **Algılayanlar saklanır.** Kurallar NPC eylemleri için bir sonraki tick'te çalıştığı için 6.2'nin algı sonucundan türetilen algılayan listesi bir tick saklanıp sonra atılır; kurallar konumdan yeniden hesaplamaz ve algı adımı bu olayları ikinci kez işlemez (bkz. `tick-dongusu.md`).
- **Oyuncu hareketi** adım 1'de işlenir, NPC konumları değişmez; oyuncunun `ayrildi`'sini kalkış mekanındaki, `geldi`'sini varış mekanındaki NPC'ler görür.

## 6. hafta: anlatma bir olaydır, içeriği algıdan geçmez

- `anlatti` (NPC ya da oyuncu anlatımı) mevcut doğrudan algıdan geçer; algı önermesi içeriksiz `anlatti(fail,hedef)`'dir (`OnermeUret` olay türünden `tur(fail, hedef)` üretir). **İçerik algıdan değil, `duydu` kanıtıyla** yalnızca dinleyene gider; böylece tanık (`acik` görünürlükte) konuşmanın olduğunu görür ama içeriğini almaz (kulak misafirliği yok, 7. hafta sonrasında bile ayrı karar).
- `anlatti` varsayılan olarak `sessiz`dir (konuşan ve dinleyen algılar); eylem tanımı `gorunurluk: acik` yazabilir. Konuşmada `gizli` anlamsızdır (reddedilir).
- Yalan algıyı etkilemez: dışarıdan görünüm doğru anlatımla aynıdır; yalan bilgisi yalnızca konuşanın zihnindeki `aktarim_kaydedildi` olayındadır ve bu olay algılanmaz.
- Kriz olayları (`olay_turu` satırdan) `acik` dış olaydır; aynı mekandaki herkes algılar.

## Sonraki haftalara kalanlar

Dedikodu, `duydu` aktarımı, konu sınırı ve yalan kaydı 6. haftada yapıldı (aşağıda ve `dedikodu.md`). Tam kayıt/yükleme, frenler, korumalı bayraklar 7. haftada.

## Açık

- Önerme türetmede boş fail/hedef atlanır (`selamladi(Ali)`). İkisi de boşsa kanıt üretilmez, yalnızca hafıza yazılır.
- Önerme adı olay türünden gelir, bu yüzden olay türleri ASCII kimlik olmalı.
- Gözlenen kanıt çelişen eski inancı ezebilir (`degil(p)` 0.9, eski `duydu` 0.4). Dengesi dedikoduyla birlikte 6. haftada sınanacak.
