# Script pour mettre à jour le dossier publish
Write-Host "Mise à jour du dossier publish..." -ForegroundColor Green

# Attendre que l'application soit fermée
$process = Get-Process -Name "DODtransfert.Client" -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "L'application est en cours d'exécution. Veuillez la fermer d'abord." -ForegroundColor Yellow
    Write-Host "Appuyez sur une touche une fois l'application fermée..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

# Supprimer l'ancien dossier publish
if (Test-Path "publish") {
    Write-Host "Suppression de l'ancien dossier publish..." -ForegroundColor Yellow
    Remove-Item -Path "publish" -Recurse -Force
}

# Renommer le nouveau dossier
Rename-Item -Path "publish-new" -NewName "publish"

Write-Host "Mise à jour terminée !" -ForegroundColor Green
Write-Host "Le nouveau dossier publish est prêt dans: F:\DODtransfert\publish" -ForegroundColor Green
