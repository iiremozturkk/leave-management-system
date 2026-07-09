\# Personel Ä°zin YÃ¶netim Sistemi



Personel Ä°zin YÃ¶netim Sistemi, Ã§alÄ±ÅŸanlarÄ±n izin taleplerini yÃ¶netmek iÃ§in geliÅŸtirilen bir backend API projesidir. Proje; ASP.NET Core Web API, Entity Framework Core, PostgreSQL ve Clean Architecture prensiplerine uygun katmanlÄ± bir yapÄ± ile geliÅŸtirilmektedir.



Bu repository, staj projesi kapsamÄ±nda fazlara ayrÄ±lmÄ±ÅŸ ÅŸekilde geliÅŸtirilmektedir. Mevcut durumda Faz 1 kapsamÄ±nda temel mimari yapÄ±, veritabanÄ± altyapÄ±sÄ±, domain modeli ve temel CRUD endpointleri tamamlanmÄ±ÅŸtÄ±r.



\---



\## Proje Durumu



| Faz | Kapsam | Durum |

|---|---|---|

| Faz 1 | Temel mimari, entity yapÄ±sÄ±, EF Core, migration, temel CRUD endpointleri | TamamlandÄ± |

| Faz 2 | Ä°ÅŸ kurallarÄ±, validasyon, izin hakkÄ± ve onay kurallarÄ± | PlanlandÄ± |

| Faz 3 | CQRS, MediatR, JWT authentication, authorization ve raporlama | PlanlandÄ± |

| Faz 4 | GeliÅŸmiÅŸ testler, final dokÃ¼mantasyon ve teslim hazÄ±rlÄ±ÄŸÄ± | PlanlandÄ± |



\---



\## Faz 1 KapsamÄ±nda Tamamlananlar



\- Domain, Application, Infrastructure ve WebAPI katmanlarÄ± oluÅŸturuldu.

\- `Employee`, `Department`, `LeaveType` ve `LeaveRequest` entity'leri tanÄ±mlandÄ±.

\- `Employee` entity'sinde `ManagerId` ile self-referencing yÃ¶netici iliÅŸkisi kuruldu.

\- Entity Framework Core `DbContext` yapÄ±sÄ± oluÅŸturuldu.

\- PostgreSQL baÄŸlantÄ±sÄ± yapÄ±landÄ±rÄ±ldÄ±.

\- Ä°lk migration oluÅŸturuldu ve baÅŸarÄ±yla uygulandÄ±.

\- Local geliÅŸtirme ortamÄ± iÃ§in Docker Compose ile PostgreSQL kurulumu eklendi.

\- Employee iÃ§in temel CRUD endpointleri yazÄ±ldÄ±.

\- LeaveRequest iÃ§in temel CRUD endpointleri yazÄ±ldÄ±.

\- Swagger UI ile API endpointleri test edilebilir hale getirildi.

\- Temel integration test projesi oluÅŸturuldu.

\- Faz 1 endpointleri Swagger UI Ã¼zerinden manuel olarak test edildi.



\---



\## Mimari YapÄ±



Proje Clean Architecture prensiplerine uygun katmanlÄ± bir yapÄ± kullanmaktadÄ±r.



```text

src/

&#x20; LeaveManagementSystem.Domain

&#x20; LeaveManagementSystem.Application

&#x20; LeaveManagementSystem.Infrastructure

&#x20; LeaveManagementSystem.WebAPI



tests/

&#x20; LeaveManagementSystem.IntegrationTests

```



\### KatmanlarÄ±n SorumluluklarÄ±



| Katman | Sorumluluk |

|---|---|

| Domain | Temel entity'ler, enum'lar ve Ã§ekirdek iÅŸ modeli |

| Application | DTO'lar ve servis interface'leri |

| Infrastructure | EF Core, PostgreSQL, DbContext ve servis implementasyonlarÄ± |

| WebAPI | Controller'lar, HTTP endpointleri ve Swagger konfigÃ¼rasyonu |

| IntegrationTests | API seviyesinde integration testler |



Mevcut request akÄ±ÅŸÄ±:



```text

Controller

&#x20; -> Application service interface

&#x20;   -> Infrastructure service implementation

&#x20;     -> AppDbContext

&#x20;       -> PostgreSQL

```



Bu yapÄ± sayesinde WebAPI katmanÄ± doÄŸrudan `AppDbContext` ile Ã§alÄ±ÅŸmaz. Controller'lar Application katmanÄ±ndaki servis interface'leri Ã¼zerinden iÅŸlem yapar.



\---



\## KullanÄ±lan Teknolojiler



\- C#

\- .NET 10

\- ASP.NET Core Web API

\- Entity Framework Core

