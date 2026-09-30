param([string]$Repository='moonlit-typing', [switch]$EnablePages)
$ErrorActionPreference='Stop'
$env:GIT_TERMINAL_PROMPT='0'
$requestText="protocol=https`nhost=github.com`n`n"
$credentialLines=$requestText | git credential fill
if($LASTEXITCODE -ne 0){throw 'GitHub 로그인 인증을 읽지 못했습니다.'}
$credentialMap=@{}
foreach($line in $credentialLines){$pair=$line -split '=',2;if($pair.Length -eq 2){$credentialMap[$pair[0]]=$pair[1]}}
$headers=@{Authorization=('Bearer '+$credentialMap['password']);Accept='application/vnd.github+json';'User-Agent'='MoonlitPress';'X-GitHub-Api-Version'='2022-11-28'}
$account=Invoke-RestMethod -Uri 'https://api.github.com/user' -Headers $headers
$owner=$account.login
if($EnablePages){
 $body=@{source=@{branch='main';path='/docs'}} | ConvertTo-Json -Depth 3
 $result=Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$Repository/pages" -Method Post -Headers $headers -ContentType 'application/json' -Body $body
 Write-Output $result.html_url
}else{
 $body=@{name=$Repository;description='Moonlit Press — a Unity 6 Korean rhythm typing game in a moonlit study';private=$false;auto_init=$false} | ConvertTo-Json
 $repo=Invoke-RestMethod -Uri 'https://api.github.com/user/repos' -Method Post -Headers $headers -ContentType 'application/json' -Body $body
 Write-Output $repo.html_url
}
