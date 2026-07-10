# Xu ly Codex app voi HungNguyen custom provider tren Windows

Tai lieu nay ghi lai ket qua chan doan thuc te ngay `2026-07-10`
(`Asia/Bangkok`) va runbook xu ly cho ba be mat:

- ChatGPT Codex desktop app tren Windows.
- Codex extension trong VS Code.
- Codex CLI.

Khong luu API key that trong file nay. Tat ca vi du chi dung ten bien
`HUNGNGUYEN_API_KEY`.

## 1. Ket luan nhanh

Loi chinh cua desktop app khong nam o DNS, TLS, model hay Responses API.
Nguyen nhan la API key chi ton tai trong mot tien trinh dang chay, trong khi
desktop app da duoc khoi dong tu truoc va Windows User/Machine environment
khong co bien do.

| Muc | Van de | Bang chung | Cach xu ly |
| --- | --- | --- | --- |
| P0 | API key da bi lo trong anh/thread | Key xuat hien tren man hinh va duoc tim thay nguyen van trong 2 session JSONL cuc bo | Thu hoi key cu va tao key moi truoc moi thao tac khac |
| P1 | Desktop app bao thieu `HUNGNGUYEN_API_KEY` | Process scope co key, User va Machine scope khong co; log app co 2 loi dung chuoi nay | Luu key moi o User scope, thoat het app, dang xuat/dang nhap Windows hoac restart may, sau do mo app lai |
| P2 | App chua hien model custom va model bi doi | Khi app khong co key, picker chi co catalog fallback; log sau do ghi de thanh `gpt-5.5` khi chon model curated | Sau khi dat key va restart, catalog provider phai hien cac model 5.6; chon `GPT-5.6-Sol` hoac dat slug trong user config |
| P3 | Tasks/Usage cua app bi `401` | Log co `254` loi `/wham/tasks/list` va `25` loi `/wham/usage` | Dang nhap ChatGPT/OpenAI rieng neu can tinh nang cloud; custom provider key khong thay the ChatGPT login |
| P4 | PowerShell khong chay lenh `codex` | `codex.ps1` bi Execution Policy chan | Dung `codex.cmd` hoac binary bundled; khong can ha Execution Policy chi de test |
| P5 | Approval co the bi `403` voi model noi bo | Luong auto-review trong phien chan doan goi `codex-auto-review` qua proxy va nhan `403` | Dung approval do user xac nhan, hoac proxy phai ho tro model reviewer; khong tu them config key khong duoc tai lieu hoa |

Thu tu sua dung la: rotate key, dat User environment, restart day du, chon lai
model trong config, tao thread moi, sau do moi xu ly cac loi cloud/approval phu.

## 2. Trang thai da xac minh

### 2.1 Phien ban

- Desktop package: `OpenAI.Codex_26.707.3748.0_x64`.
- Desktop process: `ChatGPT.exe`.
- Codex CLI trong runtime va VS Code extension: `0.144.0-alpha.4`.
- Desktop app khoi dong luc khoang `10:21`.
- VS Code khoi dong lai luc khoang `16:22`.

Chenh lech thoi diem khoi dong quan trong vi environment variable duoc tien
trinh con ke thua tai luc tien trinh duoc tao. Mot app dang chay khong tu nhan
bien moi vua duoc dat trong terminal khac.

### 2.2 Cau hinh user tai thoi diem chan doan

File dang duoc desktop app su dung:

```text
C:\Users\<user>\.codex\config.toml
```

Phan lien quan da duoc xac minh:

```toml
model = "gpt-5.5"
model_provider = "hungnguyen"
model_reasoning_effort = "xhigh"

[model_providers.hungnguyen]
name = "HungNguyen Proxy"
base_url = "https://codex.hungnguyen.codes/v1"
wire_api = "responses"
env_key = "HUNGNGUYEN_API_KEY"
```

`env_key` chi la ten cua environment variable. No khong doc key tu
`config.toml`, VS Code settings, terminal history, hoac mot thread chat.

