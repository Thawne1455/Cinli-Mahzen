# CİNLİ MAHZEN — Oyun Tasarım Dokümanı (Oyuncu / Tasarım Tarafı)

> Çalışma adı: **Cinli Mahzen**. Sürüm: GDD v1.0 — 2026-09-30
> Bu doküman "oyun ne, nasıl oynanır, nasıl hissettirir" sorularını cevaplar.
> Teknik karşılıklar için: `02_GDD_Teknik.md`. Görev listesi: `03_TODO.md`.
> **Buradaki tüm sayısal değerler `GameBalanceConfig` ScriptableObject'inde birebir aynı isimle bulunur.** Sayı değişirse önce config değişir, sonra bu doküman güncellenir.

---

## 1. Tek Cümlede Oyun

Bir define avcısı, cinli bir mahzende altın arıyor; yanında onu koruyan **iyi bir cin** var, mahzendeki **iki kötü cin** ise eşyaların içine girip adamı "kaza süsü vererek" öldürmeye çalışıyor.

- **Tür:** Asimetrik 2v2 online co-op/PvP parti oyunu
- **Oyuncu:** Tam 4 kişi (1 İnsan + 1 İyi Cin **vs** 2 Kötü Cin)
- **Maç:** 4 raund, her raund ~10 dk. Her raundda insan rolü döner → herkes 1 kez insan olur.
- **Ton:** Korku-komedi. Gergin ama salak. Ölümler komik, asla sinir bozucu değil.
- **Kamera:** Herkes her zaman **FPS**. Kötü cin bir eşyanın içine girince **TPS** (eşyanın etrafında dönen kamera).
- **İletişim:** Discord. **Takımlar ayrı ses kanalında olmalı** (İnsan+İyi Cin bir kanal, 2 Kötü Cin bir kanal). Oyun lobi ekranında bunu hatırlatır.
- **Görsel:** KayKit Dungeon (low-poly, sevimli, renkli). Karanlık ama okunaklı.

### Tasarım Sütunları (her karar bunlara göre verilir)
1. **Herkes her an meşgul.** Sıra beklemek yok. Kötü cin bile "boşta" iken tuzak kuruyor, iz sürüyor.
2. **Adil telgraf (telegraph).** Her saldırının önceden bir işareti var (titreme, gıcırtı, parlama). Dikkatli insan hayatta kalır. Ölüm "haksızlık" değil "salaklık" hissettirmeli.
3. **Kısıtlı kontrol = komedi.** Eşyalar nişan alamaz, beceriksizdir. Iskalamak da komiktir.
4. **Bilgi asimetrisi.** Her rol farklı şey görür. Konuşmak zorunlu, konuşmak da tehlikeli (ses kanalı ayrı ama insan panik yapar).

---

## 2. Hikaye & Tema

Eski bir kervansarayın altındaki unutulmuş **zindan/mahzen**. Rivayete göre dipte bir **altın sandığı** var ama mahzen cinli. Oyuncu define avcısıdır; yanında dedesinden kalma, sevimli bir **iyi cin** dolaşır. Mahzendeki **kötü cinler** ise misafirlerini sevmez ve "kaza" süsü vermeyi çok sever.

- Türk folkloru tatları: **tuz** (cin kovar), **nazar boncuğu** (bir kere korur), **define**, **mühür**.
- Süre dolarsa: *"Bekçi geldi — define avcısı kaçak kazıdan gözaltına alındı."* (Kötü cinler kazanır.)
- Ölüm sonrası **Otopsi Raporu**: *"Ölüm sebebi: Kendiliğinden devrilen raf. Şahitler: 1 fıçı, 2 şişe."*

---

## 3. Roller

### 3.1 🧔 İNSAN — Define Avcısı (Takım: Arayıcılar)
**Amaç:** 3 mühür parçasını topla → hazine odasını aç → altın sandığını al → çıkışa ulaş.

