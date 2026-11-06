# Kasaba Hanı: NPC'ler

**TASLAK** (Claude Code, 9. hafta; Yusuf seçti: "A: Yorgun han", alacaklı Rıza). Bu tablo Yusuf'un tasarım alanıdır: Yusuf düzeltir, kişilik sayıları yer tutucudur ve 11. hafta denge turunda ayarlanır.

8 NPC (en fazla 10). Başlangıç mekanı: Rıza, Mehmet, Ayşe, Kerem, Osman, Can, Fatma ve Dursun han'dadır. Yalnızca sosyalliği 0.5 ve üstü olan NPC'ler dolaşır (`git` eyleminin ön koşulu); Mehmet, Osman ve Dursun yerinde kalır, Rıza `mekandan_ayrilamaz`.

Kişilik sırası: açgözlülük, dürüstlük, cesaret, sosyallik, kin tutma, merhamet.

| Kimlik (görünen ad) | Rol               | Ne istiyor                                                                    | Neyi saklıyor                                                                  | Kişilik                       | Kiminle çarpışır                                |
| ------------------- | ----------------- | ----------------------------------------------------------------------------- | ------------------------------------------------------------------------------ | ----------------------------- | ----------------------------------------------- |
| `Riza` (Rıza)       | Hancı             | Han'ı sakin tutmak, Mehmet'ten 500 lirasını almak (`borclu(Mehmet,Riza,500)`) | Kasadaki paranın eksik olduğunu                                                | .4 .7 .5 .7 .4 .6             | Kerem (güven), Mehmet (borç)                    |
| `Mehmet` (Mehmet)   | Borçlu çiftçi     | Borcunu ertelemek                                                             | Borç sıkıntısını (kimseye anlatmaz, ama yüzünden belli olur: `borc_sikintisi`) | .5 .5 .4 .3 .5 .6             | Rıza (borç), Ayşe (dedikodu)                    |
| `Ayse` (Ayşe)       | Dedikoducu terzi  | Her şeyi ilk duymak ve anlatmak                                               | Haberi kimden duyduğunu ve kesesinin ne kadar dolu olduğunu (`para_say`)       | .5 .5 .4 **.9** .4 .5         | Mehmet, Kerem                                   |
| `Kerem` (Kerem)     | Şüpheli yabancı   | Kasabada kalıcı bir yer ve para                                               | Geçmişini; kese işini                                                          | **.8** .3 **.8** .6 .5 **.2** | Osman (dürüstlük), Can (merhamet), Dursun (kin) |
| `Osman` (Osman)     | Muhtar            | Düzeni ve itibarını korumak, mührünü bulmak                                   | Mührü kaybettiğini                                                             | .6 .7 .3 .45 .4 .4            | Kerem                                           |
| `Can` (Can)         | Mehmet'in kardeşi | Ağabeyini korumak (`kardes(Mehmet,Can)`)                                      | Ağabeyini ele verme isteğinin ağırlığını (merhameti yüksek, stresi artar)      | .3 .8 .6 .5 .6 **.8**         | Ayşe (iftira), Kerem, oyuncu (Durum 2)          |
| `Fatma` (Fatma)     | Han aşçısı        | Huzur                                                                         | Mührü temizlikte bulup depoya (`yeri(muhur,depo)`) kaldırdığını                | .3 **.9** .4 .6 .2 .8         | Dursun (kin tutma .2 / .9)                      |
| `Dursun` (Dursun)   | Nalbant           | Düzen ve hesaplaşma                                                           | Kerem'e duyduğu kini                                                           | .6 .4 .8 .4 **.9** .3         | Fatma (kin), Kerem                              |

## Kişilik çarpışmaları (hangi iki NPC hangi özellikte zıt)

| Çift           | Özellik                            | Değerler |
| -------------- | ---------------------------------- | -------- |
| Fatma – Dursun | kin tutma                          | .2 / .9  |
| Ayşe – Mehmet  | sosyallik                          | .9 / .3  |
| Kerem – Osman  | dürüstlük                          | .3 / .7  |
| Kerem – Can    | merhamet                           | .2 / .8  |
| Kerem – Dursun | cesaret (ikisi de yüksek): çatışır | .8 / .8  |

## NPC'lere kimlik bağlama notu

Eylem şemasında "yalnızca şu NPC yapabilir" alanı yoktur. İlgili eylemler (ör. `muhur_sor`, `toplanti_cagir`, `misafir_agirla`, `kardesi_ele_ver`) kişilik ön koşuluyla tek bir NPC'ye daralır; hedef bağlama ise kural içi `gercek(...)` ve `modifier_var(...)` zincirleriyle yapılır (`borc_sikintisi` → `borclu_gordum` → `borc_iste`; `para_say` → `kese_goruldu` → `kese_calmak`). Ayrıntı README'de.