### 2.3 Scope cua key

Ket qua da kiem tra ma khong in gia tri key:

| Scope | Co gia tri | Do dai |
| --- | ---: | ---: |
| Process cua phien chan doan | Co | 51 |
| Windows User | Khong | 0 |
| Windows Machine | Khong | 0 |

Day la bang chung truc tiep cho loi desktop app. VS Code/terminal hien tai co
the chay duoc vi no dang co key trong Process scope. Desktop app mo tu Start
Menu khong co key o User scope de ke thua.

### 2.4 Provider va model

Da kiem tra truc tiep ma khong in key hoac response text:

- `GET /v1/models`: HTTP `200`.
- Catalog co `gpt-5.5`: co.
- Catalog co `gpt-5.6-sol`: co.
- `POST /v1/responses` voi `gpt-5.5`: HTTP `200`.
- `POST /v1/responses` voi `gpt-5.6-sol`: HTTP `200`.
- `POST /v1/responses` voi `stream=true`: HTTP `200`, content type
  `text/event-stream`, co du event tu `response.created` den
  `response.completed` trong khoang `1.66` giay.
- `codex debug models` khi doc dung `CODEX_HOME` va co key tra cac model
  `gpt-5.6-sol`, `gpt-5.6-terra`, `gpt-5.6-luna` voi `visibility=list`.

Vi vay khong can doi `base_url`, khong can doi sang Chat Completions, va khong
can doi model de sua loi `Missing environment variable`.

### 2.5 Bang chung tu desktop log

Log duoc tim thay tai:

```text
%LOCALAPPDATA%\Packages\OpenAI.Codex_2p2nqsd0c76g0\LocalCache\Local\Codex\Logs\YYYY\MM\DD\codex-desktop-*.log
```

Su kien quan trong trong log ngay `2026-07-10`:

- Line `973`: app ghi default `newModel=gpt-5.6-sol`, `newEffort=xhigh`.
- Lines `2433`, `2448`, `2451`, `2455`: app ghi de default thanh
  `newModel=gpt-5.5` sau thao tac trong picker.
- Lines `2118` va `2508`: thread metadata that bai voi
  `Missing environment variable: HUNGNGUYEN_API_KEY`.
- App goi app-server method `model/list` de lay du lieu cho model picker.
- `/wham/tasks/list`: `254` lan HTTP `401`.
- `/wham/usage`: `25` lan HTTP `401`.

Ket luan ve menu dua tren screenshot, `model/list`, catalog runtime va log cua
build nay, khong phai cam ket API/UI lau dai: app runtime chap nhan slug
`gpt-5.6-sol`. Khi tien trinh khong co key, picker roi ve catalog curated; khi
runtime co key, catalog provider danh dau cac model 5.6 la `list`, nen chung co
the xuat hien trong dropdown sau khi restart app dung cach.

## 3. Tai sao extension chay nhung app loi

Windows co ba scope environment thuong gap:

1. `Process`: chi tien trinh hien tai va cac tien trinh con tao sau do.
2. `User`: cac tien trinh moi cua user sau khi environment duoc nap lai.
3. `Machine`: cac tien trinh moi tren may, thuong can quyen admin de sua.

Lenh nay chi dat Process scope:

```powershell
$env:HUNGNGUYEN_API_KEY = "..."
```

Neu chay lenh trong terminal cua VS Code, extension hoac CLI khoi dong tu terminal
do co the thay key. Desktop app da mo tu Start Menu hoac da chay san se khong thay.

Them key vao `[shell_environment_policy]` cung khong sua loi nay.
`shell_environment_policy` dieu khien environment Codex truyen xuong cac lenh
tool ma agent chay; provider auth can key ngay trong chinh tien trinh Codex truoc
khi agent co the chay tool.

## 4. Quy trinh sua an toan

### Buoc 0: Rotate key da bi lo

