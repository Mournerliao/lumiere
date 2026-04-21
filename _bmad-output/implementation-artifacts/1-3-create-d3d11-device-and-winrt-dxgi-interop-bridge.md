# Story 1.3: Create D3D11 Device and WinRT/DXGI Interop Bridge

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

浣滀负寮€鍙戣€咃紝
鎴戝笇鏈涙湁涓€涓獎鎺ュ彛鐨?Direct3D銆丏XGI銆乄inRT 鍜?COM 浜掓搷浣滄ˉ锛?浠ヤ究鎹曡幏鍜屾覆鏌撲唬鐮佸彲浠ュ叡浜?GPU 璧勬簮锛屽悓鏃朵笉鎶婂師鐢熷疄鐜扮粏鑺傛硠婕忓埌 UI 浠ｇ爜涓€?
## Acceptance Criteria

1. Given 鍥惧舰鍩虹璁炬柦寮€濮嬪垵濮嬪寲锛寃hen 鍒涘缓 D3D11 device provider锛宼hen 瀹冧細鍒涘缓閫傚悎 WGC 鍜?DXGI swap-chain 娓叉煋鐨?device/context銆?2. Given WGC 闇€瑕?WinRT Direct3D device锛寃hen 浜掓搷浣滄ˉ鍖呰 DXGI device锛宼hen 瀹冧細閫氳繃绐勬帴鍙ｈ繑鍥?WinRT 鍏煎鐨?Direct3D device銆?3. Given 浜掓搷浣滆皟鐢ㄥけ璐ワ紝when 鍙戠敓 HRESULT 鎴?COM 澶辫触锛宼hen 璇婃柇淇℃伅鍖呭惈 operation name銆乻tage 鍜?technical detail銆?
## Tasks / Subtasks

