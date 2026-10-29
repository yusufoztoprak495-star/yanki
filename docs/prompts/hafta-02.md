<!-- Claude Code'a verilen görev metni, birebir. Hazırlandığı yer: claude.ai. Tarih: 29 Ekim 2026. -->

# Görev: Yankı — 2. hafta (Olay kaydı, dünya, kriter dili)

## Bağlam
Plan: docs/plan/Yanki Plan v2.pdf. Bu görev SADECE 2. hafta. Önce planın 5.1, 5.2, 5.9, 6 ve 9. bölümlerini, CLAUDE.md'yi, docs/criteria-language.md'yi ve ADR-001..006'yı oku.
3. haftaya ait hiçbir şeyi yazma: kişilik, ilişki, ruh hali, inanç, hafıza, algı, karar, kural motoru.
`main`'i çek, `hafta-02` dalı aç.

## Değişmez kurallar
- Her şey `Yanki.Core` içinde, LangVersion 9.0, hiçbir NuGet paketi yok. Kod iki hedefte de (netstandard2.1, net10.0) derlenmeli.
- Determinizm:
  - `System.Random`, `DateTime.Now`, `Guid.NewGuid` yasak.
  - `string.GetHashCode()` ve `HashCode` yasak (süreçten sürece değişir).
  - `Dictionary`/`HashSet` üzerinde sırasız gezinme yasak. Gezilecek koleksiyonlar sıralı olmalı (ordinal string karşılaştırması).
  - `float`/`double` simülasyonda yasak; sayısal değerler `decimal`.
- TDD: her parça için önce kırmızı test.

## Teslimler

### 1. ADR-007: Sayısal temsil `decimal`
- Bağlam: double platformlar arası (net10 / WASM) bit düzeyinde aynı sonucu garanti etmez; durum hash'i ve golden replay bunu gerektirir.
- Seçenekler: double / decimal / sabit noktalı int. Karar: decimal (deterministik, okunabilir, 10–50 NPC ölçeğinde performans yeterli).
- "Gerekçe (Yusuf)" boş, durum "Önerildi". Şablondaki "Kabul eden / Kabul tarihi" satırları boş.

