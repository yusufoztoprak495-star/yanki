# Yankı — 4. hafta: Sonuç kuralları ve ince dikey dilim

Önce `CLAUDE.md`'yi, sonra `docs/DURUM.md`'yi oku. Tek kaynak `docs/plan/Yanki Plan v2.pdf`; 4. hafta için bölüm 5.6, 5.10 (sadece aşağıda izin verilen kısım), 6, 8, 9 ve 12'ye bak.
Plan ile bu prompt çelişirse DUR ve Yusuf'a sor.

## Hedef (hafta sonu kontrol noktası, 29 Kas)

Tek cümle: **oyuncu eylemi → algı → sonuç kuralı → fikir değişimi → `why` zinciri → CI'da golden replay**, uçtan uca, 3 NPC'lik bir mini senaryoda çalışsın.
Bu olmazsa plan gereği 6. haftadaki stres ve 8. haftadaki followup kesilir; yani bu hafta kapsamı şişirme, dikey dilimi bitir.

## Teslim edilecekler

1. **Sonuç kuralı modeli ve JSON biçimi** (plan 5.6): `ne_zaman <olay türü>` + kriterler (mevcut kriter dili, ADR-003) → etkiler. Etkiler yalnızca mevcut altı zihin olayını (`modifier_eklendi`, `eksen_degisti`, `duygu_degisti`, `stres_degisti`, `kanit_eklendi`, `hafiza_eklendi`) ve dünya olaylarını üretir. Her üretilen olay `KuralId` (kuralın kimliği) ve `NedenOlaylar` (tetikleyen olay) taşır.
2. **Kural motoru:** bir olay için eşleşen kuralları kriter dilinin mevcut `KriterDegerlendirici`'si ile değerlendirir, etkileri olay olarak günlüğe yazar. Sıra deterministik olmalı (kural kimliğine göre sıralı; sırasız `Dictionary`/`HashSet` gezintisi yok).
3. **Kural doğrulayıcı v0:** şema/sözdizimi hataları, bilinmeyen olay türü, **etkisi boş kural = hata**, kural içinde geçen kriter ifadelerini mevcut doğrulayıcıyla denetleme. Hata mesajı dosya + kural kimliği (mümkünse satır) gösterir.
4. **Tick döngüsü v0** (plan bölüm 6, yalnızca bu adımlar): (1) bekleyen oyuncu eylemleri `KuralId: player` ile olay olur, (2) doğrudan algı (3. haftada var), (3) sonuç kuralları, (4) 3. haftadaki sönme fonksiyonları, (7) tick sonunda durum hash'i. **5. ve 6. adımlar (IActionSelector, NPC eylem seçimi) bu hafta YOK**; NPC'ler henüz kendi başlarına eylem seçmez, dikey dilimi oyuncu eylemleri ve kurallar sürer.
5. **Dış API'nin en küçük hâli:** `Submit(PlayerAction)`, `Tick(n)`, durum/fikir sorgusu, `Why(olayId)` (nedensellik zincirini geriye doğru, kök olaya kadar döndürür). Plan 5.8'deki `Scenario.Load` biçimine uy ama yalnızca bu hafta gereken kadarını yaz.
6. **Minimal CLI:** `yanki run <senaryo> --seed N [--actions dosya] --ticks N`, `yanki query ...`, `yanki why <olayId>`, `yanki replay <dosya>` (tekrar oynat + hash doğrula), `yanki validate` (yalnızca kural doğrulayıcı). `diff`, `coverage`, `--ablate` YOK (11. hafta).
7. **3 NPC'lik mini senaryo** (`samples/KasabaHani/` altında ya da ayrı bir `samples/` klasöründe, Yusuf'un seçeceği yerde; Kasaba Hanı'nın 8 NPC'si 9. haftada) ve **golden replay #1**: `tests/golden/` altında kayıtlı seed + senaryo + eylem günlüğü + beklenen son hash. CI bunu tekrar oynatıp hash'i doğrular; hash'in bilerek değişmesi için tek komutlu güncelleme yolu olsun (plan bölüm 9). `tools/golden_ref.py` ile çapraz doğrulama mümkünse yap.
8. **Dokümantasyon:** `docs/DURUM.md` (zorunlu), `docs/notes/kural-motoru.md` ve `docs/notes/tick-dongusu.md` taslak notları, `docs/criteria-language.md` / `docs/authoring-guide.md` içinde kural biçimi, gerekirse **ADR-009** (kural motoru ve tick sırası kararları; "Gerekçe (Yusuf)" bölümünü BOŞ bırak). Devlog #1 için `docs/devlog/` altına **taslak iskelet** bırak (ne denendi / ne bozuldu / ne değişti başlıkları, olgusal maddeler); yazıyı Yusuf yazar.

## KAPSAM DIŞI (yazma)

IActionSelector/UtilitySelector/eylem tanımları (5. hafta), dedikodu/anlat/yalan (6.), stres krizleri (6.), cooldown, tetiklenme sınırı, **zincir derinliği D**, korumalı bayraklar, yay değişkenleri, fren olayları (7.), kayıt/yükleme API'si ve CsCheck (7.), diyalog (8.), Blazor (10.), `diff`/`coverage` (11.).
Bir kuralın zincirleme tetiklenmesi yüzünden sonsuz döngü riski varsa **bu hafta için sert bir güvenlik sınırı** koy (ör. tick başına işlenen olay üst sınırı, aşılırsa açık bir hata), ama bunu 7. haftanın "fren" sistemi gibi tasarlama; sınırın varlığını ve değerini PR'da belirt.

## Yusuf'un tasarım alanı: DURUP SOR