Key trong screenshot phai duoc coi la da bi compromise, ke ca khi anh chi duoc
gui trong mot chat rieng.

1. Mo trang quan tri cua HungNguyen provider.
2. Thu hoi key cu.
3. Tao key moi.
4. Khong dan key moi vao chat, issue, file Markdown, `config.toml`, hoac source.

Khong copy key cu dang co trong Process scope sang User scope. Lam nhu vay chi
giup app chay bang mot credential da bi lo.

### Buoc 1: Thoat desktop app

Thoat app bang menu cua app. Sau do kiem tra:

```powershell
Get-Process ChatGPT -ErrorAction SilentlyContinue |
  Select-Object Id, ProcessName, StartTime
```

Neu van con process, thu thoat tu system tray truoc. Chi dung `Stop-Process`
khi da chap nhan mat cac tac vu dang chay:

```powershell
Get-Process ChatGPT -ErrorAction SilentlyContinue | Stop-Process
```

Khong kill tat ca process `codex` mot cach mu quang vi co the lam dung extension
hoac CLI dang lam viec.

### Buoc 2: Luu key moi vao Windows User environment

Doan sau khong dat literal key vao PowerShell history. Key van ton tai trong
Windows User environment, day khong phai secret vault va bat ky process nao cua
user cung co the doc no.

```powershell
$secureKey = Read-Host "New HUNGNGUYEN_API_KEY" -AsSecureString
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureKey)

try {
    $plainKey = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)

    if ([string]::IsNullOrWhiteSpace($plainKey)) {
        throw "API key is empty."
    }

    [Environment]::SetEnvironmentVariable(
        "HUNGNGUYEN_API_KEY",
        $plainKey,
        [EnvironmentVariableTarget]::User
    )
}
finally {
    if ($bstr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }

    Remove-Variable plainKey, secureKey, bstr -ErrorAction SilentlyContinue
}
```

Kiem tra chi trang thai, khong in gia tri:

```powershell
$value = [Environment]::GetEnvironmentVariable(
    "HUNGNGUYEN_API_KEY",
    [EnvironmentVariableTarget]::User
)

[pscustomobject]@{
    Present = -not [string]::IsNullOrWhiteSpace($value)
    Length  = if ($null -eq $value) { 0 } else { $value.Length }
}

Remove-Variable value
```

Ket qua mong doi: `Present = True`, `Length > 0`.

Sau khi dat User environment, cach chac chan nhat de Explorer/Start Menu va moi
app nhan bien moi la dang xuat Windows roi dang nhap lai, hoac restart may.
Chi dong/mo lai app co the chua du neu shell Windows van giu environment cu.

### Buoc 3: Chon model mong muon trong config

Sao luu file truoc:

```powershell
$codexHome = Join-Path $env:USERPROFILE ".codex"
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
Copy-Item "$codexHome\config.toml" "$codexHome\config.toml.backup-$stamp"
```

Neu muon dung `gpt-5.6-sol`, dong app truoc, sau do dat user config nhu sau:

```toml
model = "gpt-5.6-sol"
model_provider = "hungnguyen"
model_reasoning_effort = "xhigh"

[model_providers.hungnguyen]
name = "HungNguyen Proxy"
base_url = "https://codex.hungnguyen.codes/v1"
wire_api = "responses"
env_key = "HUNGNGUYEN_API_KEY"
env_key_instructions = "Set HUNGNGUYEN_API_KEY at Windows User scope, then fully restart Codex."
requires_openai_auth = false
```

Neu muon dung `gpt-5.5`, chi doi dong `model`:

```toml
model = "gpt-5.5"
```

Luu y:

- `wire_api = "responses"` la dung. Tai lieu hien tai chi ho tro gia tri nay.
- Khong them `/responses` vao `base_url`; Codex tu ghep endpoint.
- Khong dat `base_url` thanh `/v1/chat/completions`.
- Khong dat key truc tiep bang `experimental_bearer_token` neu khong co ly do dac
  biet; tai lieu chinh thuc khuyen dung `env_key`.
