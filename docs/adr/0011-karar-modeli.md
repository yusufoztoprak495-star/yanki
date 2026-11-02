# ADR-0011: Karar modeli: eylem şeması, puanlama, karar izi ve eş zamanlı algı

**Durum:** Önerildi
**Tarih:** 2026-11-02
**Kabul eden:** —
**Kabul tarihi:** —

## Bağlam

5. hafta NPC'lerin kendi eylemlerini seçmesini ekler (plan 5.5, 6. adım 5 ve 6): eylem JSON ile tanımlanır, aday üretilir, puanlanır, seçilir, olaya dönüşür ve bu olay başka NPC'ler tarafından algılanıp sonuç kurallarını tetikler. ADR-001 (olay kaynaklı çekirdek), ADR-002 (kural + utility AI, LLM karar vermez), ADR-003 (tek kriter dili), ADR-007 (`decimal`), ADR-008 (zihin olaylardan türer) ve ADR-010 (tüm kriterler tutmalı) bağlayıcıdır. Bu ADR yeni kararları ve plana göre yapılan sapmaları tek yerde toplar.

## Karar

### 1. Hedefler v0.1'de yok (Yusuf)

Ayrı bir hedef zihin katmanı v0.1'de yoktur; v0.2 sonrası listesine alındı. NPC'nin amacı kişilik, eylem ölçütleri ve hafızadan doğar. Plan 5.4'teki "Hedefler" satırı ve plan 3'teki "kendi hedeflerine göre" ifadesi v0.1 için bu kapsamda okunur. **Plandan sapma.**

### 2. Eylem şeması (v0)

Eylemler JSON ile tanımlanır; senaryo `eylem_dosyalari` ile bağlar (yoksa NPC karar adımı hiçbir olay üretmez). Alanlar: `id`, `olay_turu`, `hedef` (`yok`, `npc`, `oyuncu`, `komsu_mekan`), `taban` (0 < taban ≤ 1000), `on_kosullar` (mevcut kriter biçimi, hepsi tutmalı, ADR-010), `olcutler`, `kisilik_carpani`, `fikir_carpani`, `ruh_hali_carpani`, `celisen_ozellikler`, `etkiler`. `etkiler` mevcut kural etkisi biçimidir (ayrı etki sistemi yok) ve en az bir öğe içerir. İstisna: `komsu_mekan` hareket eylemi `olay_turu` taşımaz ve etkisiz olabilir; hareketin kendisi etkidir. Eylemin dışarı görünen sonucu `olay_turu` türünde bir olaydır, böylece mevcut sonuç kuralları NPC eylemlerine de çalışır. Hata biçimi: `dosya:satir: eylem "id": mesaj`.

Doğrulayıcı eylem bağlamında `olay_rolu`, `olay.fail`, `olay.hedef`, `konusan`, `dinleyen`, NPC bilgisini aşan `gercek(...)` ve henüz bağlı olmayan `yay`/`yalan_soyledi` sorgularını reddeder. `celisen_ozellikler` bu hafta yalnızca okunur, doğrulanır ve karar izinde görünür; **stres yazılmaz** (6. hafta). Taban değer eklenmedi.

### 3. `eylem.hedef` rolü (Yusuf: seçenek A)

Yalnızca eylem bağlamında geçerli yeni bir rol. Kural ve diyalog bağlamında doğrulayıcı reddeder; hedefsiz eylemde de reddedilir. `komsu_mekan` eyleminde hedef bir mekandır: `fikir(eylem.hedef)`, `eksen(eylem.hedef, ...)` gibi kişi bekleyen sorgular ve kişi bekleyen etkiler (modifier, eksen, duygu) reddedilir; `eylem.hedef` yalnızca önerme içinde ve `gercek` etkisinin şablonunda kullanılabilir. Sözlüğe fonksiyon eklenmedi (16 fonksiyon). `RolAdlari.Ad` ve `SorguBaglami` artık açık `case` kullanır; tanımsız enum değeri sessizce `olay.hedef` olmaz.

### 4. Puanlama ve çarpan eşlemesi (Yusuf)

