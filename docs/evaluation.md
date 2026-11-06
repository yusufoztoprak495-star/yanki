# Değerlendirme

🚧 Bu belge ilerleyen haftalarda doldurulacak (11. hafta): tek seçim farkı ayrışma raporu, ablasyon (dedikodu ve stres
kapalı) ve 3–5 kişilik kör playtest sonuçları.

## İlk coverage tablosu: Kasaba Hanı, 20 seed (11. haftada denge turundan önce)

> **Etiket: 11. haftada denge turundan önce.** Kişilik, taban, eşik, süre ve şablon metinleri yer tutucudur; sayılar denge turunda değişir. Üretim: `dotnet run --project src/Yanki.Cli -c Release -- coverage samples/KasabaHani --seeds 20` (9. hafta sonu, 146 sn, çıkış kodu 0). Betikler `samples/KasabaHani/oynanislar/` (taslak).

**Kural başına kriter tutuyor, tohum dengesi tutmuyor.** Plan 3 kriteri (her kural en az bir kez, hiçbiri %30'u aşmaz) tuttu; en büyük kural payı `tehdit_tanik` %24.0. Ama tohum toplamları: **hırsızlık %59.9**, borç %27.2, kayıp eşya %6.6 (sonuç kuralı tetiklenmeleri, betikli koşular). Hırsızlık payı tanık kurallarından (`tehdit_tanik`, `suclama_tanik`: han kalabalığı) geliyor olabilir. Bu **11. hafta denge turu açık maddesidir** (bu hafta sayı ayarlanmadı).


- Seed sayısı: 20 (1..20)
- Tick: 168
- Betikler: gezgin, hirsizlik_suclama, iyilik_muhur, kotuluk_saklama, plan7_borc (ayrıca oyuncusuz koşu, bilgi amaçlı)
- Pay eşiği: %30 (payda: betikli koşulardaki sonuç kuralı tetiklenmeleri, 12909)
- Dünya durumu yüzünden geçersiz sayılıp atlanan betik eylemleri (betik: sayı): hirsizlik_suclama: 18
- Plan 3 kriteri: TUTTU

## Sonuç kuralları

| kimlik | tetiklenme | pay (%) | gezgin | hirsizlik_suclama | iyilik_muhur | kotuluk_saklama | plan7_borc | oyuncusuz | işaret |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| aile_iftirasi | 19 | 0.1 | 0 | 0 | 0 | 0 | 19 | 0 |  |
| aile_iftirasi_npc | 93 | 0.7 | 20 | 20 | 20 | 20 | 13 | 20 |  |
| borc_duyuldu | 526 | 4.1 | 100 | 102 | 100 | 100 | 124 | 100 |  |
| borc_odendi_alacakli | 20 | 0.2 | 0 | 0 | 20 | 0 | 0 | 0 |  |
| borc_odendi_kardes | 20 | 0.2 | 0 | 0 | 20 | 0 | 0 | 0 |  |
| borc_odendi_tanik | 140 | 1.1 | 0 | 0 | 140 | 0 | 0 | 0 |  |
| borc_sikintisi_goruldu | 200 | 1.5 | 40 | 40 | 40 | 40 | 40 | 40 |  |
| borc_talebi_hedef | 400 | 3.1 | 80 | 80 | 80 | 80 | 80 | 80 |  |
| borc_talebi_tanik | 1741 | 13.5 | 346 | 349 | 354 | 350 | 342 | 346 |  |
| ele_verme_istendi | 40 | 0.3 | 0 | 0 | 0 | 20 | 20 | 0 |  |
| hakaret_tanik | 156 | 1.2 | 0 | 0 | 0 | 156 | 0 | 0 |  |
| hakarete_kirgin | 56 | 0.4 | 0 | 0 | 0 | 56 | 0 | 0 |  |
| itiraf_alindi | 80 | 0.6 | 0 | 0 | 0 | 40 | 40 | 0 |  |
| kardes_ele_verildi_hedef | 40 | 0.3 | 0 | 0 | 0 | 20 | 20 | 0 |  |
| kardes_ele_verildi_tanik | 186 | 1.4 | 0 | 0 | 0 | 83 | 103 | 0 |  |
| kese_calindi_magdur | 92 | 0.7 | 20 | 20 | 20 | 20 | 12 | 20 |  |
| kese_calindi_tanik | 431 | 3.3 | 93 | 94 | 89 | 93 | 62 | 93 |  |
| kese_goruldu | 127 | 1.0 | 27 | 24 | 31 | 29 | 16 | 27 |  |
| kese_haberi | 608 | 4.7 | 149 | 63 | 156 | 150 | 90 | 149 |  |
| misafirperverlik | 321 | 2.5 | 92 | 28 | 140 | 0 | 61 | 92 |  |
| muhur_soran_supheli | 390 | 3.0 | 76 | 80 | 80 | 77 | 77 | 76 |  |
| muhur_soruldu_hedef | 400 | 3.1 | 80 | 80 | 80 | 80 | 80 | 80 |  |
| muhur_yeri_ogrenildi | 20 | 0.2 | 0 | 0 | 20 | 0 | 0 | 0 |  |
| muhur_yeri_ogrenildi_npc | 39 | 0.3 | 14 | 4 | 0 | 12 | 9 | 14 |  |
| saklayana_supheli | 28 | 0.2 | 0 | 8 | 0 | 20 | 0 | 0 |  |
| suclama_tanik | 2287 | 17.7 | 496 | 507 | 499 | 499 | 286 | 496 |  |
| suclanan_kirgin | 364 | 2.8 | 79 | 80 | 80 | 79 | 46 | 79 |  |
| tehdit_korkusu | 692 | 5.4 | 142 | 141 | 142 | 148 | 119 | 142 |  |
| tehdit_tanik | 3099 | 24.0 | 641 | 637 | 632 | 653 | 536 | 641 |  |
| toplanti_sakinlestirdi | 286 | 2.2 | 60 | 60 | 60 | 60 | 46 | 60 |  |
| yardima_minnet | 8 | 0.1 | 0 | 0 | 8 | 0 | 0 | 0 |  |

## Eylemler (seçilme)

| kimlik | seçilme | gezgin | hirsizlik_suclama | iyilik_muhur | kotuluk_saklama | plan7_borc | oyuncusuz | işaret |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| anlat | 5025 | 1112 | 843 | 1037 | 1083 | 950 | 1112 |  |
| borc_iste | 400 | 80 | 80 | 80 | 80 | 80 | 80 |  |
| borc_sikintisi | 200 | 40 | 40 | 40 | 40 | 40 | 40 |  |
| dinlen | 83734 | 16597 | 16955 | 16614 | 16499 | 17069 | 16597 |  |
| git | 29262 | 5815 | 5880 | 5800 | 5794 | 5973 | 5815 |  |
| kardesi_ele_ver | 40 | 0 | 0 | 0 | 20 | 20 | 0 |  |
| kasa_acmak | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| kese_calmak | 92 | 20 | 20 | 20 | 20 | 12 | 20 |  |
| misafir_agirla | 322 | 93 | 28 | 140 | 0 | 61 | 93 |  |
| muhur_sor | 400 | 80 | 80 | 80 | 80 | 80 | 80 |  |
| para_say | 300 | 60 | 60 | 60 | 60 | 60 | 60 |  |
| suclama | 2433 | 535 | 532 | 526 | 534 | 306 | 535 |  |
| tehdit_et | 1640 | 339 | 338 | 331 | 339 | 293 | 339 |  |
| toplanti_cagir | 286 | 60 | 60 | 60 | 60 | 46 | 60 |  |
| yalan_soyle | 1193 | 259 | 260 | 258 | 260 | 156 | 259 |  |
| yardim_et | 8206 | 1616 | 1528 | 1664 | 1816 | 1582 | 1616 |  |

## Kriz satırları

| kimlik | tetiklenme | gezgin | hirsizlik_suclama | iyilik_muhur | kotuluk_saklama | plan7_borc | oyuncusuz | işaret |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| itiraf | 80 | 0 | 0 | 0 | 40 | 40 | 0 |  |
| kac | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç tetiklenmedi |
| patla | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç tetiklenmedi |
| sakinles | 1567 | 330 | 333 | 321 | 332 | 251 | 330 |  |

## Frenler (tür başına olay)

| kimlik | olay | gezgin | hirsizlik_suclama | iyilik_muhur | kotuluk_saklama | plan7_borc | oyuncusuz | işaret |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| cooldown | 29635 | 6254 | 5916 | 6244 | 6443 | 4778 | 6254 |  |
| en_fazla | 684 | 99 | 76 | 97 | 339 | 73 | 99 |  |
| korumali | 100 | 20 | 20 | 20 | 20 | 20 | 20 |  |
| yay_siniri | 814 | 152 | 147 | 209 | 193 | 113 | 152 |  |

## Şablonlar (seçilme)

| kimlik | seçilme | gezgin | hirsizlik_suclama | iyilik_muhur | kotuluk_saklama | plan7_borc | oyuncusuz | işaret |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| anlat_dedikoducu | 792 | 163 | 143 | 164 | 167 | 155 | 164 |  |
| anlat_dogru | 1491 | 372 | 326 | 382 | 191 | 220 | 372 |  |
| anlat_dost | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| anlat_genel | 2858 | 598 | 536 | 649 | 560 | 515 | 598 |  |
| anlat_gergin | 2189 | 428 | 389 | 408 | 596 | 368 | 428 |  |
| anlat_kizgin | 281 | 56 | 50 | 49 | 96 | 30 | 52 |  |
| anlat_korkak | 2 | 0 | 1 | 0 | 0 | 1 | 0 |  |
| anlat_soguk | 693 | 168 | 111 | 172 | 143 | 99 | 171 |  |
| borc_cevabi_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| borc_cevabi_korkak | 400 | 80 | 80 | 80 | 80 | 80 | 80 |  |
| borc_cevabi_utangac | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| borc_talebi_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| borc_talebi_nazik | 400 | 80 | 80 | 80 | 80 | 80 | 80 |  |
| borc_talebi_soguk | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| borc_tesekkur_genel | 20 | 0 | 0 | 20 | 0 | 0 | 0 |  |
| borc_tesekkur_kardes | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| cevap_anlatim_borc | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| cevap_anlatim_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| cevap_anlatim_kese | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| cevap_anlatim_muhur | 20 | 0 | 0 | 20 | 0 | 0 | 0 |  |
| cevap_anlatim_sosyal | 22 | 0 | 2 | 0 | 0 | 20 | 0 |  |
| cevap_anlatim_supheli | 20 | 0 | 20 | 0 | 0 | 0 | 0 |  |
| cevap_genel | 4 | 0 | 1 | 1 | 1 | 1 | 0 |  |
| cevap_kinli | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| cevap_korkak | 1508 | 313 | 306 | 300 | 313 | 276 | 313 |  |
| ele_ver_cevabi_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| ele_ver_cevabi_kin | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| ele_ver_cevabi_merhamet | 40 | 0 | 0 | 0 | 20 | 20 | 0 |  |
| ele_verme_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| ele_verme_stres | 40 | 0 | 0 | 0 | 20 | 20 | 0 |  |
| hakaret_cevabi_cesur | 20 | 0 | 0 | 0 | 20 | 0 | 0 |  |
| hakaret_cevabi_genel | 40 | 0 | 0 | 0 | 40 | 0 | 0 |  |
| hakaret_cevabi_kinli | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| hirsizlik_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| hirsizlik_ofkeli | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| hirsizlik_sosyal | 92 | 20 | 20 | 20 | 20 | 12 | 20 |  |
| itiraf_durust | 40 | 0 | 0 | 0 | 20 | 20 | 0 |  |
| itiraf_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| kacis_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| kacis_korkak | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| misafir_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| misafir_merhametli | 322 | 93 | 28 | 140 | 0 | 61 | 93 |  |
| muhur_cevabi_genel | 307 | 64 | 65 | 62 | 53 | 63 | 64 |  |
| muhur_cevabi_ofkeli | 55 | 7 | 9 | 10 | 20 | 9 | 7 |  |
| muhur_sorusu_genel | 222 | 48 | 44 | 48 | 39 | 43 | 48 |  |
| muhur_sorusu_supheli | 178 | 32 | 36 | 32 | 41 | 37 | 32 |  |
| para_dedikoducu | 300 | 60 | 60 | 60 | 60 | 60 | 60 |  |
| para_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| patlama_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| patlama_ofkeli | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| sakinlik_genel | 827 | 174 | 176 | 170 | 175 | 132 | 174 |  |
| sakinlik_merhametli | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| saklama_cevabi_genel | 40 | 0 | 20 | 0 | 20 | 0 | 0 |  |
| savunma_durust | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| savunma_genel | 453 | 90 | 127 | 90 | 99 | 47 | 90 |  |
| savunma_ofkeli | 836 | 192 | 146 | 193 | 185 | 120 | 192 |  |
| selam_acgozlu | 13 | 12 | 0 | 0 | 1 | 0 | 0 |  |
| selam_borc_hafiza | 19 | 0 | 0 | 19 | 0 | 0 | 0 |  |
| selam_cesur | 36 | 33 | 0 | 0 | 3 | 0 | 0 |  |
| selam_dedikoducu | 21 | 21 | 0 | 0 | 0 | 0 | 0 |  |
| selam_genel | 72 | 72 | 0 | 0 | 0 | 0 | 0 |  |
| selam_gergin | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| selam_hirsiz_hafiza | 150 | 102 | 19 | 13 | 16 | 0 | 0 |  |
| selam_iftira_ayse | 21 | 0 | 0 | 0 | 0 | 21 | 0 |  |
| selam_iftira_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| selam_muhur_hafiza | 9 | 0 | 1 | 8 | 0 | 0 | 0 |  |
| selam_sakin | 139 | 80 | 20 | 0 | 0 | 39 | 0 |  |
| selam_soguk | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| sikinti_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| sikinti_utangac | 200 | 40 | 40 | 40 | 40 | 40 | 40 |  |
| suclama_durust | 1391 | 306 | 304 | 302 | 305 | 174 | 306 |  |
| suclama_genel | 680 | 149 | 149 | 147 | 149 | 86 | 149 |  |
| suclama_kinli | 362 | 80 | 79 | 77 | 80 | 46 | 80 |  |
| tehdit_cesur | 100 | 20 | 20 | 20 | 20 | 20 | 20 |  |
| tehdit_gecmis | 1540 | 319 | 318 | 311 | 319 | 273 | 319 |  |
| tehdit_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| tesekkur_genel | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |
| tesekkur_icten | 20 | 0 | 0 | 20 | 0 | 0 | 0 |  |
| toplanti_genel | 286 | 60 | 60 | 60 | 60 | 46 | 60 |  |
| toplanti_gergin | 0 | 0 | 0 | 0 | 0 | 0 | 0 | hiç seçilmedi |

## Atlanan betik eylemleri (betik | tick tür hedef | atlanan koşu sayısı)

| betik | eylem | atlanan koşu |
|---|---|---:|
| hirsizlik_suclama | 3 anlatti Ayse | 18 |

## Hiç seçilmeyen şablonlar (üç grup)

### (a) Konsepti hiç satır üretmedi

| şablon | concept | açıklama |
|---|---|---|
| kacis_genel | kacis | olay türü kacti hiç oluşmadı |
| kacis_korkak | kacis | olay türü kacti hiç oluşmadı |
| patlama_genel | patlama | olay türü patladi hiç oluşmadı |
| patlama_ofkeli | patlama | olay türü patladi hiç oluşmadı |

### (b) Genel şablon: aynı konseptte spesifik şablon kazandı

| şablon | concept | açıklama |
|---|---|---|
| borc_cevabi_genel | borc_cevabi | concept 480 satır üretti, hepsini spesifik şablonlar aldı |
| borc_talebi_genel | borc_talebi | concept 480 satır üretti, hepsini spesifik şablonlar aldı |
| cevap_anlatim_genel | anlatim_cevabi | concept 62 satır üretti, hepsini spesifik şablonlar aldı |
| ele_ver_cevabi_genel | ele_ver_cevabi | concept 40 satır üretti, hepsini spesifik şablonlar aldı |
| ele_verme_genel | ele_verme | concept 40 satır üretti, hepsini spesifik şablonlar aldı |
| hirsizlik_genel | hirsizlik_tepkisi | concept 112 satır üretti, hepsini spesifik şablonlar aldı |
| itiraf_genel | itiraf | concept 40 satır üretti, hepsini spesifik şablonlar aldı |
| misafir_genel | misafir | concept 415 satır üretti, hepsini spesifik şablonlar aldı |
| para_genel | para | concept 360 satır üretti, hepsini spesifik şablonlar aldı |
| sikinti_genel | sikinti | concept 240 satır üretti, hepsini spesifik şablonlar aldı |
| tehdit_genel | tehdit | concept 1979 satır üretti, hepsini spesifik şablonlar aldı |
| tesekkur_genel | tesekkur | concept 20 satır üretti, hepsini spesifik şablonlar aldı |

### (c) Spesifik şablon: kriteri ya da betiği hiç tutmadı

| şablon | concept | açıklama |
|---|---|---|
| anlat_dost | anlatma | concept 10091 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| borc_cevabi_utangac | borc_cevabi | concept 480 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| borc_talebi_soguk | borc_talebi | concept 480 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| borc_tesekkur_kardes | borc_tesekkur | concept 20 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| cevap_anlatim_borc | anlatim_cevabi | concept 62 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| cevap_anlatim_kese | anlatim_cevabi | concept 62 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| cevap_kinli | cevap | concept 1825 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| ele_ver_cevabi_kin | ele_ver_cevabi | concept 40 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| hakaret_cevabi_kinli | hakaret_cevabi | concept 60 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| hirsizlik_ofkeli | hirsizlik_tepkisi | concept 112 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| sakinlik_merhametli | sakinlik | concept 1001 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| savunma_durust | savunma | concept 1571 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| selam_gergin | selam | concept 480 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| selam_iftira_genel | selam | concept 480 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| selam_soguk | selam | concept 480 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |
| toplanti_gergin | toplanti | concept 346 satır üretti, bu şablonun kriteri ya da betiği hiç tutmadı |


## İşaretler

- Hiç tetiklenmeyen kurallar: yok
- Payı %30'u aşan kurallar: yok
- Hiç seçilmeyen şablonlar: anlat_dost, borc_cevabi_genel, borc_cevabi_utangac, borc_talebi_genel, borc_talebi_soguk, borc_tesekkur_kardes, cevap_anlatim_borc, cevap_anlatim_genel, cevap_anlatim_kese, cevap_kinli, ele_ver_cevabi_genel, ele_ver_cevabi_kin, ele_verme_genel, hakaret_cevabi_kinli, hirsizlik_genel, hirsizlik_ofkeli, itiraf_genel, kacis_genel, kacis_korkak, misafir_genel, para_genel, patlama_genel, patlama_ofkeli, sakinlik_merhametli, savunma_durust, selam_gergin, selam_iftira_genel, selam_soguk, sikinti_genel, tehdit_genel, tesekkur_genel, toplanti_gergin
