Görev: Yankı — 7. hafta (Frenler ve sağlamlık)
Bağlam

Plan: docs/plan/Yanki Plan v2.pdf. Bu görev SADECE 7. hafta. Önce şunları oku: CLAUDE.md, docs/DURUM.md, planın 3, 5.1, 5.2, 5.6, 5.8, 5.9, 5.10, 6, 7, 9, 10, 12 ve 14. bölümleri, docs/criteria-language.md, docs/authoring-guide.md, tüm ADR'ler (özellikle 001, 003, 008, 010, 011, 012), docs/notes/. main'i çek, hafta-07 dalı aç. Bu görev metnini birebir docs/prompts/hafta-07.md olarak kaydet.

Hedef kontrol noktası: kural ve eylem tekrarları frenlenir, korumalı varlık hiçbir eylem dizisinde ihlal edilmez, yay değişkeni kuralları yönlendirir, her fren günlükte iz bırakır ve "bu neden olmadı?" cevaplanır. 3 sıra etkisi testi ve CsCheck değişmezleri yeşil. "N tick koş, kaydet, yükle, M tick koş" ile "N+M tick koş" aynı sonucu verir. Can, söylentiyi Ayşe'den de duyduğunu unutmaz.

Bu hafta YAZMA:

diyalog, niyet üretimi, şablonlar (8. hafta)
Kasaba Hanı içeriği (9. hafta)
Blazor demosu (10. hafta)
ablasyon, coverage, diff, yanki validate tam sürümü
BenchmarkDotNet tablosu (tools/olcum ölçümü kalır)
anlık görüntü (snapshot) kaydı (v0.2)
hedef zihin katmanı (v0.2 sonrası)
yalanın kanıt çelişkisinden otomatik yakalanması, kulak misafirliği
kriz, stres ve taban sayılarının dengelenmesi (11. hafta)
Karara bağlananlar (Yusuf)

Bunları yeniden seçenek olarak sunma.

