# ADR-0012: Dedikodu, yalan gizliliği, aktarım kaydı, konu seçimi, stres ve kriz

**Durum:** Önerildi
**Tarih:** 2026-11-03
**Kabul eden:** —
**Kabul tarihi:** —

## Bağlam

6. hafta plan 5.3, 5.4 ve 5.5'teki mekanizmaları ekler: NPC'lerin ve oyuncunun önerme anlatması (`duydu` kanıtı, aktarımda gücün azalması, konu sınırı), yalan, kişiliğe aykırı eylemin stresi ve eşikte kriz.
   ADR-001 (olay kaynaklı çekirdek), ADR-002 (LLM karar vermez), ADR-003 (tek kriter dili), ADR-007 (`decimal`), ADR-008 (zihin olaylardan türer), ADR-010 (tüm kriterler tutmalı) ve ADR-011 (karar modeli) bağlayıcıdır.
   `Olay` tipine yeni alan eklenmez. Bu ADR yeni kararları, Yusuf'un 6. hafta onaylarını ve plana göre yapılan sapmaları tek yerde toplar.

## Karar

### 1. Yalan gizliliği

Doğru anlatım da yalan da dışarıya **aynı** `anlatti` olay türü ve aynı yük biçimiyle (`onerme`, `kok`, `gorunurluk`) çıkar. Dış olayda, yükte, algı önermesinde (`anlatti(fail,hedef)`; içerik yok) ve kural girdisinde yalan olduğunu belli eden bir iz yoktur.
Yalan bilgisi yalnızca konuşanın zihnine yazılan, algılanmayan ve kural tetiklemeyen bir zihin olayındadır (aktarım kaydı). Yalan **konuşanın kendi inancına** göre tanımlanır, dünya gerçeğine göre değil: yanlış inandığı şeyi anlatan NPC yalancı sayılmaz.
Doğrulayıcı, `aktarim` alanı olan her eylemin (doğru ya da yalan) `olay_turu` değerinin `anlatti` olmasını zorunlu kılar (M2): doğru anlatım başka bir türle çıksaydı tür farkı yalanı ele verirdi. `anlatti` çekirdeğe ayrılmış bir olay türüdür; `aktarim` alanı olmayan eylem ve senaryo bu adı kullanamaz.
`why` ve geliştirici çıktısı yalanı `eylem:<id>` ve aktarım kaydından gösterebilir; bu NPC bilgisi değildir.

`NedenOlaylar` sayısı da gizliliğin parçasıdır (M6): her `anlatti` olayının nedenleri `[karar olayı, dayanak kanıt olayı]`dır. Yalanda dayanak, yalanın çeliştiği inancın en güçlü kanıtıdır (yoksa aday elenir); bu kanıt ayrıca aktarım gücünü belirler. Böylece nedenlerin sayısı ve yalanın gücü doğru anlatımdan ayrılmaz.

### 2. Aktarım kaydı (tek yeni zihin olayı)

`aktarim_kaydedildi`, yük: `npc` (konuşan), `kime`, `onerme`, `yalan` (0|1). Yalnızca konuşanın zihnine yazılır, algılanmaz ve kural tetiklemez. Kanonik metinde `T|kime|onerme|olayId|tick|yalan` satırı olarak yalnızca kayıt varken yazılır; kayıt yokken `KanonikMetin` 6. hafta öncesiyle birebir aynıdır (iki altın hash değişmedi).
`ZihinSorguCozucusu` `yalan_soyledi(kime)` bu kayıttan okur. Aynı kayıt F kararının (aynı önerme aynı kişiye iki kez anlatılmaz) kaynağıdır.

### 3. Konu seçimi (`KonuHesabi`, tek sınıf)

