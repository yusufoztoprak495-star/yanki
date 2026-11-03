# Görev: Yankı — 6. hafta (Dedikodu ve stres)

## Bağlam

Plan: docs/plan/Yanki Plan v2.pdf. Bu görev SADECE 6. hafta.
Önce şunları oku: CLAUDE.md, docs/DURUM.md, planın 5.3, 5.4, 5.5, 5.6, 5.8, 6, 7, 9 ve 12. bölümleri, docs/criteria-language.md, docs/authoring-guide.md, tüm ADR'ler (özellikle 001, 003, 008, 010, 011), docs/notes/.
`main`'i çek, `hafta-06` dalı aç. Bu görev metnini birebir `docs/prompts/hafta-06.md` olarak kaydet.

Hedef kontrol noktası: oyuncu ya da NPC bir önerme anlatır → dinleyene `duydu` kanıtı düşer → dinleyen başka birine aktarır, güç her adımda azalır → kural fikri değiştirir → `yanki why` zinciri ilk anlatana kadar iner. Kişiliğe aykırı eylem stres biriktirir → eşikte kişiliğe uygun kriz olayı çıkar → kriz yeni bir olay zinciri başlatır → aynı seed aynı sonucu verir.

Bu hafta YAZMA:

- cooldown, tetiklenme ve zincir derinliği sınırları, fren olayları, korumalı bayraklar, yay değişkenleri, tam kayıt/yükleme (7. hafta)
- Z3 ve Z4 optimizasyonları, `replay` hash kapsamı, `AdayUretici` korumalı bayrak filtresi (7. hafta)
- 3 sıra etkisi testi ve CsCheck değişmezleri (7. hafta)
- diyalog, niyet üretimi, şablonlar (8. hafta)
- Kasaba Hanı içeriği (9. hafta)
- ablasyon, `coverage`, `diff`, `yanki validate` tam sürümü
- hedef zihin katmanı (v0.2 sonrası)
- yalanın kanıt çelişkisinden otomatik yakalanması (bu hafta yalnızca itiraf krizi ve yazarın kuralları; açık karar olarak yaz)
- kulak misafirliği (tanığın konuşmanın içeriğini duyması)

## Karara bağlananlar (Yusuf)

Bunları yeniden seçenek olarak sunma.

1. Bu hafta plan 5.3, 5.4 ve 5.5'teki dedikodu, yalan, stres ve kriz mekanizmalarını kapsar.
2. `tests/golden/mini-han-1.json` (`cee9956ef06df17f`) ve `tests/golden/mini-karar-1.json` (`fe29a63d6dfbc307`) hash'leri değişmemeli. Değişiyorsa dur, nedenini göster, onayımı bekle.
3. `samples/MiniHan` ve `samples/MiniKarar` değişmez.
4. DURUM.md'de PR #9 "Yusuf'un incelemesini bekliyor" yazıyor. PR #9 merge edildi, satırı düzelt.

## Önerilen varsayılanlar (ONAYIMDAN ÖNCE KODA GİRMEZ)

Planla birlikte bu listeyi bana göster, her madde için öneri + ödünleşimi yaz, kararımı bekle. Daha iyi bir seçenek görürsen onu da yaz. Sayılar denge alanımdır ve `ZihinAyarlari` ya da ayrı ayar sınıfında durur.

A. **Sözlük (16 → 18 fonksiyon, tasarım alanım):**

- `konu_puani(kime)` → sayı 0..1: bu NPC'nin o kişiye anlatabileceği en yüksek konu puanı. Karar ölçütlerinde kullanılır.
- `olay_konusu(desen)` → mantıksal: mevcut olay `anlatti` ise ve anlatılan önerme desene uyuyorsa doğru. Yalnızca sonuç kuralı bağlamında ve yalnızca içeriği duyan dinleyen için doğru döner.
- Alternatif: yeni fonksiyon yok, `kaynak(desen, olay.fail)` ile yetinilir. Ödünleşim: kural yanlış konuda da tetiklenebilir, puan da konunun ilginçliğini görmez.

B. **Aday yapısı:** Aday (eylem, hedef) olarak kalır. Konu adayın parçası olmaz, eylem seçildikten sonra belirlenir. Aday patlaması olmaz, karar izi biçimi korunur.

