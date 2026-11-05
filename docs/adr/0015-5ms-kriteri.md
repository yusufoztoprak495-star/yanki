# ADR-0015: 5 ms kriteri: 8 NPC v0.1 kriteri, 50 NPC v0.2 hedefi

**Durum:** Önerildi
**Tarih:** 2026-11-05
**Kabul eden:** —
**Kabul tarihi:** —

## Bağlam

Plan bölüm 3 başarı kriteri olarak "50 NPC'lik senaryoda tick başına ortalama süre 5 ms'nin altında" der. 7. hafta sonunda bu kriter 8 NPC stres fikstüründe tutuyordu, gerçekçi fikstürde tutmuyordu (6.58 ms; sonradan 5.80 ms), 50 NPC'de her iki fikstürde de çok uzaktı. Belgeler kararı 12. haftaya bırakıyordu (DURUM, `docs/notes/frenler.md`, ADR-013, Devlog #2); 9. haftada 8 NPC'lik Kasaba Hanı yazılacağı için bu karar bırakılamazdı.
Bu ADR 8. haftada verilen kararı ve ona dayanak olan ölçümleri yazar. Plan PDF'i değişmez; sapma burada kayıtlıdır. ADR-013'ün "Performans ve 5 ms hedefi" bölümü bu ADR'ye bağlanır.

## Karar

1. **v0.1 kriteri:** 8 NPC'lik senaryoda tick başına ortalama ≤ 5 ms. 9. haftaya kadar ölçüt gerçekçi fikstürdür (`tools/olcum --gercekci`), sonra gerçek Kasaba Hanı senaryosudur. 168 tick baştan oynatma bu kriterden türer (168 × 5 ms ≈ 0.84 sn), ayrı bir kriter değildir.
2. **50 NPC ≤ 5 ms v0.1 kriterinden çıkar ve v0.2 hedefi olur.** Silinmez: ölçülen 50 NPC değerleri README ve belgelerde açıkça yayımlanır.
3. **Kriter başka bir onay olmadan yeniden gevşetilmez.** Tutmazsa sayı değiştirilmez, kalan fark yazılır.
4. WASM kabul sınırı 10. haftada ölçümle belirlenir. Bu karar 12. haftaya bırakılmaz.

**Plandan sapma:** plan 3 "50 NPC'lik senaryoda"; bu ADR v0.1 için "8 NPC'lik senaryoda" der (demo 8 NPC, plan bölüm 10).

## Ölçümler