- [x] 纭 Story 1.1 鍜?Story 1.2 鍓嶇疆鏉′欢銆?(AC: 1, 2, 3)
  - [x] 纭 `Lumiere.sln`銆乣Directory.Build.props`銆乣Directory.Packages.props`銆乣src/Lumiere.Graphics/`銆乣src/Lumiere.Infrastructure/` 鍜?`tests/Lumiere.Graphics.Tests/` 宸插瓨鍦ㄣ€?  - [x] 纭褰撳墠 target/runtime 浠嶆槸 `net10.0-windows10.0.19041.0`銆乣x64` 鍜?`win-x64`銆?  - [x] 澶嶇敤 `src/Lumiere.Graphics/Hdr/HdrConstants.cs` 涓?`PreviewReadinessStatus`锛涗笉瑕侀噸鏂板畾涔?HDR 甯搁噺鎴?readiness state銆?- [x] 鍦?graphics 杈圭晫鍐呮坊鍔?D3D11 device provider銆?(AC: 1)
  - [x] 鍒涘缓 `src/Lumiere.Graphics/Devices/GraphicsDeviceProvider.cs`锛屾垨鏀惧湪 `Lumiere.Graphics` 鍐呯瓑浠蜂笖鏇寸鍚堢幇鏈夌粨鏋勭殑浣嶇疆銆?  - [x] 浣跨敤 Vortice API 鍒涘缓 Direct3D 11 device/context锛屼笉瑕佹妸闆舵暎 P/Invoke 鏀惧埌 UI 鎴?capture 浠ｇ爜涓€?  - [x] 浣跨敤 BGRA support锛屽苟浣跨敤閫傚悎 WinUI/DXGI presentation 涓?WGC interop 鐨勬湁搴?feature-level 鍒楄〃銆?  - [x] 閫氳繃绐勭被鍨嬫ā鍨嬫毚闇插垱寤哄嚭鐨?`ID3D11Device`銆乮mmediate context銆乻elected feature level 鍜屽簳灞?`IDXGIDevice`/DXGI access銆?  - [x] 瀵规墍鏈夋嫢鏈夌殑 native 瀵硅薄瀹炵幇纭畾鎬ч噴鏀俱€?- [x] 鍦?infrastructure 杈圭晫鍐呮坊鍔?WinRT/DXGI 浜掓搷浣滃熀纭€璁炬柦銆?(AC: 2, 3)
  - [x] 鍦?`src/Lumiere.Infrastructure/Interop/` 涓嬪垱寤轰簰鎿嶄綔鏂囦欢锛屼緥濡?`Direct3D11Interop.cs`銆乣DxgiInterfaceAccess.cs`锛屾垨鍏朵粬鑱岃矗鏇存槑纭殑绛変环鍛藉悕銆?  - [x] 閫氳繃 `CreateDirect3D11DeviceFromDXGIDevice`锛屾垨绛変环涓斿彈鏀寔鐨?CsWinRT/WinRT interop 璺緞锛屾妸 `IDXGIDevice` 鍖呰涓?`Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice`銆?  - [x] 鍙湁鍦ㄧ‘瀹為渶瑕佹椂鎵嶆彁渚涘弽鍚戞ˉ鎺ユ垨璁块棶 helper锛沗IDirect3DDxgiInterfaceAccess.GetInterface` 蹇呴』闅旂鍦?infrastructure API 鍚庨潰銆?  - [x] 杩斿洖 typed result锛屾垨鎶涘嚭 typed interop exception锛涘け璐ヤ俊鎭繀椤诲寘鍚?operation name銆乣PreviewReadinessStage.Interop` 鎴?`PreviewReadinessStage.Graphics`銆丠RESULT/native detail锛屼互鍙婄畝鐭殑鐢ㄦ埛鍙娑堟伅銆?- [x] 鎺ュ叆 diagnostics/readiness 璇箟锛屼絾涓嶅疄鐜板悗缁?preview 鍔熻兘銆?(AC: 1, 3)
  - [x] 鍙互鎶?device 鍒涘缓鎴愬姛璁板綍涓?graphics initialization evidence锛屼絾闄ら潪鏄惧紡楠岃瘉浜嗘墍鏈夊繀瑕佹潯浠讹紝鍚﹀垯涓嶈鎶?preview 鏍囦负 `Ready`銆?  - [x] 灏?device creation 鎴?WinRT wrapping 澶辫触鏄犲皠鍒?`PreviewReadinessStatus.Failed(...)` 鎴?`Unsupported(...)`锛屽苟鎼哄甫 stage 鍜?technical detail銆?  - [x] 鏈?story 涓嶅垱寤?WGC frame pool銆乻wap chain銆乣SwapChainPanel` attachment銆乴ive frame rendering銆乪xport銆乧lipboard銆乭otkey銆乼ray銆乤nnotation 鎴?history銆?- [x] 娣诲姞鑱氱劍鐨?provider 涓?interop 娴嬭瘯銆?(AC: 1, 2, 3)
  - [x] 鍦?`tests/Lumiere.Graphics.Tests/Devices/` 涓嬫坊鍔?device-provider result/state 琛屼负娴嬭瘯锛屾祴璇曞簲鑳藉湪 CI 鎴栨湰鍦板紑鍙戠幆澧冪ǔ瀹氳繍琛屻€?  - [x] 鍙湁褰撳疄鐜板紩鍏ヤ簡鍙祴璇曠殑 infrastructure interop/result helper 鏃讹紝鎵嶆柊澧?`tests/Lumiere.Infrastructure.Tests/Interop/` 娴嬭瘯椤圭洰銆?  - [x] 娴嬭瘯澶辫触璺緞浼氫繚鐣?operation name銆乻tage 鍜?technical detail銆?  - [x] 涓嶈鎶婁緷璧栫湡瀹?HDR 纭欢鐨?live HDR 楠岃瘉浼鎴愯嚜鍔ㄥ寲娴嬭瘯锛涢渶瑕佷汉宸ラ獙璇佹椂搴旀槑纭褰曘€?- [x] 楠岃瘉瀹炵幇銆?(AC: 1, 2, 3)
  - [x] 杩愯 `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`銆?  - [x] 杩愯 `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`銆?  - [x] 杩愯 `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`銆?  - [x] 杩愯 `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`銆?  - [x] 妫€鏌ユ渶缁?diff锛屽苟鏄庣‘璇存槑 touched 鐨?native interop銆乨isposal 鎴?HDR-readiness 璇箟銆?
## Dev Notes

### Story Scope

鏈?story 寤虹珛鍚庣画 WGC 鍜?swap-chain story 鎵€闇€鐨?GPU device 涓?WinRT/DXGI bridge銆傝繖閲屾槸 C# 鎵樼浠ｇ爜涓?Direct3D/DXGI/WinRT 鍘熺敓瀵硅薄涔嬮棿鐨勫叧閿竟鐣岋紝鍥犳瀹炵幇蹇呴』鏄庣‘璧勬簮鎵€鏈夋潈銆佸け璐ユ姤鍛婂拰閲婃斁璺緞銆?
涓嶈鍦ㄦ湰 story 瀹炵幇 Story 1.4 鐨?swap-chain attachment锛屼篃涓嶈瀹炵幇 Story 1.5 鐨?WGC frame-pool/live preview銆傛湰 story 鐨勪骇鍑哄簲鏄彲澶嶇敤鍩虹璁炬柦锛欴3D11 device/context provider锛屼互鍙婂彲浠ヤ负 WGC 浜у嚭 WinRT `IDirect3DDevice` 鐨勭獎浜掓搷浣滄ˉ銆?[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\epics.md#Story 1.3: Create D3D11 Device and WinRT/DXGI Interop Bridge`; `D:\UGit\lumiere\_bmad-output\planning-artifacts\architecture.md#Decision Impact Analysis`]

