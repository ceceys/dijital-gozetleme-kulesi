#  Dijital Gözetleme Kulesi (Digital Watchtower)
![VB.NET](https://img.shields.io/badge/Language-VB.NET-blue?style=for-the-badge&logo=visual-studio)
![Security](https://img.shields.io/badge/Security-SSL%20Check-brightgreen?style=for-the-badge&logo=lock)
![Network](https://img.shields.io/badge/Network-WHOIS%20Track-orange?style=for-the-badge&logo=globus)
![License](https://img.shields.io/badge/License-MIT-red?style=for-the-badge)

> **Advanced Network & Security Monitor** > Web varlıklarınızın sağlık durumunu, SSL güvenliğini ve alan adı otoritesini tek bir terminal ekranından yönetin, Eğer benim gibi web sitelerinizi günlük takip etme rutinine sahip olmak istiyorsanız bu araç sizi sıkıcı manuel kontrolden kurtarıp işlerinizi otomatikleştirecek.

## 🎯 Proje Hakkında
Bu proje, sistem yöneticileri ve web geliştiricileri için tasarlanmış, **VB.NET** tabanlı hibrit bir tarama aracıdır. Standart `ping` komutlarının ötesine geçerek, hedef sunucu ile **TLS 1.2** üzerinden el sıkışır, sertifika otoritesini (Issuer) analiz eder ve **WHOIS** sunucularına (Port 43) doğrudan bağlanarak domain bitiş tarihlerini sorgular.

![gozetlemekulesi](https://github.com/user-attachments/assets/a6c03d08-e8ce-4d4e-bf59-18466ba9ccb7)

## Temel Özellikler

###  Derinlemesine SSL Analizi
Sadece "sertifika var mı?" diye bakmaz. `X509Certificate2` sınıfını kullanarak şunları analiz eder:
- **Kalan Gün Hesaplama:** Bitiş süresine göre renkli uyarı sistemi (Kritik/Uyarı/Güvenli).
- **Protokol Detayları:** TLS versiyonu (örn. TLS 1.2) ve Şifreleme Algoritması (örn. AES 256).
- **Otorite Kontrolü:** Sertifikayı sağlayan kurum (Google Trust Services, R3, DigiCert vb.).

### Akıllı WHOIS (Domain) Takibi
HTTP isteklerinden bağımsız olarak, TCP üzerinden WHOIS sunucularına bağlanır.
- **TLD Duyarlı:** `.com`, `.net`, `.org` ve özellikle **`.tr` (METU/TRABİS)** uzantıları için özelleştirilmiş sunucu seçimi yapar.
- **Regex Parsing:** Ham WHOIS verisi içerisinden "Expiration Date" bilgisini ayıklar.

### Performans ve HTTP Denetimi
- **Latency Ölçümü:** Sunucu yanıt süresini milisaniye (ms) cinsinden ölçer.
- **Bot Koruması Algılama:** 403 hatalarını analiz ederek WAF/Bot koruması olup olmadığını raporlar.
- **Tarayıcı Simülasyonu:** Gerçek bir tarayıcı (User-Agent) gibi davranarak sunucu tarafındaki filtreleri aşar.

### Görsel Raporlama
- **Deep Dive Mode:** Tarama sırasında canlı "Spinner" animasyonu.
- **Dashboard:** İşlem sonunda tüm siteleri tek tabloda özetleyen renk kodlu (Yeşil/Sarı/Kırmızı) detaylı rapor.

---

Teknoloji,Kullanım Amacı
System.Net.Security,SslStream ve RemoteCertificateValidationCallback ile man-in-the-middle mantığında sertifika yakalama.
System.Net.Sockets,TcpClient ile Port 43 (WHOIS) ham veri iletişimi.
Multithreading,UI donmasını önlemek için Task.Factory ile asenkron spinner animasyonu.
Regex,Karmaşık WHOIS metin çıktılarından tarih formatlarını (yyyy-MM-dd) ayrıştırma.

Yasal Uyarı
Bu araç sadece bilgi toplama ve yönetim amaçlıdır. WHOIS sunucularına çok sık istek göndermek IP adresinizin geçici olarak engellenmesine neden olabilir.

