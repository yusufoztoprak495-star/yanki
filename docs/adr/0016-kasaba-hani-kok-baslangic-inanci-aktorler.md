# ADR-0016: Kasaba Hanı için sözlük, şema ve araç kararları: `kok`, `baslangic_inanclari`, `aktorler`, tek değerli yüklem metni, validate/coverage

**Durum:** Önerildi
**Tarih:** 2026-11-06
**Kabul eden:** —
**Kabul tarihi:** —

## Bağlam

9. hafta, Kasaba Hanı senaryosunu (plan 10: 8 NPC, 3 gerilim tohumu) ve `yanki validate` / `yanki coverage` araçlarını getirdi. İçerik yazılırken, mevcut mekaniğin tek başına karşılayamadığı beş nokta çıktı. Hepsi Yusuf'un görev metninde ve DURAK 1, 2, 2b, 2c, 3 kararlarında verildi. Bu ADR onları tek yerde kaydeder.

1. **Söylentinin kökü.** NPC kökenli söylentilerde "oyuncu kökenli" ile "NPC kökenli" ayrılamıyordu: `aile_iftirasi` hedefi `oyuncu` sabit olduğundan, Ayşe'nin kendi bildiği bir borcu Can'a anlatması oyuncuyu yanlış hedefliyordu. `kaynak(onerme, kisi)` yalnızca doğrudan kaynağa bakar.
1. **Başlangıç inancı yok.** `gercekler` dünyaya yazılır ama NPC zihnine kanıt yazmaz; `npcs.md`'deki "neyi saklıyor" sütunu mekanik olarak boştu ve plan 10'un "mekaniği olmayan süs özellik olmaz" kuralına giriyordu.
1. **Eylemi NPC'ye bağlayan alan yok.** İlk taslakta eylemler kişilik koşuluyla tek bir NPC'ye daraltıldı; bu kimliği kişilik değerlerine bağlıyordu.
1. **Tek değerli yüklem cümlesi.** `onerme_metinleri` `!` ile biten yüklem adını reddediyordu; mührün yeri tek değerli `konum!` ile yazılamıyordu. Ayrıca motor ünlü düşmesini bilmiyordu ("mühür" → "mühürün", doğrusu "mührün").
1. **Araçların yeri.** `validate` ve `coverage` mantığı çekirdeğe (dosya okumaz) ve CLI'a (10. haftada demo kullanamaz) konmamalıydı.

## Karar

