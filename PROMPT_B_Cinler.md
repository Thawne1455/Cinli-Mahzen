# 😈 PROMPTLAR — AJAN B (Cinler, Possession, Görünürlük)

> Bu dosyadaki kod bloklarını olduğu gibi Claude Code'a yapıştır.
> Sıra: **0 → 1 → 2**, sonra her gün **3**, gerektikçe **4-7**.

---

## 0. Senin Yapman Gerekenler (Claude'dan önce, elle)

1. **Unity Hub**'da **6000.3.18f1** kurulu olsun (Installs → Install Editor → Archive'dan tam bu sürüm). **Farklı sürüm KULLANMA.**
2. Ajan A "repo hazır" deyince: `git clone https://github.com/Thawne1455/Cinli-Mahzen.git` → Unity Hub → Add → klonlanan klasörü aç.
3. **MCP for Unity** paketi repo ile gelir (A kurduysa). Gelmediyse:
   Window → Package Manager → `+` → *Add package from git URL* →
   `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity`
4. Unity'de **Window → MCP for Unity** → sunucuyu başlat → **Claude Code** için *Configure*.
5. Terminalde proje klasöründe `claude` çalıştır → `/mcp` ile UnityMCP ✔ olduğunu gör.

---

## 1. İlk Prompt — MCP Bağlantı Testi

```
Sen Cinli Mahzen projesinde AJAN B'sin (Cinler, Possession, Görünürlük).

Önce Unity MCP bağlantısını doğrula:
1. unity-mcp-skill'i yükle.
2. mcpforunity://editor/state oku, ready_for_tools true mu bak.
3. mcpforunity://project/info oku: Unity sürümü 6000.3.18f1 mi, URP ve Input System var mı.
4. read_console ile hata var mı bak.
5. git log --oneline -10 ile son commit'leri göster.

Kısaca raporla. Henüz hiçbir şey değiştirme.
```

---

## 2. Başlangıç Promptu — İlk Görevler (B0.1, B0.2)

```
CLAUDE.md, GOREV_B_Cinler_Possession.md ve docs/03_TODO.md dosyalarını oku.
docs/01_GDD_Oyun.md §3.3 ve §4'ü, docs/02_GDD_Teknik.md §4 ve §6'yı oku.

git log'da "A0.5: core contracts ready" commit'i var mı kontrol et.
YOKSA: dur ve bana "A0.5 bekleniyor" de.
VARSA: B0.1 görevine başla.

B0.1: PossessableDefinition / PossessableActionDef ScriptableObject'lerini, düz C#
PossessableStateMachine, PossessionArbiterCore (§6.2'deki 6 kural), JinnEnergyCore,
CooldownTracker sınıflarını yaz. 10 adet PD_*.asset'i Oyun §4 tablosundaki değerlerle
oluştur (manage_asset). EditMode testleri: her arbiter kuralı için pozitif+negatif,
yarış durumu, Öfke çarpanları. run_tests ile çalıştır, get_test_job ile sonucu al.

Sonra B0.2: Sandbox_B.unity sahnesi + placeholder cin görselleri
(küre + göz + parçacık iz; kötü mor, iyi turkuaz; doğru layer'lar).
manage_camera screenshot include_image=True ile doğrula.

Her görevden sonra: derleme bekle → read_console 0 error → TODO [x] →
commit "<ID>: açıklama" → git pull --rebase → push → kısa rapor.
Sadece kendi klasörlerime dokun (GOREV_B §3).
```

---

## 3. ⭐ Günlük Çalışma Promptu (HER OTURUMUN BAŞINDA)

```
Sen AJAN B'sin. Oturum başlangıcı:
1. git pull yap. Gelen commit'leri özetle (A ve C ne yapmış, beni etkileyen var mı).
2. docs/CONTRACT_CHANGES.md'deki yeni satırları oku.
3. Unity MCP bağlantısını kontrol et (editor state + read_console).
4. docs/03_TODO.md'de benim sıradaki [ ] görevimi bul. Bağımlılıkları [x] mı?
   Değilse GOREV_B §5 tablosundaki "beklerken" yolunu uygula (stub / dummy ile ilerle).
5. İlgili GDD bölümlerini oku, bana 3-5 maddelik plan göster, onayımı bekle.

Onaydan sonra CLAUDE.md §3 döngüsü:
kod → derleme bekle → read_console → EditMode testleri (run_tests + get_test_job)
→ Sandbox_B.unity'de play mode testi (manage_editor play, execute_menu_item ile
CinliMahzen/Debug/* komutları, Dump State To Console'u read_console ile oku,
manage_camera screenshot include_image=True) → play'i durdur
→ TODO [x] → commit + pull --rebase + push → kısa rapor.
```

---

## 4. Sonraki Göreve Geç

```
Bu görev bitti. docs/03_TODO.md'deki sıradaki [ ] görevime geç. Aynı döngü, önce plan.
```

---

## 5. Core'da Değişiklik Gerektiğinde

```
Core'da (Scripts/Core) bir değişikliğe ihtiyacım var ama Core A'nın.
docs/CONTRACT_CHANGES.md'ye 🟡 durumlu bir satır ekle: ne, neden, kimleri etkiler.
Commit+push et. Bana A'ya iletmem gereken kısa mesajı yaz.
O sırada kendi modülümde geçici bir çözümle (adapter/stub) ilerle.
```

---

## 6. Aksiyon Test Promptu (B1.4 / B2.1 sırasında çok kullanılır)

```
<EŞYA ADI> aksiyonunu Sandbox_B'de test et:
1. Sahneye possessable prefabı ve bir test insanı/dummy hedef yerleştir (hedef, telgraf yönünde 1.5 m önde).
2. Play → execute_menu_item "CinliMahzen/Debug/Possess Nearest (Evil P3)"
   → "CinliMahzen/Debug/Trigger Action Primary (Evil P3)".
3. Telgraf süresi kadar bekle → Dump State To Console → hasar/status değerlerini oku.
4. Telgraf anında ve sonrasında screenshot al.
5. Beklenen (Oyun §4 tablosu) ile gerçekleşeni karşılaştır, tablo halinde raporla.
```

---

## 7. Hata / Takılma Durumu

```
Takıldık. read_console ile son 30 error+warning'i stacktrace ile oku,
mcpforunity://editor/state'e bak. Sorun benim klasörümdeyse düzelt.
A'nın veya C'nin kodundaysa DOKUNMA; ilgili ajana iletilecek net bir hata raporu yaz
(dosya, beklenen, gerçekleşen, log).
```
