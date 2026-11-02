# Not: Karar (Utility AI) ve NPC'lerin kendi eylemleri

> **Yazar: Claude Code** (Yusuf'un isteğiyle). **Karar (Yusuf)** satırları Yusuf'un kararlarıdır. Ayrıntılı karar kaydı: [ADR-011](../adr/0011-karar-modeli.md).

## Ne yapıyor?

Her tick'te her NPC kendi zihninden bir eylem seçer, seçim olaya dönüşür, başka NPC'ler onu algılar ve sonuç kuralları çalışır. Boru hattı:

```
eylem JSON ──► AdayUretici ──► Puanlayici ──► UtilitySelector ──► karar_verildi + eylem olayı ──► algı ──► kurallar
(hedef, ön koşul)  (eylem,hedef)   taban × ölçütler       ilk 3, %70,        (günlükte kısa iz)         (sonraki tick)
                                   × kişilik×fikir×ruh    puan ağırlıklı
```

- **Eylem** (`samples/.../eylemler.json` gibi dosyalarda): `id`, `olay_turu`, `hedef`, `taban`, `on_kosullar`, `olcutler`, üç çarpan tanımı, `celisen_ozellikler`, `etkiler`. Kriterler ve etkiler mevcut kriter dili ve kural etkisidir; ikinci bir sistem yok.
- **Aday** = (eylem, hedef). Hedef türleri: hedefsiz, aynı mekandaki NPC, aynı mekandaysa oyuncu, bağlı komşu mekan (hareket).
- **Puan** bileşenleri izde görünür: her ölçütün ham girdisi, normalize hali ve eğri çıktısı; üç çarpan; son puan.
- **Seçim:** puanı en yüksek adayın %70'ine ulaşan ilk 3 aday arasından puan ağırlıklı; `N=1` argmax. Zar `AnahtarliRng.Icin(Seed, tick, npcId, "karar")`.
- **Olaylar:** `karar_verildi` (`KuralId: karar`), eylem olayı (`KuralId: eylem:<id>`), hareket eyleminde `ayrildi` ve `geldi`; sonra algı olayları; en sonda eylem etkileri ve `konum!`.
- **Hareket:** oyuncu ve NPC aynı mekanizmayı kullanır. Oyuncu `hareket` eylemini gönderir (`yanki run --eylem hareket:pazar`).
- **`yanki why <oturum> <olayId>`:** zincir modifier'dan algıya, eylem olayına ve karar olayına iner; karar olayının altına adaylar ve puan bileşenleri yazılır. **`yanki query <oturum> <npc>`:** NPC durumunun sonunda son kararı gösterir.

## Neden böyle?

- **LLM karar vermez (ADR-002).** Seçim kural + utility + anahtarlı RNG'dir; aynı seed ve girdi aynı sonucu verir.
- **Tek durum görüntüsü.** Bütün NPC'ler adım 5 başındaki aynı durumu görür ve kararlar hiçbiri yazılmadan hesaplanır. Aksi halde NPC işleme sırası sonucu değiştirirdi. Kurallardaki tek durum görüntüsü kararıyla aynı ilke.
- **Eş zamanlı algı.** Algı adım 5 başındaki konum görüntüsüne göre hesaplanır, çünkü `konum!` değişimi en son yazılır: aynı tick'te ayrılan tanık olayı görür, varan görmez. `DogrudanAlgi`'ye özel durum gerekmedi. Algılayanlar sonraki tick'in kural değerlendirmesinde yeniden hesaplanmaz, adım 6'nın algı sonucundan saklanır; yoksa kurallar "o anki konuma" göre yanlış NPC'ye çalışırdı.
- **Günlükte kısa iz, tam iz yeniden üretilir (K3).** Ölçüm göstermiştir ki tam iz günlüğün %70'ini yiyordu ve aday sayısıyla büyüyordu (aşağıda). Simülasyon deterministik olduğu için tam iz, oturum o karara kadar replay ile oynatılarak üretilir ve günlükteki kısa izle doğrulanır; uyuşmazlık sessizce geçmez. Bellekte NPC başına yalnızca son tam iz kalır, geçmiş izler açıkça istenir.
- **Karar olayının nedenleri yalnızca seçilen adayın girdileridir.** "Neden bu eylemi seçti?" sorusunun cevabı seçilen adayın okuduğu zihin kaynaklarıdır; kaybeden adayların girdileri nedensellik zincirini şişirirdi.
- **Kaynağı olmayan girdi uydurulmaz.** Birikmiş eksen değeri tek bir olaydan gelmez (yalnızca son değişim olayı bilinir; kaynak gösterilir ve birikim notu düşülür), stres süreli sönen bir değerdir ve olaya bağlı tutulmaz. Bunlar izin `kaynaksiz` listesinde raporlanır.
- **Aday yoksa olay yok, aday var ama seçim yoksa boş seçimli olay var.** Ön koşuldan sonra hiç aday kalmayan NPC için her tick bir olay yazmak günlüğü boşuna şişirirdi.
- **Mekan hedefli olaylar.** `ayrildi`/`geldi` olaylarının `Hedef`'i karşı mekandır (`ayrildi(Can,pazar)`), böylece algıdan doğan inanç mekanı taşır. Kural mekana modifier/eksen/duygu yazamaz: doğrulayıcı bildiği yerde reddeder, bilemediği yerde (hedefi mekan olan oyuncu eylemi) kural motoru etkiyi üretmez ve zihne mekan adına kayıt yazılmaz. Davranış testle korunuyor. 7. haftada bu atlama için fren olayı yazılmalı.

## Yusuf'un kararları

- **Karar (Yusuf):** Hedef katmanı v0.1'de yok (v0.2 sonrası). Stres 6. haftada; `celisen_ozellikler` bu hafta yalnızca izde görünür.
- **Karar (Yusuf):** `eylem.hedef` rolü (seçenek A); çarpan eşlemesi parçalı doğrusal, nötr 1.0 (kişilik 0.25–2.0, fikir 0.6–1.5, ruh hali 0.8–1.25).
- **Karar (Yusuf):** Eş zamanlı algı, hareket olayları (`ayrildi` ve `geldi`, `konum!` ayrı olay), decimal eğriler (`e^y` elle yazılmış seri).
- **Karar (Yusuf):** K3 (kısa iz günlükte, tam iz replay ile) + Z1 (`HafizaVar` erken durdurma) + Z2 (hedefe bağlı olmayan ön koşul önbelleği) bu hafta; Z3 ve Z4 7. haftaya.

## Ölçüm: önce / sonra (benchmark DEĞİL)

`tools/olcum` ile, 5 eylem + 3 kural + sabit seed'li kişilikler, 168 tick. Bu bir **stres fikstürüdür**: hedefsiz `dinlen` eylemi her zaman aday olduğu için her NPC her tick bir eylem seçer. Süreler bir makinenin (20 işlemci, .NET 10) tek ölçümüdür; yayınlanacak benchmark tablosu 11. haftada üretilir. Karar girdisidir.

| ölçü                      | 3 NPC önce → sonra | 10 NPC önce → sonra   | 50 NPC önce → sonra     |
| ------------------------- | ------------------ | --------------------- | ----------------------- |
| olay sayısı               | 3 980 → 3 980      | 23 335 → 23 335       | 291 785 → 291 785       |
| günlük boyutu             | 884.3 → 438.0 KiB  | 5 969.7 → 2 405.5 KiB | 89 798.9 → 28 755.4 KiB |
| `karar_verildi` payı      | %69.5 → %38.5      | %69.4 → %24.0         | %71.3 → %10.4           |
| `karar_verildi` bayt/olay | 1 249 → 342        | 2 524 → 352           | 7 807 → 366             |
| tick başına ortalama      | 1.465 → 0.843 ms   | 5.868 → 4.270 ms      | 321.1 → 82.1 ms         |
| son durum hash'i          | aynı               | aynı                  | aynı                    |

"Önce" K1 (tam iz günlükte) ve Z1/Z2 öncesi; "sonra" K3 + Z1 + Z2 sonrası. Olay sayıları ve son durum hash'i (`3`: `5e3182d836f1850a`, `10`: `9a765f3fcd99192b`, `50`: `cde13bcacae65f9a`) iki durumda da birebir aynı: kararlar değişmedi. 50 NPC için "önce" 5, "sonra" 3 çalıştırmanın ortancası; 3 ve 10 NPC için 5'er.

**Hâlâ 5 ms'nin üstünde (50 NPC, ≈82 ms/tick):** kalan maliyetin büyük kısmı bu hafta dokunmadığımız iki yerde: durum hash'inin her tick'te bütün zihin durumunu yazması (Z3) ve algı başına iki olay yazılması ile hafızanın sınırsız büyümesi (Z4; olay sayısının %89'u kanıt + hafıza algı olayı). Bkz. `DURUM.md` açık kararlar.

