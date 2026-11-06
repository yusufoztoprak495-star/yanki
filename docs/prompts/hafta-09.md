Görev: Yankı — 9. hafta (Kasaba Hanı içeriği, yanki validate, yanki coverage) Bağlam

Plan: docs/plan/Yanki Plan v2.pdf. Bu görev SADECE 9. hafta. Plan bu haftayı hafif hafta ve tampon sayar (Yusuf ≈ 10 saat): kapsam büyütme.

Önce şunları oku: CLAUDE.md, docs/DURUM.md, planın 3, 5.2, 5.4, 5.6, 5.7, 5.9, 5.10, 7, 8, 10, 11, 12 ve 14. bölümleri, docs/criteria-language.md, docs/authoring-guide.md, ADR-003, 010, 011, 012, 013, 014, 015, docs/notes/ (özellikle diyalog.md, dedikodu.md, frenler.md, stres.md), samples/ altındaki bütün senaryolar (özellikle HanGerilim, FrenliHan, SiraEtkisiSoylenti), samples/KasabaHani/, tools/olcum/, src/Yanki.Dialogue/Sablon/SablonYukleyici.cs, src/Yanki.Cli/KomutCalistirici.cs.

Başlamadan: main'de PR #13'ün merge commit'i (162e180, "Merge pull request #13") var mı bak. Yoksa dur ve bana söyle, kendin merge etme. Varsa main'i çek, yerel hafta-08 dalını sil, hafta-09 dalı aç. Bu görev metnini birebir docs/prompts/hafta-09.md olarak kaydet. DURUM "PR #13 açık" yazıyor: ilk commit'te "merge edildi" olarak düzelt.

Hedef kontrol noktası

samples/KasabaHani/ gerçek, oynanabilir senaryodur: 8 NPC, plan 10'daki içerik bütçesi, 3 gerilim tohumu, Türkçe şablon kümesi. yanki validate samples/KasabaHani hata vermez ve kişilik özelliği uyarısı vermez. Hata ve uyarılar dosya:satir ile, hepsi birlikte listelenir. yanki coverage --seeds 20 Kasaba Hanı'nda: her sonuç kuralı en az bir kez tetiklenir, hiçbir kural tetiklenmelerin %30'undan fazlasını almaz (plan 3). Plan 7'deki sahne Kasaba Hanı'nda kendiliğinden çıkar: oyuncu Ayşe'ye Mehmet'in borcunu anlatır, Ayşe Can'a anlatır, Can'da aileme_iftira (oyuncu) oluşur, Can'ın satırı söylentiden önce ve sonra farklıdır. Bu bir senaryo testidir. 8 NPC ≤ 5 ms kriteri ilk kez gerçek senaryoda ölçülmüştür (ADR-015 açık madde 2). Tutmuyorsa kalan fark yazılmıştır. Dört ana golden ve üç diyalog golden'ı birebir aynıdır.

Bu hafta YAZMA:

Blazor demosu, oyun ekranı, zihin paneli, "kasaba özeti" ekranı, GitHub Pages, WASM ölçümü (10. hafta) BenchmarkDotNet tablosu, yanki diff, ablasyon, denge turu, playtest (11. hafta) README hızlı başlangıcı, samples/Quickstart, docs sitesi, NuGet, devlog #3 (12. hafta) gerçek LLM renderer'ı, iki seviyeli followup, gizli ilişki tohumu, anlık görüntü, hedef zihin katmanı (v0.2 sonrası) yalanın kanıt çelişkisinden otomatik yakalanması, kulak misafirliği Z4 ve olay biçimini değiştiren her şey (11. hafta adayı)

Karara bağlananlar

Bunları yeniden seçenek olarak sunma.

Bu değerlerin hepsi değişmez:

Golden son_hash gunluk_hash senaryo_hash mini-han-1 cee9956ef06df17f 1d54f660948cfc18 7b68157311e70683 mini-karar-1 fe29a63d6dfbc307 a7c3ec331d6a5217 7c36e310cc529867 han-gerilim-1 13e6ccbefed06409 70bbb55256544cd2 aa9ff6767b76478d frenli-han-1 4aece0e5ce0931bd eeb3bf6effbeaf50 8abd615d6170cd8c

Diyalog golden'ı diyalog_hash sablon_hash han-gerilim-diyalog-1 b5b952a2de64bdd4 a8a4ebf156ee251f soylenti-a 91054dbfba1147b9 886a928c5f2e3c42 soylenti-b 416a4aa7d7f7aa78 886a928c5f2e3c42