1. **Sözlük 18 → 19: `kok(onerme, kisi)`** (`kaynak` ile simetrik). `ben`'in o önerme için **etkin kökü** `kisi` olan herhangi bir kanıtı varsa doğru (`Zihin.KokVar`; `gordu` ve `cikarim` kanıtında etkin kök NPC'nin kendisidir, kökü yazılmamış eski `duydu` kanıtında anlatan). Anlatımda dinleyene konuşanın en güçlü güncel kanıtının kökü geçer. `degil(...)` önermesi kabul eder. Şablonda anlatıcı niyetlerde **yasak** (`inanc`, `kaynak` gibi, yalan gizliliği), cevap ve selam niyetlerinde serbest. Yalan gizliliği değişmez: kök kimin söylediğini gösterir, doğru mu yalan mı olduğunu göstermez (testle kilitli). Dört ana golden ve üç diyalog golden'ı birebir aynı.
2. **Senaryo `baslangic_inanclari: [{ npc, onerme }]` (isteğe bağlı).** Tick 0'da, dünya olaylarından sonra, NPC kimliğine göre ordinal sırayla mevcut `kanit_eklendi` olayıyla yazılır: `tur: gordu`, kaynak NPC'nin kendisi, kök yazılmaz (etkin kök NPC'nin kendisi), güç `AlgiAyarlari.GorduKanitGucu`, `KuralId: senaryo` (mevcut değer), `NedenOlaylar` boş, hafıza olayı yok. **Yeni olay türü yok.** `yanki why` bunu "senaryo başlangıcı" diye yazar. Alan yokken ya da boş dizide `son_hash` ve `gunluk_hash` aynıdır (testle ve yedi golden ile).
3. **Eylem `aktorler: [kimlik...]` (isteğe bağlı).** Listede olmayan NPC o eylemi aday göremez (elenen listesine de yazılmaz). **Kişilik davranışı fiyatlandırır, kimlik belirlemez.** Alan yokken davranış ve hash aynı; tüm NPC'leri listelemek alan yazmamakla aynı `son_hash`/`gunluk_hash` verir (test). `validate` senaryoda NPC olmayan kimliği eylemin `dosya:satir`'ı ile hata sayar; `Simulasyon.Olustur` de reddeder.
4. **`onerme_metinleri` tek değerli yüklemi kabul eder** (anahtar sonda bir `!` taşıyabilir, `konum!`), Dialogue içinde; çekirdek ve hash değişmez. **Ünlü düşmesi:** `adlar` kaydındaki `yumusak` gövde, ünlüyle başlayan eklerde her gövde için kullanılır (`"yumusak": "mühr"` → mührün, mühre, mührü). Motor ünlü düşmesini kendiliğinden bilmez; yazar gövdeyi elle yazar (yazmazsa düşmesiz biçim çıkar).
5. **`Yanki.Authoring` derlemesi** (`netstandard2.1;net10.0`, `LangVersion 9.0`, NuGet yok, Core ve Dialogue'a bağlı): `yanki validate` ve `yanki coverage` mantığı burada; metin ve dosya adı alır, dosya sistemine dokunmaz. CLI dosyayı okuyup çağırır; 10. haftada demo da kullanabilir. `YasakliApiTests` bu derlemeyi de tarar. **Çıkış kodları:** `validate` 0 temiz, 1 hata var (`--uyari-hata` ile uyarı da 1); `coverage` 0 plan 3 kriteri tuttu, 2 tutmadı, 1 girdi hatası.
6. **Oyuncu eylem menüsü** `samples/KasabaHani/oyuncu_eylemleri.json` (M): çekirdek şeması ve `senaryo_hash` değişmez; `validate` her girdinin `Submit`'te geçerli olduğunu gerçek `Submit` çağrısıyla denetler, menü 10 girdiyi geçemez, beş aileden eksik olan uyarıdır; menü eylemleri NPC eylem bütçesine sayılmaz, tepki kuralları kural bütçesine sayılır.
7. **`coverage` (K):** girdi modeli sabit oyuncu betikleri (`oynanislar/*.json`, oturum kaydındaki eylem biçimi) × seed, ayrıca oyuncusuz koşu. Sayım yalnızca olay günlüğünden ve diyalog satırlarından türer; simülasyona sayaç ya da olay eklenmez. Payda yalnızca sonuç kuralı tetiklenmeleridir (algı, eylem, kriz, fren ve oyuncu eylemleri hariç); "en az bir kez" ve %30 betikli koşuların toplamında değerlendirilir, oyuncusuz koşu ayrı sütun. Dünya durumu yüzünden geçersiz kalan betik eylemleri atlanır ve raporda betik, tick, tür ve hedefle sayılır. Hiç seçilmeyen şablonlar üç gruba ayrılır (konsepti satır üretmedi, genel şablon, spesifik şablon). CI'a her push'ta girmez: elle tetiklenen ayrı iş (`workflow_dispatch`).
8. **Plan 5.8 sapması:** plan örneğindeki `borclu(Mehmet,Ayse,500)` (alacaklı Ayşe) Kasaba Hanı'nda `borclu(Mehmet,Riza,500)`'dır (alacaklı Rıza); Ayşe borcu yalnızca oyuncudan duyar ve başlangıçta bu inancı yoktur. 12. haftadaki hızlı başlangıç bu senaryoyu bu biçimde kullanır; plan 5.8 API adlarının (`Scenario.Load`, `PlayerAction.Tell`, `OpinionOf`, `LastChange`, `Why`) gerçek API ile farkı DURUM'dadır.

## Değerlendirilen seçenekler

### Söylenti kökü (S)

| Seçenek                                                     | Sonuç                                                                                       |
| ----------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| (1) 19. fonksiyon yok, `en_fazla`/`cooldown` ve sabit hedef | Kasaba'da oyuncusuz koşuda bile `aileme_iftira(oyuncu)` yanlış hedefle oluşurdu; reddedildi |
| **(2) `kok(onerme, kisi)`**                                 | **Seçildi.** Kök dinleyenin zihnindeki kanıtta zaten var; yeni bilgi kaynağı yok            |
| (3) Yerleşik "aynı kökten ilk duyuş"                        | Kural tetikleme semantiğini değiştirir, golden riski; reddedildi                            |

### Başlangıç inancı

Yeni olay türü gerektirmeyen tasarım seçildi (mevcut `kanit_eklendi`, mevcut `senaryo` kural kimliği). Alternatif olan "gerçek kural içi `gercek(...)` ile yazmak" NPC kararlarında (`gercek` eylem bağlamında yasak) kullanılamaz.

### Eylem bağlama

Kişilik koşuluyla daraltma reddedildi (kimliği kişilik değerine bağlar, denge turunda bozulur). Şema alanı `aktorler` seçildi.

## Ödünleşimler

- `kok` ile şablonda anlatıcı niyetlerde `kok` yasağı, yalan gizliliği için bilinçli bir sınırdır.
- `baslangic_inanclari` yalnızca `gordu` kanıtı yazar (güç sabit); başka kanıt türü ve güç yazmak için alan genişletilmesi gerekir.
- `aktorler` aday listesinde iz bırakmaz (elenen olarak yazılmaz), karar izi küçük kalır; "bu NPC neden yapmadı" sorusu şemadaki `aktorler` listesine bakılarak cevaplanır.
- Ünlü düşmesi otomatik değildir.

## Gerekçe (Yusuf)

> **Devir notu:** Bu bölüm Yusuf'un 9. hafta görev metnindeki ve DURAK 1, 2, 2b, 2c, 3 kararlarındaki devriyle Claude Code tarafından, plandan ve Yusuf'un kararlarından derlenerek yazıldı; Yusuf onaylayana kadar bu ADR "Önerildi" kalır. Yusuf'un yazmadığı bir gerekçe uydurulmadı. Devir yalnızca 9. hafta ADR-016 içindir, sonraki ADR'lere uzanmaz.

- **`kok`:** Yusuf, kökün dinleyenin kendi zihnindeki kanıtta zaten bulunduğunu (ADR-012) ve anlatımın "X söylemiş" diye aktarılmış sayıldığını, bu yüzden NPC bilgi sınırının ihlal edilmediğini yazdı. Kök kimin söylediğini gösterir, doğru mu yalan mı olduğunu göstermez; yalan gizliliği değişmez ve testle kilitlenir. Sözlük değişikliği Yusuf'un kararıdır.
- **`baslangic_inanclari`:** Yusuf, `npcs.md` "neyi saklıyor" sütununun mekanik olarak boş kalmasının plan 10'un "süs" yasağına girdiğini yazdı ve bu alanı önerdi. Kök NPC'nin kendisi, yeni olay türü yok; alan yokken dört golden ve üç diyalog golden'ının birebir aynı kalması şartı konuldu.
- **`aktorler`:** Yusuf "kişilik davranışı fiyatlandırır, kimlik belirlemez" dedi ve kişilik koşuluyla tek NPC'ye daraltmanın geri alınmasını, yerine isteğe bağlı bir `aktorler` alanı eklenmesini istedi; alan yokken davranış ve hash aynı kalmalı, `validate` senaryoda olmayan kimliği hata saymalı.
- **`konum!` ve yumuşak gövde:** Yusuf `yeri(muhur,depo)` yerine `konum!(muhur,depo)` istedi, `onerme_metinleri`'nin tek değerli yüklemi kabul etmesini (Dialogue içinde; çekirdek ve hash değişmez) ve ünlü düşmesinin "mührün" olarak doğru üretilmesini, sessizce yanlış ek üretilmemesini istedi.
- **Alacaklı Rıza:** Yusuf, alacaklının Ayşe değil Rıza olmasını (`borclu(Mehmet,Riza,500)`), Ayşe'nin borcu yalnızca oyuncudan duymasını ve bu sapmanın ADR-016'ya ve DURUM'a yazılmasını istedi.
- **Araçlar:** Doğrulama ve kapsama mantığının CLI'da değil, CLI'ın çağırdığı ve 10. haftada demonun da kullanabileceği bir yerde durması görev metninde yazılıdır (V). `coverage` girdi modeli (K) ve oyuncu menüsünün çekirdek şemasından ayrı bir dosya olması (M) Yusuf'un onayıyla; coverage'ın her push'ta değil elle tetiklenen ayrı bir işte koşması DURAK 3 kararıdır.

## Sonuçlar

- Kolaylaşan: NPC kökenli ve oyuncu kökenli söylenti ayrı kurallarla yazılır; NPC'nin başlangıçta bildiği şey mekanik olur; bir eylemi belirli NPC'lere bağlamak kişilik değerinden bağımsızdır; Kasaba Hanı `validate --uyari-hata` ve `coverage` ile döngüde ayarlanır.
- Zorlaşan: sözlük 19 fonksiyon; şablon yazarı ünlü düşmesini elle yazar; senaryo şeması iki isteğe bağlı alan büyüdü.
- Yeniden bakılacak: hırsızlık tohumu payı %59.9 (11. hafta denge turu); `baslangic_inanclari` için başka kanıt türleri; İngilizce şablon kümesi ve gramer (a/an, 's) 12. haftada; `aktorler`'in demo arayüzünde nasıl gösterileceği (10. hafta).

## Yapılacaklar

1. [x] `kok`, `baslangic_inanclari`, `aktorler`, tek değerli yüklem metni ve ünlü düşmesi (9. hafta).
2. [x] `Yanki.Authoring`, `yanki validate`, `yanki coverage` (9. hafta).
3. [ ] 11. hafta: tohum dengesi (hırsızlık %59.9, borç %27.2).
4. [ ] 12. hafta: plan 5.8 hızlı başlangıç gerçek API adlarıyla; İngilizce şablon kümesi.
5. [ ] Yusuf gerekçeyi onaylayınca durum "Kabul edildi" yapılır.
