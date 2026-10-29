# Yankı: altın sabitler için BAĞIMSIZ doğrulama aracı.
#
# Ne yapar: tests/Yanki.Core.Tests/Determinizm/AltinSabitTests.cs içindeki sabit değerleri (altın dünyanın durum
# hash'i ve AnahtarliRng.Icin(12345, 7, "Ali", "karar") akışının ilk 5 değeri) C# koduna bakmadan, belirtimden yeniden
# hesaplar: FNV-1a 64 + SplitMix64 karıştırıcısı, UTF-8, decimal için ROUND_HALF_EVEN (Python `decimal` modülü).
# Böylece testteki sabitler kodun kendi çıktısından kopyalanmış olmaz; kod ile belirtim ayrışırsa test kırılır.
#
# Nasıl çalıştırılır (yalnızca Python 3 standart kütüphanesi, paket gerekmez):
#     python tools/golden_ref.py
# Çıktıdaki `durum_hash` ve `rng` satırları AltinSabitTests.cs'deki değerlerle birebir aynı olmalıdır.
# Betik kendi içinde de bilinen vektörleri (FNV-1a, SplitMix64) ve decimal orta nokta yuvarlamasını sınar.
#
# ÖNEMLİ: Bu betik motorun parçası DEĞİLDİR. Çekirdek C#'tır (CLAUDE.md); betik yalnızca altın sabitleri bağımsız
# doğrulayan bir geliştirici aracıdır, derlemeye, teste ya da CI'a girmez. Altın dünya (OlayUretici.SabitDunya) ya da
# hash biçimi bilerek değişirse buradaki `metin` ve sabitler güncellenip testteki değerler yeniden doğrulanır.

from decimal import Decimal, ROUND_HALF_EVEN

M = (1 << 64) - 1


def fnv1a(data: bytes) -> int:
    h = 0xCBF29CE484222325
    for b in data:
        h ^= b
        h = (h * 0x100000001B3) & M
    return h


def mix(z: int) -> int:
    z = ((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9) & M
    z = ((z ^ (z >> 27)) * 0x94D049BB133111EB) & M
    return z ^ (z >> 31)


def hesapla(data: bytes) -> int:
    return mix(fnv1a(data))


def kanonik_sayi(d: Decimal) -> str:
    q = d.quantize(Decimal("0.000001"), rounding=ROUND_HALF_EVEN)
    if q == 0:
        return "0"
    s = format(q, "f")
    if "." in s:
        s = s.rstrip("0").rstrip(".")
    return s


# Yuvarlama kuralı kendi içinde sınanır (tam orta noktalar).
for girdi, beklenen in [("0.0000005", "0"), ("0.0000015", "0.000002"), ("0.0000025", "0.000002"),
                        ("0.0000035", "0.000004"), ("1.2345675", "1.234568"), ("1.2345665", "1.234566"),
                        ("-0.0000015", "-0.000002"), ("-0.0000005", "0"), ("500", "500"), ("0.10", "0.1")]:
    assert kanonik_sayi(Decimal(girdi)) == beklenen, (girdi, kanonik_sayi(Decimal(girdi)), beklenen)

faiz = kanonik_sayi(Decimal("0.0000025"))
assert faiz == "0.000002"

metin = (
    "YANKI-DURUM|1\n"
    "V|Ali|npc\nV|Ayse|npc\nV|Mehmet|npc\nV|han|mekan\nV|pazar|mekan\n"
    "M|han|pazar\n"
    "G|borclu(Mehmet,Ayse,500)\n"
    f"G|faiz(Mehmet,Ayse,{faiz})\n"
    "G|konum!(Ali,han)\nG|konum!(Ayse,han)\nG|konum!(Mehmet,pazar)\n"
)
print("durum_hash", format(hesapla(metin.encode("utf-8")), "016x"))


def rng_icin(seed: int, tick: int, *anahtarlar: str) -> int:
    b = bytearray()
    b += seed.to_bytes(8, "little")
    b += (tick & M).to_bytes(8, "little")
    for a in anahtarlar:
        u = a.encode("utf-8")
        b += len(u).to_bytes(4, "little")
        b += u
    return hesapla(bytes(b))


durum = rng_icin(12345, 7, "Ali", "karar")
degerler = []
for _ in range(5):
    durum = (durum + 0x9E3779B97F4A7C15) & M
    degerler.append(mix(durum))
print("rng", ", ".join(f"0x{v:016x}UL" for v in degerler))

# Referans vektörleri (FNV-1a ve SplitMix64) betiğin doğruluğunu sınar.
assert format(fnv1a(b""), "016x") == "cbf29ce484222325"
assert format(fnv1a(b"foobar"), "016x") == "85944171f73967e8"
d0 = 0
ilk = []
for _ in range(2):
    d0 = (d0 + 0x9E3779B97F4A7C15) & M
    ilk.append(mix(d0))
assert ilk == [0xE220A8397B1DCDAF, 0x6E789E6AA1B965F4]
print("referans vektörleri tamam")
