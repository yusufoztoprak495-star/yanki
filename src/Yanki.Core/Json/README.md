# Json

Bağımlılıksız JSON okuyucu/yazıcı ([ADR-009](../../../docs/adr/0009-cekirdekte-bagimsiz-json-okuyucu.md)). Kural, senaryo ve oturum dosyaları için.

| Tip | Ne yapar |
|---|---|
| `JsonAyristirici.Ayristir(metin)` | Katı JSON okur; sayılar `decimal`, nesne sırası korunur, tekrar eden anahtar hata, `JsonHatasi` satır/sütun taşır. |
| `JsonDegeri` | Değişmez ağaç düğümü (`Tur`, `Satir`, `Sutun`, `Al(anahtar)`, `Elemanlar`, `Ozellikler`); programla kurma fabrikaları. |
| `JsonYazici.Yaz(deger)` | Kanonik çıktı: 2 boşluk girinti, özellikler verilen sırayla, sonda `\n`. |

Çekirdek dosya sistemine dokunmaz; metni çağıran (CLI) okur.