C. **Konu puanı:** `onem × yenilik × ilgi`.

- `onem`: önermenin bağlı olduğu olayın hafıza önemi, kayıt yoksa 0.5.
- `yenilik`: `1 − (tick − kanıt_tick) / 168`, alt sınır 0.
- `ilgi`: dinleyen önermede geçiyorsa 1; konuşanın inandığı bir önerme dinleyeni konuya bağlıyorsa 0.7 (`kardes(Mehmet,Can)` gibi, tek adım); konuşan önermede geçiyorsa 0.5; hiçbiri değilse 0.25.
- Eşik: puanı 0.1 altındaki konu anlatılmaz. Konu kalmazsa aday elenir, neden izde görünür.

D. **Aktarım gücü:** `yeni_guc = konusanin_guncel_kanit_gucu × guven_carpani`, `guven_carpani = 0.7 + 0.003 × eksen(dinleyen→konusan, guven)`. Nötr güvende 0.7 eder; plan 7'deki örnekle uyumlu. Oyuncunun kendi gücü 1.0.

- Kullanılan eksen dinleyenin konuşana duyduğu güvendir, fikir değil.
- Sonuç 0.15 altındaysa kanıt yazılmaz, söylenti ölür.
- Konuşan yalnızca inandığı ve güveni 0.3 ya da üstü olan önermeyi doğru diye anlatır.

E. **Yankı freni:** `duydu` kanıtı kök kaynağı taşır (`kanit_eklendi` yükünde `kok`, Olay tipi değişmez; `gordu` için boş kalır). Dinleyende aynı önerme için aynı kökten kanıt varsa yeni kanıt eklenmez, mevcut kanıt tazelenir. Böylece A→B→C→A zincirinde A kendi söylentisine daha çok inanmaz. Ayrıca NPC, bir konuyu onu duyduğu kişiye geri anlatmaz.

F. **Tekrar:** NPC aynı önermeyi aynı kişiye iki kez anlatmaz (kendi aktarım kaydından). Cooldown değildir, 7. hafta cooldown'ının yerini almaz.

G. **Konu sınırı:** Plan 5.3: `n = 1 + ⌊sosyallik × 2⌋`, fikir bandı `yakin` ya da `dost` ise +1. Konular puana göre azalan, eşitlikte önerme metni ordinal sırayla seçilir. Bir sohbet tek karardır ama her konu ayrı `anlatti` olayı olur, böylece kurallar konu bazında çalışır. Yalan eylemi tek konudur.

H. **Görünürlük:** `anlatti` varsayılan `sessiz`dir, yalnızca konuşan ve dinleyen algılar. Eylem tanımı `gorunurluk` ile `acik` yapabilir; tanık yalnızca konuşmanın olduğunu görür, içeriğini almaz.

I. **Yalan içeriği:** Eylem tanımında `yalan_onermeleri` (yazar listesi, `{eylem.hedef}` şablonu destekli). Konuşanın inandığı önermeyle çelişen ve `anlatilacak` yalan yoksa aday elenir. Birden çok uygun yalan varsa `AnahtarliRng.Icin(seed, tick, npc, "yalan")` seçer. Alternatif: yalnızca inandığı önermenin `degil(...)` hali; ödünleşimi iftira atamamasıdır.

J. **Stres:** Eylem tanımına `stres_tabani` eklenir. `stres += stres_tabani × Σ(celisen özellik değeri)`. Yazım adım 6.3'te `stres_degisti` olayıyla olur, `KuralId: eylem:<id>`, nedeni eylem olayıdır. `celisen_ozellikler` var ama `stres_tabani` yoksa doğrulayıcı reddeder. Ölçek önerisi: yalan söyle için 15.

K. **Kriz eşikleri:** 50 ve 80. Tek tick'te en fazla bir kriz, iki eşik birden aşıldıysa üstteki seviye. Kriz stres düşüşü: 50 seviyesinde 30, 80 seviyesinde 60. Değişmez: kriz sonrası stres alt eşiğin altında kalmalı. Doğrulayıcı buna aykırı ayarı reddeder, çünkü aksi halde her tick yeniden kriz çıkar.