Hash'ler:
mini-han-1 (cee9956ef06df17f) ve mini-karar-1 (fe29a63d6dfbc307) değişmemeli.
han-gerilim-1 (690f2f4ee9d3d385) yalnızca onayımla değişebilir.
Bu kural Z4 dahil her teslim için geçerlidir. Beklenmeyen hash değişiminde dur, nedenini göster, onayımı bekle.
samples/MiniHan ve samples/MiniKarar değişmez.
Yankı freni aynı kökten gelen kanıtı güven için tek sayar, ama kimden duyulduğu bilgisini silmez. Bu hafta ZORUNLU.
Fren yazmadığı sürece eski senaryoların davranışı aynı kalır. Fren alanı olmayan senaryoda kanonik metne hiçbir şey eklenmez.
Durum hash'ine olay kimlikleri giriyor. Günlüğe eklenen her yeni olay sonraki kimlikleri kaydırır, yani hash'i de değiştirebilir. Bunu bütün teslimlerde hesaba kat.
tests/golden/*.json içindeki son_hash değerlerini --guncelle ile sessizce değiştirme.
Kod okuyarak bulunan sorunlar

Bunlar görev metni yazılırken koddan çıktı. Planla birlikte doğrula, yanlışsa bana söyle.

Zincir derinliği şu haliyle anlamsız. Plan "aynı tick içinde D derinlik, kalanı sonraki tick'e" diyor. 4. haftadaki kaskad v0'da bütün kural çıktıları zaten sıradaki tick'e kalıyor. Aynı tick'teki derinlik hep 1. D sınırı yazılırsa hiç tetiklenmez. (Bkz. C.)
Fren olayları eski golden'ları kırabilir. Tanımsız rol ve kişi olmayan hedef frenleri eski senaryolarda da tetiklenebilir ve olay kimliklerini kaydırır. Koddan bakınca bu üç senaryoda tetiklenmiyor gibi görünüyor, ama çalıştırıp doğrula. (Bkz. A.)
Aday frenleri olay seli yaratabilir. Cooldown'daki bir aday her tick elenir. Her elenmede olay yazılırsa karar_verildi hacim sorunu daha büyük döner. (Bkz. A.)
Tek durum görüntüsü cooldown'ı delebilir. Bir adımda aynı kurala birkaç tetik gelirse hepsi adım başı görüntüsünü görür ve cooldown'dan geçer. (Bkz. B.)
calinamaz için hedef türü yok. Eylem hedefleri yok, npc, oyuncu, komsu_mekan. Eşya hedefi yok, ayrıca bir eylemin hangi bayrağı ihlal ettiği hiçbir yerde yazmıyor. (Bkz. D.)
Bekleyen eylemler kayıtta kayboluyor. Submit edilmiş ama Tick görmemiş eylemler OturumKaydi'ya girmiyor. (Bkz. F.)
Guven kanıtları noisy-or ile birleştiriyor. Aynı kökten birden çok kanıt tutulursa güven şişer. Kaynak düzeltmesi bunu da değiştirmeli. (Bkz. G.)
Yükleme süresi planın varsayımını tutturmuyor. Plan "yükleme = baştan oynatma, 50 NPC ve bir hafta için yeterince hızlı" diyor. 523.1 ms/tick ile 168 tick ≈ 88 sn. 8 NPC ile 4.62 ms/tick ≈ 0.8 sn. (Bkz. H ve 13. teslim.)
Önerilen varsayılanlar (ONAYIMDAN ÖNCE KODA GİRMEZ)

Planla birlikte bu listeyi bana göster, her madde için öneri + ödünleşimi yaz, kararımı bekle. Daha iyi bir seçenek görürsen onu da yaz. Sayılar denge alanımdır ve ayar sınıfında durur.

A. Fren olayı:

Tek olay türü: fren_uygulandi. Yük: fren, kimlik, npc, hedef. KuralId: fren:<tur>. NedenOlaylar: engellenen tetikleyici varsa o.
Türler: cooldown, en_fazla, tick_siniri, genel_sinir, zincir, korumali, tanimsiz_rol, kisi_olmayan_hedef, yay_siniri.
Algılanmaz, kural tetiklemez. ne_zaman: fren_uygulandi doğrulayıcıda reddedilir.
Kural frenleri: engellenen her tetiklemede bir olay. Yalnızca kriterleri tutmuş kural için yazılır, tanimsiz_rol hariç.
Aday frenleri: elenen listesinde ve karar izinde her zaman görünür. Olay, aynı (fren, eylem, npc, hedef) için yalnızca ilk engellemede yazılır; engel kalkıp yeniden başlarsa bir kez daha.
tanimsiz_rol ve kisi_olmayan_hedef: Alternatif olarak aynı tekilleştirme bunlara da uygulanabilir. Bunlar yazar hatası işaretidir; hacmi ölç, sonra öner.
Eski golden'lardan biri fren olayı üretirse dur, hangi olay olduğunu göster.

B. Cooldown ve tetiklenme sınırları:

Kural şeması: cooldown (tick), cooldown_kapsam (kural | npc, varsayılan npc), tick_basina_en_fazla, en_fazla.
Eylem şeması: cooldown, cooldown_kapsam (npc | hedef, varsayılan hedef), en_fazla.
Eylem tarafı DURUM'daki üç eksiği kapatır: hedef bazlı tekrar, 200 tick sonrası unutma, "en fazla k kez".
"Tetiklendi" tanımı: tüm kriterler tuttu ve en az bir etki üretildi.
Sayaçlar hafızadan okunmaz. Yalnızca olay günlüğünden türer, kuralın ya da eylemin ürettiği olaylardan güncellenir (ayrı kural_tetiklendi olayı yok).
Sayaçlar adım içinde hemen güncellenir. Sıra tetikleyici → kural → NPC, yani deterministik. Bu, "tek durum görüntüsü" ilkesinin belgeli istisnasıdır, çünkü bu değer dünya durumu değil tetiklenme kaydıdır.
cooldown: 24 olan kural t=10'da çalıştıysa t=33'te kapalı, t=34'te açıktır.
Kanonik metne yalnızca cooldown ya da en_fazla tanımlı kural/eylem için satır girer.
Genel sınır: senaryoda frenler.tick_basina_en_fazla_sonuc (M). Kuralın etkileri bölünmez, kesim kural tetiklemesi sınırındadır. Sınırı aşan tetikleme düşer ve fren olayı yazılır, sonraki tick'e ertelenmez (kuyruk büyümesin). M yalnızca sonuç kurallarının çıktısını sayar. Varsayılan değer eski senaryolarda hiç tetiklenmeyecek kadar yüksek olmalı; ölç ve öner.

C. Zincir derinliği (plandan sapma, seçim benim):

Seçenek 1 (öneri): Derinlik tick'ler arası ölçülür. Kuralın ürettiği olay 1 + nedenlerindeki en büyük derinlik alır. Algı olayları derinlik eklemez. Oyuncu, NPC eylemi ve kriz olayları 0 başlar. Derinliği D'yi (varsayılan 4) aşacak kural çalışmaz, zincir freni yazılır. Derinlik günlükten türer, Olay tipi değişmez. Ödünleşim: plandaki "kalan etkiler ertelenir" yerine etki düşer; ama erteleme zaten her zaman oluyor.
Seçenek 2: Gerçek tick içi kaskad: adım 2–3 en fazla D tur döner. Plana harfiyen uyar. Ödünleşim: 4. hafta kaskad kararı, tick-dongusu.md tetik kuyruğu ve büyük ihtimalle üç golden birden değişir.
Seçenek 3: D yazılmaz. Sarmal cooldown ve M ile durdurulur, kaskad v0 "D=1" olarak belgelenir. Ödünleşim: en az kod, ama plandaki freni düşürür ve rule→rule döngüsü cooldown'sız kalırsa sonsuza gider.
Hangisi seçilirse seçilsin ADR'ye yazılır.

D. Korumalı bayraklar:

Senaryo: korumali: { "Mehmet": ["oldurulemez"], "kese": ["calinamaz"], "Ayse": ["mekandan_ayrilamaz"] }.
Bayraklar kapalı set: planın üç bayrağı. Doğrulayıcı yazım hatasını yakalar. Alternatif: yazar tanımlı serbest dizeler. Ödünleşim: esneklik, ama yazım hatası sessiz korumasızlık olur.
Eylem şeması: ihlal_bayraklari: [{ "bayrak": "calinamaz", "varlik": "eylem.hedef" | "ben" | "<sabit kimlik>" }]. Eşya hedefi eklemez, sabit kimlik yeter. Alternatif: yeni esya hedef türü. Ödünleşim: "hangi eşyayı çal" adayı doğal olur, ama aday uzayı büyür.
mekandan_ayrilamaz: yerleşik hareket eylemine motor tarafından uygulanır, yazar bildirmez.
Filtre AdayUretici'de, ön koşullardan ÖNCE çalışır. İhlal eden aday adaylar'a hiç girmez, elenen listesine nedeniyle yazılır.
Sonuç kuralı ve eylem etkisi de ihlal edebilir: kural şemasına aynı ihlal_bayraklari alanı eklenir, ihlal eden kural çalışmaz, korumali freni yazılır.
Oyuncu: ihlal eden tür için senaryoda oyuncu_ihlalleri eşlemesi; Submit reddeder. Alternatif: oyuncu serbest, koruma yalnızca NPC'ye. Ödünleşim: plandaki "hiçbir eylem dizisinde" iddiası daralır.

E. Yay değişkenleri:

Senaryo: yay_degiskenleri: { "kasaba_gerginligi": { "baslangic": 0, "min": 0, "max": 10 } }.
Etki: { "tur": "yay", "degisken": "kasaba_gerginligi", "delta": 1 }. Yeni olay yay_degisti: algılanmaz, kural tetiklemez (sarmal açmasın). KuralId ve NedenOlaylar taşır, why buradan zincire iner.
Sınıra kırpma olur; değer kırpıldıysa yay_siniri freni yazılır.
yay(ad) kural kriterinde ve eylem ön koşul/ölçütünde okunur. Kriz satırında reddi kalır. BagliOlmayanFonksiyonlar boşalır. Sözlük 18 fonksiyon kalır.
Yazma: kural ve eylem etkisi. Değişkenler kuralların aynı adımda tuttuğu tek görüntüden OKUNUR; birden çok delta sabit sırayla toplanır.
Bilinmeyen değişken Olustur'da reddedilir. Tanımlı değişken yoksa kanonik metne satır girmez.
Sonuç bandı gibi anlatısal kapılar yok; yazar aralik ile kurar.

F. Kayıt ve yükleme:

OturumKaydi sürüm 2. Sürüm 1 okunmaya devam eder. Çekirdek dosya okumaz, metin alır; OturumOynatici.Oynat tek yükleme yoludur.
Yeni alanlar: gunluk_hash, senaryo_hash, bekleyen_eylemler.
gunluk_hash: bütün olay günlüğünün kanonik hash'i (karar_verildi ve fren olayları dahil). replay ikisini de doğrular. son_hash hesabı değişmez. Böylece seçici değişikliği de replay'de yakalanır, üç golden'ın son_hash'i aynı kalır. Eski dosyalarda alan yoksa denetim atlanır. Çıkış kodu ayrı olur.
senaryo_hash: kural, eylem ve kriz dosyalarının içeriği dahil. Tutmazsa yükleme açık hatayla durur. CLI hesaplar, CRLF/LF aynı değeri verir.
bekleyen_eylemler: bekleyen eylemler kaydedilir ve yüklemede geri Submit edilir. Alternatif: bekleyen varken Uret reddeder. Ödünleşim: basit, ama oyun her an kaydedemez.
Özel ayarlar (ZihinAyarlari, IActionSelector vb.) kayda girmez, belgelenir; v0.1'de varsayılan seçici için garanti verilir.
CLI: yanki run <kayit.json> --devam --tick M --kaydet <yeni.json> (ya da daha temiz öneri).

G. Kaynak bilgisi (yankı freni):

Seçenek 1 (öneri): Her (önerme, kök, kaynak) için ayrı duydu kanıtı tutulur. Hiçbiri silinmez. Güven hesabında her kökten yalnızca en güçlü güncel kanıt noisy-or'a girer. kaynak(desen, kisi) ve KimdenDuydu hepsini görür. Aktarım gücü ve dayanak yine en güçlü tek kanıttan gelir. Tazeleme yalnızca aynı (kök, kaynak) içinde, güncel güçle karşılaştırılır. Geri anlatmama bütün kaynakları kapsar.
Seçenek 2: Tek kanıt kalır, EkKaynaklar listesi eklenir. Ödünleşim: az değişiklik, ama "ne zaman, ne güçle" yalnızca güçlü kaynak için bilinir; plandaki "kanıtlar silinmez" tam karşılanmaz.
İki seçenek de KanonikMetin'i etkiler. Golden #3 onayımı gerekir.
haftanın kapanan tazeleme kararı: en güçlü kanıtın alanları aynı kalır, HanGerilimTests I1 değerleri (Kaynak == "Hatice", Tick == 3, 0.4886) en güçlü kanıt için korunur; ek olarak kaynak(borc, Ayse) Can için doğru döner.

H. Performans:

Z3: Zihin başına önbellekli hash. Plan bu. Alternatif: hash yalnızca istenince hesaplanır (tembel). Ödünleşim: bayat önbellek riski yok, ama plandaki "her tick sonunda hash, ayrışma noktası bulunur" için ayrı mod gerekir.
Z4: Üç seçenek: kanıt ve hafıza olaylarını birleştirme, önem eşiği, silik hafızayı durumdan düşürme. Birleştirme olay biçimini değiştirir ve bütün hash'leri kaydırır; önermeden önce hangi kazancı getirdiğini ölç.
Gossip: önerme dizini, yüklem ön filtresi, bağ dizini. Hangisini seçtiğini ölçümle gerekçelendir.
Hedef: 5 ms hedefini bu haftada tutturmak zorunlu değil. Sıra: önce 8 NPC ≤ 5 ms/tick; 50 NPC için varabildiğin yer + kalan fark. Doğruluk ve hash korunumu hızdan önce gelir.

I. Küçük kod incelemesi maddeleri:

(a) Adım 6.3 gercek "var/yok" denetimi adım öncesi görüntüye alınır; iki NPC aynı tek değerli gerçeği yazarsa son kimlik kazanır ve bu belgelenir. Alternatif: yalnızca belgelemek.
(b) KaynakToplayici ayni_mekanda gibi dünya sorgularını kaynaksiz olarak gösterir.
(c) KrizNedenleri önceki krizde tüketilmiş stres artışlarını saymaz. Bu yalnızca NedenOlaylar'ı değiştirir; durum hash'ini değiştirmez, ama gunluk_hash eklendikten SONRA yapılırsa o da etkilenir. Sıraya dikkat.
(d) Simulasyon.KararIzi yalnızca adayı olan tick'te güncellenir; doküman yorumu.
Değişmez kurallar
Çekirdek kod Yanki.Core içinde, LangVersion 9.0, NuGet paketi yok, iki hedefte de derlenir.
Determinizm: System.Random, DateTime.Now, Guid.NewGuid, GetHashCode/HashCode yasak. Sırasız Dictionary/HashSet gezintisi yasak (ordinal sıralı). float/double ve kayan noktalı matematik yasak, sayılar decimal. Tek RNG AnahtarliRng.
Her durum değişikliği bir olaydır; NedenOlaylar ve KuralId taşır.
Olay tipine yeni alan EKLEME. Gerekirse dur ve sor.
TDD: her parça için önce kırmızı test.
Kod tanımlayıcıları ve JSON anahtarları ASCII.
Çekirdeğe dosya sistemi erişimi ekleme.
JSON için mevcut Yanki.Core.Json ve KriterOkuyucu kullanılır. İkinci kriter ya da etki yolu açma.
LLM karar vermez (ADR-002).
NPC başka NPC'nin zihnini okumaz. Fren durumu ve yay değişkeni NPC bilgisi değildir, yazar aracıdır; kriter yoluyla okunması bilgi sızıntısı sayılmaz.
Tick içi önbellekler ve türetilmiş sayaçlar günlükten yeniden kurulabilir olmalı.
CsCheck yalnızca test projesine eklenir, plan onaylı tek yeni pakettir. Sürüm Directory.Packages.props içinde.
Teslimler

1. Kaynak bilgisi (önce kırmızı test)
   G kararına göre. HanGerilim seed 42'de Can'ın kaynak(borc, Ayse) sorgusu doğru dönmeli.
   Testler: güven aynı kökten şişmez; aynı kaynak tazelenir; farklı kaynak korunur; A→B→C→A'da A'nın güveni artmaz; gordu kanıtı etkilenmez; geri anlatmama iki kaynağı da görür.
   DUR: golden #3 farkını göster, önceki/sonraki kanit satırlarını ve hash'i yaz, onayımı bekle.
2. Fren olayı altyapısı
   A kararına göre tür, yazım, Simulasyon.Frenler(...) sorgusu.
   Algı ve kural kuyruğu dışında kalır.
   yanki query fren listesini gösterir. yanki why <fren olayı> engellenen tetikleyiciye iner.
   DURUM'daki iki not kapanır: atlanan kural günlükte iz bırakır, hedefi mekan olan olayda atlanan kişi etkisi iz bırakır.
3. Cooldown ve sınırlar
   B kararına göre. Şema, doğrulayıcı, KuralMotoru, AdayUretici.
   Sınır testleri: tam pencere sonu, cooldown 1, kapsam farkı, aynı tick'te çok tetik, en_fazla tam sınırda, M tam sınırda, NPC işleme sırasından bağımsızlık, 200+ tick sonrası hâlâ sayılması.
4. Zincir derinliği
   C kararına göre. Rule→rule döngülü fikstürle kırmızı test.
   Fren olayı why ile zincirin köküne iner.
5. Korumalı bayraklar
   D kararına göre. AdayUretici'deki not kapanır.
   Test: korumalı hedef seçilemez, elenme nedeni izde görünür, adaylar listesine hiç girmez, kural ihlali engellenir, oyuncu kararına göre davranır.
6. Yay değişkenleri
   E kararına göre. ZihinSorguCozucusu yay bağlanır, Simulasyon.Yay(ad) eklenir.
   docs/criteria-language.md, doğrulayıcı reddi ve kriz kuralı güncellenir.
7. Kayıt ve yükleme
   F kararına göre. Olay sırası, kimlikler, hash'ler kesintisiz koşuyla birebir aynı olmalı.
   Test: N+M denkliği, bekleyen eylemli kayıt, senaryo değişti hatası, sürüm 1 okuma, bozuk gunluk_hash, seçici değiştiğinde replay hatası.
   Üç golden'a gunluk_hash ve senaryo_hash eklenir; son_hash değişirse dur.
8. Senaryolar (İÇERİK BENİM ALANIM)

Motor testlerini fikstürle yaz. samples/ altına geçmeden dur ve şunları sun:

3 sıra etkisi testi için her biri 2–3 somut seçenek. Biri plan 7 Durum 1/Durum 2 olmalı: oyuncu Can'la söylentiden önce mi sonra mı konuşuyor. Diyalog 8. haftada geldiği için fark fikir, modifier ve why üzerinden ölçülür. Her seçenekte: NPC'ler, eylem sırası A, eylem sırası B, beklenen fark.
Frenli bir senaryo için 2–3 seçenek: en az cooldown, korumalı varlık ve bir yay değişkeni göstermeli.

Ben seçince yaz. Değişmezler koşudan ÖNCE test olarak yazılır. Yer tutucu sayıları README'de belirt. Frenli senaryo golden replay #4 olur (tests/golden/, CI her push'ta oynatır).

9. HanGerilim değerlendirmesi
   hafiza_var(itiraf_etti | sakinlesti | patladi | kacti, ben, 24) ön koşullarının cooldown ile değişip değişmeyeceğini değerlendir.
   Karşılaştırmayı göster: bugünkü davranış, cooldown'lı davranış, hash etkisi. Golden #3'ü onayım olmadan değiştirme.
10. CsCheck değişmezleri

Rastgele geçerli eylem dizileriyle, HanGerilim ve frenli senaryo üzerinde:

eksenler, stres ve yay değişkenleri sınır içinde,
aynı seed ve girdi aynı son_hash ve aynı gunluk_hash,
"N tick koş, kaydet, yükle, M tick koş" = "N+M tick koş",
korumalı varlık hiçbir eylem dizisinde ihlal edilmez,
her olayın NedenOlaylar kimlikleri günlükte vardır ve kendisinden küçüktür,
cooldown penceresi içinde aynı kapsamda ikinci tetikleme yok, en_fazla aşılmıyor,
zincir derinliği sınırı aşılmıyor (C kararı 1 ise),
yalan gizliliği korunur.

Başarısız örneğin seed'i test çıktısına yazılır. Örnek sayısı ve süre CI'da makul kalacak şekilde sabit tutulur, kararsız test bırakılmaz.

11. Z3
    Önce CsCheck kurulur, sonra hash önbelleği yazılır.
    Her tick tam hash ile önbellekli hash karşılaştırılan değişmez eklenir. Çıktı birebir aynı, altın hash'ler değişmez.
    H kararı alternatifi seçerse ona göre.
12. Z4 ve dedikodu performansı
    Gerçek senaryoyla ölç, sonra H kararına göre uygula.
    Olay biçimi değişecekse dur, seçenek ve beklenen kazançla bana gel.
13. Ölçüm (DURAK)

tools/olcum ile 8 ve 50 NPC için ölç: dedikodulu ve dedikodusuz ms/tick, tick başına olay, fren olayı oranı, 168 tick yükleme süresi. Önceki değerler: 79.5, 523.1, 2.52, 4.62 ms/tick. Sonucu docs/notes/ ve DURUM.md'ye yaz. Hedefin kaçırıldığı yerde kalan farkı açıkça yaz. Bir sonraki optimizasyonu kendin seçme, veriyle bana sun.

14. Dokümantasyon
    ADR-013: fren olayı, cooldown, sınırlar, zincir derinliği, korumalar, yay, kayıt sürüm 2, gunluk_hash, kaynak modeli. ADR-012 madde 4 ile çelişen yerleri ADR-013'te belirt; ADR-012'yi silme, durumlarını değiştirme.
    Yeni docs/notes/frenler.md ve docs/notes/kayit-yukleme.md; zihin.md, algi.md, dedikodu.md, karar.md, kural-motoru.md, tick-dongusu.md güncellemeleri.
    docs/criteria-language.md, docs/authoring-guide.md (cooldown, sınır, korumalı bayrak, yay, frenleri okuma), tests/golden/README.md.
    src/Yanki.Core/Brakes/README.md ve ilgili modül README'leri, CHANGELOG.
    docs/DURUM.md: 7. hafta, kararlar, açık kararlar, 8. haftaya taşınanlar, sıradaki hafta. Kapanan maddeler: Z3, Z4, cooldown, iki fren notu, replay kapsamı, kaynak, kod inceleme notları.
    README: durum satırı.
15. Devlog #2

docs/devlog/02-frenler.md. Çıktılar gerçek oturumdan alınır. Konu önerisi: 6. haftadaki kaynak hatası ve "kanıtlar silinmez" sözünün ardından frenlerin nasıl kurulduğu. Devlog #1'deki gibi başına "Yazar: Claude Code (Yusuf'un isteğiyle)" notu yazılır.

16. ADR gerekçesi

ADR-013 "Gerekçe (Yusuf)" bölümünü bu görev metninin deviriyle yaz. Plandan ve kararlarımdan derle, başa devir notu düş, uydurma gerekçe yazma. Durumu "Önerildi" kalır. CLAUDE.md'deki istisna satırına 7. hafta: ADR-013 ekle. Devir sonraki ADR'lere uzanmaz.

Yusuf'a bırak

Şunlarda karar verme, seçenek sun:

"Önerilen varsayılanlar" listesinin tamamı.
Sözlük değişiklikleri.
Sıra etkisi senaryoları, frenli senaryo içeriği ve denge sayıları.
Olay biçimini değiştiren her şey.
Golden #3'ü değiştiren her şey.
Çalışma şekli
Her şeyi oku, "Kod okuyarak bulunan sorunlar" listesini doğrula, superpowers:writing-plans ile adım adım plan çıkar, "Önerilen varsayılanlar" listesiyle birlikte bana göster, ONAYIMI BEKLE. C ve G seçimlerini ayrı başlıkta sor.
Onaydan sonra superpowers:test-driven-development ile çalış: önce kırmızı test, her parça küçük commit, commit sonuna Co-Authored-By. Hata ayıklarken superpowers:systematic-debugging.
Bu prompt'taki "Karara bağlananlar" dışında plan, CLAUDE.md ve bu prompt çelişirse dur ve sor.
Duraklar: 1. teslim (golden #3), 8. teslim (senaryo seçenekleri), 9. teslim, 12. teslim (olay biçimi değişirse), 13. teslim (ölçüm).
Bitirmeden superpowers:verification-before-completion:
dotnet build Yanki.slnx -c Release iki hedef, 0 uyarı.
dotnet test Yanki.slnx -c Release geçiyor.
Bütün golden'lar replay ile doğrulanmış.
Komut çıktılarını göster.
PR'dan önce superpowers:requesting-code-review ile diff'i determinizm, sıra bağımlılığı, nedensellik, bayat önbellek, NPC bilgi sınırı ve hash korunumu açısından gözden geçir.
Push et, hafta-07 → main PR'ı aç. Açıklamada ne yapıldığını, açık noktaları ve kontrol noktasının nasıl doğrulandığını yaz.
CI'ın son commit'te yeşil olduğunu doğrula ve PR linkini ver. Merge etme.
Son mesajında şunlar olsun: ne yaptın, hangi kararları sen verdin, benim yapmam gerekenler.
