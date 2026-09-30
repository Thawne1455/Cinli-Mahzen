# CLAUDE.md — Cinli Mahzen (Claude Code ajanları için çalışma kuralları)

Bu proje **3 Claude Code ajanı** (A, B, C) tarafından, 3 ayrı makinede, paralel geliştirilen 4 kişilik online asimetrik bir Unity oyunudur.

## 0. Oturum Başında (HER SEFERİNDE)
1. Kullanıcıya **hangi ajan olduğunu** sor (A / B / C) — kullanıcı zaten söylediyse sorma.
   - **A — Çekirdek & İnsan & Maç & (M4) PUN altyapısı**
   - **B — Cinler & Possession & Görünürlük**
   - **C — Dünya & Level & Hedefler & Bulmacalar & Ses & Otopsi**
2. `git pull` yap.
3. Oku: `docs/CONTRACT_CHANGES.md` (son değişiklikler), `docs/03_TODO.md` (sıradaki görevin).
4. Görevle ilgili bölümleri oku: `docs/01_GDD_Oyun.md` (tasarım), `docs/02_GDD_Teknik.md` (mimari). **Tamamını her seferinde okuma**, ilgili bölümleri oku.
5. Unity MCP bağlı mı kontrol et. Değilse kullanıcıya söyle (Unity açık mı, MCP sunucusu çalışıyor mu).

## 1. Sahiplik Kuralları (çakışmayı önler — ÇOK ÖNEMLİ)
- Sadece **kendi klasörlerinde** dosya oluştur/düzenle. Klasör sahiplikleri: `docs/02_GDD_Teknik.md §1` ağacındaki `(A)/(B)/(C)` etiketleri.
- `CM.Core` (Scripts/Core) **A'nındır**. B veya C Core'a bir şey eklemek isterse:
  1. `docs/CONTRACT_CHANGES.md`'ye satır ekle (durum: 🟡 önerildi),
  2. Kullanıcıya "A ajanına şu değişikliği ilet" diye özetle,
  3. O zamana kadar kendi modülünde geçici çözümle devam et.
  - **İstisna:** `CM.Core/Events/` altına kendi event struct'ını ekleyebilirsin (dosya başına `// Owner: B` yaz) ve kendi `MsgCode` aralığına kod ekleyebilirsin.
- `ProjectSettings/` ve `Packages/manifest.json`: **sadece A** değiştirir.
- `Game.unity`: **sadece A** düzenler. B ve C kendi `Sandbox_B.unity` / `Sandbox_C.unity` sahnelerinde çalışır.
- Başkasının prefabını düzenleme. Bileşen eklemek gerekiyorsa runtime'da `AddComponent` ya da sahibine bildir (`02_GDD_Teknik.md §11`).
- `docs/03_TODO.md`'de sadece **kendi görevlerinin** kutusunu değiştir.

## 2. Mimari Kurallar (ihlal = hata)
1. **Network altın kuralı:** Oyun durumunu değiştiren her şey `Request → Otorite doğrular → Broadcast → herkes uygular` akışıyla, `GameServices.Net` (`INetBridge`) üzerinden. Offline olsa bile. (`02_GDD_Teknik.md §4.3`)
   - Hasar, can, possession, enerji, envanter, hedefler, maç durumu → otorite.
   - `...Authority` ile biten metotlar sadece `Net.IsAuthority` iken çağrılır; başında `Debug.Assert(GameServices.Net.IsAuthority)`.
