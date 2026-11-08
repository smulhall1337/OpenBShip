param([Parameter(Mandatory=$true)][uri]$Uri,[Parameter(Mandatory=$true)][string]$OutFile)
$ErrorActionPreference='Stop'
# Use Git's existing OpenSSL and CA bundle because this agent's Schannel token
# cannot acquire TLS credentials. Certificate chain and hostname checks stay on.
$tls='C:\Program Files\Git\ucrt64\bin\openssl.exe'
$ca='C:\Program Files\Git\usr\ssl\cert.pem'
for($redirect=0;$redirect -lt 6;$redirect++) {
    if($Uri.Scheme -ne 'https' -or $Uri.Port -ne 443){throw 'Only HTTPS on port 443 is supported'}
    if($Uri.Host -notin @('api.github.com','github.com','release-assets.githubusercontent.com','objects.githubusercontent.com','raw.githubusercontent.com','api.nuget.org','www.nuget.org','globalcdn.nuget.org')){throw 'Unexpected download host'}
    $start=New-Object Diagnostics.ProcessStartInfo
    $start.FileName=$tls
    $start.Arguments='s_client -quiet -connect '+$Uri.Host+':443 -servername '+$Uri.Host+' -verify_return_error -verify_hostname '+$Uri.Host+' -CAfile "'+$ca+'"'
    $start.UseShellExecute=$false
    $start.CreateNoWindow=$true
    $start.RedirectStandardInput=$true
    $start.RedirectStandardOutput=$true
    $start.RedirectStandardError=$true
    $proc=New-Object Diagnostics.Process
    $proc.StartInfo=$start
    $memory=New-Object IO.MemoryStream
    try {
        [void]$proc.Start()
        $stderr=$proc.StandardError.ReadToEndAsync()
        $copy=$proc.StandardOutput.BaseStream.CopyToAsync($memory)
        $proc.StandardInput.Write("GET "+$Uri.PathAndQuery+" HTTP/1.0`r`nHost: "+$Uri.Host+"`r`nUser-Agent: Battleship-reference-inspection`r`nAccept-Encoding: identity`r`nConnection: close`r`n`r`n")
        $proc.StandardInput.Close()
        if(!$proc.WaitForExit(25000)){$proc.Kill();throw 'TLS download timed out'}
        [void]$copy.GetAwaiter().GetResult()
        $errorText=$stderr.GetAwaiter().GetResult()
        if($proc.ExitCode -ne 0){throw ('TLS download failed: '+$errorText)}
        $raw=$memory.ToArray()
    } finally {$memory.Dispose();$proc.Dispose()}
    $split=-1
    for($i=0;$i -lt $raw.Length-3;$i++){if($raw[$i] -eq 13 -and $raw[$i+1] -eq 10 -and $raw[$i+2] -eq 13 -and $raw[$i+3] -eq 10){$split=$i;break}}
    if($split -lt 0){throw 'HTTP response has no header terminator'}
    $header=[Text.Encoding]::ASCII.GetString($raw,0,$split)
    if($header -notmatch '^HTTP/1\.[01] (\d{3})'){throw 'Unexpected HTTP status line'}
    $status=[int]$Matches[1]
    if($status -in 301,302,303,307,308){if($header -notmatch '(?im)^Location:\s*(.+)$'){throw 'Redirect lacks location'};$Uri=[uri]::new($Uri,$Matches[1].Trim());continue}
    if($status -ne 200){throw ('Download HTTP status '+$status)}
    if($header -match '(?im)^Transfer-Encoding:\s*chunked'){throw 'Chunked HTTP responses are not supported'}
    $size=$raw.Length-$split-4
    if($header -match '(?im)^Content-Length:\s*(\d+)'){if([long]$Matches[1] -ne $size){throw 'HTTP content length mismatch'}}
    $body=[byte[]]::new($size)
    [Array]::Copy($raw,$split+4,$body,0,$size)
    [IO.File]::WriteAllBytes([IO.Path]::GetFullPath($OutFile),$body)
    Write-Output ('Downloaded '+$size+' bytes from '+$Uri.Host+' with certificate verification.')
    return
}
throw 'Too many redirects'
