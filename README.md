# Personel Izin ve Onay Yonetim Sistemi

Personel Izin Yonetim Sistemi, calisanlarin izin taleplerini yonetmek icin gelistirilen bir backend API projesidir. Proje; ASP.NET Core Web API, Entity Framework Core, PostgreSQL ve Clean Architecture prensiplerine uygun katmanli bir yapi ile gelistirilmektedir.

Bu repository, staj projesi kapsaminda fazlara ayrilmis sekilde gelistirilmektedir. Mevcut durumda Faz 1 kapsaminda temel mimari yapi, veritabani altyapisi, domain modeli, varsayilan izin turleri ve temel CRUD endpointleri tamamlanmistir.

---

## Proje Durumu

| Faz | Kapsam | Durum |
|---|---|---|
| Faz 1 | Temel mimari, entity yapisi, EF Core, migration, seed data ve temel CRUD endpointleri | Tamamlandi |
| Faz 2 | Is kurallari, validasyon, izin hakki ve onay kurallari | Planlandi |
| Faz 3 | CQRS, MediatR, JWT authentication, authorization ve raporlama | Planlandi |
| Faz 4 | Gelismis testler, final dokumantasyon ve teslim hazirligi | Planlandi |

---

## Faz 1 Kapsaminda Tamamlananlar

- Domain, Application, Infrastructure ve WebAPI katmanlari olusturuldu.
- `Employee`, `Department`, `LeaveType` ve `LeaveRequest` entity'leri tanimlandi.
- `Employee` entity'sinde `ManagerId` ile self-referencing yonetici iliskisi kuruldu.
- Entity Framework Core `DbContext` yapisi olusturuldu.
- PostgreSQL baglantisi yapilandirildi.
- Ilk migration olusturuldu ve basariyla uygulandi.
- Varsayilan `LeaveType` kayitlari migration ile seed data olarak eklendi.
- Local gelistirme ortami icin Docker Compose ile PostgreSQL kurulumu eklendi.
- Employee icin temel CRUD endpointleri yazildi.
- LeaveRequest icin temel CRUD endpointleri yazildi.
- Swagger UI ile API endpointleri test edilebilir hale getirildi.
- Temel integration test projesi olusturuldu.
- Employee ve LeaveRequest icin CRUD integration testleri eklendi.
- Faz 1 endpointleri Swagger UI uzerinden manuel olarak test edildi.

---

## Mimari Yapi

Proje Clean Architecture prensiplerine uygun katmanli bir yapi kullanmaktadir.

```text
src/
  LeaveManagementSystem.Domain
  LeaveManagementSystem.Application
  LeaveManagementSystem.Infrastructure
  LeaveManagementSystem.WebAPI

tests/
  LeaveManagementSystem.IntegrationTests
```

### Katmanlarin Sorumluluklari

| Katman | Sorumluluk |
|---|---|
| Domain | Temel entity'ler, enum'lar ve cekirdek is modeli |
| Application | DTO'lar ve servis interface'leri |
| Infrastructure | EF Core, PostgreSQL, DbContext ve servis implementasyonlari |
| WebAPI | Controller'lar, HTTP endpointleri ve Swagger konfigurasyonu |
| IntegrationTests | API seviyesinde integration testler |

Mevcut request akisi:

```text
Controller
  -> Application service interface
    -> Infrastructure service implementation
      -> AppDbContext
        -> PostgreSQL
```

Bu yapi sayesinde WebAPI katmani dogrudan `AppDbContext` ile calismaz. Controller'lar Application katmanindaki servis interface'leri uzerinden islem yapar.

---

## Kullanilan Teknolojiler

- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Docker Compose
- Swagger / Swashbuckle
- xUnit
- Git / GitHub

---

## Domain Model Ozeti

### Employee

Calisan bilgisini temsil eder.

One cikan alanlar:

- `FirstName`
- `LastName`
- `Email`
- `DepartmentId`
- `ManagerId`
- `Role`
- `IsActive`

`ManagerId` alani ile ayni tablo uzerinde self-referencing iliski kurulmustur. Boylece bir calisan baska bir calisanin yoneticisi olabilir.

```text
Employee
  -> Manager
  -> DirectReports
```

Employee silme islemi su an soft delete olarak uygulanmaktadir. Yani calisan veritabanindan fiziksel olarak silinmez, `IsActive` degeri `false` yapilir.

### Department

Calisanin bagli oldugu departmani temsil eder.

### LeaveType

Izin turunu temsil eder. Varsayilan izin turleri migration ile veritabanina eklenmektedir:

```text
Annual Leave
Sick Leave
Unpaid Leave
```

### LeaveRequest

Bir calisanin izin talebini temsil eder.

One cikan alanlar:

- `EmployeeId`
- `LeaveTypeId`
- `StartDate`
- `EndDate`
- `RequestedDays`
- `Status`
- `Reason`
- `ManagerComment`
- `ReviewedAtUtc`
- `ReviewedByEmployeeId`

