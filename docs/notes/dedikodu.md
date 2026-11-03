# Not: Dedikodu (anlatma, yalan, konu seçimi)

> **Yazar: Claude Code** (Yusuf'un isteğiyle). **Karar (Yusuf)** satırları Yusuf'un kararlarıdır. Karar kaydı: ADR-012.

## Ne yapıyor?

NPC'ler ve oyuncu bir önerme anlatır; dinleyene `duydu` kanıtı düşer; dinleyen aynı önermeyi başkasına aktarır, güç her adımda azalır; bir sonuç kuralı fikri değiştirir; `yanki why` zinciri ilk anlatana kadar iner.

- **Konuşma eylemi** `aktarim: dogru | yalan` alanı olan bir eylemdir (kod değiştirmeden yazılır, bkz. `docs/authoring-guide.md`). Seçilince adım 6.1'de **her konu için ayrı** bir `anlatti` olayı yazılır
  (`Fail` konuşan, `Hedef` dinleyen, yük: `onerme`, `kok`, `gorunurluk`; `NedenOlaylar`: karar olayı + konuşanın o önermeye dayanak kanıt olayı).
- **Algı (6.2)** mevcut doğrudan algıdır: `anlatti` varsayılan `sessiz`dir, yalnızca konuşan ve dinleyen `anlatti(A,B)` gözlemini ve hafızayı alır. `gorunurluk: acik` ise tanık konuşmanın OLDUĞUNU görür, içeriğini duymaz (kulak misafirliği yok).
- **Yazım (6.3):** dinleyene `duydu` kanıtı (kaynak: konuşan, kök, güç), konuşana aktarım kaydı (`aktarim_kaydedildi`). Dinleyen oyuncuysa kanıt yazılmaz (oyuncunun zihni yok).
- **Oyuncu anlatımı:** `PlayerAction("anlatti", hedef, onerme[, gorunurluk])`; `Submit` hedefin oyuncuyla aynı mekanda olmasını ve önermenin geçerliliğini denetler; CLI: `--eylem "anlatti:Ayse:onerme=borclu(Mehmet,Ayse,500)"`.

## Konu seçimi (`KonuHesabi`, tek sınıf)

`puan = onem × yenilik × ilgi` (ayrıntı ADR-012 §3). Karar (aday elenmesi, `konu_puani`) ve aktarım aynı sınıfı kullanır; sonuç aday aşamasında bir kez hesaplanıp seçilen adaya taşınır. Konu sınırı `n = 1 + ⌊sosyallik × 2⌋` (+1 `yakin`/`dost`),
konular puana göre azalan, eşitlikte önerme metni ordinal. Eşik: puan 0.1'in altındaki konu anlatılmaz; konu kalmazsa aday elenir ve neden izde görünür (`konu yok (önerme: konu puani 0.05 < 0.1; ...)`). Karar izi aday başına `konu_puani` ve `konular` (önerme, puan, önem, yenilik, ilgi) gösterir.

**Konuşan, dinleyenin zihnini okumaz.** Karar yalnızca konuşanın zihninden beslenir (testle korunur: dinleyen ne kadar güvensiz olursa olsun konuşanın kararı ve anlatım olayları aynıdır); dinleyenin zihni yalnızca yazılacak kanıtın gücü için, adım 5'te aynı durum görüntüsünden okunur.

## Güç, kök ve yankı freni

`yeni_guc = kanit_gucu × min(0.95, 0.7 + 0.003 × güven)`; `kanit_gucu` konuşanın o önerme için **en güçlü tek güncel kanıtı** (eşitlikte en küçük olay kimliği), `güven` dinleyenin konuşana duyduğu `guven` ekseni. Nötr güvende her adım ×0.7:
oyuncu 1.0 → Ayşe 0.7 → Can 0.49 → Zeki 0.343 → ... ; 0.15'in altı yazılmaz (söylenti ölür, ama anlatım olayı ve aktarım kaydı kalır). Sonuç kaynak kanıtın gücünü geçemez (testle).
Kanıt kök taşır (`kanit_eklendi.kok`): aynı önerme için aynı kökten `duydu` varsa yeni kanıt eklenmez, güncel (sönmüş) güçle karşılaştırılır: yeni kanıt daha güçlüyse bütün alanları (`Kaynak`, `Guc`, `Tick`, `OlayId`) yeni kanıttan gelir, değilse mevcut kanıt tamamen aynı kalır (zayıf tekrar sönümü sıfırlayamaz); kökü zihin sahibi olan kanıt hiç yazılmaz. A→B→C→A'da A'nın güveni artmaz. Konuşan, bir konuyu ondan kanıt aldığı ya da kökü olan kişiye geri anlatmaz; aynı önermeyi aynı kişiye iki kez anlatmaz (aktarım kaydından; cooldown değil).

## Yalan ve gizlilik

- Yalan, **konuşanın kendi inancına** göre tanımlanır: `yalan_onermeleri` şablonlarından (`{ben}`, `{eylem.hedef}`) konuşanın inandığı bir önermeyle çelişen ve bu kişiye söylenmemiş olanlar aday; çok varsa `AnahtarliRng.Icin(seed, tick, npc, "yalan")` seçer. Dünya gerçeği önemsizdir: yanlış inandığı şeyi `dogru` diye anlatan NPC yalancı sayılmaz.
- Dışarıdan doğru anlatımdan ayırt edilemez: aynı `anlatti` türü, aynı yük anahtarları, aynı `NedenOlaylar` sayısı (`[karar, dayanak]`), aynı algı önermesi. Yalanın gücü sabit değildir: çeliştiği inancın dayanak kanıtının gücü × güven çarpanıdır (güç farkı yalanı ele verirdi).
  Yalanın kökü konuşandır.
- Yalan bilgisi yalnızca konuşanın zihnindeki `aktarim_kaydedildi` olayındadır (yük `yalan: 1`), algılanmaz ve kural tetiklemez. `yalan_soyledi(kime)` bunu okur. `why` ve `query` çıktısı (`-- aktarımlar --`) bunu geliştiriciye gösterir; NPC bilgisi değildir.
- `olay_konusu(desen)` (yalnızca kural bağlamı) kuralı içeriği duyan dinleyen için tetikler; konuşan ve tanık için yanlıştır.

## Karar (Yusuf, 6. hafta)

- **Karar (Yusuf):** `konu_puani` yalnızca `aktarim: dogru` eylemlerinde; konu havuzu eylemin `konu_desenleri` ile (joker yüklem yok, `anlatti` konu olamaz); doğru anlatım da `anlatti` olay türünde; güç = en güçlü tek kanıt × `min(0.95, 0.7 + 0.003 × güven)`; yalanın dayanağı ve gücü çeliştiği inancın kanıtından; yalanın kökü konuşan; konuşan kendisine kanıt veren kişiye geri anlatmaz.
- **Karar (Yusuf):** duyulan konunun önemi varsayılan olarak 0.5'e iner; yazar bunu `anlatti` kuralındaki `hafiza` etkisiyle yükseltir. **11. hafta denge turunda ölçülecek.**

## Ölçüm (benchmark DEĞİL): 50 NPC, 168 tick, stres fikstürü

`tools/olcum --npc 50 --dedikodu` (sahte içerik, Kasaba Hanı değil; her NPC her tick eylem seçer; tek makine, tek çalıştırma, 20 işlemci, .NET 10). Ölçüm öncesi değer 82 ms/tick idi.

| ölçü                             | dedikodusuz (3 çalıştırma ortancası)        | dedikodulu (`anlat` tabanı 3, 1 çalıştırma) |
| -------------------------------- | ------------------------------------------- | ------------------------------------------- |
| tick başına süre                 | 79.5 ms                                     | **3105.6 ms**                               |
| olay sayısı                      | 292 041                                     | 270 804                                     |
| `kanit_eklendi` toplamı          | 130 484 (hepsi `gordu`)                     | 120 233 (gordu 119 222, duydu 1 011)        |
| `anlatti` / `aktarim_kaydedildi` | 0 / 0                                       | 1 011 / 1 007                               |
| `stres_degisti` / kriz olayı                            | (ölçülmedi; `tehdit` eylemi artık stres yazar) | 260 / 31                               |
| NPC başına en çok önerme         | (ölçülmedi)                                 | 347                                         |

Ayrıca `anlat` tabanı düşükken (hiç seçilmedi, yalnızca aday hesabı çalıştı) 603 ms/tick.

**Bulgu 1: bilgi patlaması yok (olay hacminde).** Dedikodu tüm kanıtların ≈%0.8'i (1 011 `duydu` / 120 233): kanıt ve olay hacmini algı belirler (`gordu` ≈%99; Z4, 7. hafta). `duydu` ile yayılan önerme sayısı oyuncunun tohumladığı 4'tür; yankı freni ve "geri anlatmama" kuralı aynı söylentiyi aynı kişiye iki kez yazmıyor (testle).
**Bulgu 2: tick süresi 79.5 → 3105.6 ms (≈40×); düzeltildi, aşağıda.** Neden ölçümle bulundu: konu hesabı aday aşamasında her (NPC, dinleyen) çifti için çalışır, NPC başına önerme sayısı 347'ye çıktığı için (algıdan gelen `gordu` kanıtları) (a) konuşanın tüm önermelerinin parse edilmesi ve (b) "ilgi" için inanılan önermelerin listesinin (her biri `Inanilan` = tüm önermelerle çelişki taraması, O(n²)) her dinleyen için yeniden kurulması baskın maliyettir.
Bir deneyle (geri alındı, kodda yok): inanılan listesini konuşan başına tick'te bir kez hesaplamak 3105 → 588 ms/tick'e indirdi (hâlâ ≈7×); kalan maliyet önermelerin tekrar parse edilmesi ve yalan şablonlarının her önermeyi taraması. **Düzeltme (Yusuf'un kararı, ayrı commit):** konuşan başına ayrıştırılmış önermeler, inanılan bayrakları, ham güven ve en güçlü kanıt tek tick içinde bir kez hesaplanıp (NPC, dinleyen) çiftleri için yeniden kullanılır (`KonuHesabi(..., onbellekli: true)`). Önbellek yalnızca adım 5'te kullanılır, her tick yeniden kurulur, zihin durumuna ve hash'e girmez; olay biçimine ve anlatılabilirlik kurallarına dokunulmadı. Testle: önbellekli ve önbelleksiz hesap aynı konuları ve puanları verir, sonuç NPC/önerme ekleme sırasından bağımsızdır. Olay sayısı ve son durum hash'i değişmedi (`2fc28bc53bba632b`, 270 804 olay).

| 50 NPC, dedikodulu | önce | sonra |
| --- | --- | --- |
| tick başına süre | 3105.6 ms | **523.1 ms** |

| 8 NPC | dedikodusuz | dedikodulu önce | dedikodulu sonra |
| --- | --- | --- | --- |
| tick başına süre (3 çalıştırma ortancası) | 2.52 ms | 6.87 ms | **4.62 ms** |

Kalan fark (50 NPC: 523.1 / 79.5 ≈ 6.6×) **7. hafta açık kararıdır** (Z3/Z4 ile birlikte): artık baskın maliyet konuşan başına O(n²) inanılan taraması (347 önerme) ve yalan şablonlarının her önermeyi taraması; seçenekler önerme dizini, yüklem ön filtresi, "ilgi" için bağ dizini.

## Açık

- **Kanıt tazelemesi (KAPANDI, Yusuf'un kararı):** aynı kökten tekrar artık güncel güçle karşılaştırılır; daha güçlüyse kanıtın bütün alanları yeni olaydan gelir, değilse mevcut kanıt (Tick ve OlayId dahil) aynen kalır, kök değişmez. Zayıf tekrar sönümü sıfırlayamaz, alanlar iki olaydan karışmaz. `han-gerilim-1` hash'i bu yüzden `2f778d4f93ea7b0b` → `690f2f4ee9d3d385` oldu (Yusuf onayladı; `HanGerilimTests` değişmezleri ve seed 42/7 sayıları korundu).
- **Kaynak bilgisi (7. hafta, G Seçenek 1, KAPANDI; Yusuf onayı koşulluydu, doğrulandı):** her (önerme, kök, kaynak) ayrı `duydu` kanıtıdır, hiçbiri silinmez. Aynı (kök, kaynak) tekrarı güncel (sönmüş) güçle karşılaştırılır. Güven her kökün en güçlü güncel kanıtından noisy-or ile hesaplanır; `gordu`/çıkarım kanıtları tek tek. Aktarım gücü, kök ve dayanak yine en güçlü tek kanıttan gelir. `kimden_duydu`, `kaynak(...)` ve geri anlatmama bütün kaynakları görür. Değişmez (`HanGerilimTests`, seed 42 ve 7, her tick, her NPC, her önerme): `güven = 1 − Π(gordu/çıkarım) × Π(kök başına en güçlü)`. Etki: `han-gerilim-1` `690f2f4ee9d3d385` → `d98b50f39fc358dd` (seed 7: `4bf5ab4e4786f4de` → `fb0210738ec3d335`). **İlk ayrışan olay:** #253 (tick 4, Ayşe'nin `karar_verildi`'si). Ayşe'nin Hatice'den t2'de duyduğu `borclu(Mehmet,Ayse,500)` kanıtı (#183) eskiden güçsüz olduğu için atılırdı; artık kaynak olarak durduğundan "kanıt dinleyenden geldi" kuralıyla bu konu Hatice'ye anlatılamıyor, Hatice adayının konu puanı 0.2426 → 0.1213 düşüp havuzdan çıkıyor (eski havuz Hatice+Kemal 0.8319 eşit, yeni havuz yalnızca Kemal). Sonrası bu kolun sıradan sonucu: tick 6'da Hatice'nin yalanı (aynı zar 0.449618, farklı havuz) eskiden Ayşe'ye, şimdi Can'a gidiyor; `borclu(Selim,Hatice,200)` ile `degil(...)` güvenleri 0.4853/0.4865 olduğundan yalanı alan "inanılmıyor" oluyor (eskiden Ayşe inanılmıyordu, Can inanıyordu; şimdi tersi). Gerçek `inanilan` dönüşü yalnızca bu iki NPC×önerme çiftidir; geri kalan farklar (itiraf kurbanı Ayşe→Can, `anlatti(...)` önermeleri) farklı konuşmaların sonucudur. `kardes(Mehmet,Can)` Hatice için 0.4679 → 0.4691: eskiden tek kanıt Ayşe t4 (0.4879, 20 tick sönmüş), şimdi üç kaynak (Can t7, Ayşe t8 0.4851, Kemal t9) ve güven yalnızca en güçlüsü olan Ayşe t8'in güncel gücüne eşit (0.4851 − 16×0.001 = 0.4691): daha taze kanıt, çift sayım değil.
- **Yedek kriz satırı hedefsiz olmalı:** kriterli olmayan ama hedefli satır yedek sayılmaz (doğrulayıcı hata verir), çünkü hedefi bulunamayan satır elenir ve yedek elenemez.

- **Yalanın kanıt çelişkisinden otomatik yakalanması yok.** Yalanı yakalamak yalnızca yazarın kurallarıyla olur (ör. başka kaynaktan çelişen kanıt gelince `kaynak(...)` ve `inanc(...)` ölçütleriyle) ya da itiraf krizinde. İtiraf kurbanın inancını düzeltmez (açık karar).
- Duyulan konunun önemi varsayılan 0.5 ve 168 tick'te yenilik sıfırlanır; denge turunda ölçülecek (11. hafta).
- Hedef türü `npc` ya da `oyuncu`dur; hem NPC hem oyuncuya anlatabilmek için iki eylem yazılır (`anlat`, `anlat_oyuncuya`).
- İki NPC aynı tick'te aynı dinleyene aynı kökü yazarsa sonuç NPC kimlik sırasına bağlıdır (girdi sırasına değil).
- Cooldown, tetiklenme ve zincir derinliği sınırları, fren olayları 7. haftada.
