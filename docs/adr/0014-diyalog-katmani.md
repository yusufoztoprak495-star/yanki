# ADR-0014: Diyalog katmanı: olaydan niyet, kriterle şablon seçimi, gramer, ayrı şablon dosyası

**Durum:** Önerildi
**Tarih:** 2026-11-05
**Kabul eden:** —
**Kabul tarihi:** —

## Bağlam

Plan bölüm 5.12 NPC'nin söylediğinin de "kanıtlanabilir" olmasını ister: motor her sonucun nedenini kanıtlar, diyalog da buna dayanmalı ve **LLM karar vermemelidir** (ADR-002). Plan 8. hafta işini `Yanki.Dialogue`
olarak verir: niyetten tek şablon seçimi, tekrar kontrolü, hafıza göndermesi, mini gramer, tek seviye followup, `IDialogueRenderer`. Üç kısıt vardı: (1) simülasyon durumu **hash'i** diyalogdan etkilenmemeli (diyalog açık
ya da kapalı, dört golden'ın `son_hash` ve `gunluk_hash` değerleri aynı kalır); (2) ADR-012 yalan gizliliği: bir NPC'nin yalanı, söylenişinden ayırt edilememeli; (3) ADR-004: `Yanki.Dialogue` `netstandard2.1;net10.0`,
LangVersion 9.0, NuGet'siz.

Bu ADR'nin "Gerekçe (Yusuf)" bölümü görev metninde Claude Code'a devredilmiştir (8. hafta: ADR-014 ve ADR-015); aşağıdaki not bunu belirtir. ADR, Yusuf onaylayana kadar "Önerildi" kalır.

## Karar

1. **Niyet çekirdekten değil, OLAYDAN türer** (plandan sapma: plan karar modülüne bağlıyordu). `Yanki.Dialogue`, `Simulasyon.OlayYayinlandi` abonesidir; çekirdeğe yalnızca hash'e girmeyen, salt okunur
   `Simulasyon.SorguCozucusu()` eklenmiştir. Niyet yalnızca olay türünden, fail/hedefinden, `onerme` yükünden ve konuşanın zihnindeki fikir bandı ile baskın duygudan türer; `KuralId`, `kok`, `NedenOlaylar` ve karar izi
   niyete GİRMEZ (yalan gizliliği; doğru ve yalan anlatım alan alan aynı niyettir).
2. **Şablonlar senaryodan ayrı bir dosyadır** (`sablonlar.json`). `senaryo_hash`'e girmez; kendi `sablon_hash`'i vardır (metin düzeltmesi eski kayıtları yüklenemez yapmaz). Diyalog hash'i (`diyalog_hash`) yalnızca
   varsayılan renderer'ın çıktısından hesaplanır; `yanki diyalog-replay` ayrı bir golden biçimi kullanır (dört mevcut golden'a alan eklenmez).
3. **Şablon seçimi, ADR-003'ün tek kriter dilidir:** `Secici.EnIyiSec`, konuşanın zihni (`ben` = konuşan) üzerinden. Genel (`genel: true`) şablon her niyette zorunludur ve zorunlu kriter taşıyamaz; genel olmayan
   şablonda en az bir `zorunlu` kriter vardır ("ağırlıklı kısmi eşleşme" yazılmadı: bir şablon ya uygundur ya değildir). Eşitlikte en düşük şablon kimliği; seçim anahtarlı RNG'den (`diyalog:<olay>[:sonra]`) çekilir, çekirdek
   RNG akışına dokunmaz.
