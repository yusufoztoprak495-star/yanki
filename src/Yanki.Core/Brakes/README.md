# Brakes

Frenler ve korumalar (plan 5.10, ADR-013). Çekirdeğin geri kalanı gibi bağımlılıksızdır (`netstandard2.1;net10.0`, LangVersion 9.0, determinizm kuralları geçerli).

| Tür | Ne yapar |
| --- | --- |
| `FrenOlayi`, `FrenTurleri`, `FrenBilgisi` | `fren_uygulandi` olayı (`fren`, `kimlik`, `npc`, `hedef`; `KuralId: fren:<tur>`). Algılanmaz, kural tetiklemez. |
| `TetikSiniri` | Kural/eylem şeması: `cooldown`, `kapsam` (kural\|npc \| npc\|hedef), `tick_basina_en_fazla`, `en_fazla`. |
| `TetikSayaclari` | Sayaçlar YALNIZCA olay günlüğünden türer (`Isle` her olayı katlar); `Gorunum` adım içi artışları taşır, böylece kural değerlendirmesi saf kalır. |
| `ZincirDerinligi`, `FrenAyarlari` | Zincir derinliği günlükten türer (kural çıktısı 1 + nedenlerinin en büyüğü; algı eklemez); D varsayılan 4. `FrenAyarlari` ayrıca M varsayılanını (`max(32, 8 × NPC)`) taşır. |
| `Korumalilik`, `IhlalBayragi`, `KorumaBayraklari` | `korumali`, `oyuncu_ihlalleri`, `ihlal_bayraklari`. `oldurulemez` ve `calinamaz` YALNIZCA bildiren eylem/kurallarda uygulanır. |
| `YayDegiskenleri`, `YayTanimi` | Yay değişkenleri; değer `yay_degisti` olaylarından türer. |
| `FrenBaglami` | `KuralMotoru.DegerlendirFrenli`'nin frenle ilgili girdileri. |

Fren yazılmadığı sürece davranış ve hash eskisiyle aynıdır: fren alanı olmayan senaryonun kanonik metnine ve günlüğüne hiçbir şey eklenmez.
Ayrıntı: `docs/notes/frenler.md`, `docs/adr/0013-frenler-kayit-ve-kaynak-modeli.md`, yazım: `docs/authoring-guide.md`.
