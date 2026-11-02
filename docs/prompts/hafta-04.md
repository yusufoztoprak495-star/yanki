# Görev: Yankı — 4. hafta (Sonuç kuralları ve ince dikey dilim)

## Bağlam

Plan: docs/plan/Yanki Plan v2.pdf. Bu görev SADECE 4. hafta.
Önce şunları oku: CLAUDE.md, docs/DURUM.md, planın 5.1, 5.6, 5.8, 5.9, 6, 8, 9. ve 12. bölümleri, docs/criteria-language.md, tüm ADR'ler ve docs/notes/.
`main`'i çek, `hafta-04` dalı aç. Bu görev metnini birebir `docs/prompts/hafta-04.md` olarak kaydet.

Hedef kontrol noktası: oyuncu eylemi → algı → kural → fikir değişimi → `why` zinciri → CI'da golden replay.

Bu hafta YAZMA:

- karar/utility AI, NPC'lerin kendi eylemleri, eylem JSON'ları, karar izi, IActionSelector (5. hafta)
- "anlat" eylemi, duydu kanıtı aktarımı, konu sınırı, yalan kaydı, stres krizleri (6. hafta)
- cooldown, tetiklenme ve zincir derinliği sınırları, fren olayları, korumalı bayraklar, yay değişkenleri, tam kayıt/yükleme (7. hafta)
- diyalog (8. hafta)
- `yanki validate` tam sürümü, `coverage`, `diff`

## Değişmez kurallar

- Çekirdek kod `Yanki.Core` içinde, LangVersion 9.0, NuGet paketi yok, iki hedefte de derlenir.
- Determinizm: `System.Random`, `DateTime.Now`, `Guid.NewGuid`, `GetHashCode`/`HashCode` yasak. Sırasız `Dictionary`/`HashSet` gezintisi yasak (ordinal sıralı). `float`/`double` yasak, sayılar `decimal`. Tek RNG `AnahtarliRng`.
- Her durum değişikliği bir olaydır; `NedenOlaylar` ve `KuralId` taşır.
- `Olay` tipine yeni alan EKLEME. Gerekirse dur ve sor.
- TDD: her parça için önce kırmızı test.
- Kod tanımlayıcıları ve JSON anahtarları ASCII.
- Çekirdeğe dosya sistemi erişimi ekleme. Dosya okuma CLI'da kalır; çekirdek metin ya da model alır.
- JSON için önce mevcut yaklaşımı kullan. `Yanki.Core`'da yeterli okuyucu yoksa paket eklemeden dur ve seçenek sun.

## Teslimler

### 1. Kural motoru

- Biçim: `ne_zaman <olay türü>` + kriterler → etkiler. Kriterler mevcut kriter ayrıştırıcı, doğrulayıcı ve seçicisini kullanır; ikinci bir kriter yolu açma.
- Kurallar JSON ile tanımlanır, kod değiştirmeden genişler.
- Bir kural, tetikleyici olayı algılayan her NPC için sabit ordinal sırayla bir kez değerlendirilir. Bu bağlamı netleştirmek gerekirse seçenek sun.
- v0 etkileri: fikir modifier'ı ekleme, kalıcı eksen değiştirme, ruh hali değiştirme, hafızaya kayıt ekleme, dünya gerçeği yazma. Bu listeyi genişletme.
- Etkiler doğrudan state değiştirmez, yeni olay üretir. Her olay `KuralId` ve `NedenOlaylar` taşır.
- v0 kaskad davranışı: kuralların ürettiği olaylar aynı tick'te yeniden kural tetiklemez, sıradaki tick'e kalır. Kaskad sınırının tam hali 7. haftadadır.

### 2. Doğrulayıcı v0

- Şema hatası, geçersiz kriter sözdizimi, bilinmeyen olay türü ve etkisi boş kuralı reddet.
- Hata mesajları kural kimliğini ve mümkünse dosya/satır bilgisini versin.