Beklenmeyen değişimde dur, ilk ayrışan olayı ya da satırı göster, onayımı bekle. --guncelle, --hash-ekle ve --senaryo-hash-yenile komutlarını onayımsız çalıştırma. samples/ altındaki mevcut senaryolar ve şablon dosyaları değişmez. Yeni içerik yalnızca samples/KasabaHani/ altına girer. İçerik bütçesi plan 10'dur: 8 NPC (en fazla 10), yaklaşık 15 eylem, 25–35 sonuç kuralı, 60–80 şablon (her niyet için 1 genel + birkaç spesifik), 2 yay değişkeni, 1–2 korumalı varlık, 3 gerilim tohumu (borç, kayıp eşya, hırsızlık). Gizli ilişki yok. Bütçeyi aşma, gerekirse dur ve sor. Plandaki adlar ve ilişkiler bağlayıcıdır: hancı, borçlu çiftçi, dedikoducu terzi, şüpheli yabancı, muhtar, çiftçinin kardeşi Can ve iki NPC daha. Plan 7 ve 5.8 örnekleri Kasaba Hanı'nda çalışmalıdır: Mehmet borçlu çiftçi, borclu(Mehmet,Ayse,500), Ayşe'nin sosyalliği yüksek, Can Mehmet'in kardeşi ve merhameti yüksek. 12. haftadaki hızlı başlangıç bu senaryoyu kullanır. Her kişilik özelliği en az bir eylem ölçütünde ve en az bir diyalog kriterinde kullanılır (plan 10). Mekaniği olmayan süs özellik olmaz. 5 ms kriteri ADR-015'teki gibidir: 8 NPC ortalama tick ≤ 5 ms, bu haftadan itibaren ölçüt gerçek Kasaba Hanı. Tutmazsa sayı değiştirilmez, kalan fark yazılır. İçeriği hız için kısmak benim kararımdır, kendin kısma. Dil: bu hafta Türkçe şablon kümesi yazılır. İngilizce şablon kümesi 8. hafta F kararıyla bu haftaya kalmıştı, ama bu haftanın son işidir (Bölüm 3) ve sığmazsa kesme adayıdır: kendiliğinden kesme, sor. ADR-012 ve ADR-014 sınırları içerik için de geçerlidir: yalan gizliliği, konuşan dinleyenin zihnini okumaz, şablonda gercek/olay_rolu/olay_konusu/konu_puani yok, hafiza taşıyan şablonda gönderdiği kriter zorunlu, genel şablon tekrar sınırı taşımaz. Doğrulayıcının reddettiği bir şeyi içerik için gevşetme. LLM karar vermez (ADR-002). Bu hafta LLM kodu ve ağ erişimi yok. Performans optimizasyonunda 7. haftanın kuralı geçerli: tek tur, en çok iki aday, aday profilin en büyük payından seçilir, %30'dan az kazanç sağlayan değişiklik geri alınır, olay biçimini değiştiren her şeyde dur.

Kod ve belge okuyarak bulunan durum

Bunlar görev metni yazılırken repodan çıktı, kod çalıştırılmadı. Doğrula, yanlışsa bana söyle.

