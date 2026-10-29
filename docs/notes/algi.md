# Not: Algı (Perception), doğrudan algı

> **Taslak (Claude Code).** Yusuf bunu kendi cümleleriyle yeniden yazacak. Aşağıdakiler "ne yaptım, neden" iskeletidir.

## Bu modül ne yapıyor?

Bu hafta yalnızca **doğrudan algı** var (plan 5.3, ilk madde). Bir olay olduğunda görünürlüğünü karşılayan NPC'ler onu algılar:

| Görünürlük (`Yuk["gorunurluk"]`)  | Kim algılar                                                         |
| --------------------------------- | ------------------------------------------------------------------- |
| `acik` (anahtar yoksa varsayılan) | Olayın `Mekan`'ında bulunan bütün NPC'ler (`konum!` gerçeğine göre) + `Fail` |
| `sessiz`                          | Yalnızca `Fail` ve `Hedef`                                          |
| `gizli`                           | Yalnızca `Fail`                                                     |

`DogrudanAlgi.Algilayanlar(dunya, olay)` NPC kimliklerini ordinal sıralı verir. `DogrudanAlgi.Uret(dunya, olay, gunluk)` her algılayan için sırayla iki olay üretir:

1. `kanit_eklendi`: tür `gordu`, güç `0.9`, kaynak = algılayan NPC.
2. `hafiza_eklendi`: algılanan olayın kimliği + önem `0.5`.

İkisinin de `NedenOlaylar`'ında algılanan olay, `KuralId`'sinde `algi` yazar. Üretilen olaylar günlüğe **eklenmez**; kimlik ve sıra günlüğün sonuna göre verilir,
çağıran sırayla ekler ve `ZihinDeposu.Uygula` ile işler. Önerme şöyle türetilir: `gercek_eklendi` → o gerçek; `gercek_kaldirildi` → `degil(gercek)`;
diğer türler → `<tur>(fail, hedef)`.

## Neden böyle?

- **Algı da olay üretir, durumu doğrudan değiştirmez** (ADR-001). Bu sayede "Ayse bunu neden biliyor?" sorusu `Neden(olayId)` zinciriyle `algi` kuralına ve gözlenen olaya kadar inebilir.
- **Görünürlük olayın yükünde**, Olay tipine yeni alan eklemeden (`Olay` değişmedi).
- **Fail/hedef NPC olmalı.** Dünyada NPC olarak kayıtlı olmayan varlıklar (oyuncu, eşya, mekan) algılayıcı sayılmaz; zihni olmayan bir varlığa olay üretmek hata verirdi.
- **Dünyadan okur, durumu değiştirmez.** `Uret` dünyaya ve günlüğe dokunmaz, saf bir fonksiyondur; çağrı sırası sonucu etkilemez.

## Bu hafta yazılmayanlar

Dedikodu (`anlat`, `duydu` kanıtı aktarımı), konu sınırı, yalan kaydı: 6. hafta. Tick döngüsünde algının hangi olayları alacağı (zihin olaylarının kendisinin algılanıp algılanmayacağı dahil): 4. hafta.

## Açık noktalar

Kararlı olanlar **Karar (Yusuf)** satırıyla işaretli (PR #4 incelemesinde verildi); işaretsizler hâlâ açık.

1. **`gordu` kanıt gücü `0.9`.** Aynı olay türü tekrarlanırsa (aynı önerme) kanıtlar birikir (`1 − 0.1 × 0.1 = 0.99`). Tekrar eden gözlem inancı bu kadar mı güçlendirsin?
   - **Karar (Yusuf):** Aynı NPC'nin aynı önerme, tür ve kaynaktan tekrar eden kanıtı birikmez, mevcut kanıt tazelenir (tick yeni, güç ikisinin büyüğü); bkz. `zihin.md`.
2. **Hafıza önemi `0.5` (herkes için aynı).** Kalıcılık eşiğinin (0.7) altında, yani algılanan her şey zamanla unutulur. Fail ve hedefin önemi daha yüksek (kalıcı) olsun mu?
   Önem olayın türüne ya da görünürlüğüne göre mi değişsin?
   - **Karar (Yusuf):** Hafıza önemi `0.5`, herkese aynı (mevcut davranış); `gordu` gücü `0.9`.
3. **`sessiz` olayda mekan aranmaz:** fail ve hedef, başka mekanda olsa bile algılar. Aynı mekanda olmaları şart mı?
   - **Karar (Yusuf):** `sessiz` olayda `Hedef`, olayın `Mekan`'ında bulunmalıdır (`Fail` mekandan bağımsız algılar; `Mekan` boşsa hedef algılamaz). Testler: `Sessiz_olayda_hedef_olayin_mekaninda_degilse_algilamaz`, `Mekani_olmayan_sessiz_olayi_hedef_algilamaz`.
4. **`acik` olayda `Mekan` boşsa kimse algılamaz.** Fail bile. Failın kendi eylemini her zaman algıladığı varsayımı gerekli mi?
   - **Karar (Yusuf):** Fail her görünürlükte ve `Mekan` boş olsa bile kendi olayını algılar. Testler: `Fail_her_gorunurlukte_ve_mekan_bos_olsa_bile_kendi_olayini_algilar`, `Fail_olayin_mekaninda_degilse_bile_acik_olayi_algilar`.
5. **Oyuncu bir NPC değil.** `oyuncu` varlığı dünyada `npc` türünde kayıtlıysa algılar ve zihni olması gerekir; değilse algılamaz. Oyuncunun varlık türü nedir?
   - **Karar (Yusuf):** Oyuncu NPC değildir, algılamaz, zihni yoktur.
6. **Önerme türetmede boş fail/hedef atlanır** (`selamladi(Ali)`); ikisi de boşsa kanıt üretilmez, yalnızca hafıza yazılır. Yeterli mi?
7. **Önerme adı olay türünden gelir** (`yardim_etti(oyuncu,Can)`). Olay türleri ASCII kimlik olmalı; kimlik olmayan bir tür `Gercek` doğrulamasında hata verir.
8. **Bilinmeyen görünürlük değeri hata** (`yari_acik` gibi). Sessizce `acik` sayılsın mı?
   - **Karar (Yusuf):** Kalsın: bilinmeyen görünürlük hatadır.
9. **Gözlenen kanıt çelişen eski bir inancı ezebilir** (ör. `gercek_kaldirildi` → `degil(p)` 0.9, eski `duydu` 0.4). Bu istenen davranış, ama dedikodu devreye girince dengesini sınamak gerek (6. hafta).
