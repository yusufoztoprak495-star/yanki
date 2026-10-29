# Not: Zihin (Mind)

> **Taslak (Claude Code).** Yusuf bunu kendi cümleleriyle yeniden yazacak. Aşağıdakiler "ne yaptım, neden" iskeletidir.

## Bu modül ne yapıyor?

Her NPC'nin zihni `Zihin` nesnesidir; hepsi `ZihinDeposu`'nda kimliğe göre sıralı durur. Zihin şunları taşır:

- **Kişilik:** `acgozluluk`, `durustluk`, `cesaret`, `sosyallik`, `kin_tutma`, `merhamet` (0–1). Senaryodan bir kez okunur; hiçbir olay değiştirmez
  (bir test bunu altı olay uygulayarak denetliyor).
- **İlişkiler:** kişi başına üç kalıcı eksen (`guven`, `sevgi`, `korku`, −100..100, taşarsa kırpılır) ve süreli **fikir modifier'ları**
  (tür, değer, başlangıç tick'i, süre, neden olay). Aynı türden kopyalar birikir: n'inci kopya `değer × 0.9^(n−1)`, tür başına en çok 3.
  `Fikir(a, b, tick)` = eksenlerin ağırlıklı toplamı + aktif modifier'lar. `FikirDokumu` her katkıyı neden olay kimliğiyle sıralı verir:
  "Can sana −22" satırının "neden"i buradan okunur.
- **Ruh hali:** tek baskın duygu (tür, şiddet, hedef, neden olay). Daha şiddetli yeni duygu baskın olur, eskisi `OncekiDuygu` kalır.
- **Stres:** 0–100, kırpılır, yavaş söner. Bu hafta yalnızca alan, olay ve sönme var; eylem bağlantısı ve kriz 6. haftada.
- **İnançlar:** önerme + kanıt listesi (`gordu`/`duydu`/`cikarim`; kaynak, tick, güç). `Guven = 1 − Π(1 − güç)`. Kanıt silinmez, gücü zayıflar.
  Çelişki: aynı anahtarlı tek değerli (`!`) önermenin farklı değerleri, ya da `p` ile `degil(p)`. Birikmiş güveni yüksek olan "inanılan"dır.
  `KimdenDuydu(npc, önerme)` `duydu` kanıtlarını sıralı verir; `*` joker her argüman konumunda çalışır.
- **Hafıza:** olay kimliği + önem. Önemi kalıcılık eşiğinin (0.7) **üstündeyse** kalıcı; diğerleri silikleşir ve unutma eşiğine inince sorgulardan
  çıkar. Olay günlüğünden hiçbir şey silinmez; unutmak yalnızca zihnin sorgusundan çıkmak demek.
- **Zihin olayları:** `modifier_eklendi`, `eksen_degisti`, `duygu_degisti`, `stres_degisti`, `kanit_eklendi`, `hafiza_eklendi`. `Zihin.Uygula(olay)`
  durumu ilerletir (`Dunya.Uygula` ile aynı desen). `ZihinOlaylari` olay kurucularını verir; sahibi yükteki `npc` anahtarıdır.
- **Kriter dili bağlantısı:** `ZihinSorguCozucusu`, sözlükteki 15 fonksiyonun 13'ünü gerçek veriye bağlar (11 zihin + `gercek` ve `ayni_mekanda`
  dünyadan). `yalan_soyledi` (6. hafta) ve `yay` (7. hafta) bilerek `NotSupportedException` verir.

## Neden böyle?

- **Süreli değerler saklanmaz, sorgu anında hesaplanır** (ADR-008). Her sönme için tick başına olay yazmak günlüğü şişirir ve zihni tick döngüsüne
  bağlar. Durum ham değer + başlangıç tick'ini tutar; `Sonumleme` saf fonksiyonları `(durum, simdikiTick) → değer` hesaplar. Sonuç: sorgu tick'i hash'i değiştirmez,
  zihin tick döngüsü olmadan test edilir.
- **Doğrusal sönme.** `değer − oran × geçenTick`, alt sınır 0. `decimal` ile birebir (ADR-007); üstel sönme `Math.Pow` ister.
- **Zihin durumu hash'e girer ama dünya hash'i değişmedi.** `DurumHashu.Hesapla(dunya, zihinler)`: zihin yoksa kanonik metne bir şey eklenmez, yani 2. haftanın altın hash'i (`b37a372bab89d9b4`) aynen duruyor; testle korunuyor. Zihin eklenince `ZIHIN|1` bölümü gelir (kişilik, ham durum; sönmüş değer değil).
- **Eksen olayı delta taşır, mutlak değer değil.** Kırpma uygulanırken olur, olay ters çevrilebilir okunur (`+30`, `−10`), "neden" daha açık.
- **Kanıt ve çelişki sorgu anında değerlendirilir.** "İnanılan" bir bayrak olarak saklanmaz; kanıt gücü sönünce kazanan değişebilir. Böylece bayrak güncellemek için olay gerekmez.
- **Hepsi sıralı koleksiyon, hepsi `decimal`.** Yasaklı API taraması (determinizm testi) `Mind/` ve `Perception/` için de çalışır.