4. **Şablon bağlamında reddedilen sorgular:** `olay_rolu`, `olay_konusu`, `konu_puani`, `gercek`, `yalan_soyledi` (her bağlamda: aktarım kaydını okur, yalancının sözünü ayırt ettirir); anlatıcı konuşanın olduğu
   niyetlerde (NPC'nin kendi anlatımı) ek olarak `inanc`, `kaynak`, `hafiza_var` ve `hafiza` alanı. Oyuncunun anlatımına cevap veren NPC'de (konuşan anlatıcı değil) `inanc/kaynak/hafiza` serbesttir. `stres()` serbesttir.
5. **Eşleme olay türü + kaynak (oyuncu | npc) ile yapılır;** dinleyensiz niyet (`kacti`, `sakinlesti`) satıra mekân taşır, dinleyeni gören demo süzer; dinleyensiz niyette `dinleyen` rolü, `#dinleyen#` yuvası ve
   followup hedefi `dinleyen` doğrulayıcı hatasıdır.
6. **Tekrar:** `tekrar_bekleme` (tick − son ≥ N), `bir_kez` (konuşan başına), `tekrarsiz` grupları (tükenince döngü); durum satır dizisinden türetilir, kayıttan yükleme yeniden oynatır. **Hafıza göndermesi:** konuşanın
   `Zihin.Hafiza(tick)` kaydından (en yeni, eşitlikte en düşük id; tetikleyen olay hariç), `son_n_tick` dâhil pencere. Zaman ifadesi `tick/24` gün farkıyla (bugün, dün, evvelsi gün, yazıyla "üç gün önce", geçen hafta,
   uzun zaman önce).
7. **Followup tek seviyedir:** NPC kaynaklı olayın cevabı bir sonraki tick yayınında üretilir (bekleyen liste diyalog durumundadır, kayıttan yüklemede yeniden kurulur); oyuncu kaynaklı olayın cevabı hemen. Hedef ve
   aynı-mekân denetimi üretim anında yapılır.
8. **Mini gramer:** `#kural#` ve `#olgu.ek#` yuvaları, en fazla 3 iç içe seviye, döngü hatadır. Türkçe ek motoru elle tablodur ve kültürden bağımsızdır: `yonelme, belirtme, bulunma, ayrilma, iyelik` (3. tekil kişi),
   `tamlayan`, `baslik`. Yumuşak l adları (`ince`), p/ç/t/k ile biten cins isimde yazarın verdiği yumuşak gövde (zorunlu; özel adda yumuşama yok), kişi zamirleri (bana/sana, beni/seni …; özne konumunda elle yazılmış
   ben/sen varyantları, motor fiil çekimi yapmaz). Satırın ilk harfi otomatik büyür (i → İ, ı → I, kendi tablosuyla). Yanlış ek sessizce üretilmez: açık hata.
9. **`IDialogueRenderer`:** `string Render(RenderGirdisi)`; renderer'ın göreceği her şey beyaz listeli bir `RenderGirdisi`'dir (niyet, seçilen şablon metni, seçilmiş gerçekler, gerekçe) ve bir yansıma (reflection)
   testi bunu kilitler. LLM renderer ileride bu arayüzün opsiyonel bir eklentisi olabilir; karar vermez.
10. **Sıra etkisi diyalogda görünür:** aynı iki oyuncu eylemi, farklı sırada, konuşanın zihnindeki modifier'lar üzerinden farklı satır seçer (`SiraEtkisiSoylenti`: yardım önce → minnet satırı; söylenti önce →
    güvensizlik satırı). Bu bir yan etki değil, seçimin kanıtlanabilirliğidir (`yanki diyalog --neden`).

**Plandan sapmalar:** (a) niyet karar modülünden değil olaydan; (b) "ağırlıklı kısmi eşleşme" yazılmadı; (c) şablon dosyası senaryodan ayrı ve kendi hash'i var; (d) diyalog golden'ı ayrı biçim.

## Değerlendirilen seçenekler

### Seçenek A: Olay abonesi + ayrı şablon dosyası (seçildi)

| Boyut             | Değerlendirme                                                                |
| ----------------- | ---------------------------------------------------------------------------- |
| Karmaşıklık       | Orta                                                                         |
| Maliyet           | Çekirdek kodu neredeyse değişmez (salt okunur `SorguCozucusu`); hash korunur |
| Ölçeklenebilirlik | 8 NPC'de ölçülen ek maliyet gürültü içinde; 50 NPC v0.2                      |
| Bilinirlik        | Eylem ve kural dosyalarıyla aynı biçim ve kriter dili                        |

