# Not: Kriter dili (Criteria)

> **Yazar: Claude Code** (Yusuf'un isteğiyle). **Karar (Yusuf)** satırları Yusuf'un kararlarıdır.

## Ne yapıyor?

Kriter, "şu sorgunun cevabı şu karşılaştırmayı sağlıyor mu?" sorusudur; ağırlığı ve zorunluluğu vardır. Modül beş iş yapar:

1. `alan` metnini ayrıştırır (`fikir(dinleyen)`, `gercek(konum!(Ali, han))`). Hatada karakter konumunu söyler.
2. Çağrıyı sözlüğe göre doğrular: fonksiyon var mı, argüman sayısı doğru mu, önerme yerinde mi?
3. `Kriter.Olustur` ile operatör ve değerin dönüş türüne uyduğunu denetler.
4. `ISorguCozucu` üzerinden kriteri değerlendirir.
5. Adayları puanlar, zorunlu kriteri tutmayanı eler, en iyiyi seçer ve her aday için eşleşme izini saklar.

Sözlükte 18 fonksiyon var (6. hafta: `konu_puani`, `olay_konusu`). `yay` (7. hafta) dışındakiler gerçek veriye bağlı.

## Neden böyle?

- **Tek dil, üç kullanım** (ADR-003). Sonuç kuralları, diyalog şablonları ve eylem ön koşulları aynı ayrıştırıcıyı ve doğrulayıcıyı paylaşır.
- **Sözlük kodda sabit, `docs/criteria-language.md` ile eşit olmak zorunda.** Bir test docs tablosunu okuyup kodla karşılaştırır. Sözlük Yusuf'un alanı; docs ve kod ayrışırsa test kırılır.
- **Motor dünyayı bilmez.** `ISorguCozucu` ile sorar. Zihin geldiğinde motor değişmeden yeni çözücü takıldı, testlerde sahte çözücü kullanılıyor.
- **Kısa devre yok.** Tutmayan zorunlu kriterden sonra da hepsi değerlendirilir, çünkü sonuç `why` ve kapsama raporu için lazım.
- **Seçim sırası:** önce puan, sonra özgüllük (tutan kriter sayısı), sonra anahtarlı RNG. Adaylar RNG'den önce kimliğe göre sıralanır, yani sonuç listenin sırasına bağlı değildir. Eşitlik yoksa RNG'den değer çekilmez.
- **Sonuç kuralları bu puanlamayı kullanmaz.** Kural yalnızca tüm kriterleri tutunca çalışır (ADR-010).

## Yusuf'un kararları

- **Karar (Yusuf):** `*` önermenin her argüman yerinde serbesttir (`borclu(*, Ayse, *)`).
- **Karar (Yusuf):** Konum bilinmiyorsa `ayni_mekanda` `false` döner, hata vermez.
- **Karar (Yusuf):** Puan eşitse daha özgül aday kazanır, o da eşitse anahtarlı RNG. Yeni ADR gerekmedi.
- **Karar (Yusuf):** Hedef/tanık ayrımı için `olay_rolu(fail|hedef|tanik)` sözlüğe eklendi.

## Claude'un verdiği, Yusuf'un henüz ayrıca onaylamadığı kararlar

- Mantıksal, varlık ve bant dönüşlerinde yalnızca `==` ve `!=` geçerli.
- `fikir_bandi` değeri `dusman`, `soguk`, `notr`, `yakin`, `dost` dışında olamaz.
- Önerme yalnızca `inanc`, `kaynak` (ilk argüman) ve `gercek` içinde geçer; en az bir argümanı olmalı. `degil(...)` istisnası için bkz. `zihin.md`.
- `olay` tek başına geçersizdir, yalnızca `olay.fail` ve `olay.hedef` var.
- `agirlik` negatif olabilir, sınır yok.
- Kriterin `deger`i varlık ise kimlik ya da rol adı olabilir.

## Reddedilen yol

- Sözlüğü dosyadan yüklemek: sözlük sabit ve belgeli küçük bir set olmalı. Kodda tutup testle eşitlemek daha basit.
