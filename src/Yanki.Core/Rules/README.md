# Rules

Sonuç kuralları (plan 5.6): `ne_zaman <olay türü>` + kriterler (5.9) → etkiler. Kurallar JSON ile tanımlanır, kod değiştirmeden genişler.
Etkiler doğrudan durumu değiştirmez; `KuralId` ve `NedenOlaylar` taşıyan yeni olaylar olarak kayda düşer.

**Durum:** 4. hafta teslimi tamam (v0 etkileri, doğrulayıcı v0, kaskad v0). `cooldown`, `tick_basina_en_fazla`, `yay` koşulları, zincir derinliği ve frenler 7. hafta.

## Tipler

| Tip                                                    | Ne yapar                                                                                                                                                                          |
| ------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `KuralTanimi`, `KuralEtkisi`, `EtkiTuru`               | Kural modeli. Etki türleri: `Modifier`, `Eksen`, `Duygu`, `Hafiza`, `Gercek`.                                                                                                     |
| `KuralYukleyici.Yukle(json, kaynakAdi, ekOlayTurleri)` | Kural dosyasını okur ve doğrular (doğrulayıcı v0). Tüm hatalar `dosya:satır: kural "id": mesaj` biçiminde (`KuralHatasi`) toplanır.                                               |
| `KuralMotoru`                                          | Saf değerlendirici: `Degerlendir(tetikler, ...)` → `UretilenEtki` listesi; `OlayYap` etkiyi olaya çevirir. Sıra: tetikleyici → kural kimliği (ordinal) → algılayan NPC (ordinal). |
| `TetikBilgisi`                                         | Tetikleyici olay + onu algılayanlar + her algılayanın algı olayları (üretilen olayların nedenine eklenir).                                                                        |

Bir kural yalnızca tüm kriterleri tutunca çalışır, kriteri olmayan kural her tetikleyicide çalışır; `agirlik` etkilemez ve uyarı verir ([ADR-010](../../../docs/adr/0010-kural-tum-kriterler-tutunca-calisir.md)). `KuralYuklemeSonucu.Uyarilar` uyarıları taşır.

Kriterler `Criteria/KriterOkuyucu` ile okunur (ikinci bir kriter yolu yoktur). Not ve açık noktalar: [`docs/notes/kural-motoru.md`](../../../docs/notes/kural-motoru.md).