### 3. Dış API ve tick döngüsü v0

- `Submit(PlayerAction)`, `Tick(n)`, durum sorguları ve `Neden(olayId)`.
- Plan 6. bölümden yalnızca şu adımlar: (1) oyuncu eylemleri, (2) algı, (3) sonuç kuralları, (4) sönüm ve süre dolumu, (7) durum hash'i ve dışarı olay yayını.
- 4. adımda stres krizi üretme. 5. ve 6. adımları yazma, ama sonradan takılabilecek yeri temiz bırak.
- 1 tick = 1 saat varsayımı DURUM.md ve ADR'lere uygun mu kontrol et. Kayıtlı değilse açık karar olarak yaz.
- Determinizm testleri: aynı seed + aynı girdi = aynı hash.

### 4. Minimal CLI

- `yanki run`: senaryo yükler, eylem gönderir, tick ilerletir.
- `yanki query`: NPC durumu, fikirler, modifier'lar, inançlar.
- `yanki why <olayId>`: nedensellik zincirini okunur satırlarla yazar. Plan bunu 5. haftaya koyuyor, ama 4. hafta kontrol noktası gerektirdiği için minimal hali bu haftadadır.
- `yanki replay <dosya>`: tekrar oynatır, hash'i doğrular.
- Mevcut CLI yapısına uy, yeni paket ekleme.

### 5. 3 NPC'lik mini senaryo

- İçerik Yusuf'un alanı. NPC'leri, mekanları, kuralları, sayıları ve oyuncu eylem dizisini kendin seçme.
- Motor testlerini test fikstürleriyle yaz. Mini senaryoya geçmeden dur ve 2–3 somut seçenek sun: 3 NPC, 3–5 kural, kısa eylem dizisi. Yusuf seçince `samples/` altına yaz.
- Senaryo dedikodu ve NPC kararı gerektirmemeli. Akış, oyuncu eylemini aynı mekanda görenler üzerinden çalışır.
- Uçtan uca senaryo testi: bir oyuncu eylemi fikri değiştirir, `why` zinciri modifier'dan oyuncu eylemine kadar iner.

### 6. Golden replay #1

- `tests/golden/` altında seed + senaryo + oyuncu eylem günlüğü + beklenen son hash.
- CI bunu her push'ta tekrar oynatır.
- Hash bilerek değiştiğinde tek komutla güncellenir; komutu belgele. Hash güncellemesi PR'da gerekçe ister.
- Bu dosya biçimi, 7. haftadaki kayıt/yüklemeyle aynı temele dayansın; tam kayıt/yükleme yazma.

### 7. Dokümantasyon

- `docs/DURUM.md`: 4. haftayı, açık kararları ve sıradaki haftayı güncelle.
- README: "12 haftalık planın 1. haftası" durum satırını güncelle. Diğer bölümleri doldurma.
- `docs/notes/`: kural motoru ve tick döngüsü için taslak notlar bırak.
- `docs/devlog/`: devlog #1 için yalnızca başlıklar ve Yusuf'un kullanacağı teknik gerçekler. Yazıyı Yusuf yazar.
- Gerekirse yeni ADR aç; "Gerekçe (Yusuf)" bölümünü boş bırak.

## Yusuf'a bırak

Şunlarda karar verme, seçenek sun:

- kriter sözlüğüne eklenecek yeni fonksiyon
- denge sayıları
- mini senaryo içeriği
- test senaryolarının kapsamı

## Bitiş

- `dotnet build Yanki.slnx -c Release` 0 uyarı.
- `dotnet test Yanki.slnx -c Release` geçiyor.
- Küçük commit'ler, sonlarında `Co-Authored-By` satırı.
- PR aç. Açıklamada ne yapıldığını, açık noktaları ve kontrol noktasının nasıl doğrulandığını yaz.
- CI'ın son commit'te yeşil olduğunu doğrula ve haber ver. Merge etme.