| Özellik | Değer (`config` adı) |
|---|---|
| Can | 3 (`HumanMaxHp`) |
| Hasar sonrası dokunulmazlık | 1.5 sn (`HumanInvulnAfterHit`) |
| Yürüme / Koşma | 3.5 / 5.5 m/s (`HumanWalkSpeed`, `HumanSprintSpeed`) |
| Sandık taşırken | 2.4 m/s, koşamaz (`HumanCarrySpeed`) |
| Stamina | 100, koşu −20/sn, dolum +15/sn (1 sn bekleme sonra) |
| Etkileşim menzili | 2.2 m (`InteractRange`) |
| Arama süresi (sandık/raf/fıçı arama) | basılı tut 1.0 sn (`SearchHoldTime`) |

**Yetenekleri**
- **Fener (her zaman elinde):** Ortamı aydınlatır. **Sağ tık = Parlat:** 8 m'lik koni içinde cin girmiş eşyalar 3 sn boyunca hafif titreşen bir parıltı gösterir. Bekleme 12 sn. (`LanternPulseRange`, `LanternPulseDuration`, `LanternPulseCooldown`)
- **Tekme (F):** 2 m menzil. Cin girmiş eşyaya vurursa içindeki cin 2.5 sn **sersemler** (eşya aksiyon yapamaz). Bekleme 6 sn. Cin yokken tekme atarsa sadece komik ses + istatistiğe "Boşa Tekme" yazılır.
- **Envanter:** 2 eşya slotu (1/2 ile seç, Q ile kullan).
  - 🧂 **Tuz:** Yere serper. 3 m yarıçap, 25 sn: alandaki eşyalara cin **giremez**, içeride olan cin **dışarı fırlar**. (`SaltRadius`, `SaltDuration`)
  - 🧿 **Nazar Boncuğu:** Pasif. İlk ölümcül darbede canı 1'de bırakır, kırılır. (Taşınan anda aktif.)
- **Gürültü:** Koşmak, tekme, arama ve kazma **gürültü** üretir → kötü cinler gürültüyü uzaktan görür (bkz. 3.3 Algı).

**His:** Paranoya. Her rafa şüpheyle bakmak. Sonra bir tabureye 10 sn boyunca tekme atmak.

### 3.2 😇 İYİ CİN — Koruyucu (Takım: Arayıcılar)
**Amaç:** İnsanı hayatta tutmak, mühür bulmacalarını çözdürmek.

| Özellik | Değer |
|---|---|
| Hareket | FPS uçuş, 7 m/s, duvarlardan geçer (`GoodJinnSpeed`) |
| Fiziksel etkileşim | Yok (eşya taşıyamaz, kapı açamaz) |

**Görüş (en önemli gücü):**
- Ruh formundaki kötü cinleri **20 m** içinde duvar arkasından (outline) görür. (`GoodSightSpiritRange`)
- Eşyanın içinde **hareketsiz bekleyen** (sinsi) kötü cini sadece **4 m** içinden sezer. (`GoodSightLurkRange`)
- **Aksiyon şarj eden** (saldırmak üzere olan) eşyayı **25 m** içinden parlak kırmızı görür. (`GoodSightChargeRange`)
- **Ruh İpuçlarını** görür: duvar yazıları, hayalet ayak izleri, parlayan sandıklar (sadece o görür).

**Yetenekleri:**
- **Kov (E basılı tut 1.5 sn, 3 m):** İçerideki kötü cini dışarı fırlatır: 4 sn sersem + 4 sn eşyaya giremez. Eşya 15 sn **kutsanır**. Bekleme 18 sn. *(Basılı tutarken içerideki cin uyarı görür ve kaçabilir → akıl oyunu.)* (`ExorciseHoldTime`, `ExorciseCooldown`, `ExorciseStun`)
- **İşaret (Q):** Baktığı noktaya/eşyaya 8 sn boyunca insanın da gördüğü bir parıltı koyar. Max 2 aktif, bekleme 3 sn. Discord'da "şurası" demenin kesin hali.
- **Kutsa (R):** Bir eşyayı 20 sn boyunca cin giremez yapar. Max 1 aktif, bekleme 25 sn.

**His:** Kaotik taktisyen. "Bodruma bakıyorum… DUR O RAFA YAKLAŞMA!"

### 3.3 😈 KÖTÜ CİN ×2 — Eşya Canlandırıcılar (Takım: Cinler)
**Amaç:** Süre bitmeden insanı öldürmek.

