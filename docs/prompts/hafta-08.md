Görev: Yankı — 8. hafta (Performans kararı ve Diyalog)
Bağlam

Plan: docs/plan/Yanki Plan v2.pdf. Bu görev SADECE 8. hafta.

Önce şunları oku: CLAUDE.md, docs/DURUM.md, planın 3, 5.1, 5.7, 5.8, 5.9, 8, 9, 10, 11, 12 ve 14. bölümleri, docs/criteria-language.md, docs/authoring-guide.md, tüm ADR'ler (özellikle 002, 003, 010, 011, 012, 013), docs/notes/ (özellikle frenler.md, dedikodu.md, zihin.md, tick-dongusu.md), tools/olcum/.

Başlamadan: main'de docs/DURUM.md içinde "Hafta 8 başlangıç işleri" bölümü var mı bak. Yoksa PR #12 merge edilmemiştir: dur ve bana söyle, kendin merge etme.

Sonra main'i çek, hafta-08 dalı aç. Bu görev metnini birebir docs/prompts/hafta-08.md olarak kaydet.

Hedef kontrol noktası:

5 ms sorusu kapanır: 5.80/6.58 farkı kanıtla açıklanmış, aşağıdaki karar uygulanmış, belgeler tek ifadeye inmiş.
Bir olaydan niyet, niyetten kriterle seçilmiş şablon, şablondan Türkçe ekleri doğru bir satır çıkar. yanki bu satırın nedenini gösterir.
Hafızaya gönderme yapan satır yalnızca konuşanın hatırladığı gerçek bir olaya gönderme yapar.
Aynı seed + aynı girdi = aynı satırlar. Farklı seed ya da farklı sıra ölçülebilir şekilde farklı satırlar verir.
Diyalog açıkken ve kapalıyken dört golden'ın son_hash ve gunluk_hash değerleri birebir aynıdır.

Bu hafta YAZMA:

