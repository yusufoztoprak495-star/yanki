# Değişiklik günlüğü

Biçim [Keep a Changelog](https://keepachangelog.com/tr/1.1.0/), sürümleme [SemVer](https://semver.org/lang/tr/).

## [Yayınlanmadı]

### Eklendi

- 8. hafta: `Yanki.Dialogue` (olaydan niyet, kriterle şablon seçimi, tekrar, hafıza göndermesi, Türkçe mini gramer, tek seviye followup, `IDialogueRenderer`), `yanki diyalog` ve `yanki diyalog-replay`, ayrı şablon dosyası (`sablon_hash`, `diyalog_hash`), örnek şablonlar (HanGerilim, SiraEtkisiSoylenti; taslak), diyalog golden'ları (`tests/golden/diyalog/`) ve CI adımı, ADR-014 (Önerildi).
- 8. hafta: ADR-015 (5 ms kriteri: 8 NPC v0.1 kriteri, 50 NPC v0.2 hedefi); `tools/olcum --ozet | --butce | --oynat`; `Zihin` çelişki dizini (8 NPC gerçekçi fikstür 11.16 → 2.88 ms/tick, 50 NPC ≈ 1400 → ≈ 330 ms/tick; hash'ler değişmedi).
- ADR-001..010 gerekçeleri, yeniden yazılmış modül notları ve Devlog #1 (Claude Code yazdı; ADR'ler Yusuf'un onayına kadar "Önerildi").
- 1. hafta iskeleti: `Yanki.Core` (modül klasörleri), `Yanki.Dialogue`, `Yanki.Cli` (`--version`), test projeleri, `Yanki.Benchmarks`.
- `netstandard2.1;net10.0` çoklu hedef, `Core` ve `Dialogue` için C# 9.0, merkezi paket yönetimi, deterministik derleme.
- GitHub Actions CI: derleme, test, trx artifact'ı, coverage ve Codecov yüklemesi.
- Mimari ve iskelet testleri (çekirdek yalnızca çerçeveye bağlı, her iki hedefte).
- ADR-001..006 (durum: önerildi), `CLAUDE.md`, `docs/ai-usage.md`, mimari ve kriter dili belgeleri, README iskeleti.
- 2. hafta: `Yanki.Core.Events` (`Olay`, `OlayGunlugu`: yalnızca sona ekleme, Id/Tick/Sira/nedensellik doğrulaması, `Neden` zinciri).
- 2. hafta: `Yanki.Core.World` (`Dunya`, `Gercek`, `GercekDeseni`; tek değerli `!` gerçekler, yönsüz mekan grafiği, dünya olaylarından türeyen durum, ASCII kimlikler).
- 2. hafta: anahtarlı RNG (`AnahtarliRng`: FNV-1a + SplitMix64), `Hash64`, `KanonikYazici` (`decimal`, `ToEven`, 6 basamak) ve `Dunya.DurumHash()`.
- 2. hafta: `Yanki.Core.Criteria` (ayrıştırıcı, 15 fonksiyonluk sözlük ve doğrulayıcı, `Kriter`, `ISorguCozucu`, `DunyaSorguCozucusu` (`gercek`, `ayni_mekanda`), `Secici.EnIyiSec` ve eşleşme izi). Kriterler C# ile kurulur; JSON okuma yok.
- 2. hafta testleri: determinizm, altın sabit (hash ve RNG, bağımsız hesapla doğrulanmış), yasaklı API taraması (`System.Random`, `DateTime.Now`, `double`/`float`, sırasız koleksiyonlar vb.), sözlük-docs eşitleme testi.
- ADR-007: simülasyonda `decimal` (durum: önerildi). `docs/notes/` altında `olay-kaydi.md`, `dunya.md`, `kriter-dili.md` taslakları.
- 3. hafta: `Yanki.Core.Mind` (`Zihin`, `ZihinDeposu`, `Kisilik`, ilişki eksenleri ve süreli fikir modifier'ları, `FikirDokumu`, tek duygulu ruh hali, stres, kanıtlı inançlar ve çelişki kuralı, hafıza, `Sonumleme` saf sönme fonksiyonları, `ZihinAyarlari`, altı zihin olayı, birleşik `DurumHashu`).
- 3. hafta: `Yanki.Core.Perception` (`DogrudanAlgi`: `acik`/`sessiz`/`gizli` görünürlük, `gordu` kanıtı ve hafıza olayı üretimi).
- 3. hafta: `ZihinSorguCozucusu`: sözlükteki 15 fonksiyonun 13'ü gerçek veriye bağlandı (`yalan_soyledi` 6., `yay` 7. hafta).
- 3. hafta: `VarlikTuru.Oyuncu` (`oyuncu` varlık türü; eşya değil). `inanc`/`kaynak` içinde `degil(...)`, tekrar eden kanıtın tazelenmesi, `FikirDokumu` kırpma satırı.
- ADR-008: zihin olaylardan türetilir; süreli değerler sorgu anında hesaplanır (durum: önerildi). `docs/notes/zihin.md`, `docs/notes/algi.md` taslakları. `docs/DURUM.md` proje durum dosyası (her PR'da güncellenir).
- 5. hafta: `Yanki.Core.Decision` — eylem tanımları (JSON, `EylemYukleyici` doğrulayıcı, `dosya:satir: eylem "id": mesaj`), `eylem.hedef` rolü (yalnızca eylem bağlamı), aday üretimi (`AdayUretici`), puanlama (`taban × Π(eğri) × kişilik × fikir × ruh hali`), decimal eğriler (`dogrusal`, `ustel`, `lojistik`, `basamak`; elle yazılmış `e^y`), `IActionSelector` ve `UtilitySelector` (ilk 3, %70, anahtarlı RNG), karar izi.
- 5. hafta: tick adımları 5 ve 6 (tek durum görüntüsü, eş zamanlı algı), NPC ve oyuncu hareketi (`ayrildi`, `geldi`, ayrılmış `hareket` eylemi), `karar_verildi` olayı (günlükte kısa iz; tam iz replay ile yeniden üretilir, `KararIziYenidenUretici`).
- 5. hafta: `yanki why` karar izine ve puan bileşenlerine iner, `yanki query` son kararı gösterir, `yanki run --eylem hareket:<mekan>`; CLI çıkış kodu 4 (iz uyuşmazlığı).
- 5. hafta: `samples/MiniKarar` senaryosu ve golden replay #2 (`tests/golden/mini-karar-1.json`); `tools/olcum` karar ölçüm düzeneği (çözüm ve CI dışı).
- 6. hafta: dedikodu (`anlatti`, `duydu` kanıtı aktarımı, kök ve yankı freni, `KonuHesabi` konu puanı/konu sınırı, aktarım kaydı `aktarim_kaydedildi`, `yalan_soyledi`), yalan gizliliği (doğru/yalan dışarıdan ayırt edilemez), oyuncu anlatımı (`PlayerAction("anlatti", ...)`), sözlük 18 fonksiyon (`konu_puani`, `olay_konusu`).
- 6. hafta: stres (`stres_tabani`, seçilen çelişen eylem `stres_degisti` yazar) ve kriz (`StresAyarlari`, `KrizYukleyici`/`KrizSatiri`, `kriz_dosyalari`, adım 4 tespit, adım 6 yolu); `yanki run --eylem "anlatti:...:onerme=..."`, `query` kök/aktarım, `why` krizden karar izine.
- 7. hafta: `Yanki.Core.Brakes` — `fren_uygulandi` olayı (9 tür; algılanmaz, kural tetiklemez), cooldown/`en_fazla`/`tick_basina_en_fazla`/genel sınır M (`TetikSayaclari`: sayaçlar olay günlüğünden türer), zincir derinliği D=4 (günlükten türer; etki düşer), korumalı bayraklar (`korumali`, `ihlal_bayraklari`, `oyuncu_ihlalleri`), yay değişkenleri (`yay_degiskenleri`, `yay` etkisi, `yay(ad)`), `Simulasyon.Frenler(...)`, `yanki query/why` fren desteği.
- 7. hafta: kayıt sürüm 2 (`gunluk_hash`, `senaryo_hash`, `bekleyen_eylemler`; sürüm 1 okunur), `yanki devam`, çıkış kodları 5 ve 6, `replay --hash-ekle`.
- 7. hafta (kod incelemesi sonrası): `yanki replay <kayit> --senaryo-hash-yenile`: senaryo dosyaları davranışı değiştirmeden değişince yalnızca `senaryo_hash`'i yazar; `son_hash`/`gunluk_hash` tutmuyorsa reddeder (kod 3 / 5).
- 7. hafta: kaynak modeli (her kaynak ayrı `duydu` kanıtı, güven kök başına tek), zihin kanonik metin önbelleği (Z3), CsCheck özellik testleri (CsCheck 5.0.0, yalnızca test projesi), sıra etkisi senaryoları (`SiraEtkisiSoylenti`, `SiraEtkisiKriz`, `SiraEtkisiTanik`), `samples/FrenliHan` ve golden replay #4, ADR-013, `tools/olcum --gercekci`.
- 7. hafta (değişti): `han-gerilim-1` golden'ı güncellendi (`aile_iftirasi` `en_fazla: 1`, süreler tick = saat, kaynak modeli): `690f2f4ee9d3d385` → `13e6ccbefed06409`.
- 6. hafta: `samples/HanGerilim` senaryosu ve golden replay #3 (`tests/golden/han-gerilim-1.json`); `tools/olcum --dedikodu`; konuşan başına tick içi önbellek (50 NPC dedikodulu 3105.6 → 523.1 ms/tick); `.gitattributes` (LF) ve satır sonu testleri.
- ADR-012 (dedikodu, yalan, stres, kriz; durum: önerildi), `docs/notes/dedikodu.md`, `docs/notes/stres.md`; ADR-011 ve ADR-012 gerekçeleri (Yusuf'un devriyle Claude Code yazdı).
- ADR-011 (karar modeli, durum: önerildi), `docs/notes/karar.md`, `docs/authoring-guide.md` eylem yazımı ve `dinlen` önerisi, `docs/criteria-language.md` eylem bağlamı.

### Değişti

- 7. hafta (düzeltme): yalnızca yay etkili ve `cooldown`/`en_fazla`/`tick_basina_en_fazla` tanımlı kuralda sayaç yanlış anahtara yazılıyordu (`kapsam: npc` kuralda cooldown hiç uygulanmıyor, modifier + yay etkili `kapsam: kural` kuralda tek tetikleme iki kez sayılıyordu). Bu kuralların `yay_degisti` olayına `ben` yükü eklendi. `frenli-han-1` golden'ının yalnızca `gunluk_hash` değeri değişti: `725db2e5f7a27f19` → `eeb3bf6effbeaf50` (`son_hash` `4aece0e5ce0931bd` ve `senaryo_hash` aynı; diğer üç golden'ın hash'leri aynı).
- 5. hafta: `Zihin.HafizaVar` olay kimliğine göre sıralı dizinle erken durur; hedefe bağlı olmayan eylem ön koşulları eylem başına bir kez hesaplanır (davranış ve durum hash'i değişmedi).
- 5. hafta: `RolAdlari.Ad` ve `SorguBaglami` tanımsız rolde sessizce `olay.hedef` dönmez; yasaklı API testi `Math.Pow/Exp/Log/Sqrt/Trig` kullanımını da yakalar.
- 5. hafta: `ayrildi` ve `geldi` yerleşik olay türleridir; `ayrildi`/`geldi` ve hedefi mekan olan olaylarda kural, kişi olmayan hedefe modifier/eksen/duygu yazmaz.
