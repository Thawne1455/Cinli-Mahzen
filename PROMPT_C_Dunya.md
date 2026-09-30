# 🏚️ PROMPTLAR — AJAN C (Dünya, Level, Hedefler, Ses)

> Bu dosyadaki kod bloklarını olduğu gibi Claude Code'a yapıştır.
> Sıra: **0 → 1 → 2**, sonra her gün **3**, gerektikçe **4-8**.

---

## 0. Senin Yapman Gerekenler (Claude'dan önce, elle)

1. **Unity Hub**'da **6000.3.18f1** kurulu olsun. **Farklı sürüm KULLANMA.**
2. Ajan A "repo hazır" deyince: `git clone https://github.com/Thawne1455/Cinli-Mahzen.git` → Unity Hub → Add → klonlanan klasörü aç.
3. **MCP for Unity** paketi repo ile gelir. Gelmediyse:
   Window → Package Manager → `+` → *Add package from git URL* →
   `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity`
4. Unity'de **Window → MCP for Unity** → sunucuyu başlat → **Claude Code** için *Configure*.
5. KayKit paketi repo ile gelir: **`KayKit_Dungeon_Pack_1.1_FREE/`** (repo kökü). Ayrıca indirmene gerek yok.
6. Terminalde proje klasöründe `claude` çalıştır → `/mcp` ile UnityMCP ✔ olduğunu gör.

---

## 1. İlk Prompt — MCP Bağlantı Testi

```
Sen Cinli Mahzen projesinde AJAN C'sin (Dünya, Level, Hedefler, Bulmacalar, Ses, Otopsi).

Önce Unity MCP bağlantısını doğrula:
1. unity-mcp-skill'i yükle.
2. mcpforunity://editor/state oku, ready_for_tools true mu bak.
3. mcpforunity://project/info oku: Unity sürümü 6000.3.18f1 mi, URP var mı.
4. read_console ile hata var mı bak.
5. git log --oneline -10.

Kısaca raporla. Henüz hiçbir şey değiştirme.
```

---

## 2. Başlangıç Promptu — Asset Import (C0.1, C0.2) — hemen başlayabilirsin

```
CLAUDE.md, GOREV_C_Dunya_Hedefler.md, docs/03_TODO.md ve docs/04_Asset_Eslestirme.md dosyalarını oku.

C0.1: KayKit paketi şurada çıkarılmış: <BURAYA_KLASÖR_YOLU>
- İçindeki "Assets/fbx(unity)/" klasöründeki tüm .fbx dosyalarını
  Assets/_Project/Art/KayKit/Models/ altına kopyala.
- "Assets/textures/dungeon_texture.png" → Assets/_Project/Art/KayKit/Textures/
- License.txt → Assets/_Project/Art/KayKit/
- M_KayKit_Dungeon materyalini oluştur (URP Lit, base map = dungeon_texture,
  smoothness 0.1) ve tüm modellerin materyalini buna bağla (manage_asset).
- Ölçü raporu: wall, wall_doorway, wall_gated, floor_tile_large, floor_tile_small,
  stairs, shelf_large, barrel_large, chest, chair modellerini geçici olarak sahneye koy,
  Renderer bounds'larını oku, docs/04_Asset_Eslestirme.md §1 tablosunu doldur,
  grid hücre boyutunu ve duvar yüksekliğini kesinleştir. Geçici objeleri sil.
- manage_camera screenshot ile modellerin pembe olmadığını doğrula.

C0.2: Environment ve Props prefablarını TODO'daki gibi oluştur (collider + doğru layer,
tutarlı pivot). Sandbox_C.unity'de 3x3 hücrelik bir test odası kur ve
screenshot (batch="surround", max_resolution=512) ile boşluk olmadığını doğrula.

Her görevden sonra: read_console 0 error → TODO [x] → commit "<ID>: açıklama"
→ git pull --rebase → push → kısa rapor. C0.2 bitince bana B'ye iletmem gereken
"prop prefabları hazır" mesajını yaz.
```

---

## 3. ⭐ Günlük Çalışma Promptu (HER OTURUMUN BAŞINDA)

