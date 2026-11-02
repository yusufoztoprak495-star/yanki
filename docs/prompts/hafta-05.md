# Görev: Yankı — 5. hafta (Utility AI ve NPC'lerin kendi eylemleri)

## Bağlam

Plan: docs/plan/Yanki Plan v2.pdf. Bu görev SADECE 5. hafta.
Önce şunları oku: CLAUDE.md, docs/DURUM.md, planın 3, 5.1, 5.2, 5.3, 5.4, 5.5, 5.8, 5.9, 5.10, 6, 8, 9, 11 ve 12. bölümleri, docs/criteria-language.md, tüm ADR'ler (özellikle 002, 003, 007, 008, 010), docs/notes/.
`main`'i çek, `hafta-05` dalı aç. Bu görev metnini birebir `docs/prompts/hafta-05.md` olarak kaydet.

Hedef kontrol noktası: NPC kendi eylemini seçer → eylem olaya dönüşür → başka NPC algılar → kural çalışır → fikir değişir → `yanki why` bu zinciri NPC'nin karar izine ve puan bileşenlerine kadar gösterir → aynı seed aynı sonucu verir.

Bu hafta YAZMA:

- "anlat" ve "yalan söyle" eylemleri, duydu kanıtı aktarımı, konu sınırı, yalan kaydı (6. hafta)
- stres artışı ve kriz olayları (6. hafta)
- cooldown, tetiklenme ve zincir derinliği sınırları, fren olayları, korumalı bayraklar, yay değişkenleri, tam kayıt/yükleme (7. hafta)
- diyalog, niyet üretimi (8. hafta)
- `yanki validate` tam sürümü, `coverage`, `diff`
- Kasaba Hanı içeriği (9. hafta)
- GOAP/BT adaptörü
- hedef zihin katmanı (v0.2 sonrasına ertelendi, aşağıdaki kararlara bak)

## Karara bağlananlar (Yusuf)

Bunları yeniden seçenek olarak sunma. Plan bu maddelerle çelişiyorsa bu görev metni geçerlidir; çelişkiyi ADR-011 ve DURUM.md'ye yaz.

1. **Hedefler:** ayrı hedef katmanı v0.1'de yok, v0.2 sonrası listesine alınır. v0.1'de NPC amacı kişilik, eylem ölçütleri ve hafızadan doğar. Plan 3'teki "kendi hedeflerine göre" ifadesi v0.1 için bu kapsamda okunur.
2. **Stres:** 6. haftada kalır. `celisen_ozellikler` bu hafta okunur, doğrulanır ve karar izinde görünür; stres olayı üretilmez. Taban değer eklenmez. 6. haftanın takılacağı tek nokta bırakılır.
3. **Eş zamanlı algı:** NPC eylemleri ve NPC hareketleri için algı, adım 5 başındaki konum görüntüsüne göre hesaplanır. Aynı tick'te mekandan ayrılan tanık olayı görür, aynı tick'te varan görmez. Sonuç NPC işleme sırasına bağlı olamaz.
4. **Hareket olayları:** Bir hareket iki dış olay üretir: `ayrildi` (`Mekan` = kalkış) ve `geldi` (`Mekan` = varış). `konum!` gerçeği ayrı olayla, adım 6'nın en sonunda yazılır. Bu iki olay için `DogrudanAlgi` içine özel durum ekleme. Karşılıklı yer değiştiren iki NPC birbirinin `geldi` olayını görür. Oyuncu hareketi de aynı olayları kullanır.
5. **Decimal eğriler:**
   - `ustel`: `x^k`, `k` tam sayı ve 1..8 aralığında, tekrarlı çarpımla.
   - `lojistik`: `1 / (1 + e^(-k(x-m)))`. `e^y`, `Yanki.Core` içinde elle yazılmış decimal seriyle hesaplanır; `y` belgelenmiş bir aralığa kırpılır, sabit terim sayısı kullanılır.
   - Eğri çıktısı 0..1 aralığına kırpılır ve 6 ondalığa yuvarlanır. Mevcut yuvarlama kuralına uy; yoksa dur ve sor.
   - Altın değerler `tools/golden_ref.py` içindeki bağımsız Python hesabıyla doğrulanır.
