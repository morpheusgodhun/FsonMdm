# FSON MDM

Kurumsal Android cihazları merkezi olarak yöneten, politika uygulayan, kiosk
moduyla kilitleyen ve uzaktan komut çalıştıran tam çalışır bir **Mobile Device
Management (MDM)** sistemi.

Sistem iki parçadan oluşur:

| Parça | Teknoloji | Klasör |
|-------|-----------|--------|
| Backend API | ASP.NET Core 8 · EF Core · SQLite · JWT · Clean Architecture · Multi-Tenant | `backend/` |
| Yönetim Paneli (Dashboard) | ASP.NET Core MVC · Razor · Cookie Auth (aynı API projesi içinde) | `backend/src/FsonMdm.Api/Dashboard/` + `Views/` |
| Android Agent (DPC) | Kotlin · DevicePolicyManager · Foreground Service · WorkManager · MediaProjection | `android/FsonMdmAgent/` |

> **Kapsam notu:** Bu sistem cihazın **zaten Device Owner olarak hazırlandığını**
> varsayar. Enrollment, QR ve Device Owner kurulumu kapsam dışıdır.

---

## 1. Backend

### Mimari

```
backend/
├─ FsonMdm.sln
└─ src/
   ├─ FsonMdm.Domain          # Entity'ler, enum'lar (bağımsız çekirdek)
   ├─ FsonMdm.Application      # DTO, servis arayüzleri, iş kuralları, JWT sözleşmesi
   ├─ FsonMdm.Infrastructure   # EF Core DbContext, repository'ler, JWT üretimi, migration
   └─ FsonMdm.Api              # Controller'lar, middleware, Program.cs, Swagger
```

Katmanlar tek yönlü bağımlıdır: `Api → Infrastructure → Application → Domain`.
İş mantığı servis katmanındadır; controller'lar incedir.

### Çalıştırma

**Visual Studio ile:** `backend/FsonMdm.sln` dosyasını açın, `FsonMdm.Api`
projesini başlangıç projesi yapıp çalıştırın.

**Komut satırı ile:**

```bash
cd backend/src/FsonMdm.Api
dotnet restore
dotnet run
```

İlk açılışta veritabanı otomatik migrate edilir ve demo verisi tohumlanır
(`Migrate` + `DbSeeder`). SQLite dosyası `fsonmdm.db` olarak oluşur.

> **Şema güncellemesi (önemli):** Bu sürümde cihaz konumu, ekran görüntüsü,
> uygulama envanteri ve yüklenen APK kataloğu için yeni tablolar/kolonlar tek
> `InitialCreate` migration'ına eklendi. Geliştirme ortamında mevcut bir
> `fsonmdm.db` dosyanız varsa, yeni şemanın oluşması için **dosyayı silin**;
> uygulama tekrar çalışınca veritabanını yeniden oluşturup tohumlar.

Adresler (`launchSettings.json`):

- HTTP: `http://localhost:5080`
- HTTPS: `https://localhost:7080`
- Swagger UI: kök adres (`/`) → `/swagger`

### Demo kimlik bilgileri (DbSeeder)

| Alan | Değer |
|------|-------|
| Tenant | `FSON Demo Tenant` |
| Enrollment Key | `FSON-DEMO-ENROLLMENT-KEY` |
| Admin kullanıcı | `admin` |
| Admin şifre | `Admin123!` |

> **Üretim notu:** `appsettings.json` içindeki `Jwt:Key` değeri yer tutucudur.
> Üretimde en az 32 karakterlik rastgele bir gizli anahtarla değiştirin ve
> `web.config` ortam değişkeni olarak saklayın.

### Kimlik doğrulama modeli

- **Admin** → `POST /api/auth/login` ile kullanıcı adı/şifre vererek admin JWT alır.
- **Cihaz** → `POST /api/device/register` çağrısında `X-Enrollment-Token`
  başlığında tenant enrollment anahtarını gönderir, karşılığında uzun ömürlü
  cihaz JWT'si alır. Sonraki tüm cihaz çağrıları `Authorization: Bearer <token>`
  ile yapılır.