\- PostgreSQL

\- Docker Compose

\- Swagger / Swashbuckle

\- xUnit

\- Git / GitHub



\---



\## Domain Model Ã–zeti



\### Employee



Ã‡alÄ±ÅŸan bilgisini temsil eder.



Ã–ne Ã§Ä±kan alanlar:



\- `FirstName`

\- `LastName`

\- `Email`

\- `DepartmentId`

\- `ManagerId`

\- `Role`

\- `IsActive`



`ManagerId` alanÄ± ile aynÄ± tablo Ã¼zerinde self-referencing iliÅŸki kurulmuÅŸtur. BÃ¶ylece bir Ã§alÄ±ÅŸan baÅŸka bir Ã§alÄ±ÅŸanÄ±n yÃ¶neticisi olabilir.



```text

Employee

&#x20; -> Manager

&#x20; -> DirectReports

```



Employee silme iÅŸlemi ÅŸu an soft delete olarak uygulanmaktadÄ±r. Yani Ã§alÄ±ÅŸan veritabanÄ±ndan fiziksel olarak silinmez, `IsActive` deÄŸeri `false` yapÄ±lÄ±r.



\### Department



Ã‡alÄ±ÅŸanÄ±n baÄŸlÄ± olduÄŸu departmanÄ± temsil eder.



\### LeaveType



Ä°zin tÃ¼rÃ¼nÃ¼ temsil eder.



Ã–rnek izin tÃ¼rleri:



```text

Annual Leave

Sick Leave

Unpaid Leave

```



\### LeaveRequest



Bir Ã§alÄ±ÅŸanÄ±n izin talebini temsil eder.



Ã–ne Ã§Ä±kan alanlar:



\- `EmployeeId`

\- `LeaveTypeId`

\- `StartDate`

\- `EndDate`

\- `RequestedDays`

\- `Status`

\- `Reason`

\- `ManagerComment`

\- `ReviewedAtUtc`

\- `ReviewedByEmployeeId`



Yeni oluÅŸturulan izin talepleri varsayÄ±lan olarak `Pending` durumunda baÅŸlar. `RequestedDays` deÄŸeri tarih aralÄ±ÄŸÄ±na gÃ¶re otomatik hesaplanÄ±r.



\---



\## Gereksinimler



Projeyi Ã§alÄ±ÅŸtÄ±rmak iÃ§in aÅŸaÄŸÄ±daki araÃ§larÄ±n kurulu olmasÄ± gerekir:



\- .NET 10 SDK

\- Docker Desktop

\- Git

\- DBeaver, pgAdmin veya benzeri bir veritabanÄ± aracÄ±



Entity Framework CLI yÃ¼klÃ¼ deÄŸilse aÅŸaÄŸÄ±daki komut ile kurulabilir:



```bash

dotnet tool install --global dotnet-ef

```



\---



\## Projeyi Ã‡alÄ±ÅŸtÄ±rma



AÅŸaÄŸÄ±daki komutlar repository ana dizininde Ã§alÄ±ÅŸtÄ±rÄ±lmalÄ±dÄ±r.



\### 1. Repository'yi klonlama



```bash

git clone <repository-url>

cd leave-management-system

```



\### 2. PostgreSQL'i Docker Compose ile baÅŸlatma



```bash

docker compose up -d

```



Local PostgreSQL baÄŸlantÄ± bilgileri:



```text

Host: localhost

Port: 5432

Database: leave\_management\_db

Username: postgres

Password: postgres

```



\### 3. Migration'larÄ± uygulama



```bash

dotnet ef database update --project src/LeaveManagementSystem.Infrastructure --startup-project src/LeaveManagementSystem.WebAPI

```



\### 4. Web API'yi Ã§alÄ±ÅŸtÄ±rma



```bash

dotnet run --project src/LeaveManagementSystem.WebAPI

```



Uygulama Ã§alÄ±ÅŸtÄ±ktan sonra Swagger UI aÅŸaÄŸÄ±daki adresten aÃ§Ä±labilir:



```text

http://localhost:5252/swagger

```



Uygulama farklÄ± bir portta baÅŸlarsa terminalde gÃ¶rÃ¼nen URL kullanÄ±lmalÄ± ve sonuna `/swagger` eklenmelidir.



\---



\## API Endpointleri



\### Employees



| Method | Endpoint | AÃ§Ä±klama |

|---|---|---|

| GET | `/api/employees` | TÃ¼m Ã§alÄ±ÅŸanlarÄ± listeler |

| GET | `/api/employees/{id}` | Id'ye gÃ¶re Ã§alÄ±ÅŸan getirir |

| POST | `/api/employees` | Yeni Ã§alÄ±ÅŸan oluÅŸturur |