### Current Repository Context

褰撳墠宸ヤ綔鍖哄凡缁忔湁 Story 1.1 鐨?scaffold 鍜?Story 1.2 鐨?HDR guardrails锛?
- 涓ぎ鍖呯増鏈湪 `Directory.Packages.props`銆?- Target framework/platform 榛樿鍊煎湪 `Directory.Build.props`銆?- `Lumiere.Graphics` 宸插紩鐢?`Vortice.Direct3D11`銆乣Vortice.DXGI` 鍜?`Lumiere.Infrastructure`銆?- `src/Lumiere.Graphics/Hdr/HdrConstants.cs` 宸叉毚闇?`DirectXPixelFormat.R16G16B16A16Float`銆乣Format.R16G16B16A16_Float` 鍜?`ColorSpaceType.RgbFullG10NoneP709`銆?- `src/Lumiere.Graphics/Hdr/PreviewReadinessStatus.cs` 宸插缓妯?initializing銆乺eady銆乨egraded銆乽nsupported 鍜?failed锛屽苟甯︽湁 stage/detail 瀛楁銆?- 鐜版湁鑷姩鍖栨祴璇曚娇鐢?xUnit锛屼綅浜?`tests/Lumiere.Graphics.Tests/`銆?
宸ヤ綔鍖哄瓨鍦ㄥ墠搴忓伐浣滅暀涓嬬殑鏈彁浜ゅ彉鏇淬€備笉瑕佸洖婊氭棤鍏虫枃浠讹紱鍦ㄥ綋鍓?source layout 涓婄户缁疄鐜般€?[Source: local repository inspection on 2026-04-22; `D:\UGit\lumiere\_bmad-output\implementation-artifacts\1-2-centralize-hdr-constants-and-preview-readiness-status.md#File List`]

### Technical Requirements

