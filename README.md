# Personel İzin ve Onay Yönetim Sistemi

Personel İzin ve Onay Yönetim Sistemi; çalışanların izin taleplerini yönetmek, izin bakiyelerini takip etmek, yönetici onay süreçlerini kontrol etmek ve insan kaynakları raporlamasını desteklemek için geliştirilen bir backend API projesidir.

Proje; ASP.NET Core Web API, Entity Framework Core, PostgreSQL, Clean Architecture, CQRS/MediatR, FluentValidation, JWT Bearer Authentication, Docker/Docker Compose, structured logging ve EF Core tabanlı audit logging kullanılarak katmanlı bir yapı ile geliştirilmiştir.

Bu repository, staj projesi kapsamında dört faz halinde geliştirilmiştir. Faz 1, Faz 2, Faz 3 ve Faz 4 tamamlanmıştır. Final durumda sistem; temel CRUD ve domain modelinden başlayarak izin bakiyesi ve onay kurallarını, CQRS ve JWT tabanlı güvenliği, rol/kaynak bazlı authorization'i, raporlamayı, containerized çalıştırma ortamlarını, development demo kullanıcılarını, notification logging'i ve audit trail'i uçtan uca kapsamaktadır.

---

## Proje Durumu

|Faz|Kapsam|Durum|
|-|-|-|
|Faz 1|Temel mimari, entity yapısı, EF Core, migration, seed data ve temel CRUD endpointleri|Tamamlandı|
|Faz 2|İş kuralları, validasyon, izin bakiyesi, onay akışı ve yönetici yetki kuralları|Tamamlandı|
|Faz 3|CQRS/MediatR, JWT authentication, claim ve role authorization, calendar query ve departman raporlama|Tamamlandı|
|Faz 4|Dockerized API + PostgreSQL, health/migration startup, demo users, notification logging, audit logging ve final teslim hazırlığı|Tamamlandı|

---

## Faz 1 Kapsamında Tamamlananlar

* Domain, Application, Infrastructure ve WebAPI katmanları oluşturuldu.
* `Employee`, `Department`, `LeaveType` ve `LeaveRequest` entity'leri tanımlandı.
* `Employee` entity'sinde `ManagerId` ile self-referencing yönetici ilişkisi kuruldu.
* Entity Framework Core `AppDbContext` yapısı oluşturuldu.
* PostgreSQL bağlantısı yapılandırıldı.
* İlk migration oluşturuldu ve başarıyla uygulandı.
* Varsayılan `LeaveType` kayıtları migration ile seed data olarak eklendi.
* Local geliştirme ortamı için Docker Compose ile PostgreSQL kurulumu eklendi.
* Employee için temel CRUD endpointleri yazıldı.
* LeaveRequest için temel CRUD endpointleri yazıldı.
* Swagger UI ile API endpointleri test edilebilir hale getirildi.
* Temel integration test altyapısı oluşturuldu.
* Employee ve LeaveRequest CRUD akışları gerçek PostgreSQL üzerinde test edildi.
* Faz 1 endpointleri Swagger UI üzerinden manuel olarak doğrulandı.

---

## Faz 2 Kapsamında Tamamlananlar

Faz 2 kapsamında projeye izin yönetimi için gerçek iş kuralları, bakiye hesabı ve onay akışı eklenmiştir.

Tamamlanan ana başlıklar:

* İzin bakiyesi hesaplama mantığı eklendi.
* Yıllık hak ediş `EntitledDays`, `UsedDays` ve `RemainingDays` üzerinden temsil edildi.
* İzin bakiyesi yıl bazında hesaplanır hale getirildi.
* Cross-year izin talepleri ilgili yıllara bölünerek hesaplanır hale getirildi.
* Yıllık hakkı sıfırdan büyük izin türlerinde create, update ve approve akışları için kalan izin bakiyesinin aşılması engellendi.
* Aynı çalışan için çakışan tarih aralığına sahip geçerli izin talepleri engellendi.
* Rejected izin taleplerinin yeni talepleri engellememesi sağlandı.
* İzin talebi yaşam döngüsü uygulandı: `Pending -> Approved / Rejected`.
* Yalnız talebi açan çalışanın doğrudan yöneticisinin approve/reject işlemi yapabilmesi sağlandı.
* Employee, Manager ve HR rolleri için iş kuralları tanımlandı.
* Employee, HR veya direct manager olmayan bir manager'in review yapması engellendi.
* Yetki hataları için `403 Forbidden`, iş kuralı hataları için `400 Bad Request` dönülmesi sağlandı.
* Review edilmiş izin taleplerinin tekrar approve/reject edilmesi engellendi.
* Yalnız `Pending` taleplerin update ve delete edilebilmesi sağlandı.
* Var olmayan izin talebi için review isteğinde `404 Not Found` davranışı korundu.
* Zero allowance leave type davranışı test edildi.
* Faz 2 integration test kapsamı genişletildi; aynı davranışlar Faz 3 CQRS geçişinde handler unit testleriyle de sabitlendi.

---

## Faz 3 Kapsamında Tamamlananlar

Faz 3 kapsamında servis tabanlı use-case'ler CQRS/MediatR yapısına taşınmış, JWT tabanlı kimlik doğrulama eklenmiş ve production endpointleri rol ile kaynak kapsamına göre korunmuştur.

Tamamlanan ana başlıklar:

* MediatR ve FluentValidation Application katmanına eklendi.
* Ortak `ValidationBehavior` pipeline'i oluşturuldu.
* Employee ve LeaveRequest use-case'leri Command/Query handler yapısına taşındı.
* Controller'lar business logic çalıştırmak yerine `ISender` kullanır hale getirildi.
* Legacy Employee ve LeaveRequest service yolları kontrollü geçiş sonrasında temizlendi.
* Application, Infrastructure ve WebAPI unit test projeleri eklendi.
* `UserAccount` domain modeli ve Employee ile one-to-one ilişkisi eklendi.
* Password hashing için ASP.NET Core Identity `PasswordHasher` altyapısı kullanıldı.
* `POST /api/auth/login` CQRS login akışı eklendi.
* JWT issuer, audience, signing key, expiration ve required-claim doğrulamaları eklendi.
* Swagger UI için Bearer authentication tanımı eklendi.
* Token claim'lerinin güncel veritabanı state'i ile tekrar doğrulanması sağlandı.
* Pasif UserAccount, pasif Employee ve token/DB role uyumsuzluğu engellendi.
* Employee, Manager ve HR için named authorization policy'leri eklendi.
* Default ve fallback authorization kullanılarak production endpointleri varsayılan olarak korundu.
* Login ve health endpointleri açıkça `[AllowAnonymous]` olarak tanımlandı.
* Employee administration endpointleri yalnız HR erişimine açıldı.
* Employee ve Manager için kendi izinlerine yönelik self-service kapsamı, Manager için active direct report okuma/review kapsamı ve HR için geniş okuma/raporlama kapsamı uygulandı.
* Scope dışı tekil kaynaklarda veri varlığını sızdırmamak için `404 Not Found` yaklaşımı uygulandı.
* Approve/reject reviewer identity'si request body yerine authenticated current user'dan türetilir hale getirildi.
* HR'in approve/reject yapması engellendi.
* Calendar/date-range sorgusu eklendi.
* Calendar sorgusunda inclusive overlap ve rol bazlı görünürlük uygulandı.
* HR-only departman izin istatistikleri raporu eklendi.
* Departman raporunda PostgreSQL tarafında `GroupBy`, `Count`, `Sum`, `Average` ve deterministik sıralama uygulandı.
* Authentication, authorization, Swagger metadata, calendar ve reporting davranışları unit ve integration testlerle doğrulandı.
* Son feature-branch full-suite sonucu `524/524` başarılı olarak doğrulandı.

---

## Faz 4 Kapsamında Tamamlananlar

Faz 4 kapsamında sistemin test güvencesi, container çalıştırma modeli ve operasyonel gözlemlenebilirliği tamamlanmış; local/demo ortamında tekrar edilebilir bir kurulum akışı oluşturulmuştur.

Tamamlanan ana başlıklar:

* Web API için .NET 10 SDK -> publish -> ASP.NET Core runtime adımlarını kullanan multi-stage `Dockerfile` eklendi.
* Docker build context'inden `.git`, `.vs`, `bin`, `obj`, `.env`, `TestResults` ve editor artefact'larını dışlayan `.dockerignore` eklendi.
* Docker Compose ile `postgres:17-alpine` ve Web API birlikte çalışır hale getirildi.
* PostgreSQL container'i için `pg_isready`, API container'i için `/api/health` tabanlı healthcheck eklendi.
* `/api/health` yalnız API process'ini değil, EF Core üzerinden PostgreSQL bağlantısını da doğrular hale getirildi.
* Docker ortamında API `8080` portunda çalışır ve host tarafında `5252` portuna publish edilir.
* Startup migration davranışı `DatabaseInitialization:ApplyMigrationsOnStartup` ile kontrollü hale getirildi; varsayılan uygulama ayarı `false`, Docker demo ayarı `true` olarak tutuldu.
* Startup migration için sınırlı retry uygulandı; retry global EF Core execution strategy'ye taşınmadı ve normal application transaction davranışı korunmuş oldu.
* Local secret/configuration için `.env` Git dışında tutuldu; repository'ye yalnız placeholder değerler içeren `.env.example` eklendi.
* `DemoData` configuration modeli ve conditional validation eklendi. Seeding açıksa password zorunlu, kapalıysa password zorunlu değildir.
* Development ortamında Employee, Manager ve HR rollerini kapsayan configurable demo data seeding eklendi.
* Demo seeding transaction içinde çalışır, tekrar çalıştırıldığında duplicate üretmez ve mevcut password hash'lerini gereksiz yere yenilemez.
* Demo Employee, güncel demo Manager'a direct report olarak bağlanır; beklenmeyen role, department, manager veya password uyumsuzlukları sessizce düzeltilmek yerine açık hata ile durdurulur.
* Approve ve Reject akışları için `ILeaveRequestNotificationService` abstraction'i ve structured `ILogger` tabanlı notification simulation eklendi.
* Notification log'u yalnız başarılı `SaveChangesAsync` sonrasında üretilir; business-rule veya persistence failure durumunda notification üretilmez.
* Notification log'unda `NotificationType`, `LeaveRequestId`, `EmployeeId`, `ReviewerEmployeeId`, `Status` ve entity üzerinde oluşan gerçek `ReviewedAtUtc` tutulur; manager comment veya email gibi içerikler loglanmaz.
* `AuditLog` tablosu ve `AuditAction` modeli eklendi.
* EF Core `SaveChangesInterceptor` ile `Employee` ve `LeaveRequest` mutation'ları aynı persistence akışı içinde audit edilir.
* Audit action'ları `Created`, `Updated`, `Deleted`, `Approved` ve `Rejected` olarak ayrılır; `Pending -> Approved/Rejected` geçişleri EF change tracking üzerinden tespit edilir.
* Audit kaydında actor olarak authenticated `EmployeeId`, system/seed işlemlerinde `null`, olay zamanı olarak `OccurredAtUtc` saklanır.
* `ChangedPropertiesJson` içinde yalnız property isimleri tutulur; eski/yeni property değerleri, reason, manager comment, email, password hash veya secret değerleri audit'e yazılmaz.
* Audit kapsamı bilerek `Employee` ve `LeaveRequest` ile sınırlıdır; `Department`, `LeaveType`, `UserAccount` ve `AuditLog` kendisi audit edilmez.
* `AddAuditLogs` migration'i ile `AuditLogs` tablosu ve ilgili index'ler PostgreSQL'e eklendi; `ActorEmployeeId` tarihsel scalar kimlik olarak tutulur ve FK/navigation tanımlanmaz.
* Demo configuration ve seeding için unit/integration testler; notification success/failure davranışı için handler testleri; audit davranışı için gerçek PostgreSQL integration testi eklendi.
* Son kesin doğrulanan full-suite sonucu `529/529` başarılıdır.

---

## Mimari Yapı

Proje Clean Architecture prensiplerine uygun katmanlı bir yapı kullanmaktadır.

```text
src/
  LeaveManagementSystem.Domain
  LeaveManagementSystem.Application
  LeaveManagementSystem.Infrastructure
  LeaveManagementSystem.WebAPI

tests/
  LeaveManagementSystem.Application.UnitTests
  LeaveManagementSystem.Infrastructure.UnitTests
  LeaveManagementSystem.IntegrationTests
  LeaveManagementSystem.WebAPI.UnitTests
```

### Katmanların Sorumlulukları