- Provider phai nam trong user config. Project `.codex/config.toml` khong duoc
  override `model_provider` va `model_providers`.
- Neu app dang hien GPT-5.5 du config la `gpt-5.6-sol`, khong chon mot item curated
  trong picker. Build nay co the ghi item do tro lai user config.

### Buoc 4: Mo app trong environment moi

Sau khi dang xuat/dang nhap lai hoac restart may:

1. Mo ChatGPT Codex app.
2. Mo project.
3. Mo model dropdown va kiem tra co `GPT-5.6-Sol`, `GPT-5.6-Terra` va
   `GPT-5.6-Luna`.
4. Chon `GPT-5.6-Sol` neu day la model mong muon.
5. Tao `New task`; khong dung lai thread da fail de loai cache theo thread.
6. Gui mot prompt ngan, vi du `Reply exactly OK`.
7. Kiem tra khong con banner thieu environment variable.

Neu dropdown van chi co model curated, dung `codex.cmd debug models` trong mot
terminal moi. Neu lenh nay khong hien cac model 5.6 thi terminal/app van chua
nhan dung key hoac `CODEX_HOME`.

### Buoc 5: Xac minh log moi

Chay sau khi test app:

```powershell
$logRoot = Join-Path $env:LOCALAPPDATA `
  "Packages\OpenAI.Codex_2p2nqsd0c76g0\LocalCache\Local\Codex\Logs"

$latestLog = Get-ChildItem $logRoot -Recurse -File -Filter "codex-desktop-*.log" |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 1

Select-String -LiteralPath $latestLog.FullName -Pattern @(
    "Missing environment variable",
    "Setting default model and reasoning effort",
    "status=401",
    "status=403"
) | Select-Object -Last 30 Path, LineNumber, Line
```

Khong chia se nguyen file log neu chua kiem tra secret. Khi can gui log, redact
chuoi giong key truoc.

## 5. Kiem tra provider khong lo key

### 5.1 Kiem tra scope

```powershell
$name = "HUNGNGUYEN_API_KEY"

foreach ($scope in @("Process", "User", "Machine")) {
    $value = [Environment]::GetEnvironmentVariable($name, $scope)

    [pscustomobject]@{
        Scope   = $scope
        Present = -not [string]::IsNullOrWhiteSpace($value)
        Length  = if ($null -eq $value) { 0 } else { $value.Length }
    }
}

Remove-Variable value
```

Desktop app mo tu Windows shell can `User = True` hoac `Machine = True`.
Khong can dat ca hai.

### 5.2 Kiem tra model catalog

Doan Node sau chi in status va boolean, khong in key hay response body:

```powershell
@'
const key = process.env.HUNGNGUYEN_API_KEY;

if (!key) {
  throw new Error("HUNGNGUYEN_API_KEY is missing in this process");
}

const response = await fetch("https://codex.hungnguyen.codes/v1/models", {
  headers: { Authorization: `Bearer ${key}` },
});

const body = await response.json();
const ids = Array.isArray(body.data) ? body.data.map((item) => item.id) : [];

console.log({
  status: response.status,
  modelCount: ids.length,
  hasGpt55: ids.includes("gpt-5.5"),
  hasGpt56Sol: ids.includes("gpt-5.6-sol"),
});
'@ | node
```

### 5.3 Kiem tra Responses API

```powershell
@'
const key = process.env.HUNGNGUYEN_API_KEY;
const model = "gpt-5.6-sol";

if (!key) {
  throw new Error("HUNGNGUYEN_API_KEY is missing in this process");
}

const response = await fetch("https://codex.hungnguyen.codes/v1/responses", {
  method: "POST",
  headers: {
    Authorization: `Bearer ${key}`,
    "Content-Type": "application/json",
  },
  body: JSON.stringify({
    model,
    input: "Reply exactly OK.",
    max_output_tokens: 16,
  }),
});