6. **DURUM.md eski kaydı:** PR #8 satırı merge edildi olarak düzeltilir.

## Değişmez kurallar

- Çekirdek kod `Yanki.Core` içinde, LangVersion 9.0, NuGet paketi yok, iki hedefte de derlenir.
- Determinizm: `System.Random`, `DateTime.Now`, `Guid.NewGuid`, `GetHashCode`/`HashCode` yasak. Sırasız `Dictionary`/`HashSet` gezintisi yasak (ordinal sıralı). `float`/`double` ve `Math.Pow` gibi kayan noktalı matematik yasak, sayılar `decimal`. Tek RNG `AnahtarliRng`.
- Her durum değişikliği bir olaydır; `NedenOlaylar` ve `KuralId` taşır.
- `Olay` tipine yeni alan EKLEME. Gerekirse dur ve sor.
- TDD: her parça için önce kırmızı test.
- Kod tanımlayıcıları ve JSON anahtarları ASCII.
- Çekirdeğe dosya sistemi erişimi ekleme. Dosya okuma CLI'da kalır; çekirdek metin ya da model alır.
- JSON için mevcut `Yanki.Core.Json` ve `KriterOkuyucu` kullanılır. İkinci kriter ya da sorgu yolu açma.
- LLM karar vermez (ADR-002).
- Mevcut `tests/golden/mini-han-1.json` hash'i değişmemeli. Değişiyorsa dur, nedenini göster, onayımı bekle.

## Teslimler

### 1. Eylem tanımları ve yükleyici

- Eylemler JSON ile tanımlanır, kod değiştirmeden genişler. Senaryo `eylem_dosyalari` ile bağlar. Bu alan yoksa NPC karar adımı hiçbir olay üretmez.
- v0 şeması: `id`, `olay_turu`, `hedef` türü, `taban`, `on_kosullar`, `olcutler`, kişilik/fikir/ruh hali çarpan tanımları, `celisen_ozellikler`, `etkiler`.
- `on_kosullar` mevcut kriter biçimini kullanır ve ADR-010 mantığıyla çalışır: hepsi tutmalı.
- `etkiler` en az bir öğe içerir. Mevcut kural etkisi biçimini yeniden kullan, ayrı bir etki sistemi açma. Eylemin dışarı görünen sonucu `olay_turu` türünde bir olaydır; böylece mevcut sonuç kuralları NPC eylemlerine de çalışır.
- `celisen_ozellikler` okunur, doğrulanır ve karar izinde görünür. Bu hafta stres yazılmaz.
- Doğrulayıcı:
  - şema hatası, boş etki, bilinmeyen kişilik özelliği, bilinmeyen eğri, geçersiz kriter ve yinelenen kimliği reddeder;
  - eylem bağlamında geçersiz alanları (`olay_rolu`, `olay.fail`, `olay.hedef`, `konusan`, `dinleyen`) reddeder;
  - NPC bilgisini aşan `gercek(...)` sorgusunu reddeder; sözlük bunu NPC zihnine kapatıyor;
  - henüz bağlı olmayan `yay` ve `yalan_soyledi` sorgularını açık mesajla reddeder.
- Hata biçimi kural doğrulayıcısıyla aynı olsun: `dosya:satir: eylem "id": mesaj`.

### 2. Aday üretimi