samples/KasabaHani/ yalnızca README.md ve boş bir npcs.md tablosu içeriyor. JSON içerik yok, NPC tasarımı yok. İçeriğin hepsi bu hafta yazılacak. CLI'da validate ve coverage komutu yok. Komutlar: run, replay, devam, query, why, diyalog, diyalog-replay. Yükleyiciler ayrı: SenaryoYukleyici, KuralYukleyici, EylemYukleyici, KrizTablosu, SablonYukleyici (uyarı listesi var). Tek bir senaryo doğrulama girişi yok. Plan 8'in istediği "kullanılmayan kişilik özelliği" ve "hiç ulaşılamayacak şablon" uyarıları yok. DURUM'daki 9. hafta açık kararı: NPC kökenli söylentiler için söylentinin kökünü okuyan 19. fonksiyon gerekir mi, ve anlatti kuralları aynı söylentiyi her duyuşta tetikliyor ("aynı kökten ilk duyuş" yolu). Şimdilik yazar en_fazla/cooldown ile kapatıyor. Kasaba Hanı'nda dedikoducu terzi ve NPC kökenli söylentiler olacağı için bu soru bu hafta kapanmalı (bkz. S). ADR-015 açık riski: --butce varyantı (15 eylem, 30 kural, sahte içerik) 8 NPC'de ≈ 5.8–8.0 ms/tick, p95 ≈ 18 ms. Kasaba Hanı bu hacimde olacağı için kriter tutmayabilir. Maliyetin %67'si NPC kararı (dedikodu konu seçimi ana kalem). tools/olcum gerçek senaryo dosyasını okumuyor, yalnızca kodla kurulan fikstürler var (stres, --gercekci, --butce, --diyalog). Plan 5.8'deki hızlı başlangıç (Scenario.Load, PlayerAction.Tell, OpinionOf, LastChange, Why) gerçek API adlarıyla uyuşmuyor olabilir. Bu hafta API değiştirme, yalnızca farkı DURUM'a yaz (12. haftaya not). Plan 10 "her NPC dosyası en fazla yaklaşık 10 alan" diyor, mevcut şema NPC'leri scenario.json içinde tutuyor. Şemayı değiştirme, sınırı NPC girdisi başına uygula. Oyuncunun eylem menüsü hiçbir yerde tanımlı değil. Oyuncu eylemi CLI'da serbest tür:hedef:yük. 10. haftanın menüsü bir yerden okunacak (bkz. M). SenaryoHashu senaryo + kural + eylem + kriz dosyalarını kapsar, şablonlar ayrı sablon_hash taşır (ADR-014). Kasaba Hanı da bu ayrımı korur.

Önerilen varsayılanlar

V, K, S, M ve G önerilen varsayılandır, ONAYIMDAN ÖNCE KODA GİRMEZ: planla birlikte göster, her madde için öneri + ödünleşimi yaz, kararımı bekle. Daha iyi bir seçenek görürsen onu da yaz. Sayılar denge alanımdır.

V. yanki validate (tam sürüm):

yanki validate <senaryo_dizini | scenario.json> [--sablon <dosya>]. Tek giriş, mevcut yükleyicilerin hepsini çağırır, ikinci ayrıştırma yolu açmaz. Şablon dosyası için SablonYukleyici kullanılır, uyarılar dahil. Hatalar ve uyarılar dosya:satir: tur "id": mesaj biçiminde, hepsi birlikte, dosya ve satır sırasıyla. Çıkış kodu: 0 temiz, 1 hata var. Uyarı varken çıkış kodu için öneri: 0, --uyari-hata ile 1. CI Kasaba Hanı'nı --uyari-hata ile doğrular. Plan 8'deki denetimler: JSON şeması, kriter sözdizimi, etkisi boş kural (hata, var olan), hiçbir eylem ölçütünde ya da diyalog kriterinde kullanılmayan kişilik özelliği (uyarı, iki ayrı uyarı: ölçütte yok / diyalogda yok), hiç ulaşılamayacak şablon (uyarı). "Ulaşılamaz şablon" yalnızca statik tanımdır: concept'i hiçbir eşlemede yok; konu_deseni senaryodaki hiçbir eylemin konu_desenleri/yalan_onermeleri ile ve oyuncunun anlatabileceği önermelerle eşleşemez; kriteri senaryoda olmayan sabit kimlik okuyor; sonra hedefi olan kimlik senaryoda yok. Koşuya bakan "hiç seçilmedi" denetimi coverage'ın işidir, validate'in değil. Daha iyi bir tanım görürsen yaz. Mevcut senaryolar değişmez. Onlarda çıkan uyarıları listele, düzeltme.

K. yanki coverage:

