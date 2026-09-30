# 😈 GÖREV DOKÜMANI — AJAN B: Cinler, Possession & Görünürlük

> **Kim:** İkinci arkadaş + Claude Code (kendi bilgisayarında, repo klonu ile).
> **Rolün:** Oyunun kalbi olan **cin sistemlerini** yaparsın: uçan cin kontrolü, eşyalara girme, 8 eşyanın saldırıları, iyi cinin yetenekleri ve **kimin neyi gördüğü** (görünürlük). Komedinin çoğu senin kodundan çıkacak.
> **Detaylı referans:** `docs/01_GDD_Oyun.md` §3.2, §3.3, §4, §9 · `docs/02_GDD_Teknik.md` §6 · `docs/03_TODO.md`.

---

## 1. Claude Code'a İlk Mesaj (kopyala-yapıştır)

```
Sen Cinli Mahzen projesinde AJAN B'sin (Cinler, Possession, Görünürlük).
Proje kökündeki CLAUDE.md'yi, GOREV_B_Cinler_Possession.md'yi ve docs/03_TODO.md'yi oku.
Sonra sıradaki [ ] görevimi başlat. Her görevde kabul kriterlerini Unity MCP ile doğrula
(Sandbox_B.unity sahnesinde), bitince commit + push yap ve bana kısa rapor ver.
```

---

## 2. Makine Kurulumu (bir kez)
1. Ajan A "repo hazır" deyince: `git clone https://github.com/Thawne1455/Cinli-Mahzen.git`.
2. Unity Hub → **aynı sürüm: 6000.3.18f1** → projeyi aç.
3. Unity MCP'yi bağla, Claude Code'da MCP'nin bağlı olduğunu doğrula.
4. A "kontratlar hazır" (`A0.5`) deyince `git pull` → kod yazmaya başla.

---

## 3. Sorumluluk Alanın

| Alan | Klasör |
|---|---|
| Cin hareketi, enerji, iyi cin yetenekleri, algı | `Scripts/Jinn/` (CM.Jinn) |
| Possession durum makinesi, arbiter, aksiyonlar, mermiler | `Scripts/Possession/` (CM.Possession) |
| Görünürlük, outline, soğuk nefes | `Scripts/Visibility/` (CM.Visibility) |
| Cin HUD'ları (kötü + iyi) | `Scripts/UI/Jinn/`, `Prefabs/UI/Jinn/` |
| Cin prefabları & VFX | `Prefabs/Jinn/`, `Art/VFX/` |
| Possessable prefabları | `Prefabs/Possessables/` |
| Eşya tanımları | `ScriptableObjects/Possessables/` (`PD_*.asset`) |
| Test sahnesi | `Sandbox_B.unity` |
| Mesaj kodları | `MsgCode` **60–99** aralığı |
| Event'ler | `Scripts/Core/Events/` altında kendi dosyaların (`// Owner: B`) |