- Aday = (eylem, hedef). Hedef türleri v0'da en az: hedefsiz, aynı mekandaki NPC, aynı mekandaysa oyuncu, bağlı komşu mekan.
- Ön koşullar değerlendirilir, tutmayan aday elenir.
- Karar yalnızca o NPC'nin zihninden ve kriter sözlüğünün izin verdiği sorgulardan beslenir.
- Aday üretimi tek noktada toplansın. 7. haftadaki korumalı bayrak filtresi buraya takılacak; filtreyi şimdi yazma.
- Sıra sabit: eylem kimliği, sonra hedef, ordinal.

### 3. Puanlama

- Formül: `puan = taban × Π(olcut_egrisi(girdi)) × kisilik_carpani × fikir_carpani × ruh_hali_carpani`.
- Ölçüt girdisi kriter sözlüğünden gelir (sayısal sonuç; doğru/yanlış 1/0), `min`/`max` ile 0..1 aralığına normalize edilir.
- Eğri tipleri: `dogrusal`, `ustel`, `lojistik`, `basamak`.
- Çarpan aralıkları kod sabiti ya da ayar sınırıdır, aşılamaz: kişilik 0.25–2.0, fikir 0.6–1.5, ruh hali 0.8–1.25. Sınırları testle koru.
- Hedefsiz eylemde fikir çarpanı 1'dir.
- Minnet ve benzeri modifier'lar yalnızca o hedefe yönelik adayın puanını değiştirir; NPC'nin genel kişiliğini değiştirmez.

### 4. `IActionSelector` ve `UtilitySelector`

- Arayüz: NPC, adaylar ve bağlam alır; seçilen aday ve karar izini döndürür. Karar kodu arayüz dışına bağımlı olmasın.
- Varsayılan: puanı en yüksek adayın %70'ine ulaşan ilk 3 aday arasından puan ağırlıklı seçim. N ve eşik ayarlanabilir, N=1 argmax'tır.
- Puanı 0 olan aday seçilemez.
- Eşitlikte sıra: puan azalan, sonra eylem kimliği ve hedef ordinal.
- RNG: `AnahtarliRng.Icin(Seed, tick, npcId, "karar")`. NPC işleme sırası değişse de her NPC'nin zarı aynı kalır.
- Karar sonucu olay kaydına ve kurallara arayüz üzerinden gitsin, `UtilitySelector` iç ayrıntısına değil.

### 5. Karar izi ve nedensellik