Karar (aday elenmesi, `konu_puani`) ve aktarım aynı sınıfı ve aynı sonucu kullanır; konu hesabı aday aşamasında bir kez yapılır (çünkü konu yoksa aday elenir), tick içi önbellek durum hash'ine girmez. Aday yapısı `(eylem, hedef)` kalır; `Aday.Konu` yalnızca sonucu taşır, kimliği değiştirmez.
Konu puanı `onem × yenilik × ilgi`:

- `onem`: dayanak kanıt olayının doğrudan nedeninin (algılanan ya da `anlatti` olayı) hafıza önemi, kayıt yoksa 0.5 (M4, tek adım). Duyulan konunun önemi varsayılan olarak 0.5'e iner; yazar bunu `anlatti` kuralındaki `hafiza` etkisiyle yükseltir (11. hafta denge turunda ölçülecek).
- `yenilik`: `1 − (tick − kanıt_tick) / 168`, alt sınır 0. `kanıt_tick`: dayanak kanıtın tick'i.
- `ilgi`: dinleyen önermede geçiyorsa 1; konuşanın inandığı başka bir önerme dinleyeni içerip bu önermeyle bir varlık paylaşıyorsa 0.7 (tek adım; sayı argümanları varlık sayılmaz); konuşan önermede geçiyorsa 0.5; hiçbiri değilse 0.25.
- Puanı 0.1'in altındaki konu anlatılmaz; konu kalmazsa aday elenir ve neden karar izinde görünür.

Konu havuzu eyleme özeldir (M1): `aktarim: dogru` eyleminde `konu_desenleri` zorunludur, yüklem açık yazılır (joker yüklem kabul edilmez) ve `anlatti` önermeleri desene uysa bile konu olamaz. Konu seçimi tamamen konuşanın zihninden beslenir; dinleyenin zihni yalnızca yazılacak kanıtın gücü için, kararı etkilemeden okunur.
Konuşan yalnızca inandığı ve ham güveni 0.3 ya da üstü olan önermeyi doğru diye anlatır. Konu sınırı `n = 1 + ⌊sosyallik × 2⌋`, konuşanın dinleyene fikir bandı `yakin` ya da `dost` ise +1; konular puana göre azalan, eşitlikte önerme metni ordinal sırayla seçilir. Her konu ayrı `anlatti` olayıdır; yalan tek konudur.

### 4. Aktarım gücü, kök ve yankı freni