| Özellik | Değer |
|---|---|
| Ruh formu hareketi | FPS uçuş, 6.5 m/s, duvarlardan geçer (`EvilJinnSpeed`) |
| İnsan görebilir mi? | Hayır (ruh formunda görünmez) |
| Enerji | Max 100, başlangıç 50, dolum 5/sn (`EnergyMax`, `EnergyStart`, `EnergyRegen`) |
| Eşyaya girme | 3 m menzil, 1.2 sn (`PossessRange`, `PossessTime`) — **bu sürede eşya titrer ve gıcırdar** |
| Aynı eşyaya tekrar girme | 10 sn bekleme (`ReenterCooldown`) |
| Eşyadan çıkma | Anında |

**Kurallar:**
- Aynı eşyada aynı anda **tek** cin olabilir.
- Eşyanın içindeyken **sinsi mod**: aksiyon yapmıyorsan iyi cin seni sadece 4 m içinden sezer.
- Aksiyon şarj ederken parlarsın ve telgraf verirsin (ses + titreme + ışık).
- İyi cin kötü cini **hasar veremez**, sadece kovar. Kötü cinler de iyi cine bir şey yapamaz. İyi cini **soluk** görürler (koruyucunun nerede olduğunu bilirler).

**Algı (insanı nasıl buluyorlar):**
- İnsanı normal görüş hattında görürler.
- İnsan **5 sn süren sıcak bir iz** bırakır (sadece kötü cinler görür).
- İnsanın ürettiği **gürültü** 15 m içindeki kötü cinlerde bir halka olarak görünür (`NoisePingRange`).
- **Takım arkadaşı** her zaman işaretli görünür.

**ÖFKE modu:** İnsan altın sandığını aldığı anda: enerji dolumu ×2, tüm bekleme süreleri ×0.6. (`RageRegenMult`, `RageCooldownMult`)

**His:** Pusu kuran, beceriksiz bir poltergeist. "Sen halıyla… yok halı yok. Sen fıçıyla it, ben raftan düşüreyim!"

**Boş kalmama garantisi:** Enerji dolarken bile yapılacak iş var: tuzak kurmak (sandık mimiği kurmak), eşyayı pozisyona taşımak (tabure/fıçı), iz sürmek, ışık söndürmek, iyi cini oyalamak.

---

## 4. Eşyalar (Possessable) — MVP Listesi

Tüm eşyaların **kısıtlı kontrolü** var. Nişan yok; en fazla sınırlı yön ayarı var.
Kontroller eşyanın içindeyken: **Sol tık = Aksiyon 1**, **Sağ tık = Aksiyon 2**, **A/D = sınırlı döndürme** (izin varsa), **W/A/S/D = hareket** (sadece hareketli eşyalarda), **Space veya E = çık**.

