# Cinli Mahzen

4 kişilik online asimetrik korku-komedi oyunu: **1 İnsan + 1 İyi Cin vs 2 Kötü Cin**.

Ormandaki köy evinin mahzeninde bir hazine var. İnsan, gün doğmadan evin bulmaca zincirini çözüp hazineyi kazmaya çalışır. Cevapları sadece cinler görebilir: iyi cin insana anlatır, iki kötü cin ise bulmacaları karıştırıp eşyaları fırlatır.

## Dokümanlar
| Dosya | İçerik |
|---|---|
| `CLAUDE.md` | Claude Code çalışma kuralları (otomatik yüklenir) |
| `docs/01_GDD_Oyun.md` | Oyun tasarımı: döngü, bulmacalar, roller, ışık, eşyalar, denge |
| `docs/02_GDD_Teknik.md` | Mimari: network soyutlaması, possession, bulmaca modeli, ev kontratı, PUN planı |
| `docs/03_TODO.md` | Kilometre taşları ve görevler (M0 → M6) |
| `docs/04_Asset_Eslestirme.md` | KayKit modellerinin karşılıkları + eksik asset listesi |

## Teknik
- Unity **6000.3.18f1**, URP, Input System
- Online: Photon PUN 2 (en son faz); o zamana kadar offline hotseat (F1-F4 ile rol değiştirme)
- Geliştirme: tek geliştirici + Claude Code + Unity MCP

## Başlarken
Unity'de projeyi aç, Unity MCP'yi bağla, Claude Code'a: *"CLAUDE.md ve TODO'yu oku, sıradaki görevi başlat."*

## Notlar
- KayKit Dungeon paketi (CC0) repo kökünde: `KayKit_Dungeon_Pack_1.1_FREE/` (ham kaynak).
- Discord: maçta takımlar ayrı ses kanalında olmalı.
- Photon AppId repo'ya commit edilmez.
