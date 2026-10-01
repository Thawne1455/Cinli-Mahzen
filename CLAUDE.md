# CLAUDE.md — Cinli Mahzen (Claude Code çalışma kuralları)

4 kişilik online asimetrik Unity oyunu (1 İnsan + 1 İyi Cin vs 2 Kötü Cin; köy evi, bulmaca zinciri, mahzende hazine).
**Tek geliştirici + Claude Code.** Ajan/sahiplik sistemi yok.

## 0. Oturum Başında
1. `git pull`.
2. Oku: `docs/03_TODO.md` (sıradaki görev = ilk `[ ]` ve bağımlılıkları `[x]`, ya da kullanıcının söylediği).
3. Görevle ilgili bölümleri oku: `docs/01_GDD_Oyun.md` (tasarım), `docs/02_GDD_Teknik.md` (mimari). **Tamamını her seferinde okuma.**
4. Unity MCP bağlı mı kontrol et. Değilse kullanıcıya söyle (Unity açık mı, MCP sunucusu çalışıyor mu).

## 1. Mimari Kurallar (ihlal = hata)
1. **Network altın kuralı:** Oyun durumunu değiştiren her şey `Request → Otorite doğrular → Broadcast → herkes uygular`, `GameServices.Net` (`INetBridge`) üzerinden. Offline olsa bile. (`02_GDD_Teknik.md §4.3`)
   - Can/bayılma, possession, enerji, envanter, bulmaca durumları, ışıklar, maç durumu → otorite.
   - `...Authority` ile biten metotlar sadece `Net.IsAuthority` iken çağrılır; başında `Debug.Assert(GameServices.Net.IsAuthority)`.
2. **PUN tiplerini** (`Photon.*`) sadece `Scripts/Net/Pun/` içinde kullan. M5'ten önce PUN import etme.
3. **Oyun sonucunu etkileyen Rigidbody fiziği yok.** Fırlatma/kaydırma kinematik + otorite hit-check. Rigidbody sadece kozmetik (`CosmeticPhysics`).
4. Oyun durumunu etkileyen rastgelelik = `GameRandom` (seed'li). Raund yerleşimi `RoundSetupPlanner` ile seed'den deterministik. `UnityEngine.Random` sadece kozmetik.
5. Zaman = `GameServices.Net.Time`. `Time.time` oyun mantığında yok (`Time.deltaTime` serbest).
6. Sihirli sayı yok: değerler `GameBalanceConfig` / `PossessableDefinition` / `ItemDefinition` / `PuzzleDefinition` içinde. Config'e alan eklenirse `01_GDD_Oyun.md §14` aynı commit'te güncellenir.
7. Modüller birbirinin somut sınıfına referans vermez; Core arayüzleri + `EventBus` + `GameServices.Get<T>()`.
8. Mantık mümkünse **düz C# sınıfında** → EditMode testi. MonoBehaviour ince sarmalayıcı.
9. Update'lerde allocation, `Find*`, `GetComponent`, LINQ yok.
10. Tüm kullanıcıya görünen metinler `Loc.T("anahtar")`.
11. Ev sistemleri evi **sadece marker bileşenleri** üzerinden tanır (`02_GDD_Teknik.md §8`); ev hiyerarşisine isimle erişim yok.

## 2. Çalışma Döngüsü (her görev)
1. Görevi `docs/03_TODO.md`'de `[~]` yap.
2. Önce testleri/arayüzleri düşün, sonra kodu yaz.
3. Script sonrası **Unity MCP**: asset refresh → derleme bitmesini bekle → **konsolu oku** → hata/uyarı varsa düzelt. Hata varken devam etme.
4. EditMode testlerini MCP ile çalıştır. Hepsi yeşil olmalı.
5. Kabul kriterinde MCP yazıyorsa: Play → `CinliMahzen/Debug/*` menüleri → `Dump State To Console` → gerekirse screenshot → Stop.
6. "🖐 elle" adımlar: hazırlığı yap, kullanıcıya Unity'de ne yapacağını adım adım yaz, onayını bekle.
7. Kabul kriterlerinin hepsi sağlandı → `[x]`.
8. Commit: `<GörevID>: <kısa açıklama>` (ör. `2.5: paintings puzzle + reference`). Küçük ve sık. `main`'e derlenmeyen kod push'lanmaz.
9. Kullanıcıya kısa rapor: ne yapıldı, nasıl test edildi, sıradaki görev.

Tasarım kararı değişirse (kullanıcı söylerse) önce ilgili doküman güncellenir, sonra kod.

## 3. Unity MCP İpuçları
- Oturum başında `unity-mcp-skill`'i yükle. `mcpforunity://editor/state` (`ready_for_tools`, `is_compiling`), `mcpforunity://project/info`.
- Script sonrası: `is_compiling == false` bekle → `read_console(types=["error"], include_stacktrace=True)`.
- Testler: `run_tests(mode="EditMode")` → `get_test_job(job_id, wait_timeout=60, include_failed_tests=True)`.
- Play: `manage_editor` (play/stop) · `execute_menu_item("CinliMahzen/Debug/...")` · `Dump State To Console` → `read_console`.
- Görsel: `manage_camera(action="screenshot", include_image=True, max_resolution=512)`.
- Çoklu işlem: `batch_execute` (max 25). Nesne bulma: `find_gameobjects`.
- API emin değilsen: `unity_reflect` > `unity_docs`.
- Sahne/prefab düzenledikten sonra **kaydet**. Play mode'dayken düzenleme yapma.
- Büyük script'leri dosya olarak yaz (Write); MCP'yi derleme/konsol/test/play/sahne için kullan.
- Ev kurulumu: tekrar eden yerleşimleri (duvar, zemin, marker) **editör script'i/menü komutu** ile yap, tek tek MCP çağrısıyla değil.
- Hotseat: F1-F4 oyuncu, F5 bayılmaz, F6 sınırsız enerji, F7 her şeyi göster, F8 yeni raund, F9 overlay, F10 hız ×2, F11 aşamayı tamamla, F12 dummy bot. Tuş basamıyorsan `CinliMahzen/Debug/*` eşdeğerini kullan.

## 4. Kodlama Standartları
- Namespace `CinliMahzen.<Modül>`; bir dosya = bir public tip; adlar **İngilizce**.
- `[SerializeField] private`; public alan yok.
- Log: `CMLog.Info("Kategori", "mesaj")`.
- Yeni `NetMsg` için encode/decode yardımcı + round-trip testi zorunlu.
- Enum'ların int değerlerini değiştirme (asset'ler int saklar); yeni değeri sona ekle.
- Unity 6 API'leri: `FindFirstObjectByType`, `Rigidbody.linearVelocity`.

## 5. Yapma
- Kullanıcı istemeden paket ekleme/çıkarma.
- Kullanıcı istemeden uzak dal silme, force push, geçmiş yeniden yazma.
- Prosedürel harita üreticisi yazma — ev elle kurulur, sadece **yerleşim** rastgeledir.