| PUT | `/api/employees/{id}` | Ã‡alÄ±ÅŸan bilgilerini gÃ¼nceller |

| DELETE | `/api/employees/{id}` | Ã‡alÄ±ÅŸanÄ± pasif hale getirir |



\### Leave Requests



| Method | Endpoint | AÃ§Ä±klama |

|---|---|---|

| GET | `/api/leave-requests` | TÃ¼m izin taleplerini listeler |

| GET | `/api/leave-requests/{id}` | Id'ye gÃ¶re izin talebi getirir |

| POST | `/api/leave-requests` | Yeni izin talebi oluÅŸturur |

| PUT | `/api/leave-requests/{id}` | Ä°zin talebini gÃ¼nceller |

| DELETE | `/api/leave-requests/{id}` | Ä°zin talebini siler |



Not: Åžu an sadece `Pending` durumundaki izin talepleri gÃ¼ncellenebilir veya silinebilir.



\---



\## Ã–rnek Request Body'leri



\### Employee OluÅŸturma



```json

{

&#x20; "firstName": "John",

&#x20; "lastName": "Doe",

&#x20; "email": "john.doe@example.com",

&#x20; "departmentId": "11111111-1111-1111-1111-111111111111",

&#x20; "managerId": null,

&#x20; "role": 0

}

```



\### LeaveRequest OluÅŸturma



```json

{

&#x20; "employeeId": "22222222-2222-2222-2222-222222222222",

&#x20; "leaveTypeId": "33333333-3333-3333-3333-333333333333",

&#x20; "startDate": "2026-07-15",

&#x20; "endDate": "2026-07-17",

&#x20; "reason": "Annual leave request."

}

```



Not: LeaveRequest oluÅŸturmak iÃ§in veritabanÄ±nda geÃ§erli bir `Employee` ve `LeaveType` kaydÄ± bulunmalÄ±dÄ±r. VarsayÄ±lan LeaveType seed data eklenmesi sonraki kalite iyileÅŸtirme adÄ±mlarÄ±ndan biridir.



\---



\## Testleri Ã‡alÄ±ÅŸtÄ±rma



TÃ¼m testleri Ã§alÄ±ÅŸtÄ±rmak iÃ§in:



```bash

dotnet test

```



Tam local kontrol iÃ§in:



```bash

dotnet build

dotnet test

git diff --check

git status

```



\---



\## GeliÅŸtirme Workflow'u



Projede branch bazlÄ± geliÅŸtirme akÄ±ÅŸÄ± kullanÄ±lmaktadÄ±r.



```text

main      -> stabil branch

develop   -> aktif geliÅŸtirme branch'i

feature/\* -> belirli iÅŸler iÃ§in aÃ§Ä±lan branch'ler

```



Tipik geliÅŸtirme akÄ±ÅŸÄ±:



```text

develop

&#x20; -> feature branch

&#x20;   -> implementation

&#x20;   -> build ve test

&#x20;   -> develop iÃ§ine merge

```



`main` branch'i her kÃ¼Ã§Ã¼k geliÅŸtirme iÃ§in kullanÄ±lmamaktadÄ±r. Stabil bir sÃ¼rÃ¼m hazÄ±r olduÄŸunda veya Ã¶zellikle gerekli olduÄŸunda gÃ¼ncellenir.



\---



\## Faz 1 DoÄŸrulama



Faz 1 kapsamÄ±nda aÅŸaÄŸÄ±daki kontroller tamamlanmÄ±ÅŸtÄ±r:



\- Employee CRUD endpointleri Swagger UI Ã¼zerinden test edildi.

\- LeaveRequest CRUD endpointleri Swagger UI Ã¼zerinden test edildi.

\- PostgreSQL baÄŸlantÄ±sÄ± doÄŸrulandÄ±.

\- Initial migration baÅŸarÄ±yla uygulandÄ±.

\- Local test verileri manuel testlerden sonra temizlendi.

\- `dotnet build` baÅŸarÄ±lÄ± Ã§alÄ±ÅŸtÄ±.

\- `dotnet test` baÅŸarÄ±lÄ± Ã§alÄ±ÅŸtÄ±.

\- `git diff --check` temiz sonuÃ§ verdi.

\- Git working tree temiz olarak doÄŸrulandÄ±.



\---



\## Planlanan Sonraki Ä°yileÅŸtirmeler



Faz 1 sonrasÄ± kalite iyileÅŸtirmeleri:



\- VarsayÄ±lan LeaveType seed data eklenmesi

\- Employee ve LeaveRequest iÃ§in ek CRUD integration testleri

\- README dosyasÄ±nÄ±n sonraki fazlara gÃ¶re gÃ¼ncellenmesi