const body = await response.json();

console.log({
  status: response.status,
  requestedModel: model,
  responseModel: body.model ?? null,
  hasOutput: Array.isArray(body.output),
  error: body.error?.message ?? null,
});
'@ | node
```

Neu hai test nay thanh cong ma app van bao `Missing environment variable`, khong
test lai server nua. Quay lai scope, thoi diem khoi dong app va restart Windows.

## 6. Hanh vi model `Custom` trong extension va app

### VS Code extension

Extension co the hien `Custom` khi model/provider hien tai khong nam trong danh
sach curated cua picker. Day la nhan UI, khong phai mot gia tri can ghi vao
`model = "custom"`.

Khong bao gio cau hinh:

```toml
model = "custom"
```

Phai dung slug that, vi du:

```toml
model = "gpt-5.6-sol"
```

### Desktop app

Build da kiem tra co cac dac diem:

- Runtime tung nhan `gpt-5.6-sol` tu config.
- Picker lay danh sach qua app-server `model/list`.
- Khi app thieu key, picker chi hien catalog curated nhu screenshot.
- Khi runtime co key, `codex debug models` thay `GPT-5.6-Sol`,
  `GPT-5.6-Terra`, `GPT-5.6-Luna` voi `visibility=list`.
- Chon GPT-5.5 trong picker goi `config/batchWrite` va ghi de global default.

Quy tac van hanh:

1. Dat key o User scope va restart day du.
2. Mo dropdown; uu tien chon `GPT-5.6-Sol` neu no xuat hien.
3. Neu dropdown chua refresh, dong app, sua user `config.toml`, roi mo app lai.
4. Khong chon GPT-5.5/GPT-5.4 neu muon giu slug `gpt-5.6-sol`.
5. Neu nghi bi ghi de, kiem tra hai dong `model` va `model_provider` ngay.

```powershell
Select-String "$env:USERPROFILE\.codex\config.toml" -Pattern @(
  '^model\s*=',
  '^model_provider\s*='
)
```

Khong can `model_catalog_json` cho provider nay: catalog runtime da co cac model
5.6. Catalog override thu cong chi nen dung khi provider khong tra metadata model,
vi moi entry con chua base instructions va cac capability flags cua Codex.

## 7. Profiles dung cho CLI

Profiles la co che chuyen layer cau hinh cua CLI. Tai lieu chinh thuc hien tai
quy dinh file nam canh `config.toml` va chon bang `--profile`.

User config nen giu block provider de desktop app cung dung duoc:

```toml
[model_providers.hungnguyen]
name = "HungNguyen Proxy"
base_url = "https://codex.hungnguyen.codes/v1"
wire_api = "responses"
env_key = "HUNGNGUYEN_API_KEY"
```

File `~/.codex/hung.config.toml`:

```toml
model = "gpt-5.6-sol"
model_provider = "hungnguyen"
model_reasoning_effort = "xhigh"
```

File `~/.codex/account.config.toml`:

```toml
model = "gpt-5.5"
model_provider = "openai"
model_reasoning_effort = "xhigh"
```

Chay bang PowerShell:

```powershell
codex.cmd --profile hung
codex.cmd --profile account
```

Hoac non-interactive:

```powershell
codex.cmd exec --profile hung --ephemeral "Reply exactly OK"
```

Khong dung top-level `profile = "hung"`; tu Codex `0.134.0`, selector top-level
cu khong con duoc ho tro. Desktop app picker cung khong phai UI chon CLI profile.

## 8. Ma tran xu ly loi

### `Missing environment variable: HUNGNGUYEN_API_KEY`

Nguyen nhan:

- Chi dat `$env:...` trong terminal.
- User environment chua co.
- App da chay truoc khi dat bien.
- Da restart cua so nhung process nen van con trong system tray.

Xu ly:

1. Kiem tra scope ma khong in key.
2. Dat key moi o User scope.
3. Thoat het `ChatGPT.exe`.
4. Dang xuat/dang nhap Windows hoac restart may.
5. Tao task moi.

### `401 Unauthorized` tu HungNguyen `/v1/responses`

Chi xu ly sau khi khong con loi missing environment:

- Key sai, het han, bi revoke, hoac copy thua ky tu.
- Provider khong chap nhan key moi.
- Header bi proxy trung gian loai bo.

Chay test `/v1/models` va `/v1/responses`. Khong dan key vao lenh curl literal.

### `401` tu `/wham/tasks/list` hoac `/wham/usage`

Day la auth cua ChatGPT/cloud, khong phai auth custom model provider.

Xu ly:

- Dang nhap lai ChatGPT trong app neu can Scheduled, Tasks, Usage hoac cloud.
- Khong doi `HUNGNGUYEN_API_KEY` de sua cac endpoint nay.
- Neu chi can local coding voi custom provider, tach loi nay khoi loi model call.

### Model tu `gpt-5.6-sol` quay ve `gpt-5.5`

Nguyen nhan da quan sat: desktop picker ghi global default qua
`config/batchWrite`.

Xu ly:

1. Dong app.
2. Dat lai `model = "gpt-5.6-sol"`.
3. Mo app lai.
4. Khong chon model curated trong picker.

### `Model provider 'hungnguyen' not found`

Kiem tra:

- Provider block co trong user `config.toml` khong.
- `CODEX_HOME` co tro sang thu muc khac khong.
- Co dang chay CLI trong sandbox/read-only context khong.
- Co nham dat provider block trong project `.codex/config.toml` khong; project
  config khong duoc override provider.

Chan doan CLI:

```powershell
$env:CODEX_HOME
Test-Path "$env:USERPROFILE\.codex\config.toml"
codex.cmd mcp list
```

Trong phien chan doan nay, CLI chi doc dung config sau khi `CODEX_HOME` duoc dat
tuong minh, nhung sandbox lai chan ghi `state_5.sqlite`. Day la gioi han cua
phien agent kiem thu, khong phai bang chung desktop app bi loi state DB.

### `codex.ps1 cannot be loaded because running scripts is disabled`

Khong can ha Execution Policy neu chi muon chay CLI:

```powershell
codex.cmd --version
```

Chi thay doi Execution Policy sau khi doc huong dan Microsoft va hieu anh huong.

### `403` voi `codex-auto-review`

Custom proxy co the ho tro model chat nhung khong ho tro model noi bo dung cho
approval/reviewer.

Xu ly an toan:

- Chon approval do user xac nhan trong app.
- Hoac yeu cau provider map/ho tro model reviewer.
- Khong gia dinh `review_model` se sua approval reviewer; `review_model` theo
  tai lieu la model cho `/review`, khong phai cam ket cho approval.

### TLS/plugin sync `SEC_E_NO_CREDENTIALS`

Loi nay da xuat hien khi CLI sync plugin qua GitHub bang Schannel, trong khi
Node van goi provider thanh cong. No la loi credential/TLS cua mot transport
khac, khong phai ly do app bao thieu environment variable.

Xu ly rieng:

- Kiem tra Windows credential/certificate session.
- Thu `git ls-remote` trong terminal user binh thuong.
- Cap nhat Git for Windows neu loi lap lai.
- Khong doi model provider chi de sua plugin sync.

## 9. Bao mat va session da luu key

Phep quet exact-key tai thoi diem chan doan tim thay 2 file session JSONL co key
nguyen van. Khong co key that nao duoc dua vao tai lieu nay.

Nguyen tac xu ly:

1. Rotate key la bat buoc va co hieu luc ngay ca khi khong xoa duoc moi ban sao.
2. Xoa conversation lien quan bang UI neu can giam du lieu luu tru.
3. Khong xoa tay ngau nhien file trong `~/.codex/sessions` khi app dang chay;
   session index co the bi lech.
4. Xoa file local khong dam bao ban sao server, backup, screenshot hoac log khac
   da bien mat.
5. Khong commit session, screenshot chua key, hoac log chua auth header.

Windows User environment khong ma hoa key nhu mot secret manager. Neu can muc
bao mat cao hon, Codex ho tro command-backed auth bang
`[model_providers.<id>.auth]`; credential helper phai duoc thiet ke rieng va
khong duoc ket hop voi `env_key`.

## 10. Rollback ve OpenAI login

Neu can dua app ve provider chinh thuc trong luc cho key moi:

```toml
model = "gpt-5.5"
model_provider = "openai"
model_reasoning_effort = "xhigh"
```

Giu hoac bo block `[model_providers.hungnguyen]` deu duoc neu
`model_provider = "openai"`. Khong ghi `gpt-5.6-sol` cho OpenAI login neu tai
khoan chinh thuc khong duoc cap model do.

Xoa User environment neu khong con dung proxy:

```powershell
[Environment]::SetEnvironmentVariable(
    "HUNGNGUYEN_API_KEY",
    $null,
    [EnvironmentVariableTarget]::User
)
```

Sau rollback, thoat het app va mo lai trong environment moi.

## 11. Checklist hoan tat

- [ ] Key cu trong screenshot da bi revoke.
- [ ] Key moi khong xuat hien trong chat, file repo, command history hoac log.
- [ ] `HUNGNGUYEN_API_KEY` co o Windows User scope.
- [ ] Tat ca process `ChatGPT.exe` cu da thoat.
- [ ] Windows da dang xuat/dang nhap lai hoac restart.
- [ ] `model_provider = "hungnguyen"` nam trong user config.
- [ ] `wire_api = "responses"`.
- [ ] `model` la slug mong muon, khong phai chuoi `custom`.
- [ ] Task moi khong con banner missing environment.
- [ ] Dropdown hien `GPT-5.6-Sol` hoac `codex debug models` xac nhan model co
      `visibility=list`.
- [ ] `/v1/models` va `/v1/responses` tra `200` khi can chan doan.
- [ ] Neu can Tasks/Usage, ChatGPT account da dang nhap rieng.
- [ ] Khong chon model curated trong app picker sau khi dat custom slug.

## 12. Nguon chinh thuc va gioi han xac minh

Nguon OpenAI da doi chieu ngay `2026-07-10`:

- Configuration reference:
  <https://developers.openai.com/codex/config-reference>
- Advanced configuration, profiles va custom providers:
  <https://developers.openai.com/codex/config-advanced>
- ChatGPT desktop app for Windows:
  <https://developers.openai.com/codex/app/windows>

Cac diem duoc tai lieu chinh thuc xac nhan:

- User config nam tai `~/.codex/config.toml`.
- `model_provider` tro den mot id trong `model_providers`.
- `env_key` la environment variable cung cap provider API key.
- `responses` la gia tri `wire_api` duoc ho tro.
- Project config khong duoc override provider/auth.
- Profiles la file `$CODEX_HOME/<name>.config.toml` va chon bang `--profile`.
- Windows app dung `%USERPROFILE%\.codex` cho Codex home native.

Codex manual helper khong xac minh duoc response vi thieu header
`x-content-sha256`. OpenAI Developer Docs MCP chua duoc cai va viec cai bi chan
boi luong approval `403`. Vi vay tai lieu nay dung cac trang Markdown chinh thuc
truc tiep o tren, cong voi log va test runtime cuc bo. Hanh vi menu `Custom` la
ket luan co gioi han theo build `26.707.3748.0`, khong phai giao keo UI cho cac
ban app sau. App-server V2 test da nap dung provider/model, nhung request tu child
Codex trong sandbox bi chan network; phep chay ngoai sandbox bi approval reviewer
tu choi `403`. Vi vay provider, streaming va model catalog da duoc xac minh,
nhung GUI desktop sau khi dat key moi van can mot smoke test cuoi.
