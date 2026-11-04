# ADR-0013: Frenler, korumalar, yay değişkenleri, kayıt sürüm 2 ve kaynak modeli

**Durum:** Önerildi
**Tarih:** 2026-11-04
**Kabul eden:** —
**Kabul tarihi:** —

## Bağlam

7. hafta plan 5.10'daki frenleri ve korumaları (cooldown, tetiklenme sınırı, zincir derinliği, korumalı bayraklar, yay değişkenleri), plan 5.1/5.8'deki kayıt/yüklemeyi ve plan 5.4'teki "kanıtlar silinmez, kimden duydu birinci sınıf sorgu" sözünü ekler.
   ADR-001 (olay kaynaklı çekirdek), ADR-003 (tek kriter dili), ADR-007 (`decimal`), ADR-008 (zihin olaylardan türer), ADR-010 (tüm kriterler tutmalı), ADR-011 (karar modeli) ve ADR-012 (dedikodu) bağlayıcıdır.
   `Olay` tipine yeni alan eklenmez; çekirdek dosya sistemine dokunmaz. Aşağıdaki kararlar Yusuf'un 7. hafta görev metnindeki onaylarıdır; plana göre sapmalar ayrıca işaretlidir.

## Karar

### 1. Fren olayı

Tek olay türü `fren_uygulandi`; yük: `fren`, `kimlik` (kural/eylem/değişken), `npc`, `hedef`. `KuralId: fren:<tur>`; `NedenOlaylar`: engellenen tetikleyici varsa o (aday frenlerinde eylemin son tetiklemesi).
Türler: `cooldown`, `en_fazla`, `tick_siniri`, `genel_sinir`, `zincir`, `korumali`, `tanimsiz_rol`, `kisi_olmayan_hedef`, `yay_siniri`. Fren olayı bir zihin olayı DEĞİLDİR (`ZihinOlayiMi`'ye girmez): algılanmaz ve kural tetiklemez; `ne_zaman: fren_uygulandi` doğrulayıcıda reddedilir ve ad ayrılmıştır.

- **Kural frenleri:** engellenen her tetiklemede bir olay, yalnızca kriterleri tutmuş kural için. `tanimsiz_rol` istisnadır (kriterler tanımsız rolle değerlendirilemez): (kural, tetikleyici) başına TEK olay, `npc` boş.
- **Aday frenleri (eylemler):** `AdayUretici`'de hesaplanır (adım 5 saf kalır), olaylar 6.1'de NPC kimlik sırasıyla yazılır. Elenme nedeni karar izinde her zaman görünür; fren OLAYI yalnızca ön koşullar (ve konuşmada konu) tutsaydı aday olacaksa yazılır.
- **Sapma (plan 5.10: "her fren tetiklendiğinde bir olay"):** aday frenleri aynı (fren, eylem, NPC, hedef) için son tetiklemeden sonra yalnızca ilk engellemede yazılır; engel kalkıp yeniden başlarsa bir kez daha (aksi halde cooldown'daki aday her tick olay seli yaratırdı, `karar_verildi` sorununun daha büyüğü).

### 2. Cooldown ve tetiklenme sınırları

Kural: `cooldown` (tick), `kapsam` (`kural` | `npc`, varsayılan `npc`), `tick_basina_en_fazla`, `en_fazla`. Eylem: `cooldown`, `kapsam` (`npc` | `hedef`, varsayılan `hedef`), `en_fazla`.
"Tetiklendi": kural için tüm kriterler tuttu ve en az bir etki üretildi; eylem için karar başına bir (konuşmada çoklu `anlatti`, hareketin `ayrildi` + `geldi` olayları tek sayılır). `cooldown: 24` ve t=10'da çalışan kural t=33'te kapalı, t=34'te açıktır.
`kapsam`, `cooldown` ve `en_fazla` için geçerlidir; `tick_basina_en_fazla` her zaman kural genelinde sayılır (plan 5.10) ve tick içi sayaçtır (kanonik metne girmez).

- **Sayaçlar hafızadan okunmaz, yalnızca olay günlüğünden türer** (`TetikSayaclari.Isle`, aynı günlük aynı sayaç); ayrı `kural_tetiklendi` olayı yoktur. Adım içinde hemen güncellenirler (sıra: tetikleyici → kural → NPC); bu, "tek durum görüntüsü" ilkesinin belgeli istisnasıdır, çünkü değer dünya durumu değil tetiklenme kaydıdır. Kanonik metne yalnızca `cooldown` ya da `en_fazla` tanımlı kural/eylem için `SAYAC|` satırı girer.
- **K1 (olay biçimi):** `gercek` olayında NPC bilgisi yoktur; sınır alanı olan kuralın `gercek_eklendi/kaldirildi` olayına `Yuk` içinde `ben` anahtarı eklenir (`Olay` tipi değişmez; fren alanı olmayan kuralların olayları ve hash'ler etkilenmez, testle korunur).
- **Genel sınır M:** `frenler.tick_basina_en_fazla_sonuc`, yazılmadıysa `FrenAyarlari` varsayılanı `max(32, 8 × NPC sayısı)`. Yalnızca sonuç kurallarının çıktısını sayar, kuralın etkileri bölünmez, sınırı aşan tetikleme düşer ve fren olayı yazılır (ertelenmez). 3 golden'da tick başına en çok 6, 8 NPC fikstüründe 13 çıktı ölçüldü; hiçbirinde tetiklenmez.
- **Sapma (plan: "kural başına, isteğe bağlı olarak (kural, NPC) çifti"):** `kapsam` varsayılanı `npc`'dir (plandaki okumada varsayılan `kural`); kurallar algılayan NPC başına değerlendirildiği için daha doğal kabul edildi.

### 3. Zincir derinliği (Yusuf: Seçenek 1)

Derinlik günlükten türer (`Olay` tipi değişmez): kural çıktısı `1 + nedenlerindeki en büyük derinlik`; algı olayları derinlik eklemez; oyuncu, NPC eylemi, kriz ve senaryo olayları 0 başlar. D (varsayılan 4, `FrenAyarlari`) aşılacaksa kural çalışmaz ve `zincir` freni yazılır.
**Sapma (plan 5.10/6: "kalan etkiler bir sonraki tick'e ertelenir"):** etki ertelenmez, düşer. 4. haftadaki kaskad v0 zaten her kural çıktısını bir sonraki tick'e bıraktığı için aynı tick'te derinlik hep 1'dir; derinlik tick'ler arası ölçülür. Seçenek 2 (gerçek tick içi kaskad) üç golden'ı birden değiştirirdi, Seçenek 3 (D yok) rule→rule döngüsünü cooldown'sız sonsuza bırakırdı.

### 4. Korumalı bayraklar

Senaryo `korumali` (kapalı küme: `oldurulemez`, `calinamaz`, `mekandan_ayrilamaz`), `oyuncu_ihlalleri`; eylem ve kural `ihlal_bayraklari: [{bayrak, varlik}]` (`varlik`: bağlam rolü ya da sabit kimlik). Filtre `AdayUretici`'de ön koşullardan ÖNCE çalışır: ihlal eden aday `adaylar`'a hiç girmez, nedeni elenenler listesine ve karar izine yazılır. İhlal eden kural çalışmaz ve `korumali` freni yazılır. `Submit` `oyuncu_ihlalleri` ihlallerini reddeder.
`mekandan_ayrilamaz` yerleşik harekete ve korumalı varlığın `konum!` gerçeğinin yazımına motor tarafından uygulanır (yazar bildirmez).
**Kapsam sınırı (açıkça):** `oldurulemez` ve `calinamaz` YALNIZCA `ihlal_bayraklari` bildiren eylem ve kurallarda uygulanır. Yerleşik öldürme/çalma eylemi yoktur; bildirmeyi unutan yazarı motor yakalayamaz. CsCheck korumalılık değişmezi bu kapsamdadır.

### 5. Yay değişkenleri

Senaryo `yay_degiskenleri` (`baslangic`, `min`, `max`); etki `{tur: yay, degisken, delta}` (kural ve eylem); `yay(ad)` kural kriterinde ve eylem ön koşul/ölçütünde okunur (kriz satırında kriter ve etki reddedilir); sözlük 18 fonksiyon kalır. `yay_degisti` (algılanmaz, kural tetiklemez) kırpılmış yeni değeri taşır; kırpılırsa `yay_siniri` freni yazılır. Değişkenler kuralların aynı adımda tuttuğu tek görüntüden okunur; birden çok delta sabit sırayla toplanır. Bilinmeyen değişken `Olustur`'da reddedilir; tanımlı değişken yoksa kanonik metne satır girmez.

### 6. Kayıt sürüm 2

`OturumKaydi` sürüm 2: `gunluk_hash` (bütün olay günlüğünün kanonik hash'i; `karar_verildi` ve fren olayları dahil; `son_hash` hesabı DEĞİŞMEZ, seçici/karar izi değişikliği replay'de yakalanır), `senaryo_hash` (senaryo, kural, eylem, kriz dosyalarının metinleri; CRLF/LF aynı), `bekleyen_eylemler` (Submit edilmiş ama Tick görmemiş eylemler; yüklemede aynı sırayla geri Submit edilir). Sürüm 1 okunur, denetim atlanır. `OturumOynatici.Oynat` tek yükleme yoludur; çekirdek dosya okumaz, CLI metin verir.
CLI: `yanki devam <kayit.json> [--eylem ...] [--tick M] --kaydet <yeni.json>`; çıkış kodları 5 (`gunluk_hash`), 6 (`senaryo_hash`). `replay --guncelle`/`--hash-ekle` senaryo değişikliğini bilerek kabul eder. Özel ayarlar (`ZihinAyarlari`, `IActionSelector` vb.) kayda girmez; v0.1'de varsayılan seçici için garanti verilir.

### 7. Kaynak modeli (ADR-012 madde 4'ü değiştirir)

Her (önerme, kök, kaynak) için ayrı `duydu` kanıtı tutulur, hiçbiri silinmez; aynı (kök, kaynak) tekrarı güncel güçle karşılaştırılır. Güvende her kökten yalnızca en güçlü güncel kanıt noisy-or'a girer; `kaynak(desen, kisi)`, `KimdenDuydu` ve geri anlatmama hepsini görür; aktarım gücü ve dayanak yine en güçlü tek kanıttandır.
**ADR-012 madde 4 ile çelişen yerler:** (a) "aynı kökten tekrar kanıtı tek tutulur, kaynak/alanlar yenilenir" kalktı: aynı kökten FARKLI kaynak ayrı kanıttır, eski kaynak silinmez (Can söylentiyi Ayşe'den de duyduğunu unutmaz); (b) geri anlatmama yalnızca en güçlü kaynağı değil bütün kaynakları kapsar. Yankı freninin amacı (A→B→C→A'da güven artmaz, kökü kendisi olan söylenti kanıt yazmaz) korunur. ADR-012 silinmedi, durumu değiştirilmedi.

### 8. Sonradan eklenenler (8. hafta; kod incelemesi maddeleri)

- **Korumalı varlığın `konum!`'unu yazan eylem (Minor 4, Yusuf'un kararı: seçenek a):** eylem aday aşamasında TAMAMEN düşer (`AdayUretici`); önceden yalnızca o etki düşüyor, eylem olayı ve diğer etkiler uygulanıyordu. Şablon aday aşamasında `ben`/`oyuncu`/`eylem.hedef` ile statik çözülür; nedeni karar izinde ve `korumali` fren olayında görünür. Dört golden aynı kaldı. **Sınır:** kriz satırının etkisi aynı ihlali yaparsa kriz seçilmiş eylem olduğundan yalnızca o etki düşer (kriz satırı aday değildir); `docs/authoring-guide.md`'de belgeli.
- **Kural ve eylem kimliği çakışması (Minor 5):** fren tekilleştirme anahtarı (`tür|kimlik|npc|hedef`) kural ve eylemi ayırmıyor; çakışma eylemin aday frenini bastırabilirdi. Olay biçimi değişmesin diye `Simulasyon.Olustur` aynı kimlikli kural ve eylemi reddeder.
- **`IhlalBayragi.Coz` desteklenmeyen rolde açık hata:** `oyuncu`, `konusan`, `dinleyen` rolleri hiçbir yükleyiciden gelmez (doğrulayıcı reddeder) ama elle kurulan nesnede boş kimlik (ya da `konusan` gibi sabit kimlik) dönüp korumayı SESSİZCE kapatıyordu; artık `InvalidOperationException`.
- **`yay(ad)` kaynakları (Minor 3):** karar izi, eksen gibi birikmiş bir değer olan `yay`'ın son `yay_degisti` olayını kaynak gösterir ve birikimi `kaynaksiz` notuyla raporlar; hiç değişmediyse kaynak uydurulmaz.
- **Hash sınırı (Minor 6):** `tanimsiz_rol` ve `kisi_olmayan_hedef` frenleri `cooldown`/`en_fazla` gibi fren alanı olmayan senaryolarda da yazılır; olay kimliklerini kaydırır ve `son_hash`'i değiştirir. "Fren alanı yoksa hash aynı" genel bir değişmez değildir; dört golden'da bu iki fren tetiklenmez (ölçüldü).
- **`korumali` anahtarları (Minor 8, doğrulandı ve kapatıldı):** `oldurulemez`/`mekandan_ayrilamaz` anahtarı NPC ya da oyuncu olmak zorundadır (yazım hatası `Olustur`'da reddedilir); `calinamaz` eşya anahtarı yalnızca `korumali`'de bildirilir ve bir `ihlal_bayraklari` sabit kimliğiyle uyuşmazsa reddedilir. Hiçbir `ihlal_bayraklari` tarafından kullanılmayan bir `calinamaz` bildirimi etkisizdir (korunacak bir eylem yoktur) ve yazım hatası olsa da sessiz kalabilir; bu bilinçli bir sınırdır: daha sıkı doğrulama (atıfsız bildirimi reddetmek) örneklerin ve testlerin yaptığı meşru bildirimleri de kırardı.

## Değerlendirilen seçenekler

|                         | Seçilen                                                   | Reddedilen                                                                                |
| ----------------------- | --------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| Zincir derinliği        | Tick'ler arası, etki düşer (Seçenek 1)                    | Tick içi kaskad (3 golden birden değişir); D yok (döngü sonsuza gider)                    |
| Kaynak bilgisi          | Kanıt başına kaynak, kök başına tek güven (Seçenek 1)     | Tek kanıt + `EkKaynaklar` ("ne zaman, ne güçle" yalnızca güçlü kaynak için bilinir)       |
| Sayaç                   | Günlükten türeyen katlama                                 | Hafızadan okuma; ayrı `kural_tetiklendi` olayı                                            |
| Hash önbelleği (Z3)     | Zihin başına önbellekli metin + tam hesapla karşılaştırma | Tembel (yalnızca istenince) hash: "her tick hash" için ayrı mod gerekir                   |
| HanGerilim `hafiza_var` | Kalır (krizden sonra bekleme ≠ eylemin tekrar sınırı)     | Cooldown'a çevirmek: krizler kaybolur ya da sürekli tekrar eder (`docs/notes/frenler.md`) |
| Gossip ön filtresi      | Denendi, kazanç yok, GERİ ALINDI                          | —                                                                                         |

## Ödünleşimler

- Fren olayları günlüğü büyütür. Gerçekçi 50 NPC fikstüründe `fren_uygulandi` olayların %4.1'idir (13459), çünkü KURAL frenleri engellenen her tetiklemede yazılır (ör. her tanık için `cooldown`'lı bir kural). Aday frenleri tekilleştirilir, kural frenleri değil (plan 5.10'a uyum); gerekirse kural frenleri için aynı tekilleştirme ayrı bir kararla eklenir.
- Zincir derinliği `D` aşıldığında etki düşer; ertelemek kuyruğu büyütürdü.
- `kapsam` varsayılanları (`npc`/`hedef`) yazarın bilmesi gereken bir ayrıntıdır (`docs/authoring-guide.md`).
- Kayıt yalnızca varsayılan ayarlar ve seçiciyle garanti verir.

## Performans ve 5 ms hedefi

Plan 3'teki "50 NPC'lik senaryoda tick başına ortalama 5 ms'nin altında" kriteri SİLİNMEZ. Durum (7. hafta sonu, `tools/olcum`, benchmark değil):

- **8 NPC: tutuldu** (stres fikstürü 2.4 ms dedikodusuz / 4.1 ms dedikodulu).
- **50 NPC: tutmadı.** Stres fikstürü: 35.8 ms dedikodusuz (≈ 7×), 186 ms dedikodulu (≈ 37×). Gerçekçi fikstür (8 mekan, 9 eylem, 12 kural, dedikodu açık, NPC'ler çoğu tick boşta): ölçümler `docs/notes/frenler.md`'de; gerçekçi fikstürde 8 NPC 7. hafta sonunda 6.6 ms ölçüldü (5 ms'nin üstünde). **8. hafta düzeltmesi:** bu sayı çift modlu bir dağılımın hızlı kuyruğundaydı ve çelişki dizini (`cfd02b3`) sonrası 8 NPC gerçekçi fikstür 2.88 ms (ayrıntı ve koşullar ADR-015). Kalan farklar notta sayılarla yazılıdır.
- **Karar 8. haftada verildi ([ADR-015](0015-5ms-kriteri.md)):** v0.1 kriteri 8 NPC ortalama ≤ 5 ms, 50 NPC v0.2 hedefi. (Burada önceden "12. haftadaki BenchmarkDotNet ölçümünde" yazıyordu; bu yanlıştı: karar 12. haftaya bırakılmadı, BenchmarkDotNet tablosu plana göre 11. haftadadır.) Blazor WASM'de tick süresi ve yükleme ölçülmedi (10. hafta açık maddesi).
- Z3 (zihin kanonik metin önbelleği) uygulandı. Gossip önerme dizini/yüklem ön filtresi denendi, dedikodulu 50 NPC'de kazanç sağlamadı (≈ 171–180 → ≈ 193–198 ms), geri alındı. Z4 (kanıt+hafıza birleştirme, önem eşiği/silik hafıza) 11. hafta adayıdır. (8. hafta: gossip denemesi yanlış katmana bakıyordu; asıl maliyet `Zihin.Inanilan` taramasıydı, çelişki dizini ile giderildi, bkz. ADR-015.)

## Gerekçe (Yusuf)

> **Devir notu:** Bu bölümü Claude Code, Yusuf'un 7. hafta görev metnindeki devriyle ("ADR-013 'Gerekçe (Yusuf)' bölümünü bu görev metninin deviriyle yaz. Plandan ve kararlarımdan derle") yazdı. Plandan ve Yusuf'un görev/onay mesajlarındaki kararlarından derlenmiştir; Yusuf'un yazmadığı bir gerekçe uydurulmamıştır. Yusuf onaylayana kadar ADR "Önerildi" kalır.

Plan 5.10 frenleri "Dwarf Fortress'ın sarmalları, Oblivion'ın görev kıran NPC'leri" riskine karşı koyar ve her freni bir olay yapar ki "bu neden olmadı?" sorusu da cevaplanabilsin; bu yüzden fren bir günlük olayıdır, sayaçlar ayrı durum değil günlüğün katlamasıdır (Yusuf'un kararı: sayaçlar hafızadan okunmaz, ayrı olay yok).
Aday frenlerinin tekilleştirilmesi ve fren olayının yalnızca ön koşullar tutsaydı yazılması Yusuf'un düzeltmeleridir: aday elenmesi her tick tekrarladığı için aksi halde olay seli çıkardı; elenme nedeni karar izinde yine görünür.
Zincir derinliğinde Yusuf Seçenek 1'i seçti ve "ertelenir" yerine etkinin düşmesini ADR'ye sapma olarak yazdırdı (kaskad v0 zaten her çıktıyı ertelediği için tick-içi derinlik anlamsızdı). Korumalı bayraklarda `oldurulemez`/`calinamaz`'ın yalnızca bildirilen eylemlerde uygulandığı sınırı Yusuf açıkça yazdırdı; yerleşik öldürme eylemi eklenmedi.
Yay değişkenlerinin ve fren olaylarının zihin olayı sayılmaması (algılanmaz, kural tetiklemez) sarmal açmasın diyedir. Kaynak modelinde Yusuf Seçenek 1'i seçti: plan "kanıtlar silinmez ve kimden duydu birinci sınıf sorgu" der, bu yüzden güven kök başına tek sayılır ama kaynaklar silinmez. Kayıtta `gunluk_hash` Yusuf'un önerdiği gibi `son_hash`'i değiştirmeden seçici değişikliğini yakalar; `cooldown_kapsam` adı `kapsam` olarak, `tick_basina_en_fazla` kural geneli sayılacak biçimde Yusuf'un düzeltmesiyle değişti.
5 ms hedefi planda sayısal bir başarı kriteridir; Yusuf kriterin silinmemesini, 8 NPC'nin tutulduğunun, 50 NPC'nin tutmadığının ve kalan farkın açıkça yazılmasını istedi (7. hafta sonunda kararın ölçüme bırakılmasını da istemişti; karar 8. haftada ADR-015 ile verildi).

## Sonuçlar

- Kolaylaşan: "bu neden olmadı?" sorusu `yanki why <fren olayı>` ile; cooldown/sınır/korumalı varlık/yay ile hikâye yayını sarmal açmadan yönlendirmek; kayıt/yüklemenin kesintisiz koşuyla birebir aynı olması (testli); "kimden duydu" sorgusunun doğru kalması.
- Zorlaşan: fren olayı hacmi (özellikle kural frenleri); `kapsam` ayrıntısı; `oldurulemez`/`calinamaz`'ın yazarın bildirmesine bağımlı olması; 50 NPC performansı (v0.2 hedefi, ADR-015).
- Yeniden bakılacak: kural frenleri için tekilleştirme; gerçekçi fikstürdeki fren alanlarının maliyeti (aynı oturumda aynı kodda yeniden ölçüldü, 8 NPC, ortanca: frenli 5.80 ms, fren alanları çıkarılmış 2.38 ms; iki varyant aynı işi yapmaz, kararlar 1063 ve 1344, frenli 1156 fren olayı ekler; önceki 3.7 ms'lik rakamın koşulu kayıtlı değildi ve 6.6 ms ile karşılaştırılamaz, bkz. `docs/notes/frenler.md`); Z4 (11. hafta adayı); Blazor WASM ölçümü (10. hafta). 5 ms kararı 8. haftada ADR-015 ile verildi.

## Yapılacaklar

1. [x] Fren olayı, cooldown/sınırlar, zincir derinliği, korumalı bayraklar, yay değişkenleri, kayıt sürüm 2, kaynak modeli, Z3, CsCheck değişmezleri, golden #4 (7. hafta).
2. [ ] Gerçekçi fikstürde fren makinesi maliyetinin profili ve kural frenleri için tekilleştirme kararı (11. hafta).
3. [ ] Z4: kanıt + hafıza birleştirme / önem eşiği / silik hafıza (11. hafta adayı, olay biçimini değiştirir, Yusuf'un kararı).
4. [ ] Blazor WASM'de tick süresi ve yükleme ölçümü (10. hafta). [x] 5 ms kriteri kararı 8. haftada verildi (ADR-015); BenchmarkDotNet tablosu 11. hafta.
5. [ ] Yusuf gerekçeyi onaylayınca durum "Kabul edildi" yapılır.