yanki coverage <senaryo_dizini> --seeds N [--sablon <dosya>] [--tick T]. Varsayılan T = 168 (bir oyun içi hafta, plan 10). Girdi modeli (plan bunu söylemiyor, öneri): senaryo dizininde birkaç sabit oyuncu betiği (oynanislar/*.json, mevcut kayıt biçimindeki eylem günlüğü), her betik × N seed koşulur, ayrıca oyuncusuz koşu. Alternatif: seed'den anahtarlı RNG ile türeyen rastgele geçerli oyuncu eylemleri. Ödünleşim: betikler denge çalışmamda tekrarlanabilir ve okunabilir ama benim yazdığım yolları ölçer; rastgele girdi geniş kapsar ama gerçek oynanışa benzemez ve %30 eşiğini anlamsızlaştırabilir. Çıktı Markdown: kural başına tetiklenme sayısı ve payı, eylem başına seçilme sayısı, kriz satırı başına tetiklenme, şablon başına seçilme (diyalog açıksa), fren türü başına sayı. İşaretler: hiç tetiklenmeyen kural ve şablon, payı %30'u aşan kural. Eşikler ayar sınıfında, varsayılanı plan 3. Sayım yalnızca günlükten ve diyalog satırlarından türer (KuralId, olay türü, şablon kimliği). Simülasyona sayaç ya da yeni olay ekleme. Çıkış kodu: plan 3 kriteri tutmazsa 2. CI'a eklenip eklenmeyeceği DURAK 3'te süreye göre kararımdır.

S. Söylenti kökü (DURUM'daki 9. hafta açık kararı):

Seçenekleri Kasaba Hanı tasarımı üzerinden sun, soyut değil: Kasaba'da hangi kural NPC kökenli söylentide yanlış kişiye yönelir, hangi kural aynı söylentiyi birden çok kez tetikler. En az şu üç seçenek: (1) 19. fonksiyon yok, yazar en_fazla/cooldown ve sabit hedefle kapatır (bugünkü yol); (2) söylentinin kökünü okuyan 19. sorgu fonksiyonu; (3) anlatti kuralları için yerleşik "aynı kökten ilk duyuş" kriteri ya da alanı. Her birinde: sözlük etkisi, yalan gizliliğine etkisi (kök bilgisi yalanı sızdırır mı?), NPC bilgi sınırı (dinleyen kökü gerçekten bilebilir mi, yoksa yalnızca kimden duyduğunu mu?), dört golden'a etkisi. Öneri: sözlüğe dokunmadan (1), Kasaba'da gerçek bir sorun çıkarsa (2). Sözlük değişikliği benim kararımdır.

M. Oyuncu eylem menüsü:

hafta oyuncu menüsünü bir yerden okuyacak. Öneri: samples/KasabaHani/oyuncu_eylemleri.json, çekirdek şeması değişmez, senaryo_hash'e girmez, yalnızca CLI ve ileride demo okur; her girdi tür, hedef türü, yük şablonu ve görünen metin taşır; validate her girdinin Submit'te geçerli olacağını denetler. Alternatif: senaryo şemasına alan eklemek (SenaryoYukleyici bilinmeyen alanı reddeder, çekirdek değişir). Ödünleşim yaz. Plan 10: oyuncu konuşur, bilgi verir ya da saklar, iyilik ya da kötülük yapar. Menü bu dört aileyi karşılamalı.

G. Golden ve testler:

Kasaba Hanı için golden replay #5 (tests/golden/kasaba-hani-1.json) ve diyalog golden'ı (tests/golden/diyalog/kasaba-hani-diyalog-1). Plan 3'ün "biri tarayıcı çıktısıyla karşılaştırılır" maddesi 10. haftada buna bağlanacak. Betik ve seed benim seçimimdir (DURAK 2). Değişmezler koşudan ÖNCE test olarak yazılır (KasabaHaniTests), 6. ve 7. haftadaki gibi. Kasaba Hanı, OzellikTestleri'ne (CsCheck) eklenir: sınırlar, aynı seed+girdi, N+M, korumalı varlık, NedenOlaylar, yalan gizliliği, diyalog açık/kapalı hash eşitliği. CI tohumu ve örnek sayısı değişmez, süre artışını yaz.

İçerik (BENİM ALANIM)

İçeriği sen taslaklarsın, ben seçer ve düzeltirim. Taslak olduğu README'de ve dosya başında belirtilir. Sayılar yer tutucudur, 11. hafta denge turunda ayarlanır.

DURAK 1'de sun:

Kadro için 2–3 seçenek: 8 NPC'nin her biri için isim (ASCII kimlik + görünen ad), rol, ne istiyor, neyi saklıyor, altı kişilik değeri ve bir cümle "kiminle çarpışır". Plandaki altı rol ve plan 7 ilişkileri her seçenekte sabit, seçenekler kalan iki NPC'de, kişilik dağılımında ve gerilim tohumlarının kime bağlandığında ayrışır. Her seçenekte kişilik çarpışmalarını bir tabloyla göster (hangi iki NPC hangi özellikte zıt). Gerilim tohumları için her seçenekte: borç (plan 7), kayıp eşya, hırsızlık. Hangi eşya, kimin, neden kayıp, kim çalmış ya da kim şüpheleniyor, oyuncunun hangi eylemi neyi değiştirir. Eşya, sahiplik ve konum mevcut mekaniklerle (gerçekler, tek değerli gerçekler, korumalı bayraklar) yazılabilir mi doğrula. Yeni mekanik ya da sözlük fonksiyonu gerekiyorsa dur ve sor. 1–2 korumalı varlık ve 2 yay değişkeni önerisi (biri kasaba_gerginligi, plan 5.9), her birinin hangi kuralda okunup yazıldığı. Mekan grafiği önerisi (yaklaşık 6–8 mekan).

Ben seçince samples/KasabaHani/npcs.md tablosunu doldur (Yusuf'un tasarım tablosu, seçtiğim seçenekle).

Bölüm 1: Plan ve araçlar (önce bunu yap)

Doğrula ve planla

"Kod ve belge okuyarak bulunan durum" listesini doğrula. superpowers:writing-plans ile adım adım plan çıkar. DURAK 1: plan, V/K/S/M/G önerileri, içerik seçenekleri. ONAYIMI BEKLE.

yanki validate

V kararına göre, TDD. Her denetim için önce kırmızı test, mevcut örneklerden ve küçük fikstürlerden. Doğrulama kodu CLI'da değil, CLI'ın çağırdığı ve 10. haftada demonun da kullanabileceği bir yerde durur (çekirdek dosya okumaz: metin + dosya adı alır). Hangi derlemeye koyduğunu ve nedenini söyle.

yanki coverage

K kararına göre, TDD. Sayımın günlükten bağımsız yeniden hesapla aynı çıktığını testle kilitle. Aynı seed ve betikle iki koşu aynı raporu verir. Markdown çıktısı CRLF/LF ve tr-TR/invariant kültürde aynı.

Ölçüm aracı

tools/olcum'a gerçek senaryo modu ekle (--senaryo <dizin> [--betik <dosya>] [--sablon <dosya>]), mevcut --ozet ve --oynat çıktılarıyla. Mevcut fikstürleri değiştirme. Ölçüm aracı çözüm dışındadır, simülasyon kodu değişmez.

Bölüm 2: Kasaba Hanı

Dünya ve NPC'ler

Seçtiğim kadroya göre scenario.json. validate temiz. Plan 7 ilişkileri testle kilitli.

Eylemler, kurallar, krizler

Yaklaşık 15 eylem, 25–35 kural, kriz tablosu (her seviyeye yedek satır). Her kural en az bir gerilim tohumuna ya da plan 7 sahnesine hizmet eder, README'de tohum → kural tablosu olur. Kural yazarken frenleri kullan: cooldown, en_fazla, kapsam, ihlal_bayraklari. anlatti kurallarında S kararını uygula. Sırayla yaz ve her parti sonrası validate + kısa coverage koş: önce plan 7 sahnesi (borç), sonra kayıp eşya, sonra hırsızlık. Bir tohum diğerini boğuyorsa (%30 kuralı) dur, bana sayıyla göster, sayıları kendin ayarlama.

Şablonlar (Türkçe)

60–80 şablon, her niyet için 1 genel + birkaç spesifik. Ayrı şablon dosyası (ADR-014). adlar, onerme_metinleri (her yüklem için olumsuz ve ben/sen varyantları), gramer, eşleme. Her kişilik özelliği en az bir diyalog kriterinde. Hafıza göndermeli şablonlar (plan 5.7) her tohumda en az bir tane. Followup en az iki yerde (tek seviye). Plan 7 Durum 1 ve Durum 2 satırları bu kümeden çıkar. Plan metnindeki "Ağabeyim hakkında ne dediğini Ayşe'den duydum." örneğini Durum 2'nin bir varyantı olarak kullan. Ek tablosundaki yeni adlar (Kasaba'nın görünen adları) için altın ek tablosu elle yazılır, koddan kopyalanmaz. Ünlü uyumu ya da kaynaştırma tablosunda eksik çıkarsa düzelt ve testle göster, sessizce yanlış ek üretme.

Oyuncu eylem menüsü ve betikler

M kararına göre menü dosyası. Coverage ve ölçüm için 3–4 oynanış betiği taslağı (plan 10: konuşur, bilgi verir ya da saklar, iyilik ya da kötülük yapar; biri plan 7 sahnesini oynar, biri oyuncuyu pasif bırakır). Betikler test senaryosudur, benim alanım: taslak olarak sun.

Testler

KasabaHaniTests: değişmezler koşudan önce. En az: plan 7 zinciri ve why zinciri (oyuncu → Ayşe → Can → aile_iftirasi → modifier), plan 7 Durum 1/2 satır farkı, Durum 2b (Can'ın merhameti, oyuncu Mehmet'i ele vermesini isterse stres artar), her tohumun en az bir betikte tetiklenmesi, korumalı varlığın hiçbir betikte ihlal edilmemesi, yay değişkenlerinin sınır içinde kalması, seed 42 ve 7 arasında ölçülebilir fark (olay ve satır), diyalog açık/kapalı son_hash ve gunluk_hash eşitliği. Sayılar yer tutucuysa testte değil README'de belirtilir. Testi geçmek için içeriği değiştirmen gerekiyorsa değişikliği ve nedenini bana göster, testi gevşetme.

DURAK 2: içerik taslağı

Göster: npcs.md, tohum → kural tablosu, eylem/kural/şablon sayıları (bütçeyle), validate çıktısı, coverage raporu (20 seed × her betik), plan 7 sahnesinin satırları (Durum 1/2), her NPC'den 3–4 örnek satır, oynanış betikleri, golden #5 için betik ve seed önerisi. ONAYIMI BEKLE. Golden dosyası onaydan sonra yazılır.

Ölçüm (DURAK 3)

Gerçek Kasaba Hanı, 8 NPC, ADR-015'in A/A yöntemiyle (ayrı süreçler, dönüşümlü sıra, en az 20'şer süreç): diyalog kapalı ve açık ms/tick ortanca, en düşük, en yüksek, p10–p90, p95 tick, en yavaş tick, 168 tick baştan oynatma, bellek. Koşulları (şarj, güç modu, arka plan) yaz. Bütçe varyantıyla yan yana tablo. Tutuyorsa yaz ve geç. Tutmuyorsa: adım dökümü çıkar (geçici zamanlama kodu commit'lenmez, tools/olcum/README.md'deki kural), tek optimizasyon turu, en çok iki aday, profilin en büyük payından. Hash ve olay biçimi değişmez, dört golden ve üç diyalog golden'ı aynı. %30 kuralı. Hâlâ tutmuyorsa sayıyı gevşetme, kalan farkı ve hangi içeriğin maliyet getirdiğini (eylem/kural başına pay) yaz, bana sun. 50 NPC ölçme (Kasaba 8 NPC). Coverage'ın CI'a girmesi için süresini de yaz.

Bölüm 3: İngilizce (sığarsa)

İngilizce şablon kümesi

Bölüm 1 ve 2 bittiyse ve haftanın kalanı yetiyorsa. Aynı concept'ler ve kriterler, dil: en, ayrı dosya. Türkçe ek değiştiricileri İngilizce dosyada doğrulayıcı hatasıdır. İngilizce gramer için gereken en küçük şeyleri (a/an, iyelik 's) öner, ekleme kararı benim. Sığmıyorsa dur ve sor, 12. haftaya taşıma kararı benim.

Dokümantasyon

samples/KasabaHani/README.md: kadro, tohumlar, tohum → kural tablosu, yay değişkenleri, korumalı varlıklar, betikler, yer tutucu sayılar, taslak notu. docs/authoring-guide.md: Kasaba Hanı'ndan örneklerle validate ve coverage kullanımı, denge döngüsü (yaz → validate → coverage → ayarla). docs/criteria-language.md: yalnızca S kararı sözlüğü değiştirdiyse. docs/evaluation.md: ilk coverage tablosu (20 seed), "11. haftada denge turundan önce" etiketiyle. ADR-015: açık madde 2 (gerçek senaryo ölçümü) sonucu. Yeni karar çıkarsa (S, K, M) ADR-016. ADR-016 "Gerekçe (Yusuf)" bölümünü bu görev metninin deviriyle yaz: plandan ve kararlarımdan derle, başa devir notu düş, uydurma gerekçe yazma. Durum "Önerildi" kalır. CLAUDE.md'deki istisna satırına 9. hafta: ADR-016 ekle. Devir sonraki ADR'lere uzanmaz. docs/notes/: validate ve coverage için kısa taslak not (Yusuf yeniden yazar). tests/golden/README.md, CHANGELOG, README durum satırı (ölçek ifadeleri ADR-015 ile uyumlu kalır). docs/DURUM.md: 9. hafta, kararlar, ölçüm, açık kararlar (S sonucu dahil), plan 5.8 API adı farkı (12. hafta notu), 10. haftaya taşınanlar, sıradaki hafta.

Değişmez kurallar

Yanki.Core ve Yanki.Dialogue: LangVersion 9.0, NuGet paketi yok, iki hedefte derlenir. Determinizm kuralları yeni kodun hepsi için geçerlidir (validate ve coverage dahil): System.Random, DateTime.Now, Guid.NewGuid, GetHashCode/HashCode yasak, sırasız gezinti yasak, float/double yasak, tek RNG AnahtarliRng, metin karşılaştırması ordinal, biçimlendirme InvariantCulture. Yeni kod başka bir derlemedeyse YasakliApiTests onu da tarar. Olay tipine yeni alan ekleme, yeni olay türü ekleme. Çekirdeğe yalnızca hash'e girmeyen salt okunur sorgu eklenebilir; eklersen altın değerlerle hash'in değişmediğini göster. Çekirdek Yanki.Dialogue'u tanımaz. Dialogue simülasyonu değiştiren API çağırmaz (testler var, bozma). Çekirdeğe ve Dialogue'a dosya sistemi ya da ağ erişimi ekleme. Dosyayı CLI ve araçlar okur. JSON için Yanki.Core.Json, kriter için KriterOkuyucu, şablon için SablonYukleyici. İkinci yol açma. Kod tanımlayıcıları ve JSON anahtarları ASCII. Türkçe karakter yalnızca metinlerde ve adlar tablosunda. TDD: her parça için önce kırmızı test.

Yusuf'a bırak

Şunlarda karar verme, seçenek sun:

Önerilen varsayılanlar (V, K, S, M, G). Kadro, gerilim tohumları, mekanlar, korumalı varlıklar, yay değişkenleri, oynanış betikleri, golden'ın betiği ve seed'i. Bana görünen şablon metinleri ve denge sayıları (kişilik, taban, eşik, süre, ton tablosu, zaman etiketleri). Sözlük değişiklikleri. Olay biçimini ya da herhangi bir golden'ı değiştiren her şey. Kapsam kesme ve içerik kısma (hız için dahil). Hafta sığmıyorsa dur ve sor.

Çalışma şekli

Her şeyi oku. Durum listesini doğrula. superpowers:writing-plans ile plan çıkar. DURAK 1'de planı, V/K/S/M/G önerilerini ve içerik seçeneklerini birlikte göster, ONAYIMI BEKLE. Onaydan sonra superpowers:test-driven-development ile çalış: önce kırmızı test, her parça küçük commit, commit sonuna Co-Authored-By. Hata ayıklarken superpowers:systematic-debugging. "Karara bağlananlar" dışında plan, CLAUDE.md ve bu görev metni çelişirse dur ve sor. Duraklar: DURAK 1 (plan + varsayılanlar + içerik seçenekleri), beklenmeyen hash değişimi, DURAK 2 (içerik taslağı + coverage), DURAK 3 (ölçüm). PR öncesi CI dışında geniş CsCheck koşusu: 7. haftadaki gibi 5 tohum × 200 örnek, Kasaba Hanı dahil. Tohumları ve sonucu DURUM'a yaz. Bitirmeden superpowers:verification-before-completion: dotnet build Yanki.slnx -c Release iki hedef, 0 uyarı. dotnet test Yanki.slnx -c Release geçiyor. Dört ana golden ve üç diyalog golden'ı doğrulanmış, tablodaki değerler aynı. Golden #5 ve Kasaba diyalog golden'ı doğrulanmış (onayladıysam). yanki validate samples/KasabaHani --uyari-hata ve yanki coverage samples/KasabaHani --seeds 20 çıktısı. Komut çıktılarını göster. PR'dan önce superpowers:requesting-code-review ile diff'i determinizm, kültür bağımlılığı, yalan sızıntısı, NPC bilgi sınırı, hash korunumu ve içerik bütçesi açısından gözden geçir. Önemli bulguları düzelt, kalanları DURUM'a yaz. Push et, hafta-09 → main PR'ı aç. Açıklamada ne yapıldığını, açık noktaları ve kontrol noktasının nasıl doğrulandığını yaz. CI'ın son commit'te yeşil olduğunu doğrula ve PR linkini ver. Merge etme. Son mesajında şunlar olsun: ne yaptın, hangi kararları sen verdin, benim yapmam gerekenler.