|Katman|Sorumluluk|
|-|-|
|Domain|Entity'ler, enum'lar ve çekirdek domain davranışları; audit modeli ve action enum'u|
|Application|Command/Query, handler, validator, DTO, repository/notification abstraction'ları ve application-level exception'lar|
|Infrastructure|EF Core, PostgreSQL, DbContext, repository implementasyonları, password hashing, JWT token generation, demo seeding, structured notification logging ve audit interceptor|
|WebAPI|Controller'lar, authentication/authorization pipeline'i, HTTP endpointleri, exception mapping, Swagger, health checks ve startup orchestration|
|Application.UnitTests|Handler, validator, rule sırası, authorization davranışı ve notification fake'leriyle hızlı unit testler|
|Infrastructure.UnitTests|Password hashing, JWT options validation, JWT token generation ve demo configuration validation unit testleri|
|WebAPI.UnitTests|Authorization handler, JWT/current-user ve HTTP result/exception handler unit testleri|
|IntegrationTests|Gerçek HTTP pipeline'i, OpenAPI metadata'sı ve gerçek PostgreSQL üzerinden CRUD, auth, business rules, demo seeding, reporting ve audit testleri|

Güncel CQRS request akışı:

```text
Controller
  -> ISender
    -> Command / Query
      -> Handler
        -> Application abstraction
          -> Infrastructure implementation
            -> AppDbContext
              -> PostgreSQL
```

Authentication ve authorization akışı:

```text
Bearer token
  -> Signature / issuer / audience / lifetime validation
    -> Required claim validation
      -> UserAccount ve Employee DB kontrolü
        -> Güncel role ve active-state kontrolü
          -> Named policy / resource scope
            -> Application use-case authorization kontrolü
```

Docker startup akışı:

```text
Docker Compose
  -> PostgreSQL healthcheck
    -> Web API start
      -> controlled migration + limited retry
        -> optional Development demo seed
          -> API + PostgreSQL healthcheck
```

Review ve observability akışı:

```text
Approve / Reject
  -> domain state change
    -> SaveChangesAsync
      -> AuditSaveChangesInterceptor -> AuditLogs
        -> successful persistence
          -> structured notification log
            -> reloaded response DTO
```

Bu yapı sayesinde WebAPI katmanı doğrudan `AppDbContext` ile business logic çalıştırmaz. Application katmanı EF Core, Npgsql, `HttpContext` veya concrete logging implementasyonlarını bilmez; persistence, JWT, demo seeding, audit ve notification implementasyonları Infrastructure katmanında tutulur.

---

## Kullanılan Teknolojiler

* C#
* .NET 10
* ASP.NET Core Web API
* Entity Framework Core 10
* PostgreSQL 17
* Npgsql
* Docker
* Docker Compose
* ASP.NET Core Health Checks
* MediatR
* FluentValidation
* ASP.NET Core Identity PasswordHasher
* JWT Bearer Authentication
* Swagger / Swashbuckle
* Structured logging (`ILogger`)
* xUnit
* WebApplicationFactory
* Git / GitHub
* GitHub Desktop

---



## Domain Model Özeti



### Employee

Çalışan bilgisini temsil eder.

Öne çıkan alanlar:

* `FirstName`
* `LastName`
* `Email`
* `DepartmentId`
* `ManagerId`
* `Role`
* `IsActive`

`ManagerId` alanı ile aynı tablo üzerinde self-referencing ilişki kurulmuştur. Böylece bir çalışan başka bir çalışanın yöneticisi olabilir.

```text
Employee
  -> Manager
  -> DirectReports
```

Employee delete işlemi soft delete olarak uygulanır. Çalışan veritabanından fiziksel olarak silinmez; `IsActive` değeri `false` yapılır.



### Department

Çalışanın bağlı olduğu departmanı temsil eder.

Departman izin istatistikleri, izin talebinin oluşturulduğu tarihteki tarihsel snapshot'a göre değil, sorgu anında çalışanın bağlı olduğu güncel departmana göre gruplanır.



### LeaveType

İzin türünü temsil eder. Varsayılan izin türleri migration ile veritabanına eklenir:

```text
Annual Leave
Sick Leave
Unpaid Leave
```

Öne çıkan alanlar:

* `Name`
* `DefaultAnnualAllowanceDays`
* `IsPaid`

`DefaultAnnualAllowanceDays`, ilgili izin türü için varsayılan yıllık hak ediş gün sayısını temsil eder.

Zero allowance için güncel davranış:

```text
DefaultAnnualAllowanceDays = 0
-> approved usage sorgusu yapılır
-> insufficient-balance restriction uygulanmaz
-> diğer review kuralları uygunsa approval devam edebilir
```

### LeaveRequest

Bir çalışanın izin talebini temsil eder.

Öne çıkan alanlar:

* `EmployeeId`
* `LeaveTypeId`
* `StartDate`
* `EndDate`
* `RequestedDays`
* `Status`
* `Reason`
* `ManagerComment`
* `ReviewedAtUtc`
* `ReviewedByEmployeeId`

Yeni oluşturulan izin talepleri varsayılan olarak `Pending` durumunda başlar. `RequestedDays` değeri inclusive tarih aralığına göre otomatik hesaplanır.

İzin talebi status akışı:

```text
Pending -> Approved
Pending -> Rejected
```

Approved veya Rejected durumuna geçmiş talepler tekrar review edilemez. Yalnız `Pending` talepler update veya delete edilebilir.



### UserAccount

JWT login yapabilen kullanıcı hesabını temsil eder.

Öne çıkan alanlar:

* `EmployeeId`
* `PasswordHash`
* `IsActive`
* `CreatedAtUtc`
* `UpdatedAtUtc`

Kurallar:

* Her UserAccount bir Employee'ye bağlıdır.
* Employee başına en fazla bir UserAccount bulunur.
* Plain-text password veritabanında saklanmaz.
* UserAccount ve bağlı Employee aktif değilse login veya protected endpoint erişimi verilmez.
* Token role claim'i, güncel Employee role değeri ile uyuşmalıdır.

---

### AuditLog

`Employee` ve `LeaveRequest` mutation'ları için tarihsel audit kaydını temsil eder.

Öne çıkan alanlar:

* `Id`
* `EntityName`
* `EntityId`
* `Action`
* `ChangedPropertiesJson`
* `ActorEmployeeId`
* `OccurredAtUtc`