2. **PUN tiplerini** (`Photon.*`) sadece `Scripts/Net/Pun/` içinde kullan.
3. **Oyun sonucunu etkileyen Rigidbody fiziği yok.** Saldırılar kinematik/animasyon + otorite hit-check. Rigidbody sadece kozmetik (`CosmeticPhysics` layer).
4. Oyun durumunu etkileyen rastgelelik = `GameRandom` (seed'li). `UnityEngine.Random` sadece kozmetik.
5. Zaman = `GameServices.Net.Time`. `Time.time` oyun mantığında kullanılmaz (`Time.deltaTime` serbest).
6. Sihirli sayı yok: değerler `GameBalanceConfig` / `PossessableDefinition` / `ItemDefinition` içinde. Yeni değer gerekiyorsa: A'nın config'i için CONTRACT_CHANGES; kendi SO'ların için serbest.
7. Modüller birbirinin somut sınıfına referans vermez; Core arayüzleri + `EventBus` + `GameServices.Get<T>()`.
8. Mantık mümkünse **düz C# sınıfında** (MonoBehaviour'suz) → EditMode testi yazılabilir. MonoBehaviour ince sarmalayıcı olsun.
9. Update'lerde allocation, `Find*`, `GetComponent`, LINQ yok.
10. Tüm kullanıcıya görünen metinler `Loc.T("anahtar")`.

## 3. Çalışma Döngüsü (her görev için)
1. Görevi `docs/03_TODO.md`'de `[~]` yap.
2. Önce **testleri/arayüzleri** düşün, sonra kodu yaz.
3. Script yazdıktan sonra **Unity MCP ile**: asset refresh → derlemenin bitmesini bekle → **konsolu oku** → hata/uyarı varsa düzelt. Hata varken bir sonraki adıma geçme.
4. EditMode testlerini MCP ile çalıştır (ilgili test assembly'si). Hepsi yeşil olmalı.
5. Play mode duman testi (kabul kriterinde MCP yazıyorsa): Sandbox veya Game sahnesi → Play → `CinliMahzen/Debug/*` menü komutları → `Dump State To Console` çıktısını oku → gerekirse screenshot → Play'i durdur.
6. Kabul kriterlerinin hepsi sağlandı mı? → `[x]`.
7. Commit: `<GörevID>: <kısa açıklama>` (ör. `B1.2: possession flow + orbit camera`). Küçük ve sık commit.
8. `git pull --rebase` → çakışma varsa **sadece kendi dosyalarında** çöz; başkasının dosyasında çakışma → kullanıcıya bildir, üzerine yazma.
9. `git push`.
10. Kullanıcıya kısa rapor: ne yapıldı, nasıl test edildi, diğer ajanları etkileyen bir şey var mı.

## 4. Unity MCP İpuçları
- Oturum başında `unity-mcp-skill`'i yükle. Önce kaynak oku, sonra araç kullan: `mcpforunity://editor/state` (`ready_for_tools`, `is_compiling`), `mcpforunity://project/info`.
- Script yazdıktan/düzenledikten sonra: `editor/state`'te `is_compiling == false` olana kadar bekle → `read_console(types=["error"], include_stacktrace=True)`. Error varken devam etme.
- Testler: `run_tests(mode="EditMode", ...)` → `get_test_job(job_id, wait_timeout=60, include_failed_tests=True)`.
- Play mode: `manage_editor` (play/stop) · debug komutları: `execute_menu_item("CinliMahzen/Debug/...")` · durum: `Dump State To Console` → `read_console`.
- Görsel doğrulama: `manage_camera(action="screenshot", include_image=True, max_resolution=512)`; genel bakış için `batch="surround"`.
- Çoklu işlem: `batch_execute` (max 25 komut). Nesne bulma: `find_gameobjects`. Paket: `manage_packages` (sadece A). Renderer feature: `manage_graphics` (sadece A).
- API emin değilsen: `unity_reflect` (canlı API) > `unity_docs`.
- Sahne/prefab düzenlemelerini MCP ile yaparken değişiklik sonrası **sahneyi kaydet**.
- Büyük script'leri dosya olarak yaz (Write), MCP'yi derleme/konsol/test/play mode/sahne işlemleri için kullan.
- Play mode'dayken yapılan sahne değişiklikleri kaybolur — düzenleme yapmadan önce Play'den çık.
- Harita üretmek için: `CinliMahzen/Level/Generate Map (Random Seed)`.
- Hotseat tuşları: F1-F4 oyuncu değiştir, F5 ölümsüz insan, F6 sınırsız enerji, F7 her şeyi göster, F8 yeni raund, F9 overlay, F10 hız ×2, F11 tüm mühürler, F12 dummy bot. MCP ile tuş basamıyorsan eşdeğer `CinliMahzen/Debug/*` menü komutunu kullan.

## 5. Kodlama Standartları
- Namespace: `CinliMahzen.<Modül>`; bir dosya = bir public tip; tip/metot/değişken adları **İngilizce**.
- `[SerializeField] private`; `public` alan yok.
- Log: `CMLog.Info("Kategori", "mesaj")`.
- Yeni `NetMsg` için encode/decode yardımcı + round-trip testi zorunlu.
- Unity 6 API'leri: `FindFirstObjectByType` (eski `FindObjectOfType` değil), `Rigidbody.linearVelocity` (`velocity` değil).

## 6. Yapma
- Kullanıcı istemeden paket ekleme/çıkarma, ProjectSettings değiştirme (A hariç).
- Başka ajanın görevini "yardım olsun" diye yapma — bağımlılık eksikse stub kullan veya kullanıcıya bildir.
- M4'ten önce PUN import etme veya PUN kodu yazma.
- `main` dalına derlenmeyen kod push'lama.
- Level generator (`ProceduralLevelGenerator`) **C'nindir**; C onu kalıcı olarak yazar ve geliştirir (C'nin diğer görevlerinin yanında). §8 Level kontratını bozmadan geliştirilir. A ve B generator'a değil, sadece `LevelLayout` / marker'lara bağımlıdır.