`puan = taban × Π(ölçüt_eğrisi(girdi)) × kişilik_çarpanı × fikir_çarpanı × ruh_hali_çarpanı`. Ölçüt girdisi kriter sözlüğünden gelir (doğru/yanlış 1/0), `min`/`max` ile 0..1'e normalize edilir (0..1 dışı kırpılır).

- **Kişilik:** özelliklerin ağırlıklı ortalaması (`ters` ise `1−v`); 0 → 0.25, 0.5 → 1.0, 1 → 2.0 parçalı doğrusal.
- **Fikir:** `fikir(eylem.hedef)` −100..100; −100 → 0.6, 0 → 1.0, +100 → 1.5 parçalı doğrusal; `yon: artan|azalan`.
- **Ruh hali:** listelenen duygu şiddetlerinin işaretli ağırlıklı toplamı −1..1'e kırpılır; −1 → 0.8, 0 → 1.0, +1 → 1.25.
- Tanımsız çarpan 1.0'dır; hedefsiz ve `komsu_mekan` eyleminde fikir çarpanı tanımlanamaz ve 1.0'dır. Ağırlıklar 0'dan büyüktür. Kod sabiti sınırları (0.25–2.0, 0.6–1.5, 0.8–1.25) son kırpma olarak her zaman uygulanır.
- Minnet gibi modifier'lar yalnızca o hedefe yönelik adayın fikir çarpanını etkiler; genel kişilik çarpanı ve başka hedefli adaylar değişmez (testle korunur).
- Ara değerler, çarpanlar ve puan `KanonikYazici` kuralıyla (ToEven, 6 ondalık) yuvarlanır; karşılaştırma ve seçim bu değerlerle yapılır.

### 5. Decimal eğriler (Yusuf)

`dogrusal`, `ustel` (`x^k`, k tam sayı 1..8, tekrarlı çarpım), `lojistik` (`1/(1+e^(−k(x−m)))`, `0 < k ≤ 100`, `0 ≤ m ≤ 1`) ve `basamak` (`x ≥ eşik`). `e^y` elle yazılmış decimal seriyle hesaplanır: `y` önce `[−30, 30]` aralığına kırpılır, `|y|/64` için sabit 18 terimli Taylor serisi, 6 kare alma; negatif `y` için `1/e^|y|`. Çıktı 0..1'e kırpılır ve 6 ondalığa yuvarlanır. Altın değerler `tools/golden_ref.py` içinde bağımsız Python `Decimal.exp()` hesabıyla doğrulanır. Yasaklı API testi `Math.Pow/Exp/Log/Sqrt/Trig` kullanımını da yakalar.

### 6. Seçici

`IActionSelector` (NPC, adaylar, bağlam → seçilen aday ve karar izi) ve varsayılan `UtilitySelector`: puanı en yüksek adayın %70'ine ulaşan ilk 3 aday arasından puan ağırlıklı seçim; `N` ve eşik ayarlanabilir, `N=1` argmax. Puanı 0 olan aday seçilemez. Sıra: puan azalan, eşitlikte eylem kimliği ve hedef ordinal. Zar `AnahtarliRng.Icin(Seed, tick, npcId, "karar")` akışından gelir, bu yüzden NPC işleme sırası seçimi etkilemez. Simülasyon yalnızca arayüzün sonucunu kullanır.

### 7. Aday üretimi tek noktada