- Karar izi: bütün adaylar, elenenler ve nedeni, ölçüt katkıları, üç çarpan, son puan, top-N listesi, seçilen, `celisen_ozellikler`.
- Varsayılan yaklaşım: `karar_verildi` olayı. İzi `Olay.Yuk` içinde kanonik JSON olarak taşır, `KuralId` boş bırakılmaz.
- Bu olay algılanmaz, kural tetiklemez ve durum hash'ine girmez. Testle koru.
- Eylem olayı: `KuralId` plan 5.1'e göre eylem tanımını gösterir. Kural kimlikleriyle çakışmaması için `eylem:<id>` kullan.
- Eylem olayının `NedenOlaylar` listesi karar olayını içerir. Karar olayının `NedenOlaylar` listesi karara katkı veren zihin kaynaklarını (aktif modifier'ı doğuran olay, ruh halinin nedeni vb.) tekilleştirilmiş ve sıralı içerir.
- Kaynak olayı bulunamayan girdi varsa (örneğin eksenin toplam değeri) raporla, uydurma.
- Hiç aday yoksa olay yazılmaz. Aday var ama hepsi 0 ise `karar_verildi` yazılır, seçilen boş olur.
- Bu yaklaşım günlüğü ya da oturum biçimini gereksiz şişiriyorsa dur, ölçümle birlikte alternatif sun.

### 6. Tick döngüsü

- Plan 6'nın 5. ve 6. adımlarını `Simulasyon.TekTick` içindeki işaretli yere tak.
- Bütün NPC'ler adım 5 başındaki aynı durumu görür. Aynı adımda başka NPC'nin seçimi görünmez (kurallardaki tek durum görüntüsü kararıyla aynı).
- NPC'ler kimlik sırasıyla işlenir, ama sonuç sırası değişmez olmalıdır.
- Adım 6 sırası:
  1. Seçimleri `karar_verildi`, eylem, `ayrildi` ve `geldi` olayları olarak yaz.
  2. Bu dış olayların algısını adım 5 başındaki konum görüntüsüyle hesapla ve algı olaylarını yaz.
  3. Gecikmeli eylem etkilerini ve `konum!` değişimlerini en son yaz.
- Sonuç kuralları bu eylemler için sıradaki tick'te çalışır (4. haftanın kaskad kararı). Bu tick'te algılayanların listesini sakla. Kural değerlendirirken `Algilayanlar` listesini o anki konumdan yeniden hesaplama.
- Sıradaki tick'in Algı adımı bu olayları ikinci kez işlememeli. Testle koru.
- `_devredilenler` kuyruğundaki diğer olayların mevcut davranışı değişmez.
- Zincir derinliği sınırı 7. haftadadır. Bu hafta NPC eylemleri, kural çıktıları üzerinden aynı tick'te zincirlenmez.

### 7. Hareket

- NPC hareketi ve 4. haftadan devreden oyuncu hareketi tek mekanizmayı kullanır.
- `ayrildi` ve `geldi` yerleşik olay türleridir; `Yuk` karşı mekanı taşır.
- Yalnızca bağlı komşu mekana gidilir; aksi `Submit` ve aday üretiminde reddedilir.
- Oyuncu hareketi eylem günlüğüne girer ve replay'de tekrar üretilir. Oyuncu olayları 1. adımda işlenir, NPC konumları o sırada değişmez.
- Testler:
  - aynı tick'te A'dan ayrılan tanık A'daki eylemi görür;
  - aynı tick'te A'ya varan aynı eylemi görmez;
  - A ve B arasında yer değiştiren iki NPC birbirinin `geldi` olayını görür;
  - sonuçlar NPC işleme sırasından bağımsızdır.

### 8. Dış API ve CLI

- `Simulasyon`: `KararIzi(npc)` ve son karar sorgusu, eylem tanımlarının yüklenmesi. Mevcut `Submit`, `Tick`, `Neden` korunur.
- `yanki why <olayId>` tam sürüm: karar olayına gelince adayları, puan bileşenlerini ve seçimi okunur satırlarla yazar.
- `yanki query`: NPC'nin son kararını gösterir.
- `yanki run` ve `replay` eylemli senaryolarla çalışır; oyuncu hareketi `run` ile verilebilir.
- Yeni paket yok.

### 9. Senaryo ve golden replay

- İçerik benim alanım. `samples/MiniHan` değişmesin.
- Motor testlerini fikstürlerle yaz. Yeni örnek senaryoya geçmeden dur ve 2–3 somut seçenek sun: en az 3 NPC, 3–5 eylem, mevcut kuralları tetikleyen kısa bir akış ve beklenen sonuç. Ben seçince `samples/` altına yaz.
- Seçenekler dedikodu, stres, cooldown ve korumalı bayrak gerektirmemeli.
- Aynı eylemin her tick tekrarını cooldown yazmadan, mevcut `hafiza_var(olay_turu, ben, n)` gibi ön koşullarla sınırla. Yeterli olmuyorsa açık karar olarak yaz.
- Uçtan uca senaryo testi: bir NPC eylemi başka NPC'nin fikrini değiştirir, `why` modifier'dan algıya, eylem olayına ve karar izine iner.
- Golden replay #2: seed + senaryo + oyuncu eylem günlüğü + beklenen son hash. NPC kararları dosyaya yazılmaz, tekrar hesaplanır. CI her push'ta oynatır.

### 10. Testler

- Determinizm: aynı seed + aynı girdi = aynı hash ve aynı olay günlüğü.
- NPC işleme sırası ya da kimlik listesi girişi değişse de aynı sonuç.
- Farklı seed, top-N içinde en az bir fikstürde farklı seçim üretebilir.
- N=1 argmax'tır; eşikte kalan, eşik altı ve puanı 0 aday testleri.
- Eğriler için uç değer ve monotonluk testleri; `e^y` ve lojistik için `tools/golden_ref.py` ile üretilen altın değerler.
- Çarpan sınırları: en uç zihin değerlerinde bile aralık dışına çıkmaz.
- Kişilik tutarlılığı: bir minnet modifier'ı açgözlü NPC'nin genel kişilik çarpanını ve başka hedefli adaylarını değiştirmez.
- `celisen_ozellikler` izde görünür ve stres değişmez.
- Yasaklı API testi yeni klasörleri de kapsasın.
- Mevcut bütün testler ve `mini-han-1` golden testi yeşil kalsın.

### 11. Dokümantasyon

- ADR-011: karar modeli ayrıntıları (eylem şeması, çarpan eşlemesi, karar izinin yeri, tek durum görüntüsü, eş zamanlı algı, hareket olayları, decimal eğriler, hedeflerin ertelenmesi). "Gerekçe (Yusuf)" boş kalır.
- `docs/criteria-language.md`: eylem bağlamındaki roller ve yasaklı sorgular.
- `docs/authoring-guide.md`: eylem yazımı.
- `docs/notes/karar.md`: mevcut notlarla aynı biçimde.
- `docs/notes/tick-dongusu.md` ve `docs/notes/algi.md`: 6. adım sırası, algı zamanı, hareket olayları.
- `src/Yanki.Core/Decision/README.md` ve CHANGELOG.
- `docs/DURUM.md`: 5. hafta, karara bağlananlar, açık kararlar, sıradaki hafta. PR #8 satırını düzelt. Hedeflerin v0.2 sonrasına ertelendiğini açıkça yaz.
- README: durum satırını güncelle, diğer bölümleri doldurma.

## Yusuf'a bırak

Şunlarda karar verme, seçenek sun. Planı sunarken ilk 2 maddeyi öneri + ödünleşimle birlikte sor:

1. Eylem hedefi rolü: mevcut rollere yeni `eylem.hedef` eklenmesi mi, başka çözüm mü. Sözlüğe fonksiyon ekleme gerektiren her şey benden onay ister.
2. Kişilik/fikir/ruh hali çarpan eşlemesi: değerler aralığa nasıl çevrilecek, birden çok özellik nasıl birleşecek.
3. Örnek senaryo içeriği ve varsayılan denge sayıları.

## Çalışma şekli

1. Her şeyi oku, `superpowers:writing-plans` ile adım adım plan çıkar, yukarıdaki kararlarla birlikte bana göster, ONAYIMI BEKLE.
2. Onaydan sonra `superpowers:test-driven-development` ile çalış: önce kırmızı test, her parça küçük commit, commit sonuna `Co-Authored-By`.
3. Bu prompt'taki "Karara bağlananlar" dışında plan, CLAUDE.md ve bu prompt çelişirse dur ve sor.
4. Bitirmeden `superpowers:verification-before-completion`:
   - `dotnet build Yanki.slnx -c Release` iki hedef, 0 uyarı.
   - `dotnet test Yanki.slnx -c Release` geçiyor.
   - Komut çıktılarını göster.
5. PR'dan önce `superpowers:requesting-code-review` ile kendi diff'ini determinizm, sıra bağımlılığı ve nedensellik açısından gözden geçir.
6. Push et, `hafta-05` → `main` PR'ı aç. Açıklamada ne yapıldığını, açık noktaları ve kontrol noktasının nasıl doğrulandığını yaz.
7. CI'ın son commit'te yeşil olduğunu doğrula ve PR linkini ver. Merge etme.
8. Son mesajında şunlar olsun: ne yaptın, hangi kararları sen verdin, benim yapmam gerekenler.