Konuşan o önerme için **en güçlü tek güncel kanıtı** seçer (eşitlikte en küçük olay kimliği); güç, kök, kaynak ve `NedenOlaylar` dayanağı bu aynı kanıttan gelir (ham güven kullanılmaz).
`yeni_guc = kanit_gucu × min(0.95, 0.7 + 0.003 × güven)`, `güven` dinleyenin konuşana duyduğu `guven` eksenidir (fikir değil); sonuç kaynak kanıtın gücünü geçemez. Oyuncunun kendi kanıt gücü 1.0'dır (nötr güvende 0.7, plan 7'deki örnek). 0.15'in altındaysa dinleyene kanıt yazılmaz (söylenti ölür); anlatım olayı ve konuşanın aktarım kaydı yine yazılır. Güç adım 5'te aynı durum görüntüsünden sabitlenir.
`duydu` kanıtı kök taşır (`kanit_eklendi` yükünde isteğe bağlı `kok`; `gordu` için boş, etkin kökü sahibidir). Yalanın kökü konuşandır. Dinleyende aynı önerme için aynı kökten `duydu` kanıtı varsa yenisi eklenmez, mevcut tazelenir; kökü dinleyenin kendisi olan kanıt hiç yazılmaz (A→B→C→A'da A kendi söylentisine daha çok inanmaz). Konuşan, bir konuyu ondan kanıt aldığı ya da kökü olan kişiye geri anlatmaz.

### 5. Tick adımları

Adım 5: konuşma adayları (konu ve yalan seçimi dahil) bütün NPC'ler için aynı durum görüntüsünden hesaplanır; yalan seçimi `AnahtarliRng.Icin(seed, tick, npc, "yalan")` ile. Adım 6.1: seçilen konular kadar `anlatti` olayı; 6.2: mevcut algı akışı (`sessiz` varsayılan, `acik` ise tanık yalnızca konuşmayı görür, içeriği duymaz; kulak misafirliği yok); 6.3: dinleyene `duydu` kanıtı, konuşana aktarım kaydı, stres artışı, kriz etkileri ve stres düşüşü, en son `konum!`.
Sonuç kuralları NPC anlatımları için sıradaki tick'te çalışır (4. haftanın kaskad kararı); `olay_konusu(desen)` kuralın ancak içeriği duyan dinleyen için tutmasını sağlar.

### 6. Oyuncu anlatımı

Oyuncu `anlatti` türünde eylem gönderebilir (senaryoda bildirilmez): hedef aynı mekandaki (bekleyen hareketin varış mekanı) bir NPC, yük yalnızca geçerli `onerme` ve isteğe bağlı `gorunurluk` (`sessiz`|`acik`). `kok` motor tarafından yazılır. Olay adım 1'de, `duydu` kanıtı adım 2'de algıdan sonra yazılır; eylem günlüğüne girer ve replay'de aynen üretilir. Oyuncunun zihni yoktur: aktarım/yalan kaydı tutulmaz; dünya gerçeğine aykırı söylemesi dünyayı değiştirmez.

### 7. Stres

Eylem tanımında `stres_tabani` (0 < taban ≤ 100). Eylem seçilince `stres += stres_tabani × Σ(celisen özelliklerin kişilik değeri)` adım 6.3'te `stres_degisti` olayıyla yazılır (`KuralId: eylem:<id>`, nedeni eylemin dış olayı). `celisen_ozellikler` var ama `stres_tabani` yoksa doğrulayıcı reddeder (5. haftada yalnızca izde görünen alan artık stres yazar). Karar izi seçilenin `stres_artisi` değerini gösterir (yalnızca varsa). Sönüm sorgu anında hesaplanır (ADR-008); stres hiç eklenmeyen senaryoda hash değişmez.

### 8. Kriz

Eşikler 50 ve 80, kriz sonrası düşüş 30 ve 60 (`StresAyarlari`). Değişmez: her seviyede kriz sonrası stres en alt eşiğin altında kalır. Üst seviye dışındaki her `i` için `esik[i+1] − dusus[i] ≤ esik[0]`, en üst seviye için `100 − dusus[son] < esik[0]`; aksi halde kriz her tick yinelenir ve `Simulasyon.Olustur` ayarı reddeder (50/30 ve 80/60 geçer, 50/20 ve 80/20 reddedilir).
Tespit adım 4'te tek kuraldır (geçiş durumu tutulmaz): sönmüş güncel `stres(t) ≥ esik` ise en üst uygun seviyede o NPC için kriz satırı seçilir. Senaryo yüksek stresle başlarsa kriz ilk tick'te çıkar. Krizdeki NPC o tick eylem seçmez (`karar_verildi` yazılmaz). Kriz olayı seçilmiş eylem gibi adım 6 yolundan geçer: `KuralId: kriz:<id>`, adım 5 başı konum görüntüsüyle algı, satır etkileri ve stres düşüşü (nedeni kriz olayı) 6.3'te, sonuç kuralları sıradaki tick'te; `NedenOlaylar` NPC'nin en yeni 5 stres artış olayıdır, bulunamayan neden uydurulmaz.
Kriz tablosu senaryoda `kriz_dosyalari` ile bağlanır; satır: `id`, `seviye` (1..eşik sayısı), `olay_turu`, `hedef` (`yok`, `yalan_kurbani`, `en_yakin`, `en_soguk`), `kriterler`, `etkiler`. Kriterler ADR-010 gibi hepsi tutmalıdır; tutan satırlar arasında en özgül kazanır, eşitlikte `AnahtarliRng.Icin(seed, tick, npc, "kriz")` (mevcut `Secici`, aday başına bağlamla). Hedefi aynı mekanda olmayan satır elenir. Her seviye için kriterli olmayan (ve hedefsiz) bir yedek satır zorunludur. `yalan_kurbani`: konuşanın EN SON yalan söylediği kişi; `en_yakin`/`en_soguk`: aynı mekandaki (oyuncu dahil) en yüksek/en düşük fikirli kişi, eşitlikte ordinal. Tablo yoksa kriz üretilmez, stres yine çalışır.

### 9. Sözlük 16 → 18 (Yusuf'un kararı)

`konu_puani(kime)`: yalnızca `aktarim: dogru` eylemlerinin ön koşul ve ölçütlerinde geçerlidir (konu havuzu eyleme özel desenlerden gelir); başka bağlamda doğrulayıcı reddeder. `olay_konusu(desen)`: yalnızca sonuç kuralı bağlamında; olay `anlatti` ise, `ben` olayın hedefi (içeriği duyan dinleyen) ise ve önerme desene uyuyorsa doğru, konuşan ve tanık için yanlış. `yalan_soyledi(kime)` bağlandı; "bağlı değil" reddi yalnızca `yay` için kalır.

## Değerlendirilen seçenekler

|                    | Önerilen (seçildi)                                    | Alternatif                                                                                             |
| ------------------ | ----------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| Sözlük             | `konu_puani` + `olay_konusu`                          | `kaynak(desen, olay.fail)` ile yetinmek: yanlış konuda da tetiklenir, puan konunun ilginçliğini görmez |
| Aktarım gücü       | En güçlü tek kanıt × `min(0.95, 0.7 + 0.003 × güven)` | Ham güven: bağımsız kökler birikir, ama kök/dayanak/NedenOlaylar tek kanıta bağlanamaz                 |
| Konu havuzu        | Eylemin `konu_desenleri`                              | Kısıtsız (her inanılan önerme): algı olaylarının hacmiyle bilgi patlaması                              |
| Doğru anlatım türü | `anlatti` zorunlu                                     | Serbest tür: tür farkı yalanı ele verir                                                                |
| Yalan nedenleri    | `[karar, çelişen inancın dayanağı]`                   | Yalnızca `[karar]`: neden sayısı yalanı ele verir                                                      |
| Kriz tespiti       | `stres ≥ eşik` (geçiş durumu yok)                     | Artışta geçişi saklamak: ek durum, eşik sınırında kaçan krizler                                        |
| Yalan içeriği      | Yazar listesi `yalan_onermeleri`                      | Yalnızca `degil(...)` hali: iftira atamaz                                                              |

## Ödünleşimler

- Dedikodu hesabı yalnızca konuşanın zihnini okuduğu için dinleyenin gerçek tepkisi (ör. kuşku) karara girmez; bedeli, dinleyenin güveninin yalnızca yazılan gücü belirlemesi ve zincirde ölen söylentilerin konuşana yine de "anlatılmış" saydırmasıdır.
- Aktarım ve kriz mevcut adım 5/6 hattına takıldığı için ayrı kuyruk yoktur; ama NPC işleme sırası yerine NPC kimliği sırası kullanılır (iki NPC aynı dinleyene aynı kökü yazarsa son kimlik kazanır; girdi sırasına bağlı değildir).
- Maliyet: konu hesabı aday aşamasında yapıldığından NPC başına önerme sayısıyla ve dinleyen sayısıyla büyür. 50 NPC stres fikstüründe tick süresi dedikodusuz 79.5 ms, dedikodulu 3106 ms ölçüldü (ayrıntı `docs/notes/dedikodu.md`); optimizasyon bu hafta yazılmadı, 7. haftada Z3/Z4 ile birlikte ele alınır.
- Yalan, kanıt çelişkisinden otomatik yakalanmaz (bu hafta yalnızca itiraf krizi ve yazarın kuralları); itiraf kurbanın inancını düzeltmez.

## Gerekçe (Yusuf)

> Bu gerekçeyi Claude Code, Yusuf'un 6. hafta görev metnindeki devriyle, planın 5.3–5.5 bölümlerinden ve Yusuf'un onay mesajındaki kararlardan derledi. Yusuf onaylayana kadar ADR "Önerildi" kalır.

Plan 5.3 yalanı "yalanı söyleyen bunu bilir" diye tanımlar; bundan çıkan sonuç, yalanın bilgisinin yalnızca konuşanın zihninde durması ve dünyaya hiçbir şeyin yalan olduğunu söylememesidir. Bu yüzden Yusuf, doğru ve yalan anlatımın aynı olay türü ve aynı biçimle çıkmasını, yalanın konuşanın kendi inancına göre tanımlanmasını ve nedenlerin sayısına varana kadar ayırt edilemez kalmasını istedi: tür ya da neden sayısı farkı, kural yazarını (ve ileride diyalogu) yalanı "görmeye" iter.
Aktarım gücünde ham güven yerine en güçlü tek kanıtı seçmesi, `NedenOlaylar` zincirinin tek bir kanıta bağlanması ve `why`'ın ilk anlatana kadar inebilmesi içindir; güç, kök ve dayanak aynı kanıttan gelirse zincir ile sayı çelişmez. Yankı freni (kök) A→B→C→A'da söylentinin kendi kaynağını güçlendirmesini engeller. Konu havuzunun eyleme özel olması (`konu_desenleri`) algı olaylarının hacminin (olayların ≈%90'ı algı) dedikoduya sızmasını önler; sayılar denge alanı olarak ayarlarda kaldı.
Stres ve kriz plan 5.4'ün tutarlılık ilkesinin maliyet tarafıdır: kişiliğe aykırı davranış mümkündür ama pahalıdır ve hikâye üretir. Kriz tespitinin tek kurala (`stres ≥ eşik`) indirilmesi ek durum tutmamak, kriz sonrası düşüşün alt eşiğin altında kalması değişmezi ise aynı krizin her tick yinelenmemesi içindir.

## Sonuçlar

- Kolaylaşan: Kod değiştirmeden yeni konuşma eylemi, yalan, stres maliyeti ve kriz satırı yazmak; söylentinin `why` ile ilk anlatana, krizin stres artışlarına ve karar izine kadar izlenmesi; yalan ile doğru anlatımın dışarıdan ayrılamaması testle korunur.
- Zorlaşan: Konu hesabının maliyeti (önerme sayısı); kriz satırı kriterlerinin "hepsi tutmalı" olması (alternatif koşul için ayrı satır); itirafın kurbanın inancını düzeltmemesi.
- Yeniden bakılacak: Hesabın maliyeti ve algı hacmi (Z3/Z4, 7. hafta); cooldown ve zincir derinliği (7. hafta); yalanın kanıt çelişkisinden otomatik yakalanması ve itirafın inancı düzeltmesi (açık karar); duyulan konunun önemi 0.5'e inmesi (11. hafta denge turu).

## Yapılacaklar

1. [x] Aktarım kaydı, `yalan_soyledi`, kanıt kökü ve yankı freni, `KonuHesabi`, eylem şeması ve doğrulayıcı, sözlük 18, konuşma adımı, oyuncu anlatımı, stres, kriz tablosu, CLI (6. hafta).
2. [x] Dedikodulu ölçüm (`tools/olcum --dedikodu`) ve sonucun notlara yazılması (6. hafta).
3. [ ] Konu hesabının maliyeti, Z3 ve Z4 (7. hafta, Yusuf'un kararıyla).
4. [ ] Yusuf gerekçeyi onaylayınca durum "Kabul edildi" yapılır.