`AdayUretici`: hedef türüne göre aday (hedefsiz; aynı mekandaki başka NPC; aynı mekandaysa oyuncu; bağlı komşu mekan), ön koşullar hepsi tutmalı. 7. haftadaki korumalı bayrak filtresi buraya takılacak (şimdi yazılmadı). **Z2:** `eylem.hedef` kullanmayan ön koşullar bir çağrıda (NPC'nin o karar adımında) eylem başına bir kez hesaplanır; önbellek çağrının dışına taşmaz, elenme nedenleri değişmez.

### 8. Karar izi: günlükte kısa iz, tam iz replay ile (K3, Yusuf)

`karar_verildi` olayı (`KuralId: karar`, fail karar veren NPC) günlükte **kısa iz** taşır: seçilen, top-N havuzu ve puanları, zar, aday/elenen sayıları, kaynak olaylar (`NedenOlaylar`), kaynaksız girdiler. Bütün adaylar ve puan bileşenleri günlüğe yazılmaz. Tam iz bellekte yalnızca NPC başına SON karar için tutulur (`Simulasyon.KararIzi(npc)`); geçmiş bir kararın tam izi `Simulasyon.TamIziIste` ile açıkça istenir ya da `KararIziYenidenUretici` oturumu o karara kadar replay ile oynatarak üretir ve günlükteki kısa izle doğrular; uyuşmazlık sessizce geçmez (`KararIziUyusmazligiException`, CLI çıkış kodu 4). `Simulasyon.GecmisKararKisaIzi` geçmiş karar için açıkça kısa izi döndürür; gizli önbellek yoktur. Oturum dosyasına NPC kararı ya da iz yazılmaz. Bu olay algılanmaz, kural tetiklemez (`ne_zaman` olarak yazmak doğrulayıcı hatasıdır) ve durum hash'ine girmez. **Plandan ve görev metnindeki "iz `Olay.Yuk` içinde" ifadesinden sapma** (Yusuf onayıyla, bkz. Değerlendirilen seçenekler).

Eylem olayının `KuralId`'si `eylem:<id>` (kural kimlikleriyle çakışmaz), `NedenOlaylar`'ı karar olayıdır. Karar olayının `NedenOlaylar`'ı yalnızca **seçilen adayın** girdilerine katkı veren zihin kaynaklarıdır (aktif modifier'ı doğuran olay, eksenin son değişim olayı, ruh halinin nedeni, inanç kanıtları, hatırlanan olay), tekilleştirilmiş ve sıralı. Kaynak olayı olmayan girdi uydurulmaz, `kaynaksiz` listesinde raporlanır: birikmiş eksen değeri (yalnızca son değişim bilinir, yine de kaynak gösterilir ve birikim notu düşülür) ve stres.

Hiç aday kalmazsa (ön koşullardan sonra) olay yazılmaz; aday var ama hepsi 0 puanlıysa ya da seçici bir şey seçmediyse boş seçimli `karar_verildi` yazılır. Elenenlerin hepsini yazmak günlüğü gereksiz şişirirdi.

### 9. Tek durum görüntüsü ve tick adımı 5–6

Bütün NPC'ler adım 5 başındaki aynı durumu görür: kararlar NPC kimlik sırasıyla hesaplanır ve hiçbiri yazılmadan önce hepsi hesaplanır. Adım 6 sırası: (1) `karar_verildi`, eylem (hareket eyleminde `ayrildi` ve `geldi`) olayları; (2) bu dış olayların algısı, adım 5 başındaki konum görüntüsüyle; (3) eylem etkileri ve `konum!` değişimleri en son. Sonuç kuralları bu olaylar için sıradaki tick'te çalışır (4. haftanın kaskad kararı); algılayanlar adım 6.2'nin algı sonucundan saklanır, kurallar onları yeniden hesaplamaz, sıradaki tick'in algı adımı bu olayları ikinci kez işlemez ve saklanan bilgi bir tick sonra atılır. Sıradaki tick'in tetik kuyruğu `Olay.Id` artan sırayla kurulur.

### 10. Eş zamanlı algı ve hareket (Yusuf)

Aynı tick'te mekandan ayrılan tanık olayı görür, aynı tick'te varan görmez; sonuç NPC işleme sırasına bağlı değildir. `DogrudanAlgi`'ye özel durum eklenmedi: `konum!` değişimi algıdan sonra yazıldığı için algı doğal olarak başlangıç görüntüsünü görür. Bir hareket iki dış olay üretir: `ayrildi` (`Mekan` = kalkış) ve `geldi` (`Mekan` = varış); ikisinin `Hedef` alanı **karşı mekandır** ve yükte `karsi_mekan` da bulunur, böylece algıdan doğan inanç mekanı taşır (`ayrildi(Can,pazar)`). `konum!` ayrı olayla yazılır. Karşılıklı yer değiştiren iki NPC birbirinin `geldi` olayını görür. `konum!` olayları algılanmaz ve kural tetiklemez.

