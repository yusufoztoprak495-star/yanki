# Not: Dünya, anahtarlı RNG ve durum hash'i

> **Yazar: Claude Code** (Yusuf'un isteğiyle). **Karar (Yusuf)** satırları Yusuf'un kararlarıdır.

## Ne yapıyor?

- **Dünya:** varlıklar, mekanlar arası yönsüz bağlantılar ve **gerçekler** (nesnel olarak doğru önermeler, ör. `borclu(Mehmet, Ayse, 500)`). Durum yalnızca `Dunya.Uygula` ile, olaylar uygulanarak ilerler.
- **Tek değerli gerçekler:** `!` ile bitenler (`konum!(Ali, han)`). Yeni değer yazılınca eskisi kendiliğinden düşer.
- **Anahtarlı RNG:** `AnahtarliRng.Icin(seed, tick, anahtarlar...)`. Aynı girdi aynı akışı verir, hiçbir akış başkasının çağrı sırasından etkilenmez.
- **Durum hash'i:** durumun kanonik metni üzerinden 64 bit hash. İki koşunun nerede ayrıştığını bulmaya yarar.

Varlık türleri `npc`, `mekan`, `esya` ve `oyuncu`. Oyuncu ayrı bir türdür: `konum!` taşıyabilir, fail, hedef ve fikir hedefi olabilir ama zihni ve algısı yoktur. `Varliklar(Esya)` oyuncuyu döndürmez.

## Neden böyle?

- **Dünya olaylardan türer** (ADR-001). `Uygula` dışında bir değiştirme yolu yok.
- **`decimal` ve kanonik yazım** (ADR-007). `double` platformlar arası aynı biti garanti etmez. Sayılar `ToEven` ile 6 basamağa yuvarlanır, sondaki sıfırlar kırpılır. `ToEven` yarım değerleri hep yukarı atmadığı için toplu yuvarlamada kayma olmaz.
- **Sıralı koleksiyonlar.** Sırasız `Dictionary`/`HashSet` gezintisi yasak; bir test Core kaynağını yasaklı API'ler için tarar.
- **Kendi hash'imiz.** `string.GetHashCode` süreçten sürece değişir. FNV-1a + SplitMix64 küçük ve platformdan bağımsızdır.
- **Anahtarlar uzunluk önekli.** `("ab","c")` ile `("a","bc")` aynı tohumu üretmesin diye.
- **Kimlikler ASCII** (`[A-Za-z_][A-Za-z0-9_]*`). Hash metninde ve kriter dilinde `( ) , ! * |` ayraçlarıyla yan yana duruyorlar; dar alfabe kodlama ve ayraç karışıklığını önler.
- **Altın sabitler bağımsız hesaplandı.** Hash ve RNG sabitleri C# çıktısından kopyalanmadı, `tools/golden_ref.py` ile ayrıca üretildi. Kod belirtimden saparsa test kırılır.

## Yusuf'un kararı

- **Karar (Yusuf):** Görünen ad alanı 8. haftada diyalogla gelecek. O zamana kadar kimlikler ASCII kalır.

## Claude'un verdiği, Yusuf'un henüz ayrıca onaylamadığı kararlar

- Gerçek argümanları string'dir (kimlik ya da kanonik sayı); `*` yalnızca desenlerde geçer.
- Olmayan gerçeği kaldırmak hatadır; var olan gerçeği ya da bağlantıyı tekrar eklemek sessizce geçer.
- `Dunya.Uygula` dünya dışı olayı yoksayıp `false` döner, böylece tüm günlük verilebilir.
- Hash'te sürüm başlığı var (`YANKI-DURUM|1`); biçim değişirse sürüm artar.
- `Dictionary<` ve `HashSet<` yasaklı API testine eklendi.

## Reddedilen yollar

- Sabit noktalı `long`: hızlı ama her işlemde elle ölçekleme ister (ADR-007, Seçenek C).
- Tipli gerçek argümanları: şimdilik gereksiz karmaşa.

## Açık

- 7. haftadaki korumalı bayraklar ve yay değişkenleri hash'e girecek mi? Karar verilmedi.