`AuditAction` değerleri:

```text
Created
Updated
Deleted
Approved
Rejected
```

Kurallar:

* `AuditLog`, `BaseEntity` kullanmaz; olay zamanı semantik olarak `OccurredAtUtc` alanında tutulur.
* `ActorEmployeeId` nullable scalar kimliktir; Employee ile FK/navigation kurulmaz.
* Authenticated HTTP request'lerinde actor current Employee'dir; seed/system context'lerinde actor `null` olabilir.
* `ChangedPropertiesJson` yalnız değişen property isimlerini JSON array olarak tutar; property değerlerini tutmaz.
* Modified kayıtlarda teknik `UpdatedAtUtc` alanı changed-properties listesinden çıkartılır.
* Audit kapsamı yalnız `Employee` ve `LeaveRequest` mutation'larıdır.

---

## İş Kuralları Özeti

### İzin Bakiyesi

İzin bakiyesi yıl bazında hesaplanır.

```text
EntitledDays - UsedDays = RemainingDays
```

* `EntitledDays`: İlgili izin türü için yıllık hak edilen gün sayısı
* `UsedDays`: İlgili yıl içinde approved izin günleri
* `RemainingDays`: Kalan izin günü

Yalnız `Approved` durumdaki izin talepleri kullanılmış izin olarak sayılır.

Approve sırasında incelenen mevcut talep, approved usage hesabında tekrar sayılmaz.

### Cross-Year İzinler

Bir izin talebi birden fazla yıla yayılıyorsa günler ilgili yıllara bölünür.

Örnek:

```text
2026-12-30 -> 2027-01-02
```

Bu talep toplam 4 gün sürer:

```text
2026: 2 gün
2027: 2 gün
```

Balance kontrolü her yıl için ayrı yapılır.

### Overlap Kontrolü

Aynı çalışan için çakışan tarih aralığına sahip geçerli izin talepleri engellenir.

Rejected durumdaki izin talepleri yeni izin taleplerini engellemez. Böylece reddedilmiş bir talep sonrasında aynı tarih aralığı için yeni talep oluşturulabilir.

Calendar sorgusunda overlap inclusive olarak hesaplanır:

```text
LeaveRequest.StartDate <= queryEnd
&& queryStart <= LeaveRequest.EndDate
```

### Onay Kuralı

Bir izin talebi yalnız authenticated current user olan ve talebi açan çalışanın güncel doğrudan yöneticisi konumundaki Manager tarafından approve veya reject edilebilir.

Reviewer identity istemciden alınmaz:

```text
JWT current user
-> current EmployeeId
-> Approve / Reject command
-> current direct-manager kontrolü
```

Aşağıdaki kullanıcılar approve/reject yapamaz:

* Talebi açan Employee
* HR kullanıcısı
* Başka bir Manager
* Talebi açan çalışanın güncel direct manager'i olmayan Manager
* Pasif UserAccount veya pasif Employee
* Token role claim'i güncel DB role ile uyuşmayan kullanıcı



### Authorization Kapsamı

|Rol|Temel erişim kapsamı|
|-|-|
|Employee|Kendi izin kayıtları ve self-service işlemleri|
|Manager|Kendi self-service işlemleri; güncel ve aktif direct report kayıtlarını okuma ve review|
|HR|Kendi self-service işlemleri; Employee administration, geniş leave read kapsamı ve raporlama|



Ek kurallar:

* Employee administration endpointleri HR-only'dir.
* Manager'in genel collection ve calendar scope'una kendi izin kayıtları otomatik olarak dahil edilmez; bu akışlarda yalnız güncel ve aktif direct report kayıtları döner.
* Manager kendi izin kayıtlarına `/api/leave-requests/mine`, kendi kaydı için by-id ve self-service endpointleri üzerinden erişebilir.
* Pasif direct report, Manager collection ve calendar scope'una dahil edilmez.
* HR geniş okuma ve raporlama yapabilir ancak approve/reject yapamaz.
* Scope dışı tekil kaynaklar, kaydın varlığını sızdırmamak için `404 Not Found` dönebilir.
* Token claim'leri tek başına authoritative değildir; güncel DB state'i kontrol edilir.



### Employee Administration Kuralları

* Employee administration yalnız güncel ve aktif HR kullanıcıları tarafından yapılabilir.
* Manager olarak atanacak Employee mevcut, aktif ve `Manager` rolunde olmalıdır.
* Bir Employee kendisinin manager'i olarak atanamaz.
* Manager hiyerarşisinde cycle oluşturulamaz.
* Aktif direct report'u bulunan bir Manager pasif hale getirilemez veya başka bir role geçirilemez.
* Aktif direct report'u bulunan bir Employee soft delete ile pasif hale getirilemez.
* Aktif UserAccount'a bağlı son aktif HR administrator pasif hale getirilemez veya HR dışında bir role geçirilemez.



### Lifecycle Kuralları

* Yeni izin talebi `Pending` olarak oluşturulur.
* Yalnız `Pending` talepler update edilebilir.
* Yalnız `Pending` talepler delete edilebilir.
* Yalnız `Pending` talepler approve/reject edilebilir.
* Approved veya Rejected talepler tekrar review edilemez.
* LeaveRequest delete mevcut kontratta `Pending` kayıt için fiziksel silmedir.
* Employee delete soft delete olarak uygulanır.



### Departman İzin İstatistikleri

Departman raporu yalnız `Approved` izin taleplerini hesaplar.

Her departman için:

```text
ApprovedRequestCount
TotalApprovedLeaveDays
AverageApprovedLeaveDaysPerRequest
```

üretilir.



Kurallar:

* Pending ve Rejected talepler hesaba katılmaz.
* Gruplama, çalışanın sorgu anındaki güncel departmanına göre yapılır.
* Onaylı talebi olmayan departmanlar response'ta listelenmez.
* Sonuçlar `DepartmentName`, ardından `DepartmentId` ile deterministik sıralanır.
* Filtreleme, grouping, `Count`, `Sum`, `Average` ve sıralama PostgreSQL tarafında çalışır.
* PostgreSQL'den yalnız departman bazlı aggregate sonuç satırları alınır; ham `LeaveRequest` kayıtları bellekte gruplanmaz.
* Veritabanı tarafında hesaplanan average sonucu, materialization sonrasında DTO'nun `decimal` alanına dönüştürülür.
* Provider-specific sorgu davranışı gerçek PostgreSQL integration testiyle doğrulanmıştır.

