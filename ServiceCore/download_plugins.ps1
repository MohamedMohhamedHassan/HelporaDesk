$plugins = @("advlist", "autolink", "lists", "link", "image", "charmap", "preview", "anchor", "searchreplace", "visualblocks", "code", "fullscreen", "insertdatetime", "media", "table", "help", "wordcount")
$baseUrl = "https://cdnjs.cloudflare.com/ajax/libs/tinymce/6.8.2/plugins"
$targetDir = "D:\ASU_IT_End\Projects\ServiceCore\ServiceCore\wwwroot\lib\tinymce\plugins"

foreach ($plugin in $plugins) {
    $pluginDir = Join-Path $targetDir $plugin
    if (-not (Test-Path $pluginDir)) {
        New-Item -ItemType Directory -Path $pluginDir | Out-Null
    }
    
    $url = "$baseUrl/$plugin/plugin.min.js"
    $output = Join-Path $pluginDir "plugin.min.js"
    
    Write-Host "Downloading $plugin..."
    try {
        Invoke-WebRequest -Uri $url -OutFile $output
    }
    catch {
        Write-Host "Failed to download $plugin from $url" -ForegroundColor Red
    }
}