| # | Eşya (asset) | Aksiyon | Telgraf | Etki | Enerji | Bekleme | Tek kullanımlık? |
|---|---|---|---|---|---|---|---|
| 1 | **Raf** (`shelf_large`, `shelves`) | **Devril** (sol tık) | 1.0 sn öne eğilme + gıcırtı | Önündeki 2×1.5 m alan: **3 hasar (öldürür)** | 40 | — | ✅ Devrildi mi yerde kalır |
| 2 | **Fıçı** (`barrel_large`, `barrel_small`) | **Yuvarlan** (sol tık basılı şarj) | 0.8 sn şarj, titreme | A/D ile yön (sadece cinler ok görür). 8 m/s, 12 m menzil. 1 hasar + 1.5 sn yere düşürme | 20 | 6 sn | ❌ Durduğu yerde kalır, tekrar kullanılabilir |
| 3 | **Sandalye / Tabure** (`chair`, `stool`) | **Zıpla** (WASD) / **Çarp** (sol tık) | Çarp: 0.4 sn geri çekilme | Zıpla: 0.5 sn'de 1 m, 2 enerji/zıplama. Çarp: 2 m atılma, 1 hasar | 2 / 15 | — / 5 sn | ❌ Cinin "arabası" |
| 4 | **Sandık** (`chest`, `trunk_*`) | **Mimik Kur** (sol tık: kur/boz) | Sadece girerken titreme; kurulunca sessiz | İnsan açarsa: ısırır, 1 hasar + 2 sn tutar (hareket edemez) | 25 (tetiklenince) | 15 sn | ❌ |
| 5 | **Kılıç-Kalkan** (`sword_shield`) | **Fırla** (sol tık) | 0.7 sn tıngırdama | A/D ±35°. 14 m/s düz mermi, 15 m menzil, 1 hasar | 25 | — | ✅ Yere düşer |
| 6 | **Meşale / Mum** (`torch_mounted`, `candle_triple`) | **Söndür** (sol tık) / **Alev** (sağ tık) | Söndür: 0.3 sn cızırtı. Alev: 0.6 sn parlama | Söndür: odanın ışıkları 20 sn kapanır. Alev: 2 m koni, 1 hasar | 15 / 30 | 30 sn / 12 sn | ❌ |
| 7 | **Şişe** (`bottle_*`) | **Fırlat** (sol tık) | 0.5 sn sallanma | A/D ±45°, yay çizen atış. İsabet: **Sarhoş** 4 sn (ekran sallanır, fare ters) — hasar yok | 10 | — | ✅ Kırılır |
| 8 | **Bira Fıçısı** (`keg`, `keg_decorated`) | **Patla** (sol tık basılı 3.5 sn) | Çok belirgin: tıslama, kırmızı parlama, şişme | 0-3 m: **3 hasar**, 3-5 m: 1 hasar + düşürme | 60 | — | ✅ Paramparça |

### MVP Sonrası Eşyalar
| Eşya | Aksiyon | Not |
|---|---|---|
| Masa (`table_*`) | **Kay** — 3 m iter, duvara sıkıştırırsa 1 hasar | Asset var |
| Diken zemin (`floor_tile_big_spikes`) | **Tetikle** — 0.6 sn sonra dikenler çıkar, 1 hasar | Asset var |
| Tabak yığını (`plate_stack`) | **Gürültü** — 10 m içinde telgraf seslerini bastırır, 6 sn | Asset var, destek eşyası |
| Duvar Tüfeği | **Ateş** — sabit yön, sadece ateş eder, hafif sekme | ⚠️ **Asset gerekli** |
| Kapı | **Çarp / 5 sn Kilitle** | ⚠️ **Ayrı kapı asset'i gerekli** (KayKit'te kapı duvarla bir parça) |

### Kombo Örnekleri (cinlerin keşfedeceği)
- Şişe (sarhoş) → Raf devril (sarhoş insan telgrafı göremez)
- Meşale söndür → Fıçı yuvarla (karanlıkta fark etmez)
- Sandık mimik (tutar 2 sn) → Bira fıçısı patlat (tutulan insan kaçamaz… ama 3.5 sn şarj, iyi cin görür ve kovar → dengeli)

---

## 5. Hedefler, Bulmacalar ve Maç Akışı

### 5.1 Raund Akışı

```
[Rol Açıklama 3 sn] → [Giriş 5 sn geri sayım]
      → KEŞİF FAZI  (0 → 3 mühür parçası)
      → HAZİNE FAZI (mühürlü kapı açıldı, sandığa ulaş)
      → KAÇIŞ FAZI  (sandık alındı → ÖFKE modu → çıkışa koş)
      → [Raund Sonu: Otopsi Raporu 8 sn] → sonraki raund
```

- **Raund süresi:** 10 dk (`RoundDuration`). Sayaç herkesin ekranında.
- **Cinler Uyanıyor:** İlk 20 sn kötü cinler eşyaya giremez (`JinnWakeDelay`). İnsan nefes alsın.
- **Kazanma:**
  - **Arayıcılar:** Altın sandığıyla çıkışa ulaşmak.
  - **Cinler:** İnsanın ölmesi **veya** sürenin dolması ("Bekçi geldi").

### 5.2 Mühür Parçaları (3 adet, sırası serbest)

