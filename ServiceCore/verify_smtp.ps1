$smtpServer = "smtp.gmail.com"
$smtpPort = 587
$username = "technano582@gmail.com"
$password = "kkgotbedmrptyuqg"
$to = "technano582@gmail.com" # Sending to self to test
$from = "technano582@gmail.com"
$subject = "SMTP Test from PowerShell"
$body = "This is a test email sent from PowerShell to verify SMTP settings."

$message = New-Object System.Net.Mail.MailMessage $from, $to
$message.Subject = $subject
$message.Body = $body

$smtp = New-Object System.Net.Mail.SmtpClient($smtpServer, $smtpPort)
$smtp.EnableSsl = $true
$smtp.Credentials = New-Object System.Net.NetworkCredential($username, $password)

try {
    Write-Host "Sending test email to $to..."
    $smtp.Send($message)
    Write-Host "Email sent successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Failed to send email:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.Exception.InnerException) {
        Write-Host "Inner Exception: $($_.Exception.InnerException.Message)" -ForegroundColor Red
    }
}
