# Not: Zihin (Mind)

> **Yazar: Claude Code** (Yusuf'un isteğiyle). **Karar (Yusuf)** satırları Yusuf'un kararlarıdır.

## Ne yapıyor?

Her NPC'nin bir `Zihin`'i var, hepsi `ZihinDeposu`'nda sıralı durur. Zihin şunları taşır:

- **Kişilik:** `acgozluluk`, `durustluk`, `cesaret`, `sosyallik`, `kin_tutma`, `merhamet` (0–1). Senaryodan bir kez okunur, hiçbir olay değiştirmez.
- **İlişkiler:** kişi başına üç kalıcı eksen (`guven`, `sevgi`, `korku`, −100..100) ve süreli fikir modifier'ları. `Fikir(a, b, tick)` eksenlerin ağırlıklı toplamı + aktif modifier'lardır. `FikirDokumu` her katkıyı neden olayıyla verir.
- **Ruh hali:** tek baskın duygu (tür, şiddet, hedef, neden olay). Önceki duygu `OncekiDuygu` olarak kalır.
- **Stres:** 0–100, yavaş söner. Çelişen eylem seçilince artar (6. hafta, `stres.md`); eşikte kriz çıkar.
- **İnançlar:** önerme + kanıt listesi (`gordu`, `duydu`, `cikarim`). `Guven = 1 − Π(1 − güç)`. Kanıt silinmez, gücü zayıflar. Tek değerli önermenin farklı değerleri ya da `p` ile `degil(p)` çelişir.
- **Hafıza:** olay kimliği + önem. Önemi kalıcılık eşiğinin üstündeyse kalıcıdır, diğerleri silikleşir. Unutmak yalnızca sorgudan çıkmaktır; günlükten hiçbir şey silinmez.

Durum yedi olayla ilerler: `modifier_eklendi`, `eksen_degisti`, `duygu_degisti`, `stres_degisti`, `kanit_eklendi`, `hafiza_eklendi` ve (6. hafta) `aktarim_kaydedildi`.

**6. hafta:** `Kanit` söylentinin kökünü taşır (`Kok`; `gordu` için boş, etkin kökü sahibi). Aynı kökten `duydu` varsa yeni kanıt eklenmez, mevcut tazelenir; kökü sahibi olan `duydu` hiç yazılmaz (yankı freni). Zihin ayrıca konuşanın aktarım kaydını tutar (`Aktarimlar`, `YalanSoyledi(kime)`, `AktarimVar(onerme, kime)`); kayıt yokken `KanonikMetin` eskisiyle birebir aynıdır (`T|` satırı yalnızca kayıt varken). `EnGucluKanit` önermenin en güçlü tek güncel kanıtını verir (eşitlikte en küçük olay kimliği). Ayrıntı: `dedikodu.md`.

## Neden böyle?

- **Süreli değerler saklanmaz, sorgu anında hesaplanır** (ADR-008). Tick başına sönme olayı günlüğü şişirir ve zihni tick döngüsüne bağlardı. Durum ham değer + başlangıç tick'ini tutar, `Sonumleme` gerisini hesaplar.
- **Sönme doğrusal:** `değer − oran × geçenTick`, alt sınır 0. `decimal` ile birebir çalışır; üstel sönme `Math.Pow` isterdi.
- **Dünya altın hash'i değişmedi.** Zihin yoksa kanonik metne bir şey eklenmez. Zihin varsa `ZIHIN|1` bölümü gelir; sönmüş değer değil, ham durum girer.
- **Eksen olayı delta taşır.** Kırpma uygulanırken olur, "neden" daha okunur kalır.
- **İnanç bayrağı yok.** Kazanan sorgu anında bulunur. Kanıt sönünce kazanan değişebilir ve bunun için olay gerekmez.

## Yusuf'un kararları (PR #4)

- **Karar (Yusuf):** Eksen ağırlıkları `guven 0.4`, `sevgi 0.4`, `korku −0.2`.
- **Karar (Yusuf):** Toplam fikir −100..100'e kırpılır. Kırpıldıysa `FikirDokumu` sonda `kirpma` katkısı gösterir.
- **Karar (Yusuf):** Aynı modifier türünden en çok 3 kopya sayılır, **en yeniler** sayılır; çarpanlar başlangıç sırasıyla `1, 0.9, 0.81`.
- **Karar (Yusuf):** Modifier süresi sert biter.
- **Karar (Yusuf):** Sönme oranları duygu `0.05`, stres `0.2`, kanıt `0.001`, hafıza `0.002`; **1 tick = 1 saat** varsayımıyla.
- **Karar (Yusuf):** Eşit şiddetteki yeni duygu baskın olur, eskisi `OncekiDuygu` kalır. Şiddeti 0 olan duygu baskın olmaz. Karşılaştırma, mevcut duygunun sönmüş şiddetiyle yapılır.
- **Karar (Yusuf):** İki hafıza eşiği var: kalıcılık `0.7` (üstü), unutma `0.1` (altı ve eşiti sorgudan çıkar).
- **Karar (Yusuf):** Çelişkide güveni yüksek olan inanılır. Eşitse en yeni kanıtlı olan, o da eşitse hiçbiri. Kaybeden için `inanc()` 0 döner.
- **Karar (Yusuf):** Kanıt sönünce çelişki kararının değişmesi beklenen davranıştır.
- **Karar (Yusuf):** `inanc` ve `kaynak` önerme argümanında `degil(yuklem(...))` kabul eder, `gercek` etmez. `degil` yüklem adı olarak ayrılmıştır.
- **Karar (Yusuf):** `gordu` kanıtının kaynağı NPC'nin kendisidir.
- **Karar (Yusuf):** Aynı NPC'nin aynı önerme, tür ve kaynaktan tekrar eden kanıtı birikmez; tick yenilenir, güç ikisinin büyüğü olur.
- **Karar (Yusuf):** Oyuncunun ayrı varlık türü vardır, zihni ve algısı yoktur.
- **Karar (Yusuf, PR #7):** Aynı NPC ve aynı olay için tek hafıza kaydı tutulur (bkz. `kural-motoru.md`).

## Açık

- Stres deltası sönmüş güncel değere eklenir (50 → 10 tick sonra 40, +20 ile 60). Sönme oranı onaylandı, bu ekleme kuralı ayrıca konuşulmadı.
- Kişilik aralık dışıysa `ArgumentOutOfRangeException` verir. `yanki validate` geldiğinde dosya ve satırla gösterilecek.