| Parça | Nasıl alınır | İyi cinin rolü | İnsanın rolü |
|---|---|---|---|
| **1. Kayıp Parça** | Haritadaki ~8-12 aranabilir kaptan (sandık, fıçı, raf) **birinin** içinde | Doğru kabı 6 m içinden **ruh parıltısıyla** görür | Aramak (1 sn basılı tut, gürültü yapar). Mimik sandık riski! |
| **2. Rün Bulmacası** | Bir odada 4 rün taşı (duvarda). Doğru sırayla basılmalı | Sıralama **başka bir odadaki** duvarda ruh yazısı olarak yazılı. Sadece o görür | Taşlara basar. **Yanlış basış:** sıfırlanır + büyük gürültü (cinlere konum verir) |
| **3. Hayalet İzleri** | Bir odadaki heykel/sütundan başlayan **hayalet ayak izleri**, gevşek bir zemin taşına gider | İzleri görür, insanı yönlendirir | Doğru taşı **kazar** (3 sn basılı tut — savunmasız, çok gürültülü) |

- Her parça alındığında herkes bildirim görür ("Mühür parçası: 2/3").
- **3/3 → Mühürlü kapı** (`wall_gated`) açılır. Hazine odasında **Altın Sandığı** (`chest_gold`) var.

### 5.3 Altın Sandığı (Kaçış Fazı)
- İnsan sandığı iki eliyle taşır: **eşya kullanamaz, tekme atamaz, fener parlatamaz**, hız 2.4 m/s.
- Hasar alırsa sandığı **düşürür**; tekrar almak 1 sn.
- G ile isteyerek bırakabilir (ör. önce yolu temizlemek için).
- Çıkış noktası (merdiven, `stairs`) hazine odasından **en uzak** bölgede seçilir.
- Sandık alındığı anda: tüm oyunculara "ALTIN ALINDI!" + kötü cinlere **ÖFKE**.

---

## 6. Puanlama & Maç Sonu

| Olay | Puan |
|---|---|
| Arayıcılar kazandı | İnsan +3, İyi Cin +2 |
| Cinler kazandı (öldürme) | Her kötü cin +2, öldürücü darbeyi vuran +1 bonus |
| Cinler kazandı (süre) | Her kötü cin +1 |
| Mühür parçası alındı | İnsan +0 (sadece istatistik) |

4 raund sonunda **Maç Sonu Ekranı**: puan sıralaması + komik unvanlar (istatistikten hesaplanır):
- **"Mobilya Katili"** — en çok hasar veren eşya kullanıcısı
- **"Tekmeci Dayı"** — en çok boşa tekme atan
- **"Tuz Baba"** — en çok tuz kullanan
- **"Sigortacı"** — en çok kovma yapan iyi cin
- **"Kör Bekçi"** — en çok ıskalayan cin
- **"Kendi Kendine"** — kendi tuzağına en çok takılan (ör. kendi fıçısıyla takım arkadaşını kovdurtan)

### Otopsi Raporu (her raund sonu, 8 sn)
- Ölüm sebebi (eşya + aksiyon + katil cin adı), ölüm anının küçük özeti.
- Raund istatistikleri: düşme sayısı, boşa tekme, ıskalanan atış, kovma sayısı, süre.
- İnsan kazandıysa: "Define avcısı zengin oldu. Cinler vergi dairesine şikayet etti."

---

## 7. Kontroller

### İnsan (FPS)
| Tuş | Eylem |
|---|---|
| WASD / Fare | Hareket / Bakış |
| Shift | Koş |
| E (basılı) | Etkileşim / Ara / Kaz / Al |
| F | Tekme |
| Sağ Tık | Fener Parlat |
| 1 / 2 | Eşya slotu seç |
| Q | Seçili eşyayı kullan |
| G | Altın sandığını bırak |
| Tab | Skor / hedef |
| Esc | Menü |

### Cin — Ruh Formu (FPS uçuş)
| Tuş | Eylem |
|---|---|
| WASD / Fare | Uç / Bak |
| Space / Ctrl | Yüksel / Alçal |
| Shift | Hızlı uçuş (×1.5, sadece ruh formu) |
| E | **Kötü:** Eşyaya gir (bakılan, 3 m içi) / **İyi:** Kov (basılı) |
| Q | **İyi:** İşaret koy |
| R | **İyi:** Kutsa |