## Bulgu: bekleme eylemi yoksa NPC'ler sürü halinde dolaşır

Senaryo taslaklarını çalıştırırken görüldü: yardım ve selam `hafiza_var` sınırına takılınca geriye puanı 0'dan büyük **tek aday** olarak `git` kaldı (küçük de olsa pozitif) ve üç NPC de tick 3'ten itibaren her tick birlikte mekan değiştirdi. Seçici yalnızca puanı 0'dan büyük adaylardan seçer; "hiçbir şey yapma" kendiliğinden bir aday değildir.

**Karar (Yusuf):** Her senaryoya puanı 0'dan büyük, hedefsiz, ön koşulsuz küçük bir bekleme eylemi (`dinlen`) yazmak `docs/authoring-guide.md` içinde **şiddetle önerilir**. Motor bunu zorunlu kılmaz ve yerleşik bir bekleme eylemi eklemez: bekleme, senaryonun kişiliğine ve dengesine ait bir yazar kararıdır. `samples/MiniKarar` `dinlen` içerir.

## Tekrar sınırı: `hafiza_var(olay_turu, ben, n)` yeterli mi?

Aynı eylemin her tick tekrarı cooldown yazmadan `hafiza_var(selam_oldu, ben, 5) == false` ön koşuluyla sınırlanabilir (`TekrarSiniriTests`). Gözlemler ve **sınırlar**:

