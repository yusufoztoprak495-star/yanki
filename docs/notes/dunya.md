# Not: Dünya (World), anahtarlı RNG ve durum hash'i

> **Taslak (Claude Code).** Yusuf bunu kendi cümleleriyle yeniden yazacak. Aşağıdakiler "ne yaptım, neden" iskeletidir.

## Bu modül ne yapıyor?

- **Dünya:** varlıklar (NPC, mekan, eşya), mekanlar arası yönsüz bağlantılar ve **gerçekler** (dünyada nesnel olarak doğru
  önermeler: `borclu(Mehmet, Ayse, 500)`). Durum yalnızca olaylar uygulanarak ilerliyor (`Dunya.Uygula`).
- **Tek değerli gerçekler:** `konum!(Ali, han)` gibi `!` ile bitenler. Yeni değer yazılınca eskisi otomatik düşüyor; kurallarda
  elle silmek gerekmiyor.
- **Anahtarlı RNG:** `AnahtarliRng.Icin(seed, tick, anahtarlar...)`. Aynı girdi her zaman aynı akış; hiçbir akış başkasının
  çağrı sırasından etkilenmiyor.
- **Durum hash'i:** dünya durumunun kanonik metni üzerinden 64 bit hash. İki koşunun nerede ayrıştığını bulmak için.

## Kimlikler ASCII'dir

Varlık kimlikleri, yüklem adları ve gerçek argümanları ASCII'dir (`[A-Za-z_][A-Za-z0-9_]*`). Türkçe karakterli, oyuncuya görünen
ad (ör. "Ayşe") **ileride ayrı bir alan olacak**; kimlik ile görünen ad ayrı şeyler. Bu hafta o alan yazılmadı. Neden böyle:
kimlikler hem kanonik hash metninde hem kriter dilinde ayraç olarak `( ) , ! * |` ile yan yana duruyor; ASCII ve kısıtlı bir
alfabe kodlama ve ayraç karışıklığı ihtimalini sıfırlıyor.

## Neden böyle?

- **Dünya olaylardan türetilir** (ADR-001). Dünyanın `Uygula` dışında değiştirilebilen bir yolu yok.
- **`decimal` ve kanonik yazım** (ADR-007). `double` platformlar arası bit düzeyinde aynı sonucu garanti etmez; hash ve golden
  replay buna bağlı. Sayılar `MidpointRounding.ToEven` ile 6 basamağa yuvarlanıp sondaki sıfırları kırpılarak yazılıyor.
  `ToEven`'in seçilme sebebi: yarım değerler hep yukarı gitmez, toplu yuvarlamada sistematik kayma olmaz.
- **Sıralı koleksiyonlar.** Sırasız `Dictionary`/`HashSet` gezintisi yasak; hepsi `SortedDictionary`/`SortedSet` +
  `StringComparer.Ordinal`. Bir test, Core kaynağında yasaklı API'lerin geçmediğini tarıyor.
- **Kendi hash'imiz.** `string.GetHashCode` süreçten sürece değişir. FNV-1a + SplitMix64 karıştırıcısı küçük, platformdan
  bağımsız ve bilinen test vektörleri var (testlerde).
- **Anahtarlar uzunluk önekli.** `("ab","c")` ile `("a","bc")` aynı tohumu üretmesin diye.
- **Altın sabit testi bağımsız hesaplandı.** Hash ve RNG sabitleri C# çıktısından kopyalanmadı; aynı algoritmayı ayrı bir
  betikte uygulayıp çapraz doğruladım. Böylece kod ile belirtim ayrışırsa test kırılır.

## Benim (Claude'un) verdiğim kararlar — Yusuf onaylasın

- Gerçek argümanları string (kimlik ya da kanonik sayı); `*` yalnızca desenlerde.
- Olmayan gerçeği kaldırmak hata; var olan gerçeği ya da bağlantıyı tekrar eklemek sessizce geçer.
- `Dunya.Uygula` dünya dışı olayı yoksayıp `false` döner (tüm günlük verilebilsin diye).
- Hash'te sürüm başlığı var (`YANKI-DURUM|1`, `YankiInfo.EventLogFormatVersion`); biçim değişirse sürüm artar.
- `Dictionary<` ve `HashSet<` yasaklı API testine eklendi (görev metnindeki listeye ek).

## Alternatifler

- Sabit noktalı `long` (ADR-007 Seçenek C): daha hızlı ama her işlemde elle ölçekleme.
- Gerçek argümanlarına tipli değerler (string/decimal ayrımı): şimdilik gereksiz karmaşa.

## Emin olmadığım / Yusuf'un bakması gereken noktalar

- Görünen ad alanı (yerelleştirme) ne zaman ve nerede? Diyalog haftasına (8. hafta) mı, varlık tanımına mı?
- Korumalı bayraklar (5.10) ve yay değişkenleri 7. haftada; hash'e girecekler mi? (Muhtemelen evet.)
