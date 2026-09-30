# 🧔 PROMPTLAR — AJAN A (Ana Bilgisayar)

> Bu dosyadaki kod bloklarını olduğu gibi Claude Code'a yapıştır.
> Sıra: **0 → 1 → 2 → 3**, sonra her gün **4**, gerektikçe **5-9**.

---

## 0. Senin Yapman Gerekenler (Claude'dan önce, elle)

1. **Unity Hub** → New Project → **6000.3.18f1** → şablon **Universal 3D** → ad `CinliMahzen` → konum: `C:\ajanda\CinliMahzen` (✅ oluşturuldu)
2. Proje açılınca **MCP for Unity** paketini kur (zaten kuruluysa atla):
   Window → Package Manager → `+` → *Add package from git URL* →
   `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity`
3. Unity'de **Window → MCP for Unity** → sunucuyu başlat → **Claude Code** için *Configure*.
4. `C:\Users\joker\Documents\CinliMahzen_GDD\` içindeki **her şeyi** (CLAUDE.md, docs/, GOREV_*.md, PROMPT_*.md, README.md) Unity proje köküne kopyala (Assets klasörünün yanına).
5. GitHub'da boş, private bir repo aç: `CinliMahzen`. B ve C'yi collaborator olarak ekle.
6. Terminalde proje klasörüne gir ve `claude` çalıştır. Bağlantıyı `/mcp` ile kontrol et (UnityMCP ✔ görünmeli).

---

## 1. İlk Prompt — MCP Bağlantı Testi

```
Sen Cinli Mahzen projesinde AJAN A'sın (Çekirdek, İnsan, Maç, Online altyapı) ve bu bilgisayar ANA projedir.

Önce Unity MCP bağlantısını doğrula:
1. unity-mcp-skill'i yükle.
2. mcpforunity://editor/state kaynağını oku, ready_for_tools true mu bak.
3. mcpforunity://project/info oku: Unity sürümü, render pipeline, input system durumu.
4. read_console ile mevcut hata/uyarıları listele.
5. Aktif sahnenin hiyerarşisini manage_scene ile al.

Sonuçları kısaca raporla. Henüz hiçbir şey değiştirme.
```

---

## 2. Kurulum Promptu — M0'ın İlk Yarısı (A0.1 – A0.4)

```
CLAUDE.md, GOREV_A_Cekirdek_Insan_Mac.md ve docs/03_TODO.md dosyalarını oku.
docs/02_GDD_Teknik.md'nin §0, §1, §1.1 ve §2 bölümlerini oku.

Şimdi sırasıyla A0.1, A0.2, A0.3 ve A0.4 görevlerini yap:

A0.1: Proje zaten Unity Hub'dan oluşturuldu. Editor ayarlarını kontrol et/ayarla
(Asset Serialization = Force Text, Visible Meta Files). NOT: git repo, .gitignore,
.gitattributes ve ilk commit ZATEN yapıldı ve push'landı
(remote: https://github.com/Thawne1455/Cinli-Mahzen.git). Sadece kontrol et, eksik varsa tamamla.

A0.2: Paketleri manage_packages ile ekle (Input System, AI Navigation,
Test Framework, Multiplayer Play Mode). Active Input Handling = Input System Package.
URP ayarlarını TODO'daki gibi yap.

A0.3: Klasör ağacını ve asmdef'leri §1 ve §1.1'e birebir uygun oluştur.
Her asmdef'e derlenebilir boş bir placeholder script koy.

A0.4: Layer'ları, fizik çarpışma matrisini ve XRayOutline render feature'ını
§2'ye göre kur (manage_graphics ile renderer feature). docs/CONTRACT_CHANGES.md mevcut.

Her görevden sonra: editor state'i bekle (is_compiling=false), read_console'da
error kalmadığını doğrula, görevi TODO'da [x] yap, "<ID>: açıklama" ile commit+push.
Sonunda bana arkadaşlarıma iletmem gereken mesajı hazırla ("repo hazır, klonlayın").
```

---

## 3. Core Kontratları Promptu (A0.5) — B ve C bunu bekliyor, ÖNCELİKLİ

```
A0.5 görevini yap: docs/02_GDD_Teknik.md §4.1–§4.9'daki TÜM tip ve arayüzleri
Scripts/Core içinde birebir oluştur. İmzalar bağlayıcı, değiştirme.
Stub uygulamaları da yaz (StubObjectiveInfo, StubLightService, StubLevelInfo,
StubPossessionQuery, StubMatchInfo, StubPossessionBlockerRegistry).
§4.6'daki event struct'larını Scripts/Core/Events/ altına koy.

Script'leri yazdıktan sonra derlemeyi bekle, read_console ile 0 error olduğunu doğrula.
Commit mesajı tam olarak: "A0.5: core contracts ready". Push'la.
Bana B ve C'ye iletmem gereken kısa mesajı yaz.
```

---

## 4. ⭐ Günlük Çalışma Promptu (HER OTURUMUN BAŞINDA)

```
Sen AJAN A'sın (ana bilgisayar). Oturum başlangıcı:
1. git pull yap. Gelen commit'leri özetle (B ve C ne yapmış).
2. docs/CONTRACT_CHANGES.md'de 🟡 bekleyen istek var mı? Varsa önce onu değerlendir ve bana sor.
3. Unity MCP bağlantısını kontrol et (editor state + read_console).
4. docs/03_TODO.md'de benim sıradaki [ ] görevimi bul, bağımlılıkları [x] mı kontrol et.
5. Görevin ilgili GDD bölümlerini oku ve bana 3-5 maddelik bir plan göster, onayımı bekle.

Onaydan sonra CLAUDE.md §3'teki çalışma döngüsünü uygula:
kod → derleme bekle → read_console → EditMode testleri (run_tests + get_test_job)
→ kabul kriterindeki play mode testi (manage_editor play, execute_menu_item ile
CinliMahzen/Debug/* komutları, Dump State To Console çıktısını read_console ile oku,
gerekirse manage_camera screenshot include_image=True) → play'i durdur
→ TODO [x] → commit + push → kısa rapor.
```

---

## 5. Sonraki Göreve Geç

```
Bu görev bitti. docs/03_TODO.md'deki sıradaki [ ] görevime geç. Aynı döngüyü uygula,
önce planı göster.
```

---

## 6. Kilometre Taşı Entegrasyonu (M1 / M2 sonunda)

```
M<1 veya 2> ENTEGRASYONU yapacağız. Ana bilgisayar sensin.
1. git pull. B ve C'nin M<X> görevlerinin hepsi TODO'da [x] mı kontrol et; eksik olanları listele.
2. Game.unity'yi aç. B ve C'nin sistemlerinin Game.unity'de çalışması için gereken
   sahne kurulumlarını yap (sadece Game.unity'ye dokun, başkalarının prefablarını değiştirme).
3. docs/03_TODO.md'deki "M<X> Çıkış Testi" adımlarını tek tek play mode'da uygula:
   execute_menu_item ile Debug komutları, Dump State To Console, screenshot.
4. Her adımı ✅/❌ olarak raporla. ❌ olanlar için hangi ajanın (A/B/C) sorumlu olduğunu
   ve B/C'ye iletmem gereken hata raporunu hazırla (dosya, beklenen, gerçekleşen, console log).
5. Hepsi ✅ ise commit: "M<X>: integration passed" + git tag m<X>.
```

---

## 7. Kontrat Değişikliği İsteği Geldiğinde

```
docs/CONTRACT_CHANGES.md'de B/C'den bir 🟡 istek var. Oku ve değerlendir:
- Mimari kurallara (CLAUDE.md §2) uyuyor mu?
- Mevcut kodu kırar mı, kimleri etkiler?
Bana önerini söyle. Onaylarsam Core'a uygula, derle, testleri çalıştır,
satırı ✅ yap, commit "CONTRACT: <kısa açıklama>" + push, ve arkadaşlarıma
"pull edin" mesajını hazırla.
```

---

## 8. Hata / Takılma Durumu

```
Takıldık. Şunu yap:
1. read_console ile son 30 error ve warning'i stacktrace ile oku.
2. mcpforunity://editor/state'i oku (derleme, domain reload durumu).
3. Sorunun kaynağını bul. Kendi klasörümdeyse düzelt. Başka ajanın kodundaysa
   DOKUNMA; o ajana iletilecek net bir hata raporu yaz.
```

---

## 9. Build Alma

```
CinliMahzen/Build/Windows menü komutunu çalıştır (yoksa A3.5 görevi kapsamında oluştur).
Build bitince klasör yolunu ve boyutunu söyle. Konsolda build hatası varsa listele.
```