Kasaba Hanı içeriği, tam şablon kütüphanesi (9. hafta)
yanki validate tam sürümü, yanki coverage (9. hafta)
Blazor demosu, WASM ölçümü (10. hafta)
BenchmarkDotNet tablosu, yanki diff, ablasyon, denge turu (11. hafta)
gerçek LLM renderer'ı (plan 16, v0.2 sonrası)
iki seviyeli followup
anlık görüntü kaydı, hedef zihin katmanı
yalanın kanıt çelişkisinden otomatik yakalanması, kulak misafirliği
devlog (sıradaki devlog #3, 12. hafta)
Karara bağlananlar (Yusuf)

Bunları yeniden seçenek olarak sunma.

Bu değerlerin hepsi değişmez:
Golden son_hash gunluk_hash senaryo_hash
mini-han-1 cee9956ef06df17f 1d54f660948cfc18 7b68157311e70683
mini-karar-1 fe29a63d6dfbc307 a7c3ec331d6a5217 7c36e310cc529867
han-gerilim-1 13e6ccbefed06409 70bbb55256544cd2 aa9ff6767b76478d
frenli-han-1 4aece0e5ce0931bd eeb3bf6effbeaf50 8abd615d6170cd8c
Beklenmeyen değişimde dur, ilk ayrışan olayı göster, onayımı bekle. --guncelle, --hash-ekle ve --senaryo-hash-yenile komutlarını onayımsız çalıştırma.
samples/ altındaki mevcut senaryolar değişmez.
5 ms kararı verildi (optimizasyon + yeniden tanım):
v0.1 kriteri: 8 NPC ortalama tick ≤ 5 ms. 9. haftaya kadar ölçüt gerçekçi fikstür, sonra gerçek Kasaba Hanı senaryosu. 168 tick baştan oynatma bundan türer (≈ 0.84 sn), ayrı kriter değildir.
50 NPC ≤ 5 ms v0.1 kriterinden çıkar, v0.2 hedefi olur. Silinmez: ölçülen 50 NPC değerleri README ve belgelerde açıkça yayımlanır. Plan PDF'i değişmez, sapma ADR-015'e yazılır.
Kriter başka bir onay olmadan yeniden gevşetilmez. Tutmazsa sayı değiştirilmez, kalan fark yazılır.
Gerekçe: 50 NPC gerçekçi fikstür hedefin ≈ 140 katı ve kapanacak bir fark değil. Demo 8 NPC. Asıl risk 8 NPC ve 10. haftadaki geri sarma. WASM kabul sınırı 10. haftada ölçümle belirlenir.
Bu karar 12. haftaya bırakılmaz. Yeniden seçenek olarak sunma.
Yalan gizliliği diyalog için de geçerlidir (ADR-012): diyalog katmanı KuralId, kok, karar izi ya da aktarım kaydı üzerinden bir anlatımın yalan olduğunu öğrenemez.
Konuşan, dinleyenin zihnini okumaz. Şablon kriterleri her zaman konuşanın zihni üzerinden değerlendirilir (ben = konuşan).
LLM karar vermez (ADR-002). Bu hafta LLM kodu ve ağ erişimi yok. IDialogueRenderer yalnızca arayüz ve şablon tabanlı varsayılan uygulamadır.
Performans optimizasyonunda 7. haftanın kuralı geçerli: %30'dan az kazanç sağlayan değişiklik geri alınır. Olay biçimini değiştiren her şeyde dur.
Diyalog mimarisi A kararındaki gibidir, zaman ifadesi E kararındaki gibi göreli, dil F kararındaki gibi bu hafta yalnızca Türkçe gramerdir.
Kod okuyarak bulunan sorunlar

Bunlar görev metni yazılırken koddan çıktı, kod çalıştırılmadı. Planla birlikte doğrula, yanlışsa bana söyle.

Belgeler 5 ms için çelişiyor. DURUM'un 7. hafta bölümü, docs/notes/frenler.md, ADR-013 ve docs/devlog/02-frenler.md kararı 12. haftaya bırakıyor. "Hafta 8 başlangıç işleri" bırakmıyor. Bunlar ayrıca BenchmarkDotNet ölçümünü 12. haftaya yazıyor, plan ve tools/olcum/README.md ise 11. hafta diyor.
"Makine gürültüsü" kanıtlanmadı. Belgeler 5.80 ile 6.58 farkını gürültüye bağlıyor, ama iki ölçümün dağılımı yok. 5.80 ortanca 5 çalıştırma. 6.58'in tekrar sayısı ve commit'i belgelerde yazmıyor. Arada kod değişmiş olabilir. Tek bir --tekrar koşusu süreçler arası gürültüyü de göstermez.
Gerçekçi fikstür 9. haftanın altında. Fikstür 9 eylem ve 12 kural. Plan bütçesi yaklaşık 15 eylem ve 25–35 kural. 6.58 ms, Kasaba Hanı için iyimser olabilir.
Korumalilik.cs içinde ulaşılamayan dallar olabilir. IhlalBayragi.Coz içindeki case Rol.Oyuncu: return string.Empty dalı için üç OkuDizi çağrısı da oyuncu rolüne izin vermiyor. Dolayısıyla yükleyiciden gelinmiyor. Sınıfın kurucusu public olduğu için doğrudan kurulursa boş kimlik döner ve korunan varlık sessizce korumasız kalır. Koverajı süslemek için bu dala test yazma. Önce ulaşılabilir mi bak.
sablon_dosyalari eklenemez. SenaryoYukleyici bilinmeyen alanı reddediyor, çekirdek ise diyalog katmanını tanımamalı. Şablonlar senaryo_hash'e girerse metin düzeltmesi bile eski kayıtları yüklenemez yapar. (Bkz. A.)
Diyalog KuralId okursa yalan sızar. Doğru ve yalan anlatım farklı eylem olduğundan KuralId'leri farklıdır (eylem:<id>). ADR-012 bunu yalnızca geliştirici bilgisi sayıyor. Plandaki niyet alanı "gerekçe: karar izinden ilk 2 ölçüt" de aynı sızıntıyı yaratır, çünkü karar izi seçilen eylemi içerir. (Bkz. B ve H.)
zorunlu varsayılanı false, bu yüzden şablon uydurma gönderme yapabilir. Secici.EnIyiSec kriteri tutmayan ama elenmeyen adayı 0 puanla listede tutar. "Geçen hafta bana yardım ettin" şablonunun hafiza_var kriteri tutmazsa aday genel şablonla 0 puanda eşitlenir ve RNG onu seçebilir. Kurallarda ADR-010 bunu kapatıyor, şablonlarda kapatan bir şey yok. (Bkz. C.)
Satırın gördüğü durum olaya göre değişir. OlayYayinlandi her tick'in sonunda (adım 7) tetiklenir, yani abone tick sonu durumunu görür. Oyuncu eylemi adım 1'de olur ve kuralları adım 3'te aynı tick içinde çalışır, yani satır fikir değişimini görür. NPC eylemi adım 6'da olur ve kuralları sıradaki tick'te çalışır, yani o eylemin satırı dinleyenin tepkisini görmez. (Bkz. A.)
Dışarıdan çözücü kurulamıyor gibi görünüyor. ZihinSorguCozucusu yay deposu istiyor, Simulasyon bunu dışarı vermiyor. Çözücü yalnızca çekirdek içinde kuruluyor. Yanki.Dialogue için salt okunur bir yol gerekir.
hafiza_var yalnızca doğru/yanlış döner. Satırın gönderme yapacağı olayı vermez. Olay Zihin.Hafiza(tick) kayıtlarındaki OlayId ile bulunmalı.
Takvim yok. Plandaki "Geçen pazar" örneği haftanın gününü gerektirir. Motorda yalnızca 1 tick = 1 saat var. (Bkz. E.)
Metin için üç eksik var. Kimlikler ASCII (Ayse), metin Ayşe ister. borclu(Mehmet,Ayse,500) önermesini cümleye çeviren yapı yok. Türkçe ekler (Ayşe'ye, Can'a) Tracery'de yok. (Bkz. F.)
Plan örnekleri sözlükle uyuşmuyor. Plan 5.9'daki inanc(dinleyen, ...) hem sözlükte yok hem kural 6'ya aykırı. Plan 5.7'deki hafıza.var sözlükte hafiza_var. Sözlük geçerlidir.
Yasaklı API taraması Dialogue'u kapsamıyor. YasakliApiTests yalnızca src/Yanki.Core'u tarıyor. Şablon kodunda ToLower() gibi kültüre bağlı çağrılar da risk. Türkçe I/ı farkı tr-TR makinede sonucu değiştirir.
CI düz tests/golden/*.json geziyor. Diyalog golden'ı bu klasörün köküne konursa replay kırılır. (Bkz. I.)
Diyalog kararları ve önerilen varsayılanlar

A, E ve F karara bağlandı, yeniden sunma. B, C, D, G, H ve I önerilen varsayılandır, ONAYIMDAN ÖNCE KODA GİRMEZ: planla birlikte göster, her madde için öneri + ödünleşimi yaz, kararımı bekle. Daha iyi bir seçenek görürsen onu da yaz. Sayılar denge alanımdır.

A. Mimari ve akış (KARAR, plandan sapma):

Yanki.Dialogue, OlayYayinlandi abonesidir. Çekirdek diyalog kavramı bilmez, simülasyon adımları değişmez. Çekirdeğe yalnızca hash'e girmeyen salt okunur sorgu eklenebilir.
Şablonlar senaryodan ayrı dosyadır ve API/CLI ile verilir. senaryo_hash'e girmez, ayrı sablon_hash taşır.
Plandaki "Karar modülü niyet üretir" yerine niyet, karar sonucu olaydan türetilir. ADR-014'e sapma olarak yazılır. Gerekçe: dört golden aynı kalır, diyalog kapatılabilir ve yalan gizliliği çekirdekte korunur.
Satır, tick sonu durumundan üretilir. NPC eyleminin kuralları sıradaki tick'te çalıştığı için dinleyenin cevabı aynı tick'te o eylemin etkisini görmez. Bunun oyun hissine ve followup'a etkisini DURAK 1'de söyle, gerekiyorsa bu olaylar için satırı bir tick geciktirme seçeneğini yaz.

B. Niyet şeması ve eşleme:

Niyet: konusan, dinleyen, niyet (concept), konu (önerme, varsa), ton, fikir_bandi, gerekce, olay_id.
Eşleme şablon dosyasında: olay_turu → { niyet, konusan: fail | hedef }. Eşlemesi olmayan olay satır üretmez. Şablonda eylem kimliği kullanılmaz, eşleme olay türüyle yapılır.
ton baskın duygudan türer. Eşleme tablosu yazar alanındadır. fikir_bandi mevcut fikir_bandi sorgusundan gelir.
gerekce karar izinden değil, konuşanın göstermeye izinli verilerinden üretilir: fikir bandı, baskın duygu, seçilen hafıza olayı, duyduysa kaynağı. anlatti için doğru ve yalan anlatımın niyeti alan alan aynı biçimdedir. Alternatif: plandaki gibi karar izinden ilk 2 ölçüt, ama anlatti ve kriz için maskeleme. Ödünleşim: plana yakın, ama sızıntı riski her yeni eylemde tekrar açılır.

C. Şablon seçimi:

Tek seçim yolu Secici.EnIyiSec, tek kriter okuyucu KriterOkuyucu. İkinci yol açma.
Şablon alanları: id, concept, kriterler, metin, isteğe bağlı konu_deseni, grup, tekrar, sonra, hafiza.
Sorun 7 için öneri: genel şablon (genel: true) dışındaki her şablonda en az bir kriter zorunlu olmalı. hafiza taşıyan şablonda gönderdiği kriter zorunludur. Doğrulayıcı ikisini de hata sayar. Alternatif: şablonlar için ADR-010 gibi "hepsi tutmalı". Ödünleşim: basit ve güvenli, ama Valve tarzı ağırlıklı kısmi eşleşme ölür.
Şablon bağlamında roller ben, konusan, dinleyen, oyuncu. Şablonda olay.* ve eylem.hedef reddedilir.
Reddedilenler: olay_rolu, olay_konusu, konu_puani, gercek. gercek NPC'nin bilemeyeceği dünya gerçeğini metne sızdırır. Konu seçimi yeni sözlük fonksiyonu yerine konu_deseni ile yapılır. Sözlük 18 fonksiyon kalır.
yalan_soyledi(dinleyen) tartışmalı: şablonda serbest bırakılırsa yalancının sözü dışarıdan ayırt edilebilir olur. Bu bir "tell" tasarımıdır, seçim benim. Öneri: bu hafta reddet.
Her concept için tam bir genel şablon zorunlu.

D. Tekrar kontrolleri:

tekrarsiz (grup tükenene kadar tekrar yok, tükenince döngü), tekrar_bekleme (tick), bir_kez.
Durum Yanki.Dialogue içindedir, simülasyona yazılmaz. Satır dizisinden türer ve baştan oynatmayla aynen yeniden kurulur.
Kapsam: varsayılan konuşan başına. bir_kez için alternatif: dünya başına. Ödünleşim: dünya başına seçenek, aynı replik iki NPC'de tekrarlanmaz ama içerik çabuk tükenir.
Tekrar filtresi seçimden önce uygulanır. Genel şablon tekrar sınırı taşıyamaz, doğrulayıcı reddeder. Böylece satırsız niyet kalmaz.
RNG anahtarı olay kimliğini içerir: aynı tick'te aynı NPC'nin iki satırı aynı zarı çekmez. Çekirdek RNG akışına dokunmaz.

E. Hafıza göndermeleri ve zaman ifadesi (zaman ifadesi KARAR, kalanı varsayılan):

Şablon hafiza: { "tur", "fail", "son_n_tick" } bildirir. Eşleşen kayıt konuşanın Hafiza(tick) listesinden seçilir, dünya günlüğünden değil. Silik kayıt görünmez. Birden çoksa en yeni, eşitse en düşük olay kimliği.
hafiza_var kriteri ve hafiza alanı aynı aralığı kullanır. Uyuşmazlığı doğrulayıcı yakalar.
Metinde #hafiza.zaman#, #hafiza.fail# yuvaları olur. Yuva bağlanamazsa şablon aday olamaz.
Zaman ifadesi (KARAR): göreli ifade (bugün, dün, üç gün önce). Takvim yok, senaryo şeması değişmez, oyun içi günden hesaplanır. Eşik sayıları ayar sınıfında durur.

F. Mini gramer ve dil (dil KARAR, kalanı varsayılan):

Tracery benzeri küçük uygulama, Yanki.Dialogue içinde. #kural# açılımı, kural başına seçenekler, en fazla iç içe 3 seviye, döngü yükleme hatası.
Rastgelelik yalnızca anahtarlı RNG'den. System.Globalization kültürüne bağlı hiçbir dönüşüm yok.
Türkçe ek değiştiricileri: .yonelme, .belirtme, .bulunma, .ayrilma, .iyelik. Ünlü uyumu ve kaynaştırma elle tablo. Özel ada kesme işareti, cins isme kesme işareti yok.
Altın tablo elle yazılır: Ayşe'ye, Ayşe'yi, Ayşe'de, Ayşe'den, Mehmet'e, Mehmet'i, Can'a, Hatice'ye, Selim'e, Ali'ye, hancıya, muhtara. Yanlış ek sessizce üretilmez.
Sayıya ek bu hafta yok: 500'e gibi kullanım doğrulayıcı hatası. Gerekçe: ek okunuşa bağlı.
adlar tablosu kimliği görünen ada çevirir. onerme_metinleri yüklem başına cümle verir: borclu(a,b,n). Tanımsız yüklem doğrulayıcı hatası. degil(...) için ayrı metin zorunlu.
Dil (KARAR): yapı iki dile hazırdır (dil alanı, dosya başına tek dil). Bu hafta yalnızca Türkçe gramer ve Türkçe test içeriği yazılır, İngilizce şablonlar 9. haftaya kalır.

G. Followup:

Şablonda sonra: { "hedef": "dinleyen" | "<kimlik>", "concept": "..." }. Yalnızca aynı anda aynı mekandaki NPC'ye.
Tek seviye: followup satırının sonra alanı yok sayılmaz, doğrulayıcı reddeder.
Followup yalnızca metindir. Olay üretmez, fikir değiştirmez, simülasyona dönmez.
Hedefin kriterleri kendi zihniyle değerlendirilir.

H. IDialogueRenderer:

Girdi yalnızca beyaz liste: niyet, seçilen şablon metni, seçilmiş gerçekler, gerekçe. Simulasyon, Zihin, Olay, karar izi ya da KuralId geçmez. Bunu reflection testiyle kilitle.
Çıktı tek yönlüdür, simülasyona ve tekrar durumuna geri yazılmaz.
diyalog_hash her zaman varsayılan renderer'ın çıktısından hesaplanır. Böylece deterministik olmayan bir eklenti replay'i bozmaz.
Eş zamanlı imza mı, Task mı, öneri ve gerekçe yaz. Ödünleşim: LLM eklentisi ağ kullanır, ama kullanıcı arayüzü API'si eş zamanlı daha basittir.

I. Doğrulama, CLI ve golden:

Şablon doğrulayıcı v0: dosya:satir: sablon "id": mesaj. yanki validate tam sürümü 9. haftada, ama kod onun kullanabileceği biçimde yazılır.
CLI: yanki diyalog <kayit.json> --sablon <dosya> satırları yazar. --neden <satir_no> seçilen şablonu, tutan/tutmayan kriterleri, elenen şablonları ve olaya bağlı why zincirini gösterir.
Diyalog golden'ı tests/golden/diyalog/ altına, ayrı biçimde girer: kayıt yolu, şablon yolu, diyalog_hash, sablon_hash. Mevcut dört golden dosyasına alan eklenmez. CI'a ayrı adım eklenir.
Diyalog hash'i satırları (olay kimliği, konuşan, dinleyen, şablon, metin) kapsar. CRLF/LF aynı değeri verir.
Değişmez kurallar
Yanki.Core ve Yanki.Dialogue: LangVersion 9.0, NuGet paketi yok, iki hedefte derlenir.
Determinizm kuralları Yanki.Dialogue için de geçerlidir: System.Random, DateTime.Now, Guid.NewGuid, GetHashCode/HashCode yasak. Sırasız gezinti yasak, float/double yasak. Tek RNG AnahtarliRng. Metin karşılaştırması ordinal, biçimlendirme InvariantCulture.
Çekirdek Yanki.Dialogue'u tanımaz. Testle kilitle.
Yanki.Dialogue simülasyon durumunu değiştiren hiçbir API'yi çağırmaz (Submit, Tick, zihin yazımı). Testle kilitle.
Olay tipine yeni alan ekleme. Yeni olay türü ekleme. Gerekirse dur ve sor.
TDD: her parça için önce kırmızı test.
Kod tanımlayıcıları ve JSON anahtarları ASCII. Türkçe karakter yalnızca metinlerde.
Çekirdeğe ve Dialogue'a dosya sistemi ya da ağ erişimi ekleme. Dosyayı CLI okur.
JSON için mevcut Yanki.Core.Json. Kriter için mevcut KriterOkuyucu. Metin içeren dosyada hata mesajı dosya ve satır gösterir.
Bölüm 1: Performans ve 7. haftadan kalanlar (önce bunu yap)

1. 5.80 ve 6.58 ölçümlerini açıkla

Kod değiştirme. Kod yorumu ya da belge de henüz değiştirme.

İki sayının commit, komut, tekrar sayısı ve hesap biçimini git geçmişinden bul. Bulamadığın alanı "kayıtlı değil" yaz, tahmin etme.
Aynı commit'te A/A ölçümü yap: --gercekci --npc 8 ile ayrı süreçte en az 20 koşu. Ortanca, en düşük, en yüksek ve p10–p90 yaz.
6.58 başka commit'teyse A/B ölçümü yap: iki derlemeyi dönüşümlü sırayla, en az 20'şer süreç koş. Aynı sırayla peş peşe koşma.
Fark A/A dağılımının içindeyse "gürültü" yaz. Dışındaysa kök nedeni bul: kod, fikstür, komut ya da ölçüm yöntemi.
Koşullar: dizüstü şarjda, güç modu sabit, ağır başka iş yok. Bunu rapora yaz. Yapamadığın koşulu belirt.
Ortalama ms/tick ile birlikte en yavaş tick ve p95'i de raporla. Gerekirse tools/olcum'u genişlet (çözüm dışında, ölçüm aracı). Stres fikstürünü değiştirme. 2. Maliyetin nerede olduğunu ölç
8 ve 50 NPC gerçekçi fikstürde tick adımlarına göre süre dökümü çıkar. tools/olcum/README.md geçici zamanlama kodunu repoya sokmaz: yerelde yap ve commit'leme, ya da dotnet-trace kullan (repoya paket girmez).
"Han'a kümelenme algı hacmini büyütüyor" hipotezini doğrula ya da çürüt.
Plan bütçesine yakın sahte varyant ölç (--gercekci yanında ayrı mod, yaklaşık 15 eylem ve 30 kural). Kasaba Hanı içeriği yazma, yalnızca hacim.
168 tick baştan oynatma süresini 8 ve 50 NPC için yaz. 10. haftadaki geri sarma bu maliyeti öder. WASM ölçümü yok, çarpan için sayı uydurma. 3. Optimizasyon turu (5 ms kararı)
Tek tur, en çok iki aday. Adaylar 2. adımdaki profilin en büyük payından seçilir, tahminle seçilmez.
Hedef: gerçekçi fikstür 8 NPC ≤ 5 ms (1. adımın A/A yöntemiyle). 1. adım hedefin zaten tuttuğunu gösterirse bu turu atla.
Hash ve olay biçimi değişmez. Z4 ve olay biçimi değiştiren her şey 11. hafta adayı kalır.
%30 kuralı geçerli. Tutmayan aday geri alınır, sonucu docs/notes/frenler.md'ye yazılır.
Hedef tutmazsa sayıyı gevşetme, üçüncü aday deneme. Kalan farkı yaz, Bölüm 2'ye geç. 9. haftada gerçek senaryoda yeniden ölçülür.
Sonucu DURAK 1'de göster: önce/sonra sayılar, plan bütçesine yakın varyant, 168 tick oynatma ve 50 NPC değerleri. Bütçe varyantı 10 ms'yi aşıyorsa açıkça uyar. 4. Belgeleri tek ifadeye indir

Kaynak yapıldığı yerde değişir, sayılar güncellenir.

docs/DURUM.md (7. hafta "5 ms HEDEFİ" maddesi, 5. ve 6. hafta açık kararları, "Hafta 8 başlangıç işleri"), docs/notes/frenler.md, ADR-013, docs/devlog/02-frenler.md ve tools/olcum/README.md.
8 NPC v0.1 kriteri ile 50 NPC v0.2 hedefi ayrı yazılır. Stres ve gerçekçi fikstür ayrı etiketlenir.
"12. haftada karar" ifadesi kalkar. BenchmarkDotNet haftası plana göre düzeltilir (11. hafta).
Devlog geçmiş kaydıdır. Yeniden yazma, gerekiyorsa tarihli düzeltme notu ekle.
ADR-015 yaz (5 ms kriteri): karar, plandan sapma, 1. ve 2. adımın sayıları, reddedilen seçenekler. ADR-013 ilgili maddede buna bağlanır.
README'nin başarı/ölçek ifadeleri 8 NPC kriterini ve ölçülen 50 NPC değerini doğru yazar. 5. Korumalilik.cs testleri ve kod incelemesi maddeleri
Önce coverage aracıyla kapsanmayan satırları gerçekten listele. %68,65'in neyin yüzdesi olduğunu doğrula.
Ulaşılabilir dal için kırmızı test yaz. Ulaşılamaz dal için sil ya da açık hata fırlat, hangisini seçtiğini söyle. Korumayı sessizce kapatan dal kalmasın (sorun 4).
haftanın açık Minor maddeleri:
3 (KaynakToplayici yay dalı): kırmızı testle düzelt.
5 (kural ve eylem kimliği çakışması): önce test. Sorun gerçekse düzelt, değilse kapat.
8 (korumali anahtarları): önce mevcut durumu doğrula, Olustur bazı durumları zaten reddediyor. Yazım hatası hâlâ sessizse kapat.
4 (konum! ihlalinin eylemde yarım uygulanması): seçenekleri DURAK 1'de sun: eylem tamamen düşer ya da belgeli olarak kalır. Bana sormadan değiştirme.
6 (tanimsiz_rol ve kisi_olmayan_hedef hash etkisi): authoring-guide.md ve ADR-013'e yaz.
7 (ZincirDerinligi belleği): yalnızca profil anlamlı gösterirse DURAK 1'de seçenek olarak öner.
Hash değişirse dur. 6. ADR-013 onayı
ADR-013'ü 4. ve 5. adımlardan sonra güncel hale getir.
Bana kısa özet sun: kararlar, plandan sapmalar, sınırlar, açık maddeler.
"ADR-013'ü onaylıyorum" yazarsam durumu "Kabul edildi" yap ve DURUM'daki maddeyi kapat. Bu cümle CLAUDE.md'deki "durumları sen değiştirmezsin" kuralına bu ADR için açık talimattır. Yazmazsam "Önerildi" kalır.
Bölüm 2: Diyalog 7. İskelet ve mimari testleri
DialogueInfo yer tutucusu kalkar, ArchitectureTests yorumu güncellenir.
Test: çekirdek Dialogue'a bağımlı değil, Dialogue yazma API'si çağırmıyor.
YasakliApiTests src/Yanki.Dialogue'u da tarar.
Çekirdeğe gerekli salt okunur API (A kararına göre). Hash'e etkisi olmadığını altın değerlerle göster. 8. Şablon dosyası ve doğrulayıcı

C, D, F, G ve I kararlarına göre. Eşleme, adlar, onerme_metinleri, gramer ve şablonlar tek bir yükleyiciden okunur. Hatalar dosya:satir, bütün hatalar birlikte listelenir.

9. Niyet üretimi

A ve B kararlarına göre.

Test: eşlemesiz olay satır üretmez; oyuncu eylemi hedef NPC'ye satır üretir; doğru ve yalan anlatım aynı biçimde niyet verir; niyette KuralId ve eylem kimliği yok.

10. Şablon seçimi ve tekrar kontrolleri

C ve D kararlarına göre.

Test: spesifik varyant geneli yener; zorunlu tutmayınca elenir; 0 puan eşitliğinde uydurma gönderme seçilmez (sorun 7 için önce kırmızı); tam pencere sınırında tekrar_bekleme; bir_kez; grup tükenince; aynı tick iki satırda farklı zar.

11. Hafıza göndermeleri

E kararına göre.

Test: yalnızca hatırlanan olay; silik kayıt yok; başkasının hafızası okunmaz; duymadığı olaya gönderme yok; zaman ifadesinin her sınırı; en yeni kuralı.

12. Mini gramer

F kararına göre. Altın tablo elle yazılır, koddan kopyalanmaz. tr-TR ve invariant kültürde aynı çıktı.

13. Followup

G kararına göre.

Test: tek seviye; aynı mekan koşulu; hedef kendi zihniyle seçilir; simülasyona etki yok.

14. IDialogueRenderer

H kararına göre. Varsayılan şablon renderer'ı ve beyaz liste reflection testi.

15. CLI

I kararına göre. --neden çıktısı why ile aynı dili konuşur.

16. Senaryolar (İÇERİK BENİM ALANIM)

Motor testlerini fikstürle yaz. samples/ altına ya da bana görünen şablon metinlerine geçmeden dur ve sun (DURAK 2):

Küçük bir şablon seti için 2–3 seçenek. Mevcut senaryoyu değiştirmeden, yan dosya olarak (HanGerilim öneri, başka önerin varsa yaz). Her seçenekte: eşlenen olaylar, niyetler, göstereceği davranışlar, 3–4 örnek satır.
Sıra etkisini satırda gösterecek 2–3 seçenek. Plan 7 Durum 1/Durum 2 biri olmalı: oyuncu Can'la söylentiden önce mi sonra mı konuşuyor? Beklenen satır farkını yaz.

Ben seçince değişmezleri koşudan ÖNCE test olarak yaz. Yer tutucu sayılar ve metinler README'de belirtilir. Diyalog golden'ı tests/golden/diyalog/ altına girer.

17. Değişmezler

CsCheck, rastgele geçerli eylem dizileri, CI'da makul ve sabit tohumlu:

diyalog açık ve kapalı: son_hash ve gunluk_hash aynı,
aynı seed ve girdi: aynı satır dizisi ve diyalog_hash,
"N tick koş, kaydet, yükle, M tick koş" ile "N+M tick koş": aynı satırlar,
k tick'e kadar oynatma: tam oynatmanın ilk k tick'lik satırlarıyla birebir aynı,
her satırın olay kimliği günlükte var,
hafiza şablonunun olayı konuşanın hafızasında var,
tekrar_bekleme ve bir_kez ihlal edilmiyor,
doğru ve yalan anlatım diyalog niyetinde ayırt edilemiyor (yalanın gerçekten söylendiği senaryoda, testin boş geçmediğini denetle),
tr-TR ve invariant kültürde aynı diyalog_hash.

PR öncesi CI dışında geniş koşu yap: 7. haftadaki gibi 5 tohum × 200 örnek. Tohumları ve sonucu DURUM'a yaz.

18. Ölçüm (DURAK 3)

8 NPC gerçekçi fikstür, diyalog kapalı ve açık, aynı A/A yöntemiyle: ms/tick, satır başına süre, 168 tick oynatma, bellek. Diyalog 8 NPC kriterini aşırıyorsa söyle. Bir sonraki optimizasyonu kendin seçme.

19. Dokümantasyon
    ADR-014 (diyalog): akış, niyet, plandan sapmalar, şablon seçimi, tekrar durumu, yalan gizliliği, renderer sınırı, hash'ler.
    ADR-014 "Gerekçe (Yusuf)" bölümünü bu görev metninin deviriyle yaz: plandan ve kararlarımdan derle, başa devir notu düş, uydurma gerekçe yazma. Durum "Önerildi" kalır. CLAUDE.md'deki istisna satırına 8. hafta: ADR-014 ekle. ADR-015'e de aynı kural uygulanır ve aynı satıra ADR-015 eklenir. Devir sonraki ADR'lere uzanmaz.
    Yeni docs/notes/diyalog.md, src/Yanki.Dialogue/README.md.
    docs/criteria-language.md (şablon bağlamı, roller, reddedilenler), docs/authoring-guide.md (şablon yazımı, eşleme, gramer, ekler, hafıza, followup), docs/architecture.md, tests/golden/README.md, CHANGELOG, README durum satırı.
    docs/DURUM.md: 8. hafta, kararlar, açık kararlar, 9. haftaya taşınanlar, sıradaki hafta. "Hafta 8 başlangıç işleri" bölümü kapanır.
    Yusuf'a bırak

Şunlarda karar verme, seçenek sun:

Önerilen varsayılanlar (B, C, D, G, H, I).
Sözlük değişiklikleri.
Bana görünen şablon metinleri, senaryo içeriği ve denge sayıları (ton tablosu, zaman eşikleri dahil).
Olay biçimini ya da herhangi bir golden'ı değiştiren her şey.
Kapsam kesme. Plan 12'nin sırası: önce followup, sonra mini gramer. Hafta sığmıyorsa dur ve sor, kendiliğinden kesme.
Çalışma şekli
Her şeyi oku. "Kod okuyarak bulunan sorunlar" listesini doğrula. superpowers:writing-plans ile adım adım plan çıkar. Bölüm 1'in 1–3. adımlarını yap. DURAK 1'de ölçüm ve optimizasyon raporunu, planı, önerilen varsayılanları (B, C, D, G, H, I) ve Minor 4 seçeneklerini birlikte göster, ONAYIMI BEKLE.
Onaydan sonra superpowers:test-driven-development ile çalış: önce kırmızı test, her parça küçük commit, commit sonuna Co-Authored-By. Hata ayıklarken superpowers:systematic-debugging.
"Karara bağlananlar" dışında plan, CLAUDE.md ve bu görev metni çelişirse dur ve sor.
Duraklar: DURAK 1 (ölçüm + plan + varsayılanlar + Minor 4), beklenmeyen hash değişimi, DURAK 2 (içerik), DURAK 3 (ölçüm).
Bitirmeden superpowers:verification-before-completion:
dotnet build Yanki.slnx -c Release iki hedef, 0 uyarı.
dotnet test Yanki.slnx -c Release geçiyor.
Dört golden replay ile doğrulanmış, tablodaki değerler aynı.
Diyalog golden'ı doğrulanmış.
Komut çıktılarını göster.
PR'dan önce superpowers:requesting-code-review ile diff'i determinizm, kültür bağımlılığı, tek yönlü akış, yalan sızıntısı, NPC bilgi sınırı ve hash korunumu açısından gözden geçir. Önemli bulguları düzelt, kalanları DURUM'a yaz.
Push et, hafta-08 → main PR'ı aç. Açıklamada ne yapıldığını, açık noktaları ve kontrol noktasının nasıl doğrulandığını yaz.
CI'ın son commit'te yeşil olduğunu doğrula ve PR linkini ver. Merge etme.
Son mesajında şunlar olsun: ne yaptın, hangi kararları sen verdin, benim yapmam gerekenler.
