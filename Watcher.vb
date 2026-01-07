Imports System
Imports System.IO
Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Net.Security
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Diagnostics
Imports System.Security.Cryptography.X509Certificates
Imports System.Security.Authentication
Imports System.Collections.Generic
Imports System.Text.RegularExpressions

Module Watcher
    ' Configuration
    Private ReadOnly SuccessColor As ConsoleColor = ConsoleColor.Green
    Private ReadOnly ErrorColor As ConsoleColor = ConsoleColor.Red
    Private ReadOnly WarningColor As ConsoleColor = ConsoleColor.Yellow
    Private ReadOnly InfoColor As ConsoleColor = ConsoleColor.Cyan
    Private ReadOnly LabelColor As ConsoleColor = ConsoleColor.DarkGray
    Private ReadOnly DefaultColor As ConsoleColor = ConsoleColor.Gray
    Private ReadOnly BgColor As ConsoleColor = ConsoleColor.Black

    ' Certificate Capture
    Private LastCapturedCert As X509Certificate2 = Nothing

    Sub Main()
        ' Enable TLS 1.2 (3072) - Essential for modern sites
        Try
            ServicePointManager.SecurityProtocol = DirectCast(3072, SecurityProtocolType)
        Catch
        End Try

        ' Capture the certificate during handshake
        ServicePointManager.ServerCertificateValidationCallback = Function(s, c, ch, e)
                                                                      If c IsNot Nothing Then
                                                                          LastCapturedCert = New X509Certificate2(c)
                                                                      End If
                                                                      Return True
                                                                  End Function

        SetupConsole()
        
        Dim filePath As String = "siteler.txt"
        If Not File.Exists(filePath) Then
            Console.ForegroundColor = ErrorColor
            Console.WriteLine("HATA: siteler.txt bulunamadı!")
            Console.ReadKey()
            Return
        End If

        Dim sites = File.ReadAllLines(filePath)
        Dim cleanSites As New List(Of String)
        For Each s In sites
             If Not String.IsNullOrWhiteSpace(s) Then cleanSites.Add(s.Trim())
        Next

        PrintHeader()

        Dim allResults As New List(Of CheckResult)

        For i As Integer = 0 To cleanSites.Count - 1
            Dim site = cleanSites(i)
            
            ' Process and collect result
            Dim result = ProcessSite(site)
            result.SiteName = site
            allResults.Add(result)

            Console.WriteLine() ' Spacer

            ' Increased Delay: 1.5 seconds to read
            Thread.Sleep(1500)

            ' Clear every 3 items (but not after the last one)
            If (i + 1) Mod 3 = 0 Then
                If (i + 1) < cleanSites.Count Then
                     Console.WriteLine("    Gozetleme suruyor, ekran temizleniyor...")
                     Thread.Sleep(1000)
                     Console.Clear()
                     PrintHeader()
                End If
            End If
        Next

        PrintFinalReport(allResults)
    End Sub

    Sub SetupConsole()
        Console.Title = "DIJITAL GOZETLEME KULESI - DEEP SCAN"
        Console.BackgroundColor = BgColor
        Console.ForegroundColor = DefaultColor
        Console.Clear()
    End Sub

    Sub PrintHeader()
        Console.ForegroundColor = InfoColor
        Console.WriteLine("    DIJITAL GOZETLEME KULESI [DEEP DIVE MODE]")
        Console.WriteLine("    " & new String("="c, 40))
        Console.WriteLine()
        Console.ForegroundColor = DefaultColor
    End Sub

    Function ProcessSite(originalSite As String) As CheckResult
        ' Detailed Block Layout
        ' [ SPINNER ] google.com
        
        Console.ForegroundColor = ConsoleColor.White
        Console.Write("[")
        Dim spinnerPos As Integer = Console.CursorLeft
        Console.Write("   ") ' Space for spinner
        Console.Write("] ")
        Console.ForegroundColor = ConsoleColor.DarkYellow ' Turuncu
        Console.Write(originalSite.ToUpper())
        Console.WriteLine()
        
        Dim cts As New CancellationTokenSource()
        Dim spinnerTask = Task.Factory.StartNew(Sub() RunSpinner(spinnerPos, cts.Token))

        Dim result As New CheckResult()

        Try
            Dim host As String = GetHostFromUrl(originalSite)
            Dim urlToCheck As String = originalSite
            If Not urlToCheck.StartsWith("http") Then urlToCheck = "https://" & urlToCheck

            ' Reset captured cert
            LastCapturedCert = Nothing

            ' 1. DNS Resolution (IPs)
            Try
                Dim ipEntry As IPHostEntry = Dns.GetHostEntry(host)
                Dim ipList As New List(Of String)
                For Each ip In ipEntry.AddressList
                    ipList.Add(ip.ToString())
                    If ipList.Count >= 3 Then Exit For ' Limit to 3 IPs
                Next
                result.IPAddress = String.Join(", ", ipList.ToArray())
            Catch ex As Exception
                result.IPAddress = "DNS Failed: " & ex.Message
                Throw ex 
            End Try

            ' 2. HTTP Check
            Dim sw As New Stopwatch()
            sw.Start()

            Dim request As HttpWebRequest = DirectCast(WebRequest.Create(urlToCheck), HttpWebRequest)
            request.Method = "GET"
            request.Timeout = 15000
            ' Browser Headers
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"
            request.Headers.Add("Accept-Language", "en-US,en;q=0.9")
            request.AllowAutoRedirect = True
            
            Using response As HttpWebResponse = DirectCast(request.GetResponse(), HttpWebResponse)
                sw.Stop()
                result.LatencyMs = sw.ElapsedMilliseconds
                result.StatusCode = CInt(response.StatusCode)
                result.StatusDescription = response.StatusDescription

                ' Fallback SSL: Check captured variable first, then service point
                If LastCapturedCert IsNot Nothing Then
                     UpdateCertDetails(result, LastCapturedCert)
                ElseIf request.ServicePoint.Certificate IsNot Nothing Then
                     Dim cert As X509Certificate2 = New X509Certificate2(request.ServicePoint.Certificate)
                     UpdateCertDetails(result, cert)
                End If

                If result.StatusCode >= 400 Then Throw New Exception("HTTP " & result.StatusCode)
            End Using

            ' 3. Deep SSL Inspection (Better Cipher Info)
            If urlToCheck.StartsWith("https") Then
                Try
                    Using client As New TcpClient()
                        client.Connect(host, 443)
                        Using sslStream As New SslStream(client.GetStream(), False, Function(s, c, ch, e) True)
                            sslStream.AuthenticateAsClient(host)
                            ' If we got here, handshake worked
                            result.SslProtocol = sslStream.SslProtocol.ToString()
                            result.CipherAlgorithm = sslStream.CipherAlgorithm.ToString()
                            result.HashAlgorithm = sslStream.HashAlgorithm.ToString()
                            
                            If sslStream.RemoteCertificate IsNot Nothing Then
                                Dim cert As X509Certificate2 = New X509Certificate2(sslStream.RemoteCertificate)
                                UpdateCertDetails(result, cert) ' Overwrite with fresh info from stream
                            End If
                        End Using
                    End Using
                Catch
                    ' Deep probe failed, but if we already have info from HTTP request, we are good.
                End Try
            End If

            ' 4. Domain Expiry Check (WHOIS)
            GetDomainInfo(host, result)

            result.Success = True
            
        Catch ex As WebException
             If ex.Response IsNot Nothing Then
                 Dim resp As HttpWebResponse = DirectCast(ex.Response, HttpWebResponse)
                 result.StatusCode = CInt(resp.StatusCode)
                 If result.StatusCode = 403 Then
                    result.FailureReason = "Erisim Reddedildi (403 Bot Korumasi)"
                 Else
                    result.FailureReason = "HTTP " & result.StatusCode
                 End If
             Else
                 result.FailureReason = ex.Message
             End If
             result.Success = False
        Catch ex As Exception
            result.FailureReason = ex.Message
            result.Success = False
        End Try

        ' Stop Spinner
        cts.Cancel()
        Try
             spinnerTask.Wait()
        Catch
        End Try

        ' Update Status Block
        Console.SetCursorPosition(spinnerPos, Console.CursorTop - 1) ' Go back to spinner line
        If result.Success Then
            Console.ForegroundColor = SuccessColor
            Console.Write(" OK")
        Else
            Console.ForegroundColor = ErrorColor
            Console.Write("FAIL")
        End If
        Console.CursorTop += 1 ' Move back down
        Console.SetCursorPosition(0, Console.CursorTop)

        ' Print Stats Block
        If result.Success Then
            PrintDetail("IP ADRES", result.IPAddress)
            PrintDetail("YANIT", result.StatusCode & " " & result.StatusDescription & " (" & result.LatencyMs & "ms)")

            ' SSL Info Print
            If result.SslDaysLeft > -999 OrElse Not String.IsNullOrEmpty(result.SslProtocol) Then
                Console.ForegroundColor = InfoColor
                Console.Write("      SSL       : ")
                Console.ForegroundColor = ConsoleColor.White
                If String.IsNullOrEmpty(result.SslProtocol) Then result.SslProtocol = "TLS (Web)"
                Console.WriteLine("{0} | {1}", result.SslProtocol, result.CipherAlgorithm)
                
                PrintDetail("SERTIFIKA", result.SslIssuer)
                
                Console.ForegroundColor = LabelColor
                Console.Write("      SSL BITIS : ")
                If result.SslDaysLeft < 0 Then
                    Console.ForegroundColor = ErrorColor
                    Console.WriteLine("GECTI! ACIL MUDAHALE GEREKIYOR ({0})", result.SslExpiryDate)
                ElseIf result.SslDaysLeft < 15 Then
                    Console.ForegroundColor = WarningColor
                    Console.WriteLine("{0} Gun Kaldi ({1})", result.SslDaysLeft, result.SslExpiryDate)
                Else 
                    Console.ForegroundColor = SuccessColor
                    Console.WriteLine("{0} Gun Kaldi ({1})", result.SslDaysLeft, result.SslExpiryDate)
                End If
            End If

            ' DOMAIN Info Print
            Console.ForegroundColor = LabelColor
            Console.Write("      ALAN ADI  : ")
            If result.DomainDaysLeft > -999 Then
                 If result.DomainDaysLeft < 0 Then
                    Console.ForegroundColor = ErrorColor
                    Console.WriteLine("GECTI! ACIL MUDAHALE GEREKIYOR ({0})", result.DomainExpiryDate)
                 ElseIf result.DomainDaysLeft < 30 Then
                    Console.ForegroundColor = WarningColor
                    Console.WriteLine("{0} Gun Kaldi ({1})", result.DomainDaysLeft, result.DomainExpiryDate)
                 Else 
                    Console.ForegroundColor = SuccessColor
                    Console.WriteLine("{0} Gun Kaldi ({1})", result.DomainDaysLeft, result.DomainExpiryDate)
                 End If
            Else
                 Console.ForegroundColor = DefaultColor
                 Console.WriteLine("- (Bilgi Alınamadı)")
            End If

        Else
            PrintDetail("HATA", result.FailureReason)
        End If

        Return result
    End Function

    Sub UpdateCertDetails(result As CheckResult, cert As X509Certificate2)
        result.SslIssuer = cert.Issuer
        result.SslDaysLeft = CInt((cert.NotAfter - DateTime.Now).TotalDays)
        result.SslExpiryDate = cert.NotAfter.ToString("yyyy-MM-dd")
        
        Dim issuer As String = result.SslIssuer
        Dim cnStart = issuer.IndexOf("CN=")
        If cnStart >= 0 Then
                Dim cnEnd = issuer.IndexOf(",", cnStart)
                If cnEnd = -1 Then cnEnd = issuer.Length
                result.SslIssuer = issuer.Substring(cnStart + 3, cnEnd - cnStart - 3)
        Else
                Dim oStart = issuer.IndexOf("O=")
                If oStart >= 0 Then
                    Dim oEnd = issuer.IndexOf(",", oStart)
                    If oEnd = -1 Then oEnd = issuer.Length
                    result.SslIssuer = issuer.Substring(oStart + 2, oEnd - oStart - 2)
                End If
        End If
    End Sub

    Sub GetDomainInfo(host As String, result As CheckResult)
        result.DomainDaysLeft = -999 ' Not Checked/Found
        Try
            Dim tld As String = host.Substring(host.LastIndexOf("."))
            Dim whoisServer As String = "whois.verisign-grs.com" ' Default for com/net
            
            ' Determine TLD Server
            If host.EndsWith(".tr") Then
                whoisServer = "whois.metu.edu.tr"
            ElseIf host.EndsWith(".org") Then
                whoisServer = "whois.pir.org"
            ElseIf host.EndsWith(".info") Then
                whoisServer = "whois.afilias.net"
            End If
            
            ' Connect Port 43
            Using client As New TcpClient()
                client.Connect(whoisServer, 43)
                client.ReceiveTimeout = 5000 ' 5s timeout
                client.SendTimeout = 5000
                Using ns As NetworkStream = client.GetStream()
                    Using reader As New StreamReader(ns, Encoding.ASCII)
                        Using writer As New StreamWriter(ns, Encoding.ASCII)
                            writer.WriteLine(host)
                            writer.Flush()
                            Dim response As String = reader.ReadToEnd()
                            
                            ' Regex Parsing
                            Dim dateStr As String = ""
                            Dim patterns As String() = {
                                "Registry Expiry Date:\s*([0-9]{4}-[0-9]{2}-[0-9]{2})",
                                "Expiration Date:\s*([0-9]{4}-[0-9]{2}-[0-9]{2})",
                                "Expires\s*:\s*([0-9]{4}-[0-9]{2}-[0-9]{2})",
                                "Valid Until\s*:\s*([0-9]{4}-[0-9]{2}-[0-9]{2})"
                            }

                            For Each p In patterns 
                                Dim m As Match = Regex.Match(response, p, RegexOptions.IgnoreCase)
                                If m.Success Then 
                                    dateStr = m.Groups(1).Value
                                    Exit For
                                End If
                            Next

                             If Not String.IsNullOrEmpty(dateStr) Then
                                 Dim expDate As DateTime
                                 If DateTime.TryParse(dateStr, expDate) Then
                                     result.DomainExpiryDate = expDate.ToString("yyyy-MM-dd")
                                     result.DomainDaysLeft = CInt((expDate - DateTime.Now).TotalDays)
                                 End If
                             End If
                        End Using
                    End Using
                End Using
            End Using
        Catch
            ' Valid failure, ignore
        End Try
    End Sub

    Sub PrintDetail(label As String, value As String)
        Console.ForegroundColor = LabelColor
        Console.Write("      {0,-10}: ", label)
        Console.ForegroundColor = DefaultColor
        Console.WriteLine(value)
    End Sub

    Sub RunSpinner(leftPos As Integer, token As CancellationToken)
        Dim spinnerChars() As Char = {"|"c, "/"c, "-"c, "\"c}
        Dim ctr As Integer = 0
        Dim originalTop As Integer = Console.CursorTop
        While Not token.IsCancellationRequested
            Try
                Console.SetCursorPosition(leftPos, originalTop - 1) ' Safe check?
                Console.ForegroundColor = InfoColor
                Console.Write(" " & spinnerChars(ctr Mod 4) & " ")
                ctr += 1
                Thread.Sleep(80)
            Catch
            End Try
        End While
    End Sub

    Function GetHostFromUrl(url As String) As String
        Dim u As String = url
        If Not u.StartsWith("http") Then u = "http://" & u 
        Dim uri As New Uri(u)
        Return uri.Host
    End Function

    Sub PrintFinalReport(results As List(Of CheckResult))
        Console.Clear()
        PrintHeader()
        
        Console.WriteLine("    DETAYLI SONUC RAPORU")
        Console.WriteLine("    " & new String("-"c, 110))
        ' Adjusted columns: SITE | STATUS | SSL KALAN | DOMAIN KALAN | PROVIDER
        Console.WriteLine("    {0,-22} {1,-8} {2,-15} {3,-23} {4,-25} {5}", "SITE", "DURUM", "SSL KALAN", "DOMAIN BITIS", "SERTIFIKA", "IP")
        Console.WriteLine("    " & new String("-"c, 110))

        Dim working As Integer = 0
        Dim failed As Integer = 0

        For Each r In results
            Dim displayName As String = r.SiteName
            If displayName.Length > 21 Then displayName = displayName.Substring(0, 18) & "..."
            
            Console.Write("    {0,-22}", displayName.ToUpper())
            
            If r.Success Then
                working += 1
                Console.ForegroundColor = SuccessColor
                Console.Write("{0,-8}", "[OK]")
                Console.ForegroundColor = DefaultColor
                
                ' SSL Expiry
                If r.SslDaysLeft > -999 Then
                    If r.SslDaysLeft < 0 Then
                         Console.ForegroundColor = ErrorColor
                         Console.Write("{0,-15}", "GECTI! ACIL")
                    ElseIf r.SslDaysLeft < 15 Then 
                         Console.ForegroundColor = WarningColor 
                         Console.Write("{0,-15}", r.SslDaysLeft & " Gun")
                    Else 
                         Console.ForegroundColor = SuccessColor
                         Console.Write("{0,-15}", r.SslDaysLeft & " Gun")
                    End If
                Else
                    Console.ForegroundColor = DefaultColor
                    Console.Write("{0,-15}", "-")
                End If
                Console.ForegroundColor = DefaultColor

                ' DOMAIN Expiry
                If r.DomainDaysLeft > -999 Then
                    Dim domainStr As String = r.DomainExpiryDate & " (" & r.DomainDaysLeft & "g)"
                    If r.DomainDaysLeft < 0 Then
                        Console.ForegroundColor = ErrorColor
                        Console.Write("{0,-23}", "GECTI! (" & r.DomainExpiryDate & ")")
                    ElseIf r.DomainDaysLeft < 30 Then 
                        Console.ForegroundColor = WarningColor 
                        Console.Write("{0,-23}", domainStr)
                    Else 
                        Console.ForegroundColor = SuccessColor
                        Console.Write("{0,-23}", domainStr)
                    End If
                Else
                    Console.ForegroundColor = DefaultColor
                    Console.Write("{0,-23}", "-")
                End If
                Console.ForegroundColor = DefaultColor

                ' Issuer
                Dim shortIssuer As String = r.SslIssuer
                If String.IsNullOrEmpty(shortIssuer) Then shortIssuer = "-"
                If shortIssuer.Length > 24 Then shortIssuer = shortIssuer.Substring(0, 21) & "..."
                Console.Write("{0,-25}", shortIssuer)

                Console.WriteLine(r.IPAddress)
            Else
                failed += 1
                Console.ForegroundColor = ErrorColor
                Console.Write("{0,-8}", "[FAIL]")
                Console.ForegroundColor = DefaultColor
                Console.Write("{0,-15}", "-")
                Console.Write("{0,-23}", "-")
                Console.Write("{0,-25}", "-")
                Console.ForegroundColor = ErrorColor
                Console.WriteLine(r.FailureReason)
            End If
            Console.ForegroundColor = DefaultColor
        Next
        
        Console.WriteLine()
        Console.WriteLine("    " & new String("="c, 40))
        Console.WriteLine("    TOPLAM: {0} | CALISAN: {1} | HATALI: {2}", results.Count, working, failed)
        
        Console.WriteLine()
        Console.WriteLine("Cikis icin bir tusa basin...")
        Console.ReadKey()
    End Sub

    Class CheckResult
        Public Property SiteName As String
        Public Property Success As Boolean
        Public Property StatusCode As Integer
        Public Property StatusDescription As String
        Public Property LatencyMs As Long
        Public Property IPAddress As String
        Public Property FailureReason As String
        
        Public Property SslProtocol As String
        Public Property CipherAlgorithm As String
        Public Property HashAlgorithm As String
        
        Public Property SslIssuer As String
        Public Property SslDaysLeft As Integer
        Public Property SslExpiryDate As String
        
        Public Property DomainDaysLeft As Integer
        Public Property DomainExpiryDate As String
    End Class
End Module