---



## Gereksinimler

Projeyi çalıştırmak için aşağıdaki araçların kurulu olması gerekir:

* .NET 10 SDK
* Docker Desktop ve Docker Compose
* Git
* DBeaver, pgAdmin veya benzeri bir veritabanı aracı (opsiyonel)

Entity Framework CLI yüklü değilse aşağıdaki komut ile kurulabilir:



```bash
dotnet tool install --global dotnet-ef
```

### JWT Configuration

JWT için aşağıdaki configuration alanları kullanılır:



```text
Jwt:Issuer
Jwt:Audience
Jwt:SigningKey
Jwt:AccessTokenExpirationMinutes
```

`Jwt:SigningKey` en az 32 UTF-8 byte olmalıdır. `Jwt:AccessTokenExpirationMinutes` değeri 1 ile 1440 arasında olmalıdır.

Gerçek production signing key repository'ye veya README'ye yazılmamalıdır. Environment variable, user secrets veya güvenli secret/configuration mekanizması kullanılmalıdır.

Environment variable adları ASP.NET Core configuration formatında şu şekildedir:

```text
Jwt__Issuer
Jwt__Audience
Jwt__SigningKey
Jwt__AccessTokenExpirationMinutes
```

### Database Initialization

Startup migration davranışı şu configuration ile kontrol edilir:



```text
DatabaseInitialization:ApplyMigrationsOnStartup
```

Varsayılan `appsettings.json` değeri `false`'tur. Docker Compose demo ortamında bu değer:

```text
DatabaseInitialization__ApplyMigrationsOnStartup=true
```

olarak override edilir. Migration startup'ta sınırlı retry ile uygulanır; retry tükendiğinde uygulama başlatma hatasını gizlemez.



### Demo Data Configuration

Development demo kullanıcıları için:

```text
DemoData:SeedOnStartup
DemoData:Password
```

kullanılır.



Kurallar:

* `SeedOnStartup=false` ise password zorunlu değildir.
* `SeedOnStartup=true` ise `DemoData:Password` boş olamaz.
* Seeder yalnız Development ortamında startup akisine bağlıdır.
* Docker Compose değerleri `.env` dosyasından `DEMO_DATA_SEED_ON_STARTUP` ve `DEMO_DATA_PASSWORD` ile alır.
* Gerçek `.env` dosyası Git ve Docker build context'i dışında tutulur.

Repository'deki `.env.example` şu değişkenleri tanımlar:

```text
POSTGRES_DB
POSTGRES_USER
POSTGRES_PASSWORD
JWT_SIGNING_KEY
DEMO_DATA_SEED_ON_STARTUP
DEMO_DATA_PASSWORD
```

---



## Projeyi Çalıştırma

Aşağıdaki komutlar repository ana dizininde çalıştırılmalıdır.



### Seçenek A - Docker Compose ile Tüm Sistemi Çalıştırma

Final demo ve hızlı kurulum için önerilen yol budur.



#### 1. Repository'yi klonlama

```bash
git clone <repository-url>
cd leave-management-system
```

#### 2. Local `.env` dosyasını oluşturma

Repository'deki `.env.example` dosyasını `.env` olarak kopyalayın.

PowerShell:

```powershell
Copy-Item .env.example .env
```

Bash:

```bash
cp .env.example .env
```

Ardından `.env` içindeki placeholder değerleri local development için değiştirin. Örnek:

```text
POSTGRES_DB=leave_management_db
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
JWT_SIGNING_KEY=local-development-jwt-signing-key-at-least-32-bytes-2026
DEMO_DATA_SEED_ON_STARTUP=true
DEMO_DATA_PASSWORD=Demo123!
```

Bu değerler yalnız local development örneğidir. Production secret'ları bu şekilde repository'de veya README'de tutulmamalıdır. `.env` Git tarafından ignore edilir.



#### 3. API + PostgreSQL'i başlatma

```bash
docker compose up --build -d
```

Compose akışı:

```text
PostgreSQL container
  -> pg_isready healthcheck
  -> API container
  -> startup migration
  -> optional demo seed
  -> /api/health
```

Docker Compose içinde:

```text
PostgreSQL: localhost:5432
Web API:    http://localhost:5252
Swagger:    http://localhost:5252/swagger
Health:     http://localhost:5252/api/health
```

Container durumlarını kontrol etmek için:

```bash
docker compose ps
```

Sağlıklı durumda hem PostgreSQL hem API container'ının `healthy` olması beklenir.



#### 4. Demo kullanıcılar

`DEMO_DATA_SEED_ON_STARTUP=true` olduğunda Development ortamında aşağıdaki hesaplar idempotent olarak oluşturulur:

|Rol|Email|Password|
|-|-|-|
|Employee|`employee.demo@example.com`|`.env` içindeki `DEMO_DATA_PASSWORD`|
|Manager|`manager.demo@example.com`|`.env` içindeki `DEMO_DATA_PASSWORD`|
|HR|`hr.demo@example.com`|`.env` içindeki `DEMO_DATA_PASSWORD`|



Yukarıdaki `.env` örneği kullanıldıysa üç hesap için de local demo password'u `Demo123!` olur.

Demo Employee, Demo Manager'in direct report'udur. Seeder tekrar çalıştırıldığında doğru mevcut kayıtları duplicate etmez veya password hash'lerini gereksiz yere değiştirmez.



#### 5. Container loglarını izleme

API logları:

```bash
docker compose logs -f api
```

Notification simulation approve/reject sonrasında structured log olarak bu akışta görülebilir.



#### 6. Sistemi durdurma veya temiz reset

Container'ları durdurup volume'u korumak için:

```bash
docker compose down
```

Tamamen temiz veritabanı ile yeniden başlamak için:

```bash
docker compose down -v
docker compose up --build -d
```

`down -v` PostgreSQL volume'unu siler. Sonraki startup'ta migration ve Development demo seeding yeniden çalışır.



### Seçenek B - Docker Dışında Web API'yi Local Çalıştırma



API'yi `dotnet run` ile çalıştırmak isterseniz PostgreSQL'i ayrı olarak başlatabilirsiniz:

```bash
docker compose up -d postgres
```

`appsettings.Development.json` local PostgreSQL için `localhost:5432 / leave_management_db / postgres` development connection string'ini içerir.