L. **Kriz tablosu:** Senaryo `kriz_dosyalari` ile JSON bağlar. Satır: `id`, `seviye`, `olay_turu`, `hedef` (`yok`, `yalan_kurbani`, `en_yakin`), `kriterler`, `etkiler`. Seçim mevcut `Secici` ile yapılır, RNG `AnahtarliRng.Icin(seed, tick, npc, "kriz")`. Hedefi aynı mekanda olmayan satır elenir. Her seviye için kriterli olmayan bir yedek satır zorunludur. Alan yoksa kriz üretilmez, stres yine çalışır.

## Değişmez kurallar

- Çekirdek kod `Yanki.Core` içinde, LangVersion 9.0, NuGet paketi yok, iki hedefte de derlenir.
- Determinizm: `System.Random`, `DateTime.Now`, `Guid.NewGuid`, `GetHashCode`/`HashCode` yasak. Sırasız `Dictionary`/`HashSet` gezintisi yasak (ordinal sıralı). `float`/`double` ve kayan noktalı matematik yasak, sayılar `decimal`. Tek RNG `AnahtarliRng`.
- Her durum değişikliği bir olaydır; `NedenOlaylar` ve `KuralId` taşır.
- `Olay` tipine yeni alan EKLEME. Gerekirse dur ve sor.
- TDD: her parça için önce kırmızı test.
- Kod tanımlayıcıları ve JSON anahtarları ASCII.
- Çekirdeğe dosya sistemi erişimi ekleme.
- JSON için mevcut `Yanki.Core.Json` ve `KriterOkuyucu` kullanılır. İkinci kriter ya da etki yolu açma.
- LLM karar vermez (ADR-002).
- Konuşan, dinleyenin zihnini okuyamaz. Karar yalnızca kendi zihninden beslenir.

## Teslimler

### 1. Yalan gizliliği (önce kırmızı test)

- Doğru anlatım da yalan da dış dünyaya aynı `anlatti` olay türüyle çıkar. Dış olayda, yükte, algı önermesinde ve kural girdisinde yalan olduğunu belli eden hiçbir iz olmamalı.
- Yalan bilgisi ayrı, algılanmayan ve kural tetiklemeyen bir zihin olayıyla yalnızca konuşanın zihnine yazılır.
- Yalan eyleminin `olay_turu` değeri `anlatti` olmak zorundadır, doğrulayıcı başka değeri reddeder.
- `why` ve geliştirici çıktısı yalanı `eylem:<id>` ve zihin olayından gösterebilir; bu NPC bilgisi değildir.
- Test: yalan ile doğru anlatım, dinleyen ve tanık için bütün sorgularda birbirinden ayırt edilemez.
- Test: yanlış inandığı şeyi anlatan NPC yalancı sayılmaz. Yalan, konuşanın kendi inancına göre tanımlanır, dünya gerçeğine göre değil.

### 2. Aktarım kaydı ve `yalan_soyledi`

- Tek yeni zihin olayı: konuşanın zihnine `kime`, `onerme`, `yalan` bilgisini yazan aktarım kaydı. Yalan kaydı plan 5.3'teki `yalan_söyledim(kime, ne)` karşılığıdır, F maddesi de bunu okur.
- Kayıt boşken `KanonikMetin` değişmemeli; böylece eski golden hash'ler korunur.
- `ZihinSorguCozucusu` `yalan_soyledi(kime)` bağlanır. Kural ve eylem doğrulayıcılarındaki "bağlı değil" reddi yalnızca `yay` için kalır.

### 3. Konu seçimi

- A kararına bağlı olarak sorgular eklenir. Hesap tek sınıfta toplanır, karar ve aktarım aynı sonucu kullanır.
- Aynı tick'te aynı girdi aynı sonucu verir. Maliyet ölçülür; gerekirse tick içi önbellek kullanılır ama durum hash'ine girmez.
- Karar izi aday başına konu puanını gösterir.

### 4. "Anlat" ve "yalan söyle" eylemleri

