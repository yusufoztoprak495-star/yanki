# Proje durumu

Tek bakışta: neredeyiz, ne bitti, Yusuf'tan ne bekleniyor. **Her PR'da güncellenir** (CLAUDE.md); PR bunsuz açılmaz.

## Son güncelleme

- **Tarih:** 2026-11-03
- **PR:** [PR #10](https://github.com/yusufoztoprak495-star/yanki/pull/10) (6. hafta: dedikodu, yalan, stres, kriz; `hafta-06` → `main`), Yusuf'un incelemesini bekliyor. [PR #9](https://github.com/yusufoztoprak495-star/yanki/pull/9) (5. hafta) ve [PR #8](https://github.com/yusufoztoprak495-star/yanki/pull/8) merge edildi.

## Biten haftalar

### 1. hafta: kurulum ve iskelet ([PR #1](https://github.com/yusufoztoprak495-star/yanki/pull/1), [PR #2](https://github.com/yusufoztoprak495-star/yanki/pull/2))

- .NET çözümü (`netstandard2.1;net10.0`), CI ve Codecov adımı.
- README iskeleti, `CLAUDE.md`, `docs/ai-usage.md`.
- ADR-001..006.
- Kriter dili sözlüğü v0.1 (15 fonksiyon).

### 2. hafta: olay kaydı, dünya, kriter dili ([PR #3](https://github.com/yusufoztoprak495-star/yanki/pull/3))

- Olay kaydı (`NedenOlaylar`, `KuralId`) ve dünya (gerçekler, mekan grafiği).
- Anahtarlı RNG ve dünya durum hash'i.
- Kriter ayrıştırıcı, doğrulayıcı ve seçici (eşitlikte özgül olan kazanır, sonra RNG).
- ADR-007 (`decimal`), `tools/golden_ref.py`, 318 test.

### 3. hafta: zihin ve doğrudan algı ([PR #4](https://github.com/yusufoztoprak495-star/yanki/pull/4))

- Zihin: kişilik, 3 eksen + süreli fikir modifier'ları (`FikirDokumu`), tek duygulu ruh hali, stres, kanıtlı inançlar (çelişki kuralı, `KimdenDuydu`), hafıza.
- Saf sönme fonksiyonları (`Sonumleme`, `ZihinAyarlari`) ve altı zihin olayı; zihin durum hash'e giriyor, dünya altın hash'i değişmedi.
- Doğrudan algı: `acik`/`sessiz`/`gizli`, `gordu` kanıtı + hafıza olayı (`KuralId: algi`).
- `ZihinSorguCozucusu`: kriter dili sözlüğünün 13/15 fonksiyonu gerçek veriye bağlı (`yalan_soyledi` 6., `yay` 7. hafta).
- ADR-008, `docs/notes/zihin.md`, `docs/notes/algi.md`; 508 test (iki hedefte derleniyor, 0 uyarı).

### 4. hafta: sonuç kuralları ve ince dikey dilim ([PR #7](https://github.com/yusufoztoprak495-star/yanki/pull/7))

- **JSON okuyucu/yazıcı** (`Yanki.Core.Json`, bağımlılıksız, sayılar `decimal`, satır/sütun bilgili hata); ADR-009. `KriterOkuyucu` kriterleri JSON'dan okur (ikinci kriter yolu yok).
- **Kural motoru** (`Rules`): `ne_zaman` + kriterler (hepsi tutmalı, ADR-010) → etkiler; v0 etkileri modifier, eksen, ruh hali, hafıza, dünya gerçeği; etkiler yeni olay (`KuralId` + `NedenOlaylar`). Doğrulayıcı v0 `dosya:satır: kural "id": mesaj` verir, `agirlik` için uyarır. Sözlüğe `olay_rolu` eklendi (16 fonksiyon).
- **Tick döngüsü v0 ve dış API** (`Simulation`): `Submit(PlayerAction)`, `Tick(n)`, sorgular, `Neden(olayId)`; adımlar 1, 2, 3, 4, 7. Kaskad v0: kural çıktıları sıradaki tick'e kalır.
- **CLI:** `yanki run | query | why | replay` (`replay --guncelle` golden hash'i tek komutla günceller).
- **Mini senaryo `samples/MiniHan`** (Yusuf'un seçimi: seçenek A + hedef/tanık düzeltmesi) ve **golden replay #1** (`tests/golden/mini-han-1.json`); CI her push'ta tekrar oynatır.
- Uçtan uca test: oyuncu eylemi fikri değiştirir, `why` zinciri modifier'dan oyuncu eylemine iner. Notlar: `docs/notes/kural-motoru.md`, `docs/notes/tick-dongusu.md`; Devlog #1 `docs/devlog/01-ilk-dikey-dilim.md`.
- Testler: 649 (Core) + 24 (senaryo/CLI/golden) = 673; iki hedef derleniyor, 0 uyarı. Golden hash değişti (hafıza tekilleştirme): `504f3142d1dc15dd` → `cee9956ef06df17f`.

### 5. hafta: utility AI ve NPC'lerin kendi eylemleri ([PR #9](https://github.com/yusufoztoprak495-star/yanki/pull/9))
- **Eylem tanımları** JSON ile (`eylem_dosyalari`), doğrulayıcı `dosya:satir: eylem "id": mesaj`; kriterler ve etkiler mevcut okuyucularla (ikinci yol yok). Yeni rol `eylem.hedef` (yalnızca eylem bağlamında; sözlüğe fonksiyon eklenmedi, 16 fonksiyon).
- **Karar:** `AdayUretici` (tek nokta), `Puanlayici` (`taban × Π(eğri) × kişilik × fikir × ruh hali`, çarpan sınırları 0.25–2.0 / 0.6–1.5 / 0.8–1.25), decimal eğriler (`dogrusal`, `ustel`, `lojistik`, `basamak`; elle yazılmış `e^y`, altın değerler `tools/golden_ref.py` ile), `IActionSelector` + `UtilitySelector` (ilk 3, %70, `AnahtarliRng.Icin(seed, tick, npc, "karar")`).
- **Tick adımları 5 ve 6:** tek durum görüntüsü, eş zamanlı algı (aynı tick'te ayrılan tanık görür, varan görmez), algılayanlar saklanıp sıradaki tick'te kurallara taşınır, tetik kuyruğu `Olay.Id` sırasıyla. **Hareket:** `ayrildi` + `geldi` (`Hedef` karşı mekan), oyuncu `hareket` eylemi `Submit`'te komşuluk denetimiyle.
- **Karar izi:** `karar_verildi` günlükte KISA iz taşır; tam iz replay ile yeniden üretilir ve kısa izle doğrulanır (K3). `yanki why` zinciri modifier'dan algıya, eylem olayına ve karar izinin puan bileşenlerine indirir; `yanki query` son kararı gösterir; çıkış kodu 4 iz uyuşmazlığıdır.
- **Performans (Z1/Z2):** `HafizaVar` olay kimliğine göre sıralı dizinle erken durur (naif erken durdurmanın yanlış olduğu kırmızı testle gösterildi), hedefe bağlı olmayan ön koşullar bir kez hesaplanır. Ölçüm (benchmark değil, `tools/olcum`): 50 NPC stres fikstüründe tick 321 → 82 ms, `karar_verildi` günlük payı %71 → %10; ayrıntı `docs/notes/karar.md`.
- **Senaryo `samples/MiniKarar`** (Yusuf'un seçimi: A + C'nin hareketi) ve **golden replay #2** (`tests/golden/mini-karar-1.json`, seed 42, 24 tick, hash `fe29a63d6dfbc307`); uçtan uca `MiniKararTests` (kontrol noktasının yedi koşulu, seed 42/7 farkı).
- **Hedefler (ayrı hedef zihin katmanı) v0.1'de yok, v0.2 sonrası listesinde** (Yusuf'un kararı); plan 5.4/3'teki "hedefler" ifadesi v0.1 için "kişilik + eylem ölçütleri + hafıza" olarak okunur.
- ADR-011, notlar (`karar.md`, `tick-dongusu.md`, `algi.md`), `authoring-guide.md` (eylem yazımı, `dinlen`), `criteria-language.md` (eylem bağlamı), `Decision/README.md`. Testler: 1107 (Core) + 50 (senaryo/CLI/golden) = 1157; iki hedef derleniyor, 0 uyarı. `mini-han-1` hash'i değişmedi (`cee9956ef06df17f`, ayrıca testle kilitli).
- **Plandan sapmalar (ADR-011'de):** hedef katmanı v0.2 sonrası; eylem olayı `KuralId: eylem:<id>`; karar izi günlükte kısa, tam iz replay ile; sonuç kuralları NPC eylemleri için bir sonraki tick'te çalışır (4. haftanın kaskad kararı, plan 6'daki "2. adıma geri besleme" aynı tick'te değil).

### 6. hafta: dedikodu, yalan, stres ve kriz ([PR #10](https://github.com/yusufoztoprak495-star/yanki/pull/10))
- **Dedikodu:** NPC ve oyuncu `anlatti` olayı ile önerme anlatır; dinleyene `duydu` kanıtı (kök, güç `en güçlü tek kanıt × min(0.95, 0.7 + 0.003 × güven)`, 0.15 altı yazılmaz), konuşana aktarım kaydı (`aktarim_kaydedildi`). Yankı freni (kök), geri anlatmama, aynı önermeyi aynı kişiye iki kez anlatmama. Tek hesap sınıfı `KonuHesabi` (`onem × yenilik × ilgi`, konu sınırı `1 + ⌊sosyallik × 2⌋` (+1 yakın/dost)). Konuşan dinleyenin zihnini okumaz (testle).
- **Yalan gizliliği:** doğru ve yalan anlatım dışarıdan ayırt edilemez (aynı olay türü, yük, `NedenOlaylar` sayısı, güç); yalan bilgisi yalnızca konuşanın zihnindeki algılanmayan, kural tetiklemeyen aktarım kaydında. Yalan konuşanın kendi inancına göre (yanlış inandığı şeyi anlatan yalancı sayılmaz).
- **Stres ve kriz:** `stres_tabani`; seçilen çelişen eylem stres yazar. Eşikler 50/80, düşüşler 30/60, `StresAyarlari` değişmezi (`esik[i+1] − dusus[i] ≤ esik[0]`, `100 − dusus[son] < esik[0]`). Tespit adım 4 (`stres ≥ eşik`), kriz seçilmiş eylem gibi adım 6 yolundan; kriz tablosu (`kriz_dosyalari`, `seviye` 1 = alt eşik, 2 = üst eşik; kriterler hepsi tutmalı; hedef `yok`/`yalan_kurbani`/`en_yakin`/`en_soguk`; seviye başına yedek satır zorunlu).
- **Sözlük 16 → 18** (`konu_puani`, `olay_konusu`), `yalan_soyledi` bağlandı. Eylem şemasına `aktarim`, `konu_desenleri`, `yalan_onermeleri`, `gorunurluk`, `stres_tabani`; doğrulayıcı `dosya:satir: eylem|kriz "id": mesaj`.
- **CLI:** `run --eylem "anlatti:Ayse:onerme=borclu(Mehmet,Ayse,500)"` (parantezli yük), `kriz_dosyalari`, `query` kök ve aktarım kayıtları, `why` krizden stres artışına ve karar izine iner.
- **Senaryo `samples/HanGerilim`** (Yusuf'un seçimi: seçenek 3, "Han'da gerilim") ve **golden replay #3** (`tests/golden/han-gerilim-1.json`, seed 42, 24 tick, `2f778d4f93ea7b0b`). Değişmezler koşudan önce yazıldı (`HanGerilimTests`): Ayşe → Can söylentisi ve `aileme_iftira`; Hatice'nin 2 farklı yalanı, 1 itiraf; Selim'in 2 tehdidi, 1 patlama; Can'ın tehdit döngüsü yok. Seed 7 de aynı sayıları verir.
- **Ölçüm** (`tools/olcum --dedikodu`, benchmark değil): 50 NPC dedikodusuz 79.5 ms/tick (öncesi 82). Dedikodulu ilk ölçüm 3105.6 ms/tick'ti (algoritma hatası); Yusuf'un kararıyla bu PR'da ayrı commit olarak konuşan başına tick içi önbellekle düzeltildi: **50 NPC 523.1 ms/tick, 8 NPC 4.62 ms/tick** (dedikodusuz 8 NPC 2.52). Olay biçimi ve son hash değişmedi. Bilgi patlaması yok: dedikodu kanıtların ≈%0.8'i, hacmi algı belirliyor. Ayrıntı `docs/notes/dedikodu.md`.
- **Satır sonu:** `KararCliTests`'in `\n` içeren `Replace`'i CRLF checkout'ta eşleşmiyordu (kök neden testteydi); düzeltildi, `.gitattributes` (`* text=auto eol=lf`) ve `SatirSonuTests` (CRLF/LF aynı hash) eklendi.
- ADR-012, `docs/notes/dedikodu.md`, `docs/notes/stres.md`, `criteria-language.md` (18 fonksiyon), `authoring-guide.md` (konuşma, yalan, stres, kriz). ADR-011 ve ADR-012 "Gerekçe (Yusuf)" bölümlerini Yusuf'un görev metnindeki devriyle Claude Code yazdı; `CLAUDE.md` kuralı buna göre güncellendi, durumlar "Önerildi" kaldı. Testler: 1344 (Core) + 72 (senaryo/CLI/golden) = 1416; iki hedef derleniyor, 0 uyarı. `mini-han-1` (`cee9956ef06df17f`) ve `mini-karar-1` (`fe29a63d6dfbc307`) hash'leri değişmedi.
- **Plandan sapmalar (ADR-012'de):** doğru anlatım da `anlatti` türünde (yalan gizliliği); kriz tespiti geçiş durumu tutmaz; yalanın kanıt çelişkisinden otomatik yakalanması yok.

## Ertelenenler (Yusuf'un işi)

- [ ] **ADR-011 ve ADR-012 "Gerekçe (Yusuf)" onayı:** gerekçeleri Yusuf'un 6. hafta görev metnindeki devriyle Claude Code yazdı. Yusuf onaylayınca durumlar "Kabul edildi" yapılır (kararlar o zamana kadar da bağlayıcıdır).
- [ ] **ADR-001..010 "Gerekçe (Yusuf)" onayı:** gerekçeleri Yusuf'un isteğiyle Claude Code yazdı. Yusuf onaylayınca durumlar "Kabul edildi" yapılır.
- [x] **`docs/notes/` notları:** yedisi Yusuf'un isteğiyle Claude Code tarafından yeniden yazıldı. Açık kalan ve onaylanmamış kararlar notlarda ayrı bölümde.
- [x] **Devlog #1:** `docs/devlog/01-ilk-dikey-dilim.md` (Claude Code yazdı, çıktılar golden oturumdan).
- [x] **`CODECOV_TOKEN` secret'ı:** GitHub repo ayarlarına eklendi.
- [x] **Mini senaryo (3 NPC):** `samples/MiniHan` (4. hafta). Kişilik özelliklerinden `cesaret` dışındakiler 0.5 yer tutucu; 11. hafta denge turunda ayarlanacak.
- [x] **Gerilim senaryosu:** `samples/HanGerilim` (6. hafta). Kişilik, taban, `stres_tabani`, eşik ve düşüş sayıları yer tutucu; 11. hafta denge turunda ayarlanacak.
- [ ] **Kasaba Hanı NPC'leri:** 9. haftada 8.

## Açık kararlar (Yusuf'u bekleyen)

- 3. haftanın büyük kararları PR #4 incelemesinde verildi (eksen ağırlıkları, sönme oranları, hafıza eşikleri, birikme, çelişki eşitliği, duygu eşitliği, algı değerleri); notlarda **Karar (Yusuf)** olarak işli.
- İkinci inceleme turunda kalan küçük noktalar da karara bağlandı ve koda işlendi: `inanc(degil(...))`, tekrar eden kanıtın tazelenmesi, failın her zaman algılaması,
  `FikirDokumu` kırpma satırı; `gordu` kaynağı ve bilinmeyen görünürlük hatası olduğu gibi kaldı.
- `oyuncu` artık ayrı bir varlık türü (`VarlikTuru.Oyuncu`), eşya değil.
- 3. hafta için Yusuf'u bekleyen açık karar kalmadı (yalnızca ADR-008 "Gerekçe (Yusuf)" yazımı).

**4. hafta: karara bağlananlar (Yusuf, PR #7 incelemesi):**

- [x] **Hedef/tanık ayrımı:** sözlüğe `olay_rolu(fail|hedef|tanik)` eklendi (16. fonksiyon); `inanc(...)` yöntemi kaldırıldı. Regresyon: daha önce tehdit edilmiş NPC, başkasına yapılan tehdidi görünce tanıktır.
- [x] **Kural tetiklenmesi:** kural yalnızca tüm kriterleri tutunca çalışır; kriteri olmayan kural her tetikleyici olayda çalışır; `agirlik` etkilemez, doğrulayıcı uyarır (ADR-010, gerekçe boş).
- [x] **Algı kapsamı (KAPANDI):** algı yalnızca dış (zihin olmayan) olayları alır; algı olayları (`KuralId: algi`) kural tetiklemez.
- [x] **Hafıza:** aynı NPC ve aynı olay için tek kayıt; `hafiza` etkisi kayıt varsa önemi `max(eski, yeni)` yapar, yoksa yeni kayıt açar; her değişiklik yine `hafiza_eklendi` olayıyla kaydedilir.
- [x] **Tanımsız rol:** mevcut davranış kalır (kural o olay için sessizce atlanır). **7. hafta notu:** bu durum için fren olayı yazılmalı (atlanan kural günlükte iz bırakmalı).
- [x] **MiniHan:** `olay_rolu` kullanıyor. `cesaret` dışındaki kişilik değerleri 0.5 kalıyor; **11. hafta denge turunda ayarlanacak.**

**PR #8 (Yusuf, merge edildi):** `RepoStructureTests` içindeki "ADR-007 gerekçesi boş olmalı" kontrolü kaldırıldı; durum kontrolleri kalıyor. `CLAUDE.md` kuralı henüz değişmedi.

**4. hafta: ikinci inceleme turunda kapatılanlar (Yusuf):**

- [x] **Tek durum görüntüsü:** bir adımdaki bütün kurallar adım başındaki durumu görür; aynı adımdaki başka kuralın çıktısı görünmez. Kalıyor.
- [x] **Gerçek etkisi tekrarsız yazılır:** durumu değiştirmeyen etki üretilmez, aynı adımda aynı gerçek tek kez yazılır.
- [x] **Oyuncu hareketi 5. haftaya kaldı** (v0'da oyuncunun mekanı senaryodan gelir ve değişmez).
- [x] **1 tick = 1 saat** `docs/notes/zihin.md` madde 5'te **Karar (Yusuf)** olarak kayıtlı.
- Açık kalan tek ayrıntı: `hafiza` etkisi her zaman tetikleyici olayı hatırlatır (başka olay seçme yok); etki hedefi senaryoda yoksa `Simulasyon.Olustur` reddeder.
- Doğrulayıcı uyarıları: `agirlik` ve `zorunlu: false` uyarı verir (ADR-010). JSON okuyucuya 8 MiB boyut sınırı eklendi (sınır testleri sırasında eksik bulundu).

**5. hafta: ölçüm durağında karara bağlananlar ve 7. haftaya bırakılanlar (Yusuf):**

- [x] **K3:** `karar_verildi` günlükte kısa iz taşır; tam iz replay ile yeniden üretilir ve günlükteki kısa izle doğrulanır (ADR-011).
- [x] **Z1 / Z2:** `HafizaVar` olay kimliğine göre sıralı dizinle erken durur; hedefe bağlı olmayan ön koşullar eylem başına bir kez hesaplanır.
- [ ] **Z3 (7. hafta):** durum hash'i zihin başına önbellekli yazılacak. CsCheck değişmezleri ve tam hash karşılaştırmasıyla bayat önbellek aranacak. Çıktı birebir aynı kalacak, altın hash değişmeyecek. (Ölçüm: 50 NPC stres fikstüründe hash tek başına tick süresinin büyük bölümü; 5 ms hedefi bunsuz tutmaz.)
- [ ] **Z4 (7. hafta):** algı başına iki olay (kanıt + hafıza) ve sınırsız büyüyen hafıza maliyeti gerçek senaryoyla ölçülecek (stres fikstüründe olayların %89'u algı olayı). Seçenekler: kanıt ve hafıza olaylarının birleşmesi, önem eşiği, silik hafızanın durumdan düşmesi. Olay biçimi değişecekse karar Yusuf'un.

**5. hafta: 6. ve 7. haftaya taşınan açık kararlar (Yusuf):**
- [ ] **7. hafta:** Z3 ve Z4'ü 50 NPC için 82 ms/tick ölçümünden başlayarak ele al; 5 ms hedefi şu an tutmuyor (`tools/olcum`, `docs/notes/karar.md`).
- [ ] **7. hafta:** cooldown, `hafiza_var` tekrar sınırının üç eksiğini kapatmalı: hedef bazlı tekrar, 200 tick sonrası unutma (pencere ne olursa olsun), "en fazla k kez".
- [x] **6. hafta (KAPANDI):** "Gerilim" senaryosu ve golden replay #3: `samples/HanGerilim`, `tests/golden/han-gerilim-1.json`. `samples/MiniKarar` tehdit kurallarını yüklü taşır ama tetiklemez (değişmedi).
- [ ] **7. hafta:** hedefi mekan olan olayda (ör. hedefi mekan olan oyuncu eylemi) atlanan kişi etkisi için fren olayı; korumalı bayrak filtresi `AdayUretici`'ye takılacak.
- [ ] **7. hafta (kod incelemesi notları):** (a) Adım 6.3'te eylem etkileri sırayla yazılır ve `gercek` etkisinin "zaten var/yok" denetimi aynı adımdaki önceki NPC'nin etkisini görür; sonuç NPC kimlik sırasına bağlıdır (girdi sırasına değil), ama "tek durum görüntüsü" ilkesiyle tam örtüşmez. İki NPC aynı tick'te aynı tek değerli gerçeği yazarsa son kimlik kazanır; ya sıra kuralı olarak belgelenecek ya da denetim adım öncesi görüntüye alınacak. (b) `KaynakToplayici` dünya durumu okuyan sorguları (`ayni_mekanda`) ne kaynak gösterir ne `kaynaksiz`'a yazar; `inanc` kişisiz kullanımda ve `hafiza_var` eşleşen tüm kayıtları gösterir (fazla gösterme, uydurma değil). (c) `replay` hash'i `karar_verildi` olaylarını kapsamaz (durum hash'inde değiller); seçici değişikliği durumu değiştirmiyorsa `replay` geçer, `why` ise kısa iz karşılaştırmasıyla (çıkış kodu 4) yakalar. `Simulasyon.KararIzi(npc)` yalnızca adayı olan tick'lerde güncellenir; izin `Tick` alanına bakın.
- [ ] **11. hafta:** `samples/MiniKarar` kişilik değerleri (merhamet, sosyallik, cesaret, kin_tutma, açgözlülük dışındakiler 0.5) ve taban sayıları yer tutucudur; denge turunda ayarlanacak.

**6. hafta: karara bağlananlar ve sonraki haftalara bırakılanlar (Yusuf):**

- [x] **Kriz `seviye`:** 1 = alt eşik, 2 = üst eşik; doğrulayıcı başka değeri reddeder.
- [x] **Söylenti kökü:** 19. fonksiyon yok. `HanGerilim`'de söylentinin tek kökü oyuncu olduğundan `aileme_iftira` modifier'ının hedefi `oyuncu` sabit (README'de belirtildi).
- [x] **Performans:** konuşan başına tick içi önbellek bu PR'da (ayrı commit). 3105.6 → 523.1 ms/tick (50 NPC), 8 NPC 4.62 ms/tick.
- [ ] **Performans (7. hafta, Z3/Z4 ile):** 50 NPC dedikodulu 523.1 ms/tick hedefin (5 ms) çok üstünde; kalan fark ≈6.6× (dedikodusuz 79.5 ms). Baskın maliyet konuşan başına O(n²) inanılan taraması (NPC başına 347 önerme) ve algı hacmi; seçenekler önerme dizini, yüklem ön filtresi, bağ dizini.
- [ ] **7. hafta:** cooldown geldiğinde `samples/HanGerilim`'in `hafiza_var(itiraf_etti | sakinlesti | patladi | kacti, ben, 24)` ön koşulları yeniden değerlendirilecek. **Golden #3 (`han-gerilim-1.json`) değişirse Yusuf'un onayı alınır.**
- [ ] **7. hafta (kod incelemesi):** aynı kökten daha zayıf `duydu` tazelemede `Kaynak` güçlü olanda kalırken `Tick`/`OlayId` yeni olaya geçiyor (`why` son anlatıma iner, sönüm sıfırlanır). Düzeltmek `han-gerilim-1` hash'ini etkileyebilir; Yusuf'un kararı (golden değişirse onay). Ayrıca `KrizNedenleri` günlüğü sondan tarar ve önceki krizde tüketilmiş artışları da sayabilir (kapsayıcı, uydurma değil).
- [ ] **9. hafta (açık karar):** Kasaba Hanı'nda NPC kökenli söylentiler için söylentinin kökünü okuyan bir sorgu (19. fonksiyon) gerekir mi? Şimdilik `aile_iftirasi` hedefi `oyuncu` sabit; NPC kökenli bir borç söylentisi aynı kuralı oyuncuya karşı tetikler.
- [ ] **11. hafta:** kriz sonrası stres ~24 iken tek güçlü eylem yeniden krize yetiyor (24 + 27 ≥ 50); düşüş ve `stres_tabani` dengesi ölçülecek. `HanGerilim`'de bunu `hafiza_var` ile 24 tick engelliyoruz.
- [ ] **11. hafta:** duyulan konunun önemi varsayılan olarak 0.5'e iner; yazar bunu `anlatti` kuralındaki `hafiza` etkisiyle yükseltir. Dengesi ölçülecek.
- [ ] **Açık (kapsam dışı bırakıldı):** yalanın kanıt çelişkisinden otomatik yakalanması yok (yalnızca itiraf krizi ve yazarın kuralları); itiraf kurbanın inancını düzeltmez; kulak misafirliği yok; hedef türü `npc` ya da `oyuncu` (ikisine anlatmak için iki eylem).

## Sıradaki hafta

- **7. hafta:** frenler (cooldown, tetiklenme ve zincir derinliği sınırları, fren olayları, korumalı bayraklar, yay değişkenleri), tam kayıt/yükleme, Z3 ve Z4 (50 NPC 523.1 ms/tick'ten başlayarak), `replay` hash kapsamı, 3 sıra etkisi testi ve CsCheck değişmezleri. Görev metni Yusuf'tan gelecek.
