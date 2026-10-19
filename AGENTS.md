# AGENTS.md — Görev Yönlendirme ve Alt Ajan Atama Kuralları

Bu dosya, ana ajanın (orkestratör) gelen her görevi nasıl analiz edeceğini, parçalara ayıracağını ve her parçayı **görevin gerektirdiği zekâ seviyesine sahip** bir alt ajana nasıl atayacağını tanımlar.

Temel ilke: **En ucuz yeterli model.** Her iş, onu güvenilir şekilde yapabilecek en hafif modele verilir. Güçlü modeller yalnızca gerçekten gerektiğinde kullanılır.

---

## 1. Model Katmanları

| Katman | Model | Ne zaman | Örnek işler |
|---|---|---|---|
| **K1 – Hızlı** | `haiku` | Mekanik, net tanımlı, düşük risk | Dosya arama, grep, özet çıkarma, format dönüştürme, basit yeniden adlandırma, log tarama, test çalıştırıp sonucu raporlama |
| **K2 – Dengeli** | `sonnet` | Standart geliştirme ve analiz işleri | Özellik ekleme, hata ayıklama, birim test yazma, kod incelemesi, dokümantasyon yazma, orta büyüklükte refactor |
| **K3 – Derin** | `opus` | Çok adımlı akıl yürütme, belirsizlik, yüksek risk | Mimari tasarım, karmaşık hata kök nedeni analizi, güvenlik incelemesi, performans optimizasyonu, birden çok modülü etkileyen değişiklikler |
| **K4 – En Üst** | `fable` | Nadir; K3'ün iki denemede çözemediği veya çok yüksek zorluktaki işler | Yeni algoritma tasarımı, çok büyük kod tabanında kapsamlı yeniden yapılandırma, zor matematiksel/teknik problemler |

> Not: Model adları Claude Code'daki alt ajan `model` alanıyla uyumludur (`haiku`, `sonnet`, `opus`, `fable`). Ortamınızda bir model yoksa bir alt katmana düşün.

---

## 2. Görev Puanlama (Yönlendirme Algoritması)

Her görev için aşağıdaki 5 boyutu **0–2** arasında puanla:

| Boyut | 0 | 1 | 2 |
|---|---|---|---|
| **Belirsizlik** | Adımlar tamamen belli | Bazı kararlar gerekli | Problem tanımı bile net değil |
| **Kapsam** | Tek dosya / tek adım | Birkaç dosya / modül | Sistem geneli |
| **Akıl yürütme derinliği** | Mekanik | Mantıksal çıkarım | Çok adımlı, ödünleşimli kararlar |
| **Hata maliyeti** | Kolay geri alınır | Orta | Veri kaybı, güvenlik, üretim etkisi |
| **Yenilik** | Bilinen kalıp | Uyarlama gerekir | Özgün çözüm gerekir |

**Toplam puan → Katman:**

- **0–2 → K1 (`haiku`)**
- **3–5 → K2 (`sonnet`)**
- **6–8 → K3 (`opus`)**
- **9–10 → K4 (`fable`)**

### Zorunlu yükseltme kuralları (puandan bağımsız)
- Güvenlik, kimlik doğrulama, ödeme, şifreleme kodu → **en az K3**
- Veritabanı migrasyonu veya geri alınamaz işlem → **en az K3**
- Bir alt ajan aynı görevde **2 kez başarısız** olursa → **bir üst katmana** yükselt
- Kullanıcı açıkça "en iyi kalite" isterse → **en az K3**

### Zorunlu düşürme kuralları
- Yalnızca okuma/arama yapan işler → **K1** (sonuç yorumlaması gerekse bile, yorumu orkestratör yapar)
- Aynı işin tekrarlanan kopyaları (ör. 50 dosyaya aynı değişiklik) → kalıbı K2/K3 belirler, uygulamayı **K1** yapar

---

## 3. Görev Ayrıştırma

Büyük bir görev geldiğinde orkestratör:

1. **Anla:** İsteği tek cümleyle yeniden ifade et. Belirsizse ve yanlış tahmin pahalıysa kullanıcıya sor.
2. **Böl:** Görevi bağımsız alt görevlere ayır (her biri tek bir net çıktı üretmeli).
3. **Puanla:** Her alt görevi Bölüm 2'ye göre ayrı ayrı puanla.
4. **Sırala:** Bağımlılıkları belirle; bağımsız olanları **paralel** başlat.
5. **Ata:** Her alt görevi uygun ajan tipine ve modele ver (Bölüm 4).
6. **Doğrula:** Sonuçları birleştir, Bölüm 6'daki kontrolü uygula.

