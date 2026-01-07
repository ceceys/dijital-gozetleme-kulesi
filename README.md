#  Dijital Gözetleme Kulesi (Digital Watchtower)
![VB.NET](https://img.shields.io/badge/Language-VB.NET-blue?style=for-the-badge&logo=visual-studio)
![Security](https://img.shields.io/badge/Security-SSL%20Check-brightgreen?style=for-the-badge&logo=lock)
![Network](https://img.shields.io/badge/Network-WHOIS%20Track-orange?style=for-the-badge&logo=globus)
![License](https://img.shields.io/badge/License-MIT-red?style=for-the-badge)


> **Advanced Network & Security Monitor** > Web varlıklarınızın sağlık durumunu, SSL güvenliğini ve alan adı otoritesini tek bir terminal ekranından yönetin, Eğer benim gibi web sitelerinizi günlük takip etme rutinine sahip olmak istiyorsanız bu araç sizi sıkıcı manuel kontrolden kurtarıp işlerinizi otomatikleştirecek.

## 🎯 Proje Hakkında
Bu proje, sistem yöneticileri ve web geliştiricileri için tasarlanmış, **VB.NET** tabanlı hibrit bir tarama aracıdır. Standart `ping` komutlarının ötesine geçerek, hedef sunucu ile **TLS 1.2** üzerinden el sıkışır, sertifika otoritesini (Issuer) analiz eder ve **WHOIS** sunucularına (Port 43) doğrudan bağlanarak domain bitiş tarihlerini sorgular.

Nasıl Kullanılır?
Uygulamanın sorunsuz çalışması için Watcher.exe ve siteler.txt dosyalarının aynı klasör içerisinde bulunması gerekmektedir.

1. Adım: Dosyaları Edinin
Tüm gerekli dosyaları tek seferde indirmek için Google Drive klasörünü kullanabilirsiniz:

Tüm Dosyaları İndir (Google Drive)
https://drive.google.com/drive/folders/1PKsWxx9cDS4lYn9GMigQrvA7hgtGYzC0?usp=sharing
-

2. Adım: Klasör Yapısını Kontrol Edin
İndirdiğiniz dosyaları bir klasöre çıkarttığınızda görünüm şu şekilde olmalıdır:

Klasör | Watcher.exe | siteler.txt | Watcher.vb
-


3. Adım: Kendi Listeni Oluştur
siteler.txt dosyasını açın ve takip etmek istediğiniz web sitelerini her satıra bir tane gelecek şekilde yazıp kaydedin:

google.com,aselsan.com,cecey.net...
-

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

Teknik Altyapı ve Çalışma Mantığı
Bu uygulama, arka planda birkaç kritik teknolojiyi bir arada kullanarak derinlemesine tarama yapar. İlk olarak, System.Net.Security kütüphanesindeki SslStream ve RemoteCertificateValidationCallback özelliklerinden yararlanarak, hedef sunucuyla güvenli bir bağlantı kurar ve sertifika detaylarını henüz el sıkışma aşamasında bir "man-in-the-middle" mantığıyla yakalar. Alan adı bilgilerine ulaşmak için ise System.Net.Sockets üzerinden TcpClient kullanarak doğrudan Port 43 (WHOIS) sunucularıyla ham veri iletişimi kurar.

Kullanıcı deneyimini iyileştirmek adına, yoğun tarama işlemleri sırasında arayüzün donmasını engellemek için Multithreading (Çoklu İş Parçacığı) yapısı kullanılmıştır; Task.Factory sayesinde asenkron bir "spinner" animasyonu arka planda akıcı bir şekilde çalışır. Son olarak, WHOIS sunucularından gelen karmaşık ve düzensiz metin yığınları arasından tarih bilgilerini hatasız bir şekilde ayıklamak için gelişmiş Regex (Düzenli İfadeler) desenleri kullanılarak yyyy-MM-dd formatında veri madenciliği yapılır.

Lisans ve Özgürlük
Bu proje tamamen açık kaynaklıdır. Proje içerisindeki tüm dosyaları (Watcher.vb, Watcher.exe, siteler.txt) dilediğiniz gibi indirebilir, değiştirebilir, geliştirebilir ve kendi adınızla veya markanızla yeniden yayınlayabilirsiniz. Kod üzerinde herhangi bir kısıtlama yoktur; geliştirip daha ileriye taşımanızdan mutluluk duyarım!
----

The application leverages System.Net.Security's SslStream and RemoteCertificateValidationCallback to capture certificate details during the handshake process. Domain information is retrieved via System.Net.Sockets by establishing raw TCP communication with WHOIS servers on Port 43. To ensure a smooth user interface, Multithreading with Task.Factory handles asynchronous animations , while advanced Regex patterns parse complex WHOIS outputs to extract expiry dates in yyyy-MM-dd format.


License & Contribution
This project is fully open-source. You are free to download, modify, enhance, and republish all files within this project (Watcher.vb, Watcher.exe, siteler.txt) as you see fit. There are no restrictions on the code; feel free to build upon it and share your own version!
----

Yasal Uyarı
Bu araç sadece bilgi toplama ve yönetim amaçlıdır. WHOIS sunucularına çok sık istek göndermek IP adresinizin geçici olarak engellenmesine neden olabilir.



