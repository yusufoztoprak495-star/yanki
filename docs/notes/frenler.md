# Not: Frenler (7. hafta, TASLAK)

> **Yazar: Claude Code** (Yusuf'un isteğiyle). Bu not 7. hafta boyunca doldurulur; T16'da tamamlanır. **Karar (Yusuf)** satırları Yusuf'un kararlarıdır.

## Ne yapıyor?

Frenler kural ve eylem tekrarlarını sınırlar, korumalı varlıkları ihlalden korur, yay değişkenleriyle dramatik yayı yönlendirir; her fren günlükte `fren_uygulandi` olarak iz bırakır, böylece "bu neden olmadı?" sorusu `yanki why <fren olayı>` ile cevaplanır. Kararlar ve sapmalar: `docs/adr/0013-frenler-kayit-ve-kaynak-modeli.md`; yazım: `docs/authoring-guide.md`.

- **Fren olayı:** `fren`, `kimlik`, `npc`, `hedef`; `KuralId: fren:<tur>`; algılanmaz, kural tetiklemez. Kural frenleri engellenen her tetiklemede yazılır (`tanimsiz_rol`: (kural, tetikleyici) başına tek, `npc` boş); aday frenleri engel başına bir kez ve yalnızca ön koşullar tutsaydı.
- **Sayaçlar** (`cooldown`, `kapsam`, `tick_basina_en_fazla`, `en_fazla`, genel sınır M) hafızadan okunmaz, olay günlüğünden türer; adım içinde hemen güncellenir (tek durum görüntüsünün belgeli istisnası).
- **Zincir derinliği** günlükten türer; D=4 aşılırsa kural çalışmaz ve `zincir` freni yazılır (etki düşer, ertelenmez).
- **Korumalı bayraklar:** `oldurulemez` ve `calinamaz` YALNIZCA `ihlal_bayraklari` bildiren eylem ve kurallarda uygulanır; `mekandan_ayrilamaz` yerleşik harekete ve `konum!` yazımına motor tarafından uygulanır.
- **Yay değişkenleri:** `yay_degisti` kırpılmış değeri taşır, `yay_siniri` freni; `yay(ad)` kriterde okunur.

## HanGerilim: `hafiza_var(...)` ön koşulları cooldown'a çevrilmeli mi? (T11)

**Karar (Yusuf):** Hayır. `samples/HanGerilim` eylemlerinin `hafiza_var(itiraf_etti | sakinlesti | patladi | kacti, ben, 24)` ön koşulları değişmez; cooldown bu koşulların yerini tutmaz.
Golden #3 (`han-gerilim-1.json`) bu karardan etkilenmez.

**Neden:** İki mekanizma farklı şeyi ifade eder. `hafiza_var(kriz, ben, 24)` krizden SONRA 24 tick eylemi engeller (toparlanma penceresi). Cooldown eylemin KENDİ son tetiklemesinden sayar (tekrar sınırı).
Aynı oyuncu eylemleri, seed 42, 24 tick ile üç varyant koşuldu (`eylemler.json` bir kopyada değiştirildi, `yalan_soyle` ve `tehdit_et`):

|                                          | bugün (`hafiza_var`) | `hafiza_var` yerine `cooldown: 24` | `hafiza_var` yerine `cooldown: 3` |
| ---------------------------------------- | -------------------- | ---------------------------------- | --------------------------------- |
| Hatice yalanı                            | 2                    | 1                                  | 7                                 |
| `itiraf_etti` / `sakinlesti` / `patladi` | 1 / 1 / 1            | **0 / 0 / 0**                      | 5 / 8 / 3                         |
| Tehdit (Can / Selim)                     | 2 / 2                | 1 / 1                              | 8 / 5                             |
| Fren olayı                               | 0                    | 6                                  | 32                                |
| `son_hash`                               | `d98b50f39fc358dd`   | `47d73a8cc99b4a9f`                 | `69669ea2cb4bd8ba`                |

- `cooldown: 24`: eylemler tek seferlik olur, stres hiçbir zaman kriz eşiğine ulaşmaz, HanGerilim'in ana değişmezleri (iki yalan, bir itiraf, Selim'in patlaması) bozulur.
- `cooldown: 3`: kriz tekrar tekrar çıkar; krizden sonraki toparlanma modellenmez.
- Ön koşulların yanına `cooldown` eklemek de (ör. `tehdit_et`'e `cooldown: 2`) Can'ın t2 ve t3'teki iki tehdidini bire indirir ve hash'i değiştirir.

Cooldown'ın doğru kullanımı `samples/FrenliHan`'da gösterilir (kural tekrarı). Krizden sonra bekleme `hafiza_var` ile kalır.

## Ölçüm (7. hafta, T13; `tools/olcum`, benchmark değil)

Stres fikstürü (her NPC her tick eylem seçer), 168 tick, 3 çalıştırmanın ortancası, bu makinede. Önceki değerler 6. hafta sonundaki ölçümdür; Z3 (zihin kanonik metin önbelleği) ve 7. haftanın diğer işlerinden sonra:

| | Önceki (ms/tick) | Şimdi (ms/tick) | Değişim | 168 tick yükleme (baştan oynatma) |
|---|---|---|---|---|
| 8 NPC dedikodusuz | 2.52 | **2.08** | −17% | 0.35 sn |
| 8 NPC dedikodulu | 4.62 | **3.79** | −18% | 0.64 sn |
| 50 NPC dedikodusuz | 79.5 | **32.4** | −59% | 5.4 sn |
| 50 NPC dedikodulu | 523.1 | **183.7** | −65% | 30.9 sn (önceden ≈ 88 sn) |

- **5 ms hedefi:** 8 NPC'de TUTULDU (2.08 / 3.79). 50 NPC'de tutmuyor: dedikodusuz 32.4 ms (6.5×), dedikodulu 183.7 ms (37×).
- **Tick başına olay:** 8 NPC ≈ 96 (dedikodusuz), 50 NPC ≈ 1738 (dedikodusuz) ve ≈ 1698 (dedikodulu). Dedikodulu 50 NPC'de kanıtların %99.7'si `gordu` (126946 / 127268), NPC başına en çok 328 önerme; `karar_verildi` günlük boyutunun %10.7'si.
- **Fren olayı oranı:** stres fikstüründe 0 (fren alanı yok). Gerçek frenli koşular: `frenli-han-1` 8/675 = %1.19, `han-gerilim-1` 3/1693 = %0.18 (üç `en_fazla` freni).
- **Z3'ün kazancı:** önbelleksiz referans hesapla zihin hash'i 50 NPC dedikodusuzda tick başına 27.7 ms (tick süresinin ≈ %85'i), dedikodulu 63.1 ms; önbellek (bölüm önbellekleri + hafıza satır önbelleği) bu payın büyük kısmını kaldırdı. Kalan maliyet bu fikstürde her zihin her tick değiştiği için tam metni yeniden birleştirmek ve FNV ile hash'lemektir.
- **Kalan fark (50 NPC):** dedikodusuz 32.4 → 5 ms için ≈ 6.5×; dedikodulu 183.7 → 5 ms için ≈ 37×. Dedikodu maliyeti ≈ 151 ms/tick (183.7 − 32.4): konuşan başına O(n²) inanılan taraması (NPC başına 328 önerme) ve algı hacmi (≈ 1700 olay/tick).

**Sonraki optimizasyon adayları (VERİYLE sunulur, SEÇİLMEDİ; karar Yusuf'un):**
1. *Gossip: önerme dizini / yüklem ön filtresi.* `anlat` eyleminin `konu_desenleri`ne uymayan önermeler tarama dışına çıkar. Dedikodu maliyetinin (≈ 151 ms/tick) çoğu bu taramadadır; olay biçimi ve hash değişmez. Beklenen kazanç: dedikodulu 50 NPC'de büyük (tahmin: ≥ 2×), ölçülmeden söz verilmez.
2. *Z4: kanıt + hafıza olaylarının birleştirilmesi.* Algı başına iki olay yerine biri: olay sayısı ≈ %45 azalır (kanıt + hafıza ≈ olayların %89'u). **Olay biçimini değiştirir, bütün hash'leri kaydırır**; Yusuf'un onayı gerekir.
3. *Z4: silik hafızanın durumdan düşürülmesi / önem eşiği.* Hafıza büyümesini keser, davranışı ve hash'i değiştirebilir (algı hafızası önem 0.2).
4. *Hash'i artımlı yapmak.* FNV zincirleme olduğu için ön ekin yeniden kullanımı ancak ön ekteki zihinler değişmediğinde mümkündür; stres fikstüründe her zihin her tick değiştiğinden kazanç yok, gerçek senaryolarda küçük.

## Son ölçüm tablosu (7. hafta sonu, aynı tablo, iki fikstür AYRI etiketli)

`tools/olcum`, benchmark değil; 168 tick, 3 çalıştırmanın ortancası, aynı makine. Son satırlar `TetikSayaclari.KanonikMetin` önbelleği (sayaç metni yalnızca sayaç değişince yeniden kurulur; hash'ler değişmedi) sonrasıdır.

| Fikstür | NPC | ms/tick | 5 ms hedefi | olay | fren olayı |
|---|---|---|---|---|---|
| **STRES** (her NPC her tick eylem seçer), dedikodusuz | 8 | 2.40 | tutuldu | 16200 | 0 |
| **STRES**, dedikodulu | 8 | 4.08 | tutuldu | 16107 | 0 |
| **STRES**, dedikodusuz | 50 | 35.8 | tutmadı (≈ 7×) | 292041 | 0 |
| **STRES**, dedikodulu | 50 | 186.1 | tutmadı (≈ 37×) | 285312 | 0 |
| **GERÇEKÇİ** (dedikodu açık) | 8 | 6.58 | tutmadı (≈ 1.3×) | 11412 | 1156 |
| **GERÇEKÇİ** (dedikodu açık) | 50 | ≈ 685–722 | tutmadı (≈ 140×) | 327763 | 13459 |

**Gerçekçi fikstürün varsayımları** (`tools/olcum/GercekciFikstur.cs`, `--gercekci`; stres fikstürü DEĞİŞMEDİ): sahte içerik, Kasaba Hanı değil. 8 mekan (han merkez, 7 bağlantı); NPC'ler mekanlara eşit olmayan dağılır (han 6/20, pazar 4/20, mahalle 3/20 … orman 1/20); 9 eylem (`selam`, `yardim`, `tehdit`, `git`, `anlat`, `yalan_soyle`, `calis`, `sohbet`, `dinlen`); **her zaman aday olan eylem yok** (`dinlen` yalnızca stres > 5), yani NPC'ler çoğu tick boşta kalır (50 NPC'de 168 tick'te yalnızca 8142 karar); `selam`, `yardim`, `tehdit`, `git`, `calis`, `sohbet` cooldown'lıdır; 12 kural (üçü `cooldown`, üçü `en_fazla`); 4 kriz; dedikodu açık (oyuncu t1'de han'daki dört NPC'ye dört borç söylentisi anlatır, ayrıca 2 yalan önermesi); oyuncu 6 kez yardım/tehdit eder. Kişilikler anahtarlı RNG'den (seed 4321).
**Okuma:** gerçekçi 50 NPC fikstürü stresten DAHA YAVAŞ. Olası neden (ölçülmüş değil, hipotez): han'a kümelenme (50 NPC'nin ≈ 15'i) herkesin herkesi görmesini, yani **algı olayı hacmini** büyütür (141 bin `gordu`, olayların %43'ü; 8 NPC'de 3146); algı maliyeti mekandaki kişi sayısının karesiyle büyür. Fren makinesi maliyeti: 8 NPC'de cooldown/en_fazla alanları çıkarılınca 3.7 ms (frenli 7.4–9.0 ms; bu oyunda yaklaşık 2×), fren olayı olayların %10 (8 NPC) ve %4.1 (50 NPC) kadarıdır, çünkü kural frenleri engellenen her tetiklemede yazılır. Beklenti ("boşta NPC'li fikstür daha hızlı") DOĞRULANMADI.

## Denenen ve kapatılan optimizasyonlar (Yusuf'un kararı: yalnızca 1 uygulandı, <%30 kazanç ise geri al)

1. **Gossip önerme dizini / yüklem ön filtresi: DENENDİ, KAZANÇ YOK, GERİ ALINDI.** Dedikodulu 50 NPC, A/B aynı makinede: dizinsiz 171.1 / 179.5 ms/tick, dizinli 198.3 / 193.0 ms/tick (kazanç yok, hafif kayıp; eşik %30). 1479 + 117 test ve 4 golden yeşildi, yani doğruluk sorunu değil. Sonuç: dedikodu maliyeti (≈ 150 ms/tick) konu taramasında DEĞİL; yeri bilinmiyor, **profil gerekir** (11–12. hafta).
2. **Z4 kanıt + hafıza birleştirme: UYGULANMADI → 11–12. hafta adayı.** Olay biçimini değiştirir, tüm hash'leri kaydırır.
3. **Z4 önem eşiği / silik hafızanın durumdan düşmesi: UYGULANMADI → 11–12. hafta adayı.** Davranışı ve hash'i değiştirebilir.
4. **Artımlı hash: KAPANDI.** FNV zincirleme olduğundan ön ek yeniden kullanımı ancak ön ekteki zihinler değişmezse mümkün; stres fikstüründe kazanç yok (hash'in payı: 50 NPC dedikodusuzda önbelleksiz tam hesap 37 ms, ama önbellekli yol bunu zaten kaldırdı).
**Açık:** Blazor WASM'de tick süresi ve yükleme ölçülmedi (10. hafta). 5 ms kriteri 12. haftada BenchmarkDotNet ölçümüyle karara bağlanır (silinmedi).
