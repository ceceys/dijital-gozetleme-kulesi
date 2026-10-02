# Dijital Gözetleme Kulesi

Web sitelerinin erişilebilirliğini, SSL sertifika süresini ve alan adı bitiş tarihini tek ekranda izleyen Windows konsol aracı.

| Sürüm | Platform | Lisans | İndirme |
|---|---|---|---|
| 1.0.0 | Windows, .NET Framework 4 | MIT | [Son sürüm](../../releases/latest) |

Sorumlu olunan sitelerin her sabah elle kontrol edilmesi yerine tek komutla durum raporu almak için geliştirildi.

---

## Ne yapar?

Listedeki her site için sırayla:

- **HTTP denetimi:** durum kodu ve yanıt süresi (ms). 403 yanıtlarında bot korumasını ayrıca belirtir.
- **DNS:** sitenin çözümlendiği IP adresleri (en fazla üç).
- **SSL:** TLS el sıkışması sırasında sertifikayı okur; protokol, şifreleme algoritması, sertifika sağlayıcısı ve kalan gün.
- **Alan adı:** WHOIS sunucusuna (port 43) doğrudan bağlanıp bitiş tarihini bulur. `.com`, `.net`, `.org`, `.info` ve `.tr` uzantılarını destekler.

Tarama bitince bütün siteler renk kodlu tek bir tabloda özetlenir.

| Durum | SSL | Alan adı |
|---|---|---|
| Uyarı (sarı) | 15 günden az | 30 günden az |
| Kritik (kırmızı) | süresi geçmiş | süresi geçmiş |

## Kullanım

1. [Son sürümden](../../releases/latest) `Watcher.exe` ve `siteler.txt` dosyalarını aynı klasöre indirin.
2. `siteler.txt` dosyasına izlenecek siteleri her satıra bir tane gelecek şekilde yazın:

   ```
   ornek.com
   ornek.com.tr
   ornek.org
   ```

3. `Watcher.exe` dosyasını çalıştırın.

## Teknik ayrıntılar

| Bileşen | Kullanılan |
|---|---|
| Sertifika okuma | `SslStream`, `RemoteCertificateValidationCallback`, `X509Certificate2` |
| WHOIS | `TcpClient` ile port 43, TLD'ye göre sunucu seçimi, Regex ile tarih ayrıştırma |
| HTTP | `HttpWebRequest`, tarayıcı başlıkları, TLS 1.2 |
| Arayüz | Konsol, ayrı iş parçacığında çalışan ilerleme göstergesi |

Kaynak kod tek dosyadadır: [`Watcher.vb`](Watcher.vb).

## Not

WHOIS sunucularına kısa aralıklarla çok sayıda sorgu göndermek IP adresinin geçici olarak engellenmesine yol açabilir. Araç bilgi toplama ve izleme amaçlıdır.

## Lisans

[MIT](LICENSE) · **by cecey** · [LinkedIn](https://www.linkedin.com/in/cuma-ali-dirik/) · [GitHub](https://github.com/ceceys)

---

### English summary

**Dijital Gözetleme Kulesi** ("Digital Watchtower") is a Windows console tool written in VB.NET that checks a list of websites in one run: HTTP status and latency, resolved IPs, TLS certificate details and days left, and domain expiry via direct WHOIS (port 43) queries for .com, .net, .org, .info and .tr. Results are summarized in a color-coded table. MIT licensed.
