Get-ChildItem -Path .\Models -Filter *.cs | ForEach-Object {
    $modelName = $_.BaseName
    dotnet aspnet-codegenerator controller `
        -name "${modelName}Controller" `
        -m $modelName `
        -dc ApplicationDbContext `
        --relativeFolderPath Controllers `
        --useDefaultLayout `
        --referenceScriptLibraries
}