- Eylem şemasına aktarım alanı eklenir (`aktarim`: `dogru` | `yalan`), kod değiştirmeden yeni konuşma eylemi yazılabilir.
- Hedef aynı mekandaki NPC ya da oyuncu olabilir.
- Adım 6.1: seçilen konular kadar `anlatti` olayı yazılır. Yük: `onerme`, `kok`.
- `NedenOlaylar`: karar olayı + konuşanın o önermeye dayanak kanıt olayı. `why` zinciri buradan önceki anlatana iner.
- Adım 6.2: mevcut algı akışı. Dinleyenin hafızası ve `anlatti(A,B)` gözlemi yazılır.
- Adım 6.3: dinleyene `duydu` kanıtı, konuşana aktarım kaydı. Dinleyen oyuncu ise kanıt yazılmaz.
- Çelişen önerme mevcut inanç kuralıyla çözülür, ikinci yol açılmaz.
- Kanıt yazımı adım başı durum görüntüsünü bozmaz: bir NPC'nin bu tick'te duyduğu, başka NPC'nin bu tick'teki kararını etkilemez. Testle koru.

### 5. Oyuncu anlatımı

- Oyuncu `anlatti` türünde eylem gönderebilir: hedef + önerme.
- `Submit`, hedefin oyuncuyla aynı mekanda olmasını ve önermenin geçerliliğini denetler.
- Kanıt, adım 1–2 akışında yazılır. Eylem günlüğüne girer, replay'de tekrar üretilir.
- Oyuncunun zihni yoktur, yalan kaydı tutulmaz. Dünya gerçeğine aykırı söylemesi NPC'ye bu yolla sızmaz.

### 6. Stres

- J kararına göre eylem seçildiğinde yazılır. Eylem seçilmediyse stres değişmez.
- Hiç stres eklenmeyen senaryoda hash değişmez.
- `yanki query` güncel stresi gösterir, karar izi `celisen_ozellikler` ile birlikte uygulanan stres artışını gösterir.

### 7. Kriz

- Tespit adım 4'tedir. Stres yalnızca artışla eşik geçer; sönüm sorgu anında hesaplanır, kriz için tick başına olay yazılmaz.
- Krizdeki NPC o tick eylem seçmez, `karar_verildi` yazılmaz.
- Kriz olayı, o NPC'nin seçilmiş eylemi gibi adım 6 yolundan geçer: olay, adım 5 başı konum görüntüsüyle algı, etkiler ve stres düşüşü 6.3'te, sonuç kuralları sıradaki tick'te. Ayrı bir kuyruk açma.
- `KuralId: kriz:<id>`. Kriz, en yeni 5 stres artış olayını `NedenOlaylar` içinde taşır. Bulunamayan neden uydurulmaz.
- `why`, krizden stres artışlarına, eylemlere ve karar izine iner.
- Testler: tam eşikte, eşik altında, sönümle eşik altına düşünce, iki eşik birden, kriz sonrası aynı krizin tekrarlanmaması, kişiliğe göre farklı satır seçilmesi, yedek satır.

### 8. Doğrulayıcı

Biçim: `dosya:satir: eylem "id": mesaj` ve kriz için `dosya:satir: kriz "id": mesaj`. Şema hatası, geçersiz aktarım değeri, `anlatti` dışı yalan olay türü, boş ya da geçersiz yalan önermesi, eksik `stres_tabani`, geçersiz kriz seviyesi, eksik yedek satır ve K değişmezine aykırı ayar reddedilir.

### 9. Tick döngüsü ve dış API

- Yorumları ve `docs/notes/tick-dongusu.md`'yi güncelle. "Stres krizi olayı üretilmez" notu kalkar.
- `Simulasyon`: aktarım kaydı, kim kimden duydu, stres sorguları.
- `yanki why`, `query`, `run`, `replay` konuşmalı ve krizli senaryolarla çalışır. Oyuncu anlatımı `run` ile verilebilir.
- Yeni paket yok.

### 10. Senaryo ve golden replay #3

- İçerik benim alanım. Motor testlerini fikstürlerle yaz.
- `samples/` altına geçmeden dur, 2–3 somut "Gerilim" seçeneği sun: en az 4 NPC, eylemler, kurallar, kısa akış, beklenen sonuç. Biri plan 7'deki Ayşe → Can zincirini, biri yalan → stres → itiraf krizini göstermeli.
- Ben seçince yaz. Kişilik ve taban sayılarının yer tutucu olanlarını README'de belirt.
- Uçtan uca test: anlatım zinciri kuralı tetikler, `why` ilk anlatana iner; aynı senaryoda kriz çıkar ve `why` eyleme iner.
- Golden replay #3: seed + senaryo + oyuncu eylem günlüğü + beklenen hash. CI her push'ta oynatır.