- D3D11 device 鍒涘缓灞炰簬 `Lumiere.Graphics`锛沀I銆乷verlay 鍜?capture 浠ｇ爜涓嶅緱鐩存帴鍒涘缓 Direct3D device銆?[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\architecture.md#Executive Architecture Summary`]
- COM銆乄inRT銆丏XGI bridge code 蹇呴』鏀惧湪 `Lumiere.Infrastructure/Interop` 鐨勫皬鍨?API 鍚庨潰銆?[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\architecture.md#Structure Patterns`]
- Device/context 蹇呴』閫傚悎鍚庣画 WGC frame-pool 鍒涘缓鍜?DXGI swap-chain 娓叉煋锛涗笉瑕侀潰鍚?bitmap/GDI screenshot path 浼樺寲銆?[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\prd.md#Technical Success`; `D:\UGit\lumiere\_bmad-output\project-context.md#Critical Don't-Miss Rules`]
- 浠讳綍鎷ユ湁 D3D11銆丏XGI銆乄inRT銆丆OM銆乻wap-chain銆乼exture 鎴?frame-pool 璧勬簮鐨勭被閮藉繀椤诲疄鐜扮‘瀹氭€ч噴鏀俱€?[Source: `D:\UGit\lumiere\_bmad-output\project-context.md#Language-Specific Rules`]
- 棰勬湡鐨勫钩鍙板け璐ュ簲鍙樻垚鏄庣‘鐨?degraded/unsupported/failed status锛屾垨甯﹁瘖鏂殑 typed interop failure锛涗笉瑕侀潤榛樿繑鍥?`null` 鎴栧悶鎺?HRESULT銆?[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\architecture.md#Error Handling Standard`]

### Architecture Compliance

鏈?story 鐨勮竟鐣岃鍒欙細

- `Lumiere.Graphics` 鎷ユ湁 `GraphicsDeviceProvider`銆乨evice/context 鍒涘缓銆乫eature level 鍜?graphics-stage readiness reporting銆?- `Lumiere.Infrastructure` 鎷ユ湁 native interop declarations銆丆OM pointer conversion銆丠RESULT translation 鍜?`IDirect3DDevice` wrapping helpers銆?- `Lumiere.Capture` 鍚庣画鍙互娑堣垂 WinRT Direct3D device锛屼絾鏈?story 涓嶅垱寤?WGC capture session銆?- `Lumiere.Overlay` 鍜?`Lumiere.App` 搴斾繚鎸佷笉鍙橈紝闄ら潪缂栬瘧寮曠敤闇€瑕佹瀬灏忕殑 composition 璋冩暣銆?- `HdrConstants` 缁х画鏄?HDR pixel/format/color-space 鍊肩殑鍞竴鏉ユ簮锛涗笉瑕佸湪 interop 灞傛坊鍔犵珵浜夋€у父閲忋€?
瀹炵幇搴旈伒瀹?one primary type per file锛屽苟浣跨敤鑱岃矗鏄庣‘鐨勫悕绉帮紝渚嬪 `GraphicsDeviceProvider`銆乣Direct3D11Interop`銆乣DxgiDeviceInterop` 鎴?`NativeInteropException`銆傞伩鍏?`Helpers.cs` 杩欑被娉涘悕銆?[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\architecture.md#File Structure Patterns`]

### Library / Framework Requirements

浣跨敤宸茬粡鎵瑰噯鐨勫寘鐗堟湰锛?
- `Microsoft.WindowsAppSDK` `1.8.260317003`
- `Vortice.Direct3D11` `3.8.3`
- `Vortice.DXGI` `3.8.3`
- 浠呭綋鏈簰鎿嶄綔瀹炵幇纭疄闇€瑕佹椂锛屼娇鐢?`Microsoft.Windows.CsWinRT` `2.2.0`

2026-04-22 鐨勬渶鏂版妧鏈牳鏌ワ細

- Microsoft Learn 璁板綍 `CreateDirect3D11DeviceFromDXGIDevice` 鐢ㄤ簬浠?`IDXGIDevice` 鍒涘缓 `IDirect3DDevice` wrapper锛屽苟杩斿洖 `S_OK` 鎴?HRESULT error code銆?[Source: https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.directx.direct3d11.interop/nf-windows-graphics-directx-direct3d11-interop-createdirect3d11devicefromdxgidevice]
- Microsoft Learn 璁板綍 `IDirect3DDxgiInterfaceAccess` 鏄?WinRT Direct3D device/surface 瀵硅薄鐢ㄤ簬鍙栧洖 wrapped DXGI interface 鐨?COM interface銆?[Source: https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.directx.direct3d11.interop/ns-windows-graphics-directx-direct3d11-interop-idirect3ddxgiinterfaceaccess]
- Microsoft DirectX guidance 浣跨敤鏈夊簭 feature level 鍒涘缓 D3D11 device锛屽苟鍦?interop 鍦烘櫙涓娇鐢?`D3D11_CREATE_DEVICE_BGRA_SUPPORT`銆?[Source: https://learn.microsoft.com/en-us/windows/uwp/gaming/setting-up-directx-resources]
- NuGet 鏄剧ず `Vortice.Direct3D11` 鍜?`Vortice.DXGI` `3.8.3` 鏄綋鍓嶅寘鐗堟湰锛屽苟鍏煎 `net10.0` / Windows target銆?[Source: https://www.nuget.org/packages/Vortice.Direct3D11/; https://www.nuget.org/packages/Vortice.DXGI/]
- NuGet 鏄剧ず `Microsoft.Windows.CsWinRT` stable `2.2.0`锛涘瓨鍦ㄦ洿鏂扮殑 prerelease build锛屼絾 MVP 涓嶉€夋嫨瀹冿紝闄ら潪瀹炵幇鏃惰褰曚簡鍏蜂綋 blocker銆?[Source: https://www.nuget.org/packages/Microsoft.Windows.CsWinRT]

### File Structure Requirements

棰勬湡瀹炵幇浣嶇疆濡備笅锛涘彧鏈夊綋鐜版湁浠ｇ爜寤虹珛浜嗘洿娓呮櫚鐨勭瓑浠风粨鏋勬椂鎵嶈皟鏁达細

```text
src/
  Lumiere.Graphics/
    Devices/
      GraphicsDeviceProvider.cs
      GraphicsDeviceResources.cs
      GraphicsDeviceCreationOptions.cs
  Lumiere.Infrastructure/
    Interop/
      Direct3D11Interop.cs
      DxgiInterfaceAccess.cs
      NativeInteropException.cs
tests/
  Lumiere.Graphics.Tests/
    Devices/
      GraphicsDeviceProviderTests.cs
```

濡傛灉 direct native calls 闇€瑕佹柊澧炲寘寮曠敤锛屽簲鍦?`Directory.Packages.props` 涓泦涓坊鍔犲苟璇存槑鍘熷洜銆備笉瑕佸湪鍗曚釜 project file 鍐呮坊鍔犱复鏃剁増鏈彿銆?[Source: `D:\UGit\lumiere\Directory.Packages.props`; `D:\UGit\lumiere\_bmad-output\project-context.md#Development Workflow Rules`]

### UX Requirements Relevant to This Story

鏈?story 涓嶅疄鐜板彲瑙?UX锛屼絾蹇呴』浜у嚭鏈潵 UX 鍙互璇氬疄鍛堢幇鐨?diagnostics锛?
- 鍒濆鍖栨湡闂翠笉寰楁殫绀?HDR readiness 宸叉垚绔嬨€?- Device creation failure 搴旇瘑鍒棶棰樺彂鐢熷湪 graphics 杩樻槸 interop銆?- 鐢ㄦ埛鍙澶辫触娑堟伅搴旂畝鐭笖闈炴妧鏈寲锛泃echnical detail 鍙互鍖呭惈 HRESULT銆乷peration name 鍜?native API銆?- Degraded/unsupported/failed 鐘舵€佸繀椤讳笌 ready 鐘舵€佹竻鏅板尯鍒嗐€?
[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\ux-design-specification.md#Loading States`; `D:\UGit\lumiere\_bmad-output\planning-artifacts\ux-design-specification.md#Feedback Patterns`]

### Testing Requirements

娴嬭瘯搴旈槻姝㈠疄鐜版紓绉伙紝浣嗕笉瑕佸亣瑁呴獙璇佺湡瀹?HDR 纭欢锛?
- 瀵规ā鎷?HRESULT 鎴?interop failure 鐨?result/diagnostic mapping 鍋氬崟鍏冩祴璇曘€?- 鍦ㄥ彲琛屽娴嬭瘯 disposal idempotency锛岀壒鍒槸鏃犻渶鐪熷疄 GPU surface 涔熻兘娴嬭瘯鐨?wrapper/resource class銆?- 濡傛灉 device creation 娴嬭瘯浼氳Е鍙戞湰鏈?GPU锛岃淇濇寔鏄惧紡涓旂ǔ鍋ワ紱涓嶈璁?CI 渚濊禆 HDR 纭欢銆?- 淇濈暀鐜版湁 HDR constants/readiness tests銆?- 濡傛灉鏂板 `Lumiere.Infrastructure.Tests`锛屽皢鍏跺姞鍏?`Lumiere.sln`锛屽苟娌跨敤鐩稿悓 xUnit 妯″紡銆?
[Source: `D:\UGit\lumiere\_bmad-output\project-context.md#Testing Rules`; `D:\UGit\lumiere\tests\Lumiere.Graphics.Tests\Hdr\PreviewReadinessStatusTests.cs`]

### Previous Story Intelligence

Previous story file: `D:\UGit\lumiere\_bmad-output\implementation-artifacts\1-2-centralize-hdr-constants-and-preview-readiness-status.md`銆?
瑕佺户鎵跨殑鍏抽敭缁忛獙锛?
- Story 1.2 宸插湪 `Lumiere.Graphics.Hdr` 涓疄鐜?HDR constants 鍜?readiness status锛涚洿鎺ュ鐢ㄨ繖浜涚被鍨嬨€?- `PreviewReadinessStatus` 浣跨敤 private constructor 鍜?factory methods锛屽悗缁唬鐮佸簲閫氳繃杩欎簺 factory 鎶ュ憡 readiness锛屼笉鑳芥瀯閫犱换鎰?ready state銆?- 娴嬭瘯宸茬粡閿佸畾 constants 蹇呴』淇濇寔 FP16/scRGB锛屼笖 unvalidated status 涓嶈兘鏄?ready銆?- Story 1.2 宸查€氳繃鐨勯獙璇佸懡浠ゅ寘鎷?restore銆乥uild銆乼est 鍜?format verification锛屽苟浣跨敤 `Platform=x64`銆?- 涓嶈鎻愬墠鍔犲叆 WGC銆乻wap chain 鎴?live preview 琛屼负锛?.2 鏄庣‘鎶婅繖浜涚暀缁欏悗缁?story銆?
### Git Intelligence

鏈€杩戞彁浜ゆ爣棰橈細

- `21a2cea chore: update .gitignore and remove deprecated agent files`
- `06f20db chore: initial project scaffold`

鍙墽琛岀粨璁猴細

- 褰撳墠宸ヤ綔鍖哄凡缁忔湁 Git锛屼笉鍐嶉€傜敤鏈€鏃?project-context 涓€滄湭妫€娴嬪埌 Git鈥濈殑鏃у娉ㄣ€?- 宸叉彁浜?scaffold 浠嶇劧寰堣杽锛涚湡姝ｆ湁鐢ㄧ殑瀹炵幇涓婁笅鏂囦富瑕佹潵鑷湭鎻愪氦鐨?Story 1.2 鏂囦欢鍜岃鍒掓枃妗ｃ€?- 鏆傛椂涓嶈渚濊禆娣卞眰鍘嗗彶妯″紡銆傛柊浠ｇ爜搴斿皬鑰屾槑纭€佸己绫诲瀷锛屽苟涓ユ牸璐村悎 architecture銆?
### Anti-Patterns to Avoid

- 涓嶈鎶?`D3D11CreateDevice`銆丆OM pointer handling銆乄inRT wrapping 鎴?HRESULT parsing 鏀惧埌 `Lumiere.App`銆乣Lumiere.Overlay` 鎴?`Lumiere.Capture`銆?- 涓嶈浣跨敤 `BitmapImage`銆乣SoftwareBitmap`銆丟DI銆?-bit textures 鎴?SDR screenshot 浣滀负渚垮埄璺緞銆?- 涓嶈閲嶅瀹氫箟 `HdrConstants` 鍊笺€?- 涓嶈鍚炴帀 HRESULT锛屼篃涓嶈鍦ㄦ病鏈?diagnostic context 鏃惰繑鍥?`null`銆?- 涓嶈鎶?raw COM pointer 鎴?native handle 鏆撮湶鍒板娉涙ā鍧楄竟鐣屼箣澶栥€?- 涓嶈鍦ㄦ湰 story 鍒涘缓 WGC frame pool銆佹妸 swap chain attach 鍒?`SwapChainPanel`锛屾垨娓叉煋 live frame銆?- 涓嶈娣诲姞 export銆乧lipboard銆乭otkey銆乼ray銆乤nnotation銆乭istory銆乧loud銆乼elemetry 鎴?network 琛屼负銆?
### Project Context Reference

瀹炵幇鍓嶈闃呰 `D:\UGit\lumiere\_bmad-output\project-context.md`銆傛湰 story 鐨勬渶楂樹紭鍏堢骇瑙勫垯锛?
- Native interop code 蹇呴』闅旂鍦ㄧ獎 API 鍚庛€?- Direct3D/DXGI resource ownership 蹇呴』鏄庣‘涓?disposable銆?- Capture callbacks 鍜?UI mutation 蹇呴』涓哄悗缁?story 淇濇寔鍒嗙銆?- HDR correctness 浼樺厛浜庝究鍒╋紱涓嶅厑璁搁潤榛?SDR fallback銆?- 浠讳綍瑙﹀強 resource lifetime 鎴?HDR readiness 鐨勫彉鏇撮兘蹇呴』鍦?review notes 涓鏄庛€?
## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- 2026-04-22: Confirmed Story 1.1/1.2 prerequisites, project target/runtime, Vortice package availability, and existing HDR readiness/constant types.
- 2026-04-22: Added failing device-provider and interop diagnostics tests, then implemented D3D11 device provider, disposable graphics resources, and WinRT/DXGI bridge.
- 2026-04-22: Initial no-restore build failed after adding Infrastructure package references; ran restore and continued.
- 2026-04-22: Parallel build/test caused a transient test DLL file lock; sequential build succeeded.
- 2026-04-22: `dotnet format --verify-no-changes` initially reported line-ending fixes for new files; ran `dotnet format` and re-verified clean.

### Completion Notes List

- Implemented `GraphicsDeviceProvider` with BGRA support, ordered D3D feature levels, hardware D3D11 device creation, immediate context exposure, selected feature level, and typed DXGI access.
- Implemented deterministic disposal for owned D3D11/DXGI resources in `GraphicsDeviceResources`.
- Added `Direct3D11Interop` and `NativeInteropException` under Infrastructure so `IDXGIDevice` can be wrapped into WinRT `IDirect3DDevice` without exposing raw COM pointer handling outside the interop boundary.
- Mapped graphics and interop failures into `PreviewReadinessStatus.Failed(...)` with operation/stage/technical details, while successful device creation remains initialization evidence and does not mark preview `Ready`.
- Added focused xUnit coverage for BGRA/feature-level options, HRESULT/operation diagnostics, and readiness-stage failure mapping. No live HDR hardware validation or WGC/swap-chain/preview behavior was added.

### File List

- src/Lumiere.Graphics/Devices/GraphicsDeviceCreationOptions.cs
- src/Lumiere.Graphics/Devices/GraphicsDeviceException.cs
- src/Lumiere.Graphics/Devices/GraphicsDeviceProvider.cs
- src/Lumiere.Graphics/Devices/GraphicsDeviceResources.cs
- src/Lumiere.Infrastructure/Interop/Direct3D11Interop.cs
- src/Lumiere.Infrastructure/Interop/NativeInteropException.cs
- src/Lumiere.Infrastructure/Lumiere.Infrastructure.csproj
- tests/Lumiere.Graphics.Tests/Devices/GraphicsDeviceProviderTests.cs
- _bmad-output/implementation-artifacts/1-3-create-d3d11-device-and-winrt-dxgi-interop-bridge.md
- _bmad-output/implementation-artifacts/sprint-status.yaml

### Change Log

- 2026-04-22: Implemented D3D11 device provider, WinRT/DXGI interop bridge, diagnostics mapping, focused tests, and marked story ready for review.

### Review Findings

- [x] [Review][Patch] Ensure device creation cannot disable BGRA support [src/Lumiere.Graphics/Devices/GraphicsDeviceProvider.cs:31]