**Artıları:** hash'ten bağımsız; yalan gizliliği yapısal; metin düzeltmesi kayıt bozmaz. **Eksileri:** niyet kararın "nedenini" değil olayı okur; karar izi `--neden` ile ayrıca gösterilir.

### Seçenek B: Karar modülünden niyet çıkarmak (plandaki ilk fikir)

Karar modülü eylemin seçilme nedenini bilir; ama doğru anlatım ile yalan farklı eylemdir, bu yüzden niyet veya gerekçe yalanı ele verirdi (ADR-012). Reddedildi.

### Seçenek C: Şablonu senaryo dosyasına gömmek

Her metin düzeltmesi `senaryo_hash`'i ve dolayısıyla tüm kayıtları bozardı. Reddedildi.

## Ödünleşimler

- **Hız:** diyalog açık/kapalı fark 8 NPC'de ölçüm gürültüsü içindedir (bkz. `docs/notes/diyalog.md`); replay ve tick başına ölçümleri tutarlı hale getirme çalışması ADR-015'e bağlıdır.
- **El yazısı gramer:** yalnızca Türkçe ve seçilen ek kümesi; fiil çekimi yok. Kasaba Hanı metinlerinde gerekirse varyant elle yazılır.
- **Metin kalitesi** sayısal bir ölçüt değildir; şablon içeriği Yusuf'un tasarım alanındadır.

## Gerekçe (Yusuf)

> **Devir notu:** Bu bölümü Claude Code, Yusuf'un 8. hafta görev metnindeki devriyle yazdı. Plandan (bölüm 5.12, 8. hafta) ve Yusuf'un görev ile DURAK 1 / DURAK 2 onay mesajlarındaki kararlarından derlenmiştir;
> Yusuf'un yazmadığı bir gerekçe uydurulmamıştır. Yusuf onaylayana kadar ADR "Önerildi" kalır.

Plan, motorun her sonucun nedenini kanıtlamasını ve LLM'in karar vermemesini ister; bu yüzden niyet kural ve olaydan türer, metin seçimi aynı kriter diliyle yapılır, LLM yalnızca opsiyonel bir renderer olabilir.
Yusuf'un DURAK 1 düzeltmeleri: anlatma gerekçesi yalnızca fikir bandı ve baskın duygudur (hafıza olayı ve kaynak yalanı ele verirdi); "ağırlıklı kısmi eşleşme" yazılmaz; `yalan_soyledi` hiçbir şablon bağlamında kullanılmaz;
`tekrar_bekleme` eşiği `tick − son ≥ N`'dir; `bir_kez` konuşan başınadır; followup bekleyen listesi diyalog durumundadır; `IDialogueRenderer` `string Render(RenderGirdisi)`'dir; `KuralId` yalnızca `--neden`'de görünür.
DURAK 2'de: Kasaba Hanı'ndan bağımsız bir şablon kümesi (HanGerilim, ~20 şablon), sıra etkisi için SiraEtkisiSoylenti (Seçenek A), ton eşikleri, zaman ifadesi etiketleri, otomatik büyük harf, kaynaklı eşleme,
dinleyensiz niyetler, kişi zamirleri ve ünsüz yumuşaması Yusuf'un kararlarıdır.

## Sonuçlar

- Kolaylaşan: şablon yazarı yalnızca JSON ve kriter dilini bilir; her satırın nedeni `--neden` ile görülür; diyalog golden'ı metin regresyonunu yakalar.
- Zorlaşan: ek ve zamir kuralları el tablosudur; yeni dil eklemek yeni tablo ve yeni test demektir.
- Yeniden bakılacak: diyalog ölçümü gerçek Kasaba Hanı hacminde (9. hafta); ton eşikleri 11. hafta denge turunda; LLM renderer eklentisi (opsiyonel, v0.2 sonrası).

## Yapılacaklar

1. [ ] Yusuf: ADR'yi gözden geçir; onaylıyorsa durumu "Kabul edildi" yap.
2. [ ] 9. hafta: Kasaba Hanı için gerçek şablon kümesi; diyalog maliyetini gerçek senaryoda yeniden ölç.
3. [ ] 11. hafta: ton eşikleri ve tekrar sayıları denge turunda.
