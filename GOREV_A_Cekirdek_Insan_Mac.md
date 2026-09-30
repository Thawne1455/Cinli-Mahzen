# 🧔 GÖREV DOKÜMANI — AJAN A: Çekirdek, İnsan & Maç (ANA BİLGİSAYAR)

> **Kim:** Ana projenin bulunduğu bilgisayardaki arkadaş + Claude Code.
> **Rolün:** Projenin **kurucusu ve entegratörü.** Proje iskeletini, ortak kontratları (Core), insan karakterini, maç akışını ve en sonda online altyapıyı sen yaparsın. `Game.unity` sahnesini, `ProjectSettings` klasörünü ve paketleri **sadece sen** değiştirirsin.
> **Detaylı referans:** `docs/01_GDD_Oyun.md`, `docs/02_GDD_Teknik.md`, `docs/03_TODO.md` (görev ID'leri aynı).

---

## 1. Claude Code'a İlk Mesaj (kopyala-yapıştır)

```
Sen Cinli Mahzen projesinde AJAN A'sın (Çekirdek, İnsan, Maç, Online altyapı).
Proje kökündeki CLAUDE.md'yi, GOREV_A_Cekirdek_Insan_Mac.md'yi ve docs/03_TODO.md'yi oku.
Sonra sıradaki [ ] görevimi başlat. Her görevde kabul kriterlerini Unity MCP ile doğrula,
bitince commit + push yap ve bana kısa rapor ver.
```

---

## 2. Sorumluluk Alanın

| Alan | Klasör / Dosya |
|---|---|
| Ortak kontratlar, servisler, network soyutlaması | `Scripts/Core/` (CM.Core) |
| İnsan karakteri, kamera, input | `Scripts/Player/` (CM.Player), `Prefabs/Player/`, `Art/Characters/` |
| Maç akışı, roller, puanlar, istatistikler | `Scripts/Match/` (CM.Match) |
| Debug araçları, hotseat modu | `Scripts/DebugTools/` |
| UI framework, Loc, menüler, İnsan HUD'u | `Scripts/UI/Common/`, `Scripts/UI/Human/`, `Prefabs/UI/Common/`, `Prefabs/UI/Human/` |
| Eşyalar (Tuz, Nazar) | `Prefabs/Items/`, `ScriptableObjects/Items/` |
| Config | `ScriptableObjects/Config/GameBalanceConfig.asset` |
| Sahneler | `Boot.unity`, `MainMenu.unity`, **`Game.unity`**, `Sandbox_A.unity` |
| Proje ayarları & paketler | `ProjectSettings/`, `Packages/manifest.json` |
| Online (M4) | `Scripts/Net/Pun/`, `Assets/Photon/` |

**❌ Dokunmadığın yerler:** `Scripts/Jinn`, `Possession`, `Visibility` (B) · `Scripts/World`, `Objectives`, `Audio` (C) · B ve C'nin prefab/sahneleri.

---

## 3. Ana Bilgisayar Olarak Ek Sorumlulukların

1. **GitHub reposunu sen kurarsın** ve B ile C'yi collaborator olarak eklersin.
2. **Entegrasyon:** Her kilometre taşı sonunda B ve C'nin sistemlerini `Game.unity`'de birleştirir ve **çıkış testini** (M1, M2...) bu bilgisayarda koşarsın.
3. **Kontrat bekçisi:** B veya C `docs/CONTRACT_CHANGES.md`'ye 🟡 bir istek yazarsa değerlendirir, Core'a uygular, satırı ✅ yaparsın.
4. **Build:** Test build'leri bu bilgisayardan alınır (`CinliMahzen/Build/Windows`).
5. **M4'te Photon AppId'yi** oluşturur, arkadaşlarına özelden iletirsin (repo'ya girmez).

---

## 4. Görev Sırası

### 🟥 M0 — Kurulum (EN ÖNCE SEN — B ve C seni bekliyor)

| # | ID | Görev | Kabul |
|---|---|---|---|
| 1 | A0.1 | Unity **6000.3.18f1**, Universal 3D şablonu, proje `CinliMahzen`. Force Text, Visible Meta. `.gitignore` + `.gitattributes`. `CLAUDE.md`, `docs/` ve 3 `GOREV_*.md` dosyası proje köküne. GitHub'a push | Repo açılıyor, konsolda hata yok |
| 2 | A0.2 | Paketler: Input System, AI Navigation, Test Framework, Multiplayer Play Mode. Active Input = New | Derleme temiz |
| 3 | A0.3 | Klasör ağacı + asmdef'ler (Teknik §1, §1.1) | Bağımlılık grafiği birebir |
| 4 | A0.4 | Layer'lar, fizik matrisi, XRay render feature (Teknik §2). `docs/CONTRACT_CHANGES.md` | Commit |
| — | 📣 | **B ve C'ye haber ver: "Repo hazır, klonlayabilirsiniz."** | |
| 5 | A0.5 | **Core kontratları + stub'lar** (Teknik §4.1–§4.8) | Commit mesajı: `A0.5: core contracts ready` |
| — | 📣 | **B ve C'ye haber ver: "Kontratlar hazır, pull edin."** B ve C'nin kod yazması buna bağlı | |
| 6 | A0.6 | OfflineNetBridge, EventBus, EntityRegistry, GameServices + EditMode testleri | Testler yeşil |
| 7 | A0.7 | GameBalanceConfig (Oyun §12), ItemDefinition, Loc, CMLog, GameRandom | Determinizm testi yeşil |
| 8 | A0.8 | Input Actions (Human/Spirit/Possessed/UI/Debug) + LocalInputSource | Map geçişi çalışıyor |
| 9 | A0.9 | Boot/MainMenu/Game/Sandbox_A sahneleri, auto-bootstrap, F9 debug overlay, `Dump State To Console` menüsü | Game.unity Play → "Bootstrap OK" |

### 🟧 M1 — Çekirdek Döngü

| # | ID | Görev | Kabul |
|---|---|---|---|
| 10 | A1.1 | PawnBase + Human FPS controller + stamina (Teknik §5.1–5.2) | WASD/koşu/stamina çalışıyor |
| 11 | A1.2 | CameraRig: FPS / Orbit / DeathCam + role göre culling | **B bunu bekliyor** (orbit kamera) |
| 12 | A1.3 | Interactor (basılı tutma, ipucu event'i) | **C bunu bekliyor** (kap arama) |
| 13 | A1.4 | HumanHealth (otoriter), ölüm, ragdoll placeholder | **B bunu bekliyor** (hasar) |
| 14 | A1.5 | PlayerRegistry + Hotseat (F1-F4) + PawnSpawner | 4 piyon arası geçiş |
| 15 | A1.6 | MatchStateMachine: Setup → Intro → Playing → RoundEnd → yeni raund | Kill Human → yeni harita |
| 16 | A1.7 | Human HUD minimal | Can, stamina, ipucu, sayaç |
| — | 🔗 | **M1 ENTEGRASYONU** (Game.unity'de B+C sistemleri) + M1 çıkış testi | TODO'daki M1 testi geçti |

### 🟨 M2 — Tam Oynanış

| # | ID | Görev |
|---|---|---|
| 17 | A2.1 | Envanter (2 slot) + Tuz (`IPossessionBlocker`) + Nazar |
| 18 | A2.2 | Tekme (`IKickable` çağırır) + Fener Parlat (`IPossessionQuery` kullanır) |
| 19 | A2.3 | StatusController: Knockdown, Grabbed, Drunk, Slowed — **B'nin şişe/mimik aksiyonları bekliyor** |
| 20 | A2.4 | NoiseEmitter — **B'nin gürültü halkası bekliyor** |
| 21 | A2.5 | 4 raund rotasyonu + MatchEnd + ScoreService + StatsService + TitleCalculator — **C'nin Otopsi ekranı bekliyor** |
| 22 | A2.6 | Human HUD tam |
| — | 🔗 | **M2 ENTEGRASYONU** + M2 çıkış testi |

### 🟩 M3 — Cila

| ID | Görev |
|---|---|
| A3.1 | MainMenu, Pause, Ayarlar (hassasiyet, FOV, ses, kalite) |
| A3.2 | Rol açıklama ekranı + giriş geri sayımı |
| A3.3 | İnsan karakter modeli (asset gelince) + FPS fener el modeli |
| A3.4 | Performans turu (≥ 90 FPS, Update'lerde GC 0) |
| A3.5 | Windows build menüsü |

### 🟦 M4 — Online (EN SON)

| ID | Görev |
|---|---|
| A4.1 | PUN 2 import + AppId (yerel) + PunConnection (region `eu`) |
| A4.2 | Lobi UI: 6 haneli oda kodu, 4 slot, hazır, başlat, Discord takım kanalı hatırlatması |
| A4.3 | **PunNetBridge** (MsgCode → RaiseEvent) + PunPlayerRegistry + oda özellikleri — **B ve C'nin M4 görevleri bunu bekliyor** |
| A4.4 | PunPawnSync + PunPawnSpawner |
| A4.5 | Kopma / master ayrılması → lobiye dönüş |
| ALL4.6 | Online test checklist (B ve C ile birlikte) |

---

## 5. Teslim Ettiklerin → Kim Bekliyor

| Sen teslim edersin | Bekleyen | Neden |
|---|---|---|
| A0.1–A0.4 (repo) | B, C | Projeyi klonlamak için |
| **A0.5 (Core kontratları)** | B, C | Tüm kodları bu arayüzlere dayanıyor |
| A1.2 CameraRig API (`SetFps`, `SetOrbit`, `SetDeathCam`) | B | Cin kamerası, eşya içi TPS |
| A1.1 `IHumanInput` | B | Dummy İnsan botu |
| A1.3 Interactor | C | Kap arama, rün, kazı |
| A1.4 HumanHealth (`IDamageable`) | B | Eşya saldırıları |
| A1.6 MatchState / `IMatchInfo` | B, C | Cinler uyanma süresi, level yeniden üretimi |
| A2.3 StatusController | B | Şişe (Drunk), Mimik (Grabbed) |
| A2.4 NoiseEmitter | B | Kötü cin gürültü halkası |
| A2.5 Stats/Score | C | Otopsi ve maç sonu ekranları |
| A4.3 PunNetBridge | B, C | Online senkron |

## 6. Beklediklerin

| Kimden | Ne | Ne için |
|---|---|---|
| C | C0.3 Level kontratı + marker'lar | PawnSpawner (spawn noktaları). O zamana kadar `StubLevelInfo` kullan |
| C | C1.1 LevelService | Maç akışında harita üretimi |
| C | C1.3 Container/Loot | Tuz ve Nazar'ı kaptan bulmak |
| B | B1.2 Possession + `IKickable`, `IPossessionQuery` | Tekme ve fener. O zamana kadar stub kullan |

---

## 7. Dikkat Et
- **Core'u erken ve doğru yaz.** Sonradan Core değişikliği herkesin kodunu kırar. A0.5'te Teknik §4'ü birebir uygula.
- Stub'lar (`StubObjectiveInfo`, `StubLightService` vb.) B ve C'nin seni beklemeden çalışabilmesi için var. Hepsini A0.5'te yaz.
- `Game.unity`'yi sen yönetiyorsun. B veya C "şu sahneye eklensin" derse ekleme işini sen yaparsın.
- Online (M4) başlamadan PUN import etme.
