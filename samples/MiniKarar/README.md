# MiniKarar: 5. haftanın NPC kararı dilimi

NPC'lerin **kendi eylemlerini seçtiği** küçük senaryo: NPC eylemi → olay → başka NPC algılar → kural çalışır → fikir değişir → `yanki why` zinciri karar izinin puan bileşenlerine kadar iner → aynı seed aynı sonucu verir. **Kasaba Hanı değildir** (o, 9. hafta). Dedikodu, stres, cooldown ve korumalı bayrak yoktur.
İçeriği Yusuf seçti (5. hafta seçenek A + C'nin hareketi). [`MiniHan`](../MiniHan/README.md) değişmedi; kurallar oradan kopyalandı.

## Varlıklar

Mekanlar hat biçiminde bağlıdır: `han` — `pazar` — `degirmen`.

| Varlık   | Mekan    | Kişilik (diğerleri 0.5 yer tutucu) |
| -------- | -------- | ---------------------------------- |
| `oyuncu` | han      |                                    |
| `Ayse`   | han      | merhamet 0.9, sosyallik 0.7        |
| `Can`    | pazar    | cesaret 0.8, kin_tutma 0.6         |
| `Veli`   | degirmen | acgozluluk 0.9                     |

Kişilik değerleri **yer tutucudur**; 11. hafta denge turunda ayarlanacak. Taban ve ölçüt sayıları (aşağıda) senaryoya özgü ve yer tutucudur.

## Eylemler (`eylemler.json`)

| Eylem       | Hedef       | Olay                | Taban | Ölçüt / ön koşul                                                                                                      |
| ----------- | ----------- | ------------------- | ----- | --------------------------------------------------------------------------------------------------------------------- |
| `yardim_et` | NPC         | `yardim_etti`       | 1     | ölçüt `merhamet`; ön koşul `hafiza_var(yardim_etti, ben, 12) == false`; kişilik çarpanı merhamet; fikir çarpanı artan |
| `selam_ver` | yok         | `selam_verdi`       | 0.5   | ölçüt `sosyallik`; ön koşul `hafiza_var(selam_verdi, ben, 6) == false`                                                |
| `git`       | komşu mekan | `ayrildi` + `geldi` | 0.25  | ölçüt `acgozluluk` (üstel k=2)                                                                                        |
| `dinlen`    | yok         | `dinlendi`          | 0.1   | yok (bekleme eylemi, bkz. `docs/authoring-guide.md`)                                                                  |

`dinlen` bilinçli olarak vardır: yardım ve selam sınırlayıcıya takılınca geriye puanı 0'dan büyük hedefsiz bir eylem kalmazsa NPC'ler tek pozitif adaya, yani `git`'e düşer ve sürü halinde dolaşır.

## Kurallar (`kurallar.json`, MiniHan'dan kopya)

`yardim_minneti` (yardımın hedefine `olay.fail` için `minnet` +15, 336 tick), `tehdit_korkusu`, `tehdit_cesur_ofkesi`, `tehdit_tanigi`. Bu senaryoda **tehdit eylemi yoktur**; üç tehdit kuralı yüklüdür ama tetiklenmez (Gerilim senaryosu için ayrılmıştır, bkz. `docs/DURUM.md`).

## Akış (seed 42, 24 tick)

Oyuncu: `t3 yardim_etti → Ayse`, `t5 hareket → pazar`.

| Tick  | Olay                                                                                                                                                                                             |
| ----- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1     | Ayse (handa), Can (pazarda), Veli (değirmende) birer `selam_verdi`; hepsi tek başına olduğundan kimse kimseyi görmez.                                                                            |
| 2     | Veli değirmenden pazara geçer (`ayrildi`/`geldi`).                                                                                                                                               |
| 3     | Oyuncu Ayse'ye yardım eder: Ayse'de oyuncuya `minnet +15` (aynı tick). Can ve Veli pazarda **aynı tick'te** birbirine yardım eder (biri diğerinin yardımını henüz göremez: tek durum görüntüsü). |
| 4     | İki yönlü `minnet +15` (Can→Veli, Veli→Can). Veli aynı tick'te pazardan ayrılır; kural yine de ona çalışır (saklı algılayan).                                                                    |
| 5     | Oyuncu pazara gider: `ayrildi` (han) ve `geldi` (pazar); han'dakiler `ayrildi(oyuncu,pazar)`, pazardakiler `geldi(oyuncu,han)` inancını edinir.                                                  |
| 11–19 | Ayse ve Veli birbirine yardım eder (12 tick sınırı: aynı NPC iki yardım arasında en az 13 tick bekler), Can ile Veli yine pazarda yardımlaşır.                                                   |

Son durum (tick 24): Ayse oyuncuya `+15`, Veli'ye `+15`; Can Veli'ye `+15`; Veli Ayse'ye `+15` ve Can'a `+28.5` (iki yardım, ikinci kopya ×0.9). Son durum hash'i `fe29a63d6dfbc307`.

**Seed farkı:** aynı akışta seed 7, tick 1'den ayrışır (ör. Veli seed 42'de `selam_ver`, seed 7'de `git → pazar` seçer: ikisi de top-3 havuzunda, zar belirler) ve hash'i `b70dde20c25bd622` olur.

## Komutlar

```bash
# Golden oturumu üretmek (gerçek CLI):
dotnet run --project src/Yanki.Cli -- run samples/MiniKarar/scenario.json --seed 42 --tick 2 --eylem yardim_etti:Ayse --tick 2 --eylem hareket:pazar --tick 20 --kaydet tests/golden/mini-karar-1.json

# Replay (hash doğrulama):
dotnet run --project src/Yanki.Cli -- replay tests/golden/mini-karar-1.json

# NPC durumu ve son kararı:
dotnet run --project src/Yanki.Cli -- query tests/golden/mini-karar-1.json Veli

# Veli'nin Can'a minnet modifier'ı (#72) → algı → Can'ın yardımı → Can'ın kararı ve puan bileşenleri:
dotnet run --project src/Yanki.Cli -- why tests/golden/mini-karar-1.json 72
```

`why #72` çıktısının özü:

```
#72 modifier_eklendi (tick 4, kural yardim_minneti) @pazar deger=15 hedef=Can npc=Veli sure=336 tur=minnet
← #56 yardim_etti (tick 3, kural eylem:yardim_et) Can → Veli @pazar
   ← #55 karar_verildi (tick 3, kural karar) Can @pazar secilen_eylem=yardim_et secilen_hedef=Veli
      karar izi: Can, tick 3 (ilk 3 aday, eşik 0.7)
        seçilen: yardim_et → Veli
        aday yardim_et → Veli: puan 0.5 = taban 1 × ölçütler [kisilik(merhamet): girdi 0.5 → normal 0.5 → dogrusal çıktı 0.5] × kişilik 1 × fikir 1 × ruh hali 1; sıra 1, havuzda
        aday selam_ver: elendi — on_kosul #1 tutmadi: hafiza_var(selam_verdi,ben,6) == false
```

Tam iz günlükte değil, replay ile yeniden üretilir (ADR-011); `why` bunu otomatik yapar ve günlükteki kısa izle doğrular.

## Testler

- Uçtan uca: `tests/Yanki.Scenarios.Tests/MiniKararTests.cs` (kontrol noktasının yedi koşulu, seed 42/7 farkı, aynı seed aynı hash ve günlük, `why` zinciri).
- Golden replay: [`tests/golden/mini-karar-1.json`](../../tests/golden/mini-karar-1.json) (`GoldenReplayTests`, CI her push'ta oynatır). NPC kararları dosyaya yazılmaz, tekrar hesaplanır.