Yeni olusturulan izin talepleri varsayilan olarak `Pending` durumunda baslar. `RequestedDays` degeri tarih araligina gore otomatik hesaplanir.

---

## Gereksinimler

Projeyi calistirmak icin asagidaki araclarin kurulu olmasi gerekir:

- .NET 10 SDK
- Docker Desktop
- Git
- DBeaver, pgAdmin veya benzeri bir veritabani araci

Entity Framework CLI yuklu degilse asagidaki komut ile kurulabilir:

```bash
dotnet tool install --global dotnet-ef
```

---

## Projeyi Calistirma

Asagidaki komutlar repository ana dizininde calistirilmalidir.

### 1. Repository'yi klonlama

```bash
git clone <repository-url>
cd leave-management-system
```

### 2. PostgreSQL'i Docker Compose ile baslatma

```bash
docker compose up -d
```

Local PostgreSQL baglanti bilgileri:

```text
Host: localhost
Port: 5432
Database: leave_management_db
Username: postgres
Password: postgres
```

### 3. Migration'lari uygulama

```bash
dotnet ef database update --project src/LeaveManagementSystem.Infrastructure --startup-project src/LeaveManagementSystem.WebAPI
```

### 4. Web API'yi calistirma

```bash
dotnet run --project src/LeaveManagementSystem.WebAPI
```

Uygulama calistiktan sonra Swagger UI asagidaki adresten acilabilir:

```text
http://localhost:5252/swagger
```

Uygulama farkli bir portta baslarsa terminalde gorunen URL kullanilmali ve sonuna `/swagger` eklenmelidir.

---

## API Endpointleri

### Employees

| Method | Endpoint | Aciklama |
|---|---|---|
| GET | `/api/employees` | Tum calisanlari listeler |
| GET | `/api/employees/{id}` | Id'ye gore calisan getirir |
| POST | `/api/employees` | Yeni calisan olusturur |
| PUT | `/api/employees/{id}` | Calisan bilgilerini gunceller |
| DELETE | `/api/employees/{id}` | Calisani pasif hale getirir |

### Leave Requests

| Method | Endpoint | Aciklama |
|---|---|---|
| GET | `/api/leave-requests` | Tum izin taleplerini listeler |
| GET | `/api/leave-requests/{id}` | Id'ye gore izin talebi getirir |
| POST | `/api/leave-requests` | Yeni izin talebi olusturur |
| PUT | `/api/leave-requests/{id}` | Izin talebini gunceller |
| DELETE | `/api/leave-requests/{id}` | Izin talebini siler |

Not: Su an sadece `Pending` durumundaki izin talepleri guncellenebilir veya silinebilir.

---

## Ornek Request Body'leri

### Employee Olusturma

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

### LeaveRequest Olusturma

```json
{
  "employeeId": "22222222-2222-2222-2222-222222222222",
  "leaveTypeId": "10000000-0000-0000-0000-000000000001",
  "startDate": "2026-07-15",
  "endDate": "2026-07-17",
  "reason": "Annual leave request."
}
```

Not: LeaveRequest olusturmak icin veritabaninda gecerli bir `Employee` kaydi bulunmalidir. Varsayilan `LeaveType` kayitlari migration ile otomatik eklenmektedir.

---

## Testleri Calistirma

Tum testleri calistirmak icin:

```bash
dotnet test
```

Integration testler ayri `leave_management_test_db` veritabani uzerinden calisir. Testler calistirilirken migration'lar bu test veritabanina uygulanir.

Tam local kontrol icin:

```bash
dotnet build
dotnet test
git diff --check
git status
```

---

## Gelistirme Workflow'u

Projede branch bazli gelistirme akisi kullanilmaktadir.

```text
main      -> stabil branch
develop   -> aktif gelistirme branch'i
feature/* -> belirli isler icin acilan branch'ler
```

Tipik gelistirme akisi:

```text
develop
  -> feature branch
    -> implementation
    -> build ve test
    -> develop icine merge
```

`main` branch'i her kucuk gelistirme icin kullanilmamaktadir. Stabil bir surum hazir oldugunda veya ozellikle gerekli oldugunda guncellenir.

---

## Faz 1 Dogrulama

Faz 1 kapsaminda asagidaki kontroller tamamlanmistir:

- Employee CRUD endpointleri Swagger UI uzerinden manuel olarak test edildi.
- LeaveRequest CRUD endpointleri Swagger UI uzerinden manuel olarak test edildi.
- Employee ve LeaveRequest CRUD integration testleri basariyla calisti.
- PostgreSQL baglantisi dogrulandi.
- Initial migration basariyla uygulandi.
- Varsayilan LeaveType seed data migration ile eklendi.
- Testler ayri `leave_management_test_db` veritabani uzerinden calistirildi.
- Local test verileri manuel testlerden sonra temizlendi.
- `dotnet build` basarili calisti.
- `dotnet test` basarili calisti.
- `git diff --check` temiz sonuc verdi.
- Git working tree temiz olarak dogrulandi.