### 11. Testler

Yukarıdakilere ek olarak:

- Determinizm: aynı seed + aynı girdi = aynı hash ve aynı olay günlüğü.
- NPC işleme sırası değişse de aynı sonuç.
- Güç her adımda azalır, eşik altında aktarım durur.
- A→B→C→A yankısında A'nın güveni artmaz.
- Aynı konu aynı kişiye iki kez anlatılmaz.
- Konu sınırı sosyallik uçlarında ve yakın ilişkide doğru.
- Yanlış konuşmayı konuşan dışında kimse hedefin zihninden okumaz.
- Yasaklı API testi yeni klasörleri de kapsar.
- Mevcut bütün testler ve iki golden testi yeşil kalır.

### 12. Ölçüm

`tools/olcum` ile 50 NPC için dedikodulu ölçüm yap: tick süresi, olay sayısı, kanıt sayısı. Ölçüm öncesi değer 82 ms/tick. Optimizasyon yazma, sonucu `docs/notes/` ve DURUM.md'ye yaz. Bilgi patlaması görürsen veriyle bana göster.

### 13. Dokümantasyon

- ADR-012: dedikodu, yalan gizliliği, aktarım kaydı, konu seçimi, stres ve kriz kararları.
- `docs/criteria-language.md`: yeni fonksiyonlar, `yalan_soyledi`, bağlam kuralları.
- `docs/authoring-guide.md`: konuşma eylemi, yalan, stres ve kriz yazımı.
- Yeni `docs/notes/dedikodu.md` ve `docs/notes/stres.md`; `zihin.md`, `algi.md`, `karar.md`, `tick-dongusu.md` güncellemeleri.
- İlgili modül README'leri, CHANGELOG.
- `docs/DURUM.md`: 6. hafta, verilen kararlar, açık kararlar, 7. haftaya taşınanlar, sıradaki hafta. PR #9 satırını düzelt. 5. haftadan taşınan 6. hafta maddesini kapat.
- README: durum satırı.

### 14. ADR gerekçeleri

Gerekçe yazımını sana devrettim. ADR-011 ve ADR-012 "Gerekçe (Yusuf)" bölümlerini PR #8'deki gibi yaz. `CLAUDE.md`'deki "sen doldurma" kuralını bu devre göre güncelle. Durumları ben onaylayana kadar değiştirme.

## Yusuf'a bırak

Şunlarda karar verme, seçenek sun:

1. "Önerilen varsayılanlar" listesinin tamamı.
2. Sözlük değişiklikleri.
3. Örnek senaryo içeriği ve denge sayıları.
4. Olay biçimini değiştiren her şey.

## Çalışma şekli

1. Her şeyi oku, `superpowers:writing-plans` ile adım adım plan çıkar, "Önerilen varsayılanlar" listesiyle birlikte bana göster, ONAYIMI BEKLE.
2. Onaydan sonra `superpowers:test-driven-development` ile çalış: önce kırmızı test, her parça küçük commit, commit sonuna `Co-Authored-By`.
3. Bu prompt'taki "Karara bağlananlar" dışında plan, CLAUDE.md ve bu prompt çelişirse dur ve sor.
4. Senaryoya geçmeden 10. teslimdeki seçenekler için dur.
5. Bitirmeden `superpowers:verification-before-completion`:
   - `dotnet build Yanki.slnx -c Release` iki hedef, 0 uyarı.
   - `dotnet test Yanki.slnx -c Release` geçiyor.
   - Komut çıktılarını göster.
6. PR'dan önce `superpowers:requesting-code-review` ile diff'i determinizm, sıra bağımlılığı, nedensellik ve NPC bilgi sınırı açısından gözden geçir.
7. Push et, `hafta-06` → `main` PR'ı aç. Açıklamada ne yapıldığını, açık noktaları ve kontrol noktasının nasıl doğrulandığını yaz.
8. CI'ın son commit'te yeşil olduğunu doğrula ve PR linkini ver. Merge etme.
9. Son mesajında şunlar olsun: ne yaptın, hangi kararları sen verdin, benim yapmam gerekenler.