```
Sen AJAN C'sin. Oturum başlangıcı:
1. git pull yap. Gelen commit'leri özetle (A ve B ne yapmış, beni etkileyen var mı).
2. docs/CONTRACT_CHANGES.md'deki yeni satırları oku.
3. Unity MCP bağlantısını kontrol et (editor state + read_console).
4. docs/03_TODO.md'de benim sıradaki [ ] görevimi bul. Bağımlılıkları [x] mı?
   Değilse GOREV_C §5 tablosundaki "beklerken" yolunu uygula.
5. İlgili GDD bölümlerini oku, bana 3-5 maddelik plan göster, onayımı bekle.

Onaydan sonra CLAUDE.md §3 döngüsü:
kod → derleme bekle → read_console → EditMode testleri (run_tests + get_test_job)
→ Sandbox_C.unity'de play/edit mode testi (execute_menu_item ile
CinliMahzen/Level/* ve CinliMahzen/Debug/* komutları, read_console,
manage_camera screenshot include_image=True) → play'i durdur
→ TODO [x] → commit + pull --rebase + push → kısa rapor.
```

---

## 4. Sonraki Göreve Geç

```
Bu görev bitti. docs/03_TODO.md'deki sıradaki [ ] görevime geç. Aynı döngü, önce plan.
```

---

## 5. Harita Doğrulama Promptu (C0.4 ve sonrasında sık kullanılır)

```
Haritayı doğrula:
1. execute_menu_item "CinliMahzen/Level/Generate Map (Random Seed)" — 3 kez, farklı seed.
2. Her seferde "CinliMahzen/Level/Validate Current Level" çalıştır, raporu read_console'dan oku.
3. Her harita için kuş bakışı screenshot al (manage_camera, view_position yukarıdan,
   max_resolution=512, include_image=True).
4. Determinizm: aynı seed ile 2 kez üret, LevelHash eşit mi?
5. Tablo: seed | oda sayısı | döngü var mı | vault→çıkış mesafesi | validator sonucu | hash eşit mi.
```

---

## 6. ProceduralLevelGenerator v2 (C3.4 — kalite & çeşitlilik)

```
C3.4'e başlıyoruz: ProceduralLevelGenerator v2.
docs/02_GDD_Teknik.md §7.2 (v2), §8 ve docs/01_GDD_Oyun.md §11'i oku.
1. Mevcut v1'i incele; v2 yaklaşımını öner: şekilli odalar (L/T) mi, elle hazırlanmış oda şablonları
   (room prefab + soket) + prosedürel yerleşim mi. Artı/eksi ve KayKit ile uyumu. Onayımı bekle.
2. Oda rollerini graf/yol mesafesiyle ata (Start, Vault, Exit ≥ 35 m, rün ↔ ipucu ≥ 2 oda).
3. Oynanış sezgileri: vault çevresinde döngü, çıkmaz sınırı, koridor/oda oranı. Ayarlar LevelGenSettings'te.
4. CinliMahzen/Level/Batch Report (100 seeds) menüsü → tablo raporu.
5. Determinizm testi + 100 seed Validator testi + 3 seed'de tam raund play testi + üstten screenshot'lar.
Kontrat (§8.1–8.4) değişecekse önce CONTRACT_CHANGES'e yaz ve bana söyle.
```

---

## 7. Core'da Değişiklik Gerektiğinde

```
Core'da (Scripts/Core) bir değişikliğe ihtiyacım var ama Core A'nın.
docs/CONTRACT_CHANGES.md'ye 🟡 durumlu satır ekle (ne, neden, kimleri etkiler),
commit+push et, A'ya iletmem gereken mesajı yaz. Bu sırada geçici çözümle ilerle.
```

---

## 8. Hata / Takılma Durumu

```
Takıldık. read_console ile son 30 error+warning'i stacktrace ile oku,
mcpforunity://editor/state'e bak. Sorun benim klasörümdeyse düzelt.
A'nın veya B'nin kodundaysa DOKUNMA; ilgili ajana net bir hata raporu yaz.
```