**❌ Dokunmadığın yerler:** `Scripts/Core` (event'lerin hariç) · `Game.unity` · `ProjectSettings` · A ve C'nin klasörleri.
Core'da bir değişiklik gerekiyorsa `docs/CONTRACT_CHANGES.md`'ye 🟡 satır yaz ve A'ya haber ver.

---

## 4. Görev Sırası

### 🟥 M0 — Kurulum

| # | ID | Görev | Kabul |
|---|---|---|---|
| 1 | B0.1 | `PossessableDefinition` + `PossessableActionDef` SO'ları. Düz C#: `PossessableStateMachine`, `PossessionArbiterCore` (6 kural), `JinnEnergyCore`, `CooldownTracker`. 10 adet `PD_*.asset` (Oyun §4 tablosu) | Her arbiter kuralı için + / − testi, yarış durumu, Öfke çarpanları → **tüm testler yeşil** |
| 2 | B0.2 | `Sandbox_B.unity` + placeholder cin görselleri (küre + gözler + parçacık; kötü mor, iyi turkuaz) | MCP screenshot |

> 💡 B0.1 tamamen düz C#. A'nın A0.5'ini bekler ama Unity sahnesine ihtiyaç duymaz, hemen başlayabilirsin.

### 🟧 M1 — Çekirdek Döngü

| # | ID | Görev | Kabul (MCP) |
|---|---|---|---|
| 3 | B1.1 | **SpiritController**: FPS uçuş, Space/Ctrl yüksel/alçal, Shift boost, noclip, level sınırı | Kötü cin duvardan geçiyor, sınırdan çıkamıyor |
| 4 | B1.2 | **Possession akışı**: `PossessionSystem`, `Possessable` (`IKickable`), girme telgrafı (1.2 sn titreme + gıcırtı hook'u), Space/E ile çıkma, `PossessionCameraBinder` → A'nın `CameraRig.SetOrbit`. `IPossessionQuery`'yi GameServices'e kaydet | Rafa gir → TPS kamera → çık → FPS. 10 sn içinde tekrar girme → `PossessDenied(Cooldown)` |
| 5 | B1.3 | `P_Poss_Base` + `Shelf`, `Barrel`, `Chair`, `Stool` varyantları (modeller C'den) | 4 prefab doğru `PD_*` ile |
| 6 | B1.4 | **İlk aksiyonlar:** `ToppleAction` (raf), `RollAction` (fıçı), `HopMove` + `LungeAction` (sandalye/tabure). Telgraf → otorite hit-check → sonuç | Raf devrilir → insan ölür → RoundEnd. Fıçı: 1 hasar + düşürme. Sandalye zıplıyor |
| 7 | B1.5 | Evil HUD minimal: enerji barı, aksiyon kartları, deny mesajı | Screenshot |

### 🟨 M2 — Tam Oynanış

| # | ID | Görev | Kabul (MCP) |
|---|---|---|---|
| 8 | B2.1 | **Kalan aksiyonlar:** Mimik sandık (`IInteractInterceptor`), Kılıç fırlatma, Meşale söndür/alev, Şişe fırlat (Drunk), Bira fıçısı patla + **projectile sistemi** (Teknik §6.4) | Her aksiyon dummy insana karşı test edildi |
| 9 | B2.2 | **VisibilityService** (Teknik §6.6 tablosu birebir) + XRay outline + insana soğuk nefes efekti | İyi cin: 20 m ruh / 4 m sinsi / 25 m şarj kuralları doğru |
| 10 | B2.3 | **İyi cin:** Kov (1.5 sn basılı + içerideki cine uyarı), İşaret (Q), Kutsa (R) | Kovulan cin 4 sn sersem, eşya 15 sn kutsanmış |
| 11 | B2.4 | Kötü cin algısı: ısı izi, gürültü halkası, takım arkadaşı outline | F3'te görünüyor, F1/F2'de görünmüyor |
| 12 | B2.5 | **Öfke modu** (altın alınınca enerji ×2, cooldown ×0.6) + kırmızı vinyet | |
| 13 | B2.6 | Evil HUD + Good HUD tam (Oyun §10 madde 5-6) | |
| 14 | B2.7 | **Dummy İnsan botu** (`BotInput : IHumanInput`, NavMesh ile gezer, kap arar), F12 | Bot kendi başına dolaşıyor |

### 🟩 M3 — Cila

| ID | Görev |
|---|---|
| B3.1 | **Juice:** possession ve aksiyon VFX'leri, ekran sarsıntısı, squash & stretch, komik zamanlama |
| B3.2 | **Runtime denge paneli:** debug overlay'de config ve `PD_*` değerleri için kaydırıcılar (playtest'te çok lazım) |
| B3.3 | Cin görselleri v2: yüz ifadeleri (bekliyor / şarj ediyor / sersem) |
| C3.5 (ortak) | Post-MVP eşyalar (Masa Kay, Diken, Tabak Gürültü): **prefab ve aksiyon sende**, soket C'de |

### 🟦 M4 — Online (EN SON)

| ID | Görev |
|---|---|
| B4.1 | Possession ve aksiyonların online doğrulaması. `PossessedMove` unreliable. Mimik "armed" bilgisi **sadece cin takımına ve menzildeki iyi cine** hedefli gönderilir |
| B4.2 | Görünürlük ve iyi cin yetenekleri online |
| M5 P5.2 | **Denge sorumlusu sensin:** playtest verisine göre config ayarları (hedef: arayıcı kazanma oranı %45-55) |

---

## 5. Beklediklerin → Beklerken Ne Yaparsın

| Kimden | Ne | Beklerken |
|---|---|---|
| A | **A0.5 Core kontratları** | Hiçbir şey — bunu bekle (kısa sürer) |
| A | A1.1 PawnBase + `IHumanInput` | B0.1 testleri, B0.2 görseller |
| A | A1.2 CameraRig | B1.1'i geçici kendi kameranla test et, sonra bağla |
| A | A1.4 HumanHealth (`IDamageable`) | Sandbox_B'de test için geçici `DummyDamageable` küp (kendi klasöründe) |
| A | A2.3 StatusController | Şişe/mimik aksiyonlarını status çağrısı hariç bitir |
| A | A2.4 NoiseEmitter | Gürültü halkasını test event'iyle yap |
| C | C0.2 Prop prefabları / modeller | Ham fbx'leri Sandbox_B'ye sürükleyerek test et |
| C | C1.5 LightService | Söndür aksiyonunu `StubLightService` ile yaz |
| C | C2.4 Altın akışı (`GoldStateEvt`) | Öfke'yi debug tuşuyla tetikleyerek test et |

## 6. Teslim Ettiklerin → Kim Bekliyor

| Sen teslim edersin | Bekleyen | Neden |
|---|---|---|
| B1.2 `IPossessionQuery`, `IKickable` | A | Fener parlatma, tekme |
| B1.3 Possessable prefabları | C | Populator bu prefabları haritaya yerleştirir |
| B2.1 `IInteractInterceptor` (mimik) | C | Kap açılmadan önce mimik kontrolü |
| B2.7 Dummy İnsan botu | Herkes | Tek başına cin testi |

---

## 7. Tasarım Notları (komedi senin elinde)
- **Telgraf kutsaldır.** Her saldırının önceden görsel + işitsel bir uyarısı olmalı. Uyarısız ölüm = sinir bozucu, uyarılı ölüm = komik.
- **Kısıtlı kontrol:** Eşyalar nişan almaz. A/D ile sınırlı dönebilir, o kadar. Iskalamak da komiktir.
- Aksiyon animasyonlarını `AnimationCurve` ile yap (Animator yok). Abartılı yap: raf devrilmeden önce "hıh" diye geriye esnesin.
- Oyun sonucunu etkileyen Rigidbody fiziği **yok**. Devrilme, yuvarlanma ve fırlatma kinematik. Kırık parçalar gibi kozmetik şeylerde fizik serbest.
- Tüm değerler `PD_*.asset` içinde. Koda sayı yazma.