- Her tablo `TenantId` taşır; sorgular JWT claim'inden çözülen tenant ile mantıksal
  olarak izole edilir.

### API uçları

| Metot | Uç | Yetki | Açıklama |
|-------|-----|-------|----------|
| POST | `/api/auth/login` | Anonim | Admin girişi |
| POST | `/api/device/register` | Enrollment başlığı | Cihaz kaydı → cihaz JWT |
| POST | `/api/device/heartbeat` | Cihaz | Canlılık sinyali, LastSeen güncelle |
| GET  | `/api/device/list` | Admin | Tenant cihaz listesi |
| GET  | `/api/policy/{deviceId}` | Cihaz/Admin | Aktif politikayı getir |
| POST | `/api/policy/update` | Admin | Politika oluştur/güncelle (versiyon otomatik artar) |
| POST | `/api/command/create` | Admin | Komut kuyruğa ekle (LOCK·MESSAGE·RESTART·INSTALL_APK·REQUEST_LOCATION·SCREENSHOT) |
| GET  | `/api/command/pending/{deviceId}` | Cihaz/Admin | Bekleyen komutlar |
| POST | `/api/command/ack` | Cihaz | Komut durum güncelle (SENT·DONE) |
| POST | `/api/device/location` | Cihaz | Konum bildir (lat/lng/accuracy) |
| GET  | `/api/device/{deviceId}/locations` | Admin | Konum geçmişi |
| POST | `/api/device/apps` | Cihaz | Yüklü uygulama envanterini bildir |
| GET  | `/api/device/{deviceId}/apps` | Admin | Cihazın yüklü uygulamaları |
| POST | `/api/device/screenshot` | Cihaz | Ekran görüntüsü yükle (multipart) |
| POST | `/api/app/upload` | Admin | APK yükle (multipart) |
| GET  | `/api/app/list` | Admin | Yüklü APK kataloğu |
| GET  | `/api/app/download/{id}` | Admin/Cihaz | APK indir (cihaz kurulum için) |

`backend/FsonMdm.http` dosyası tüm akışı uçtan uca test eden hazır REST
istekleri içerir (login → register → heartbeat → policy update → policy fetch →
device list → LOCK komutu → poll → ack).

### Politika sözleşmesi (JSON)

```json
{
  "kioskMode": true,
  "blockCamera": true,
  "blockSettings": true,
  "blockPlayStore": true,
  "allowedApps": ["com.android.chrome"]
}
```

---

## 2. Yönetim Paneli (Dashboard)

Cihazları görüntülemek ve yönetmek için sunucu tarafında render edilen bir
**ASP.NET Core MVC paneli**, aynı `FsonMdm.Api` projesi içinde yer alır. API
JWT ile, panel ise **cookie tabanlı** (`Dashboard` şeması) kimlik doğrulaması
kullanır; ikisi de aynı `ITenantContext` üzerinden tenant'ı çözer, böylece
Application servisleri değişmeden çalışır.

### Erişim

1. Backend'i çalıştırın.
2. Tarayıcıdan kök adrese gidin: `http://localhost:5080/` → otomatik olarak
   `/dashboard` adresine yönlendirir.
3. Demo admin bilgileriyle giriş yapın: `admin` / `Admin123!`.

### Sayfalar

| Sayfa | Yol | İşlev |
|-------|-----|-------|
| Cihazlar | `/dashboard` | Tüm cihazlar, durum, son görülme, son konum |
| Cihaz Detay | `/dashboard/device/{id}` | Bilgi kartı, uzaktan komutlar, konum haritası (Leaflet/OSM), son ekran görüntüsü, yüklü uygulamalar, APK kurulum |
| Politika & Kiosk | `/dashboard/policy` | Kısıtlama anahtarları + cihazların bildirdiği uygulamalardan kiosk beyaz listesi seçimi + manuel paket ekleme |
| Uygulamalar (APK) | `/dashboard/apps` | APK yükleme, katalog listesi, seçilen cihaza kurulum komutu gönderme |

