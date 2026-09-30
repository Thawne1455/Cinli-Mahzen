# CİNLİ MAHZEN — Asset Eşleştirme

> Kaynak: repo kökündeki `KayKit_Dungeon_Pack_1.1_FREE/` klasörü (ham paket, Unity import etmez) → **`Assets/fbx(unity)/`** (Unity için hazırlanmış fbx'ler) + `Assets/textures/dungeon_texture.png`.
> Tüm modeller **tek texture atlas** kullanır → tek materyal `M_KayKit_Dungeon`.
> Lisans: KayKit (CC0 — `License.txt` repo'ya `Art/KayKit/` altında kopyalanır).

---

## 1. Ölçüler (C0.1'de C doldurur)

| Model | Bounds (x × y × z) | Pivot | Not |
|---|---|---|---|
| `wall` | 4.00 × 4.00 × 1.00 | segment merkezi, taban y=0; uzunluk yerel X, kalınlık Z (±0.5) | Grid hücre boyutu buradan |
| `wall_doorway` | 4.00 × 4.00 × 1.00 | wall ile aynı; kapı mesh'i ayrı çocuk (`wall_doorway_door`), `Env_WallDoorway` prefabında gizli = açık geçit | |
| `wall_gated` | 4.00 × 4.00 × 1.00 | wall ile aynı (kapalı parmaklık) | Vault kapısı |
| `floor_tile_large` | 4.00 × 0.15 × 4.00 | hücre merkezi; y −0.10…+0.05 (üst yüzey +0.05) | |
| `floor_tile_small` | 2.00 × 0.15 × 2.00 | karo merkezi; y −0.10…+0.05 | |
| `stairs` | 5.00 × 5.10 × 4.00 | alt kenar ortası; +Z yönünde yükselir (z 0…4) | Çıkış |
| `shelf_large` | 2.00 × 0.45 × 0.50 | arka yüz z=0, öne doğru +0.5; ölçü tek raf bölümü | |
| `barrel_large` | 1.80 × 2.00 × 1.80 | taban merkezi | |
| `chest` | 1.70 × 1.30 × 1.45 | taban merkezi (z −0.70…+0.75) | |
| `chair` | 0.75 × 1.23 × 0.75 | taban merkezi | |

**Grid hücre boyutu (kesin):** `4 m` (wall/floor_tile_large 4 m) → `LevelGenSettings.CellSize` ve `02_GDD_Teknik.md §7.2`'ye yazılır.
**Duvar yüksekliği:** `4 m` (`LevelGenSettings.WallHeight = 4`; duvar kalınlığı 1 m, grid çizgisine ortalı → köşelerde `pillar` (1.5 m) boşluğu kapatır) → Spirit tavan sınırı (`§6.1`) buna göre.

---

## 2. Ortam (Environment) — Ajan C

| Kullanım | Model(ler) |
|---|---|
| Zemin (oda) | `floor_tile_large`, `floor_tile_small`, `floor_tile_small_decorated`, `floor_tile_small_broken_A/B`, `floor_tile_small_weeds_A/B` (rastgele varyasyon) |
| Zemin (ahşap oda — yatakhane/meyhane) | `floor_wood_large`, `floor_wood_large_dark`, `floor_wood_small(_dark)` |
| Zemin (toprak koridor) | `floor_dirt_*` |
| Duvar | `wall`, `wall_cracked`, `wall_broken` (varyasyon), `wall_half` |
| Köşe / bağlantı | `wall_corner`, `wall_corner_small`, `wall_Tsplit`, `wall_crossing`, `wall_endcap`, `wall_pillar` |
| Kapı açıklığı | `wall_doorway`, `wall_doorway_sides`, `wall_arched` |
| **Hazine kapısı (mühürlü)** | `wall_gated` (kapalı) → açılınca parmaklık aşağı iner / kaybolur; alternatif `wall_corner_gated` |
| Pencere süsü | `wall_window_closed`, `wall_archedwindow_gated` |
| Raf duvar | `wall_shelves` (dekor, possessable değil) |
| Sütun | `column`, `pillar`, `pillar_decorated` |
| **Çıkış** | `stairs` / `stairs_wide` / `stairs_walled` (yukarı çıkan merdiven + ışık huzmesi) |
| Tavan (opsiyonel) | `ceiling_tile` |
| Moloz / dekor | `rubble_half`, `rubble_large`, `crates_stacked`, `box_stacked`, `barrel_small_stack` |
| Temel | `floor_foundation_*` (harita kenarı) |
| İskele (dekor) | `wall_scaffold`, `wall_*_scaffold` |

---

## 3. Possessable Eşyalar — Ajan B (prefab), Ajan C (model import)

| Oyun eşyası | Model(ler) | Aranabilir kap mı? | SocketCategory |
|---|---|---|---|
| Raf | `shelf_large`, `shelves`, `shelf_small` (küçük: daha az hasar varyantı — post-MVP) | ✅ | WallLarge |
| Fıçı | `barrel_large`, `barrel_large_decorated`, `barrel_small` | ✅ | Floor, Corner |
| Sandalye | `chair` | ❌ | Floor |
| Tabure | `stool` | ❌ | Floor |
| Sandık (mimik) | `chest`, `trunk_large_A/B/C`, `trunk_medium_A/B/C`, `trunk_small_A/B/C` | ✅ | Floor |
| Kılıç-Kalkan | `sword_shield` (fırladıktan sonra `sword_shield_broken` yere düşer) | ❌ | WallMount |
| Meşale | `torch_mounted` (duvar), `torch_lit` | ❌ | WallMount |
| Mum | `candle_triple`, `candle_lit`, `candle_thin_lit` | ❌ | Table |
| Şişe | `bottle_A_brown/green`, `bottle_A_labeled_*`, `bottle_B_*`, `bottle_C_*` | ❌ | Table |
| Bira Fıçısı | `keg`, `keg_decorated` | ❌ | Corner |
| *(post-MVP)* Masa | `table_medium`, `table_long`, `table_small` (+ `_decorated`, `_tablecloth`) | ❌ | Floor |
| *(post-MVP)* Diken | `floor_tile_big_spikes` | ❌ | FloorTile |
| *(post-MVP)* Tabak yığını | `plate_stack` | ❌ | Table |

**Masalar** MVP'de possessable değil ama `Table` soketlerinin taşıyıcısıdır (şişe/mum masanın üstüne konur).

---

## 4. Hedef Nesneleri — Ajan C

| Oyun nesnesi | Model |
|---|---|
| Altın Sandığı | `chest_gold` |
| Hazine odası süsü | `coin_stack_large/medium/small`, `coin`, `sword_shield_gold` |
| Mühür Parçası (görsel) | `key` (altın renk tint yoksa olduğu gibi) — HUD ikonu da buradan render |
| Rün Taşı | ⚠️ Model yok → `floor_tile_small_decorated` duvara dikey + üstüne TMP sembol / basit quad |
| Kazı Noktası | `floor_tile_small_broken_A` |
| Rün İpucu Duvarı | Normal `wall` + SpiritOnly layer'da parlayan sembol quad'ları |
| Anahtar halkası (dekor) | `keyring_hanging` |

---

## 5. İnsan & Eşyalar — Ajan A

| Nesne | Model |
|---|---|
| Fener (FPS elde) | `torch_lit` (sap aşağı, elde tutulur) |
| Tuz (yerdeki pickup) | ⚠️ Model yok → `bottle_C_brown` beyaz tint veya `box_small` + "TUZ" etiketi |
| Nazar (pickup) | ⚠️ Model yok → mavi küre primitive + beyaz/siyah iç küreler (kodla oluşturulabilir) |
| İnsan karakteri | ⚠️ **YOK** — bkz. §6 |

---

## 6. ⚠️ Eksik Asset Listesi (kullanıcı temin edecek)

| Öncelik | Asset | Neden | Öneri (ücretsiz) | Geçici çözüm |
|---|---|---|---|---|
| 🔴 Yüksek | **İnsan karakter modeli + animasyonlar** (idle, yürü, koş, taşı, düş, kalk, ölüm) | Cinler insanı TPS/FPS'te görüyor | **KayKit Adventurers** (aynı stil, CC0) + KayKit Character Animations; alternatif Mixamo | Kapsül + şapka + fener (primitive) |
| 🟠 Orta | **Ses efektleri** (gıcırtı, titreme, devrilme, yuvarlanma, ısırık, tıngırtı, patlama, cam kırılması, tekme, adım, cin fısıltısı, UI) | Telgraf adaleti sese bağlı | Kenney Audio packs (CC0), freesound.org (CC0 filtre) | Sessiz + log |
| 🟠 Orta | **Türkçe karakterli font** | UI | Google Fonts: Nunito / Baloo 2 (stile uygun, yuvarlak) | TMP varsayılan (ğ/ş/ı eksik olabilir!) |
| 🟡 Düşük | **Ayrı kapı modeli** | "Kapı" possessable (post-MVP) | KayKit Dungeon Remastered (kapı içerir) | Kapı possessable'ı erteleniyor |
| 🟡 Düşük | **Tüfek / tromblon** | "Duvar Tüfeği" (post-MVP) | Quaternius / Kenney weapon packs | Erteleniyor |
| 🟡 Düşük | **Müzik** (menü + gerilimli ambiyans) | Atmosfer | Incompetech / Pixabay Music | Yok |
| 🟡 Düşük | **Cin VFX texture'ları** (duman, parıltı) | Cin görseli | Kenney Particle Pack (CC0) | URP default particle |

Yeni asset gelince: `Assets/_Project/Art/<Kategori>/` altına, lisans dosyasıyla birlikte, bu tabloya "✅ eklendi" notu.
