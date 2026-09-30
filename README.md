# Cinli Mahzen — GDD Paketi

4 kişilik online asimetrik korku-komedi oyunu: **1 İnsan + 1 İyi Cin vs 2 Kötü Cin**.

## Dosyalar
| Dosya | İçerik | Kim okur |
|---|---|---|
| `GOREV_A_Cekirdek_Insan_Mac.md` | **Ana bilgisayar** — Ajan A'nın görev dokümanı | A + Claude Code |
| `GOREV_B_Cinler_Possession.md` | Ajan B'nin görev dokümanı | B + Claude Code |
| `GOREV_C_Dunya_Hedefler.md` | Ajan C'nin görev dokümanı | C + Claude Code |
| `PROMPT_A_AnaBilgisayar.md` / `PROMPT_B_Cinler.md` / `PROMPT_C_Dunya.md` | Claude Code'a yapıştırılacak hazır promptlar (MCP kurulumu dahil) | Her arkadaş kendi dosyasını |
| `CLAUDE.md` | Claude Code ajanlarının çalışma kuralları (sahiplik, mimari, test döngüsü) | Claude Code (otomatik yüklenir) |
| `docs/01_GDD_Oyun.md` | Oyun tasarımı: roller, eşyalar, bulmacalar, akış, UI, denge tablosu | Herkes |
| `docs/02_GDD_Teknik.md` | Teknik mimari: kontratlar, network, possession, level kontratı, PUN planı | Claude Code + geliştiriciler |
| `docs/03_TODO.md` | Ajan bazlı görev listesi, bağımlılıklar, kabul kriterleri (M0 → M5) | Claude Code |
| `docs/04_Asset_Eslestirme.md` | KayKit modellerinin oyundaki karşılıkları + eksik asset listesi | Herkes |
| `docs/CONTRACT_CHANGES.md` | Ortak kontrat değişiklik kaydı | Claude Code |

## Kurulum (sırayla)
1. **Arkadaş A** Unity 6000.3.18f1 ile projeyi oluşturur (TODO `A0.1`), bu klasördeki `CLAUDE.md` ve `docs/` klasörünü **Unity proje köküne** kopyalar, GitHub'a push'lar.
2. A, `A0.1–A0.4` görevlerini bitirip push'lar.
3. **Arkadaş B ve C** repoyu klonlar, Unity'de açar, Unity MCP'yi bağlar.
4. Her biri kendi Claude Code'una şöyle başlar:
   > "Sen **Ajan B**'sin. CLAUDE.md'yi ve docs/03_TODO.md'yi oku, sıradaki görevimi başlat."
5. Her görev bitince Claude commit+push yapar; diğerleri oturum başında pull eder.

## Ajanlar
- **A** — Çekirdek, İnsan, Maç akışı, UI framework, (M4) Photon PUN
- **B** — Cinler, Possession, Eşya aksiyonları, Görünürlük, İyi cin yetenekleri
- **C** — Harita (test generator + kontrat), Populator, Hedefler, Bulmacalar, Işık, Ses, Otopsi

## Önemli Notlar
- **Online (Photon PUN 2) en son (M4).** Ama kod baştan network'e hazır yazılıyor.
- **Prosedürel level generator'ı sen getireceksin** → `02_GDD_Teknik.md §8` kontratını uygulaması yeterli. Entegrasyon görevi: `C3.4`.
- **Discord:** Maçta takımlar ayrı ses kanalında olmalı.
- **Photon AppId** repo'ya commit edilmez, herkes kendi yerel ayarına girer (M4).
- Eksik asset'ler (insan karakteri, sesler, font): `04_Asset_Eslestirme.md §6`.