Startup migration varsayılan olarak kapalı olduğu için migration'ları manuel uygulayın:

```bash
dotnet ef database update --project src/LeaveManagementSystem.Infrastructure --startup-project src/LeaveManagementSystem.WebAPI
```

JWT signing key'i repository dışında tanımlayın. Örneğin user secrets:

```bash
dotnet user-secrets set "Jwt:SigningKey" "local-development-jwt-signing-key-at-least-32-bytes-2026" --project src/LeaveManagementSystem.WebAPI
```

Local `dotnet run` akışı `.env` dosyasını otomatik olarak okumaz. Demo seeding'i Docker dışında da kullanmak isterseniz configuration'i environment variable veya user secrets ile açıkça verin:

```bash
dotnet user-secrets set "DemoData:SeedOnStartup" "true" --project src/LeaveManagementSystem.WebAPI
dotnet user-secrets set "DemoData:Password" "Demo123!" --project src/LeaveManagementSystem.WebAPI
```

Ardından:

```bash
dotnet run --project src/LeaveManagementSystem.WebAPI --launch-profile http
```

Swagger:

```text
http://localhost:5252/swagger
```

Health endpoint:

```text
http://localhost:5252/api/health
```

---

## Authentication ve Swagger Kullanımı

### Login

```http
POST /api/auth/login
```

Request body:

```json
{
  "email": "hr.demo@example.com",
  "password": "<DEMO_DATA_PASSWORD>"
}
```

Başarılı response alanları:

```json
{
  "accessToken": "<jwt-access-token>",
  "expiresAtUtc": "2026-08-06T18:00:00Z",
  "userAccountId": "11111111-1111-1111-1111-111111111111",
  "employeeId": "22222222-2222-2222-2222-222222222222",
  "email": "hr.demo@example.com",
  "role": 3
}
```

Role değerleri:

```text
1 = Employee
2 = Manager
3 = HR
```

Email bulunamaması, password'un yanlış olması, UserAccount'in pasif olması veya Employee'nin pasif olması durumlarında public response aynı genel `401 Unauthorized` kontratını kullanır.



### Swagger Authorize

1. `/api/auth/login` endpoint'i ile token alın.
2. Swagger UI içindeki `Authorize` butonunu açın.
3. Access token'i Bearer authentication alanına girin.
4. Protected endpointleri kullanıcı rolüne ve resource scope'una göre test edin.

HTTP header örneği:

```text
Authorization: Bearer <jwt-access-token>
```

---



## API Endpointleri

### Health ve Authentication

|Method|Endpoint|Erişim|Açıklama|
|-|-|-|-|
|GET|`/api/health`|Anonymous|API ve PostgreSQL bağlantısını doğrulayan health check|
|POST|`/api/auth/login`|Anonymous|Email ve password ile JWT access token üretir|

### Employees

|Method|Endpoint|Erişim|Açıklama|
|-|-|-|-|
|GET|`/api/employees`|HR|Çalışanları listeler|
|GET|`/api/employees/{id}`|HR|Id'ye göre çalışan getirir|
|POST|`/api/employees`|HR|Yeni çalışan oluşturur|
|PUT|`/api/employees/{id}`|HR|Çalışan bilgilerini günceller|
|DELETE|`/api/employees/{id}`|HR|Çalışanı soft delete ile pasif hale getirir|

### Leave Requests

|Method|Endpoint|Erişim|Açıklama|
|-|-|-|-|
|GET|`/api/leave-requests`|Authenticated, role scope|Employee/Manager/HR için yetkili koleksiyon kapsamını döndürür|
|GET|`/api/leave-requests/mine`|Authenticated|Current employee'nin kendi izin taleplerini döndürür|
|GET|`/api/leave-requests/calendar`|Authenticated, role scope|Tarih aralığıyla kesişen izin taleplerini takvim response'u olarak döndürür|
|GET|`/api/leave-requests/{id}`|Authenticated, resource scope|Id'ye göre yetkili izin talebini getirir|
|POST|`/api/leave-requests`|Authenticated, self-service scope|Yeni izin talebi oluşturur|
|PUT|`/api/leave-requests/{id}`|Authenticated, ownership scope|Pending izin talebini günceller|
|DELETE|`/api/leave-requests/{id}`|Authenticated, ownership scope|Pending izin talebini siler|
|GET|`/api/leave-requests/balance`|Authenticated, self-service scope|Current employee'nin belirli yıl ve izin türü için izin bakiyesini getirir|
|POST|`/api/leave-requests/{id}/approve`|Manager, direct-report scope|İzin talebini authenticated direct manager olarak onaylar|
|POST|`/api/leave-requests/{id}/reject`|Manager, direct-report scope|İzin talebini authenticated direct manager olarak reddeder|

### Reports

|Method|Endpoint|Erişim|Açıklama|
|-|-|-|-|
|GET|`/api/reports/department-leave-statistics`|HR|Approved izinleri güncel departmana göre gruplayan istatistik raporunu döndürür|



---

## Örnek Request ve Response'lar



### Employee Oluşturma



```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "departmentId": "11111111-1111-1111-1111-111111111111",
  "managerId": null,
  "role": 1
}
```

Employee administration endpointleri yalnız HR tarafından kullanılabilir.



### LeaveRequest Oluşturma



```json
{
  "leaveTypeId": "10000000-0000-0000-0000-000000000001",
  "startDate": "2026-07-15",
  "endDate": "2026-07-17",
  "reason": "Annual leave request."
}
```

`employeeId` request body'den alınmaz. İzin talebinin sahibi, doğrulanmış JWT üzerinden bulunan current Employee'dir. UserAccount ve bağlı Employee aktif olmalıdır. Varsayılan LeaveType kayıtları migration ile otomatik eklenir.



### LeaveRequest Approve

Reviewer identity request body'den alınmaz. Authenticated Manager'in current Employee ID'si kullanılır.



```json
{
  "managerComment": "Approved by direct manager."
}
```

### LeaveRequest Reject



```json
{
  "managerComment": "Rejected by direct manager."
}
```

### Leave Balance Sorgusu



```http
GET /api/leave-requests/balance?leaveTypeId=10000000-0000-0000-0000-000000000001&year=2026
```

`employeeId` query parameter olarak alınmaz. Bakiye, doğrulanmış JWT üzerinden bulunan current Employee için hesaplanır.



Örnek response:



```json
{
  "employeeId": "22222222-2222-2222-2222-222222222222",
  "leaveTypeId": "10000000-0000-0000-0000-000000000001",
  "leaveTypeName": "Annual Leave",
  "year": 2026,
  "entitledDays": 20,
  "usedDays": 5,
  "remainingDays": 15
}
```

### Calendar Sorgusu



```http
GET /api/leave-requests/calendar?startDate=2026-08-01&endDate=2026-08-31
```

Kurallar:

* Başlangıç ve bitiş tarihleri inclusive'dir.
* Ters tarih aralığı `400 Bad Request` ile reddedilir.
* Employee yalnız kendi kayıtlarını görür.
* Manager yalnız güncel ve aktif direct report kayıtlarını görür.
* HR tarih aralığıyla kesişen geniş kayıt kapsamını görebilir.
* Scope filtreleri PostgreSQL sorgusunda uygulanır.



### Departman İzin İstatistikleri



```http
GET /api/reports/department-leave-statistics
```

Örnek response:

```json
[
  {
    "departmentId": "33333333-3333-3333-3333-333333333333",
    "departmentName": "Engineering",
    "approvedRequestCount": 2,
    "totalApprovedLeaveDays": 6,
    "averageApprovedLeaveDaysPerRequest": 3
  }
]
```

Örnek GUID yalnız dokümantasyon verisidir.

Rapor kuralları:

* Yalnız HR erişebilir.
* Endpoint parametre almaz.
* Yalnız Approved talepler hesaplanır.
* Pending ve Rejected talepler dahil edilmez.
* Gruplama Employee'nin sorgu anındaki güncel departmanına göre yapılır.
* Onaylı talebi bulunmayan departmanlar listelenmez.

---



## Hata Davranışları

|Durum|HTTP Status|Açıklama|
|-|-|-|
|FluentValidation veya iş kuralı hatası|`400 Bad Request`|Örneğin geçersiz tarih aralığı, bakiye yetersizliği, overlap veya geçersiz lifecycle işlemi|
|Token yok veya token geçersiz|`401 Unauthorized`|Authentication başarısız|
|Geçerli token fakat role, policy veya active-state kontrolü başarısız|`403 Forbidden`|Örneğin HR review denemesi, inactive account veya token/DB role uyumsuzluğu|
|Kaynak bulunamadı|`404 Not Found`|İstenen kayıt gerçekten mevcut değil|
|Kaynak kullanıcının resource scope'u dışında|`404 Not Found`|Kaydın varlığını yetkisiz kullanıcıya sızdırmamak için|
|Login bilgileri geçersiz veya hesap kullanılamaz durumda|`401 Unauthorized`|Email/password/account durumu ayrıntısı public response'ta açıklanmaz|

API, validation ve business-rule hatalarında standart ProblemDetails tabanlı response'lar kullanır.

---



## Testleri Çalıştırma



Tüm solution testlerini çalıştırmak için:



```bash
dotnet test
```

Restore işlemi daha önce tamamlandıysa:



```bash
dotnet test --no-restore
```

Integration testler ayrı `leave_management_test_db` PostgreSQL veritabanı üzerinden çalışır. Test startup'i production `AppDbContext` options configuration'ını kontrollü olarak override eder, test connection string'ini kullanır ve audit interceptor'i test pipeline'ına bir kez bağlar. Gerekli migration'lar test veritabanına uygulanır.

Tam local kontrol için:



```bash
dotnet build --no-restore
dotnet test --no-restore
git diff --check
git status
```

Son kesin doğrulanan full-suite sonucu:

```text
529 total
529 succeeded
0 failed
0 skipped
Build succeeded
git diff --check clean
```

Test türleri:

* Application handler ve validator unit testleri
* Domain ve business-order testleri
* Infrastructure password hashing, JWT options validation, token generation ve DemoData options validation unit testleri
* WebAPI authorization/result/exception handler unit testleri
* Gerçek HTTP pipeline integration testleri
* Gerçek PostgreSQL repository ve aggregate sorgu testleri
* JWT login ve bearer validation testleri
* Employee/Manager/HR authorization matrix testleri
* Calendar/date-range scope testleri
* Department reporting aggregate testleri
* Swagger/OpenAPI authentication ve security metadata testleri
* DemoData seeding ve idempotency integration testi
* Audit trail scope/action/actor/changed-property integration testi

Test edilen ana senaryolardan bazıları:

* Employee ve LeaveRequest CRUD
* Validation pipeline ve ProblemDetails
* Overlap rejection
* Balance calculation
* Cross-year balance allocation
* Remaining balance kontrolü
* Direct manager approval ve rejection
* Reviewer identity'nin claims/current-user'dan türetilmesi
* Non-direct manager, Employee ve HR review yasakları
* UserAccount ve Employee active-state kontrolleri
* Token/DB role mismatch
* Missing/invalid token davranışı
* Employee own scope
* Manager active direct-report scope
* HR geniş read ve reporting scope'u
* Scope dışı tekil resource için 404
* Default ve fallback authorization
* Inclusive calendar overlap
* Geçersiz calendar tarih aralığı
* Approved-only department grouping
* Department count, total ve average hesapları
* Onaylı talebi olmayan departmanın rapordan dışlanması
* Swagger Bearer security metadata'sı
* EF Core/Npgsql sorgularinin gerçek PostgreSQL tarafında çalışması
* Demo seeding açıkken password configuration zorunluluğu
* Demo Employee/Manager/HR kayıtları ve direct-manager ilişkisi
* Repeated demo seed sonrasında duplicate/ID/hash/timestamp değişmemesi
* Approve/Reject success sonrasında notification'in tam bir kez üretilmesi
* Business-rule veya SaveChanges failure durumunda notification üretilmemesi
* Audit'in yalnız Employee ve LeaveRequest kapsamında kalması
* Create/Update/Delete/Approve/Reject audit action'ları
* Authenticated request actor EmployeeId ve system context actor `null` davranışı
* Audit changed-properties alanında değer yerine yalnız property isimlerinin tutulması

---



## Geliştirme Workflow'u



Projede branch bazlı geliştirme akışı kullanılır.



```text
main      -> stabil branch
develop   -> tamamlanan feature'ların birleştiği geliştirme branch'i
feature/* -> belirli kapsam için açılan branch'ler
```

Tipik geliştirme akışı:

```text
develop
  -> feature branch
    -> implementation
      -> targeted build/test
        -> full build/test
          -> git diff/status kontrolü
            -> commit ve push
              -> develop içine merge
```

Temel kurallar:

* Başarılı build ve test sonucu görülmeden commit atılmaz.
* `git diff --check` temiz olmadan commit atılmaz.
* Untracked dosyalar dahil değişen dosya kapsamı kontrol edilir.
* Her commit tek bir anlamlı konuyu kapsar.
* İlgisiz refactor aynı feature commit'ine eklenmez.
* Migration yalnız entity/configuration/schema değişikliğinde oluşturulur.
* Provider-specific EF Core sorguları gerçek PostgreSQL integration testiyle doğrulanır.
* `main` her küçük geliştirme için kullanılmaz; stabil sürüm hazırlığında güncellenir.

---

## Faz 1 Doğrulama

Faz 1 kapsamında aşağıdaki kontroller tamamlanmıştır:

* Employee CRUD endpointleri Swagger UI üzerinden manuel olarak test edildi.
* LeaveRequest CRUD endpointleri Swagger UI üzerinden manuel olarak test edildi.
* Employee ve LeaveRequest CRUD integration testleri başarıyla çalıştı.
* PostgreSQL bağlantısı doğrulandı.
* Initial migration başarıyla uygulandı.
* Varsayılan LeaveType seed data migration ile eklendi.
* Testler ayrı `leave_management_test_db` veritabanı üzerinden çalıştırıldı.
* Local test verileri manuel testlerden sonra temizlendi.
* `dotnet build` başarılı çalıştı.
* `dotnet test` başarılı çalıştı.
* `git diff --check` temiz sonuç verdi.
* Git working tree temiz olarak doğrulandı.

---

## Faz 2 Doğrulama

Faz 2 kapsamında aşağıdaki kontroller tamamlanmıştır:

* Leave balance endpointi test edildi.
* Approve ve reject endpointleri direct manager ile doğrulandı.
* Non-direct manager, Employee ve HR review denemeleri reddedildi.
* Overlap senaryosu `400 Bad Request` ile reddedildi.
* Yetersiz balance senaryosu `400 Bad Request` ile reddedildi.
* Yetki hataları `403 Forbidden` olarak doğrulandı.
* Cross-year leave balance mantığı integration test ile doğrulandı.
* Approved/Rejected lifecycle guard kuralları test edildi.
* Rejected request status-aware overlap davranışı test edildi.
* Zero allowance davranışı test edildi.
* CQRS'e taşınmadan önceki ürün davranışları testlerle sabitlendi.
* Faz 2 davranışları Faz 3 CQRS geçişinden sonra da regresyon testleriyle korundu.

---

## Faz 3 Doğrulama

Faz 3 kapsamında aşağıdaki kontroller tamamlanmıştır:

* Employee ve LeaveRequest use-case'lerinin CQRS/MediatR üzerinden çalıştığı doğrulandı.
* Controller'ların `ISender` kullandığı ve legacy service yollarının temizlendiği doğrulandı.
* FluentValidation pipeline'i unit ve HTTP integration testleriyle doğrulandı.
* Login endpointinin JWT access token ve role bilgisi döndürdüğü doğrulandı.
* Infrastructure unit testleriyle password hashing, verification, rehash-needed, JWT options validation ve token generation akışları test edildi.
* JWT signature, issuer, audience, lifetime ve required claim kontrolleri test edildi.
* Tokensiz veya geçersiz token ile protected endpoint erişimi engellendi.
* UserAccount/Employee active-state ve token/DB role uyumu doğrulandı.
* Employee, Manager ve HR authorization policy'leri test edildi.
* Reviewer identity'nin request body yerine claims/current-user'dan türetildiği doğrulandı.
* Default/fallback authorization ve `[AllowAnonymous]` metadata'sı test edildi.
* Calendar sorgusunun inclusive overlap ve role-scope davranışları test edildi.
* Department raporunun Approved-only grouping, count, total, average ve sıralama davranışları gerçek PostgreSQL ile doğrulandı.
* Swagger Bearer security metadata'sı test edildi.
* Son full-suite sonucu `524/524` başarılı olarak doğrulandı.
* `git diff --check` temiz sonuç verdi.
* Department reporting final commit'i sonrasında feature branch'in remote ile eşit ve working tree'nin temiz olduğu doğrulandı.

---

## Faz 4 Doğrulama

Faz 4 kapsamında aşağıdaki kontroller tamamlanmıştır:

* Multi-stage Docker image başarıyla build edildi.
* Docker Compose ile PostgreSQL ve Web API birlikte ayağa kaldırıldı.
* PostgreSQL `pg_isready` healthcheck'i ve API `/api/health` healthcheck'i doğrulandı.
* `/api/health` endpoint'inin PostgreSQL bağlantısını da kontrol ettiği doğrulandı.
* Startup migration'in temiz veritabanı üzerinde kontrollü olarak çalıştığı doğrulandı.
* Migration retry davranışı global EF Core retry'dan ayrıldı ve mevcut explicit transaction testleri korunarak full suite tekrar yeşile getirildi.
* `.env` dosyasının Git dışında kaldığı ve `.env.example` içinde yalnız placeholder/configuration alanları bulunduğu doğrulandı.
* DemoData configuration validation test edildi.
* Demo Employee, Manager ve HR kullanıcılarının gerçek Docker ortamında login olabildiği doğrulandı.
* Demo seeder gerçek PostgreSQL üzerinde iki kez çalıştırılarak idempotency, ID/hash/timestamp korunumu ve direct-manager ilişkisi test edildi.
* Notification logging approve ve reject için başarılı persistence sonrasında tam bir kez çalışacak şekilde test edildi.
* Business-rule failure ve `SaveChangesAsync` failure durumlarında notification üretilmediği test edildi.
* Audit interceptor'in production ve integration-test EF Core pipeline'ına doğru bağlandığı doğrulandı.
* `AddAuditLogs` migration'inin yalnız `AuditLogs` tablosu/index'lerini eklediği ve `ActorEmployeeId` için FK oluşturmadığı kontrol edildi.
* Audit behavior gerçek PostgreSQL integration testiyle actor, action, scope, timestamp ve changed-properties bazında doğrulandı.
* Son full-suite sonucu `529/529` başarılı olarak doğrulandı.
* `dotnet build` başarılı ve `git diff --check` temiz olarak doğrulandı.