### 2. Events modülü (plan 5.1)
- Değişmez `Olay` tipi. Alanlar:
  - `Id` (sıralı, 1'den başlar), `Tick`, `Sira` (tick içi sıra),
  - `Tur` (string), `Fail`, `Hedef`, `Mekan` (varlık kimlikleri, boş olabilir),
  - `Yuk` (sıralı, değişmez anahtar-değer listesi),
  - `NedenOlaylar` (olay kimlikleri listesi), `KuralId` (string; oyuncu eylemlerinde "player").
- `OlayGunlugu`: yalnızca sona ekleme yapılabilir, kayıtlar değiştirilemez. Eklerken doğrula:
  - `Tick` geriye gidemez, `Sira` tick içinde artar,
  - `NedenOlaylar` içindeki her kimlik günlükte zaten var ve yeni olaydan önce.
  - Kural ihlalinde açık mesajlı exception.
- `Neden(olayId)`: nedensellik zincirini geriye doğru, deterministik sırayla döndürür.

### 3. World modülü (plan 5.2)
- Varlıklar: NPC, mekan, eşya (kimlik + tür).
- Gerçekler: `yuklem(arg1, arg2, ...)`.
  - Tek değerli gerçekler `!` ile işaretlenir (`konum!(Ali, han)`). Anahtar = yüklem + son argüman hariç tüm argümanlar. Yeni değer yazılınca eski değer otomatik düşer.
- Mekan grafiği: yönsüz bağlantılar; `Komsular(mekan)` sıralı döner, `BagliMi(a, b)`.
- Dünya durumu olaylardan türetilir. Bu hafta yalnızca dünya olayları: `gercek_eklendi`, `gercek_kaldirildi`, `varlik_eklendi`, `mekan_baglandi`. `Dunya.Uygula(olay)` ile durum ilerler.
- Korumalı bayraklar, yay değişkenleri, zihin YOK (7. ve 3. hafta).

### 4. Anahtarlı RNG
- `AnahtarliRng.Icin(seed, tick, anahtar1, anahtar2, ...)`: aynı girdi her zaman aynı akışı verir. Çağrı sırası ya da başka NPC'lerin çağrıları sonucu etkilemez.
- Kendi implementasyonun olsun: anahtarları UTF-8 bayt olarak, kendi yazdığın 64 bit hash (ör. FNV-1a veya xxHash64) ile tohum yap, akış için SplitMix64 kullan.
- `SonrakiUlong()`, `Aralik(int min, int maxHaric)`, `SonrakiDecimal01()` (decimal, 0 dahil 1 hariç).

### 5. Durum hash'i
- Dünya durumunun kanonik serileştirmesi: her koleksiyon ordinal sıralı, decimal'ler sabit biçimde (InvariantCulture, sabit ölçek).
- Üzerinden kendi 64 bit hash'in. `Dunya.DurumHash()` hex string döner.

### 6. Criteria modülü: kriter dili (plan 5.9 + docs/criteria-language.md)
- **Ayrıştırıcı:** `alan` ifadesini parse eder.
  - Biçim: `fonksiyon(arg, ...)`. Argümanlar: tanımlayıcı, sayı, `*`, iç içe önerme (`borclu(Mehmet, *, *)`, `konum!(Ali, han)`), roller (`ben`, `oyuncu`, `konusan`, `dinleyen`, `olay.fail`, `olay.hedef`).
  - Hatalı ifadede konum (karakter indeksi) içeren açık hata mesajı.
- **Sözlük doğrulaması:** docs/criteria-language.md'deki 15 fonksiyonun adı, argüman sayısı ve dönüş türü (sayı / mantıksal / varlık / bant) koda tanım olarak girilsin. Bilinmeyen fonksiyon, yanlış argüman sayısı ve dönüş türüyle uyumsuz operatör (ör. mantıksal değerde `<`) hata versin.
- **Kriter modeli:** `alan`, `op` (`<`, `<=`, `>`, `>=`, `==`, `!=`, `aralik`), `deger`, `agirlik` (varsayılan 1), `zorunlu` (varsayılan false). JSON okuma YOK; kriterler C# ile kurulur.
- **Değerlendirme:** `ISorguCozucu` arayüzü fonksiyon çağrılarını çözer. Motor bu hafta yalnızca dünyaya dayanan iki fonksiyonu gerçekten çözer: `gercek` ve `ayni_mekanda` (konum! gerçeklerinden). Diğer 13'ü 3. haftada; testlerde sahte (fake) çözücüyle test et.
- **Puanlama ve seçim:**
  - Aday puanı = tutan kriterlerin ağırlık toplamı. Bir `zorunlu` kriter tutmazsa aday elenir.
  - `EnIyiSec(adaylar, rng)`: en yüksek puanlı aday kazanır, eşitlikte anahtarlı RNG ile seçilir.
  - Sonuç bir "eşleşme izi" de döndürsün: hangi kriter tuttu, hangisi tutmadı (ileride `why` aracı bunu kullanacak).

### 7. Testler
- Her modül için birim testleri.
- **Determinizm testleri:**
  - Aynı olay dizisi iki kez uygulanınca aynı hash.
  - Olaylar farklı nesne örnekleriyle kurulsa da aynı hash.
  - RNG: aynı anahtar → aynı değerler; çağrı sırası değişince her anahtarın değerleri değişmez.
- **Altın sabit testi:** Küçük sabit bir dünya kur (3 NPC, 2 mekan, birkaç gerçek), hash'ini ve RNG'nin ilk 5 değerini test içinde sabit değer olarak yaz. Bu, ileride biri `GetHashCode` gibi kararsız bir şey kullanırsa testi kırar.
- **Yasaklı API testi:** `src/Yanki.Core` altındaki .cs dosyalarını tarayan bir test. `System.Random`, `DateTime.Now`, `DateTime.UtcNow`, `Guid.NewGuid`, `GetHashCode(`, `HashCode.`, `double`, `float` geçiyorsa testi kırsın. Polyfill gibi bilinçli istisnalar için açık bir izin listesi olsun.
- Mevcut mimari testleri yeşil kalsın.

### 8. Dokümanlar
- Her yeni modülün (Events, World, Criteria) README'sini güncelle.
- `docs/notes/` altına modül başına kısa "ne yapıyor, neden böyle" taslağı: `olay-kaydi.md`, `dunya.md`, `kriter-dili.md`. Ben kendi cümlelerimle yeniden yazacağım.
- CHANGELOG'a ekle.

## Çalışma şekli
1. Planı ve dosyaları oku, `superpowers:writing-plans` ile adım adım plan çıkar, bana göster, ONAYIMI BEKLE.
2. Onaydan sonra `superpowers:test-driven-development` ile her parçada önce test. Her parça ayrı küçük commit.
3. Bitirmeden `superpowers:verification-before-completion`: `dotnet build Yanki.slnx -c Release` (iki hedef, 0 uyarı) ve `dotnet test Yanki.slnx -c Release` çıktılarını göster.
4. Push et, `hafta-02` → `main` PR'ı aç. CI yeşil olunca PR linkini ver. Merge'i ben yapacağım.
5. Son mesajında şunlar olsun: ne yaptın, hangi kararları sen verdin (varsa), benim yapmam gerekenler.

## Yapma
- 3. haftanın kodunu yazma (zihin, algı, karar, kural motoru).
- JSON okuma, dosya sistemi ya da ağ erişimi ekleme.
- Paket ekleme.
- Plan, CLAUDE.md ve bu prompt çelişirse dur ve bana sor.