- **Çalışıyor:** pencere 5 iken eylem tick 2'de seçilir, tick 3..7 engelli (fark 1..5), tick 8'de yeniden serbest: döngü pencere + 1 tick. Engelli tick'lerde NPC boşta kalmaz, yedek eylem seçilir.
- **Hedefe göre değil, eylem türüne göre:** `hafiza_var` yalnızca (olay türü, fail, pencere) alır. "Aynı kişiye tekrar etme ama başkasına edebilirsin" yazılamaz.
- **Hafıza unutulunca sınır kalkar:** algıdan gelen hafıza önemi 0.5'tir ve 200 tick sonra unutulur; pencere 500 olsa bile sınır 200. tick'te kalkar. Büyük pencereler güvenilmez (kalıcı hafıza için önem > 0.7 gerekir).
- **"En fazla k kez" ya da tick başına sınır ifade edilemez**, yalnızca "son n tick'te hiç yapmadı".

Bu hafta için yeterli; cooldown, tetiklenme sınırı ve fren olayları 7. haftada (görev metni).

## Açık noktalar

- Z3 ve Z4 (7. hafta, Yusuf'un kararı bekleniyor): bkz. `DURUM.md`.
- Mekan hedefli olayda atlanan etki için fren olayı (7. hafta).
- Eksenin toplam değerinin tek kaynak olayı yok; şimdilik son değişim olayı kaynak gösteriliyor ve birikim `kaynaksiz` içinde not ediliyor. Eksen değişimlerinin tümünü izlemek gerekirse zihin modelinin değişmesi gerekir.
- `komsu_mekan` eyleminde `eylem.hedef` bir mekan olduğundan hedefe karşı fikir/ilişki hesabı yok; mekana karşı ilişki ihtiyacı çıkarsa ayrı bir karar gerekir.