> Harita için Leaflet ve OpenStreetMap karoları CDN üzerinden çağrılır; panelin
> bu özelliğinin çalışması için tarayıcının internet erişimi gerekir.

---

## 3. Android Agent (DPC)

### Mimari

```
android/FsonMdmAgent/app/src/main/kotlin/com/fson/mdm/
├─ core/         Constants, Prefs (token/deviceId/policy önbelleği)
├─ data/         MdmRepository + remote/ (Retrofit ApiService, ApiClient, AuthInterceptor, DTO'lar)
├─ device/       PolicyEnforcer, KioskManager, CommandExecutor, MdmDeviceAdminReceiver,
│                LocationProvider, AppInventory, ApkInstaller, MediaProjectionHolder
├─ service/      HeartbeatService (foreground), MdmSyncEngine, PolicyWorker, BootReceiver,
│                ScreenCaptureService (MediaProjection ile tek kare yakalama)
├─ permission/   PermissionManager (sistem ayar yönlendirmeleri + konum izni)
└─ ui/           MainActivity (kontrol paneli), KioskActivity (kilitli mod)
```

### Açma ve çalıştırma

1. **Android Studio** ile `android/FsonMdmAgent` klasörünü açın (Gradle senkronu
   bağımlılıkları indirir).
2. Cihazın/emülatörün **Device Owner** olarak ayarlı olduğundan emin olun
   (kapsam gereği bu adım haricî olarak yapılır). Geliştirme için tipik komut:
   ```
   adb shell dpm set-device-owner com.fson.mdm/.device.MdmDeviceAdminReceiver
   ```
   (Cihazda hesap tanımlı olmamalıdır.)
3. Uygulamayı çalıştırın.

### Sunucu adresi