`CLAUDE.md` gereği aşağıdakilerde karar verme. **Kod yazmaya başlamadan önce** bu kararlar için seçenekleri (artıları/eksileriyle, bir öneriyle) sun ve Yusuf'un cevabını bekle:

1. **Kural JSON şeması:** alan adları ve yapı (ör. `ne_zaman`, `kriterler`, `etkiler`; etki türlerinin söz dizimi; etki değerlerinin ifade yöntemi).
2. **Etkilerde değişken kullanımı:** tetikleyen olaydan (fail, hedef, mekan) ve eşleşen kriterden hedef/değer alma biçimi.
3. **Kural çalıştırma sırası ve ayni tick'te ürettiği olaylara kuralın tepki vermesi** (aynı tick'te zincir mi, bir sonraki tick'e mi). Plan bölüm 6 adım 6 "2. adıma geri besleme" der; bu hafta nasıl sadeleşeceğini sor.
4. **3 NPC'nin kimlikleri ve kişilikleri**, mini senaryonun başlangıç dünyası (mekanlar, gerçekler), **ve 2–3 başlangıç sonuç kuralının içeriği** (hangi olay türü hangi fikir/modifier/duygu değişimini yapar, sayılar). Sen sadece seçenek öner.
5. **Golden replay #1'in eylem günlüğü** (hangi oyuncu eylemleri, kaç tick) ve **hangi senaryo testlerinin yazılacağı**.
6. **Kural kapsam/tetiklenme için `KuralId` adlandırma kuralı.**
7. **Kural doğrulayıcı hata/uyarı seviyeleri** (plan: etkisiz kural = hata; geri kalanı sor).

Cevaplar gelince onları `docs/DURUM.md`'ye ve ilgili nota **Karar (Yusuf)** olarak işle (3. haftadaki gibi).

## Çalışma sırası

1. Okuma: `CLAUDE.md`, `DURUM.md`, planın ilgili bölümleri, `docs/notes/*.md`, ADR'ler, `Yanki.Core` içinde `Events/`, `Mind/`, `Perception/`, `Criteria/`, `World/`. Mevcut örüntüleri (olay kurucuları, `KanonikYazici`, `ZihinDeposu`, `ISorguCozucu`) yeniden kullan; paralel bir yapı kurma.
2. Yukarıdaki **karar sorularını sor**, cevabı bekle.
3. Uygulama planı yaz (kısa), Yusuf'a göster.
4. TDD: her parça için önce kırmızı test, sonra kod. Küçük, anlamlı commit'ler (≤72 karakter, Türkçe olabilir, `Co-Authored-By` satırı ile).
5. Dal: `hafta-04`. `main`'e doğrudan commit yok. PR'ı açmadan önce `docs/DURUM.md` güncel olmalı.

## Değişmezler ve kalite çıtası

- `Yanki.Core` hâlâ `netstandard2.1;net10.0`, sıfır NuGet bağımlılığı, `LangVersion 9.0`. CLI ve testler bunun dışında kalabilir ama çekirdeğe ağ, dosya sistemi, veritabanı **eklenmez**; senaryo/kural **dosyalarını okuma CLI'da** ya da çekirdeğe string/stream olarak verilir.
- Determinizm: `System.Random`, `DateTime.Now`, `Guid.NewGuid`, sırasız gezinti yok. Mevcut `YasakliApiTests` / `DeterminizmTests` yeşil kalmalı.
- Aynı seed + aynı girdi = aynı hash; ayrıca **dünya ve zihin altın hash'lerinin 3. haftadan değişmediğini** doğrula. Değişirse nedenini açıkla.
- Sayılar `decimal` (ADR-007), kanonik yazım `KanonikYazici` ile.
- Uyarı = hata; bastırma, düzelt. Paket ekleme (sormadan hiçbirini).
- Her olay `NedenOlaylar` ve `KuralId` taşır; `Why` zinciri, her olayın neden kimliklerinin günlükte var olduğunu varsayabilmeli (yoksa test kırılsın).
- Yeni testler iki hedefte de derlenip çalışmalı (`dotnet build Yanki.slnx -c Release` 0 uyarı, `dotnet test Yanki.slnx -c Release` yeşil). Çıktıyı PR'da yapıştır.

## Bitti sayılması için (kabul kriterleri)

- [ ] `yanki run ... --seed 42` mini senaryoyu çalıştırır; `yanki why <olayId>` en az bir fikir değişimini oyuncu eylemine kadar geri izler (oyuncu → algı → kural → modifier).
- [ ] Etkisiz kural doğrulayıcıda hata verir; bilinmeyen olay türü/hatalı kriter hata mesajı kural kimliğini gösterir.
- [ ] Golden replay #1 CI'da koşuyor; hash bilerek bozulunca CI kırmızı, güncelleme komutu belgelenmiş.
- [ ] Aynı senaryo iki kez koşunca tick tick aynı hash'ler.
- [ ] Sönme/zihin/algı davranışı 3. haftadan değişmedi (508 test yeşil kalır, yenileri eklenir).
- [ ] `docs/DURUM.md` güncel (4. hafta bölümü, Ertelenenler/Açık kararlar, test sayısı), `docs/notes/` taslakları yazılı, ADR-009 (gerekiyorsa) "Önerildi" ve gerekçesi boş.
- [ ] PR açıklaması: ne yapıldı, hangi kararlar Yusuf'tan alındı, kapsam dışı bırakılanlar, hash değişimi var/yok.

## Sonunda bana ver

Kısa bir özet: yapılanlar, test sayısı, bilinçli olarak yapılmayanlar, Yusuf'un hâlâ vermesi gereken kararlar ve incelemesi gereken en riskli 3 dosya.
