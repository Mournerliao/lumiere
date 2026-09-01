!macro customInstall
  IfFileExists "$INSTDIR\resources\windows-identity\Lumiere.Identity.msix" 0 identity_done
  DetailPrint "Registering Lumiere Windows identity"
  nsExec::ExecToLog 'powershell.exe -NoLogo -NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File "$INSTDIR\resources\windows-installer\register-identity.ps1" -InstallDirectory "$INSTDIR"'
  Pop $0
  ${If} $0 != 0
    DetailPrint "Windows identity registration failed; Lumiere will retain the system capture border."
  ${EndIf}
  identity_done:
!macroend

!macro customUnInstall
  ${IfNot} ${isUpdated}
    DetailPrint "Removing Lumiere Windows identity"
    nsExec::ExecToLog 'powershell.exe -NoLogo -NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File "$INSTDIR\resources\windows-installer\unregister-identity.ps1"'
    Pop $0
  ${EndIf}
!macroend