Oyuncu hareketi ayrılmış `hareket` eylemidir (senaryoda bildirilmez, `ayrildi`/`geldi`/`karar_verildi` gibi çekirdeğe ayrılmış adlar senaryoda yeniden tanımlanamaz): hedef bağlı komşu mekan olmalıdır, komşuluk `Submit` anında denetlenir, tick başına bir hareket kabul edilir. Oyuncu olayları adım 1'de `Submit` sırasıyla işlenir ve oyuncunun konumu hemen değişir; aynı tick'te hareketten sonra gelen oyuncu eylemi varış mekanında olur; NPC konumları değişmez. Hareket eylem günlüğüne girer ve replay'de tekrar üretilir.

### 11. Mekan hedefli olaylar

`ayrildi` ve `geldi` olaylarında `olay.hedef` bir mekandır: doğrulayıcı bu olaylarda `olay.hedef`'e modifier/eksen/duygu yazan kuralı reddeder. Statik olarak bilinemeyen durumda (hedefi mekan olan bir oyuncu eylemi) kural motoru kişi olmayan hedefe etki üretmez; zihne mekan adına ilişki kaydı yazılmaz (sessiz durum bozulması yok). Bu atlama için 7. haftada fren olayı yazılacak.

### 12. Performans (Z1, Yusuf)

`Zihin.HafizaVar` olay kimliğine göre sıralı ayrı bir dizinle erken durur. Pencere hatırlanan olayın tick'ine göre uygulanır; hafıza kayıtlarının ekleniş sırası bu anahtara göre sıralı değildir (kural `hafiza` etkisi bir önceki tick'in olayını sona ekler, mevcut kayıt yerinde güncellenir) ve naif erken durdurmanın yanlış sonuç verdiği kırmızı testle gösterildi. Günlük olay tick'inin kimlikle azalmamasını zorunlu kıldığı için kimliğe göre sıralı dizin doğrudur; eski tam taramayla seed'li rastgele karşılaştırma testi ve değişmeyen hash ile korunur. Z3 (hash önbelleği) ve Z4 (algı hacmi) 7. haftaya bırakıldı.

Z2'nin yanında ölçülen sonuç (benchmark değil, `tools/olcum`; ayrıntı `docs/notes/karar.md`): 50 NPC stres fikstüründe tick başına 321 → 82 ms, `karar_verildi` günlük payı %71 → %10; olay sayıları ve son durum hash'leri K3/Z1/Z2 öncesiyle birebir aynı. 5 ms/tick hedefi 50 NPC'de hâlâ tutmuyor; kalan maliyet durum hash'i (Z3) ve algı hacmi (Z4).

### 13. Bekleme eylemi ve tekrar sınırı
Seçici yalnızca puanı 0'dan büyük adaylardan seçer. Sınırlı eylemler (`hafiza_var` ile) elenince geriye tek pozitif aday olarak `git` kalırsa bütün NPC'ler her tick sürü halinde dolaşır (senaryo taslaklarında görüldü). Puanı 0'dan büyük, hedefsiz, ön koşulsuz küçük bir bekleme eylemi (`dinlen`) `docs/authoring-guide.md` içinde **şiddetle önerilir**; motor bunu **zorunlu kılmaz** ve yerleşik bir bekleme eylemi eklemez (bekleme senaryonun dengesine ait bir yazar kararıdır). Tekrar sınırı cooldown gelene kadar `hafiza_var(olay_turu, ben, n)` ile yapılır; sınırları (hedefe göre değil eylem türüne göre, hafıza yaklaşık 200 tick sonra unutulur, "en fazla k kez" ifade edilemez) 7. haftada cooldown ile kapatılacak.