## Açık noktalar (Yusuf karar versin)

Hepsi varsayılan olarak bir öneri; `ZihinAyarlari` ya da ilgili kod yerinden değişir.

1. **Eksen ağırlıkları:** `guven 0.4`, `sevgi 0.4`, `korku −0.2` (korku fikri düşürür). Toplam ağırlık 1.0 olduğundan eksenler tek başına ±100'ü aşmaz.
   Korku işareti: korkulan kişiye "soğuk" mu olunmalı?
2. **Fikir kırpma:** toplam fikir (eksen + modifier) −100..100'e kırpılıyor, döküm katkıları kırpılmamış. Modifier'lar tek başına kırpmayı aşabilir; böyle olsun mu?
3. **Birikme sınırı aşılınca:** tür başına en çok 3 kopya sayılıyor; fazlası **en eskilerden sonra gelenler** olarak sayılmıyor (eskisi dolunca devreye girer).
   Alternatif: en yeni 3 kopya sayılır ya da en güçlü 3. Hangisi?
4. **Modifier süresi sert biter.** Sürenin sonunda aniden düşer (solma yok). Planda böyle ("süresi dolunca düşer"); doğrusal solma ister misin?
5. **Tick = ne kadar oyun içi zaman?** Plandaki örnekler (`minnet 14 gün`, `hafiza_var(..., 168)`) 1 tick = 1 saat gibi duruyor; kod bunu varsaymıyor, ama oranlar buna göre seçildi.
6. **Sönme oranları (tick başına, doğrusal):**
   duygu şiddeti `0.05` (şiddet 1 → 20 tick), stres `0.2` (100 → 500 tick), kanıt gücü `0.001` (0.9 → ≈900 tick), hafıza önemi `0.002`.
   Üstel (yarı ömür) sönme istersen ADR gerekir (ADR-008 "yeniden bakılacak").
7. **Duygu eşitliği:** yeni duygu baskın olmak için eskisinden **kesin** daha şiddetli olmalı (eşitse eskisi kalır). Eski duygu sönmüşse (şiddet 0) herhangi bir yeni duygu baskın olur.
   `OncekiDuygu` sönmüş olsa da tutulur. Doğru mu?
8. **Hafıza eşikleri:** kalıcılık eşiği `0.7` (**üstü** kalıcı; tam 0.7 kalıcı değil). Planda "silikleşip eşik altına düşünce çıkar" der ama hangi eşik olduğunu söylemez;
   ben ayrı bir **unutma eşiği** ekledim, `0.1` (**≤ 0.1** olunca sorgudan çıkar). Tek eşik mi olsun?
9. **Çelişkide eşitlik kuralı:** güven eşitse **en yeni kanıtı olan** önerme inanılır; o da eşitse **hiçbiri** inanılmaz (`inanc(...)` 0 döner). Alternatif: eskisi kalsın, ya da ikisi de yarım güvenle.
10. **Çelişkide kaybeden `inanc()` 0 döner** (ham güven `Zihin.Guven` ile hâlâ okunur). Kriter dilinde kaybedenin ham güveni görünmüyor; görünsün mü?
11. **`degil(p)` kriter dilinde sorgulanamaz:** sözlük kuralı önerme içinde önerme yasaklıyor. Zihin `degil(p)` inançlarını tutuyor ve çelişkiyi çözüyor, ama `inanc(degil(...))` yazılamaz.
    Sözlük Yusuf'un alanı olduğundan dokunmadım; istenirse ayrı bir karar.
12. **`degil` yüklem adı olarak ayrıldı:** `degil(Ali)` gibi bir gerçek yazılamaz.
13. **Kanıt kaynağı `gordu` için NPC'nin kendisi** (doğrudan algıda). `kaynak(p, ben)` bu yüzden doğru döner. Başka bir değer istenir mi (ör. `gozlem`)?
14. **Kanıt gücü sönünce çelişki kararı da değişir:** eski kanıtla desteklenen inanç zamanla başka bir inanca yenilebilir. İstenen davranış bu mu?
15. **Neden olay kimliği:** bir modifier/duygu/kanıt, kendi eklendiği olayın kimliğini taşır (o olayın `NedenOlaylar` zinciri geriye gider). Eksen katkısı için son `eksen_degisti` olayı.
16. **Stres:** olay delta taşır, **sönmüş güncel değere** eklenir (ör. 50 → 10 tick sonra 40, +20 ile 60). Planda sönme hızı belirtilmediğinden `0.2` öneri.
17. **Kişilik aralık dışı değeri** (örn. 1.2) `ArgumentOutOfRangeException` ile reddediliyor; senaryo doğrulayıcı (yanki validate) bunu dosya/satırla gösterecek (4. hafta sonrası).