**Alt ajan açma eşiği:** Görev ana bağlamda 3 adımdan kısa sürecekse alt ajan açma — doğrudan yap. Her alt ajan sıfırdan başlar ve bağlamı yeniden kurmak maliyetlidir.

---

## 4. Ajan Rolleri

| Rol | Varsayılan model | Görev | Araçlar |
|---|---|---|---|
| **kesifci** (Explore) | `haiku` | Kod tabanında dosya/sembol bulma, yapı haritası çıkarma | Salt okunur: Glob, Grep, Read |
| **planlayici** (Plan) | `opus` | Uygulama planı, mimari kararlar, risk analizi | Salt okunur |
| **gelistirici** | `sonnet` | Kod yazma, değiştirme, refactor | Tüm araçlar |
| **testci** | `haiku` → `sonnet` | Testleri çalıştırma (haiku), test yazma (sonnet) | Bash, Read, Write |
| **inceleyici** | `opus` | Kod incelemesi, güvenlik ve doğruluk kontrolü — **işi yapan ajandan farklı olmalı** | Salt okunur |
| **arastirmaci** | `sonnet` | Web/dokümantasyon araştırması, kütüphane karşılaştırma | WebSearch, WebFetch, Read |
| **dokumantasyoncu** | `sonnet` | README, API dokümanı, değişiklik notları | Read, Write |
| **uzman** | `fable` | K3'ün çözemediği en zor problemler | Tüm araçlar |

Rol modeli, Bölüm 2 puanına göre yukarı veya aşağı değiştirilebilir. Örn. basit bir README düzeltmesi `dokumantasyoncu` + `haiku` olabilir.

---

## 5. Alt Ajana Görev Verme Şablonu

Her alt ajan **soğuk başlar**; ihtiyacı olan her şeyi istemde vermelisin:

```
## Görev
<Tek cümlelik net hedef>

## Bağlam
<İlgili dosya yolları, önceki bulgular, kısıtlar>

## Kapsam
- Yapılacak: <...>
- Yapılmayacak: <...>

## Başarı ölçütü
<Görevin bittiğini nasıl anlarız — ör. "tüm testler geçiyor">

## Çıktı formatı
<Ne döndürmeli — ör. "değişen dosyaların listesi + 3 cümlelik özet">
```

---

## 6. Doğrulama ve Kalite Kapısı

- **K1 çıktıları:** Orkestratör hızlıca göz atar.
- **K2 çıktıları:** Testler çalıştırılır; başarısızsa aynı ajana bir kez düzeltme şansı verilir, sonra yükseltilir.
- **K3/K4 çıktıları ve yüksek riskli işler:** İşi yapmamış, **bağımsız** bir `inceleyici` ajan kontrol eder (iş kendi kendini notlamasın).
- Sonuçlar birleştirilmeden önce çelişkiler giderilir.

---

## 7. Maliyet ve Verimlilik Kuralları

- Paralel çalışabilecek alt görevleri **aynı anda** başlat.
- Aynı konuda devam eden bir ajan varsa yeni ajan açma; mevcut olana mesaj gönder (`SendMessage`).
- Alt ajandan büyük dosya dökümleri değil, **sonuç ve özet** iste.
- Bir görev için en fazla **2 yükseltme** yapılır; K4'te de başarısız olunursa kullanıcıya durum raporlanır.

---

## 8. Hızlı Karar Örnekleri

| Görev | Puan | Atama |
|---|---|---|
| "Projede `getUser` fonksiyonu nerede kullanılıyor?" | 1 | kesifci + `haiku` |
| "Login formuna e-posta doğrulaması ekle" | 4 | gelistirici + `sonnet` → testci + `haiku` |
| "Uygulama zaman zaman donuyor, nedenini bul" | 7 | planlayici + `opus` (analiz) → gelistirici + `sonnet` (düzeltme) → inceleyici + `opus` |
| "Ödeme modülünü yeni sağlayıcıya taşı" | 8 + zorunlu yükseltme | planlayici + `opus` → gelistirici + `opus` → inceleyici + `opus` |
| "300 dosyada eski import yolunu değiştir" | 2 (tekrarlı) | Kalıbı orkestratör belirler → gelistirici + `haiku` (paralel gruplar) |
| "Dağıtık sistem için yeni bir konsensüs algoritması tasarla" | 10 | uzman + `fable` → inceleyici + `opus` |

---

## 9. Raporlama

Görev tamamlandığında orkestratör kullanıcıya şunu verir:
- Ne yapıldığı (kısa özet)
- Hangi alt görevin hangi modelle yapıldığı (istenirse)
- Açık kalan riskler veya önerilen bir sonraki adım