- Emülatör için varsayılan: `http://10.0.2.2:5080/` (host makinenin loopback'i).
- Fiziksel cihaz için, uygulama açılış ekranındaki **Sunucu adresi** alanına
  PC'nizin LAN IP'sini yazın (ör. `http://192.168.1.20:5080/`).
- HTTP trafiği `network_security_config.xml` ile yalnızca yerel adreslere açıktır.

### Çalışma akışı

1. Uygulama açılır → kontrol panelinde Device Owner durumu ve izinler görünür.
2. **İzinler** bölümünden eksik izinler tek tek verilir (bildirim, pil muafiyeti,
   kullanım erişimi, üzerine çizim).
3. **Cihazı Kaydet** → `register` çağrısı, status **ACTIVE** olur, cihaz JWT'si
   saklanır ve politika hemen çekilip uygulanır.
4. **HeartbeatService** (foreground) açılır; her ~45 sn'de bir döngü çalışır:
   heartbeat → politika çek & uygula → bekleyen komutları çalıştır & ACK'le.
   `PolicyWorker` (WorkManager) servis öldürülürse 15 dk'da bir yeniden ayağa
   kaldırır; `BootReceiver` yeniden başlatmada devreye girer.
5. **Kiosk Modu**: politika `kioskMode=true` ise veya panelden başlatılınca
   uygulama HOME launcher olur, lock-task (tek uygulama) devreye girer ve yalnızca
   `allowedApps` listesindeki uygulamalar açılabilir.

### Politika uygulama eşlemesi

| Politika alanı | DevicePolicyManager karşılığı |
|----------------|-------------------------------|
| `blockCamera` | `setCameraDisabled` |
| `blockPlayStore` | `setApplicationHidden(com.android.vending)` |
| `blockSettings` | `setApplicationHidden(com.android.settings)` |
| `allowedApps` | `setLockTaskPackages` (+ agent) |
| `kioskMode` | `startLockTask` + persistent HOME |

### Komut çalıştırma

| Komut | Davranış |
|-------|----------|
| `LOCK` | `lockNow()` ile ekranı anında kilitler |
| `MESSAGE` | Yüksek öncelikli bildirim olarak yönetici mesajını gösterir |
| `RESTART` | `reboot()` ile cihazı yeniden başlatır (Device Owner) |
| `INSTALL_APK` | Payload'daki APK id'siyle `/api/app/download/{id}`'den indirir, `PackageInstaller` ile sessizce kurar (Device Owner) |
| `REQUEST_LOCATION` | `LocationManager` ile tek konum alır ve `/api/device/location`'a bildirir |
| `SCREENSHOT` | Operatör izni verildiyse `MediaProjection` ile tek kare yakalayıp yükler |

Her komut önce **SENT**, başarıyla çalıştırılınca **DONE** olarak ACK'lenir.

### Yeni özellikler için notlar

- **Konum takibi:** Uygulama açılışındaki **Konum erişimi** iznini verin. Tek
  seferlik `REQUEST_LOCATION` komutu uygulama ön planda/yakın zamanda
  kullanılmışken güvenilir çalışır; senkron döngüsü ayrıca en iyi çabayla
  periyodik konum bildirir.
- **Uygulama envanteri:** Senkron döngüsünde en fazla 6 saatte bir yüklü
  uygulamalar bildirilir; bunlar panelde kiosk beyaz listesi seçiminde listelenir.
- **Uzaktan görüntüleme (ekran görüntüsü):** Bu MVP'de "remote control",
  **tek kare ekran görüntüsü** temeli olarak gerçeklenmiştir. Sürekli canlı
  yayın + uzaktan dokunma enjeksiyonu kapsam dışıdır. Ekran yakalama için
  operatörün **MainActivity'deki "Uzaktan görüntüleme" iznini** vermesi gerekir;
  MediaProjection token'ı kalıcı olmadığından bu izin **uygulama her açıldığında
  bir kez** verilmelidir.
- **APK kurulumu:** Sessiz kurulum cihazın **Device Owner** olmasını gerektirir.

---

## 4. MVP Test Senaryosu

1. Backend'i çalıştırın (`dotnet run`). (Varsa eski `fsonmdm.db`'yi silin.)
2. Tarayıcıdan `http://localhost:5080/` → panele `admin / Admin123!` ile girin.
3. Android uygulamasını emülatörde açın, izinleri verin (konum dâhil), **Cihazı
   Kaydet**. → Panelde **Cihazlar** sayfasında cihaz **Active** görünür.
4. **Politika & Kiosk** sayfasında `kioskMode` ve `blockCamera` işaretleyip
   kaydedin → cihaz bir sonraki döngüde uygular.
5. **Cihaz Detay** sayfasından:
   - **Konum İste** → kısa süre sonra harita konum noktasını gösterir.
   - **Ekran Görüntüsü Al** → (uygulamada uzaktan görüntüleme izni verildiyse)
     son ekran görüntüsü kartında görünür.
   - **Kilitle / Yeniden Başlat / Mesaj** komutlarını test edin.
6. **Uygulamalar** sayfasından bir APK yükleyip ilgili cihaza **Kur** komutu
   gönderin → cihaz indirir ve sessizce kurar (Device Owner).

---

## 5. Notlar

- Backend yeni NuGet paketi gerektirmez; yalnızca standart ASP.NET Core / EF Core
  paketleri kullanılır. Şifreler `PBKDF2 (SHA256, 100k iter)` ile saklanır.
- Tüm kullanıcı arayüzü metinleri Türkçe; kod ve veritabanı tanımlayıcıları İngilizce.
- Üretim dağıtımında HTTPS kullanın ve cleartext istisnalarını kaldırın.
