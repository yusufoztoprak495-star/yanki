# Not: Kriter dili (Criteria)

> **Taslak (Claude Code).** Yusuf bunu kendi cümleleriyle yeniden yazacak. Aşağıdakiler "ne yaptım, neden" iskeletidir.

## Bu modül ne yapıyor?

Bir kriter, "şu sorgunun cevabı şu karşılaştırmayı sağlıyor mu?" sorusudur; ağırlığı ve zorunluluğu vardır. Modül:

1. `alan` metnini (`fikir(dinleyen)`, `gercek(konum!(Ali, han))`) ayrıştırır; hata olursa karakter konumunu söyler.
2. Ayrıştırılan çağrıyı sözlüğe göre doğrular (bilinen fonksiyon, doğru argüman sayısı, önerme yeri).
3. `Kriter.Olustur` operatörün ve değerin dönüş türüne uyduğunu doğrular (mantıksal değerde `<` olmaz).
4. Bir `ISorguCozucu` ile kriteri değerlendirir. Bu hafta yalnızca dünyaya dayanan `gercek` ve `ayni_mekanda` gerçekten çözülüyor;
   diğer 13 fonksiyonun çözümü zihin gelince (3. hafta).
5. Adayları puanlar (tutan kriterlerin ağırlık toplamı), zorunlu kriteri tutmayanı eler, en iyiyi seçer; puan eşitse daha çok
   kriteri tutanı (daha özgül), o da eşitse anahtarlı RNG kullanır ve her adayın **eşleşme izini** (hangi kriter tuttu/tutmadı) saklar.

## Neden böyle?

- **Tek dil, üç kullanım** (ADR-003): sonuç kuralları, diyalog şablonları, eylem ön koşulları aynı ayrıştırıcıyı,
  doğrulayıcıyı ve puanlayıcıyı paylaşıyor. Biri değişirse hepsi birlikte değişiyor.
- **Sözlük kodda sabit ve `docs/criteria-language.md` ile eşleşmek zorunda.** Bir test docs tablosunu okuyup koddaki 15
  tanımla (ad, argüman sayısı, dönüş türü, örnek kriter) karşılaştırıyor; sözlük Yusuf'un tasarım alanı olduğundan, docs
  değişip kod değişmezse (ya da tersi) test kırılır.
- **Çözücü arayüzü.** Kriter motoru dünyayı, zihni ya da hafızayı doğrudan bilmiyor; `ISorguCozucu` ile soruyor. Böylece
  zihin gelince motor değişmeden yeni çözücü takılıyor, testlerde sahte çözücü kullanılıyor.
- **Tüm kriterler değerlendirilir, kısa devre yok.** Zorunlu bir kriter tutmasa bile diğerlerinin sonucu iz için lazım
  (`yanki why` ve kapsama raporu).
- **Puan eşitse özgül olan kazanır; o da eşitse adaylar kimliğe göre sıralanır, sonra RNG seçer.** (ADR-003: "en çok kriteri
  sağlayan en özgül kazanır; eşitlikte anahtarlı RNG".) Özgüllük = tutan kriter sayısı; tutmayan kriterler sayılmaz. Böylece sonuç, adayların listeye hangi sırayla verildiğinden
  bağımsız; yalnızca (seed, tick, anahtar) belirliyor. Eşitlik yoksa RNG'den değer çekilmiyor.

## Benim (Claude'un) verdiğim kararlar — Yusuf onaylasın

- Mantıksal, varlık ve bant dönüşlerinde yalnızca `==` ve `!=` geçerli (bantlar sıralı olsa da `<` yok).
- `fikir_bandi` değeri `dusman`, `soguk`, `notr`, `yakin`, `dost` dışında olamaz; `eksen`'in üçüncü seçeneği
  (`guven|sevgi|korku`) şimdilik doğrulanmıyor, yalnızca argüman sayısı.
- Önerme (`yuklem(a, b)`) yalnızca `inanc`, `kaynak`(ilk argüman) ve `gercek` içinde; önerme içinde önerme ve önerme dışında
  `*` yok. Önermenin en az bir argümanı olmalı.
- `olay` tek başına geçersiz (yalnızca `olay.fail`, `olay.hedef`).
- Rol bağlamda tanımsızsa sessiz `false` yerine açık hata.
- `agirlik` negatif olabilir (ceza gibi); sınır koymadım.
- Kriterin `deger`i varlık ise kimlik ya da rol adı (`oyuncu`, `olay.fail` ...) olabilir.

## Alternatifler

- Kriterleri baştan JSON'dan okumak: 2. haftanın kapsamı dışı; JSON okuma kural motoruyla (4. hafta) gelecek.
- Sözlüğü JSON/dosyadan yüklemek: sözlük "sabit, belgelenmiş küçük bir set" olmalı (plan 5.9); kodda tutmak ve docs ile
  testle eşitlemek daha basit.

## Emin olmadığım / Yusuf'un bakması gereken noktalar

- `gercek(...)` ve `inanc(...)` önermelerinde `*` her argüman konumunda mı serbest olmalı (yüklemin kendisinde değil)?
  - **Karar (Yusuf):** `*` önermenin her argüman yerinde serbesttir (ör. `borclu(*, Ayse, *)`). Testler: `Gercek_joker_her_arguman_yerinde_serbest` ve sözlük doğrulama testleri (`inanc`, `gercek`, `kaynak`).
- `ayni_mekanda(hedef)`: iki tarafın da `konum!` gerçeği yoksa `false` döndürüyorum; "bilinmiyor" ayrı bir durum olmalı mı?
  - **Karar (Yusuf):** Konum bilinmiyorsa `false`, hata yok. Mevcut davranış bu; `AyniMekanda_konumu_olmayan_varlik_icin_false` testi korur.
- Puan eşitliğinde RNG yerine ikinci bir kriter (öncelik) istenir mi?
  - **Karar (Yusuf):** Puan eşitse daha çok kriteri tutan aday (daha özgül olan) kazanır; o da eşitse anahtarlı RNG. ADR-003'ün
    "en özgül kazanır, eşitlikte RNG" kuralıyla uyumlu (puan birincil, özgüllük ikinci, RNG üçüncü); yeni ADR gerekmedi.
    Testler: `Ayni_puan_farkli_kriter_sayisi_ozgul_olan_kazanir`, `Puan_ozguluktan_once_gelir`,
    `Puan_ve_ozgulluk_ikisi_de_esitse_anahtarli_rng_secer`, `Ozgulluk_yalnizca_tutan_kriterleri_sayar`.