### 14. Onaylanan adlar ve uygulama sırası
Yusuf'un onayıyla: Z1 için `OlayId`'ye göre sıralı ayrı dizin (naif erken durdurma yok; dizin hash'e girmez), CLI çıkış kodu 4 (iz uyuşmazlığı), API adları `TamIziIste`, `YakalananIz`, `TutulanTamIzSayisi`, `GecmisKararKisaIzi`, `KararIziYenidenUretici`, `AnahtarliRng` ile sabit seed'li rastgele testler. Uygulama sırası bağımlılığa göre değişti (aday → puanlama → seçici → karar izi → tick ve hareket); kapsam aynı. Aday yoksa olay yazılmaması, karar kaynaklarının yalnızca seçilen adaydan toplanması, hareket olaylarının `Hedef`'inin karşı mekan olması, mekan hedefli olayda kişi etkisi üretilmemesi ve yasaklı API kapsamı (`Math.Pow/Exp/Log/Sqrt/Trig`) bu ADR'de kaydedilmiştir.

## Değerlendirilen seçenekler

### Karar izinin yeri

|                                            | K1: tam iz günlükte                         | K2: günlükte kısaltılmış tam iz | **K3: kısa iz günlükte, tam iz replay ile** |
| ------------------------------------------ | ------------------------------------------- | ------------------------------- | ------------------------------------------- |
| Karmaşıklık                                | Düşük                                       | Orta                            | Orta (yeniden üretici, yakalama API'si)     |
| Günlük büyümesi                            | Aday sayısıyla (50 NPC: olay başına 7.8 KB) | Hâlâ ≈1 KB/olay                 | ≈350 bayt/olay, aday sayısından bağımsız    |
| Ölçülen `karar_verildi` payı (3/10/50 NPC) | %69.5 / %69.4 / %71.3                       | tahmin: %64 / %59 / %34         | %38.5 / %24.0 / %10.4                       |
| Günlüğe bakan biri                         | Her şeyi görür                              | Havuz + ilk 5                   | Yalnızca kısa iz; tam iz için replay        |

K1 ve K2 durak eşiğini (yarısı) aştı ya da küçük ölçeklerde aşıyordu; K3 seçildi. Replay deterministik olduğu için tam iz yeniden üretilebilir ve günlükle doğrulanır.

### `eylem.hedef` rolü

**A (seçildi):** yeni rol, yalnızca eylem bağlamında; fonksiyon eklemez. **B:** `olay.hedef`'i eylemde hedefe bağlamak: iki bağlamda iki anlam. **C:** rol yok: hedef başına puanlama yazılamaz.

## Ödünleşimler

K3 ile günlük küçük ve aday sayısından bağımsız kalır, ama günlükten tam izi doğrudan okuyamazsınız; `why` oturumu iki kez oynatır (bir kez günlük için, bir kez izi yakalamak için). Karar kodu replay'e bağımlıdır: senaryo, kural ya da eylem dosyası değişirse eski oturumun tam izi üretilemez ve açık hata alınır (sessiz yanlış gösterim yerine). Hedeften bağımsız ön koşul önbelleği yalnızca bir çağrıda yaşar; basit ama farklı hedeflerde aynı sonucu varsayar (kriter sözlüğü bu varsayımı sağlar: `eylem.hedef` kullanmayan sorgu hedefe bağlı değildir).

## Gerekçe (Yusuf)

> Yusuf yazacak. Bu bölümü Claude Code doldurmaz; ADR Yusuf gerekçeyi yazana kadar "Önerildi" kalır, kararlar yine de bağlayıcıdır.

## Sonuçlar

- Kolaylaşan: NPC kararlarını ve nedenlerini `why` ile puan bileşenlerine kadar izlemek; eylemleri kod değiştirmeden JSON ile genişletmek; seçiciyi `IActionSelector` ile değiştirmek.
- Zorlaşan: günlükten tek başına tam iz okumak; algı hacmi (olayların ≈%90'ı algı olayı) ve durum hash'inin her tick'te bütün durumu yazması ölçeklenmeyi sınırlar.
- Yeniden bakılacak: Z3 (hash önbelleği) ve Z4 (algı başına iki olay, sınırsız hafıza) 7. haftada; `hafiza_var` ile tekrar sınırı (cooldown 7. haftada); korumalı bayrak filtresi `AdayUretici`'ye; stres `celisen_ozellikler` ile 6. haftada.

## Yapılacaklar

1. [x] Eylem şeması, yükleyici, doğrulayıcı, `eylem.hedef` rolü (5. hafta).
2. [x] Aday üretimi, puanlama, `UtilitySelector`, karar izi (kısa iz + tam iz yeniden üretimi), tick adımları 5–6, hareket (5. hafta).
   `samples/MiniKarar` ve golden replay #2 (`tests/golden/mini-karar-1.json`).
3. [x] Z1 ve Z2 (5. hafta).
4. [ ] Z3 ve Z4 (7. hafta, Yusuf'un kararıyla).
5. [ ] Yusuf gerekçeyi yazınca durum "Kabul edildi" yapılır.