Tümü `tools/olcum` ile alınmıştır (**benchmark değildir**: BenchmarkDotNet'in ısınma ve istatistik titizliği yoktur; BenchmarkDotNet tablosu plana göre 11. haftadadır).

**Koşullar:** Windows 11 dizüstü, Intel Core i7-12700H, .NET SDK 10.0.401, Release, şarjda (AC), güç planı "Dengeli" (sabitlenmedi, değiştirilmedi), arka planda tarayıcı ve editör açık, ağır başka iş yok. Fikstür sahte içeriktir (Kasaba Hanı değil). Her süreç ısınır (10 tick), sonra 5 kez 168 tick koşar; "süreç ortancası" bu beş koşunun ms/tick ortancasıdır. A/B ölçümlerinde kollar dönüşümlü sırayla, ayrı süreçlerde koşuldu; yanında aynı derlemenin kopyası (A/A) vardır.

### 5.80 ve 6.58 farkı

- 6.58 ms, `60eab29` kodunda (`src` `606530c` ile aynı) ölçüldü; tekrar sayısı kayıtlı değil (not başlığı 3, aracın varsayılanı 5). 5.80 ms HEAD'de (`103fdae` sonrası) ortanca 5 koşu olarak ölçüldü.
- **Fark kod kaynaklı değildir.** `103fdae` yalnızca yay etkili kural yolunu değiştirir; gerçekçi fikstürde yay değişkeni yoktur. `60eab29` ile HEAD dönüşümlü ölçüldü (A/A + A/B, 12 tur, 36 süreç, tek sürücü): üç kolun süreç ortancaları 12.11 / 11.79 / 11.72 ms, fark A/A dağılımının içinde.
- **Süreçlerde çift mod görüldü:** bazı süreçler 5.07–6.2 ms'de, çoğu 10–14 ms'de koştu (A/A p10–p90 = 9.0–13.8 ms). 5.80 ve 6.58 bu dağılımın hızlı kuyruğundadır. **Modun nedeni bilinmiyor.** Denenip etkisiz bulunanlar: eşzamanlı GC kapatma, gen0 boyutu, zorunlu GC, tiered JIT kapatma, P/E çekirdeğe sabitleme; işlemci süresi duvar saatine eşit çıktı (bekleme değil, işlemci yavaş çalışıyor). Bugünkü makine 7. hafta belgelerindeki seviyenin ≈ 2 katı yavaştı.
- Dolayısıyla optimizasyon öncesi tek bir ms/tick sayısı anlamsızdı; bundan sonra dağılım (ortanca, en düşük, en yüksek, p10–p90) yazılır.

### Maliyet nerede (yerel zamanlama yaması, commit edilmedi)

| Adım (gerçekçi fikstür)                     | 8 NPC önce | 50 NPC önce | 8 NPC sonra | 50 NPC sonra |
| ------------------------------------------- | ---------- | ----------- | ----------- | ------------ |
| 5 NPC kararı (aday + seçici)                | %88        | %96.5       | %66         | %83          |
| 7a durum hash                               | %7.3       | %2.5        | %15.5       | %12.2        |
| 6.1 + 6.2 + 6.3 olay yazımı ve eylem algısı | %3.7       | %0.8        | %15.3       | %3.6         |
| 2 algı + 3 kurallar + 4 kriz + 1 oyuncu     | %1.1       | %0.3        | %3.2        | %1.1         |

- **"Han'a kümelenme algı hacmini büyütüyor" hipotezi çürütüldü:** algı adımları (2 ve 6.2) tick'in %1.8'i (8 NPC) ve %0.6'sıdır (50 NPC). Kümelenmenin etkisi dolaylıdır (aynı mekandaki (konuşan, dinleyen) çifti sayısı).
- **Asıl neden `Zihin.Inanilan` taramasıydı:** her önerme için konuşanın bütün inançlarını tarıyor (konuşan başına O(P²)); dedikodu konu seçimi (`anlat`, `yalan_soyle`) bunu her tick her çift için çağırıyordu. 7. haftada denenen "gossip önerme dizini" yanlış katmana bakıyordu.

### Optimizasyon turu (en çok iki aday, %30 kuralı)

- **Aday 1: `KonuHesabi.Ilgi` döngüsü, kazançsız, geri alındı** (11.57 → 12.49 ms; zamanlayıcı argüman ifadesini de kapsadığı için yanlış profil okunmuştu).
- **Aday 2: çelişki dizini (`cfd02b3`), uygulandı.** `Onerme.Celisir` yalnızca aynı olgunun olumlu/olumsuz çiftinde ya da aynı tek değerli anahtarın farklı olumlu değerlerinde doğrudur; yalnızca bu gruplar taranır. Hash ve olay biçimi değişmedi (dört golden aynı).

| Ölçü (8 NPC gerçekçi, 21 tur × 3 kol, 63 süreç) | Önce (`10375cf`) | Sonra (`cfd02b3`) | Sonra, A/A kopyası |
| ----------------------------------------------- | ---------------- | ----------------- | ------------------ |
| süreç ortancası (ms/tick)                       | **11.16**        | **2.88**          | 2.95               |
| en düşük süreç ortancası                        | 5.56             | 2.49              | 2.48               |
| en yüksek süreç ortancası                       | 14.50            | 3.34              | 3.67               |
| p10–p90                                         | 8.99–13.52       | 2.64–3.31         | 2.56–3.44          |
| süreç içi en hızlı koşu (ortanca)               | 6.00             | 1.99              | 2.01               |
| süreç içi en yavaş koşu (ortanca / en yüksek)   | 14.98 / 16.75    | 4.30 / 4.84       | 4.27 / 4.50        |
| p95 tick (ortanca)                              | 31.2             | 8.2               | 8.3                |
| en yavaş tick (ortanca)                         | 60.3             | 23.6              | 22.8               |

- **Yavaş mod optimizasyondan sonra görülmüyor:** önce süreçlerin bir kısmı ilk iki koşudan sonra 11–15 ms'e çıkıyordu; sonra 63 sürecin hiçbirinde çıkmadı. Sonrası süreç içi örüntü her süreçte aynı: ilk iki koşu ısınma (3.6–4.8), sonra 2.0–2.3 ms.
- **Kriter durumu: 8 NPC ≤ 5 ms gerçekçi fikstürde TUTUYOR** (süreç ortancası 2.88 ms; "sonra" kollarındaki 210 koşunun hiçbiri 4.84 ms'yi aşmadı).
- p95 tick ≈ 8 ms ve en yavaş tick ≈ 23 ms ortalama kriterini bozmaz ama kare bütçesi olan arayüzler için not edilmelidir.

**50 NPC gerçekçi fikstür (v0.2 hedefi için yayımlanan değer):** önce 1312–1488 ms/tick, sonra 327–342 ms/tick (tek süreç, 4 süreç dönüşümlü sıra). Hedefin ≈ 65–70 katı; v0.2 hedefi olarak açık kalır. (Belgelerdeki önceki "≈ 140×" ≈ 700 ms'lik 7. hafta ölçümüne aittir.)

### 168 tick baştan oynatma (`OturumOynatici.Oynat` ile gerçek süre, hash doğrulamalı; `tools/olcum --oynat`)

| Fikstür              | Oynat ortanca | En düşük – en yüksek | Tekrar | `son_hash` / `gunluk_hash` |
| -------------------- | ------------- | -------------------- | ------ | -------------------------- |
| 8 NPC gerçekçi       | 0.68 sn       | 0.64 – 0.77 sn       | 5      | tutuyor / tutuyor          |
| 8 NPC bütçe varyantı | 1.14 sn       | 0.62 – 1.25 sn       | 5      | tutuyor / tutuyor          |
| 50 NPC gerçekçi      | 59.3 sn       | 57.5 – 68.4 sn       | 3      | tutuyor / tutuyor          |

Kurulum süresi ayrıdır (≈ 2 ms). 10. haftadaki geri sarma bu maliyeti öder; WASM ölçülmedi, çarpan için sayı uydurulmadı.

## Açık risk: plan bütçesine yakın varyant

`--butce` (sahte içerik: 15 eylem, 30 kural, 8 mekan; Kasaba Hanı içeriği değil, yalnızca hacim) 8 NPC'de **≈ 5.8–8.0 ms/tick** (iki yöntem: adım profili 5.8, `--ozet` 7.4–8.0), p95 tick ≈ 18 ms, en yavaş tick 30–40 ms ölçüldü. **Bu, 5 ms kriterinin üstündedir** (10 ms'nin altında). Gerçekçi fikstür 9 eylem ve 12 kural olduğundan, gerçek Kasaba Hanı için iyimserdir.

- Bu hafta üçüncü bir optimizasyon adayı denenmedi (kural: tek tur, en çok iki aday). Kriter gevşetilmedi.
- 9. haftada gerçek Kasaba Hanı senaryosunda yeniden ölçülür. Kalan payların dökümü (bütçe, 8 NPC): NPC kararı %67 (dedikodu konu seçimi ana kalem), durum hash %15.6, olay yazımı + eylem algısı %13.
- Z4 (algı başına iki olayın birleşmesi, önem eşiği) olay biçimini değiştirir; 11. hafta adayı olarak kalır.

## Değerlendirilen seçenekler

| Seçenek                                             | Karar                                                                                                  |
| --------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| Kriteri 50 NPC'de koruyup optimizasyona devam etmek | Reddedildi: 50 NPC hâlâ ≈ 65× uzakta, kapanacak bir fark değil; demo 8 NPC.                            |
| Kriteri 50 NPC için gevşetmek (örn. 50 ms)          | Reddedildi: kriter onaysız gevşetilmez; gevşetme yerine hedef v0.2'ye alındı ve değerler yayımlanıyor. |
| Kararı 12. haftaya bırakmak                         | Reddedildi: 9. haftada 8 NPC'lik senaryo yazılıyor.                                                    |
| 8 NPC kriteri + 50 NPC v0.2 hedefi (seçilen)        | Kabul edildi.                                                                                          |

## Gerekçe (Yusuf)

> **Devir notu:** Bu bölümü Claude Code, Yusuf'un 8. hafta görev metnindeki devriyle ("ADR-015'e de aynı kural uygulanır") yazdı. Gerekçe Yusuf'un görev metnindeki kararından ve planın 3. ve 10. bölümlerinden derlenmiştir; Yusuf'un yazmadığı bir gerekçe uydurulmamıştır. Yusuf onaylayana kadar ADR "Önerildi" kalır.

Yusuf'un görev metninde yazdığı gerekçe: 50 NPC gerçekçi fikstür hedefin ≈ 140 katıydı ve kapanacak bir fark değildi; demo 8 NPC'dir; asıl risk 8 NPC ve 10. haftadaki geri sarmadır; WASM kabul sınırı 10. haftada ölçümle belirlenir; bu karar 12. haftaya bırakılmaz. Plan bölüm 10 demo senaryosunu 8 NPC (en fazla 10) olarak tanımlar.
Yusuf kriterin silinmemesini, 50 NPC için ölçülen değerlerin açıkça yayımlanmasını, plan PDF'inin değişmemesini ve kriterin başka bir onay olmadan yeniden gevşetilmemesini, tutmazsa sayı değiştirilmeyip kalan farkın yazılmasını istedi. Optimizasyonda 7. haftanın kuralı (%30'dan az kazanç geri alınır, olay biçimini değiştiren her şeyde durulur) korundu.

## Sonuçlar

- Kolaylaşan: 9. haftadan itibaren tek ve ölçülebilir bir hız kriteri (8 NPC, ortalama ≤ 5 ms); 50 NPC değerleri gizlenmeden v0.2 hedefi olarak yayımlanır; ölçümler dağılım olarak yazılır.
- Zorlaşan: bütçe varyantı kriterin üstünde; gerçek senaryo 9. haftada ölçülünce kriter tutmayabilir (o durumda sayı değil, kalan fark yazılır). p95 ve en yavaş tick ortalamadan belirgin yüksek.
- Yeniden bakılacak: 9. hafta gerçek senaryo ölçümü; 10. hafta WASM kabul sınırı; 11. hafta BenchmarkDotNet tablosu; Z4; ölçüm makinesindeki iki modlu davranışın nedeni (optimizasyon sonrası görülmüyor).

## Yapılacaklar

1. [x] 5.80/6.58 farkını kanıtla açıkla, adım dökümü, optimizasyon turu, replay ölçümü (8. hafta).
2. [ ] 9. hafta: gerçek Kasaba Hanı senaryosunda 8 NPC ölçümü (bütçe riski).
3. [ ] 10. hafta: Blazor WASM tick süresi ve yükleme ölçümü; kabul sınırı.
4. [ ] 11. hafta: BenchmarkDotNet tablosu (10 / 50 / 200 NPC).
5. [ ] Yusuf gerekçeyi onaylayınca durum "Kabul edildi" yapılır.

## Diyalog maliyeti (8. hafta sonu, dönüşümlü ölçüm)

8 NPC gerçekçi fikstür, `tools/olcum --gercekci --npc 8 [--diyalog]`, aynı derleme, kapalı/açık dönüşümlü 20'şer süreç (her süreçte hem `--oynat` hem `--ozet`), aynı koşullar (AC, Dengeli güç planı, arka planda editör açık).

| Ölçü (süreç ortancası) | Diyalog kapalı: ortanca (en düşük–en yüksek; p10–p90) | Diyalog açık |
| --- | --- | --- |
| `OturumOynatici` ile 168 tick baştan oynatma (ms) | 514 (443–618; 454–575) | 541 (457–749; 488–607) |
| tick başına ortalama (ms) | 2.78 (2.57–3.53; 2.64–3.23) | 3.06 (2.57–3.91; 2.64–3.53) |

- Fark replay'de ≈ +5 %, tick başında ≈ +10 %; iki kolda da dağılımlar büyük ölçüde örtüşür. Aynı düzenekle ve dönüşümlü ölçülünce replay ve tick ölçümleri **çelişmiyor**; diyalog 8 NPC'de ≤ 5 ms kriterini bozmaz (3.06 ms).
- Önceki "+%24 replay (0.54 s'ye karşı 0.43 s)" farkı, kolların ayrı zamanlarda ardışık koşulmasından (süreç çift modu, bkz. yukarısı) geliyordu; 0.68 s ve 0.43 s gibi tek koşular aynı dağılımın iki ucudur (kapalı kolda da 0.44–0.62 s görüldü). Kök neden: ölçüm düzeneğinin dönüşümlü olmaması, diyalog değil. Süreç çift modunun kendisi hâlâ açıklanmış değildir.
- Diyalog fikstürü sahte içeriktir (hacim ölçümü); gerçek Kasaba Hanı hacmi 9. haftada yeniden ölçülür. 50 NPC v0.2 hedefidir.