### Kötü Cin — Eşyanın İçinde (TPS)
| Tuş | Eylem |
|---|---|
| Fare | Kamerayı eşyanın etrafında döndür (nişan **değil**) |
| Sol / Sağ Tık | Aksiyon 1 / 2 |
| A / D | Sınırlı döndürme (izin varsa) |
| WASD | Hareket (sadece sandalye/tabure) |
| Space / E | Eşyadan çık |

---

## 8. Kamera & His

| Durum | Kamera | FOV | Not |
|---|---|---|---|
| İnsan | FPS, hafif baş sallanması | 75 | Elinde fener modeli (`torch_lit`) görünür |
| Cin ruh formu | FPS, hafif süzülme, renkli vinyet (kötü: mor, iyi: turkuaz) | 85 | Dünya hafif desature |
| Kötü cin eşyada | TPS orbit, eşyaya 3.5 m, çarpışmadan kaçan | 70 | Geçiş: 0.3 sn yumuşak lerp. Aksiyon yönü okla gösterilir (sadece cinler görür) |
| İnsan ölümü | Ragdoll + 2 sn yavaş çekim kill-cam, sonra Otopsi | — | Komik ses efekti |

---

## 9. Görsel & İşitsel İpuçları (Telgraf Sistemi — oyunun adaleti buna bağlı)

| Durum | İnsan ne görür/duyar | İyi cin | Kötü cin |
|---|---|---|---|
| Kötü cin ruh formunda yakında (3 m) | Hafif **soğuk nefes** buğusu + ürperti sesi | Kötü cini görür | — |
| Eşyaya girme (1.2 sn) | Eşya **titrer + gıcırdar** (3D ses) | Görür | — |
| Eşyada sinsi bekleme | Hiçbir şey (fener parlatınca parıltı) | Sadece 4 m içi | Takım arkadaşı görür |
| Aksiyon şarjı | Eşyaya özel telgraf (tablodaki) | Parlak kırmızı, 25 m | — |
| Kovma basılı tutuluyor | — | İlerleme çubuğu | İçerideki cine **"KOVULUYORSUN!"** uyarısı |

---

## 10. UI Ekranları

1. **Ana Menü:** Oyna (Online — M4), Test Oyunu (offline), Ayarlar, Çıkış
2. **Lobi (M4):** Oda kodu, 4 oyuncu slotu, hazır butonu, Discord takım kanalı hatırlatması
3. **Rol Açıklama (3 sn):** Büyük yazı "SEN İNSANSIN / SEN İYİ CİNSİN / SEN KÖTÜ CİNSİN" + takım arkadaşının adı
4. **HUD — İnsan:** Can (3 kalp), stamina, 2 eşya slotu, fener bekleme, tekme bekleme, mühür 0/3, sayaç, etkileşim ipucu (ortada)
5. **HUD — İyi Cin:** Kov/İşaret/Kutsa bekleme süreleri, insanın canı, mühür 0/3, sayaç, insanın yönünü gösteren kenar oku
6. **HUD — Kötü Cin:** Enerji barı, aksiyon kartları (tuş + maliyet + bekleme), takım arkadaşı durumu, sayaç, ÖFKE göstergesi
7. **Otopsi Raporu** (raund sonu)
8. **Maç Sonu** (puanlar + unvanlar)
9. **Duraklatma / Ayarlar:** Fare hassasiyeti, FOV, ses seviyeleri, grafik kalitesi

Dil: **Türkçe** öncelikli. Tüm metinler anahtar tabanlı (`Loc`) — İngilizce sonra eklenebilir. Font Türkçe karakter (ğüşıöçİ) desteklemeli.

---

## 11. Harita Kuralları (Level Generator için tasarım gereksinimleri)

Prosedürel harita üreticisi kullanıcı tarafından dışarıdan eklenecek. Oyun tasarımı açısından haritanın sağlaması gerekenler:

- **Boyut:** 8-14 oda, tek kat (MVP). Toplam yürüme mesafesi uçtan uca ~60-90 m.
- **Başlangıç odası:** İnsan burada doğar. Cinler haritanın öteki yarısında doğar.
- **Hazine odası:** Tek girişli, girişi **mühürlü kapı** (`wall_gated`).
- **Çıkış:** Hazine odasından en uzak odalardan biri (min 35 m yol mesafesi).
- **Bulmaca odaları:** Rün odası, rün ipucu odası (farklı oda, en az 2 oda uzakta), hayalet iz odası.
- **Eşya yoğunluğu:** Oda başına 2-5 possessable, koridorlarda 0-1. Toplam ~30-45.
- **Aranabilir kaplar:** 8-12 (bir kısmı possessable sandık/fıçı).
- **Işık:** Her odada en az 1 meşale/mum (Söndür aksiyonu için).
- **Döngü (loop) koridorlar:** En az 1 döngü olmalı ki insan kovalamaca sırasında çıkmaza sıkışmasın.

Teknik kontrat: `02_GDD_Teknik.md` → §8 Level Kontratı.

---

## 12. Denge — Tek Tablo (ilk değerler, playtest ile değişecek)

| Config adı | Değer | Açıklama |
|---|---|---|
| `RoundDuration` | 600 | sn |
| `JinnWakeDelay` | 20 | sn |
| `IntroCountdown` | 5 | sn |
| `RoundsPerMatch` | 4 | |
| `KeyFragmentsRequired` | 3 | |
| `HumanMaxHp` | 3 | |
| `HumanInvulnAfterHit` | 1.5 | sn |
| `HumanWalkSpeed` / `HumanSprintSpeed` / `HumanCarrySpeed` | 3.5 / 5.5 / 2.4 | m/s |
| `StaminaMax` / `StaminaDrain` / `StaminaRegen` / `StaminaRegenDelay` | 100 / 20 / 15 / 1.0 | |
| `InteractRange` | 2.2 | m |
| `SearchHoldTime` / `DigHoldTime` / `PickupGoldTime` | 1.0 / 3.0 / 1.0 | sn |
| `KickRange` / `KickCooldown` / `KickStun` | 2.0 / 6 / 2.5 | |
| `LanternPulseRange` / `LanternPulseDuration` / `LanternPulseCooldown` | 8 / 3 / 12 | |
| `SaltRadius` / `SaltDuration` / `SaltStartCount` | 3 / 25 / 0 | Tuz kaplardan bulunur |
| `NazarStartCount` | 0 | Kaplardan bulunur |
| `ItemSlotCount` | 2 | |
| `EvilJinnSpeed` / `GoodJinnSpeed` / `SpiritBoostMult` | 6.5 / 7 / 1.5 | |
| `EnergyMax` / `EnergyStart` / `EnergyRegen` | 100 / 50 / 5 | |
| `PossessRange` / `PossessTime` / `ReenterCooldown` | 3 / 1.2 / 10 | |
| `ExorciseRange` / `ExorciseHoldTime` / `ExorciseCooldown` / `ExorciseStun` / `ExorcisePossessLock` / `BlessAfterExorcise` | 3 / 1.5 / 18 / 4 / 4 / 15 | |
| `PingDuration` / `PingCooldown` / `PingMaxActive` | 8 / 3 / 2 | |
| `BlessDuration` / `BlessCooldown` / `BlessMaxActive` | 20 / 25 / 1 | |
| `GoodSightSpiritRange` / `GoodSightLurkRange` / `GoodSightChargeRange` | 20 / 4 / 25 | m |
| `HeatTrailDuration` / `NoisePingRange` | 5 / 15 | |
| `ColdBreathRange` | 3 | m |
| `RageRegenMult` / `RageCooldownMult` | 2.0 / 0.6 | |
| `LootTable`: Tuz %25, Nazar %10, Boş %65 | | Mühür parçası kabı hariç |

Possessable değerleri §4 tablosundadır ve her birinin `PossessableDefinition` asset'inde tutulur.

---

## 13. Kapsam Dışı (MVP'de YOK)
- Oyun içi sesli sohbet (Discord kullanılıyor; ileride Photon Voice eklenebilir)
- Çok katlı haritalar
- Kozmetik / kilit açma / ilerleme sistemi
- Bot oyuncular (sadece geliştirme amaçlı "Dummy İnsan" botu var)
- Matchmaking (sadece oda koduyla arkadaş odası)
- Ortada oyuna katılma (late join)
