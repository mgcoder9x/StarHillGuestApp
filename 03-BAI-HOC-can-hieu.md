# Linux VPS Web/IoT — Giáo trình và phòng lab từ số 0

<!-- markdownlint-disable MD013 MD033 -->
<!-- MD013: giữ lệnh/bảng dễ sao chép; MD033: thẻ a tạo anchor ASCII ổn định. -->

> **Baseline:** Ubuntu Server 24.04 LTS. File này là phần bài học/thực hành cho mọi ID trong [`02-LO-TRINH-tong-the.md`](02-LO-TRINH-tong-the.md). Cách suy luận, rubric và quy tắc an toàn nằm trong [`01-TU-DUY-nen-tang.md`](01-TU-DUY-nen-tang.md).

## Dự án xuyên suốt: Dashboard giám sát nhiệt độ/độ ẩm

Toàn bộ hành trình dùng một hệ thống có thể bắt đầu không cần phần cứng:

- Simulator đóng vai cảm biến, tạo `temperature`, `humidity`, `device_id` và timestamp.
- MQTT là đường telemetry chính; REST qua HTTPS là đường so sánh/fallback.
- Mosquitto nhận message; worker kiểm tra payload rồi ghi PostgreSQL.
- API trả dữ liệu lịch sử; WebSocket/SSE đưa điểm mới tới trình duyệt.
- Nginx phục vụ dashboard và reverse proxy API/realtime qua HTTPS.
- Mỗi thiết bị có identity riêng; dữ liệu sai hoặc trái quyền bị từ chối.

```text
Simulator/cảm biến
  ├─ MQTT TLS → Mosquitto → worker ─┐
  └─ REST HTTPS → API ──────────────┤
                                    ▼
                               PostgreSQL
                                    │
                        API lịch sử + WebSocket/SSE
                                    │
                              Nginx HTTPS
                                    │
                                Trình duyệt
```

Đây là hệ thống **near-real-time**, không phải hard real-time. Cảm biến thật là biến thể sau cùng; simulator là lựa chọn mặc định để người mới có thể học ngay.

## Cách dùng file này

1. Tìm ID tiếp theo trong File 02 và kiểm tra `Cần trước`.
2. Đọc mục tiêu, tự trả lời câu **Tự hỏi**, rồi mới đọc **Chốt lại**.
3. Chạy **Preflight**, dự đoán output, sau đó làm lab trên VM.
4. So sánh với **Bằng chứng đạt**; PID, IP, timestamp và phiên bản cụ thể có thể khác.
5. Làm **Break/fix** mà không xem **Gợi ý sửa** trước.
6. Diễn tập **Rollback/reset** trong snapshot hoặc bản sao. Nếu bài sau phụ thuộc sản phẩm vừa tạo, dựng lại trạng thái **Bằng chứng đạt** trước khi tiếp tục; không cleanup mù tài nguyên dùng chung.
7. Làm **Checkpoint** độc lập, ghi nhật ký và tự chấm rubric trong File 01.

## Quy ước môi trường

Mỗi block lệnh được gắn một nhãn trong phần mô tả:

- **HOST:** Windows/máy cá nhân đang chạy hypervisor hoặc SSH client.
- **VM:** Ubuntu lab có snapshot; nơi làm bài phá/fix.
- **VPS:** Ubuntu public; chỉ dùng sau `GATE-VPS`.
- **CLIENT:** một máy nằm ngoài VPS để kiểm tra từ Internet.

Trước lệnh có `sudo`, thay đổi mạng, xóa hoặc ghi đè, chạy:

```bash
whoami
hostname
pwd
```

Không gõ ký hiệu `$` hoặc `#` nếu chúng xuất hiện trong phần output minh họa. Trong các block `bash` của tài liệu này, dòng không bắt đầu bằng chú thích là lệnh có thể chạy sau khi thay placeholder.

**Kỷ luật khi dán block nhiều dòng:** đọc toàn block trước; các block dùng lại biến phải chạy trong cùng một Bash session. Không tự bật `set -e` trong shell tương tác: thiết lập đó tồn tại sau block và một lệnh lỗi về sau có thể đóng phiên SSH. Bài này bắt trạng thái lỗi dự kiến bằng `if ...; then ...; else ...; fi`. Mọi `exit` dùng cho guard phải nằm trong script hoặc subshell `( ... )`; không gõ `exit` trần trên phiên SSH trừ khi thật sự muốn đăng xuất. Một assertion `test` thất bại có nghĩa **dừng bài tại đó**, đọc bằng chứng và rollback; không tiếp tục dán block kế tiếp.

## Placeholder dùng xuyên suốt

| Placeholder | Thay bằng | Ví dụ không phải secret |
| --- | --- | --- |
| `LAB_USER` | user thường trên VM/VPS | `toan` |
| `VM_IP` | IP private của VM | `192.168.56.20` |
| `VPS_IP` | public IP của VPS | `203.0.113.10` |
| `YOUR_DOMAIN` | domain do bạn sở hữu | `iot.example.com` |
| `APP_USER` | system user chạy app | `iotapp` |
| `APP_DIR` | thư mục release/app | `/srv/iot-api` |
| `DEVICE_ID` | mã thiết bị | `sensor-001` |
| `HOST_IP` | source IP mà VM thấy từ HOST (`${SSH_CLIENT%% *}` trong phiên SSH) | `192.168.56.1` |
| `VPS_IPV6` | public IPv6 chỉ khi route/firewall/listener đã hoạt động | `2001:db8::10` |
| `YOUR_LAB_HOST` | hostname có thể resolve tới MQTT lab và khớp certificate SAN | `mqtt.lab.example` |
| `UNIT_NAME` | tên systemd unit khi dùng snippet kiểm tra chung | `iot-api` |
| `TEN_MOI` | hostname mới do bạn chọn, không gõ nguyên placeholder | `linux-lab` |
| `CHANGE_ME` | giá trị mẫu bắt buộc thay trước khi dùng; không phải secret mặc định | `lab-user` |
| `REGION/CITY` | timezone IANA khi minh họa đổi timezone | `Asia/Ho_Chi_Minh` |
| `TEN_CHUNG_CHI_DA_XAC_NHAN` | tên certificate đã liệt kê bằng `certbot certificates` | `iot.example.com` |
| `MQTT_LAB_CA_PATH` | path tuyệt đối tới CA client của MQTT lab | `$HOME/.config/mqtt-lab-ca.crt` |
| `PASSWORD_DA_NHAP_O_mosquitto_passwd` | password đã nhập tương tác; không ghi literal vào tài liệu | `(nhập tại prompt)` |
| `CURRENT_DEVICE_PASSWORD` | password hiện tại, chỉ nhập trong file lab cục bộ | `(nhập tại prompt)` |
| `NEW_DEVICE_PASSWORD` | password mới, chỉ nhập trong file lab cục bộ | `(nhập tại prompt)` |
| `SECRET` | giá trị bí mật của lab; không in vào tài liệu, Git, log hoặc command history | `(không ghi)` |

`DEMO_REVOKE_ME` và `LAB_ROTATE_ME` là canary giả để tìm trong lab, không phải credential hay placeholder cần điền.

`203.0.113.0/24` là dải tài liệu, không phải IP VPS thật. Secret không được viết trực tiếp vào tài liệu, Git hoặc command history; tạo riêng trong lab và lưu với quyền phù hợp.

## Command, option, argument và output

```text
program [option] [argument]
```

Ví dụ `ls -la /etc` có program `ls`, options `-l` và `-a`, argument `/etc`.

- **stdin:** dữ liệu lệnh nhận vào.
- **stdout:** output bình thường.
- **stderr:** lỗi/cảnh báo.
- **Exit status:** `0` thường là thành công; số khác cần đọc lỗi.

Kiểm exit status của lệnh vừa chạy:

```bash
printf 'exit=%s\n' "$?"
```

Các ký hiệu cần biết:

- `|` nối stdout lệnh trước vào stdin lệnh sau.
- `>` ghi đè; `>>` nối thêm.
- `'...'` giữ nội dung gần như nguyên văn; `"..."` vẫn cho phép mở rộng biến.
- `*`, `?`, `[...]` là glob; preview bằng `printf '%s\n' pattern*` trước lệnh nguy hiểm.
- `Ctrl+C` yêu cầu dừng foreground process; `Ctrl+D` gửi EOF; `q` thoát `less`/`man`.

## Mức rủi ro và preflight dùng chung

| Nhãn | Ý nghĩa |
| --- | --- |
| 🟢 | Quan sát, không đổi trạng thái. |
| 🟡 | Thay đổi nhỏ, rollback rõ. |
| 🟠 | Cần đặc quyền hoặc ảnh hưởng service/network. |
| 🔴 | Có thể mất dữ liệu, mất SSH hoặc không boot; chỉ làm trong snapshot/đĩa thử. |
| 💰 | Có thể phát sinh phí cloud. |

Với bài 🟠/🔴 phải ghi được: target, backup/snapshot, lệnh validate, rollback và bằng chứng sau thay đổi. Với SSH/firewall phải giữ phiên hiện tại, kiểm provider/VM console và thử phiên thứ hai.

## Cấu trúc thư mục dự án

Tài liệu dùng quy ước sau:

```text
/srv/iot-simulator/          simulator code/service
/srv/iot-api/                API source/repo khi phát triển
/srv/iot-releases/           các release theo full commit hash
/srv/iot-current             symlink tới release đang chạy
/etc/iot-dashboard/          config và secret reference, quyền hạn chế
/var/lib/iot-dashboard/      state do app quản lý nếu có
/var/log/                    log truyền thống của Nginx và một số service
systemd journal              log app/worker mặc định
```

`/var/www` là quy ước phổ biến cho static content, không phải vị trí bắt buộc. Luôn đọc `root` trong Nginx và `WorkingDirectory` trong systemd thay vì đoán từ tên thư mục.

## Quy trình xác minh lặp lại

### Service

Thay `UNIT_NAME` bằng tên unit thật trước khi chạy; ví dụ với API:

```bash
UNIT_NAME=iot-api
sudo systemd-analyze verify "/etc/systemd/system/$UNIT_NAME.service"
sudo systemctl status "$UNIT_NAME" --no-pager
sudo journalctl -u "$UNIT_NAME" -n 50 --no-pager
```

### Nginx

```bash
sudo nginx -t
sudo systemctl reload nginx
curl -fsS http://127.0.0.1/health
```

### Mạng từ trong ra ngoài

```text
DNS A/AAAA
→ route/provider firewall
→ UFW/nftables
→ listener và bind address (`ss`)
→ TCP/TLS
→ HTTP/app
→ database/downstream
```

Firewall `allow` không tạo listener. `ping` không kiểm tra TCP port. `ss` trả lời “máy có listener không”; `curl` trả lời “protocol có phản hồi không”.

### SSH/firewall an toàn

1. Xác nhận console cứu hộ.
2. Giữ phiên SSH thứ nhất.
3. Cho phép đúng SSH port trong UFW.
4. Validate SSH bằng `/usr/sbin/sshd -t`.
5. Reload, không reboot vội.
6. Mở phiên SSH thứ hai và xác minh `sudo`.
7. Chỉ sau đó mới đóng phiên thứ nhất hoặc tắt password login.

## Công nghệ tham chiếu

- Python 3 + virtual environment cho sample app/simulator/worker.
- FastAPI/Uvicorn cho API và WebSocket.
- PostgreSQL làm database Core; TimescaleDB là lựa chọn mở rộng.
- Mosquitto cho MQTT.
- Nginx cho static/reverse proxy/TLS edge.
- systemd trước Docker để người học hiểu process/service; Docker/Compose học ở Cấp 6.

Các đoạn code mẫu là bộ khung học tập, không tự động trở thành production-ready. Bài bảo mật, quan sát, backup và vận hành bổ sung các lớp còn thiếu.

---

## Cấp -1 — Phòng lab an toàn

<a id="lab-01"></a>

### LAB-01 — Tạo VM, snapshot và restore

**Cần trước:** không. **Mục tiêu:** có một Ubuntu Server 24.04 LTS có thể phá thử và quay lại trạng thái cũ. **Rủi ro:** 🟡; chỉ thao tác trên VM, không chọn disk/partition của máy thật.

**Loại suy:** VM là phòng mô phỏng; snapshot là điểm lưu. Giới hạn: snapshot phụ thuộc hypervisor và không thay thế backup độc lập.

**Tự hỏi:** Vì sao người mới không nên học `fstab`, firewall hoặc quyền file lần đầu trên VPS đang dùng thật?

**Chốt lại:** vì một lỗi có thể làm mất boot hoặc mất SSH. VM có console và snapshot làm hậu quả nhỏ, nhanh lặp lại.

**Lab — HOST:** tạo VM bằng Hyper-V, VirtualBox, VMware hoặc hypervisor bạn đang có:

- ISO Ubuntu Server 24.04 LTS chính thức.
- 2 vCPU, 2–4 GB RAM, disk 20 GB dạng tăng động.
- Adapter 1 dùng NAT để VM ra Internet.
- Thêm **host-only adapter** để HOST truy cập VM trực tiếp; trong tài liệu, `VM_IP` là IP host-only. Nếu hypervisor không hỗ trợ host-only, dùng NAT port-forward `HOST 127.0.0.1:2222 → VM :22` và thêm `-p 2222` vào lệnh SSH.
- Chưa bridge trực tiếp ra LAN nếu chưa hiểu firewall.
- Tạo user thường, không bật dịch vụ không cần thiết.

Sau khi cài, chạy trên **VM**:

```bash
hostnamectl
cat /etc/os-release
printf 'before-snapshot\n' > "$HOME/snapshot-marker.txt"
```

Tắt VM sạch, tạo snapshot tên `clean-24.04`, bật lại rồi đổi marker:

```bash
printf 'after-snapshot\n' > "$HOME/snapshot-marker.txt"
cat "$HOME/snapshot-marker.txt"
```

Restore snapshot và kiểm tra:

```bash
cat "$HOME/snapshot-marker.txt"
```

Từ **HOST**, xác nhận đường truy cập trước khi học SSH hardening. Với host-only:

```bash
ssh LAB_USER@VM_IP
```

Với NAT port-forward:

```bash
ssh -p 2222 LAB_USER@127.0.0.1
```

Nếu VM chưa có SSH server, cài ở SEC-04; tại LAB-01 chỉ cần chứng minh HOST thấy IP host-only bằng ping/TCP theo hypervisor hoặc ghi rõ port-forward đã cấu hình.

**Bằng chứng đạt:** OS là Ubuntu 24.04; sau restore, marker trở lại `before-snapshot`; network plan ghi rõ host-only hay NAT port-forward và `VM_IP`/port sẽ dùng.

**Break/fix:** nếu marker không quay lại, kiểm tra bạn đã restore đúng snapshot và VM đã tắt/được hypervisor xử lý theo yêu cầu. Không tạo snapshot mới đè lên bằng chứng cũ.

**Rollback/reset:** restore `clean-24.04`; nếu snapshot lỗi, xóa riêng VM lab và cài lại từ ISO — không xóa virtual disk chưa xác định.

**Checkpoint:** tự tạo marker A → snapshot → đổi thành B → restore → chứng minh marker là A.

**Feynman:** VM khác VPS thế nào? Snapshot khác backup thế nào? Vì sao NAT giảm nhưng không loại bỏ mọi rủi ro?

<a id="lab-02"></a>

### LAB-02 — Phân biệt HOST, VM và VPS

**Cần trước:** LAB-01. **Mục tiêu:** trước mỗi lệnh, xác định đúng máy, user, thư mục và IP sẽ bị tác động. **Rủi ro:** 🟢.

**Loại suy:** prompt giống bảng tên căn phòng. Giới hạn: prompt có thể tùy chỉnh hoặc giống nhau, nên phải xác minh bằng lệnh.

**Tự hỏi:** Hai cửa sổ terminal cùng hiện `~` có chứng minh chúng đang ở cùng máy không?

**Chốt lại:** không. `~` chỉ là home của user hiện tại trên máy hiện tại.

**Lab — VM:** ghi bộ nhận dạng:

```bash
whoami
hostname
pwd
printf 'shell=%s\n' "$SHELL"
ip -br addr
ip route
```

Đổi hostname VM để dễ nhận biết:

```bash
sudo hostnamectl set-hostname linux-lab
hostname
```

Trong nhật ký, lập bảng:

```text
Môi trường | whoami | hostname | pwd | IP | đường cứu hộ
HOST       | ...    | ...      | ... | ...| VS Code/console
VM         | ...    | linux-lab| ... | ...| hypervisor console
VPS        | chưa dùng         |     |    | provider console
```

**Bằng chứng đạt:** nhìn output, bạn chỉ ra được lệnh tiếp theo tác động HOST hay VM; biết mở console VM khi SSH local lỗi.

**Break/fix:** mở hai terminal, một ở HOST và một SSH vào VM; cố ý `cd /tmp` ở một cửa sổ. Dùng bộ lệnh nhận dạng để tìm đúng cửa sổ, không tạo/xóa file để đoán.

**Rollback/reset:** đổi hostname nếu cần bằng `sudo hostnamectl set-hostname TEN_MOI`; logout/login để prompt cập nhật.

**Fixture cho checkpoint:**

```text
A: whoami=toan; hostname=windows-host; pwd=/c/Users/toan; ip=192.168.1.10
B: whoami=toan; hostname=linux-lab; pwd=/home/toan; ip=192.168.56.20
C: whoami=toan; hostname=iot-vps; pwd=/srv; ip=203.0.113.10
```

**Checkpoint:** phân loại A/B/C là HOST/VM/VPS và nêu ít nhất hai bằng chứng cho mỗi lựa chọn; câu trả lời chỉ dựa vào màu/prompt không đạt.

**Feynman:** local và remote là gì? `pwd` trả lời câu hỏi nào? IP nào có thể thay đổi sau reboot/NAT?

<a id="lab-03"></a>

### LAB-03 — VPS, chi phí, console và cleanup plan

**Cần trước:** LAB-02. **Mục tiêu:** đánh giá VPS mà chưa cần mua, chuẩn bị đường cứu hộ và kế hoạch ngừng tính phí. **Rủi ro:** 🟢/💰 nếu tự tạo tài nguyên.

**Loại suy:** VPS là căn phòng thuê trong datacenter. Giới hạn: nhà cung cấp còn quản lý hypervisor, network, billing và một số firewall bên ngoài máy.

**Tự hỏi:** Xóa hệ điều hành bên trong VPS có chắc hóa đơn dừng không?

**Chốt lại:** không. Phải xóa/terminate tài nguyên trong control panel; disk, snapshot, IP hoặc backup tách rời vẫn có thể tính phí.

**Lab — không cần mua:** chọn một provider và đọc trang tạo VPS, nhưng dừng trước nút xác nhận. Ghi mà không chụp token/secret:

- image Ubuntu 24.04;
- region;
- giá theo giờ/tháng và thuế nếu có;
- public IPv4/IPv6 có tính phí không;
- console/rescue mode ở đâu;
- firewall/security group mặc định;
- snapshot/volume/backup tính phí thế nào;
- thao tác terminate và cách xác nhận hóa đơn.

Tạo checklist:

```text
[ ] Budget alert/hạn mức
[ ] SSH key public đã chuẩn bị
[ ] Provider console đã tìm thấy
[ ] Firewall provider chỉ mở thứ cần thiết
[ ] Snapshot/backup có chủ đích
[ ] Ghi danh sách VPS, disk, snapshot, IP
[ ] Ngày/giờ UTC sẽ terminate
[ ] Kiểm tra hóa đơn sau cleanup
```

**Bằng chứng đạt:** chỉ ra được đường cứu hộ khi SSH mất và mọi loại tài nguyên cần kiểm tra khi cleanup.

**Break/fix:** tình huống “VPS đã terminate nhưng hóa đơn còn tăng”. Liệt kê giả thuyết: volume, snapshot, reserved IP, backup hoặc tài nguyên region khác; kiểm từng mục trong billing inventory.

**Rollback/reset:** nếu đã tạo VPS, terminate theo provider; xóa tài nguyên phụ sau khi xác nhận không chứa dữ liệu cần giữ; không đăng credential vào nhật ký.

**Checkpoint:** trình bày quy trình từ mất SSH → vào console → rollback config → đăng nhập lại, rồi quy trình terminate → kiểm tài nguyên phụ → kiểm billing.

**Feynman:** provider firewall khác UFW thế nào? Console khác SSH thế nào? Vì sao VPS “tắt máy” vẫn có thể tính phí?

---

## Cấp 0 — Terminal, shell và dữ liệu dạng file

<a id="cli-01"></a>

### CLI-01 — Sống sót trong terminal

**Cần trước:** LAB-02. **Mục tiêu:** đọc command/option/argument, dùng trợ giúp, history, Tab và thoát chương trình đúng cách. **Rủi ro:** 🟢.

**Loại suy:** shell là người phiên dịch; terminal là cửa sổ giao tiếp. Giới hạn: shell không phải Linux và có nhiều shell khác Bash.

**Tự hỏi:** Trong `ls -la /etc`, phần nào quyết định chương trình, cách chạy và target?

**Chốt lại:** `ls` là program, `-la` là options, `/etc` là argument.

**Lab — VM:**

```bash
printf 'hello\n'
pwd
ls --help
man pwd
history | tail
printf 'exit=%s\n' "$?"
```

Trong `man`, dùng `/pattern`, `n`, `Shift+N`, `q`. Gõ `jour` rồi nhấn Tab để quan sát completion; đừng chạy lệnh lạ chỉ vì Tab hoàn tất tên.

Chạy foreground process và yêu cầu dừng:

```bash
sleep 300
```

Nhấn `Ctrl+C`, rồi kiểm tra:

```bash
printf 'exit=%s\n' "$?"
```

**Bằng chứng đạt:** thoát `man` bằng `q`, dừng `sleep` bằng `Ctrl+C`, phân tích đúng ba phần của lệnh và đọc exit status.

**Break/fix:** nếu terminal “kẹt” ở `less`/`man`, thử `q`; nếu command foreground đang chạy, thử `Ctrl+C`; không đóng cưỡng bức trước khi xác định chương trình.

**Rollback/reset:** `Ctrl+L` chỉ dọn màn hình; `reset` dùng khi hiển thị terminal hỏng; mở shell mới nếu cần.

**Checkpoint:** tự tra ý nghĩa một option của `ls`, một option của `cp`, thoát tài liệu và giải thích output mà không tìm bài giải sẵn.

**Feynman:** terminal khác shell? Exit status để làm gì? `Ctrl+C` có phải xóa process khỏi máy trong mọi trường hợp không?

<a id="cli-02"></a>

### CLI-02 — Cây thư mục và đường dẫn

**Cần trước:** CLI-01. **Mục tiêu:** dùng path tuyệt đối/tương đối, `.`, `..`, `$HOME`, đọc file bằng công cụ phù hợp. **Rủi ro:** 🟢.

**Loại suy:** filesystem là cây có gốc `/`. Giới hạn: các mount khác nhau có thể được ghép vào cùng cây.

**Tự hỏi:** `/home/LAB_USER/log.txt` và `./log.txt` có luôn là cùng file không?

**Chốt lại:** path tuyệt đối bắt đầu từ `/`; path tương đối bắt đầu từ thư mục hiện tại.

**Lab — VM:**

```bash
pwd
printf 'home=%s\n' "$HOME"
ls -la /
cd "$HOME"
mkdir -p linux-course/cli-02/docs
printf 'sensor ok\n' > linux-course/cli-02/docs/status.txt
cd linux-course/cli-02
pwd
ls -la ./docs
readlink -f ./docs/status.txt
less ./docs/status.txt
```

Nhấn `q` để thoát `less`. Quan sát một số convention:

```bash
ls -ld /home /etc /var/log /srv /tmp
```

`/etc` thường chứa config; `/srv` phù hợp service data/code có chủ đích; log có thể ở `/var/log` hoặc systemd journal. Không suy đoán vị trí chỉ từ convention.

**Bằng chứng đạt:** `readlink -f` trả đúng path tuyệt đối; đọc được file từ thư mục khác bằng path tương đối và tuyệt đối.

**Break/fix:** tạo lỗi `No such file or directory` bằng cách chạy từ thư mục khác. Chẩn đoán bằng `pwd`, `ls` từng tầng và `readlink -f`, không dùng `sudo`.

**Rollback/reset:**

```bash
cd "$HOME"
rm -r -- "$HOME/linux-course/cli-02"
```

Chỉ chạy sau `printf '%s\n' "$HOME/linux-course/cli-02"` và xác nhận đúng target.

Chuẩn bị fixture rồi chuyển khỏi home:

```bash
mkdir -p "$HOME/linux-course/checkpoint"
printf 'checkpoint-ok\n' > "$HOME/linux-course/checkpoint/answer.txt"
cd /tmp
```

**Checkpoint:** từ `/tmp`, đọc file `$HOME/linux-course/checkpoint/answer.txt` bằng một path tuyệt đối và một path tương đối đã tự tính; cả hai phải in `checkpoint-ok`.

**Feynman:** `/` khác `/root`? `.` và `..` nghĩa gì? Vì sao `pwd` là preflight quan trọng?

<a id="cli-03"></a>

### CLI-03 — File, quote, glob và xóa an toàn

**Cần trước:** CLI-02. **Mục tiêu:** tạo/copy/move/xóa file có khoảng trắng, preview glob và nhận biết file ẩn. **Rủi ro:** 🟡.

**Loại suy:** quote là hộp giữ nhiều ký tự thành một đối số. Giới hạn: single quote và double quote xử lý biến/ký tự đặc biệt khác nhau.

**Tự hỏi:** Vì sao `rm report *` nguy hiểm hơn `rm -- "report *"`?

**Chốt lại:** shell mở rộng glob trước khi `rm` chạy; khoảng trắng còn tách đối số nếu không quote.

**Lab — VM:**

```bash
mkdir -p "$HOME/linux-course/cli-03/inbox"
cd "$HOME/linux-course/cli-03/inbox"
printf '22.5\n' > 'sensor one.txt'
printf '23.1\n' > 'sensor two.txt'
printf 'local config\n' > .settings
cp -- 'sensor one.txt' '../sensor one.copy.txt'
mv -- 'sensor two.txt' 'sensor two.renamed.txt'
ls -la
printf 'glob match: %s\n' sensor*.txt
```

Dùng `--` để kết thúc options khi công cụ hỗ trợ. Thử file bắt đầu bằng dấu gạch:

```bash
printf 'edge case\n' > ./-reading.txt
ls -l -- ./-reading.txt
rm -- ./-reading.txt
```

**Bằng chứng đạt:** file có khoảng trắng không bị tách; `.settings` chỉ hiện khi dùng `-a`; glob preview khớp đúng file.

**Break/fix:** chạy `cp sensor one.txt /tmp/` để thấy shell tách sai. Sửa bằng quote và kiểm số argument qua `printf '<%s>\n' 'sensor one.txt'`.

**Rollback/reset:** preview rồi xóa đúng thư mục lab:

```bash
printf 'remove target: %s\n' "$HOME/linux-course/cli-03"
rm -r -- "$HOME/linux-course/cli-03"
```

Không thêm `-f`; lỗi là tín hiệu cần đọc.

**Checkpoint:** tạo cây có file `device A.txt`, `.env.example`, `-note`; copy/move/xóa riêng từng file mà không đổi tên để né quote.

**Feynman:** shell mở rộng glob khi nào? Single quote khác double quote? Vì sao `--` hữu ích?

<a id="cli-04"></a>

### CLI-04 — stdin, stdout, stderr, pipe và redirection

**Cần trước:** CLI-03. **Mục tiêu:** nối lệnh, tách output/lỗi, phân biệt ghi đè/nối thêm và ghi file đặc quyền bằng `tee`. **Rủi ro:** 🟡; `>` có thể ghi đè.

**Loại suy:** pipe là ống nối output tới input. Giới hạn: pipe mặc định chỉ nối stdout; định dạng giữa hai chương trình vẫn phải tương thích.

**Tự hỏi:** Vì sao `sudo printf 'x' > /root/file` vẫn có thể bị `Permission denied`?

**Chốt lại:** shell hiện tại mở file cho `>` trước; `sudo` chỉ áp dụng cho `printf`. Dùng `sudo tee` khi thật sự cần.

**Lab — VM:**

```bash
mkdir -p "$HOME/linux-course/cli-04"
cd "$HOME/linux-course/cli-04"
printf 'ok\nerror\nok\n' > events.log
grep 'ok' events.log | wc -l
ls events.log missing.log >stdout.txt 2>stderr.txt
printf 'first\n' > combined.txt
printf 'second\n' >> combined.txt
cat stdout.txt
cat stderr.txt
cat combined.txt
```

Chứng minh ghi đè trong file lab:

```bash
printf 'before\n' > overwrite-demo.txt
printf 'after\n' > overwrite-demo.txt
cat overwrite-demo.txt
```

Ví dụ quyền nâng cao chỉ trên file lab `/tmp`:

```bash
printf 'owned by root\n' | sudo tee /tmp/cli-04-root.txt >/dev/null
sudo cat /tmp/cli-04-root.txt
```

**Bằng chứng đạt:** `wc` trả `2`; stdout/stderr tách đúng; `combined.txt` có hai dòng; hiểu vì sao `>` mất dòng cũ.

**Break/fix:** chạy pipeline với pattern không tồn tại, kiểm `PIPESTATUS` ngay sau pipeline trong Bash:

```bash
grep 'missing-pattern' events.log | wc -l
printf 'pipeline statuses: %s\n' "${PIPESTATUS[*]}"
```

Output `0` dòng không đồng nghĩa mọi stage thành công theo cùng nghĩa nghiệp vụ.

**Rollback/reset:**

```bash
sudo rm -- /tmp/cli-04-root.txt
rm -r -- "$HOME/linux-course/cli-04"
```

**Checkpoint:** từ một command có cả stdout/stderr, lưu riêng hai luồng, lọc stdout qua pipe và chứng minh exit status của bước quan trọng.

**Feynman:** `>` khác `>>`? Pipe chuyển luồng nào? Tại sao `sudo >` không hoạt động như người mới tưởng?

<a id="cli-05"></a>

### CLI-05 — Lọc và tổng hợp text

**Cần trước:** CLI-04. **Mục tiêu:** dùng `grep`, `wc`, `sort`, `uniq`, `cut`; đọc `awk`/`sed` mà không sửa file gốc. **Rủi ro:** 🟢.

**Loại suy:** pipeline là dây chuyền, mỗi công cụ làm một việc. Giới hạn: text không có schema chặt; delimiter hoặc format đổi có thể làm pipeline sai âm thầm.

**Tự hỏi:** Vì sao `uniq` thường cần `sort` trước khi đếm toàn bộ giá trị trùng?

**Chốt lại:** `uniq` chỉ gộp các dòng giống nhau liền kề.

**Lab — VM:**

```bash
mkdir -p "$HOME/linux-course/cli-05"
cd "$HOME/linux-course/cli-05"
cat > telemetry.csv <<'CSV'
device,status,temperature
sensor-01,ok,22.5
sensor-02,error,999
sensor-01,ok,22.7
sensor-03,error,missing
CSV

grep -n ',error,' telemetry.csv
grep -c ',error,' telemetry.csv
cut -d, -f1 telemetry.csv | tail -n +2 | sort | uniq -c
awk -F, 'NR>1 && $2=="ok" {sum+=$3; n++} END {if(n) print sum/n}' telemetry.csv
sed -n '1,3p' telemetry.csv
```

Không dùng `sed -i` trong bài nền; output ra stdout để giữ file gốc.

**Bằng chứng đạt:** tìm đúng hai dòng lỗi; đếm `sensor-01` hai lần; tính trung bình hai record `ok`.

**Break/fix:** đổi một dòng sang delimiter `;`. Quan sát pipeline CSV trả sai hoặc thiếu; dùng `head`, `grep`, `awk -F` để chứng minh format không đồng nhất trước khi sửa dữ liệu.

**Rollback/reset:** giữ `telemetry.csv` gốc; tạo file sửa mới bằng redirection khác tên. Xóa riêng thư mục lab khi xong.

Fixture checkpoint độc lập:

```bash
cat > checkpoint.csv <<'CSV'
device,status
a,ok
a,error
b,error
b,ok
b,error
CSV
```

**Checkpoint:** từ `checkpoint.csv`, báo cáo `error=3`, device `a=2`, device `b=3`; không sửa input và không đếm header.

**Feynman:** `grep` trả exit status 1 có luôn là lỗi hệ thống? Vì sao pipeline text dễ vỡ? Khi nào cần parser CSV/JSON thật?

<a id="cli-06"></a>

### CLI-06 — Archive, checksum và truyền file

**Cần trước:** CLI-03. **Mục tiêu:** tạo/kiểm/giải nén archive, so checksum và truyền bằng `scp`/`rsync` có dry-run. **Rủi ro:** 🟡.

**Loại suy:** `tar` đóng thùng; gzip nén thùng. Giới hạn: archive không tự là backup nếu cùng nằm trên disk sẽ hỏng và chưa từng restore thử.

**Tự hỏi:** File archive tồn tại có chứng minh dữ liệu phục hồi được không?

**Chốt lại:** không; phải liệt kê, giải nén sang nơi sạch và kiểm nội dung/checksum.

**Lab — VM:**

```bash
mkdir -p "$HOME/linux-course/cli-06/source"
printf 'dashboard config\n' > "$HOME/linux-course/cli-06/source/config.txt"
printf 'sample data\n' > "$HOME/linux-course/cli-06/source/data.txt"
cd "$HOME/linux-course/cli-06"
sha256sum source/* > checksums.txt
tar -czf lab-backup.tar.gz source checksums.txt
tar -tzf lab-backup.tar.gz
mkdir restore
tar -xzf lab-backup.tar.gz -C restore
cd restore
sha256sum -c checksums.txt
```

Đường Core dùng `rsync` local nên chưa phụ thuộc SSH:

```bash
mkdir -p /tmp/cli-06-copy
rsync -avhn -- "$HOME/linux-course/cli-06/source/" /tmp/cli-06-copy/
rsync -avh -- "$HOME/linux-course/cli-06/source/" /tmp/cli-06-copy/
diff -ru "$HOME/linux-course/cli-06/source" /tmp/cli-06-copy
```

Sau SEC-04, mở rộng sang remote; xem dry-run trước:

```bash
rsync -avhn -- "$HOME/linux-course/cli-06/source/" LAB_USER@VM_IP:/tmp/cli-06-copy/
```

Bỏ `n` chỉ sau khi output dry-run đúng. `scp -r` phù hợp copy đơn giản; `rsync` hữu ích cho đồng bộ và dry-run.

**Bằng chứng đạt:** `tar -t` đúng file; checksum restore đều `OK`; local `rsync` dry-run/transfer/diff pass.

**Break/fix:** sửa `restore/source/data.txt`, chạy lại `sha256sum -c`; dùng mismatch làm bằng chứng corruption, không tạo checksum mới để che lỗi.

**Rollback/reset:** xóa riêng `/tmp/cli-06-copy` trên máy đích sau khi `hostname`/`pwd` xác nhận; restore snapshot nếu chọn sai môi trường.

**Checkpoint:** đóng gói một cây file mới, ghi checksum ngoài/nằm cùng archive theo thiết kế, truyền tới thư mục trống và restore verify thành công.

**Feynman:** archive khác backup? Checksum chứng minh điều gì và không chứng minh điều gì? Dấu `/` cuối source của `rsync` ảnh hưởng bố cục đích ra sao?

<a id="cli-07"></a>

### CLI-07 — Git, diff, rollback và không lưu secret

**Cần trước:** CLI-03. **Mục tiêu:** theo dõi code/config mẫu, đọc diff, phục hồi file và loại secret khỏi repo ngay từ đầu. **Rủi ro:** 🟡.

**Loại suy:** Git là lịch sử thay đổi có địa chỉ commit. Giới hạn: Git không phải backup cho dữ liệu runtime và xóa secret ở commit mới không xóa nó khỏi lịch sử cũ.

**Tự hỏi:** Thêm `.env` vào `.gitignore` sau khi commit secret có làm secret biến mất khỏi lịch sử không?

**Chốt lại:** không. Phải rotate/revoke secret và làm sạch lịch sử theo quy trình phù hợp; phòng ngừa tốt hơn chữa.

**Lab — VM:**

```bash
sudo apt update
sudo apt install -y git
mkdir -p "$HOME/linux-course/iot-dashboard"
cd "$HOME/linux-course/iot-dashboard"
git init
git config user.name 'Linux Learner'
git config user.email 'learner@example.invalid'
cat > .gitignore <<'EOF'
.env
*.key
*.pem
__pycache__/
.venv/
EOF
cat > README.md <<'EOF'
# IoT dashboard lab

Không chứa credential thật.
EOF
cat > .env.example <<'EOF'
DB_HOST=127.0.0.1
DB_NAME=iot
DB_USER=CHANGE_ME
DB_PASSWORD=CHANGE_ME
EOF
git add .gitignore README.md .env.example
git diff --cached
git commit -m 'Initialize safe lab repository'
```

Tạo secret giả cục bộ và xác minh bị ignore:

```bash
printf 'DB_PASSWORD=local-only-not-real\n' > .env
git status --short --ignored
git ls-files
```

Thử sửa và phục hồi file theo dõi:

```bash
printf '\nBroken line\n' >> README.md
git diff -- README.md
git restore -- README.md
git status --short
```

**Bằng chứng đạt:** `.env` hiện ignored nhưng không có trong `git ls-files`; diff cho thấy thay đổi; restore chỉ phục hồi file chỉ định.

**Break/fix:** dùng secret giả `DEMO_REVOKE_ME` commit trong repo lab, quan sát nó còn trong `git log -p`; không dùng secret thật. Gỡ file khỏi tracking, commit sửa và giải thích vì sao trong sự cố thật vẫn phải rotate trước.

**Rollback/reset:** `git restore -- FILE` cho thay đổi chưa commit; không dùng `git reset --hard` khi chưa hiểu vì có thể mất nhiều thay đổi. Xóa repo lab chỉ sau khi xác nhận không có file cần giữ.

**Checkpoint:** tạo repo mới có `.gitignore`, `.env.example`, hai commit; sửa sai một file rồi phục hồi riêng file đó; chứng minh không có `.env`, key hoặc password thật trong tracked files/history.

**Feynman:** working tree, staging và commit khác nhau? `.gitignore` áp dụng hồi tố không? Tại sao deploy bằng commit/tag dễ rollback hơn sửa tay?

---

## Cấp 1 — User, quyền và SSH có kỷ luật

<a id="sec-01"></a>

### SEC-01 — User, group, root và `sudo`

**Cần trước:** CLI-03. **Mục tiêu:** làm việc bằng user thường, chỉ nâng quyền cho hành động cần thiết và xác minh identity trước thay đổi. **Rủi ro:** 🟠.

**Loại suy:** root giữ chìa khóa vạn năng; `sudo` cho phép chạy command với identity khác theo policy và thường ghi nhận lần gọi. Giới hạn: một command có thể mở shell/chạy nhiều hành động; log sudo không phải audit bất biến đầy đủ và `sudo` không tự làm lệnh an toàn.

**Tự hỏi:** Vì sao chạy toàn bộ dashboard bằng root làm một lỗi ứng dụng nguy hiểm hơn?

**Chốt lại:** process bị chiếm sẽ có quyền của user chạy nó. Least privilege giới hạn file, process và thiết bị mà kẻ tấn công chạm tới.

**Preflight — VM:** snapshot; kiểm `whoami`, `hostname`; không thử trên VPS trước GATE-VPS.

```bash
whoami
id
groups
sudo -l
sudo adduser labops
sudo usermod -aG sudo labops
id labops
getent group sudo
```

Mở một terminal/console khác và đăng nhập `labops`; group mới chỉ chắc chắn áp dụng trong phiên đăng nhập mới:

```bash
su - labops
id
sudo -v
exit
```

Không dùng `sudo su` làm thói quen. Với app, dùng system user không có shell ở SYS-03.

**Bằng chứng đạt:** `labops` là user riêng, thuộc group `sudo` sau phiên mới; lệnh thường không chạy bằng root; `sudo` yêu cầu xác thực theo policy.

**Break/fix:** nếu `labops` chưa thấy group mới, kiểm `id labops`, đăng xuất/đăng nhập; không lặp `usermod` mù. Nếu sudoers sai, dùng console/recovery và `visudo`, không chỉnh trực tiếp bằng editor tùy ý.

**Rollback/reset:** trước khi xóa user, kiểm process/file của họ. Trong VM lab:

```bash
sudo deluser labops sudo
sudo deluser --remove-home labops
```

**Checkpoint:** tạo user lab khác, cấp quyền sudo, xác minh trong phiên mới, rồi gỡ quyền sudo mà user vẫn đăng nhập thường được.

**Feynman:** user khác process identity thế nào? `sudo` khác đăng nhập root? Vì sao app cần user riêng?

<a id="sec-02"></a>

### SEC-02 — Quyền file, owner, group và umask

**Cần trước:** SEC-01. **Mục tiêu:** đặt quyền tối thiểu và sửa `Permission denied` bằng owner/group/mode đúng, không dùng `chmod 777`. **Rủi ro:** 🟠.

**Loại suy:** owner, group và others là ba nhóm người trước một cửa có quyền đọc/ghi/đi qua. Giới hạn: ACL, capabilities và policy AppArmor còn có thể ảnh hưởng ngoài mode bits.

**Tự hỏi:** Vì sao thư mục cần bit `x` dù bạn chỉ muốn đọc tên file bên trong?

**Chốt lại:** với directory, `x` cho phép traverse; `r` liệt kê tên; `w` tạo/xóa entry khi kết hợp quyền phù hợp.

**Preflight — VM:** chỉ dùng `/srv/iot-permission-lab`; ghi owner/mode hiện tại trước khi sửa.

```bash
sudo groupadd --force iotreaders
sudo usermod -aG iotreaders "$USER"
sudo install -d -o root -g iotreaders -m 0750 /srv/iot-permission-lab
printf 'sample\n' | sudo tee /srv/iot-permission-lab/readings.txt >/dev/null
sudo chown root:iotreaders /srv/iot-permission-lab/readings.txt
sudo chmod 0640 /srv/iot-permission-lab/readings.txt
ls -ld /srv/iot-permission-lab
ls -l /srv/iot-permission-lab/readings.txt
namei -l /srv/iot-permission-lab/readings.txt
```

Đăng xuất/đăng nhập để group mới áp dụng, rồi:

```bash
id
cat /srv/iot-permission-lab/readings.txt
umask
```

Đọc mode: `0640` = owner đọc/ghi, group đọc, others không quyền. Không cấp write nếu app chỉ cần read.

**Bằng chứng đạt:** thành viên `iotreaders` đọc được; user không thuộc group bị từ chối; file không world-readable/writable.

**Break/fix:** bỏ `x` khỏi directory trong snapshot VM, quan sát lỗi rồi dùng `namei -l` tìm tầng chặn. Khôi phục `0750`; không “sửa” bằng `777`.

**Rollback/reset:**

```bash
sudo rm -r -- /srv/iot-permission-lab
sudo gpasswd -d "$USER" iotreaders
sudo groupdel iotreaders
```

**Checkpoint:** tạo directory config cho group lab: owner toàn quyền, group chỉ đọc/traverse, others không quyền; chứng minh cả ca cho phép và từ chối.

**Feynman:** `rwx` trên file khác directory? `chown` khác `chmod`? Vì sao recursive permission change nguy hiểm?

<a id="sec-03"></a>

### SEC-03 — Sửa cấu hình bằng Nano, diff, validate và rollback

**Cần trước:** CLI-04. **Mục tiêu:** sửa một file có backup, xem diff, validate trước khi áp dụng và phục hồi khi sai. **Rủi ro:** 🟡 trong home; 🟠 với `/etc`.

**Loại suy:** sửa config giống sửa bảng điều khiển; backup là bản trước thay đổi. Giới hạn: backup cùng disk không thay thế version control hoặc disaster backup.

**Tự hỏi:** “Editor lưu thành công” có chứng minh cấu hình hợp lệ không?

**Chốt lại:** không. Editor chỉ ghi bytes; parser/service mới quyết định syntax và semantics.

**Lab — VM:**

```bash
mkdir -p "$HOME/linux-course/sec-03"
cd "$HOME/linux-course/sec-03"
cat > app.json <<'JSON'
{
  "bind": "127.0.0.1",
  "port": 8080,
  "log_level": "info"
}
JSON
cp -a app.json app.json.before
nano app.json
```

Trong Nano: `Ctrl+O`, Enter để lưu; `Ctrl+X` để thoát; `Ctrl+W` để tìm. Sau khi đổi `log_level`:

```bash
diff -u app.json.before app.json
python3 -m json.tool app.json >/dev/null
printf 'validate_exit=%s\n' "$?"
```

Với file hệ thống, ưu tiên `sudoedit FILE`: editor chạy dưới user, sau đó `sudo` thay file có kiểm soát.

**Bằng chứng đạt:** diff chỉ có thay đổi dự kiến; JSON validator exit `0`; backup không đổi.

**Break/fix:** xóa một dấu phẩy/quote, chạy validator, đọc line/column; không reload service. Tự sửa rồi validate lại.

**Rollback/reset:**

```bash
cp -a app.json.before app.json
python3 -m json.tool app.json >/dev/null
```

**Checkpoint:** backup một config mới, thay đúng một giá trị, tạo một lỗi syntax, phát hiện trước apply và phục hồi bản hợp lệ.

**Feynman:** syntax khác semantics? Diff bảo vệ khỏi lỗi nào? Vì sao validate phải đứng trước reload?

<a id="sec-04"></a>

### SEC-04 — SSH key và hardening không tự khóa cửa

**Cần trước:** SEC-01, SEC-02, SEC-03. **Mục tiêu:** xác minh host fingerprint, đăng nhập bằng key ở phiên thứ hai, validate SSH config và giữ console cứu hộ. **Rủi ro:** 🔴; chỉ làm VM snapshot trước.

**Loại suy:** public key là ổ khóa được gắn lên server; private key là chìa không rời client. Giới hạn: key bị đánh cắp hoặc không có passphrase vẫn có thể bị lạm dụng; authorization còn phụ thuộc account/policy.

**Tự hỏi:** Tại sao không đóng phiên SSH cũ ngay sau khi sửa config?

**Chốt lại:** phiên cũ là đường rollback nếu config/key/firewall mới sai. Chỉ đóng sau khi phiên hai và `sudo` hoạt động.

**Preflight:** snapshot VM; mở hypervisor console; biết `VM_IP`; giữ terminal SSH thứ nhất nếu đã có. Các block HOST dùng Bash trong WSL/Git Bash; với PowerShell, cú pháp biến/path khác.

**VM console — cài và kiểm SSH server:**

```bash
sudo apt update
sudo apt install -y openssh-server
sudo systemctl enable --now ssh
sudo /usr/sbin/sshd -t
sudo ss -lntp 'sport = :22'
```

Nếu bạn dùng port-forward 2222 ở LAB-01, các lệnh HOST thêm `-p 2222`.

**HOST — tạo key có passphrase:**

```bash
ssh-keygen -t ed25519 -a 64 -f "$HOME/.ssh/linux_course_ed25519" -C 'linux-course'
```

**VM console — xem fingerprint host:**

```bash
sudo ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub
```

So với fingerprint client hiển thị ở lần kết nối đầu; không gõ `yes` chỉ vì địa chỉ “có vẻ đúng”. Cài public key bằng `ssh-copy-id` nếu có:

```bash
ssh-copy-id -i "$HOME/.ssh/linux_course_ed25519.pub" LAB_USER@VM_IP
ssh -i "$HOME/.ssh/linux_course_ed25519" LAB_USER@VM_IP
```

Trong **phiên thứ hai**, kiểm:

```bash
whoami
hostname
sudo -v
```

Chỉ sau đó mới harden trong **VM**. Ubuntu đọc drop-in theo thứ tự và `sshd` thường giữ giá trị đầu tiên, nên kiểm effective config thay vì tin tên file:

```bash
sudo tee /etc/ssh/sshd_config.d/00-linux-course-hardening.conf >/dev/null <<'EOF'
PermitRootLogin no
PasswordAuthentication no
KbdInteractiveAuthentication no
PubkeyAuthentication yes
EOF
sudo /usr/sbin/sshd -t
sudo /usr/sbin/sshd -T | grep -E '^(permitrootlogin|passwordauthentication|kbdinteractiveauthentication|pubkeyauthentication) '
sudo systemctl reload ssh
```

Từ **HOST**, xác nhận negative path bằng một phiên riêng. Lệnh này phải bị từ chối sau khi bạn thử nhập password; không thêm `|| true` vào bằng chứng:

```bash
ssh -o PubkeyAuthentication=no -o PreferredAuthentications=password,keyboard-interactive LAB_USER@VM_IP
```

Sau đó xác nhận positive path vẫn thành công:

```bash
ssh -i "$HOME/.ssh/linux_course_ed25519" LAB_USER@VM_IP
```

Mở **phiên thứ ba** bằng key. Thử password login từ client mới phải bị từ chối; key login phải thành công.

**Bằng chứng đạt:** host fingerprint đã so; key login và `sudo` ở phiên mới pass; `sshd -t` exit 0; `sshd -T` in `kbdinteractiveauthentication no`, `passwordauthentication no`, `pubkeyauthentication yes`; negative password-only login bị từ chối; console và phiên cũ vẫn sẵn sàng.

**Break/fix:** trong snapshot VM, tạo drop-in có directive sai, chạy `sshd -t` và quan sát reload không được phép. Không restart/reboot. Xóa/sửa file từ phiên cũ.

**Rollback/reset:**

```bash
sudo rm -- /etc/ssh/sshd_config.d/00-linux-course-hardening.conf
sudo /usr/sbin/sshd -t
sudo systemctl reload ssh
```

Nếu mất SSH, dùng console VM/provider, restore drop-in hoặc snapshot; không tạo VPS mới trước khi hiểu lỗi.

**Checkpoint:** từ VM sạch, cài key, kiểm fingerprint, harden, thử key/password bằng phiên mới, rồi rollback mà không mất quyền truy cập.

**Feynman:** private/public key ở đâu? Fingerprint giải quyết rủi ro gì? `sshd -t`, `sshd -T` và login test khác nhau?

---

## Cấp 2 — Package, process, service và hệ điều hành

<a id="sys-01"></a>

### SYS-01 — APT, repository và candidate version

**Cần trước:** SEC-01. **Mục tiêu:** làm mới metadata, kiểm nguồn/candidate, cài và gỡ package có bằng chứng. **Rủi ro:** 🟠.

**Loại suy:** repository là kho đã cấu hình; `apt update` tải mục lục kho. Giới hạn: package trong repo không nhất thiết là phiên bản mới nhất trên Internet hoặc không có lỗ hổng.

**Tự hỏi:** `apt update` có nâng cấp chương trình đang cài không?

**Chốt lại:** không; nó làm mới package index từ các repository đã cấu hình. `apt upgrade` mới tính thay đổi package và cần đọc kế hoạch trước.

**Lab — VM:**

```bash
sudo apt update
apt-cache policy jq
apt-cache show jq | less
sudo apt install jq
command -v jq
jq --version
dpkg -s jq | grep -E '^(Status|Version):'
```

Đọc danh sách thay đổi trước khi xác nhận; không tự động chạy `upgrade` chỉ vì bài hướng dẫn cài một tool.

**Bằng chứng đạt:** biết repository/candidate/installed version; package status là `install ok installed`; binary chạy được.

**Break/fix:** gõ sai tên package và đọc `Unable to locate package`; kiểm spelling, `apt search`, index và repository thay vì tải script ngẫu nhiên.

**Rollback/reset:**

```bash
sudo apt remove jq
sudo apt autoremove --dry-run
```

Chỉ bỏ `--dry-run` sau khi danh sách đúng; package khác có thể phụ thuộc tool, nên không purge mù.

**Checkpoint:** chọn một package nhỏ, kiểm policy/source/candidate, cài, xác minh binary và gỡ có kiểm soát.

**Feynman:** `update` khác `upgrade`? Candidate version từ đâu? Vì sao script `curl | sh` cần đánh giá kỹ hơn?

<a id="sys-02"></a>

### SYS-02 — Process, job, signal, session và tmux

**Cần trước:** CLI-01. **Mục tiêu:** tìm process, quản lý job và dùng SIGTERM trước SIGKILL; chọn tmux cho tương tác, systemd cho service. **Rủi ro:** 🟡.

**Loại suy:** process là một lần chương trình đang chạy; PID là số ca làm. Giới hạn: nhiều process/threads có thể cùng tạo một dịch vụ.

**Tự hỏi:** Mất SSH có làm mọi process chắc chắn chết không?

**Chốt lại:** không. Foreground job gắn terminal có thể nhận SIGHUP/mất I/O, nhưng hành vi phụ thuộc shell/process/cách khởi chạy. Workload lâu dài phải có process manager.

**Lab — VM:**

```bash
sleep 300 &
LAB_PID=$!
printf 'pid=%s\n' "$LAB_PID"
jobs -l
ps -o pid,ppid,user,stat,etime,cmd -p "$LAB_PID"
kill -TERM "$LAB_PID"
wait "$LAB_PID"
printf 'wait_exit=%s\n' "$?"
```

Nếu cần phiên tương tác dài:

```bash
sudo apt install -y tmux
tmux new -s lab
```

Trong tmux chạy `watch -n 2 date -u`; nhấn `Ctrl+B`, rồi `d` để detach; nối lại:

```bash
tmux attach -t lab
```

Thoát process rồi `exit` để đóng session. Không dùng tmux thay systemd cho app production.

**Bằng chứng đạt:** tìm đúng PID; TERM dừng process; tmux detach/attach giữ phiên tương tác.

**Break/fix:** tạo process cố ý bỏ qua TERM trong VM, rồi chỉ dùng KILL sau khi đã xác minh TERM không có tác dụng:

```bash
python3 - <<'PY' &
import signal
import time
signal.signal(signal.SIGTERM, signal.SIG_IGN)
while True:
    time.sleep(1)
PY
IGNORE_PID=$!
kill -TERM "$IGNORE_PID"
sleep 1
ps -p "$IGNORE_PID"
kill -KILL "$IGNORE_PID"
wait "$IGNORE_PID"
printf 'wait_exit=%s\n' "$?"
```

Bằng chứng: process còn sau TERM, biến mất sau KILL; `wait` phản ánh kết thúc bởi signal. KILL không cho process cleanup.

**Rollback/reset:** dừng job/tmux session lab; không `kill` theo tên chung có thể trúng process khác.

**Checkpoint:** chạy hai `sleep`, dừng đúng một PID bằng TERM, chứng minh process kia còn; tạo/reconnect một tmux session.

**Feynman:** program khác process? TERM khác KILL? tmux khác systemd?

<a id="sys-03"></a>

### SYS-03 — systemd service cho simulator

**Cần trước:** SYS-02, SEC-02. **Mục tiêu:** chạy simulator non-root, có working directory, journal, restart policy và tự khởi động. **Rủi ro:** 🟠.

**Loại suy:** systemd là quản lý ca; `enable` xếp lịch vào boot, `start` cho làm ngay, `Restart=` xử lý một số crash. Giới hạn: restart không sửa bug, không bảo đảm dependency/data khỏe và có thể tạo crash loop.

**Tự hỏi:** `systemctl enable` có làm service tự sống lại sau mọi crash không?

**Chốt lại:** không. `enable` tạo liên kết vào target; restart behavior do unit và loại failure quyết định.

**Preflight — VM:** snapshot; target chỉ `/srv/iot-simulator`, `/etc/systemd/system/iot-simulator.service` và system user `iotapp`.

```bash
id -u iotapp >/dev/null 2>&1 || sudo useradd --system --home /srv/iot-simulator --shell /usr/sbin/nologin iotapp
sudo install -d -o root -g iotapp -m 0750 /srv/iot-simulator
sudo tee /srv/iot-simulator/simulator.py >/dev/null <<'PY'
import json
import os
import random
import time
from datetime import datetime, timezone

device_id = os.environ.get("DEVICE_ID", "sensor-001")

while True:
    row = {
        "device_id": device_id,
        "temperature": round(random.uniform(20, 30), 1),
        "humidity": round(random.uniform(40, 70), 1),
        "server_time": datetime.now(timezone.utc).isoformat(),
    }
    print(json.dumps(row), flush=True)
    time.sleep(5)
PY
sudo chown root:iotapp /srv/iot-simulator/simulator.py
sudo chmod 0640 /srv/iot-simulator/simulator.py
sudo tee /etc/systemd/system/iot-simulator.service >/dev/null <<'UNIT'
[Unit]
Description=IoT telemetry simulator lab
After=network.target

[Service]
Type=simple
User=iotapp
Group=iotapp
WorkingDirectory=/srv/iot-simulator
ExecStart=/usr/bin/python3 /srv/iot-simulator/simulator.py
Restart=on-failure
RestartSec=3
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true

[Install]
WantedBy=multi-user.target
UNIT
sudo systemd-analyze verify /etc/systemd/system/iot-simulator.service
sudo systemctl daemon-reload
sudo systemctl enable --now iot-simulator
sudo systemctl status iot-simulator --no-pager
sudo journalctl -u iot-simulator -n 5 --no-pager
```

Xác minh user/process:

```bash
systemctl show iot-simulator -p MainPID -p User -p Restart
ps -o user,pid,ppid,cmd -p "$(systemctl show -p MainPID --value iot-simulator)"
```

Thử restart policy trong VM:

```bash
sudo kill -KILL "$(systemctl show -p MainPID --value iot-simulator)"
sleep 4
systemctl show iot-simulator -p ActiveState -p MainPID
```

**Bằng chứng đạt:** verify pass; active; process user `iotapp`; journal có JSON; PID mới sau crash; `is-enabled` trả `enabled`.

**Break/fix:** backup unit, đổi `ExecStart` sang path sai, daemon-reload/restart và dùng journal tìm `No such file`; không sửa nhiều directive cùng lúc.

**Rollback/reset:** restore unit backup hoặc dừng/gỡ lab:

```bash
sudo systemctl disable --now iot-simulator
sudo rm -- /etc/systemd/system/iot-simulator.service
sudo systemctl daemon-reload
sudo systemctl reset-failed
```

Chỉ xóa `/srv/iot-simulator` khi không cần simulator cho các bài sau.

**Checkpoint:** từ script mới, tạo unit non-root, verify, enable/start, chứng minh journal/reboot/restart-after-crash và sửa một lỗi `ExecStart`.

**Feynman:** start/enable/restart policy khác nhau? Vì sao `WorkingDirectory` và `User` quan trọng? Crash loop gây hại gì?

<a id="sys-04"></a>

### SYS-04 — Đọc log và tìm nguyên nhân

**Cần trước:** SYS-03. **Mục tiêu:** lọc journal theo unit, boot, time và priority; phân biệt status summary với nguyên nhân. **Rủi ro:** 🟢.

**Loại suy:** log là dấu vết, không phải chân lý đầy đủ. Giới hạn: app có thể log thiếu, sai level hoặc làm lộ secret.

**Tự hỏi:** Dòng đỏ cuối `systemctl status` có luôn là nguyên nhân gốc không?

**Chốt lại:** không; status chỉ trích một đoạn. Cần time window, unit và sự kiện trước lỗi.

**Lab — VM:**

```bash
sudo systemctl status iot-simulator --no-pager
sudo journalctl -u iot-simulator -n 20 --no-pager
sudo journalctl -u iot-simulator --since '10 minutes ago' --no-pager
sudo journalctl -u iot-simulator -p warning..alert --no-pager
sudo journalctl -b -u iot-simulator --no-pager
journalctl --list-boots
```

Dừng/start để tạo sự kiện có thời gian biết trước:

```bash
date -u
sudo systemctl restart iot-simulator
sudo journalctl -u iot-simulator --since '1 minute ago' --no-pager
```

**Bằng chứng đạt:** trích đúng dòng trong khoảng UTC; chỉ ra lifecycle message của systemd và stdout JSON của app.

**Break/fix:** dùng lỗi unit từ SYS-03; ghi symptom, time, command và dòng evidence quyết định. Không kết luận chỉ từ “failed”.

**Rollback/reset:** sửa unit/script, validate rồi restart; dùng `journalctl` xác minh không còn lỗi mới. Không xóa journal để “làm sạch”.

**Checkpoint:** người khác tạo một lỗi service; trong 10 phút, tìm nguyên nhân bằng journal và đưa đúng ba dòng bằng chứng, không sửa trước khi đo.

**Feynman:** log khác metric? `-b` nghĩa gì? Vì sao timestamp UTC hữu ích?

<a id="sys-05"></a>

### SYS-05 — PATH, environment, config và secret

**Cần trước:** SYS-03, CLI-04. **Mục tiêu:** phân biệt “command not found”, biến thiếu, config sai và secret handling; cấp quyền config cho đúng service user. **Rủi ro:** 🟠.

**Loại suy:** PATH là danh sách kệ mà shell tìm executable; environment là tờ cấu hình process nhận lúc khởi động. Giới hạn: biến môi trường không tự là secret vault và có thể lộ qua debug/dump/quyền cao.

**Tự hỏi:** File `.env` nằm ngoài Git có tự động an toàn không?

**Chốt lại:** không; còn quyền file, backup, process/log exposure, rotation và nơi phân phối.

**Lab — VM:**

```bash
printf '%s\n' "$PATH" | tr ':' '\n'
command -v python3
type -a python3
sudo install -d -o root -g iotapp -m 0750 /etc/iot-dashboard
sudo tee /etc/iot-dashboard/simulator.env >/dev/null <<'EOF'
DEVICE_ID=sensor-001
LOG_LEVEL=info
EOF
sudo chown root:iotapp /etc/iot-dashboard/simulator.env
sudo chmod 0640 /etc/iot-dashboard/simulator.env
sudo install -d -m 0755 /etc/systemd/system/iot-simulator.service.d
sudo tee /etc/systemd/system/iot-simulator.service.d/environment.conf >/dev/null <<'EOF'
[Service]
EnvironmentFile=/etc/iot-dashboard/simulator.env
EOF
sudo systemd-analyze verify /etc/systemd/system/iot-simulator.service
sudo systemctl daemon-reload
sudo systemctl restart iot-simulator
sudo systemctl show iot-simulator -p EnvironmentFiles
sudo journalctl -u iot-simulator -n 10 --no-pager | grep 'sensor-001'
```

Các giá trị trên không phải secret. Với password/token thật, không in bằng `systemctl show`, log hoặc `env`; dùng credential/secret mechanism phù hợp và quyền tối thiểu ở OPS-06.

**Bằng chứng đạt:** `command -v` tìm đúng binary; service đọc file được; mode `0640`, owner `root:iotapp`; Git không track file secret.

**Break/fix:** trong subshell, đặt PATH thiếu `/usr/bin`:

```bash
(
  PATH=/tmp
  python3 --version
  printf 'exit=%s\n' "$?"
)
```

Dùng `command -v`, PATH và absolute path phân biệt lỗi tìm command với lỗi app.

**Rollback/reset:** xóa drop-in rồi reload/restart; chỉ xóa config khi service không còn phụ thuộc:

```bash
sudo rm -- /etc/systemd/system/iot-simulator.service.d/environment.conf
sudo systemctl daemon-reload
sudo systemctl restart iot-simulator
sudo rm -- /etc/iot-dashboard/simulator.env
```

**Checkpoint:** tạo config non-secret và một secret giả; đặt quyền khác nhau, chứng minh app user đọc đúng file và user không thuộc group bị từ chối; không log giá trị.

**Feynman:** PATH dùng lúc nào? Environment được kế thừa ra sao? Vì sao rotate/revoke quan trọng hơn “giấu file”?

<a id="sys-06"></a>

### SYS-06 — systemd timer và cron

**Cần trước:** SYS-03, SYS-04. **Mục tiêu:** chạy job định kỳ có log, trạng thái và hành vi missed-run rõ; so sánh cron. **Rủi ro:** 🟠.

**Loại suy:** timer là lịch hẹn, service oneshot là công việc. Giới hạn: timer không bảo đảm job nghiệp vụ thành công hoặc không chạy chồng nếu thiết kế sai.

**Tự hỏi:** Vì sao một dòng cron chạy tốt bằng tay có thể thất bại theo lịch?

**Chốt lại:** môi trường/PATH/working directory/user khác và output không được theo dõi đúng.

**Lab — VM:**

```bash
sudo tee /etc/systemd/system/iot-health-log.service >/dev/null <<'UNIT'
[Unit]
Description=Record IoT simulator health

[Service]
Type=oneshot
ExecStart=/usr/bin/systemctl is-active --quiet iot-simulator.service
UNIT
sudo tee /etc/systemd/system/iot-health-log.timer >/dev/null <<'TIMER'
[Unit]
Description=Check IoT simulator every minute

[Timer]
OnCalendar=*-*-* *:*:00
Persistent=true
Unit=iot-health-log.service

[Install]
WantedBy=timers.target
TIMER
sudo systemd-analyze verify /etc/systemd/system/iot-health-log.service /etc/systemd/system/iot-health-log.timer
sudo systemctl daemon-reload
sudo systemctl enable --now iot-health-log.timer
systemctl list-timers iot-health-log.timer --all
sudo systemctl start iot-health-log.service
sudo journalctl -u iot-health-log.service -n 20 --no-pager
```

`Persistent=true` cho calendar timer kiểm tra lần lẽ ra đã chạy gần nhất khi máy tắt và có thể kích hoạt sau khi bật lại; nó không lưu mọi lần bỏ lỡ và không biến timer thành job queue bền vững.

**Bằng chứng đạt:** timer có `NEXT/LAST`; service oneshot exit 0 khi simulator active; journal ghi lần chạy.

**Break/fix:** dừng simulator, chạy health service và quan sát failed; xác định dependency hỏng, không “sửa” timer.

**Rollback/reset:**

```bash
sudo systemctl disable --now iot-health-log.timer
sudo rm -- /etc/systemd/system/iot-health-log.timer /etc/systemd/system/iot-health-log.service
sudo systemctl daemon-reload
sudo systemctl reset-failed
```

**Checkpoint:** tạo timer cho script lab, chứng minh success/failure trong journal và giải thích lựa chọn timer thay cron.

**Feynman:** timer và service chia vai trò thế nào? `Persistent` không bảo đảm gì? Làm sao tránh job chạy chồng?

<a id="sys-07"></a>

### SYS-07 — UTC, NTP và timestamp thiết bị

**Cần trước:** LAB-02. **Mục tiêu:** xác minh đồng bộ giờ, lưu UTC và phân biệt `device_time` với `received_at`. **Rủi ro:** 🟠 khi đổi time service; lab chủ yếu 🟢.

**Loại suy:** server time là đồng hồ ở trạm nhận; device time là đồng hồ trên kiện hàng. Giới hạn: NTP server không sửa đồng hồ của cảm biến đang offline hoặc cấu hình sai.

**Tự hỏi:** Nếu sensor gửi timestamp năm 2099, có nên ghi đó là thời điểm nhận dữ liệu không?

**Chốt lại:** không. Giữ thời điểm thiết bị khai báo riêng và thời điểm server nhận đáng tin hơn; validate/quarantine timestamp bất thường.

**Lab — VM:**

```bash
timedatectl status
date -u --iso-8601=seconds
systemctl status systemd-timesyncd --no-pager
```

Nếu NTP tắt trong VM:

```bash
sudo timedatectl set-timezone UTC
sudo timedatectl set-ntp true
timedatectl status
```

Tạo record minh họa:

```bash
python3 - <<'PY'
import json
from datetime import datetime, timezone
payload = {"device_id": "sensor-001", "device_time": "2099-01-01T00:00:00Z"}
payload["received_at"] = datetime.now(timezone.utc).isoformat()
print(json.dumps(payload, indent=2))
PY
```

Policy Core: parse timezone-aware; lưu UTC; từ chối/quarantine timestamp quá xa; luôn ghi `received_at` server.

**Bằng chứng đạt:** timezone UTC; NTP synchronized sau khi chờ hợp lý; record có hai field không bị trộn.

**Break/fix:** input timestamp thiếu timezone hoặc sai format. App phải 4xx/quarantine, không tự đoán local timezone.

**Rollback/reset:** timezone có thể đổi lại bằng `timedatectl set-timezone REGION/CITY`, nhưng database Core vẫn lưu UTC; không chỉnh giờ tay trên VPS có NTP nếu không có kế hoạch.

**Checkpoint:** phân loại năm payload: hợp lệ, thiếu timezone, tương lai xa, quá cũ, thiếu timestamp; ghi `received_at` cho mọi request được quan sát.

**Feynman:** timezone khác timestamp? NTP giải quyết lớp nào? Vì sao IoT cần cả hai thời điểm?

<a id="sys-08"></a>

### SYS-08 — Disk, filesystem, mount và `fstab` an toàn

**Cần trước:** CLI-03, LAB-01. **Mục tiêu:** quan sát dung lượng/mount và kiểm một entry `fstab` trên image loop trong snapshot trước reboot. **Rủi ro:** 🔴; chỉ VM snapshot.

**Loại suy:** disk là đất, partition/volume là lô, filesystem là cách tổ chức kho, mount là gắn cửa kho vào cây `/`. Giới hạn: LVM, RAID, network filesystem và container volume thêm tầng khác.

**Tự hỏi:** Vì sao `df` và `du` có thể không bằng nhau?

**Chốt lại:** deleted-open files, reserved blocks, mount khác, sparse file và phạm vi đo có thể làm khác biệt.

**Preflight:** snapshot `before-fstab`; mở console VM; backup file; tuyệt đối không chọn `/dev/sda`, `/dev/nvme*` hoặc disk thật theo ví dụ mù.

```bash
lsblk -f
df -hT
du -sh "$HOME"
findmnt
sudo cp -a /etc/fstab /etc/fstab.before-linux-course
sudo truncate -s 128M /var/tmp/iot-lab.img
sudo mkfs.ext4 -F /var/tmp/iot-lab.img
sudo install -d -m 0755 /mnt/iot-lab
printf '%s\n' '/var/tmp/iot-lab.img /mnt/iot-lab ext4 loop,nofail 0 0' | sudo tee -a /etc/fstab
sudo findmnt --verify --verbose
sudo mount -a
findmnt /mnt/iot-lab
```

Ghi marker rồi unmount/mount lại:

```bash
printf 'mount works\n' | sudo tee /mnt/iot-lab/marker.txt >/dev/null
sudo umount /mnt/iot-lab
sudo mount -a
sudo cat /mnt/iot-lab/marker.txt
```

**Bằng chứng đạt:** verify không báo lỗi fatal; `mount -a` exit 0 trước reboot; `findmnt` đúng source/target/type; marker còn.

**Break/fix:** trong snapshot, đổi `ext4` thành `ext44`, chạy `findmnt --verify` và `mount -a`; **không reboot**. Dùng diff với backup, phục hồi rồi validate lại.

**Rollback/reset:** chỉ xóa backing image sau khi unmount thành công:

```bash
(
  set -Eeuo pipefail
  if mountpoint -q /mnt/iot-lab; then
    sudo umount /mnt/iot-lab
  fi
  sudo cp -a /etc/fstab.before-linux-course /etc/fstab
  sudo findmnt --verify --verbose
  sudo mount -a
  sudo rm -- /var/tmp/iot-lab.img /etc/fstab.before-linux-course
  sudo rmdir /mnt/iot-lab
)
```

Nếu `umount`, verify hoặc `mount -a` lỗi, subshell dừng trước thao tác xóa nhưng phiên SSH không thoát. Đọc process đang dùng mount bằng `sudo fuser -vm /mnt/iot-lab`; không thêm force/lazy unmount chỉ để cleanup tiếp.

Nếu VM không boot, dùng console/recovery hoặc restore snapshot.

**Checkpoint:** tạo image/mountpoint khác, thêm entry, validate/mount/reboot VM, rồi cố ý sai và phục hồi từ console mà không chạm disk hệ thống thật.

**Feynman:** disk/filesystem/mount khác nhau? `nofail` đánh đổi gì? Vì sao RAID/snapshot không thay backup?

<a id="sys-09"></a>

### SYS-09 — Boot, target và recovery

**Cần trước:** SYS-03, SYS-04, SYS-08. **Mục tiêu:** đọc boot journal, target/dependency, xử lý một unit failed từ console. **Rủi ro:** 🔴; chỉ VM snapshot.

**Loại suy:** boot là chuỗi bàn giao từ firmware tới kernel rồi userspace/systemd. Giới hạn: không phải mọi service failed đều chặn boot; `degraded` vẫn có thể đăng nhập.

**Tự hỏi:** `systemctl is-system-running` trả `degraded` có nghĩa kernel không khởi động được không?

**Chốt lại:** không; thường có ít nhất một unit failed. Cần xem unit và dependency cụ thể.

**Lab — VM:**

```bash
systemctl get-default
systemctl is-system-running || true
systemctl --failed --no-pager
systemd-analyze time
systemd-analyze critical-chain
sudo journalctl -b -p warning..alert --no-pager
journalctl --list-boots
```

Tạo unit lỗi **không chặn boot**:

```bash
sudo tee /etc/systemd/system/iot-boot-lab.service >/dev/null <<'UNIT'
[Unit]
Description=Intentional non-critical boot failure lab

[Service]
Type=oneshot
ExecStart=/bin/false
RemainAfterExit=yes

[Install]
WantedBy=multi-user.target
UNIT
sudo systemd-analyze verify /etc/systemd/system/iot-boot-lab.service
sudo systemctl daemon-reload
sudo systemctl enable iot-boot-lab
sudo systemctl start iot-boot-lab || true
systemctl --failed --no-pager
sudo journalctl -u iot-boot-lab -b --no-pager
```

Trong VM, reboot từ console nếu checkpoint yêu cầu; sau boot, kiểm `journalctl -b` và `-b -1`.

**Bằng chứng đạt:** xác định unit failed, exit code và boot hiện tại/trước; biết service không critical vì máy vẫn đạt multi-user target.

**Break/fix:** thay `/bin/false` bằng `/bin/true`, verify, daemon-reload, restart, reset-failed; chứng minh trạng thái trở lại running.

**Rollback/reset:**

```bash
sudo systemctl disable --now iot-boot-lab
sudo rm -- /etc/systemd/system/iot-boot-lab.service
sudo systemctl daemon-reload
sudo systemctl reset-failed
```

Nếu lỗi thật làm mất boot, dùng hypervisor/provider console, recovery target hoặc snapshot; không sửa tiếp qua một SSH không ổn định.

**Checkpoint:** tạo một unit non-critical failed qua reboot, tìm bằng boot journal, sửa và xác minh `systemctl --failed` sạch.

**Feynman:** target khác service? `enable` tạo quan hệ gì? Boot journal giúp phân biệt lỗi hiện tại và boot trước thế nào?

---

## Cấp 3 — Mạng, DNS, firewall và giao thức

<a id="net-01"></a>

### NET-01 — IP, route, TCP/UDP, socket, port và bind address

**Cần trước:** LAB-02, SYS-02. **Mục tiêu:** xác định process đang nghe giao thức/địa chỉ/port nào và máy gửi packet qua route nào. **Rủi ro:** 🟢.

**Loại suy:** IP là địa chỉ tòa nhà, port là số quầy, socket là điểm giao tiếp thật của process. Giới hạn: NAT, IPv6, nhiều interface và proxy làm đường đi phức tạp hơn phép ví.

**Tự hỏi:** App nghe `127.0.0.1:8080` có truy cập trực tiếp từ HOST qua `VM_IP:8080` được không?

**Chốt lại:** không. Loopback chỉ nhận kết nối phát sinh trong cùng network namespace; muốn nhận từ interface khác phải bind phù hợp, nhưng public bind cũng tăng bề mặt tấn công.

**Lab — VM:**

```bash
ip -br addr
ip route
ip route get 1.1.1.1
ss -lntup
python3 -m http.server 8080 --bind 127.0.0.1
```

Giữ server chạy; từ terminal VM thứ hai:

```bash
ss -lntp 'sport = :8080'
curl -I http://127.0.0.1:8080
```

Từ **HOST**, thử `curl http://VM_IP:8080`; dự kiến không kết nối vì bind loopback. Dừng server bằng `Ctrl+C`, rồi chỉ trong mạng VM lab:

```bash
python3 -m http.server 8080 --bind 0.0.0.0
```

Kiểm lại bằng `ss` và HOST. `0.0.0.0` nghĩa là mọi địa chỉ IPv4 local, không có nghĩa mọi client Internet chắc chắn tới được; firewall/NAT vẫn tham gia.

**Bằng chứng đạt:** phân biệt `127.0.0.1:8080` với `0.0.0.0:8080`; chỉ đúng PID/listener và route mặc định.

**Break/fix:** HOST không vào được dù bind `0.0.0.0`; kiểm VM network mode, IP, listener và host firewall theo từng lớp, không tắt toàn bộ firewall để thử.

**Rollback/reset:** `Ctrl+C`; xác minh `ss -lnt 'sport = :8080'` không còn listener.

**Checkpoint:** tạo listener loopback rồi all-interface, dự đoán và kiểm từ VM/HOST; giải thích khác nhau bằng output `ss`.

**Feynman:** IP khác port? Listener khác firewall rule? TCP khác UDP ở mức bài này cần biết gì?

<a id="net-02"></a>

### NET-02 — DNS, A, AAAA, TTL và cache

**Cần trước:** NET-01. **Mục tiêu:** truy vấn DNS bằng nhiều resolver và phát hiện A/AAAA sai hoặc cache cũ. **Rủi ro:** 🟢 khi chỉ truy vấn.

**Loại suy:** DNS là danh bạ tên → dữ liệu. Giới hạn: DNS phân tán, có cache và nhiều record; nó không biết app có hoạt động hay không.

**Tự hỏi:** Domain trả đúng A record có bảo đảm mọi client vào web được không?

**Chốt lại:** không. Client có thể ưu tiên AAAA sai; route/firewall/listener/TLS/app còn có thể lỗi.

**Lab — VM:**

```bash
sudo apt update
sudo apt install -y dnsutils
getent ahosts example.com
dig example.com A +noall +answer
dig example.com AAAA +noall +answer
dig example.com A +trace
resolvectl status
```

So sánh resolver hệ thống và resolver công khai:

```bash
dig @1.1.1.1 example.com A +noall +answer
dig @8.8.8.8 example.com A +noall +answer
```

Không sửa `/etc/resolv.conf` bằng tay trên hệ được quản lý bởi `systemd-resolved` nếu chưa hiểu owner của file.

**Bằng chứng đạt:** đọc được name, type, value, TTL; kiểm A và AAAA riêng; biết `getent` phản ánh đường phân giải mà ứng dụng hệ thống thường dùng.

**Break/fix:** dùng một hostname cố ý không tồn tại và phân biệt `NXDOMAIN` với timeout. Với case AAAA cũ, so `curl -4` và `curl -6` chỉ để chẩn đoán, không coi ép IPv4 là sửa gốc.

**Rollback/reset:** bài chỉ đọc; nếu đã đổi DNS lab, phục hồi qua công cụ quản lý network rồi kiểm `resolvectl`.

**Checkpoint:** chọn domain có A/AAAA, ghi kết quả từ resolver hệ thống và hai resolver public, giải thích một khác biệt do TTL/cache nếu quan sát được.

**Feynman:** A khác AAAA? TTL ảnh hưởng gì? DNS thành công không chứng minh lớp nào phía sau?

<a id="net-03"></a>

### NET-03 — Chọn đúng công cụ chẩn đoán mạng

**Cần trước:** NET-01. **Mục tiêu:** dùng `ss`, `ping`, `curl`, DNS tool đúng câu hỏi và phân loại lỗi listener, reachability, TLS, HTTP. **Rủi ro:** 🟢.

**Loại suy:** mỗi công cụ là một đồng hồ đo cho một tầng. Giới hạn: một phép đo thành công không chứng minh toàn bộ đường đi.

**Tự hỏi:** `ping` thất bại có chứng minh server tắt hoặc port 443 đóng không?

**Chốt lại:** không. ICMP có thể bị chặn; ping không tạo kết nối TCP 443 và không kiểm HTTP/TLS.

**Lab — VM:** khởi động HTTP server loopback từ NET-01 rồi đo. Lệnh tới port 65534 dự kiến thất bại; chạy riêng và ghi exit status thay vì che bằng `|| true`:

```bash
ss -lntp 'sport = :8080'
curl -v --max-time 5 http://127.0.0.1:8080/
curl -v --max-time 5 http://127.0.0.1:65534/
printf 'refused_exit=%s\n' "$?"
ping -c 2 127.0.0.1
getent hosts localhost
```

Đọc thứ tự `curl -v`: resolve → connect → request → response. HTTP `500` chứng minh TCP/HTTP đã tới server; `Connection refused` thường cho thấy không listener/REJECT; timeout gợi ý drop/route nhưng cần thêm bằng chứng.

**Bằng chứng đạt:** với mỗi output, chỉ ra tầng đã pass và tầng chưa được kiểm.

**Break/fix:** hai case thực hành: dừng listener; listener trả 404. Với timeout/firewall chưa học, dùng fixture output và viết phép đo phân biệt; thao tác rule thật chuyển sang NET-04.

**Rollback/reset:** dừng process lab; không thay firewall trong bài này.

**Checkpoint:** nhận ba symptom `NXDOMAIN`, `Connection refused`, `HTTP 500`; chọn lệnh đầu tiên và giải thích vì sao.

**Feynman:** `ss` và `curl` trả lời câu hỏi khác nhau thế nào? 404 có nghĩa mạng hỏng không? Timeout có một nguyên nhân duy nhất không?

<a id="net-04"></a>

### NET-04 — Provider firewall và UFW mà không mất SSH

**Cần trước:** SEC-04, NET-03. **Mục tiêu:** bật UFW theo trình tự giữ SSH, chỉ mở bề mặt cần và chứng minh rule không tạo service. **Rủi ro:** 🔴; VM snapshot trước, VPS chỉ sau GATE-VPS.

**Loại suy:** provider firewall là cổng ngoài khu đất; UFW là bảo vệ trong tòa nhà. Giới hạn: allow rule không thay authentication/TLS/app authorization và không tạo listener.

**Tự hỏi:** Vì sao chạy `ufw enable` trước `ufw allow OpenSSH` có thể nguy hiểm?

**Chốt lại:** default incoming deny có thể chặn phiên SSH mới; phiên hiện tại có thể còn nhưng reconnect thất bại.

**Preflight — VM:** snapshot, console mở, SSH phiên một giữ nguyên, biết port SSH thực bằng effective config và listener. Xác định source IP mà VM thật sự thấy từ phiên SSH hiện tại:

```bash
HOST_IP=${SSH_CLIENT%% *}
printf 'HOST_IP=%s\n' "$HOST_IP"
test -n "$HOST_IP"
case "$HOST_IP" in (*[!0-9A-Fa-f:.]*) printf 'Unexpected source address: %s\n' "$HOST_IP" >&2; false ;; esac
```

Nếu `SSH_CLIENT` trống vì đang dùng console, lấy IP HOST trên host-only network và xác minh bằng log/packet; không đoán IP Wi-Fi khi traffic đi qua NAT/port-forward.

```bash
sudo /usr/sbin/sshd -T | grep '^port '
SSH_PORT=$(sudo /usr/sbin/sshd -T | awk '$1=="port"{print $2; exit}')
printf 'SSH_PORT=%s\n' "$SSH_PORT"
sudo ss -lntp | grep ssh
sudo ufw status verbose
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow "$SSH_PORT/tcp" comment 'SSH before enable'
sudo ufw show added
sudo ufw enable
sudo ufw status numbered
```

Mở **phiên SSH thứ hai** và kiểm `sudo -v`. Sau đó tạo listener 8080 bind all-interface trong VM; trước allow, HOST không nên tới nếu network/firewall áp dụng. Mở tạm:

```bash
sudo ufw allow from "$HOST_IP" to any port 8080 proto tcp comment 'temporary lab'
sudo ufw status numbered
```

Kiểm từ HOST, rồi xóa rule bằng chính cú pháp hoặc số rule sau khi xem lại:

```bash
sudo ufw delete allow from "$HOST_IP" to any port 8080 proto tcp
```

Không copy `HOST_IP` placeholder nguyên văn.

**Bằng chứng đạt:** phiên SSH thứ hai còn hoạt động; port 8080 chỉ tới từ source cho phép khi có listener; sau khi process dừng, rule còn nhưng kết nối không thành công — chứng minh rule không tạo listener.

**Break/fix:** trong snapshot VM, tạo deny 8080 trước allow và quan sát rule order bằng `ufw status numbered`; xóa đúng rule. Không dùng `ufw reset` trên VPS để chữa nhanh.

**Rollback/reset:** console VM:

```bash
sudo ufw disable
sudo ufw status verbose
```

Trên VPS, ưu tiên sửa allow SSH từ console rồi test phiên hai; không disable toàn bộ lâu hơn cần thiết.

**Checkpoint:** từ UFW inactive, thiết lập default deny, giữ SSH, mở tạm 8080 cho một source, chứng minh allow/deny/listener riêng biệt rồi xóa rule.

**Feynman:** provider firewall khác UFW? Allowed khác reachable? Vì sao hai phiên SSH là control an toàn?

<a id="net-05"></a>

### NET-05 — HTTP, HTTPS và TLS theo từng lớp

**Cần trước:** NET-02, NET-03. **Mục tiêu:** đọc request/response/status/header và phân biệt DNS, TCP, TLS, HTTP failure. **Rủi ro:** 🟢.

**Loại suy:** HTTP là ngôn ngữ trao đổi; TLS là đường hầm xác thực/mã hóa trước khi nói HTTP. Giới hạn: HTTPS bảo vệ đường truyền, không sửa authorization, injection hoặc máy đầu cuối bị chiếm.

**Tự hỏi:** Certificate còn hạn nhưng hostname không khớp có an toàn không?

**Chốt lại:** client phải kiểm chain, thời hạn và hostname; bỏ verify bằng `-k` che lỗi chứ không sửa lỗi.

**Lab — VM/CLIENT:**

```bash
curl -I http://example.com
curl -v --max-time 10 https://example.com/ -o /dev/null
openssl s_client -connect example.com:443 -servername example.com </dev/null 2>/dev/null | openssl x509 -noout -subject -issuer -dates -ext subjectAltName
```

Đọc status phổ biến: 2xx thành công; 3xx chuyển hướng; 4xx request/client/authorization; 5xx server/downstream. Không coi mọi 4xx là lỗi mạng.

Thử endpoint không tồn tại:

```bash
curl -sS -D - https://example.com/definitely-not-a-real-path -o /dev/null
```

**Bằng chứng đạt:** chỉ được bước DNS/TCP/TLS/HTTP trong verbose output; đọc hostname/SAN và status/header.

**Break/fix:** gọi HTTPS bằng IP hoặc hostname sai trong lab để thấy verify fail; sửa bằng hostname đúng/SNI, không dùng `-k` trong bằng chứng pass.

**Rollback/reset:** bài chỉ đọc; xóa output certificate nếu đã lưu và có dữ liệu không cần giữ.

**Checkpoint:** phân tích bốn output: DNS fail, TCP refused, TLS hostname fail, HTTP 503; ghi lớp hỏng và phép đo tiếp theo.

**Feynman:** TLS bảo vệ gì? SNI/SAN liên quan hostname thế nào? 503 khác connection refused?

---

<a id="gate-vps"></a>

## GATE-VPS — Cổng chuyển từ VM sang VPS công khai

**Cần trước:** LAB-03, CLI-06, CLI-07, SEC-04, SYS-03, SYS-04, SYS-08, NET-04. **Mục tiêu:** chứng minh đủ kỹ năng cứu hộ, least privilege, firewall, service, backup và cleanup trước khi phơi máy ra Internet. **Rủi ro:** 🔴/💰.

**Tự hỏi:** Vì sao hoàn thành SSH key nhưng chưa biết restore backup vẫn chưa đủ để vận hành VPS?

**Chốt lại:** an toàn truy cập chỉ là một lớp; hệ thống public còn cần phục hồi cấu hình/dữ liệu, quan sát lỗi và kiểm soát chi phí.

### Bài kiểm bắt buộc trên VM

- Restore snapshot LAB-01 thành công.
- Key login bằng phiên hai; `sudo`; `sshd -t`/effective config pass.
- UFW default deny nhưng giữ SSH; console đã thử.
- App/simulator non-root, journal đọc được, sống logout/reboot, restart sau crash có kiểm soát.
- Listener nội bộ bind loopback khi không cần public.
- `fstab` lab được validate và phục hồi từ console.
- Archive/config test được restore; repo không chứa secret.

### Dựng VPS disposable

1. Đặt budget/cleanup time; chọn Ubuntu 24.04; nạp **public key**, không upload private key.
2. Ghi fingerprint từ provider console và đối chiếu ở lần SSH đầu.
3. Giữ console + phiên SSH gốc; tạo user sudo/key và test phiên hai.
4. Allow SSH ở provider firewall và UFW trước default deny/enable.
5. Update metadata/security package có kiểm soát; reboot nếu cần rồi test lại.
6. Chỉ sau key/console pass mới tắt root/password SSH; validate/reload/test phiên ba.
7. Deploy một service lab loopback; kiểm journal/reboot.
8. Tạo backup nhỏ, restore sang đường đích sạch.
9. Gây lỗi `ExecStart`, tìm bằng journal, rollback.
10. Inventory VPS, volume, snapshot, IP; terminate và kiểm billing khi hoàn tất bài disposable.

### Bằng chứng pass/fail

**Bằng chứng đạt:** mọi ô checklist dưới đây phải được đánh dấu bằng output/log/console hoặc artifact tương ứng; ô chưa có evidence là chưa đạt.

```text
[ ] Console cứu hộ mở được
[ ] Fingerprint đã đối chiếu
[ ] Phiên SSH thứ hai bằng key + sudo pass
[ ] SSH được allow trước UFW enable
[ ] Root/password policy effective đúng
[ ] Service non-root sống reboot
[ ] DB/app internal không public
[ ] Backup restore thử pass
[ ] Một lỗi được chẩn đoán từ journal
[ ] Secret không vào Git/log
[ ] Tài nguyên thừa đã terminate và billing kiểm lại
[ ] Mỗi ô có đường dẫn/command evidence trong nhật ký lab
```

Thiếu bất kỳ mục nào: quay lại VM/bài prerequisite; không “pass tạm”.

**Break/fix:** thực hiện trên VPS disposable: sai path `ExecStart`, không sửa SSH/firewall cùng lúc. Ghi hiện tượng, evidence, root cause, rollback và verify.

**Rollback/reset:** dùng phiên cũ/console; restore config backup; snapshot chỉ là phương án bổ sung. Khi bỏ VPS, terminate resource và kiểm volume/snapshot/IP/backup tách rời.

**Checkpoint:** từ checklist trống, dựng VPS test, harden không lockout, chạy service, gây/sửa một lỗi, restore một backup, rồi cleanup hoàn toàn.

**Feynman:** vì sao GATE-VPS là cổng năng lực chứ không phải một lệnh? Điều gì VM không mô phỏng đủ? “Tắt VPS” khác “terminate” thế nào?

---

## Cấp 4 — App, Nginx, domain, HTTPS và deploy

Phần này cung cấp app học tập tối giản chưa có persistence; WEB-01 chỉ học HTTP/validation trong memory, còn PostgreSQL được thêm ở DATA-01. Không public Uvicorn trực tiếp.

<a id="web-01"></a>

### WEB-01 — API/dashboard tối giản trên loopback

**Cần trước:** CLI-07, SYS-05, NET-05. **Mục tiêu:** tạo virtual environment, chạy FastAPI bind loopback, đọc JSON/HTTP và trả 4xx cho input sai. **Rủi ro:** 🟡.

**Loại suy:** API là quầy nhận yêu cầu có hợp đồng dữ liệu; dashboard là mặt hiển thị. Giới hạn: framework tự validation kiểu cơ bản nhưng không tự giải quyết authorization, rate limit hay business rule.

**Tự hỏi:** Vì sao app bind `127.0.0.1:8080` vẫn phục vụ được Internet khi có Nginx?

**Chốt lại:** Nginx là public edge nhận request rồi proxy nội bộ; app không cần phơi port riêng.

**Lab — VM:**

```bash
sudo apt update
sudo apt install -y python3-venv
SOURCE_DIR="$HOME/linux-course/iot-dashboard"
mkdir -p "$SOURCE_DIR"
cd "$SOURCE_DIR"
python3 -m venv .venv
.venv/bin/pip install 'fastapi>=0.110,<1' 'uvicorn[standard]>=0.29,<1'
cat > app.py <<'PY'
import math
from datetime import datetime, timezone
from pathlib import Path

from fastapi import FastAPI, HTTPException
from fastapi.responses import HTMLResponse
from pydantic import BaseModel, ConfigDict, Field, field_validator

app = FastAPI(title="IoT dashboard lab")
VERSION = Path(__file__).with_name("VERSION").read_text().strip() if Path(__file__).with_name("VERSION").exists() else "dev"

class Telemetry(BaseModel):
    model_config = ConfigDict(extra="forbid")
    device_id: str = Field(pattern=r"^[a-z0-9-]{3,32}$")
    temperature: float = Field(ge=-40, le=85)
    humidity: float = Field(ge=0, le=100)

    @field_validator("temperature", "humidity", mode="before")
    @classmethod
    def require_json_number(cls, value):
        if isinstance(value, bool) or not isinstance(value, (int, float)):
            raise ValueError("must be a JSON number")
        if isinstance(value, float) and not math.isfinite(value):
            raise ValueError("must be finite")
        return value

@app.get("/health")
def health():
    return {"status": "ok", "version": VERSION, "time": datetime.now(timezone.utc).isoformat()}

@app.post("/api/validate")
def validate_telemetry(row: Telemetry):
    if row.temperature > 60 and row.humidity > 95:
        raise HTTPException(status_code=422, detail="implausible combined reading")
    return {"accepted": True, "reading": row.model_dump()}

@app.get("/", response_class=HTMLResponse)
def dashboard():
    return """<!doctype html><html lang='vi'><meta charset='utf-8'><title>IoT Lab</title><body><h1>Dashboard nhiệt độ/độ ẩm</h1><pre id='health'>loading...</pre><script>fetch('/health').then(r=>r.json()).then(x=>health.textContent=JSON.stringify(x,null,2)).catch(e=>health.textContent=e)</script></body></html>"""
PY
printf '%s\n' 'fastapi>=0.110,<1' 'uvicorn[standard]>=0.29,<1' > requirements.txt
printf 'dev\n' > VERSION
cat > .gitignore <<'EOF'
.venv/
.env
*.key
*.pem
__pycache__/
EOF
git init
git config user.name 'Linux Learner'
git config user.email 'learner@example.invalid'
git add app.py VERSION requirements.txt .gitignore
git commit -m 'Add minimal IoT API'
.venv/bin/uvicorn app:app --host 127.0.0.1 --port 8080
```

Terminal VM thứ hai:

```bash
ss -lntp 'sport = :8080'
curl -sS http://127.0.0.1:8080/health | python3 -m json.tool
curl -sS -o /dev/null -w '%{http_code}\n' http://127.0.0.1:8080/
curl -sS -o /tmp/validate.json -w '%{http_code}\n' -H 'Content-Type: application/json' -d '{"device_id":"sensor-001","temperature":22.5,"humidity":55}' http://127.0.0.1:8080/api/validate
cat /tmp/validate.json
```

**Bằng chứng đạt:** listener là `127.0.0.1:8080`; health/page trả 200; JSON hợp lệ trả 200; temperature ngoài range trả 422, app không crash.

**Break/fix:** gửi JSON thiếu quote/field và đọc 422; phân biệt validation client input với HTTP 500 server bug.

**Rollback/reset:** `Ctrl+C`; xóa `/tmp/validate.json`. Giữ source, `.venv` và commit cho WEB-02/WEB-06; không xóa repo lab hoặc `.venv` trước khi các bài sau hoàn tất.

**Checkpoint:** thêm field tùy chọn `battery` từ 0–100, test giá trị biên/sai và chứng minh endpoint health vẫn hoạt động.

**Feynman:** loopback giảm bề mặt tấn công thế nào? 4xx khác 5xx? Virtual environment giải quyết gì?

<a id="web-02"></a>

### WEB-02 — Chạy app bằng systemd user riêng

**Cần trước:** WEB-01, SYS-03. **Mục tiêu:** chuyển ownership, chạy Uvicorn non-root qua systemd, journal/restart/reboot được kiểm thử. **Rủi ro:** 🟠.

**Tự hỏi:** Chạy được bằng tay có chứng minh app chạy đúng dưới service user không?

**Chốt lại:** không; environment, working directory, permissions và lifecycle khác.

**Lab — VM:** dừng Uvicorn foreground, rồi:

```bash
SOURCE_DIR="$HOME/linux-course/iot-dashboard"
id -u iotapp >/dev/null 2>&1 || sudo useradd --system --home /srv/iot-api --shell /usr/sbin/nologin iotapp
sudo install -d -o root -g iotapp -m 0750 /srv/iot-api
sudo cp -a "$SOURCE_DIR/app.py" "$SOURCE_DIR/VERSION" "$SOURCE_DIR/requirements.txt" /srv/iot-api/
sudo python3 -m venv /srv/iot-api/.venv
sudo /srv/iot-api/.venv/bin/pip install --requirement /srv/iot-api/requirements.txt
sudo chown -R root:iotapp /srv/iot-api
sudo chmod -R u=rwX,g=rX,o= /srv/iot-api
sudo test -x /srv/iot-api/.venv/bin/uvicorn
sudo -u iotapp /srv/iot-api/.venv/bin/uvicorn --version >/dev/null
sudo tee /etc/systemd/system/iot-api.service >/dev/null <<'UNIT'
[Unit]
Description=IoT dashboard API
After=network.target

[Service]
Type=simple
User=iotapp
Group=iotapp
WorkingDirectory=/srv/iot-api
ExecStart=/srv/iot-api/.venv/bin/uvicorn app:app --host 127.0.0.1 --port 8080
Restart=on-failure
RestartSec=3
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/var/lib/iot-dashboard

[Install]
WantedBy=multi-user.target
UNIT
sudo install -d -o iotapp -g iotapp -m 0750 /var/lib/iot-dashboard
sudo systemd-analyze verify /etc/systemd/system/iot-api.service
sudo systemctl daemon-reload
sudo systemctl enable --now iot-api
sudo systemctl status iot-api --no-pager
sudo journalctl -u iot-api -n 20 --no-pager
curl -fsS http://127.0.0.1:8080/health
```

**Bằng chứng đạt:** `ps` cho user `iotapp`; active/enabled; app sống logout/reboot; kill process tạo PID mới theo restart policy.

**Break/fix:** backup unit, đặt sai `WorkingDirectory`; verify có thể không bắt mọi lỗi runtime, nên restart và đọc journal. Phục hồi đúng một directive.

**Rollback/reset:** restore unit backup, verify, daemon-reload/restart. Không xóa app vì WEB-03 phụ thuộc.

**Checkpoint:** chứng minh service chạy non-root, port loopback, journal có request, reboot xong health 200 và một lỗi working directory được sửa bằng evidence.

**Feynman:** service user giới hạn blast radius gì? Vì sao verify chưa đủ? `ReadWritePaths` dùng để làm gì?

<a id="web-03"></a>

### WEB-03 — Nginx reverse proxy và app nội bộ

**Cần trước:** WEB-02, NET-05. **Mục tiêu:** Nginx nghe HTTP, proxy `/api`, `/health` và `/docs` tới app loopback; app port không public. **Rủi ro:** 🟠.

**Loại suy:** Nginx là lễ tân public; app là phòng xử lý nội bộ. Giới hạn: reverse proxy không tự làm app an toàn hoặc khỏe.

**Lab — VM:**

```bash
sudo apt update
sudo apt install -y nginx
sudo tee /etc/nginx/sites-available/iot-dashboard >/dev/null <<'NGINX'
server {
    listen 80;
    listen [::]:80;
    server_name _;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
NGINX
sudo ln -sfn /etc/nginx/sites-available/iot-dashboard /etc/nginx/sites-enabled/iot-dashboard
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx
curl -fsS http://127.0.0.1/health
curl -fsS http://VM_IP/health
ss -lntp | grep -E ':(80|8080) '
```

Không copy `VM_IP`. Uvicorn phải ở loopback; Nginx nghe 80. Trên VPS, UFW chỉ mở 80/443, không 8080.

**Bằng chứng đạt:** cả localhost Nginx và `VM_IP` trả health; `8080` chỉ bind loopback; `nginx -t` pass.

**Break/fix:** đổi upstream thành `127.0.0.1:8081`, validate vẫn có thể pass nhưng request trả 502; dùng Nginx error log, `ss`, `curl` app trực tiếp để tìm sai port.

```bash
sudo tail -n 30 /var/log/nginx/error.log
```

**Rollback/reset:** phục hồi config upstream, `nginx -t`, reload; không restart cả VM.

**Checkpoint:** dựng server block từ file trống, proxy health/page, tạo lỗi upstream, chứng minh 502 và sửa; từ HOST xác nhận 8080 không trực tiếp reachable.

**Feynman:** 502 nói gì? Nginx log khác app journal? Reverse proxy giúp TLS/port/internal app ra sao?

<a id="web-04"></a>

### WEB-04 — Domain A/AAAA trước TLS

**Cần trước:** GATE-VPS, WEB-03, NET-02. **Mục tiêu:** trỏ domain tới VPS, chỉ thêm AAAA khi IPv6 end-to-end hoạt động và kiểm HTTP từ client ngoài. **Rủi ro:** 🟠/💰.

**Tự hỏi:** Vì sao AAAA sai có thể làm web lúc được lúc không tùy client?

**Chốt lại:** client/network hỗ trợ IPv6 có thể ưu tiên AAAA; nếu route/firewall/listener IPv6 sai, một nhóm client fail dù A đúng.

**Preflight — VPS/CLIENT:** domain thuộc quyền bạn; app/Nginx HTTP đã hoạt động bằng IP; UFW/provider allow 80; biết public IPv4 và IPv6 thực nếu có.

Tại DNS provider, tạo:

```text
Type A    Name iot    Value VPS_IP    TTL 300 (lab)
Type AAAA Name iot    Value VPS_IPV6  chỉ khi IPv6 đã test
```

**CLIENT bên ngoài:**

```bash
dig YOUR_DOMAIN A +noall +answer
dig YOUR_DOMAIN AAAA +noall +answer
curl -4 -I --max-time 10 http://YOUR_DOMAIN/
curl -6 -I --max-time 10 http://YOUR_DOMAIN/ || true
```

**VPS:** đổi `server_name _;` thành domain thật, rồi:

```bash
sudo nginx -t
sudo systemctl reload nginx
```

**Bằng chứng đạt:** A đúng; AAAA trống hoặc đúng IPv6 đã hoạt động; HTTP bằng domain từ client ngoài trả response dự kiến.

**Break/fix:** tạo AAAA sai chỉ trong domain lab nếu có quyền và thời gian TTL ngắn; quan sát `curl -6`, xóa record sai, chờ TTL và kiểm resolver public. Không xin certificate trước khi ổn định.

**Rollback/reset:** xóa/khôi phục DNS record; restore Nginx config backup; không dùng `/etc/hosts` làm bằng chứng DNS public.

**Checkpoint:** từ DNS chưa có record, cấu hình A, kiểm bằng hai resolver và client ngoài; nếu thêm AAAA phải chứng minh IPv6 listener/firewall/request.

**Feynman:** DNS control plane khác web data plane? TTL ảnh hưởng rollback? Vì sao domain đứng trước Certbot?

<a id="web-05"></a>

### WEB-05 — Certbot, HTTPS và renewal

**Cần trước:** WEB-04. **Mục tiêu:** cấp certificate sau DNS/HTTP, kiểm hostname/chain/redirect và renewal dry-run. **Rủi ro:** 🟠/💰 domain/VPS.

**Tự hỏi:** Vì sao mở tạm tất cả port không phải cách sửa ACME challenge?

**Chốt lại:** HTTP-01 cần DNS đúng và port 80 tới đúng Nginx/challenge; mở rộng bề mặt không sửa A/AAAA/server block sai.

**Preflight:** backup Nginx; A/AAAA public đúng; HTTP domain từ ngoài pass; email hợp lệ; UFW/provider 80/443; không rate-limit CA bằng thử lặp mù.

```bash
sudo apt update
sudo apt install -y certbot python3-certbot-nginx
sudo nginx -t
sudo certbot --nginx -d YOUR_DOMAIN
```

Chọn redirect nếu app sẵn sàng. Kiểm từ CLIENT:

```bash
curl -I http://YOUR_DOMAIN/
curl -I https://YOUR_DOMAIN/
openssl s_client -connect YOUR_DOMAIN:443 -servername YOUR_DOMAIN </dev/null 2>/dev/null | openssl x509 -noout -subject -issuer -dates -ext subjectAltName
```

VPS:

```bash
systemctl status certbot.timer --no-pager
sudo certbot renew --dry-run
```

**Bằng chứng đạt:** HTTP redirect HTTPS; HTTPS verify không cần `-k`; SAN đúng; dry-run pass; timer có lịch.

**Break/fix:** challenge fail: kiểm lần lượt DNS A/AAAA, port 80 provider/UFW, Nginx server_name, listener và access/error log. Không tắt firewall toàn bộ, không thêm `--no-verify`.

**Rollback/reset:** restore Nginx backup rồi `nginx -t`/reload. Trước khi xóa certificate, liệt kê tên chính xác bằng `sudo certbot certificates`; chỉ chạy `sudo certbot delete --cert-name TEN_CHUNG_CHI_DA_XAC_NHAN` khi không còn site phụ thuộc.

**Checkpoint:** trên domain lab, từ HTTP pass đến certificate/redirect/dry-run; ghi evidence từng lớp và sửa một lỗi AAAA/server_name mô phỏng.

**Feynman:** ACME xác minh điều gì? Certificate miễn phí không đồng nghĩa phần nào miễn phí? Renewal dry-run bảo vệ khỏi rủi ro gì?

<a id="web-06"></a>

### WEB-06 — Deploy commit/tag, smoke test và rollback

**Cần trước:** CLI-07, WEB-02, WEB-03. **Mục tiêu:** deploy release immutable-ish theo commit, dùng symlink `current`, health/version smoke test và rollback. **Rủi ro:** 🟠.

**Loại suy:** mỗi release là hộp có nhãn commit; `current` chỉ hộp đang dùng. Giới hạn: DB migration và external state có thể làm code rollback không đủ.

**Lab — VM trước VPS:** dùng repo source ở `$HOME/linux-course/iot-dashboard`; `/srv/iot-api` chỉ là bản chạy cũ do root quản lý. Mọi block từ lúc tính `RELEASE_ID` đến smoke/rollback dùng lại biến shell và phải chạy trong **cùng phiên Bash**. Nếu reconnect, tính lại `SOURCE_DIR`, `RELEASE_ID`, `RELEASE_DIR` và đọc lại symlink hiện tại; không đoán giá trị cũ từ trí nhớ. Release phải sinh từ **đúng commit**, không copy working tree vì working tree còn có thể chứa `.env`, `.venv` và file ignored/untracked:

```bash
SOURCE_DIR="$HOME/linux-course/iot-dashboard"
cd "$SOURCE_DIR"
test -d .git
git status --short
git add app.py VERSION requirements.txt .gitignore
if git diff --cached --quiet; then
  :
elif git diff --cached --check; then
  git commit -m 'Prepare IoT API release'
else
  printf 'Staged patch contains whitespace errors; inspect diff before commit\n' >&2
  false
fi
test -z "$(git status --porcelain --untracked-files=all)"
RELEASE_ID=$(git rev-parse HEAD)
RELEASE_DIR="/srv/iot-releases/$RELEASE_ID"
PREVIOUS_TARGET=
if sudo test -e /srv/iot-current && ! sudo test -L /srv/iot-current; then
  printf 'Refusing to replace non-symlink /srv/iot-current\n' >&2
  false
fi
if sudo test -L /srv/iot-current; then
  PREVIOUS_TARGET=$(sudo readlink -f /srv/iot-current)
  sudo test -d "$PREVIOUS_TARGET"
fi
printf 'release=%s\nprevious=%s\n' "$RELEASE_ID" "$PREVIOUS_TARGET"
```

Nếu không có thay đổi staged, dùng commit hiện tại sau khi working tree sạch; không che lỗi Git bằng `|| true`. `git archive` chỉ lấy tracked content của commit, nên `.env`, `.venv` và file ignored không đi vào artifact. Tạo release trước, dựng venv **ngay tại đường dẫn release**, rồi cài dependency; không copy venv từ đường dẫn tạm vì shebang trong script có thể giữ path cũ:

```bash
(
  set -Eeuo pipefail
  if sudo test -e "$RELEASE_DIR"; then
    printf 'Release already exists: %s\n' "$RELEASE_DIR" >&2
    exit 1
  fi
  STAGE_DIR=$(mktemp -d)
  cleanup() { rm -rf -- "$STAGE_DIR"; }
  trap cleanup EXIT
  git archive "$RELEASE_ID" | tar -x -C "$STAGE_DIR"
  printf '%s\n' "$RELEASE_ID" > "$STAGE_DIR/VERSION"
  sudo install -d -o root -g iotapp -m 0750 "$RELEASE_DIR"
  sudo cp -a "$STAGE_DIR/." "$RELEASE_DIR/"
  sudo python3 -m venv "$RELEASE_DIR/.venv"
  sudo "$RELEASE_DIR/.venv/bin/pip" install --requirement "$RELEASE_DIR/requirements.txt"
  sudo chown -R root:iotapp "$RELEASE_DIR"
  sudo find "$RELEASE_DIR" -type d -exec chmod 0750 {} +
  sudo find "$RELEASE_DIR" -type f -exec chmod 0640 {} +
  sudo find "$RELEASE_DIR" -type f -path '*/.venv/bin/*' -exec chmod 0750 {} +
  sudo ln -sfn "$RELEASE_DIR" /srv/iot-current.new
  sudo mv -Tf /srv/iot-current.new /srv/iot-current
  readlink -f /srv/iot-current
)
```

Các biến `RELEASE_DIR`/`RELEASE_ID` được đọc từ shell cha; `exit 1` và trap chỉ tác động subshell. Nếu block fail sau khi đã tạo release nhưng trước khi đổi symlink, `/srv/iot-current` vẫn giữ target cũ; kiểm directory mới rồi xóa riêng nó hoặc chạy lại từ commit khác, không sửa release dở dang.

Để pin tuyệt đối cả dependency transitive trong môi trường thật, sinh và review lockfile có hash bằng công cụ quản lý dependency; `requirements.txt` giới hạn version trong lab này vẫn có thể resolve bản transitive khác ở thời điểm khác.

Backup unit, rồi dùng `sudoedit /etc/systemd/system/iot-api.service` đổi `WorkingDirectory=/srv/iot-current` và `ExecStart=/srv/iot-current/.venv/bin/uvicorn app:app --host 127.0.0.1 --port 8080`. Diff/verify trước restart; smoke test phải khớp **đúng** commit:

```bash
sudo cp -a /etc/systemd/system/iot-api.service /etc/systemd/system/iot-api.service.before-WEB-06
sudoedit /etc/systemd/system/iot-api.service
sudo diff -u /etc/systemd/system/iot-api.service.before-WEB-06 /etc/systemd/system/iot-api.service || true
sudo systemd-analyze verify /etc/systemd/system/iot-api.service
sudo systemctl daemon-reload
sudo systemctl restart iot-api
curl -fsS http://127.0.0.1:8080/health | tee /tmp/iot-smoke.json
EXPECTED_VERSION="$RELEASE_ID" python3 - <<'PY'
import json
import os
with open('/tmp/iot-smoke.json') as f:
    x = json.load(f)
assert x['status'] == 'ok'
assert x['version'] == os.environ['EXPECTED_VERSION'], (x['version'], os.environ['EXPECTED_VERSION'])
print(x['version'])
PY
```

**Bằng chứng đạt:** archive lấy đúng tracked commit; release không chứa `.git`, `.env` hoặc file ignored; symlink, endpoint version và full commit hash khớp; health pass; nếu đã có release trước thì target đó còn nguyên; deploy không sửa trực tiếp release đang chạy. Lần deploy đầu tiên phải ghi rõ `previous` rỗng và không được claim rollback code cho tới khi có release tốt thứ hai.

**Break/fix:** tạo commit/release có syntax/health fail; smoke test phải chặn việc gọi bản đó là tốt.

**Rollback/reset:** sau smoke fail, rollback bằng target đã lưu, không dùng placeholder:

```bash
if test -n "$PREVIOUS_TARGET" && sudo test -d "$PREVIOUS_TARGET"; then
  sudo ln -sfn "$PREVIOUS_TARGET" /srv/iot-current
  sudo systemctl restart iot-api
  curl -fsS http://127.0.0.1:8080/health
else
  printf 'No valid previous release; restore unit/config backup\n' >&2
  false
fi
```

Không sửa tay release lỗi để che audit trail; không xóa release đang chạy. Vì block này có thể được dán vào shell tương tác, `false` chỉ báo fail và không đóng phiên SSH; nếu viết thành script riêng mới dùng `exit 1` sau khi đã chuẩn bị trap/rollback.

**Checkpoint:** tạo hai release có version khác, deploy B, cố ý fail C, rollback B và chứng minh bằng endpoint/log/symlink.

**Feynman:** commit/tag/artifact liên hệ thế nào? Symlink rollback không giải quyết migration nào? Vì sao smoke test phải chạy sau restart?

<a id="web-07"></a>

### WEB-07 — Validation, authorization và bảo vệ web căn bản

**Cần trước:** WEB-05, WEB-06. **Mục tiêu:** từ chối input sai, phân biệt authentication/authorization, dùng query parameterized, header/cookie/rate-limit theo threat model. **Rủi ro:** 🟠.

**Loại suy:** authentication hỏi “ai”; authorization hỏi “được làm gì”. Giới hạn: security header không bù cho access control/injection sai.

**Tự hỏi:** User đã đăng nhập có được đọc mọi device không?

**Chốt lại:** không; mỗi object/action phải kiểm authorization server-side.

**Lab — VM:** chỉnh trực tiếp `app.py` của WEB-01 bằng credential giả của lab; không dùng production secret. Thêm `HTTPException` vào import FastAPI, rồi thêm helper và **thay thế** endpoint `/api/validate` hiện có bằng đoạn sau:

```python
from fastapi import Header, HTTPException

DEVICE_KEYS = {"demo-key-a": "sensor-001", "demo-key-b": "sensor-002"}

def authenticated_device(x_device_key: str = Header(default="")) -> str:
    device = DEVICE_KEYS.get(x_device_key)
    if not device:
        raise HTTPException(status_code=401, detail="invalid device credential")
    return device

@app.post("/api/validate")
def validate_telemetry(row: Telemetry, x_device_key: str = Header(default="")):
    device = authenticated_device(x_device_key)
    if row.device_id != device:
        raise HTTPException(status_code=403, detail="device identity mismatch")
    if row.temperature > 60 and row.humidity > 95:
        raise HTTPException(status_code=422, detail="implausible combined reading")
    return {"accepted": True, "device_id": device, "reading": row.model_dump()}
```

Restart app sau khi validate code; gọi `/api/validate` bằng key A với device A phải pass, còn key A với device B phải 403. Endpoint `/api/ingest` có ghi PostgreSQL được tạo ở IOT-01; WEB-07 không phụ thuộc bài sau. Khi thêm SQL ở DATA-01/IOT-01, luôn dùng placeholder/parameter của driver, không nối chuỗi input.

Nginx thêm các header không làm hỏng dashboard hiện tại:

```nginx
add_header X-Content-Type-Options nosniff always;
add_header Referrer-Policy no-referrer always;
client_max_body_size 64k;
```

Chưa bật CSP ở bước này vì dashboard WEB-01 và IOT-05 còn dùng inline script; policy `script-src 'self'` sẽ chặn chính client Core dù trang HTML vẫn trả 200. Chỉ đánh dấu CSP pass sau khi chuyển JavaScript sang static file cùng origin hoặc dùng nonce/hash được sinh và kiểm đúng. Không thêm `'unsafe-inline'` chỉ để hết lỗi mà không ghi trade-off.

Commit thay đổi source, tạo/activate release mới theo WEB-06, validate unit rồi restart. Test qua Nginx; URL `/api/ingest` chỉ xuất hiện từ IOT-01:

```bash
curl -sS -o /dev/null -w 'allowed=%{http_code}\n' -H 'Content-Type: application/json' -H 'X-Device-Key: demo-key-a' -d '{"device_id":"sensor-001","temperature":22,"humidity":50}' http://127.0.0.1/api/validate
curl -sS -o /dev/null -w 'cross_device=%{http_code}\n' -H 'Content-Type: application/json' -H 'X-Device-Key: demo-key-a' -d '{"device_id":"sensor-002","temperature":22,"humidity":50}' http://127.0.0.1/api/validate
curl -sS -o /dev/null -w 'anonymous=%{http_code}\n' -H 'Content-Type: application/json' -d '{"device_id":"sensor-001","temperature":22,"humidity":50}' http://127.0.0.1/api/validate
```

Kỳ vọng lần lượt `200`, `403`, `401`. Test thêm type/range sai phải `422`; body vượt `client_max_body_size` phải `413`. Rate limit ingestion có phép đo cụ thể được thêm ở IOT-01 thay vì đặt ngưỡng tùy tiện cho health/page.

**Bằng chứng đạt:** key sai/thiếu 401; identity chéo 403; input range/type/size sai 4xx; server không log key; header xuất hiện sau Nginx validate/reload; release endpoint khớp commit chứa authorization mới.

**Break/fix:** cố truy cập device B bằng credential A; nếu trả 2xx, đây là Broken Object Level Authorization. Sửa server-side mapping, không ẩn nút ở UI.

**Rollback/reset:** restore app/Nginx config backup, test syntax/unit và endpoints. Rotate/revoke mọi credential thật nếu lộ; xóa khỏi Git không đủ.

**Checkpoint:** viết ma trận allow/deny cho A/B/anonymous, chạy test tự động hoặc curl; tất cả kết quả đúng status, không có secret trong log.

**Feynman:** 401 khác 403? Validation khác authorization? Parameterized query ngăn lớp lỗi nào?

<a id="web-08"></a>

### WEB-08 — Capstone Web: dựng lại và kiểm bề mặt public

**Cần trước:** WEB-05, WEB-06, WEB-07. **Mục tiêu:** dựng Web tier từ runbook, kiểm port/DNS/TLS/release/security/rollback bằng evidence. **Rủi ro:** 🟠/💰 trên VPS.

**Tự hỏi:** Trang mở được có đủ chứng minh deploy an toàn và rollback được không?

**Chốt lại:** không; còn port nội bộ, identity, TLS renewal, version, log, reboot và rollback.

### Runbook tối thiểu

1. GATE-VPS pass; user/key/UFW/console.
2. App release pin commit, venv/dependency, user `iotapp`.
3. systemd verify/start/journal/reboot.
4. App bind loopback; Nginx 80/443; provider/UFW không mở 8080.
5. Domain A và AAAA nếu dùng; HTTP ngoài pass.
6. Certbot; HTTPS/redirect/SAN/dry-run.
7. Validation/authorization matrix và header.
8. Smoke test health/version/page.
9. Rollback release và xác minh version cũ.
10. Inventory resource/port/secret; cleanup staging/disposable.

Kiểm port **VPS**:

```bash
sudo ss -lntup
sudo ufw status verbose
sudo systemctl --failed --no-pager
curl -fsS http://127.0.0.1:8080/health
```

Kiểm **CLIENT ngoài**:

```bash
curl -fsS https://YOUR_DOMAIN/health
curl -I https://YOUR_DOMAIN/
```

Dùng port scanner chỉ với VPS bạn sở hữu/được phép; tối thiểu thử kết nối các port đã dự kiến từ client. Public dự kiến: SSH theo policy, 80, 443; không 8080/PostgreSQL/MQTT plaintext.

**Bằng chứng đạt:** runbook tái dựng được; domain HTTPS; version khớp; internal port không public; reboot pass; renewal dry-run; auth matrix; rollback pass.

**Break/fix:** deploy release health fail hoặc Nginx upstream sai; người học phải chọn rollback trước hay sửa-forward theo impact, ghi quyết định và evidence.

**Rollback/reset:** release symlink + config backup; không rollback TLS/DNS tùy tiện nếu nhiều dịch vụ dùng chung. Khi xóa VPS disposable, kiểm volume/snapshot/IP.

**Checkpoint:** từ VM/VPS sạch, một người khác dùng runbook dựng Web tier và thực hiện rollback mà không hỏi tác giả các bước ngầm.

**Feynman:** “production-ready” còn thiếu gì trước DATA/IOT/OPS? Bề mặt public tối thiểu là gì? Runbook tốt phải chứa bằng chứng nào?

---

## Cấp 5 — Database, telemetry và IoT

> Core chọn PostgreSQL làm database chính. SQLite có thể dùng cho prototype local; TimescaleDB/InfluxDB là nhánh mở rộng, không bắt buộc đồng thời.

<a id="data-01"></a>

### DATA-01 — PostgreSQL role, database và schema tối thiểu

**Cần trước:** SEC-02, SYS-07, WEB-01. **Mục tiêu:** tạo database/role ít quyền, query parameterized và giữ DB ở loopback/private network. **Rủi ro:** 🟠.

**Loại suy:** database là kho có bảng và người giữ chìa riêng. Giới hạn: role quyền cao, backup sai hoặc query không kiểm soát vẫn phá/mất dữ liệu.

**Tự hỏi:** Vì sao app không nên dùng role PostgreSQL có quyền tạo database hoặc superuser?

**Chốt lại:** lỗi/app bị chiếm sẽ có blast radius lớn. App chỉ cần quyền trên schema/tables cần thiết.

**Preflight — VM:** snapshot; dữ liệu lab không thật; không mở port 5432 public.

```bash
sudo apt update
sudo apt install -y postgresql postgresql-contrib
sudo systemctl enable --now postgresql
sudo -u postgres psql <<'SQL'
CREATE ROLE iot_owner NOLOGIN;
CREATE ROLE iotapp LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
CREATE DATABASE iot_dashboard OWNER iot_owner;
SQL
sudo -u postgres psql -d iot_dashboard <<'SQL'
REVOKE ALL ON DATABASE iot_dashboard FROM PUBLIC;
GRANT CONNECT ON DATABASE iot_dashboard TO iotapp;
SET ROLE iot_owner;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
CREATE TABLE public.readings (
    id bigserial PRIMARY KEY,
    device_id text NOT NULL,
    temperature double precision NOT NULL,
    humidity double precision NOT NULL,
    device_time timestamptz,
    received_at timestamptz NOT NULL DEFAULT now()
);
GRANT USAGE ON SCHEMA public TO iotapp;
GRANT SELECT, INSERT ON TABLE public.readings TO iotapp;
GRANT USAGE, SELECT ON SEQUENCE public.readings_id_seq TO iotapp;
ALTER DEFAULT PRIVILEGES FOR ROLE iot_owner IN SCHEMA public
    GRANT SELECT, INSERT ON TABLES TO iotapp;
ALTER DEFAULT PRIVILEGES FOR ROLE iot_owner IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO iotapp;
RESET ROLE;
SQL
sudo -u postgres psql
```

Trong prompt `psql`, đặt password bằng `\password iotapp`, nhập hai lần rồi `\q`. Cách này không ghi password literal vào shell history/process list. Vì `NOINHERIT` chỉ ảnh hưởng membership role còn `GRANT` trực tiếp vẫn hoạt động, app vẫn dùng được DML đã cấp.

Tạo `.pgpass` mode `0600` bằng prompt ẩn chỉ trong VM lab, rồi kiểm app role thật sự insert/select được nhưng không có quyền quản trị:

```bash
install -m 0600 /dev/null "$HOME/.pgpass"
read -rsp 'Password for iotapp: ' IOT_DB_PASSWORD; printf '\n'
printf '127.0.0.1:5432:iot_dashboard:iotapp:%s\n' "$IOT_DB_PASSWORD" > "$HOME/.pgpass"
unset IOT_DB_PASSWORD
psql -h 127.0.0.1 -U iotapp -d iot_dashboard <<'SQL'
SELECT current_user, current_database();
INSERT INTO readings (device_id, temperature, humidity, device_time)
VALUES ('sensor-001', 22.5, 55, '2026-01-01T00:00:00Z');
SELECT device_id, temperature, humidity FROM readings ORDER BY id DESC LIMIT 1;
SQL
sudo -u postgres psql -c "SELECT rolname, rolsuper, rolcreatedb, rolcreaterole FROM pg_roles WHERE rolname IN ('iot_owner','iotapp');"
sudo -u postgres psql -Atqc 'SHOW listen_addresses;'
sudo -u postgres psql -Atqc 'SHOW port;'
sudo ss -lntp '( sport = :5432 )'
```

Từ **HOST** đối với VM hoặc **CLIENT ngoài** đối với VPS, thay đúng placeholder IP rồi xác nhận kết nối bị từ chối/timeout. Lệnh dự kiến lỗi nên lưu status thay vì che bằng `|| true`:

```bash
if nc -vz -w 3 VM_IP 5432; then
  printf 'FAIL: PostgreSQL is reachable from another machine\n' >&2
  false
else
  DB_PORT_STATUS=$?
  printf 'PASS: external TCP connection was refused/timed out (status=%s)\n' "$DB_PORT_STATUS"
fi
```

Phải đọc output để phân biệt “refused/timeout” với lỗi dùng sai lệnh hoặc placeholder chưa thay; exit khác 0 tự nó chưa chứng minh firewall/bind policy đúng.

Nếu client chưa có `nc`, cài package `netcat-openbsd` hoặc dùng công cụ TCP tương đương; không đánh dấu pass khi chưa có phép đo từ máy khác.

**Bằng chứng đạt:** `iot_owner` là `NOLOGIN`; `iotapp` không superuser/createdb/createrole nhưng insert/select được; table/sequence grant đúng; `SHOW listen_addresses` và `ss` chứng minh bind policy; phép đo từ client khác chứng minh port 5432 không public.

**Break/fix:** dùng password sai hoặc tên database sai; phân biệt authentication failure với service/listener/firewall bằng `systemctl`, journal và `ss`.

**Rollback/reset:** trong VM lab:

```bash
rm -f -- "$HOME/.pgpass"
sudo -u postgres psql -c "DROP DATABASE IF EXISTS iot_dashboard;"
sudo -u postgres psql -c "DROP ROLE IF EXISTS iotapp;"
sudo -u postgres psql -c "DROP ROLE IF EXISTS iot_owner;"
```

**Checkpoint:** tạo role/database/table mới bằng quyền tối thiểu, insert/select một row, chứng minh role không thể làm hành động quản trị không cần.

**Feynman:** role khác user Linux? DB bind khác app authorization? Vì sao password file không phải secret manager?

<a id="data-02"></a>

### DATA-02 — Mô hình telemetry, UTC, index và retention preview

**Cần trước:** DATA-01, SYS-07. **Mục tiêu:** lưu device time/receive time riêng, validate range và truy vấn theo device/khoảng thời gian có index. **Rủi ro:** 🟡/🟠.

**Lưu ý về timestamp:** PostgreSQL `timestamptz` có thể chấp nhận chuỗi không ghi timezone và diễn giải theo timezone của session. Vì sau khi cast không thể biết input ban đầu có offset hay không, API/worker phải bắt buộc RFC 3339 có `Z` hoặc offset trước khi insert.

**Loại suy:** mỗi record là một mẩu đo có nhãn thiết bị và thời điểm. Giới hạn: index tăng storage/write cost; retention xóa dữ liệu nên không làm thật trước backup/preview.

**Tự hỏi:** Nếu cảm biến offline 10 phút rồi gửi bù, dùng thời điểm nào để tính “dữ liệu mới nhất”?

**Chốt lại:** giữ cả `device_time` và `received_at`; query lịch sử theo device time, freshness theo receive time, policy late data rõ ràng.

**Lab — VM:**

```bash
sudo -u postgres psql -d iot_dashboard <<'SQL'
ALTER TABLE readings ADD COLUMN IF NOT EXISTS message_id text;
UPDATE readings SET message_id = 'legacy-' || id WHERE message_id IS NULL;
ALTER TABLE readings ALTER COLUMN message_id SET NOT NULL;
ALTER TABLE readings ADD CONSTRAINT temperature_range CHECK (temperature BETWEEN -40 AND 85);
ALTER TABLE readings ADD CONSTRAINT humidity_range CHECK (humidity BETWEEN 0 AND 100);
CREATE UNIQUE INDEX IF NOT EXISTS readings_device_message_uq ON readings (device_id, message_id);
CREATE INDEX IF NOT EXISTS readings_device_time_idx ON readings (device_id, device_time DESC);
INSERT INTO readings (device_id, message_id, temperature, humidity, device_time, received_at)
VALUES ('sensor-001', '00000000-0000-4000-8000-000000000001', 22.5, 55, '2026-01-01T00:00:00Z', now());
SELECT device_id, temperature, humidity, device_time, received_at
FROM readings
WHERE device_id = 'sensor-001'
ORDER BY device_time DESC LIMIT 10;
EXPLAIN (COSTS OFF) SELECT * FROM readings WHERE device_id='sensor-001' AND device_time > '2025-12-31T00:00:00Z';
SQL
```

Retention chỉ preview:

```sql
-- Chỉ xem record sẽ quá hạn; chưa DELETE.
SELECT count(*)
FROM readings
WHERE received_at < now() - interval '90 days';
```

Dòng backfill `legacy-<id>` chỉ dành cho record lab cũ từ DATA-01; trong hệ thống thật phải có migration policy được review thay vì tự phát minh identity. Sau migration, `message_id NOT NULL` cùng unique index mới bảo đảm không có đường ghi `NULL` né idempotency key.

**Bằng chứng đạt:** `message_id` là `NOT NULL`; unique index chặn trùng theo device/message; constraint chặn nhiệt độ/độ ẩm ngoài range; query giữ hai timestamp; index `(device_id, device_time DESC)` tồn tại và predicate khớp thứ tự khóa. Với fixture nhỏ, planner có thể chọn `Seq Scan` hợp lý; chỉ dùng plan làm pass/fail sau khi có đủ dữ liệu và baseline.

**Break/fix:** PostgreSQL có thể nhận timestamp thiếu timezone, nên đừng chờ DB báo parse error. Gửi input `2026-01-01T00:00:00` qua API/worker: validator phải trả 422/quarantine trước insert; input `2026-01-01T00:00:00Z` hoặc có offset mới pass. Với range sai, giữ DB constraint làm lớp cuối, không nới constraint để cho dữ liệu bẩn qua.

**Rollback/reset:** xóa row lab theo `device_id`/timestamp chính xác; không `DELETE` không có `WHERE`; restore snapshot nếu migration test sai.

**Checkpoint:** thêm dữ liệu đến trễ, query latest theo device time và freshness theo receive time; viết retention query preview và giải thích thời điểm backup trước delete.

**Feynman:** index đổi write/read ra sao? Device time có đáng tin tuyệt đối không? Retention khác backup?

<a id="iot-01"></a>

### IOT-01 — REST ingestion có identity, validation và rate limit

**Cần trước:** WEB-07, DATA-02. **Mục tiêu:** nhận payload telemetry qua HTTPS/API, kiểm device identity, kiểu/range/kích thước và giới hạn tốc độ. **Rủi ro:** 🟠.

**Preflight — VM/VPS lab:** DATA-02 migration pass; app source/release hiện tại đã backup; passfile chỉ app user đọc; Nginx/HTTPS từ WEB-05 hoạt động; mọi key trong bài là credential giả. Ghi count trước test để chứng minh case bị từ chối không tạo row.

**Loại suy:** API ingestion là cổng cân hàng; validator không cho kiện sai vào kho. Giới hạn: validation không chứng minh thiết bị thật; cần credential/ACL/rotation.

**Tự hỏi:** Chỉ kiểm `temperature` là số có đủ bảo vệ database không?

**Chốt lại:** cần kiểm device identity, range, timestamp, payload size, authorization và chống spam; database constraint là lớp cuối, không thay validation API.

**Lab — VM:** cài driver PostgreSQL vào venv source rồi mở rộng model/endpoint. DB credential phải đọc từ `PGPASSFILE` mode `0600`, không hard-code:

```bash
cd "$HOME/linux-course/iot-dashboard"
.venv/bin/pip install 'psycopg[binary]>=3.1,<4'
printf '%s\n' 'psycopg[binary]>=3.1,<4' >> requirements.txt
```

```python
import os
import re
import uuid
from datetime import datetime, timezone

import psycopg
from fastapi import Header
from pydantic import field_validator

DEVICE_KEYS = {"demo-key-a": "sensor-001", "demo-key-b": "sensor-002"}

class IngestTelemetry(Telemetry):
    message_id: str
    device_time: str

    @field_validator("message_id", mode="before")
    @classmethod
    def valid_message_id(cls, value) -> str:
        if not isinstance(value, str):
            raise ValueError("message_id must be a string UUID")
        return str(uuid.UUID(value))

    @field_validator("device_time", mode="before")
    @classmethod
    def timezone_required(cls, value) -> str:
        if not isinstance(value, str):
            raise ValueError("device_time must be a string")
        if not (value.endswith("Z") or re.search(r"[+-]\d{2}:\d{2}$", value)):
            raise ValueError("device_time must include Z or UTC offset")
        try:
            parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
        except ValueError as exc:
            raise ValueError("device_time must be RFC 3339") from exc
        if parsed.tzinfo is None:
            raise ValueError("device_time must include timezone")
        if abs((parsed - datetime.now(timezone.utc)).total_seconds()) > 86400:
            raise ValueError("device_time outside 24-hour lab window")
        return value

def db_connect(*, row_factory=None):
    kwargs = {
        "host": os.environ.get("DATABASE_HOST", "127.0.0.1"),
        "dbname": os.environ.get("DATABASE_NAME", "iot_dashboard"),
        "user": os.environ.get("DATABASE_USER", "iotapp"),
    }
    if row_factory is not None:
        kwargs["row_factory"] = row_factory
    if passfile := os.environ.get("PGPASSFILE"):
        kwargs["passfile"] = passfile
    elif password_file := os.environ.get("DATABASE_PASSWORD_FILE"):
        with open(password_file, encoding="utf-8") as secret:
            kwargs["password"] = secret.read().strip()
    else:
        raise RuntimeError("configure PGPASSFILE or DATABASE_PASSWORD_FILE")
    return psycopg.connect(**kwargs)


@app.post("/api/ingest", status_code=201)
def ingest(row: IngestTelemetry, x_device_key: str = Header(default="")):
    device = DEVICE_KEYS.get(x_device_key)
    if not device:
        raise HTTPException(status_code=401, detail="invalid device credential")
    if row.device_id != device:
        raise HTTPException(status_code=403, detail="device identity mismatch")
    with db_connect() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """INSERT INTO readings
                   (device_id, message_id, temperature, humidity, device_time)
                   VALUES (%s, %s, %s, %s, %s)
                   ON CONFLICT (device_id, message_id) DO NOTHING
                   RETURNING id""",
                (row.device_id, row.message_id, row.temperature, row.humidity, row.device_time),
            )
            inserted = cur.fetchone()
    return {"accepted": True, "duplicate": inserted is None, "device_id": device}
```

Cài passfile riêng cho service với owner là chính service user và mode `0600`; libpq có thể bỏ qua password file có quyền group/world. Nhập password ẩn, không dùng `$HOME/.pgpass` sau khi DATA-01 rollback:

```bash
sudo install -d -o root -g iotapp -m 0750 /etc/iot-dashboard
read -rsp 'PostgreSQL password for iotapp: ' IOT_DB_PASSWORD; printf '\n'
printf '127.0.0.1:5432:iot_dashboard:iotapp:%s\n' "$IOT_DB_PASSWORD" | sudo install -o iotapp -g iotapp -m 0600 /dev/stdin /etc/iot-dashboard/iotapp.pgpass
unset IOT_DB_PASSWORD
sudo install -d -m 0755 /etc/systemd/system/iot-api.service.d
sudo tee /etc/systemd/system/iot-api.service.d/database.conf >/dev/null <<'UNIT'
[Service]
Environment=PGPASSFILE=/etc/iot-dashboard/iotapp.pgpass
UNIT
```

Commit `app.py` và `requirements.txt`, tạo/activate release mới theo WEB-06, rồi `systemd-analyze verify`, daemon-reload, restart và đọc journal trước test. Nginx thêm body/rate limit; `limit_req_zone` phải ở `http` context, ví dụ `/etc/nginx/conf.d/iot-rate.conf`:

```nginx
limit_req_zone $binary_remote_addr zone=iot_ingest:10m rate=5r/s;
```

Trong `server`:

```nginx
client_max_body_size 64k;
location = /api/ingest {
    limit_req zone=iot_ingest burst=3 nodelay;
    limit_req_status 429;
    proxy_pass http://127.0.0.1:8080;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
}
```

Validate/reload rồi test acceptance qua **HTTPS public hostname** từ CLIENT; loopback HTTP chỉ dùng chẩn đoán nội bộ. Dùng timestamp/UUID động:

```bash
sudo nginx -t && sudo systemctl reload nginx
NOW=$(date -u +%FT%TZ)
MID=$(cat /proc/sys/kernel/random/uuid)
BODY=$(printf '{"message_id":"%s","device_id":"sensor-001","device_time":"%s","temperature":22.5,"humidity":55}' "$MID" "$NOW")
VALID_STATUS=$(curl -sS -o /tmp/ingest.json -w '%{http_code}' -H 'Content-Type: application/json' -H 'X-Device-Key: demo-key-a' -d "$BODY" "https://YOUR_DOMAIN/api/ingest")
test "$VALID_STATUS" = 201
sudo -u postgres psql -d iot_dashboard -c "SELECT device_id,message_id,count(*) FROM readings WHERE message_id='$MID' GROUP BY device_id,message_id;"
```

Lặp cùng `MID` và assert cả response lẫn DB:

```bash
DUPLICATE_STATUS=$(curl -sS -o /tmp/ingest-duplicate.json -w '%{http_code}' -H 'Content-Type: application/json' -H 'X-Device-Key: demo-key-a' -d "$BODY" "https://YOUR_DOMAIN/api/ingest")
test "$DUPLICATE_STATUS" = 201
python3 - <<'PY'
import json
with open('/tmp/ingest-duplicate.json', encoding='utf-8') as response:
    assert json.load(response)['duplicate'] is True
PY
ROW_COUNT=$(sudo -u postgres psql -At -d iot_dashboard -c "SELECT count(*) FROM readings WHERE message_id='$MID';")
test "$ROW_COUNT" = 1
```

Tạo body lớn hơn 64 KiB và kiểm `413`:

```bash
python3 - <<'PY' > /tmp/too-large.json
import json
print(json.dumps({"padding": "x" * 70000}))
PY
LARGE_STATUS=$(curl -sS -o /dev/null -w '%{http_code}' -H 'Content-Type: application/json' -H 'X-Device-Key: demo-key-a' --data-binary @/tmp/too-large.json "https://YOUR_DOMAIN/api/ingest")
test "$LARGE_STATUS" = 413
```

Tạo burst đồng thời và yêu cầu ít nhất một response `429`; dùng cùng body hợp lệ, DB unique key vẫn chống duplicate:

```bash
export BODY
seq 1 20 | xargs -P20 -I{} sh -c 'curl -sS -o /dev/null -w "%{http_code}\n" -H "Content-Type: application/json" -H "X-Device-Key: demo-key-a" -d "$BODY" "https://YOUR_DOMAIN/api/ingest"' | tee /tmp/rate-status.txt
grep -qx '429' /tmp/rate-status.txt
unset BODY
```

Test thêm key sai 401, identity chéo 403 và range/timezone sai 422; query DB chứng minh các case bị từ chối không tạo row.

**Bằng chứng đạt:** valid 201 và DB đúng một row; duplicate không nhân bản; key sai 401; device chéo 403; range/timezone/type sai 422; body lớn 413; burst vượt limit 429; không insert trước validation pass.

**Break/fix:** bỏ check identity trong branch lab và chạy case chéo; test phải bắt lỗi. Khôi phục trước khi commit.

**Rollback/reset:** restore code commit/config; rotate key giả nếu đã ghi ra log; không dùng key giả làm credential VPS.

**Checkpoint:** hoàn thành bảng test 5 case (valid, unknown key, mismatch, range, malformed/too large) và lưu status/response không chứa secret.

**Feynman:** 401/403 khác nhau? API validation và DB constraint chia lớp ra sao? Rate limit bảo vệ availability hay confidentiality?

<a id="iot-02"></a>

### IOT-02 — Mosquitto broker, topic và publish/subscribe local

**Cần trước:** NET-01, SYS-01. **Mục tiêu:** hiểu broker, publish/subscribe, topic và giữ listener MQTT local/cô lập trong lab. **Rủi ro:** 🟠; không public anonymous listener.

**Loại suy:** broker là bưu điện nhận và phân tuyến message theo topic. Giới hạn: broker không tự hiểu payload, ownership thiết bị hoặc quyền nghiệp vụ.

**Tự hỏi:** Publisher gửi message thành công có nghĩa worker đã lưu DB không?

**Chốt lại:** không; cần subscriber/worker, topic đúng, QoS/policy và bằng chứng downstream.

**Lab — VM:**

```bash
sudo apt update
sudo apt install -y mosquitto mosquitto-clients
sudo systemctl enable --now mosquitto
sudo ss -lntp | grep mosquitto
```

Tạo config **chỉ cho loopback lab**, không dùng trên VPS public:

```bash
(
  set -Eeuo pipefail
  sudo test ! -e /etc/mosquitto/conf.d/iot-lab-local.conf
  sudo tee /etc/mosquitto/conf.d/iot-lab-local.conf >/dev/null <<'CONF'
listener 1883 127.0.0.1
allow_anonymous true
CONF
  # Mosquitto 2.0 has no parser-only validator; restart is guarded and rollback follows.
  if ! sudo systemctl restart mosquitto; then
    sudo journalctl -u mosquitto -n 50 --no-pager
    sudo rm -- /etc/mosquitto/conf.d/iot-lab-local.conf
    sudo systemctl restart mosquitto
    exit 1
  fi
  sudo systemctl status mosquitto --no-pager
  sudo journalctl -u mosquitto -n 30 --no-pager
)
```

`exit 1` ở đây chỉ dừng subshell sau khi đã phục hồi file local; phiên terminal của người học vẫn mở.

Terminal 1 subscriber:

```bash
mosquitto_sub -h 127.0.0.1 -p 1883 -t 'lab/devices/+/telemetry' -v
```

Terminal 2 publisher:

```bash
mosquitto_pub -h 127.0.0.1 -p 1883 -t 'lab/devices/sensor-001/telemetry' -m '{"device_id":"sensor-001","temperature":22.5,"humidity":55}'
```

**Bằng chứng đạt:** subscriber nhận đúng topic/message; broker chỉ bind loopback; anonymous chỉ tồn tại trong mạng lab cô lập và được đánh dấu không dùng public.

**Break/fix:** subscriber nghe `sensor-002` nên không thấy sensor-001; kiểm topic literal/wildcard bằng `-v`, log và listener.

**Rollback/reset:** nếu dừng ở IOT-02 thì có thể xóa `iot-lab-local.conf`, restart và kiểm `is-active`/listener/journal; Mosquitto 2 có thể trở về local-only listener mặc định thay vì không nghe port nào. Nếu học tiếp IOT-03, **không cleanup ở đây**: giữ file đến khi TLS config/cert/ACL đã sẵn sàng, rồi IOT-03 xóa local listener và restart nguyên tử. Mosquitto 2.0 trong Ubuntu 24.04 không có `--test-config` (tùy chọn này thuộc Mosquitto 2.1+); nếu restart sau thay đổi thất bại, phục hồi đúng file vừa đổi trước khi thử lại. Không để anonymous listener còn sau khi chuyển IOT-03.

**Checkpoint:** tạo topic `lab/devices/+/telemetry`, publish hai device và chứng minh wildcard nhận cả hai nhưng topic không lẫn dữ liệu.

**Feynman:** broker khác worker? Topic wildcard làm gì? QoS/reconnect chưa được bảo đảm bởi publish exit 0 như thế nào?

<a id="iot-03"></a>

### IOT-03 — MQTT TLS, per-device credential và ACL

**Cần trước:** IOT-02, WEB-05, NET-04. **Mục tiêu:** tắt anonymous/plaintext public, bật TLS listener lab, tách identity/topic và test revoke. **Rủi ro:** 🔴; chỉ VM/VPS disposable có console.

**Loại suy:** TLS bảo vệ đường vận chuyển; ACL là danh sách phòng thiết bị được phép vào. Giới hạn: password/TLS không thay thế rotation, authorization server-side hoặc bảo vệ private key.

**Tự hỏi:** Dùng cùng một user/password cho 100 device có cho biết device nào bị lộ không?

**Chốt lại:** không; mỗi thiết bị cần identity/credential/ACL riêng để audit, revoke và giới hạn topic.

**Preflight:** backup Mosquitto config; tạo cert lab CA/server; không dùng certificate thật hoặc gửi private key vào Git. Nếu không có công cụ PKI, chỉ hoàn thành mô hình quyền trên local; không giả vờ TLS pass. Nếu restart/rollback làm đổi trạng thái listener, ghi rõ state chuyển giao trước khi sang bài kế tiếp.

```bash
(
  set -Eeuo pipefail
  umask 077
  sudo test ! -e /etc/mosquitto.failed-iot-03
  sudo tar -czf - /etc/mosquitto > "$HOME/mosquitto-before-iot-03.tgz"
  chmod 0600 "$HOME/mosquitto-before-iot-03.tgz"
  sudo systemctl is-active --quiet mosquitto
  sudo ss -lntp | grep mosquitto
  sudo grep -Eq '^[[:space:]]*persistence[[:space:]]+true([[:space:]]|$)' /etc/mosquitto/mosquitto.conf
  sudo grep -Eq '^[[:space:]]*persistence_location[[:space:]]+/var/lib/mosquitto/([[:space:]]|$)' /etc/mosquitto/mosquitto.conf
)
```

Preflight dừng nếu directory bảo toàn config lỗi từ lần trước còn tồn tại; review/đổi tên nó trước khi thử lại. `umask 077` chỉ áp dụng trong subshell, không âm thầm đổi quyền mặc định cho các bài sau.

Chưa xóa/restart listener local ở đây. Mosquitto 2 có thể tự tạo local-only listener `127.0.0.1:1883` khi không còn `listener` explicit; vì vậy “xóa file rồi 1883 phải biến mất” là assertion sai. Chuẩn bị đầy đủ certificate/password/ACL/TLS config trước, sau đó mới xóa file local và restart một lần.

Ví dụ **lab-only** tạo CA/server cert trên VM (đặt `YOUR_LAB_HOST` đúng hostname/IP lab):

```bash
(
  set -Eeuo pipefail
  umask 077
  sudo install -d -o root -g mosquitto -m 0750 /etc/mosquitto/certs
  sudo install -d -o root -g iotapp -m 0750 /etc/iot-dashboard
  TLS_WORK=$(mktemp -d)
  cleanup_tls_work() { rm -rf -- "$TLS_WORK"; }
  trap cleanup_tls_work EXIT
  cd "$TLS_WORK"
  openssl req -x509 -newkey rsa:2048 -nodes -days 7 -subj '/CN=IoT Lab CA' -keyout lab-ca.key -out lab-ca.crt
  openssl req -newkey rsa:2048 -nodes -subj '/CN=YOUR_LAB_HOST' -addext 'subjectAltName=DNS:YOUR_LAB_HOST' -keyout lab-server.key -out lab-server.csr
  printf '%s\n' 'subjectAltName=DNS:YOUR_LAB_HOST' 'extendedKeyUsage=serverAuth' 'keyUsage=digitalSignature,keyEncipherment' > lab-server.ext
  openssl x509 -req -days 7 -in lab-server.csr -CA lab-ca.crt -CAkey lab-ca.key -CAcreateserial -extfile lab-server.ext -out lab-server.crt
  openssl x509 -in lab-server.crt -noout -subject -issuer -dates -ext subjectAltName
  sudo install -o root -g mosquitto -m 0644 lab-ca.crt lab-server.crt /etc/mosquitto/certs/
  sudo install -o root -g mosquitto -m 0640 lab-server.key /etc/mosquitto/certs/
  sudo install -o root -g iotapp -m 0640 lab-ca.crt /etc/iot-dashboard/mqtt-ca.crt
  install -d -m 0700 "$HOME/.config"
  install -m 0644 lab-ca.crt "$HOME/.config/mqtt-lab-ca.crt"
)
sudo mosquitto_passwd -c /etc/mosquitto/passwd sensor-001
sudo mosquitto_passwd /etc/mosquitto/passwd sensor-002
sudo mosquitto_passwd /etc/mosquitto/passwd iot-worker
sudo chown root:mosquitto /etc/mosquitto/passwd
sudo chmod 0640 /etc/mosquitto/passwd
sudo tee /etc/mosquitto/acl >/dev/null <<'ACL'
user sensor-001
topic write lab/devices/sensor-001/telemetry
user sensor-002
topic write lab/devices/sensor-002/telemetry
user iot-worker
topic read lab/devices/+/telemetry
ACL
sudo chown root:mosquitto /etc/mosquitto/passwd /etc/mosquitto/acl
sudo chmod 0640 /etc/mosquitto/passwd /etc/mosquitto/acl
```

CA private key trong bài chỉ dùng để ký certificate server lab một lần rồi bị trap xóa cùng workspace tạm; bản cài đặt giữ server key và public CA cần thiết. Muốn cấp thêm certificate, tạo CA lab mới hoặc thiết kế nơi lưu CA key riêng có backup/quyền/lifecycle—không để CA key lâu dài trong `/tmp`.

Config public-style **chỉ sau khi cert/console/firewall đã chuẩn bị**:

```bash
(
  set -Eeuo pipefail
  sudo tee /etc/mosquitto/conf.d/iot-tls.conf >/dev/null <<'CONF'
listener 8883
cafile /etc/mosquitto/certs/lab-ca.crt
certfile /etc/mosquitto/certs/lab-server.crt
keyfile /etc/mosquitto/certs/lab-server.key
allow_anonymous false
password_file /etc/mosquitto/passwd
acl_file /etc/mosquitto/acl
CONF
  sudo test -f /etc/mosquitto/conf.d/iot-lab-local.conf
  sudo rm -- /etc/mosquitto/conf.d/iot-lab-local.conf
  # Mosquitto 2.0 has no parser-only validator; restart is guarded and rollback follows.
  if ! sudo systemctl restart mosquitto; then
    sudo journalctl -u mosquitto -n 50 --no-pager
    sudo test ! -e /etc/mosquitto.failed-iot-03
    sudo mv /etc/mosquitto /etc/mosquitto.failed-iot-03
    sudo tar -xzf "$HOME/mosquitto-before-iot-03.tgz" -C /
    sudo systemctl restart mosquitto
    printf 'Failed config preserved at /etc/mosquitto.failed-iot-03\n' >&2
    exit 1
  fi
  sudo systemctl status mosquitto --no-pager
  sudo journalctl -u mosquitto -n 30 --no-pager
  if sudo ss -H -lnt '( sport = :1883 )' | grep -q .; then
    printf 'Plaintext MQTT listener is still present; inspect config before continuing\n' >&2
    exit 1
  fi
  test -n "$(sudo ss -H -lnt '( sport = :8883 )')"
  sudo ss -lnt '( sport = :8883 )'
)
```

Subshell dừng trước các test client nếu listener policy sai. Khi TLS listener đã start nhưng assertion 1883 fail, không restore mù: đọc `mosquitto -v`/journal và effective config ownership, sửa nguồn tạo listener rồi chạy lại assertion.

Dùng client option đã hỗ trợ phổ biến thay vì phụ thuộc option-file theo phiên bản. Password được nhập ẩn nên không nằm literal trong history, nhưng `-P` vẫn có thể thoáng xuất hiện trong process list; chỉ làm trên VM disposable không có user khác. CA client là bản public ở `$HOME/.config/mqtt-lab-ca.crt`:

```bash
read -rsp 'Password for sensor-001: ' MQTT_PASSWORD; printf '\n'
NOW=$(date -u +%FT%TZ)
MID=$(cat /proc/sys/kernel/random/uuid)
mosquitto_pub -V mqttv5 --cafile "$HOME/.config/mqtt-lab-ca.crt" -h YOUR_LAB_HOST -p 8883 -q 1 -u sensor-001 -P "$MQTT_PASSWORD" -t 'lab/devices/sensor-001/telemetry' -m "{\"message_id\":\"$MID\",\"device_id\":\"sensor-001\",\"device_time\":\"$NOW\",\"temperature\":22,\"humidity\":55}"
```

Kiểm ACL âm tính bằng cùng credential nhưng topic B; dùng MQTT v5 + QoS 1 để broker trả PUBACK reason code mà client có thể biến thành nonzero. MQTT 3.1.1 không cho publisher một negative acknowledgement tương đương, nên exit status không phải bằng chứng ACL đáng tin trong case đó. Sau đó revoke và chứng minh credential cũ thất bại. Các lệnh dự kiến lỗi nằm trong condition để không thay shell option của phiên tương tác:

```bash
(
  set -Eeuo pipefail
  if mosquitto_pub -V mqttv5 --cafile "$HOME/.config/mqtt-lab-ca.crt" -h YOUR_LAB_HOST -p 8883 -q 1 -u sensor-001 -P "$MQTT_PASSWORD" -t 'lab/devices/sensor-002/telemetry' -m '{"message_id":"00000000-0000-4000-8000-000000000002","device_id":"sensor-002","temperature":22,"humidity":55}'; then
    printf 'FAIL: broker accepted credential A on topic B\n' >&2
    exit 1
  else
    WRONG_TOPIC_STATUS=$?
    printf 'PASS: wrong topic rejected (status=%s)\n' "$WRONG_TOPIC_STATUS"
  fi
  sudo cp -a /etc/mosquitto/passwd /etc/mosquitto/passwd.before-revoke
  sudo mosquitto_passwd -D /etc/mosquitto/passwd sensor-001
  if ! sudo systemctl reload mosquitto; then
    sudo cp -a /etc/mosquitto/passwd.before-revoke /etc/mosquitto/passwd
    sudo chown root:mosquitto /etc/mosquitto/passwd
    sudo chmod 0640 /etc/mosquitto/passwd
    sudo systemctl restart mosquitto
    exit 1
  fi
  sudo systemctl is-active --quiet mosquitto
  if mosquitto_pub -V mqttv5 --cafile "$HOME/.config/mqtt-lab-ca.crt" -h YOUR_LAB_HOST -p 8883 -q 1 -u sensor-001 -P "$MQTT_PASSWORD" -t 'lab/devices/sensor-001/telemetry' -m '{"message_id":"00000000-0000-4000-8000-000000000003","device_id":"sensor-001","temperature":22,"humidity":55}'; then
    printf 'FAIL: revoked credential opened a new connection\n' >&2
    exit 1
  else
    REVOKED_STATUS=$?
    printf 'PASS: revoked credential rejected (status=%s)\n' "$REVOKED_STATUS"
  fi
)
unset MQTT_PASSWORD
```

Nếu wrong-topic test không bị từ chối, subshell dừng **trước khi revoke** để giữ credential hiện tại cho chẩn đoán ACL. Sau khi block thành công, `sensor-001` đã bị revoke có chủ đích.

Với client đã kết nối từ trước, sửa/reload password file không nhất thiết cưỡng bức ngắt session hiện tại; test ở trên chứng minh **kết nối mới** bằng credential đã revoke bị từ chối. Nếu threat model yêu cầu cắt ngay session đang sống, disconnect/kick client theo khả năng broker hoặc restart có kiểm soát rồi chứng minh old session không còn publish được.

Tạo lại `sensor-001` bằng `sudo mosquitto_passwd /etc/mosquitto/passwd sensor-001`, reload và ghi password mới vào password file của simulator khi học IOT-06; không lưu password trong evidence.

**Bằng chứng đạt:** TLS verify bằng CA; plaintext listener 1883 absent; TLS listener 8883 present; correct topic succeeds; wrong topic và kết nối mới bằng credential đã revoke thất bại; payload identity enforcement remains trách nhiệm của worker.

**Break/fix:** sai hostname/SAN hoặc CA; đọc TLS/client error, kiểm cert dates/SAN/permissions/listener. Không dùng `--insecure` làm pass.

**Rollback/reset:** nếu kết thúc toàn nhánh MQTT, dừng listener, restore config backup, rotate/remove lab credentials/certs và xóa private key lab; trên public VPS phải giữ SSH và console. Nếu học tiếp IOT-04/IOT-06, **không restore về anonymous local listener**: tạo lại `sensor-001`, giữ TLS/ACL/CA, rồi chứng minh kết nối mới và topic đúng hoạt động. Đây là resume contract bắt buộc.

**Checkpoint:** tạo hai device identity, ACL riêng, test publish đúng/sai/revoke và lưu bằng chứng status/log không chứa password; cuối checkpoint ghi rõ state chuyển giao là “TLS pass + `sensor-001` active” hoặc “nhánh MQTT đã cleanup”, không để trạng thái mơ hồ.

**Feynman:** TLS khác ACL? Password file khác device registry? Vì sao anonymous không được dùng public?

<a id="iot-04"></a>

### IOT-04 — Worker validate, ghi PostgreSQL và reconnect

**Cần trước:** IOT-01, IOT-03, DATA-02, SYS-03. **Mục tiêu:** subscribe topic, validate JSON/device/range/time, ghi DB idempotent cơ bản và reconnect khi broker tạm dừng. **Rủi ro:** 🟠.

**Preflight — VM:** IOT-03 đang ở trạng thái TLS pass **và `sensor-001` đã được tạo lại sau bài revoke**; PostgreSQL/schema/passfile từ IOT-01 còn hoạt động; snapshot VM; ghi count hiện tại và giữ message ID riêng cho từng test. Không dùng sensor/credential production. Nếu IOT-03 vừa kết thúc ở trạng thái revoked, chạy bước tạo lại credential cuối IOT-03, reload broker và test kết nối mới trước khi tiếp tục.

**Loại suy:** worker là nhân viên nhập kho; broker là băng chuyền. Giới hạn: process restart không tự giải quyết duplicate, message ordering hoặc mất dữ liệu nếu không có policy.

**Tự hỏi:** Nếu worker nhận cùng message hai lần, database có được phép tạo hai record không?

**Chốt lại:** tùy nghiệp vụ; phải có message/device sequence hoặc idempotency key và policy retry được ghi rõ.

**Lab — VM:** DATA-02 đã tạo unique key `(device_id, message_id)`. Tạo source/venv worker:

```bash
WORKER_SRC="$HOME/linux-course/iot-worker"
mkdir -p "$WORKER_SRC"
cd "$WORKER_SRC"
python3 -m venv .venv
.venv/bin/pip install 'paho-mqtt>=2,<3' 'psycopg[binary]>=3.1,<4'
cat > worker.py <<'PY'
import json
import logging
import math
import os
import re
import uuid
from datetime import datetime, timezone

import paho.mqtt.client as mqtt
import psycopg

logging.basicConfig(level=logging.INFO, format="%(levelname)s %(message)s")
TOPIC_RE = re.compile(r"^lab/devices/([a-z0-9-]{3,32})/telemetry$")

def parse_payload(topic: str, raw: bytes) -> dict:
    match = TOPIC_RE.fullmatch(topic)
    if not match:
        raise ValueError("topic_not_allowed")
    data = json.loads(raw)
    required = {"message_id", "device_id", "device_time", "temperature", "humidity"}
    if set(data) != required:
        raise ValueError("unexpected_fields")
    if not isinstance(data["device_id"], str) or not re.fullmatch(r"[a-z0-9-]{3,32}", data["device_id"]):
        raise ValueError("device_id_format")
    if data["device_id"] != match.group(1):
        raise ValueError("topic_identity_mismatch")
    if not isinstance(data["message_id"], str):
        raise ValueError("message_id_format")
    data["message_id"] = str(uuid.UUID(data["message_id"]))
    if isinstance(data["temperature"], bool) or not isinstance(data["temperature"], (int, float)) or not -40 <= data["temperature"] <= 85:
        raise ValueError("temperature_range")
    if isinstance(data["humidity"], bool) or not isinstance(data["humidity"], (int, float)) or not 0 <= data["humidity"] <= 100:
        raise ValueError("humidity_range")
    if isinstance(data["temperature"], float) and not math.isfinite(data["temperature"]):
        raise ValueError("temperature_not_finite")
    if isinstance(data["humidity"], float) and not math.isfinite(data["humidity"]):
        raise ValueError("humidity_not_finite")
    raw_time = data["device_time"]
    if not isinstance(raw_time, str) or not (raw_time.endswith("Z") or re.search(r"[+-]\d{2}:\d{2}$", raw_time)):
        raise ValueError("timezone_required")
    try:
        parsed = datetime.fromisoformat(raw_time.replace("Z", "+00:00"))
    except ValueError as exc:
        raise ValueError("timestamp_format") from exc
    if parsed.tzinfo is None:
        raise ValueError("timezone_required")
    if abs((parsed - datetime.now(timezone.utc)).total_seconds()) > 86400:
        raise ValueError("timestamp_window")
    return data

def db_connect():
    host = os.environ.get("DATABASE_HOST", "127.0.0.1")
    database = os.environ.get("DATABASE_NAME", "iot_dashboard")
    user = os.environ.get("DATABASE_USER", "iotapp")
    kwargs = {"host": host, "dbname": database, "user": user}
    passfile = os.environ.get("PGPASSFILE")
    password_file = os.environ.get("DATABASE_PASSWORD_FILE")
    if passfile:
        kwargs["passfile"] = passfile
    elif password_file:
        with open(password_file, encoding="utf-8") as secret:
            kwargs["password"] = secret.read().strip()
    else:
        raise RuntimeError("configure PGPASSFILE or DATABASE_PASSWORD_FILE")
    return psycopg.connect(**kwargs)


def insert_reading(data: dict) -> bool:
    with db_connect() as conn:
        with conn.cursor() as cur:
            cur.execute(
                """INSERT INTO readings
                   (device_id, message_id, temperature, humidity, device_time)
                   VALUES (%s, %s, %s, %s, %s)
                   ON CONFLICT (device_id, message_id) DO NOTHING
                   RETURNING id""",
                (data["device_id"], data["message_id"], data["temperature"], data["humidity"], data["device_time"]),
            )
            return cur.fetchone() is not None


def on_connect(client, userdata, flags, reason_code, properties):
    if reason_code != 0:
        logging.error("mqtt_connect_failed reason=%s", reason_code)
        return
    logging.info("mqtt_connected")
    client.subscribe("lab/devices/+/telemetry", qos=1)

def on_disconnect(client, userdata, disconnect_flags, reason_code, properties):
    logging.warning("mqtt_disconnected reason=%s", reason_code)

def on_message(client, userdata, message):
    try:
        data = parse_payload(message.topic, message.payload)
    except Exception as exc:
        logging.error("ingest_rejected reason=%s topic=%s", str(exc), message.topic)
        client.ack(message.mid, message.qos)  # Invalid payloads are not retried.
        return
    try:
        inserted = insert_reading(data)
    except Exception:
        logging.exception("ingest_retryable topic=%s", message.topic)
        logging.shutdown()
        os._exit(1)  # No ACK; supervisor restarts and the persistent session can redeliver.
    logging.info("ingest result=%s device=%s message_id=%s", "inserted" if inserted else "duplicate", data["device_id"], data["message_id"])
    client.ack(message.mid, message.qos)

client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2, client_id="iot-worker", protocol=mqtt.MQTTv311, clean_session=False, manual_ack=True)
client.username_pw_set("iot-worker", open(os.environ["MQTT_PASSWORD_FILE"], encoding="utf-8").read().strip())
client.tls_set(ca_certs=os.environ["MQTT_CA_FILE"])
client.reconnect_delay_set(min_delay=1, max_delay=30)
client.on_connect = on_connect
client.on_disconnect = on_disconnect
client.on_message = on_message
client.connect_async(os.environ["MQTT_HOST"], int(os.environ.get("MQTT_PORT", "8883")), keepalive=30)
client.loop_forever(retry_first_connection=True)
PY
```

Cấp credential bằng prompt, không đưa password vào command line/history. Password nhập phải khớp user `iot-worker` đã tạo ở IOT-03:

```bash
sudo install -d -o root -g iotapp -m 0750 /etc/iot-dashboard
sudo install -o root -g iotapp -m 0640 /dev/null /etc/iot-dashboard/iot-worker.mqtt-password
read -rsp 'MQTT password for iot-worker: ' MQTT_WORKER_PASSWORD; printf '\n'
printf '%s\n' "$MQTT_WORKER_PASSWORD" | sudo tee /etc/iot-dashboard/iot-worker.mqtt-password >/dev/null
unset MQTT_WORKER_PASSWORD
sudo test -f /etc/iot-dashboard/iotapp.pgpass
sudo chown iotapp:iotapp /etc/iot-dashboard/iotapp.pgpass
sudo chmod 0600 /etc/iot-dashboard/iotapp.pgpass
sudo tee /etc/iot-dashboard/worker.env >/dev/null <<'EOF'
MQTT_HOST=YOUR_LAB_HOST
MQTT_CA_FILE=/etc/iot-dashboard/mqtt-ca.crt
MQTT_PASSWORD_FILE=/etc/iot-dashboard/iot-worker.mqtt-password
PGPASSFILE=/etc/iot-dashboard/iotapp.pgpass
EOF
sudo chown root:iotapp /etc/iot-dashboard/worker.env
sudo chmod 0640 /etc/iot-dashboard/worker.env
```

Cài worker release và unit non-root:

```bash
sudo install -d -o root -g iotapp -m 0750 /srv/iot-worker
sudo cp -a "$WORKER_SRC/worker.py" /srv/iot-worker/
sudo python3 -m venv /srv/iot-worker/.venv
sudo /srv/iot-worker/.venv/bin/pip install 'paho-mqtt>=2,<3' 'psycopg[binary]>=3.1,<4'
sudo chown -R root:iotapp /srv/iot-worker
sudo find /srv/iot-worker -type d -exec chmod 0750 {} +
sudo find /srv/iot-worker -type f -exec chmod 0640 {} +
sudo find /srv/iot-worker -type f -path '*/.venv/bin/*' -exec chmod 0750 {} +
sudo -u iotapp /srv/iot-worker/.venv/bin/python -c 'import paho.mqtt.client, psycopg'
sudo install -d -o root -g iotapp -m 0750 /etc/iot-dashboard
sudo chown root:iotapp /etc/iot-dashboard/iot-worker.mqtt-password /etc/iot-dashboard/worker.env
sudo chmod 0640 /etc/iot-dashboard/iot-worker.mqtt-password /etc/iot-dashboard/worker.env
sudo chown iotapp:iotapp /etc/iot-dashboard/iotapp.pgpass
sudo chmod 0600 /etc/iot-dashboard/iotapp.pgpass
sudo tee /etc/systemd/system/iot-worker.service >/dev/null <<'UNIT'
[Unit]
Description=MQTT to PostgreSQL ingestion worker
After=network-online.target mosquitto.service postgresql.service
Wants=network-online.target

[Service]
User=iotapp
Group=iotapp
WorkingDirectory=/srv/iot-worker
EnvironmentFile=/etc/iot-dashboard/worker.env
ExecStart=/srv/iot-worker/.venv/bin/python /srv/iot-worker/worker.py
Restart=on-failure
RestartSec=5
NoNewPrivileges=true
PrivateTmp=true
ProtectHome=true
ProtectSystem=strict

[Install]
WantedBy=multi-user.target
UNIT
sudo systemd-analyze verify /etc/systemd/system/iot-worker.service
sudo systemctl daemon-reload
sudo systemctl enable --now iot-worker
sudo systemctl status iot-worker --no-pager
sudo journalctl -u iot-worker -n 50 --no-pager
```

Publish cùng `message_id` hai lần qua client được cấu hình an toàn ở IOT-03, rồi query count. Message hợp lệ phải `inserted` lần đầu, `duplicate` lần hai và DB count vẫn `1`. Gửi topic/device/range/time sai phải ghi `ingest_rejected` và không tăng count.

Tách failure drill thành hai case, vì broker down không thể nhận message còn worker down với broker sống mới kiểm được persistent session/replay.

**Case A — broker down/reconnect:** dừng broker, xác nhận publish từ client mới fail; bật broker và kiểm worker reconnect/resubscribe:

```bash
sudo systemctl stop mosquitto
if mosquitto_pub -V mqttv5 --cafile "$HOME/.config/mqtt-lab-ca.crt" -h YOUR_LAB_HOST -p 8883 -q 1 -t 'lab/devices/sensor-001/telemetry' -m '{}'; then
  printf 'FAIL: publish unexpectedly succeeded while broker was stopped\n' >&2
  false
else
  BROKER_DOWN_PUBLISH_STATUS=$?
  printf 'PASS: broker-down publish failed (status=%s)\n' "$BROKER_DOWN_PUBLISH_STATUS"
fi
sudo systemctl start mosquitto
sudo journalctl -u iot-worker --since '2 minutes ago' --no-pager
```

Case A không cần credential: TCP/TLS connect phải fail vì broker đã dừng; message thử `{}` không được tính là telemetry và không thể “replay” vì broker đã down trước lúc publish.

**Case B — worker offline/replay qua broker restart:** broker vẫn chạy và package persistence đã được xác minh ở IOT-03. Dừng worker, publish QoS 1 bằng `sensor-001`, restart broker khi worker còn dừng, rồi mới bật worker. Persistent session `client_id=iot-worker`, `clean_session=False` phải nhận message queued từ disk; DB có đúng một row theo `message_id`. Publish lại cùng payload sau đó để chứng minh idempotency vẫn giữ một row.

```bash
read -rsp 'Current sensor-001 password: ' CURRENT_DEVICE_PASSWORD; printf '\n'
sudo systemctl stop iot-worker
QUEUED_MID=$(cat /proc/sys/kernel/random/uuid)
QUEUED_NOW=$(date -u +%FT%TZ)
QUEUED_BODY=$(printf '{"message_id":"%s","device_id":"sensor-001","device_time":"%s","temperature":22,"humidity":55}' "$QUEUED_MID" "$QUEUED_NOW")
mosquitto_pub -V mqttv5 --cafile "$HOME/.config/mqtt-lab-ca.crt" -h YOUR_LAB_HOST -p 8883 -q 1 -u sensor-001 -P "$CURRENT_DEVICE_PASSWORD" -t 'lab/devices/sensor-001/telemetry' -m "$QUEUED_BODY"
sudo systemctl restart mosquitto
sudo systemctl is-active --quiet mosquitto
sudo systemctl start iot-worker
QUEUED_COUNT=0
for attempt in $(seq 1 30); do
  QUEUED_COUNT=$(sudo -u postgres psql -At -d iot_dashboard -c "SELECT count(*) FROM readings WHERE message_id='$QUEUED_MID';")
  test "$QUEUED_COUNT" = 1 && break
  sleep 1
done
test "$QUEUED_COUNT" = 1
mosquitto_pub -V mqttv5 --cafile "$HOME/.config/mqtt-lab-ca.crt" -h YOUR_LAB_HOST -p 8883 -q 1 -u sensor-001 -P "$CURRENT_DEVICE_PASSWORD" -t 'lab/devices/sensor-001/telemetry' -m "$QUEUED_BODY"
unset CURRENT_DEVICE_PASSWORD QUEUED_BODY
sleep 3
QUEUED_COUNT_AFTER_DUPLICATE=$(sudo -u postgres psql -At -d iot_dashboard -c "SELECT count(*) FROM readings WHERE message_id='$QUEUED_MID';")
test "$QUEUED_COUNT_AFTER_DUPLICATE" = 1
```

**Bằng chứng đạt:** service/code/config tồn tại; message hợp lệ ghi một row; duplicate không nhân bản; message sai bị từ chối; broker-down publish fail rõ ràng; journal có disconnect/reconnect/subscription; message QoS 1 được broker nhận khi worker offline vẫn tồn tại qua broker restart, được replay khi worker start và vẫn đúng một row sau khi publish trùng.

**Break/fix:** sai topic, CA/hostname, MQTT password hoặc DB passfile; phân biệt broker TLS/auth, subscription, validation và DB insert qua log không chứa secret.

**Rollback/reset:**

```bash
sudo systemctl disable --now iot-worker
sudo rm -- /etc/systemd/system/iot-worker.service
sudo systemctl daemon-reload
sudo rm -r -- /srv/iot-worker
sudo rm -- /etc/iot-dashboard/worker.env /etc/iot-dashboard/iot-worker.mqtt-password
```

Giữ `/etc/iot-dashboard/iotapp.pgpass` vì API từ IOT-01/IOT-05 còn dùng; chỉ xóa passfile khi đã dừng mọi consumer và có kế hoạch tạo lại. Xóa record lab bằng `(device_id, message_id)` cụ thể; không `TRUNCATE` database dùng chung.

**Checkpoint:** gửi valid/invalid/duplicate, broker-down publish-fail và worker-offline replay cases; hoàn thành bảng expected status, DB count, log evidence; restart/reboot worker vẫn hoạt động và duplicate không nhân row.

**Feynman:** at-least-once có thể duplicate vì sao? Reconnect khác replay? Worker cần backpressure nào khi DB chậm?

<a id="iot-05"></a>

### IOT-05 — Lịch sử API, WebSocket/SSE và freshness

**Cần trước:** IOT-01, IOT-04. **Mục tiêu:** dashboard đọc lịch sử từ API, nhận điểm mới gần realtime, hiển thị stale/reconnect và không mất lịch sử khi socket hỏng. Vì generator SSE dùng driver PostgreSQL blocking, lab phải chuyển mỗi lần đọc batch qua `anyio.to_thread.run_sync`; không gọi blocking DB trực tiếp trong `async def`. **Rủi ro:** 🟡/🟠.

**Preflight — VM/VPS lab:** API đang chạy với DB passfile; Nginx site hiện tại đã backup và `nginx -t` pass; có ít nhất một row lịch sử và một message ID mới để đo; browser/client ở môi trường có thể gửi HTTPS. `proxy_buffering off` được thêm và kiểm ở chính bài này, không phải điều kiện đã tồn tại. Snapshot trước khi sửa dashboard.

**Loại suy:** API là sổ lưu trữ; WebSocket/SSE là chuông báo điểm mới. Giới hạn: chuông không phải sổ, không bảo đảm replay/ordering/delivery nếu client ngắt.

**Tự hỏi:** WebSocket mất kết nối có nên làm dashboard mất toàn bộ lịch sử không?

**Chốt lại:** không. Lịch sử phải truy vấn lại qua API; realtime chỉ là tối ưu hiển thị và cần reconnect/freshness.

**Lab:** Core chọn **SSE**; WebSocket chỉ là phần so sánh. Cài dependency DB nếu chưa có và thêm history/SSE vào app. Query phải dùng parameterized SQL, page limit và device authorization. Route `def readings()` được FastAPI chạy trong thread pool; còn công việc nằm bên trong async generator không tự được offload, nên `fetch_rows` blocking phải được gọi qua AnyIO:

```python
import anyio
import asyncio
import json
from fastapi import Query
from fastapi.responses import StreamingResponse
from psycopg.rows import dict_row

@app.get("/api/readings")
def readings(
    device_id: str,
    after_id: int = Query(default=0, ge=0),
    limit: int = Query(default=100, ge=1, le=500),
    x_device_key: str = Header(default=""),
):
    device = DEVICE_KEYS.get(x_device_key)
    if not device:
        raise HTTPException(status_code=401, detail="invalid device credential")
    if device != device_id:
        raise HTTPException(status_code=403, detail="device identity mismatch")
    with db_connect(row_factory=dict_row) as conn, conn.cursor() as cur:
        cur.execute(
            """SELECT id, message_id, device_id, temperature, humidity,
                      device_time, received_at
               FROM readings
               WHERE device_id=%s AND id>%s
               ORDER BY id ASC LIMIT %s""",
            (device_id, after_id, limit),
        )
        return cur.fetchall()

@app.get("/api/events")
async def events(
    device_id: str,
    after_id: int = Query(default=0, ge=0),
    x_device_key: str = Header(default=""),
):
    device = DEVICE_KEYS.get(x_device_key)
    if not device:
        raise HTTPException(status_code=401, detail="invalid device credential")
    if device != device_id:
        raise HTTPException(status_code=403, detail="device identity mismatch")

    def fetch_rows(after_id: int):
        with db_connect(row_factory=dict_row) as conn, conn.cursor() as cur:
            cur.execute(
                """SELECT id, message_id, device_id, temperature, humidity,
                          device_time, received_at
                   FROM readings
                   WHERE device_id=%s AND id>%s
                   ORDER BY id ASC LIMIT 100""",
                (device_id, after_id),
            )
            return cur.fetchall()

    async def stream():
        cursor = after_id
        while True:
            rows = await anyio.to_thread.run_sync(fetch_rows, cursor)
            for row in rows:
                cursor = row["id"]
                yield f"id: {cursor}\ndata: {json.dumps(row, default=str)}\n\n"
            if not rows:
                yield ": keepalive\n\n"
            await asyncio.sleep(2)

    return StreamingResponse(
        stream(),
        media_type="text/event-stream",
        headers={
            "Cache-Control": "no-cache",
            "X-Accel-Buffering": "no",
        },
    )
```

Nginx phải giữ stream thay vì buffer toàn bộ response. Thêm location riêng trước location tổng quát, rồi `nginx -t` validate trước reload:

```nginx
location = /api/events {
    proxy_pass http://127.0.0.1:8080;
    proxy_http_version 1.1;
    proxy_buffering off;
    proxy_cache off;
    proxy_read_timeout 75s;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
}
```

Keepalive hiện phát mỗi 2 giây, ngắn hơn `proxy_read_timeout`; nếu đổi hai giá trị này, luôn giữ timeout edge dài hơn khoảng keepalive. SSE native `EventSource` không gửi custom header. Với device key, client Core dùng `fetch()` stream hoặc cookie/session HttpOnly cho người dùng web; lab CLI kiểm header bằng curl:

```bash
curl -fsS -H 'X-Device-Key: demo-key-a' 'http://127.0.0.1:8080/api/readings?device_id=sensor-001&after_id=0&limit=10'
curl -N --max-time 10 -H 'X-Device-Key: demo-key-a' 'http://127.0.0.1:8080/api/events?device_id=sensor-001&after_id=0'
```

Thay hàm `dashboard()` health-only bằng client lab dưới đây. Client không đặt key trong URL; nó bắt người học nhập key giả của lab, tải catch-up theo `after_id`, dedupe theo ID, đọc SSE bằng `fetch()`, báo stale và reconnect. Đây chưa phải UI production hoặc cơ chế session cho người dùng thật:

```python
@app.get("/", response_class=HTMLResponse)
def dashboard():
    return """<!doctype html>
<html lang="vi">
<meta charset="utf-8">
<title>IoT Dashboard Lab</title>
<style>
body { font: 16px system-ui; max-width: 900px; margin: 2rem auto; padding: 0 1rem; }
.controls { display: flex; gap: .5rem; flex-wrap: wrap; }
#status[data-state="stale"] { color: #b45309; }
#status[data-state="live"] { color: #15803d; }
table { width: 100%; border-collapse: collapse; margin-top: 1rem; }
th, td { border-bottom: 1px solid #ddd; padding: .5rem; text-align: left; }
</style>
<h1>Dashboard nhiệt độ/độ ẩm</h1>
<div class="controls">
  <label>Device <input id="device" value="sensor-001"></label>
  <label>Lab key <input id="key" type="password" value="demo-key-a"></label>
  <button id="connect">Kết nối</button>
</div>
<p id="status" data-state="stale">Chưa kết nối</p>
<table>
  <thead><tr><th>ID</th><th>Device</th><th>°C</th><th>%RH</th><th>Received UTC</th></tr></thead>
  <tbody id="rows"></tbody>
</table>
<script>
let lastId = 0;
let lastReceivedAt = null;
let lastEventAt = 0;
let streamOpenedAt = 0;
let generation = 0;
const seenIds = new Set();
const statusNode = document.querySelector('#status');
const rowsNode = document.querySelector('#rows');

function nextReconnectDelayMs(attempt) {
  const normalizedAttempt = Number.isFinite(attempt) ? Math.max(1, Math.floor(attempt)) : 1;
  return Math.min(1000 * (2 ** (normalizedAttempt - 1)), 30000);
}

const sleep = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));

function setStatus(text, state = 'stale') {
  statusNode.textContent = text;
  statusNode.dataset.state = state;
}

function addReading(row, source = 'history') {
  if (seenIds.has(row.id)) return;
  seenIds.add(row.id);
  lastId = Math.max(lastId, row.id);
  lastReceivedAt = row.received_at;
  if (source === 'sse') lastEventAt = Date.now();
  const tr = document.createElement('tr');
  for (const value of [row.id, row.device_id, row.temperature, row.humidity, row.received_at]) {
    const td = document.createElement('td');
    td.textContent = String(value);
    tr.appendChild(td);
  }
  rowsNode.appendChild(tr);
}

async function requestJson(path, key) {
  const response = await fetch(path, {headers: {'X-Device-Key': key}});
  if (!response.ok) throw new Error(`HTTP ${response.status}`);
  return response.json();
}

async function catchUp(device, key, currentGeneration) {
  while (currentGeneration === generation) {
    const query = new URLSearchParams({device_id: device, after_id: lastId, limit: 500});
    const page = await requestJson(`/api/readings?${query}`, key);
    if (currentGeneration !== generation) return;
    page.forEach(row => addReading(row));
    if (page.length < 500) return;
  }
}

async function consumeSse(response, currentGeneration) {
  if (!response.body) throw new Error('response body is not streamable');
  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  while (currentGeneration === generation) {
    const {value, done} = await reader.read();
    if (currentGeneration !== generation) {
      await reader.cancel();
      return;
    }
    if (done) throw new Error('SSE stream ended');
    buffer += decoder.decode(value, {stream: true});
    const blocks = buffer.split('\n\n');
    buffer = blocks.pop() ?? '';
    for (const block of blocks) {
      const data = block.split('\n')
        .filter(line => line.startsWith('data:'))
        .map(line => line.slice(5).trim())
        .join('\n');
      if (data) {
        addReading(JSON.parse(data), 'sse');
        setStatus(`Live — last id ${lastId}`, 'live');
      }
    }
  }
  await reader.cancel();
}

async function connectLoop(device, key, currentGeneration) {
  let attempt = 0;
  let connectedAt = 0;
  while (currentGeneration === generation) {
    try {
      setStatus('Đang đồng bộ lịch sử…');
      await catchUp(device, key, currentGeneration);
      if (currentGeneration !== generation) return;
      const query = new URLSearchParams({device_id: device, after_id: lastId});
      const response = await fetch(`/api/events?${query}`, {headers: {'X-Device-Key': key}});
      if (currentGeneration !== generation) {
        await response.body?.cancel();
        return;
      }
      if (!response.ok) throw new Error(`SSE HTTP ${response.status}`);
      connectedAt = Date.now();
      streamOpenedAt = connectedAt;
      setStatus(`Live — last id ${lastId}`, 'live');
      await consumeSse(response, currentGeneration);
    } catch (error) {
      if (currentGeneration !== generation) return;
      if (streamOpenedAt && Date.now() - streamOpenedAt >= 30000) attempt = 0;
      connectedAt = 0;
      streamOpenedAt = 0;
      attempt += 1;
      const delay = nextReconnectDelayMs(attempt);
      setStatus(`Stale/reconnecting in ${delay} ms — ${error.message}`);
      await sleep(delay);
    }
  }
}

document.querySelector('#connect').addEventListener('click', () => {
  generation += 1;
  lastId = 0;
  lastReceivedAt = null;
  lastEventAt = 0;
  streamOpenedAt = 0;
  seenIds.clear();
  rowsNode.replaceChildren();
  const device = document.querySelector('#device').value.trim();
  const key = document.querySelector('#key').value;
  connectLoop(device, key, generation);
});

setInterval(() => {
  const freshnessAnchor = lastEventAt || streamOpenedAt;
  if (freshnessAnchor && Date.now() - freshnessAnchor > 15000) {
    setStatus(`Stale — last receive ${lastReceivedAt ?? 'none'}`);
  }
}, 1000);
</script>
</html>"""
```

Sau khi commit source, tạo/activate release mới theo WEB-06; chạy `sudo nginx -t && sudo systemctl reload nginx`. Không xóa lịch sử khi SSE hỏng; mỗi reconnect luôn catch-up từ `lastId` trước khi mở stream mới. Trong browser console, kiểm policy deterministic:

```javascript
[0, 1, 2, 3, 4, 5, 6, 20].map(nextReconnectDelayMs)
// [1000, 1000, 2000, 4000, 8000, 16000, 30000, 30000]
```

**Bằng chứng đạt:** bảng backoff đúng; reload/history trả dữ liệu; SSE qua HTTPS/Nginx nhận điểm mới có `id` mà không chờ buffer; key/device chéo bị 401/403; disconnect hiển thị stale; reconnect catch-up theo `after_id` không bỏ record và UI dedupe theo `id`.

**Break/fix:** dừng worker/broker; history vẫn đọc được, SSE chỉ keepalive/stale; bật lại, publish mới và kiểm catch-up/reconnect.

**Rollback/reset:** revert commit app/client; giữ DB readings để kiểm restore; không dùng mock event làm bằng chứng pass.

**Checkpoint:** test 4 trạng thái history-only, SSE healthy qua HTTPS/Nginx, disconnect/stale, reconnect/catch-up; ghi last ID, timestamp, độ trễ điểm mới và record count.

**Feynman:** WebSocket/SSE khác polling? Freshness khác latency? Vì sao reconnect cần API history?

<a id="iot-06"></a>

### IOT-06 — Nhiều simulator, device identity và dashboard lọc

**Cần trước:** IOT-01, IOT-05. **Mục tiêu:** thêm ít nhất hai simulator có credential/topic riêng, dữ liệu không lẫn và dashboard lọc device. **Rủi ro:** 🟠.

**Loại suy:** mỗi device là một nhân viên có thẻ riêng; registry là danh sách nhân viên. Giới hạn: mã device không tự chứng minh phần cứng thật hoặc nguồn cung cấp identity.

**Lab — VM/staging:** tạo simulator MQTT lấy identity/config từ từng instance, không hard-code sensor-001:

```bash
SIM_SRC="$HOME/linux-course/iot-mqtt-simulator"
mkdir -p "$SIM_SRC"
cd "$SIM_SRC"
python3 -m venv .venv
.venv/bin/pip install 'paho-mqtt>=2,<3'
cat > simulator.py <<'PY'
import json
import os
import random
import time
import uuid
from datetime import datetime, timezone

import paho.mqtt.client as mqtt

device = os.environ["DEVICE_ID"]
client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2, client_id=device)
client.username_pw_set(device, open(os.environ["MQTT_PASSWORD_FILE"], encoding="utf-8").read().strip())
client.tls_set(ca_certs=os.environ["MQTT_CA_FILE"])
client.connect(os.environ["MQTT_HOST"], 8883, 30)
client.loop_start()
try:
    while True:
        payload = {
            "message_id": str(uuid.uuid4()),
            "device_id": device,
            "device_time": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
            "temperature": round(random.uniform(20, 30), 1),
            "humidity": round(random.uniform(40, 70), 1),
        }
        info = client.publish(f"lab/devices/{device}/telemetry", json.dumps(payload), qos=1)
        info.wait_for_publish()
        time.sleep(int(os.environ.get("INTERVAL_SECONDS", "5")))
finally:
    client.loop_stop()
PY
sudo install -d -o root -g iotapp -m 0750 /srv/iot-mqtt-simulator
sudo cp -a simulator.py /srv/iot-mqtt-simulator/
sudo python3 -m venv /srv/iot-mqtt-simulator/.venv
sudo /srv/iot-mqtt-simulator/.venv/bin/pip install 'paho-mqtt>=2,<3'
sudo chown -R root:iotapp /srv/iot-mqtt-simulator
sudo find /srv/iot-mqtt-simulator -type d -exec chmod 0750 {} +
sudo find /srv/iot-mqtt-simulator -type f -exec chmod 0640 {} +
sudo find /srv/iot-mqtt-simulator -type f -path '*/.venv/bin/*' -exec chmod 0750 {} +
sudo -u iotapp /srv/iot-mqtt-simulator/.venv/bin/python -c 'import paho.mqtt.client'
sudo tee /etc/systemd/system/iot-simulator@.service >/dev/null <<'UNIT'
[Unit]
Description=IoT MQTT simulator %i
After=network-online.target mosquitto.service
Wants=network-online.target

[Service]
User=iotapp
Group=iotapp
WorkingDirectory=/srv/iot-mqtt-simulator
EnvironmentFile=/etc/iot-dashboard/devices/%i.env
ExecStart=/srv/iot-mqtt-simulator/.venv/bin/python /srv/iot-mqtt-simulator/simulator.py
Restart=on-failure
RestartSec=5
NoNewPrivileges=true
ProtectHome=true
ProtectSystem=strict

[Install]
WantedBy=multi-user.target
UNIT
```

Tạo hai config/credential bằng prompt, không ghi password trong tài liệu/history:

```bash
sudo install -d -o root -g iotapp -m 0750 /etc/iot-dashboard/devices
for device in sensor-001 sensor-002; do
  sudo install -o root -g iotapp -m 0640 /dev/null "/etc/iot-dashboard/devices/$device.password"
  read -rsp "MQTT password for $device: " P; printf '\n'
  printf '%s\n' "$P" | sudo tee "/etc/iot-dashboard/devices/$device.password" >/dev/null
  unset P
  sudo tee "/etc/iot-dashboard/devices/$device.env" >/dev/null <<EOF
DEVICE_ID=$device
MQTT_HOST=YOUR_LAB_HOST
MQTT_CA_FILE=/etc/iot-dashboard/mqtt-ca.crt
MQTT_PASSWORD_FILE=/etc/iot-dashboard/devices/$device.password
INTERVAL_SECONDS=5
EOF
  sudo chown root:iotapp "/etc/iot-dashboard/devices/$device.env"
  sudo chmod 0640 "/etc/iot-dashboard/devices/$device.env"
done
sudo systemd-analyze verify /etc/systemd/system/iot-simulator@.service
sudo systemctl daemon-reload
sudo systemctl enable --now iot-simulator@sensor-001 iot-simulator@sensor-002
```

Kiểm broker/worker/DB:

```bash
sudo systemctl status iot-simulator@sensor-001 iot-simulator@sensor-002 --no-pager
sudo journalctl -u iot-worker --since '5 minutes ago' --no-pager
sudo -u postgres psql -d iot_dashboard -c "SELECT device_id, count(*) FROM readings WHERE received_at > now() - interval '5 minutes' GROUP BY device_id ORDER BY device_id;"
```

Dashboard/API phải kiểm device authorization; sửa URL không được vượt key/session server-side.

**Bằng chứng đạt:** hai instance/identity/topic riêng; count/filter đúng; credential A không publish B; dừng một instance không làm mất lịch sử/device kia.

**Break/fix:** đổi `DEVICE_ID` chéo với credential/topic; ACL hoặc worker phải từ chối, DB không nhận row chéo.

**Rollback/reset:**

```bash
sudo systemctl disable --now iot-simulator@sensor-001 iot-simulator@sensor-002
sudo rm -- /etc/systemd/system/iot-simulator@.service
sudo systemctl daemon-reload
sudo rm -r -- /srv/iot-mqtt-simulator /etc/iot-dashboard/devices
```

Revoke credential bằng `mosquitto_passwd -D` hoặc cơ chế phiên bản cài đặt, test revoked fail trước cleanup.

**Checkpoint:** thêm `sensor-003` bằng env/password/ACL, enable instance, test history/SSE/filter, rồi revoke/disable mà 001/002 vẫn hoạt động.

**Feynman:** device ID khác user identity? Registry giải quyết vòng đời gì? Vì sao authorization phải ở API chứ không chỉ dashboard filter?

## Cổng M5 — Pipeline IoT hoàn chỉnh

Chỉ đánh dấu đạt sau khi từng đoạn có implementation và evidence:

```text
simulator/device identity
  → MQTT TLS hoặc REST auth
  → broker/worker
  → validation
  → PostgreSQL (UTC + device/receive time)
  → history API
  → SSE near-real-time
  → dashboard freshness/reconnect
```

DATA-01/02 và IOT-02/03 chỉ là bằng chứng từng lớp; không suy diễn pipeline hoàn chỉnh từ các layer riêng lẻ. Mọi case valid, invalid, unauthorized và duplicate phải có expected result, log/DB evidence và rollback. Broker-down/replay được kiểm ở IOT-04; DB-down/freshness và incident evidence được kiểm ở OPS-04/OPS-08. Không public PostgreSQL, app port, MQTT plaintext hoặc anonymous broker.

---

## Cấp 6 — Vận hành đáng tin cậy

<a id="ops-01"></a>

### OPS-01 — Container sau native service

**Cần trước:** IOT-01, IOT-04. **Mục tiêu:** phân biệt image/container/process/kernel/volume/config và chạy app non-root có healthcheck. **Rủi ro:** 🟠.

**Tự hỏi:** Container image có bảo đảm app chạy giống nhau trên mọi máy không?

**Chốt lại:** không; architecture, kernel capabilities, volume, network, config, resource và secret còn phụ thuộc host.

**Lab — VM:** cài Docker Engine + Compose plugin theo nguồn chính thức hỗ trợ Ubuntu 24.04 hoặc package được quản trị trong môi trường của bạn. Kiểm socket/context. Docker daemon mode cần `sudo docker`; group `docker` tương đương quyền root trên host, không thêm user chỉ để bỏ `sudo` mà không hiểu rủi ro.

Tại repo source, tạo `Dockerfile` và dùng dependency file đã có:

```dockerfile
FROM python:3.12-slim
WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
RUN groupadd --system --gid 10000 iotsecrets \
 && useradd --system --uid 10001 --gid 10000 --no-create-home app
COPY app.py VERSION ./
USER 10001:10000
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --retries=3 CMD ["python", "-c", "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8080/health')"]
CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8080"]
```

```bash
cd "$HOME/linux-course/iot-dashboard"
sudo docker build -t iot-api:lab .
sudo docker run -d --name iot-api-lab -p 127.0.0.1:18080:8080 iot-api:lab
curl -fsS http://127.0.0.1:18080/health
sudo docker inspect --format '{{.Config.User}}' iot-api-lab
sudo docker inspect --format '{{json .State.Health}}' iot-api-lab
```

**Bằng chứng đạt:** health pass; image khai báo UID/GID non-root `10001:10000`; port chỉ map loopback; code/config trong image nhưng dữ liệu bền vững không đặt ở writable layer.

**Break/fix:** sai dependency/environment/architecture; dùng inspect/logs/health và host resource để phân loại.

**Rollback/reset:**

```bash
sudo docker rm -f iot-api-lab
sudo docker image rm iot-api:lab
```

**Checkpoint:** thay config non-secret, build tag mới, health/identity pass và giải thích phần nào vẫn phụ thuộc host. DB volume được kiểm ở OPS-02, không phải bài này.

**Feynman:** container khác VM? Image khác volume? Healthcheck khác process alive?

<a id="ops-02"></a>

### OPS-02 — Docker Compose cho app, worker, DB và broker

**Cần trước:** OPS-01, DATA-02, IOT-04. **Mục tiêu:** chạy nhiều service có network/volume/secret phù hợp; chỉ edge publish port. **Rủi ro:** 🟠.

**Preflight — VM:** dùng stack disposable không chứa dữ liệu thật; xác nhận Docker/Compose hoạt động, các port `18080` chưa bị chiếm và thư mục source đúng. Compose network `internal: true` chặn đường ra ngoài của container; vì vậy image phải được build/pull và package phải được chuẩn bị **trước** khi stack chạy. Bài vẫn publish API ở host loopback `127.0.0.1:18080` để test—đó là host port có giới hạn, không phải “không publish port nào”.

**Lab — VM:** tạo stack Compose **riêng** với API, worker, PostgreSQL và Mosquitto. Stack này dùng listener MQTT plaintext chỉ trong private Docker network; TLS/public MQTT đã được học ở native path IOT-03 và không được suy diễn từ Compose lab. Tạo worker image từ source IOT-04:

```dockerfile
# Lưu tại $HOME/linux-course/iot-worker/Dockerfile
FROM python:3.12-slim
WORKDIR /worker
RUN groupadd --system --gid 10000 iotsecrets \
 && useradd --system --uid 10002 --gid 10000 --no-create-home worker
RUN pip install --no-cache-dir 'paho-mqtt>=2,<3' 'psycopg[binary]>=3.1,<4'
COPY worker.py .
USER 10002:10000
CMD ["python", "/worker/worker.py"]
```

Worker cần cho phép TLS tùy chọn để cùng source chạy native TLS hoặc Compose private plaintext. Thay phần thiết lập MQTT cuối `worker.py` bằng:

```python
client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2, client_id="iot-worker", protocol=mqtt.MQTTv311, clean_session=False, manual_ack=True)
client.username_pw_set("iot-worker", open(os.environ["MQTT_PASSWORD_FILE"], encoding="utf-8").read().strip())
if mqtt_ca := os.environ.get("MQTT_CA_FILE"):
    client.tls_set(ca_certs=mqtt_ca)
client.reconnect_delay_set(min_delay=1, max_delay=30)
client.on_connect = on_connect
client.on_disconnect = on_disconnect
client.on_message = on_message
client.connect_async(os.environ["MQTT_HOST"], int(os.environ.get("MQTT_PORT", "8883")), keepalive=30)
client.loop_forever(retry_first_connection=True)
```

Tạo `$HOME/linux-course/iot-dashboard/compose-init.sql`. PostgreSQL image bootstrap bằng role `postgres`; script tạo role owner `NOLOGIN`, app role ít quyền, schema và index như DATA-01/02:

```sql
CREATE ROLE iot_owner NOLOGIN;
CREATE ROLE iotapp LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
\getenv IOTAPP_PASSWORD IOTAPP_PASSWORD
ALTER ROLE iotapp PASSWORD :'IOTAPP_PASSWORD';
CREATE DATABASE iot_dashboard OWNER iot_owner;
\connect iot_dashboard
REVOKE ALL ON DATABASE iot_dashboard FROM PUBLIC;
GRANT CONNECT ON DATABASE iot_dashboard TO iotapp;
SET ROLE iot_owner;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
CREATE TABLE readings (
    id bigserial PRIMARY KEY,
    device_id text NOT NULL,
    message_id text NOT NULL,
    temperature double precision NOT NULL CHECK (temperature BETWEEN -40 AND 85),
    humidity double precision NOT NULL CHECK (humidity BETWEEN 0 AND 100),
    device_time timestamptz,
    received_at timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX readings_device_message_uq ON readings (device_id, message_id);
CREATE INDEX readings_device_time_idx ON readings (device_id, device_time DESC);
GRANT USAGE ON SCHEMA public TO iotapp;
GRANT SELECT, INSERT ON readings TO iotapp;
GRANT USAGE, SELECT ON SEQUENCE readings_id_seq TO iotapp;
RESET ROLE;
```

Tạo `$HOME/linux-course/iot-dashboard/mosquitto-compose.conf`:

```text
listener 1883
allow_anonymous false
password_file /mosquitto/config/passwd
acl_file /mosquitto/config/acl
persistence true
persistence_location /mosquitto/data/
```

Tạo ACL:

```text
user sensor-001
topic write lab/devices/sensor-001/telemetry
user sensor-002
topic write lab/devices/sensor-002/telemetry
user iot-worker
topic read lab/devices/+/telemetry
topic read $SYS/broker/version
```

Tạo `compose.yaml`:

```yaml
services:
  postgres:
    image: postgres:16-bookworm
    user: "0:0"
    environment: {}
    secrets: [postgres_password, iotapp_password]
    entrypoint:
      - /bin/bash
      - -ceu
      - |
        # Read secrets while this wrapper is root, then pass only the
        # resulting value to the official entrypoint on its postgres re-exec.
        test -s /run/secrets/postgres_password
        test -s /run/secrets/iotapp_password
        export POSTGRES_PASSWORD="$$(cat /run/secrets/postgres_password)"
        export IOTAPP_PASSWORD="$$(cat /run/secrets/iotapp_password)"
        unset POSTGRES_PASSWORD_FILE IOTAPP_PASSWORD_FILE
        exec docker-entrypoint.sh postgres

    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./compose-init.sql:/docker-entrypoint-initdb.d/10-iot.sql:ro
    networks: [internal]
    healthcheck:
      test: ["CMD-SHELL", "runuser -u postgres -- psql -d iot_dashboard -Atqc \"SELECT to_regclass('public.readings') IS NOT NULL\" | grep -qx t"]
      interval: 5s
      timeout: 3s
      retries: 20

  broker:
    image: eclipse-mosquitto:2
    restart: on-failure
    command: ["mosquitto", "-c", "/mosquitto/config/mosquitto.conf"]
    volumes:
      - ./mosquitto-compose.conf:/mosquitto/config/mosquitto.conf:ro
      - ./compose-mosquitto-passwd:/mosquitto/config/passwd:ro
      - ./compose-mosquitto-acl:/mosquitto/config/acl:ro
      - mqttdata:/mosquitto/data
    secrets: [worker_mqtt_password]
    group_add: ["10000"]
    networks: [internal]
    healthcheck:
      test: ["CMD-SHELL", "mosquitto_sub -h 127.0.0.1 -p 1883 -u iot-worker -P \"$$(cat /run/secrets/worker_mqtt_password)\" -t '$$SYS/broker/version' -C 1 -W 3 >/dev/null"]
      interval: 5s
      timeout: 5s
      retries: 20

  api:
    image: iot-api:lab
    user: "10001:10000"
    environment:
      DATABASE_HOST: postgres
      DATABASE_NAME: iot_dashboard
      DATABASE_USER: iotapp
      DATABASE_PASSWORD_FILE: /run/secrets/iotapp_password
    secrets: [iotapp_password]
    networks: [internal]
    ports:
      - "127.0.0.1:18080:8080"
    depends_on:
      postgres:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "python", "-c", "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8080/health')"]
      interval: 10s
      timeout: 3s
      retries: 10

  worker:
    image: iot-worker:lab
    user: "10002:10000"
    restart: on-failure
    environment:
      MQTT_HOST: broker
      MQTT_PORT: "1883"
      MQTT_PASSWORD_FILE: /run/secrets/worker_mqtt_password
      DATABASE_HOST: postgres
      DATABASE_NAME: iot_dashboard
      DATABASE_USER: iotapp
      DATABASE_PASSWORD_FILE: /run/secrets/iotapp_password
    secrets: [worker_mqtt_password, iotapp_password]
    networks: [internal]
    depends_on:
      postgres:
        condition: service_healthy
      broker:
        condition: service_healthy

  mqtt-test:
    image: iot-worker:lab
    profiles: [test]
    user: "10002:10000"
    entrypoint: ["python", "-c"]
    command:
      - |
        import json, sys
        import paho.mqtt.client as mqtt
        with open("/run/test/payload.json", encoding="utf-8") as fixture:
            payload = json.load(fixture)
        with open("/run/test/topic", encoding="utf-8") as fixture:
            topic = fixture.read().strip()
        client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2, protocol=mqtt.MQTTv311)
        with open("/run/secrets/sensor_001_mqtt_password", encoding="utf-8") as secret:
            client.username_pw_set("sensor-001", secret.read().strip())
        client.connect("broker", 1883, 30)
        client.loop_start()
        info = client.publish(topic, json.dumps(payload), qos=1)
        info.wait_for_publish(timeout=10)
        client.disconnect()
        client.loop_stop()
        sys.exit(0 if info.is_published() else 1)
    volumes:
      - ./mqtt-test-payload.json:/run/test/payload.json:ro
      - ./mqtt-test-topic:/run/test/topic:ro
    networks: [internal]
    secrets: [sensor_001_mqtt_password]
    depends_on:
      broker:
        condition: service_started

networks:
  internal:
    internal: true
volumes:
  pgdata:
  mqttdata:

secrets:
  postgres_password:
    file: ./secrets/postgres_password
  iotapp_password:
    file: ./secrets/iotapp_password
  worker_mqtt_password:
    file: ./secrets/worker_mqtt_password
  sensor_001_mqtt_password:
    file: ./secrets/sensor_001_mqtt_password
```

Tạo secret và password file trong VM; `compose-mosquitto-passwd` phải được sinh bằng `mosquitto_passwd`, không tự viết hash. Dùng cùng password worker trong broker file và Compose secret:

```bash
cd "$HOME/linux-course/iot-dashboard"
printf '%s\n' 'secrets/' 'compose-mosquitto-passwd' 'mqtt-test-payload.json' 'mqtt-test-topic' >> .gitignore
install -d -m 0700 secrets
for name in postgres_password iotapp_password worker_mqtt_password sensor_001_mqtt_password; do
  install -m 0600 /dev/null "secrets/$name"
  read -rsp "$name: " VALUE; printf '\n'
  printf '%s\n' "$VALUE" > "secrets/$name"
  unset VALUE
done
# Compose file-secrets are bind-mounted in this lab. GID 10000 must match
# the non-root API/worker images; only secrets mounted to a service are visible there.
sudo chgrp 10000 secrets/*
chmod 0640 secrets/*
stat -c '%a %u:%g %n' secrets/*
sudo apt install -y mosquitto-clients
# Enter the same values that are in the corresponding secret files. Do not
# use mosquitto_passwd -b here: -b puts the password in process arguments.
mosquitto_passwd -c compose-mosquitto-passwd iot-worker
mosquitto_passwd compose-mosquitto-passwd sensor-001
mosquitto_passwd compose-mosquitto-passwd sensor-002
# The worker health check and the MQTT test below verify that the entered
# values match the mounted secret files without printing either value.
sudo chown 1883:1883 compose-mosquitto-passwd
sudo chmod 0640 compose-mosquitto-passwd
cat > compose-mosquitto-acl <<'ACL'
user sensor-001
topic write lab/devices/sensor-001/telemetry
user sensor-002
topic write lab/devices/sensor-002/telemetry
user iot-worker
topic read lab/devices/+/telemetry
topic read $SYS/broker/version
ACL
chmod 0644 compose-mosquitto-acl
git check-ignore secrets/postgres_password secrets/iotapp_password secrets/worker_mqtt_password secrets/sensor_001_mqtt_password compose-mosquitto-passwd mqtt-test-payload.json mqtt-test-topic
sudo docker build -t iot-api:lab "$HOME/linux-course/iot-dashboard"
sudo docker build -t iot-worker:lab "$HOME/linux-course/iot-worker"
sudo docker compose config --quiet
sudo docker compose up -d --wait --wait-timeout 120
sudo docker compose ps
sudo docker inspect --format '{{.State.Status}} {{.State.Health.Status}}' "$(sudo docker compose ps -q postgres)" | grep -qx 'running healthy'
sudo docker inspect --format '{{.State.Status}} {{.State.Health.Status}}' "$(sudo docker compose ps -q broker)" | grep -qx 'running healthy'
sudo docker compose logs postgres | grep -E 'running /docker-entrypoint-initdb.d/10-iot.sql|database system is ready'
WORKER_CONNECTED=0
for attempt in $(seq 1 30); do
  if sudo docker compose logs worker | grep -q 'mqtt_connected'; then
    WORKER_CONNECTED=1
    break
  fi
  sleep 1
done
test "$WORKER_CONNECTED" = 1
POSTGRES_HOST_PORT=$(sudo docker compose port postgres 5432 2>/dev/null || true)
BROKER_HOST_PORT=$(sudo docker compose port broker 1883 2>/dev/null || true)
test -z "$POSTGRES_HOST_PORT"
test -z "$BROKER_HOST_PORT"
sudo docker compose exec -T api sh -ceu 'test "$(id -u):$(id -g)" = 10001:10000; test -r /run/secrets/iotapp_password; test ! -e /run/secrets/postgres_password'
sudo docker compose exec -T worker sh -ceu 'test "$(id -u):$(id -g)" = 10002:10000; test -r /run/secrets/iotapp_password; test -r /run/secrets/worker_mqtt_password; test ! -e /run/secrets/postgres_password'
```

Custom entrypoint PostgreSQL đọc hai file secret khi còn root, export giá trị chỉ trong runtime process rồi gọi entrypoint chính; các giá trị không nằm trong Compose model hoặc `Config.Env`. Trong init script, `\getenv IOTAPP_PASSWORD IOTAPP_PASSWORD` đọc biến môi trường mà `psql` kế thừa; câu lệnh phải được chạy bằng `psql` với `ON_ERROR_STOP` để init fail ngay nếu biến thiếu hoặc SQL lỗi. API/worker không nhận admin secret và đọc app secrets qua GID `10000`. Giá trị runtime vẫn có thể được root/host quan sát trong process environment; đây không phải secret manager. Init scripts chỉ chạy khi `pgdata` trống, nên sau khi sửa init SQL phải dùng một volume lab mới hoặc chủ ý `down -v` sau backup—restart container không chạy migration lại. Sau `up`, kiểm `docker compose ps` phải là healthy và grep log phải thấy `database system is ready`; nếu init fail, dừng tại đó và xem log, không coi stack đã đạt.

Bằng chứng cả REST và MQTT thật sự đi qua DB, không chỉ health/process-connect. Trước hết publish từ test service nội bộ; password chỉ được Python process đọc từ secret file, không đưa vào argv:

```bash
MQTT_MID=$(cat /proc/sys/kernel/random/uuid)
MQTT_NOW=$(date -u +%FT%TZ)
printf '{"message_id":"%s","device_id":"sensor-001","device_time":"%s","temperature":23,"humidity":56}\n' "$MQTT_MID" "$MQTT_NOW" > mqtt-test-payload.json
printf '%s\n' 'lab/devices/sensor-001/telemetry' > mqtt-test-topic
sudo docker compose --profile test run --rm -T mqtt-test
MQTT_INSERTED=0
for attempt in $(seq 1 30); do
  MQTT_INSERTED=$(sudo docker compose exec -T postgres runuser -u postgres -- psql -U postgres -At -d iot_dashboard -c "SELECT count(*) FROM readings WHERE message_id='$MQTT_MID';")
  test "$MQTT_INSERTED" = 1 && break
  sleep 1
done
test "$MQTT_INSERTED" = 1

DENIED_MID=$(cat /proc/sys/kernel/random/uuid)
printf '{"message_id":"%s","device_id":"sensor-001","device_time":"%s","temperature":23,"humidity":56}\n' "$DENIED_MID" "$MQTT_NOW" > mqtt-test-payload.json
printf '%s\n' 'lab/devices/sensor-002/telemetry' > mqtt-test-topic
if sudo docker compose --profile test run --rm -T mqtt-test; then
  DENIED_PUBLISH_STATUS=0
else
  DENIED_PUBLISH_STATUS=$?
fi
printf 'denied_publish_client_status=%s (not decisive under MQTT 3.1.1)\n' "$DENIED_PUBLISH_STATUS"
sleep 3
DENIED_COUNT=$(sudo docker compose exec -T postgres runuser -u postgres -- psql -U postgres -At -d iot_dashboard -c "SELECT count(*) FROM readings WHERE message_id='$DENIED_MID';")
test "$DENIED_COUNT" = 0

MISMATCH_MID=$(cat /proc/sys/kernel/random/uuid)
printf '{"message_id":"%s","device_id":"sensor-002","device_time":"%s","temperature":23,"humidity":56}\n' "$MISMATCH_MID" "$MQTT_NOW" > mqtt-test-payload.json
printf '%s\n' 'lab/devices/sensor-001/telemetry' > mqtt-test-topic
sudo docker compose --profile test run --rm -T mqtt-test
sleep 3
MISMATCH_COUNT=$(sudo docker compose exec -T postgres runuser -u postgres -- psql -U postgres -At -d iot_dashboard -c "SELECT count(*) FROM readings WHERE message_id='$MISMATCH_MID';")
test "$MISMATCH_COUNT" = 0
sudo docker compose logs --since 30s worker | grep -q 'ingest_rejected reason=topic_identity_mismatch'
```

Positive path có deadline 30 giây; hết deadline hoặc count khác `1` là fail. Negative ACL path gửi payload self-consistent của `sensor-001` nhưng publish vào topic `sensor-002`; client có thể không nhận ACK lỗi theo MQTT 3.1.1, nên bằng chứng quyết định là DB giữ count `0`. Negative worker path publish vào topic A được ACL cho phép nhưng payload khai `sensor-002`; broker nhận, worker ghi `ingest_rejected` và DB vẫn `0`. Hai test tách riêng broker authorization khỏi payload identity validation. Sau đó kiểm REST:

```bash
curl -fsS http://127.0.0.1:18080/health
NOW=$(date -u +%FT%TZ)
MID=$(cat /proc/sys/kernel/random/uuid)
BODY=$(printf '{"message_id":"%s","device_id":"sensor-001","device_time":"%s","temperature":22.5,"humidity":55}' "$MID" "$NOW")
curl -fsS -H 'Content-Type: application/json' -H 'X-Device-Key: demo-key-a' -d "$BODY" http://127.0.0.1:18080/api/ingest
REST_INSERTED=$(sudo docker compose exec -T postgres runuser -u postgres -- psql -U postgres -At -d iot_dashboard -c "SELECT count(*) FROM readings WHERE message_id='$MID';")
test "$REST_INSERTED" = 1
sudo docker compose stop api
sudo docker compose rm -f api
sudo docker compose up -d api
API_RECREATE_COUNT=$(sudo docker compose exec -T postgres runuser -u postgres -- psql -U postgres -At -d iot_dashboard -c "SELECT count(*) FROM readings WHERE message_id='$MID';")
test "$API_RECREATE_COUNT" = 1

sudo docker compose stop postgres
sudo docker compose rm -f postgres
sudo docker compose up -d --wait --wait-timeout 120 postgres
PERSISTED_AFTER_DB_RECREATE=$(sudo docker compose exec -T postgres runuser -u postgres -- psql -U postgres -At -d iot_dashboard -c "SELECT count(*) FROM readings WHERE message_id='$MID';")
test "$PERSISTED_AFTER_DB_RECREATE" = 1

sudo docker compose stop worker
sudo docker compose rm -f worker
sudo docker compose up -d worker
WORKER_RECONNECTED=0
for attempt in $(seq 1 30); do
  if sudo docker compose logs --since 1m worker | grep -q 'mqtt_connected'; then
    WORKER_RECONNECTED=1
    break
  fi
  sleep 1
done
test "$WORKER_RECONNECTED" = 1
RECREATE_MID=$(cat /proc/sys/kernel/random/uuid)
printf '{"message_id":"%s","device_id":"sensor-001","device_time":"%s","temperature":24,"humidity":57}\n' "$RECREATE_MID" "$(date -u +%FT%TZ)" > mqtt-test-payload.json
printf '%s\n' 'lab/devices/sensor-001/telemetry' > mqtt-test-topic
sudo docker compose --profile test run --rm -T mqtt-test
RECREATE_COUNT=0
for attempt in $(seq 1 30); do
  RECREATE_COUNT=$(sudo docker compose exec -T postgres runuser -u postgres -- psql -U postgres -At -d iot_dashboard -c "SELECT count(*) FROM readings WHERE message_id='$RECREATE_MID';")
  test "$RECREATE_COUNT" = 1 && break
  sleep 1
done
test "$RECREATE_COUNT" = 1
```

**Bằng chứng đạt:** `config --quiet` pass và không in secret; API insert qua network nội bộ; worker kết nối broker/DB bằng user riêng; DB/broker không publish host port; recreate cả API lẫn PostgreSQL container vẫn giữ row trong named volume; ACL sai topic và worker payload identity mismatch đều không tạo row; volume MQTT bền nhưng không được gọi là backup.

**Break/fix:** sai DB password/schema, worker password/topic hoặc service dependency; đọc `docker compose logs SERVICE`, health và query từng tầng, không restart toàn stack mù.

**Rollback/reset:** `docker compose down` không dùng `-v` trước backup/restore test. Chỉ `down -v` khi target là stack lab, dump đã kiểm và người học chủ ý xóa toàn dữ liệu.

**Checkpoint:** publish một message qua broker và một message qua REST, query đúng từng row; recreate API/worker/PostgreSQL container vẫn giữ DB; DB/broker không có host port; credential/topic sai không tạo row.

**Feynman:** Compose network khác public port? `down` khác `down -v`? Health dependency giải quyết race nào?

<a id="ops-03"></a>

### OPS-03 — Backup, restore và RPO/RTO cơ bản

**Cần trước:** DATA-02, WEB-06, OPS-01. **Mục tiêu:** backup DB/config, restore sang môi trường sạch, ghi thời gian mất dữ liệu và thời gian phục hồi; phân biệt snapshot/replica/backup. **Rủi ro:** 🔴; chỉ data lab. Phần lệnh native bên dưới dùng PostgreSQL host từ DATA-01; nếu đang theo Compose, dùng `docker compose exec -T postgres pg_dump ...` và restore target riêng, không trộn hai database nguồn.

**Tự hỏi:** Archive có tồn tại nhưng chưa restore thử thì đã gọi “backup tốt” chưa?

**Chốt lại:** chưa. Restore drill là bằng chứng backup có thể dùng.

**Lab — VM/staging:** backup có thể chứa config/secret nên tạo directory/file mode chặt ngay từ đầu. Để metadata và dump mô tả cùng trạng thái logic trong lab, dừng **mọi writer đã biết**: API REST, worker và simulator. Danh sách unit đã dừng được lưu trên đĩa để vẫn khôi phục được nếu terminal mất; với Compose hoặc writer khác, phải thay inventory tương ứng. Chỉ chạy block dump sau khi block freeze exit `0`:

```bash
install -d -m 0700 "$HOME/iot-backups"
BACKUP_WRITER_STATE="$HOME/iot-backups/WRITERS_TO_RESTART"
(
  set -Eeuo pipefail
  if test -s "$BACKUP_WRITER_STATE"; then
    printf 'Unfinished freeze state exists: %s\n' "$BACKUP_WRITER_STATE" >&2
    exit 1
  fi
  install -m 0600 /dev/null "$BACKUP_WRITER_STATE"
  for unit in iot-api iot-worker iot-simulator@sensor-001 iot-simulator@sensor-002; do
    if systemctl is-active --quiet "$unit"; then
      sudo systemctl stop "$unit"
      printf '%s\n' "$unit" >> "$BACKUP_WRITER_STATE"
    fi
  done
)
```

Nếu block freeze lỗi, chưa được dump. Khởi động lại từng unit đã ghi trong `WRITERS_TO_RESTART`, sửa nguyên nhân rồi tạo state file mới. Sau khi freeze thành công, dump và đếm khi writer vẫn dừng; count trước/sau phải bằng nhau. Redirect ngoài `sudo` cho phép file dump thuộc user nhưng dữ liệu do process `postgres` đọc:

```bash
(
  set -Eeuo pipefail
  umask 077
  SOURCE_COUNT_BEFORE_DUMP=$(sudo -u postgres psql -At -d iot_dashboard -c 'SELECT count(*) FROM readings;')
  sudo -u postgres pg_dump --format=custom iot_dashboard > "$HOME/iot-backups/iot_dashboard.dump"
  SOURCE_COUNT_AT_BACKUP=$(sudo -u postgres psql -At -d iot_dashboard -c 'SELECT count(*) FROM readings;')
  test "$SOURCE_COUNT_BEFORE_DUMP" = "$SOURCE_COUNT_AT_BACKUP"
  BACKUP_FINISHED=$(date -u +%FT%TZ)
  printf '%s\n%s\n' "$BACKUP_FINISHED" "$SOURCE_COUNT_AT_BACKUP" > "$HOME/iot-backups/BACKUP_METADATA"
  cp "$HOME/iot-backups/WRITERS_TO_RESTART" "$HOME/iot-backups/WRITERS_AT_BACKUP"
  CONFIG_MANIFEST=$(mktemp)
  for path in \
    /etc/iot-dashboard \
    /etc/systemd/system/iot-api.service \
    /etc/systemd/system/iot-api.service.d \
    /etc/systemd/system/iot-worker.service \
    /etc/systemd/system/iot-simulator@.service \
    /etc/nginx/sites-available/iot-dashboard \
    /etc/nginx/sites-enabled/iot-dashboard \
    /etc/nginx/conf.d/iot-rate.conf \
    /etc/mosquitto/mosquitto.conf \
    /etc/mosquitto/conf.d \
    /etc/mosquitto/passwd \
    /etc/mosquitto/acl \
    /etc/mosquitto/certs; do
    if sudo test -e "$path"; then
      printf '%s\n' "${path#/}" >> "$CONFIG_MANIFEST"
    fi
  done
  test -s "$CONFIG_MANIFEST"
  sudo tar -C / -czf - --files-from=- < "$CONFIG_MANIFEST" > "$HOME/iot-backups/iot-config.tgz"
  cp "$CONFIG_MANIFEST" "$HOME/iot-backups/CONFIG_MANIFEST"
  rm -- "$CONFIG_MANIFEST"
  cd "$HOME/iot-backups"
  sha256sum iot_dashboard.dump iot-config.tgz CONFIG_MANIFEST BACKUP_METADATA WRITERS_AT_BACKUP > SHA256SUMS
  sha256sum -c SHA256SUMS
  tar -tzf iot-config.tgz
  tar -tzf iot-config.tgz > ARCHIVE_CONTENTS
  while IFS= read -r path; do
    grep -Fqx "$path" ARCHIVE_CONTENTS || grep -Fq "${path%/}/" ARCHIVE_CONTENTS
  done < CONFIG_MANIFEST
  rm -- ARCHIVE_CONTENTS
)
```

Strict mode và `umask 077` chỉ sống trong subshell; artifact dở dang không được tiếp tục coi là backup, còn shell của người học giữ umask ban đầu.

Archive này cố ý không lấy runtime state `/var/lib/mosquitto` vì Core lấy PostgreSQL làm nguồn dữ liệu bền; nếu broker retained/session queue là dữ liệu cần khôi phục, phải dừng broker nhất quán và backup state đó theo runbook riêng. Archive vẫn chứa secret/private key của lab nếu chúng nằm trong các path trên; phải mã hóa và lưu ngoài host nguồn theo policy thật, không coi mode `0600` trên cùng máy là backup độc lập. Source release/commit và dependency lock phải tồn tại trong remote/artifact store riêng; config archive không thay Git/artifact backup.

Khởi động lại đúng những writer đã active trước freeze và xác minh từng unit. Không xóa state file: nó là bằng chứng backup đã dừng thành phần nào và được tái dùng cho failure cleanup nếu terminal mất.

```bash
BACKUP_RESTART_OK=1
while IFS= read -r unit; do
  if ! sudo systemctl start "$unit" || ! sudo systemctl is-active --quiet "$unit"; then
    printf 'Failed to restore writer: %s\n' "$unit" >&2
    BACKUP_RESTART_OK=0
  fi
done < "$HOME/iot-backups/WRITERS_TO_RESTART"
if test "$BACKUP_RESTART_OK" = 1; then
  rm -- "$HOME/iot-backups/WRITERS_TO_RESTART"
fi
test "$BACKUP_RESTART_OK" = 1
```

Tạo DB restore lab khác, không đè DB đang chạy. OS user `postgres` thường không traverse được `$HOME`; copy dump vào `/tmp` với owner/mode phù hợp trước restore. Đây vẫn chỉ là restore cùng cluster; checkpoint “môi trường sạch” phải dùng VM/container PostgreSQL mới cùng major version và migration/role provisioning đã version-control.

```bash
sudo install -o postgres -g postgres -m 0600 "$HOME/iot-backups/iot_dashboard.dump" /tmp/iot_dashboard.dump
sudo -u postgres createdb iot_restore_lab
sudo -u postgres pg_restore --dbname=iot_restore_lab /tmp/iot_dashboard.dump
sudo -u postgres psql -d iot_restore_lab -c 'SELECT count(*) FROM readings;'
install -d -m 0700 "$HOME/iot-restore-config"
tar -xzf "$HOME/iot-backups/iot-config.tgz" -C "$HOME/iot-restore-config"
(
  cd "$HOME/iot-backups"
  sha256sum -c SHA256SUMS
)
sudo rm -- /tmp/iot_dashboard.dump
```

Ghi `backup_at`, `restore_started`, `restore_finished`, số record, RPO/RTO thực tế.

### Restore drill sang PostgreSQL sạch

Cùng major version với nguồn, dựng một PostgreSQL container độc lập trên loopback port `55432`, không dùng volume/database hiện tại. Trước phép đo, inventory rồi dừng **đúng các writer đang hoạt động**, gồm cả API REST có thể ghi. Không dùng `disable` vì mục tiêu chỉ là đóng băng ghi trong drill. Lưu state trên đĩa thay vì biến shell để cleanup vẫn hoạt động sau reconnect:

```bash
DRILL_WRITER_STATE="$HOME/iot-backups/DRILL_WRITERS_TO_RESTART"
(
  set -Eeuo pipefail
  if test -s "$DRILL_WRITER_STATE"; then
    printf 'Unfinished drill freeze state exists: %s\n' "$DRILL_WRITER_STATE" >&2
    exit 1
  fi
  install -m 0600 /dev/null "$DRILL_WRITER_STATE"
  for unit in iot-api iot-worker iot-simulator@sensor-001 iot-simulator@sensor-002; do
    if systemctl is-active --quiet "$unit"; then
      sudo systemctl stop "$unit"
      printf '%s\n' "$unit" >> "$DRILL_WRITER_STATE"
    fi
  done
)
```

Chỉ thêm unit vào state file nếu nó thực sự thuộc lab và stop thành công; với Compose hoặc writer khác, inventory/dừng đúng service tương ứng. Nếu freeze lỗi, khởi động lại các unit đã ghi rồi sửa trước khi tiếp tục. Sau đó bắt đầu đồng hồ RTO **trước khi provision target**. Dùng password file bind-read-only để secret không nằm trong `docker inspect Config.Env`:

```bash
RESTORE_PREPARED=0
RESTORE_SECRET_STATE="$HOME/iot-backups/RESTORE_SECRET_DIR_PATH"
RESTORE_STARTED_STATE="$HOME/iot-backups/RESTORE_STARTED_EPOCH"
if sudo docker inspect iot-restore-postgres >/dev/null 2>&1 || test -e "$RESTORE_SECRET_STATE" || test -e "$RESTORE_STARTED_STATE"; then
  RESTORE_STATE_OK=1
  if ! sudo docker inspect iot-restore-postgres >/dev/null 2>&1 || ! test -r "$RESTORE_SECRET_STATE" || ! test -r "$RESTORE_STARTED_STATE" || ! test -r "$HOME/iot-backups/RESTORE_STARTED" || ! test -r "$HOME/iot-backups/RESTORE_INCIDENT_AT" || ! test -r "$HOME/iot-backups/SOURCE_COUNT_AT_INCIDENT"; then
    printf 'Incomplete restore state; inspect container and state files before continuing\n' >&2
    RESTORE_STATE_OK=0
  fi
  if test "$RESTORE_STATE_OK" = 1; then
    RESTORE_SECRET_DIR=$(cat "$RESTORE_SECRET_STATE")
    MOUNT_SOURCE=$(sudo docker inspect --format '{{range .Mounts}}{{if eq .Destination "/run/secrets/postgres_password"}}{{.Source}}{{end}}{{end}}' iot-restore-postgres)
    if ! test -d "$RESTORE_SECRET_DIR" || test "$MOUNT_SOURCE" != "$RESTORE_SECRET_DIR/postgres_password"; then
      printf 'Restore state does not match the container mount; stop and inspect\n' >&2
      RESTORE_STATE_OK=0
    fi
  fi
  if test "$RESTORE_STATE_OK" = 1; then
    RESTORE_STARTED_EPOCH=$(cat "$HOME/iot-backups/RESTORE_STARTED_EPOCH")
    RESTORE_STARTED=$(cat "$HOME/iot-backups/RESTORE_STARTED")
    INCIDENT_AT=$(cat "$HOME/iot-backups/RESTORE_INCIDENT_AT")
    SOURCE_COUNT_AT_INCIDENT=$(cat "$HOME/iot-backups/SOURCE_COUNT_AT_INCIDENT")
    case "$RESTORE_STARTED_EPOCH" in (*[!0-9]*|'') printf 'Invalid persisted restore start time\n' >&2; RESTORE_STATE_OK=0 ;; esac
    case "$SOURCE_COUNT_AT_INCIDENT" in (*[!0-9]*|'') printf 'Invalid persisted incident count\n' >&2; RESTORE_STATE_OK=0 ;; esac
    if test -z "$RESTORE_STARTED" || test -z "$INCIDENT_AT"; then
      printf 'Persisted UTC timestamps are missing\n' >&2
      RESTORE_STATE_OK=0
    fi
  fi
  if test "$RESTORE_STATE_OK" = 1; then
    CONTAINER_RUNNING=$(sudo docker inspect --format '{{.State.Running}}' iot-restore-postgres)
    if test "$CONTAINER_RUNNING" = true || sudo docker start iot-restore-postgres >/dev/null; then
      RESTORE_PREPARED=1
    else
      printf 'Restore container could not be started; inspect logs before continuing\n' >&2
      RESTORE_STATE_OK=0
    fi
  fi
else
  (
    cd "$HOME/iot-backups"
    sha256sum -c SHA256SUMS
  )
  mapfile -t BACKUP_META < "$HOME/iot-backups/BACKUP_METADATA"
  test "${#BACKUP_META[@]}" = 2
  BACKUP_FINISHED=${BACKUP_META[0]}
  SOURCE_COUNT_AT_BACKUP=${BACKUP_META[1]}
  test -n "$BACKUP_FINISHED"
  case "$SOURCE_COUNT_AT_BACKUP" in (*[!0-9]*|'') printf 'Invalid backup row count\n' >&2; false ;; esac
  RESTORE_STARTED_EPOCH=$(date +%s)
  RESTORE_STARTED=$(date -u +%FT%TZ)
  INCIDENT_AT=$(date -u +%FT%TZ)
  SOURCE_COUNT_AT_INCIDENT=$(sudo -u postgres psql -At -d iot_dashboard -c 'SELECT count(*) FROM readings;')
  RESTORE_SECRET_DIR=$(mktemp -d)
  chmod 0700 "$RESTORE_SECRET_DIR"
  printf '%s\n' "$RESTORE_SECRET_DIR" | install -m 0600 /dev/stdin "$RESTORE_SECRET_STATE"
  printf '%s\n' "$RESTORE_STARTED_EPOCH" | install -m 0600 /dev/stdin "$HOME/iot-backups/RESTORE_STARTED_EPOCH"
  printf '%s\n' "$RESTORE_STARTED" | install -m 0600 /dev/stdin "$HOME/iot-backups/RESTORE_STARTED"
  printf '%s\n' "$INCIDENT_AT" | install -m 0600 /dev/stdin "$HOME/iot-backups/RESTORE_INCIDENT_AT"
  printf '%s\n' "$SOURCE_COUNT_AT_INCIDENT" | install -m 0600 /dev/stdin "$HOME/iot-backups/SOURCE_COUNT_AT_INCIDENT"
  read -rsp 'Temporary restore postgres password: ' RESTORE_ADMIN_PASSWORD; printf '\n'
  printf '%s\n' "$RESTORE_ADMIN_PASSWORD" | install -m 0600 /dev/stdin "$RESTORE_SECRET_DIR/postgres_password"
  unset RESTORE_ADMIN_PASSWORD
  sudo docker run -d --name iot-restore-postgres \
    --mount "type=bind,src=$RESTORE_SECRET_DIR/postgres_password,dst=/run/secrets/postgres_password,readonly" \
    -e POSTGRES_PASSWORD_FILE=/run/secrets/postgres_password \
    -p 127.0.0.1:55432:5432 \
    postgres:16-bookworm
  RESTORE_DB_READY=0
  for attempt in $(seq 1 60); do
    if sudo docker exec iot-restore-postgres pg_isready -U postgres; then
      RESTORE_DB_READY=1
      break
    fi
    sleep 1
  done
  test "$RESTORE_DB_READY" = 1
  RESTORE_PREPARED=1
fi
test "$RESTORE_PREPARED" = 1
mapfile -t BACKUP_META < "$HOME/iot-backups/BACKUP_METADATA"
BACKUP_FINISHED=${BACKUP_META[0]}
SOURCE_COUNT_AT_BACKUP=${BACKUP_META[1]}
test -n "$BACKUP_FINISHED"
case "$SOURCE_COUNT_AT_BACKUP" in (*[!0-9]*|'') printf 'Invalid backup row count\n' >&2; false ;; esac
test -n "$RESTORE_STARTED_EPOCH" -a -n "$RESTORE_STARTED" -a -n "$INCIDENT_AT"
case "$RESTORE_STARTED_EPOCH" in (*[!0-9]*|'') printf 'Missing restore start state; do not report RTO\n' >&2; false ;; esac
case "$SOURCE_COUNT_AT_INCIDENT" in (*[!0-9]*|'') printf 'Missing incident count; do not report RPO\n' >&2; false ;; esac
if ! sudo docker exec iot-restore-postgres pg_isready -U postgres >/dev/null; then
  printf 'Restore target is not ready; inspect logs before continuing\n' >&2
  false
fi

# A resume keeps the original start time and target; it never resets the RTO clock.
# If an existing state was incomplete or mismatched, the guard above fails before restore.
```

Nếu writer không được đóng băng, `LOST_ROWS` chỉ là chênh lệch so với snapshot count tại một thời điểm, không phải tổng mất dữ liệu cuối cùng.

Provision role/database trước restore vì `pg_dump` không chứa global roles. Dùng một password app lab mới, không tái dùng production secret. Truyền password qua file tạm mode `0600` thay vì `docker exec -e` hoặc `psql -v` trên argv; root host/container vẫn có thể đọc trong thời gian drill, nên xóa cả hai bản ngay sau provisioning:

```bash
RESTORE_PROVISIONED_STATE="$HOME/iot-backups/RESTORE_PROVISIONED"
if test -s "$RESTORE_PROVISIONED_STATE"; then
  printf 'Restore roles/database already provisioned; resuming without re-entering the app password\n'
elif test -e "$RESTORE_PROVISIONED_STATE"; then
  printf 'Restore provisioning marker is empty; inspect state before continuing\n' >&2
  false
else
  read -rsp 'Temporary restored iotapp password: ' RESTORED_IOTAPP_PASSWORD; printf '\n'
  printf '%s\n' "$RESTORED_IOTAPP_PASSWORD" | install -m 0600 /dev/stdin "$RESTORE_SECRET_DIR/iotapp_password"
  unset RESTORED_IOTAPP_PASSWORD
  sudo docker cp "$RESTORE_SECRET_DIR/iotapp_password" iot-restore-postgres:/tmp/iotapp_password
  sudo docker exec -u 0 iot-restore-postgres chown postgres:postgres /tmp/iotapp_password
  sudo docker exec -u 0 iot-restore-postgres chmod 0600 /tmp/iotapp_password
  sudo docker exec -i -u postgres iot-restore-postgres /bin/bash -ceu '
    export RESTORED_IOTAPP_PASSWORD="$(cat /tmp/iotapp_password)"
    psql -v ON_ERROR_STOP=1 -U postgres
    unset RESTORED_IOTAPP_PASSWORD
  ' <<'SQL'
CREATE ROLE iot_owner NOLOGIN;
CREATE ROLE iotapp LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
\getenv RESTORED_IOTAPP_PASSWORD RESTORED_IOTAPP_PASSWORD
ALTER ROLE iotapp PASSWORD :'RESTORED_IOTAPP_PASSWORD';
CREATE DATABASE iot_dashboard OWNER iot_owner;
SQL
  sudo docker exec -u 0 iot-restore-postgres rm -- /tmp/iotapp_password
  rm -- "$RESTORE_SECRET_DIR/iotapp_password"
  printf 'provisioned\n' | install -m 0600 /dev/stdin "$RESTORE_PROVISIONED_STATE"
fi
# Keep RESTORE_SECRET_DIR until final cleanup; the postgres admin secret remains mounted there.
```

Copy dump vào target mới và restore với owner từ dump đã được provision. Sau restore, kiểm role, count và sample; checksum dump vẫn phải pass. Toàn bộ verify/đo chạy trong subshell strict để count/checksum sai không tiếp tục in một báo cáo RTO giả:

```bash
(
set -Eeuo pipefail
sudo docker cp "$HOME/iot-backups/iot_dashboard.dump" iot-restore-postgres:/tmp/iot_dashboard.dump
sudo docker exec iot-restore-postgres pg_restore --exit-on-error -U postgres -d iot_dashboard /tmp/iot_dashboard.dump
RESTORED_COUNT=$(sudo docker exec iot-restore-postgres psql -U postgres -At -d iot_dashboard -c 'SELECT count(*) FROM readings;')
LATEST_TELEMETRY_AT=$(sudo docker exec iot-restore-postgres psql -U postgres -At -d iot_dashboard -c "SELECT COALESCE(to_char(max(received_at) AT TIME ZONE 'UTC', 'YYYY-MM-DD\"T\"HH24:MI:SS\"Z\"'), '') FROM readings;")
sudo docker exec iot-restore-postgres psql -U postgres -d iot_dashboard -c 'SELECT device_id, received_at FROM readings ORDER BY id DESC LIMIT 3;'
sudo docker exec iot-restore-postgres psql -U postgres -Atqc "SELECT rolname,rolsuper,rolcanlogin FROM pg_roles WHERE rolname IN ('iot_owner','iotapp') ORDER BY rolname;"
if test "$RESTORED_COUNT" != "$SOURCE_COUNT_AT_BACKUP"; then
  printf 'FAIL: restored_count=%s differs from source_count_at_backup=%s\n' "$RESTORED_COUNT" "$SOURCE_COUNT_AT_BACKUP" >&2
  false
fi
if test "$RESTORED_COUNT" -gt "$SOURCE_COUNT_AT_INCIDENT"; then
  printf 'FAIL: restored count exceeds incident snapshot\n' >&2
  false
fi
(
  cd "$HOME/iot-backups"
  sha256sum -c SHA256SUMS
)
RESTORE_FINISHED_EPOCH=$(date +%s)
RESTORE_FINISHED=$(date -u +%FT%TZ)
RTO_SECONDS=$((RESTORE_FINISHED_EPOCH - RESTORE_STARTED_EPOCH))
ROWS_AFTER_BACKUP=$((SOURCE_COUNT_AT_INCIDENT - SOURCE_COUNT_AT_BACKUP))
BACKUP_AGE_AT_INCIDENT_SECONDS=$(( $(date -d "$INCIDENT_AT" +%s) - $(date -d "$BACKUP_FINISHED" +%s) ))
test "$ROWS_AFTER_BACKUP" -ge 0
test "$BACKUP_AGE_AT_INCIDENT_SECONDS" -ge 0
printf 'backup_finished=%s\nincident_at=%s\nrestore_started=%s\nrestore_finished=%s\nrto_seconds=%s\nsource_count_at_backup=%s\nsource_count_at_incident=%s\nrestored_count=%s\nrows_after_backup=%s\nbackup_age_at_incident_seconds=%s\nlatest_telemetry_at=%s\n' \
  "$BACKUP_FINISHED" "$INCIDENT_AT" "$RESTORE_STARTED" "$RESTORE_FINISHED" "$RTO_SECONDS" "$SOURCE_COUNT_AT_BACKUP" "$SOURCE_COUNT_AT_INCIDENT" "$RESTORED_COUNT" "$ROWS_AFTER_BACKUP" "$BACKUP_AGE_AT_INCIDENT_SECONDS" "$LATEST_TELEMETRY_AT"
if test -n "$LATEST_TELEMETRY_AT"; then
  DATA_FRESHNESS_GAP_SECONDS=$(( $(date -d "$INCIDENT_AT" +%s) - $(date -d "$LATEST_TELEMETRY_AT" +%s) ))
  test "$DATA_FRESHNESS_GAP_SECONDS" -ge 0
  printf 'data_freshness_gap_seconds=%s\n' "$DATA_FRESHNESS_GAP_SECONDS"
else
  printf 'data_freshness_gap_seconds=unknown (restored table empty)\n'
fi
)
```

Negative safety check: target chỉ bind loopback host port `55432`; từ máy khác không được reachable. Đây là restore target mới, không phải host PostgreSQL đang chạy.

**Bằng chứng đạt:** checksum pass; same-cluster rehearsal và clean-container restore đều query được count/sample; `restored_count == source_count_at_backup`; roles được provision riêng và không biến `iotapp` thành superuser; config manifest/archive được đối chiếu; nguồn không bị overwrite; RTO gồm provision + restore + verify. `rows_after_backup` và `backup_age_at_incident_seconds` là bằng chứng khoảng mất dữ liệu tiềm năng theo backup schedule; `data_freshness_gap_seconds` chỉ mô tả telemetry mới nhất, không được đổi tên thành RPO. Nếu không cô lập writer hoặc không có row, phải ghi giới hạn và không gọi phép đo đó là RPO chính xác.

**Break/fix:** sửa/cắt archive copy hoặc checksum mismatch; dừng restore, lấy bản backup khác; không tạo checksum mới từ file hỏng.

**Rollback/reset:**

```bash
sudo -u postgres dropdb --if-exists iot_restore_lab
RESTORE_SECRET_STATE="$HOME/iot-backups/RESTORE_SECRET_DIR_PATH"
if test -r "$RESTORE_SECRET_STATE"; then
  RESTORE_SECRET_DIR=$(cat "$RESTORE_SECRET_STATE")
fi
SECRET_CLEANUP_OK=1
if sudo docker inspect iot-restore-postgres >/dev/null 2>&1; then
  MOUNT_SOURCE=$(sudo docker inspect --format '{{range .Mounts}}{{if eq .Destination "/run/secrets/postgres_password"}}{{.Source}}{{end}}{{end}}' iot-restore-postgres)
  if test -z "${RESTORE_SECRET_DIR:-}"; then
    printf 'Refusing container removal: saved secret-directory state is missing; mount source=%s\n' "$MOUNT_SOURCE" >&2
    SECRET_CLEANUP_OK=0
  elif test "$MOUNT_SOURCE" != "$RESTORE_SECRET_DIR/postgres_password"; then
    printf 'Refusing container removal: saved path and container mount disagree\n' >&2
    SECRET_CLEANUP_OK=0
  else
    sudo docker rm -f iot-restore-postgres
  fi
fi
if sudo docker inspect iot-restore-postgres >/dev/null 2>&1; then
  printf 'Refusing secret cleanup: restore container still exists\n' >&2
  SECRET_CLEANUP_OK=0
fi
if test "$SECRET_CLEANUP_OK" = 1 && test -n "${RESTORE_SECRET_DIR:-}" && test -d "$RESTORE_SECRET_DIR"; then
  case "$RESTORE_SECRET_DIR" in
    /tmp/tmp.*) rm -rf -- "$RESTORE_SECRET_DIR" ;;
    *) printf 'Refusing unexpected secret directory: %s\n' "$RESTORE_SECRET_DIR" >&2; SECRET_CLEANUP_OK=0 ;;
  esac
fi
if test "$SECRET_CLEANUP_OK" = 1; then
  rm -f -- "$RESTORE_SECRET_STATE" "$HOME/iot-backups/RESTORE_STARTED_EPOCH" "$HOME/iot-backups/RESTORE_STARTED" "$HOME/iot-backups/RESTORE_INCIDENT_AT" "$HOME/iot-backups/SOURCE_COUNT_AT_INCIDENT" "$HOME/iot-backups/RESTORE_PROVISIONED"
fi
DRILL_RESTART_OK=1
while IFS= read -r unit; do
  if ! sudo systemctl start "$unit" || ! sudo systemctl is-active --quiet "$unit"; then
    printf 'Failed to restore drill writer: %s\n' "$unit" >&2
    DRILL_RESTART_OK=0
  fi
done < "$HOME/iot-backups/DRILL_WRITERS_TO_RESTART"
if test "$DRILL_RESTART_OK" = 1; then
  rm -- "$HOME/iot-backups/DRILL_WRITERS_TO_RESTART"
fi
sudo -u postgres psql -At -d iot_dashboard -c 'SELECT max(received_at) FROM readings;'
test "$SECRET_CLEANUP_OK" = 1
test "$DRILL_RESTART_OK" = 1
```

Nếu state file mất nhưng container còn, lấy `MOUNT_SOURCE` bằng `docker inspect`, xác nhận thủ công source kết thúc bằng `/postgres_password`, parent là thư mục `mktemp` riêng của drill và không chứa file ngoài dự kiến; chỉ sau đó mới xóa container rồi parent. Không đoán hoặc dùng glob rộng.

Giữ ít nhất một bản backup đã kiểm cho bài incident/DR; chỉ xóa `$HOME/iot-backups` sau khi xác nhận có bản độc lập khác hoặc chủ ý kết thúc toàn bộ lab.

**Checkpoint:** từ dump/config đã tạo, dựng PostgreSQL container/VM sạch cùng major version, provision role/database riêng, restore, verify count/sample/checksum, chứng minh target không public và ghi RPO/RTO. Same-cluster restore chỉ là rehearsal; clean target mới là điều kiện pass.

**Feynman:** snapshot khác logical backup? Replica có bảo vệ xóa nhầm không? RPO/RTO quyết định gì?

<a id="ops-04"></a>

### OPS-04 — Logs, metrics, freshness và alert có hành động

**Cần trước:** SYS-04, IOT-05. **Mục tiêu:** theo dõi latency/traffic/errors/saturation cùng sensor freshness, ingestion error/lag, DB/disk và MQTT disconnect. **Rủi ro:** 🟡.

**Tự hỏi:** Alert “CPU cao” có đủ để người trực biết làm gì không?

**Chốt lại:** không; alert cần ngưỡng, tác động, owner, đường kiểm tra và runbook.

**Lab:** bắt đầu bằng journal/host/DB evidence:

```bash
systemctl show iot-api -p ActiveState -p MainPID
journalctl -u iot-api --since '10 minutes ago' --no-pager
df -h
free -h
sudo -u postgres psql -d iot_dashboard -c "SELECT max(received_at), count(*) FROM readings WHERE received_at > now() - interval '10 minutes';"
```

Core triển khai một alert freshness thật bằng oneshot + timer. Script trả nonzero và log `ALERT data_stale` khi không có dữ liệu trong 2 phút:

```bash
sudo install -o root -g iotapp -m 0750 /dev/null /usr/local/sbin/iot-freshness-check
sudo tee /usr/local/sbin/iot-freshness-check >/dev/null <<'BASH'
#!/usr/bin/env bash
set -Eeuo pipefail
if ! state=$(psql -h 127.0.0.1 -U iotapp -At -d iot_dashboard -c "SELECT CASE WHEN max(received_at) IS NOT NULL AND max(received_at) >= now() - interval '2 minutes' THEN 'fresh' ELSE 'stale' END FROM readings;"); then
  printf 'ALERT freshness_check_failed checked_at=%s\n' "$(date -u +%FT%TZ)" >&2
  exit 2
fi
if [[ "$state" != fresh ]]; then
  printf 'ALERT data_stale checked_at=%s\n' "$(date -u +%FT%TZ)" >&2
  exit 1
fi
printf 'OK data_fresh checked_at=%s\n' "$(date -u +%FT%TZ)"
BASH
sudo chown root:iotapp /usr/local/sbin/iot-freshness-check
sudo chmod 0750 /usr/local/sbin/iot-freshness-check
sudo tee /etc/systemd/system/iot-freshness-check.service >/dev/null <<'UNIT'
[Unit]
Description=Check IoT telemetry freshness

[Service]
Type=oneshot
User=iotapp
Group=iotapp
Environment=PGPASSFILE=/etc/iot-dashboard/iotapp.pgpass
ExecStart=/usr/local/sbin/iot-freshness-check
NoNewPrivileges=true
PrivateTmp=true
ProtectHome=true
ProtectSystem=strict
UNIT
sudo tee /etc/systemd/system/iot-freshness-check.timer >/dev/null <<'TIMER'
[Unit]
Description=Run IoT freshness check every minute

[Timer]
OnCalendar=*-*-* *:*:00
Persistent=true
Unit=iot-freshness-check.service

[Install]
WantedBy=timers.target
TIMER
sudo systemd-analyze verify /etc/systemd/system/iot-freshness-check.service /etc/systemd/system/iot-freshness-check.timer
sudo systemctl daemon-reload
sudo systemctl enable --now iot-freshness-check.timer
sudo systemctl start iot-freshness-check.service
sudo journalctl -u iot-freshness-check.service -n 20 --no-pager
```

Alert này mới tạo signal trong journal; production còn cần rule forwarding/on-call. Runbook phải ghi condition, impact, first commands, owner, escalation và clear condition. Không gửi payload/secret.

**Bằng chứng đạt:** các lệnh manual ghi lại process/log/disk/RAM/DB freshness; khi simulator/worker chạy, journal có `OK data_fresh`; dừng ingestion trên 2 phút tạo `ALERT data_stale`; dừng PostgreSQL tạo `ALERT freshness_check_failed`; phục hồi tạo `OK` và `systemctl reset-failed` clear state. Bài này chưa triển khai Prometheus/tracing hay alert forwarding, nên không được claim full metrics platform.

**Break/fix:** ngưỡng 2 phút quá thấp so interval thực gây noise; đo baseline rồi điều chỉnh threshold, không disable alert mù.

**Rollback/reset:**

```bash
sudo systemctl disable --now iot-freshness-check.timer
sudo rm -- /etc/systemd/system/iot-freshness-check.timer /etc/systemd/system/iot-freshness-check.service /usr/local/sbin/iot-freshness-check
sudo systemctl daemon-reload
sudo systemctl reset-failed
```

**Checkpoint:** kích hoạt stale, dùng runbook tìm worker/broker/DB, phục hồi, verify journal chuyển ALERT → OK và ghi timeline UTC.

**Feynman:** metric/log/trace khác nhau? Freshness khác latency? Alert không có runbook gây hậu quả gì?

<a id="ops-05"></a>

### OPS-05 — Update và phòng thủ SSH theo threat model

**Cần trước:** GATE-VPS, NET-04. **Mục tiêu:** inventory public services, cập nhật có kiểm soát và hiểu fail2ban không thay key/firewall/patch. **Rủi ro:** 🟠.

**Tự hỏi:** Bật fail2ban có cho phép bỏ SSH key và vá lỗi không?

**Chốt lại:** không. Nó chỉ giảm một số brute-force pattern; không thay identity, least privilege, patching và network control.

**Lab — VM/VPS disposable:**

```bash
sudo apt update
apt list --upgradable 2>/dev/null
sudo ss -lntup
sudo ufw status verbose
```

Nếu bật fail2ban, chỉ sau SSH policy/journal đúng. Ubuntu không bảo đảm jail `sshd` đã enable chỉ vì package được cài, nên tạo local override tối thiểu, kiểm effective config rồi mới khởi động:

```bash
sudo apt install -y fail2ban
sudo tee /etc/fail2ban/jail.d/sshd.local >/dev/null <<'CONF'
[sshd]
enabled = true
backend = systemd
port = ssh
CONF
sudo fail2ban-client -t
sudo systemctl enable --now fail2ban
sudo fail2ban-client status
sudo fail2ban-client status sshd
```

Nếu SSH dùng port khác, thay `port = ssh` bằng port effective đã xác minh ở SEC-04/NET-04; không đoán.

Cấu hình jail phải backup/validate; không tự ban IP đang dùng nếu chưa có console/allowlist.

**Bằng chứng đạt:** inventory port; biết update nào pending; SSH key-only/console; fail2ban status và log được giải thích.

**Break/fix:** tạo vài login failure trong VM, tìm jail log; không brute-force VPS. Kiểm false positive và đường unban qua console.

**Rollback/reset:** xóa riêng `/etc/fail2ban/jail.d/sshd.local`, chạy `sudo fail2ban-client -t`, restart/disable fail2ban theo mục tiêu lab; không xóa key/SSH hardening. Nếu config mới lỗi, phục hồi file backup trước restart.

**Checkpoint:** viết threat model gồm attacker/cửa/bằng chứng/control/giới hạn, không gọi fail2ban là “bảo mật đầy đủ”.

**Feynman:** threat model khác checklist? Fail2ban bảo vệ tầng nào? Patch window cần rollback gì?

<a id="ops-06"></a>

### OPS-06 — Secret lifecycle: create, rotate, revoke

**Cần trước:** SYS-05, OPS-02, IOT-06. **Mục tiêu:** tạo secret ngoài Git, cấp quyền tối thiểu, rotate/revoke DB/device credential và kiểm không lộ log/process. **Rủi ro:** 🔴 nếu dùng credential thật.

**Tự hỏi:** Xóa secret khỏi file hiện tại có đủ nếu nó từng ở Git/log không?

**Chốt lại:** không; phải revoke/rotate, đánh giá history/backup/log và kiểm nơi sao chép.

**Lab — credential MQTT thật của device lab:** rotate `sensor-001`, vì thay file giả không chứng minh revoke. Dùng prompt không echo; password không nằm literal trong shell history nhưng `-P` có thể thoáng hiện trong process list, nên chỉ thực hành trên VM disposable không có user khác:

```bash
cd "$HOME/linux-course/iot-dashboard"
git rev-parse --is-inside-work-tree
ROTATION_APPLIED=0
if git grep -nF 'LAB_ROTATE_ME'; then
  printf 'Secret-like value is tracked by Git; rotate/revoke it before continuing\n' >&2
  false
else
  read -rsp 'Current sensor-001 password: ' OLD_DEVICE_PASSWORD; printf '\n'
  ROTATE_NOW=$(date -u +%FT%TZ)
  ROTATE_MID=$(cat /proc/sys/kernel/random/uuid)
  mosquitto_pub --cafile "$HOME/.config/mqtt-lab-ca.crt" -h YOUR_LAB_HOST -p 8883 -u sensor-001 -P "$OLD_DEVICE_PASSWORD" -t 'lab/devices/sensor-001/telemetry' -m "{\"message_id\":\"$ROTATE_MID\",\"device_id\":\"sensor-001\",\"device_time\":\"$ROTATE_NOW\",\"temperature\":22,\"humidity\":55}"
  sudo cp -a /etc/mosquitto/passwd /etc/mosquitto/passwd.before-rotation
  sudo mosquitto_passwd /etc/mosquitto/passwd sensor-001
  sudo chown root:mosquitto /etc/mosquitto/passwd
  sudo chmod 0640 /etc/mosquitto/passwd
  if ! sudo systemctl reload mosquitto; then
    sudo cp -a /etc/mosquitto/passwd.before-rotation /etc/mosquitto/passwd
    sudo chown root:mosquitto /etc/mosquitto/passwd
    sudo chmod 0640 /etc/mosquitto/passwd
    sudo systemctl restart mosquitto
    false
  else
    sudo systemctl is-active --quiet mosquitto
    ROTATION_APPLIED=1
  fi
fi
test "$ROTATION_APPLIED" = 1
```

Credential cũ phải fail. Sau đó nhập password mới vào biến tạm và chứng minh pass:

```bash
OLD_REJECTED=0
ROTATE_OLD_NOW=$(date -u +%FT%TZ)
ROTATE_OLD_MID=$(cat /proc/sys/kernel/random/uuid)
if mosquitto_pub --cafile "$HOME/.config/mqtt-lab-ca.crt" -h YOUR_LAB_HOST -p 8883 -u sensor-001 -P "$OLD_DEVICE_PASSWORD" -t 'lab/devices/sensor-001/telemetry' -m "{\"message_id\":\"$ROTATE_OLD_MID\",\"device_id\":\"sensor-001\",\"device_time\":\"$ROTATE_OLD_NOW\",\"temperature\":22,\"humidity\":55}"; then
  printf 'FAIL: old credential still opens a new connection\n' >&2
  false
else
  OLD_STATUS=$?
  OLD_REJECTED=1
  printf 'PASS: old credential rejected (status=%s)\n' "$OLD_STATUS"
fi
if test "$OLD_REJECTED" = 1; then
  unset OLD_DEVICE_PASSWORD
  read -rsp 'New sensor-001 password: ' NEW_DEVICE_PASSWORD; printf '\n'
  ROTATE_NEW_NOW=$(date -u +%FT%TZ)
  ROTATE_NEW_MID=$(cat /proc/sys/kernel/random/uuid)
  mosquitto_pub --cafile "$HOME/.config/mqtt-lab-ca.crt" -h YOUR_LAB_HOST -p 8883 -u sensor-001 -P "$NEW_DEVICE_PASSWORD" -t 'lab/devices/sensor-001/telemetry' -m "{\"message_id\":\"$ROTATE_NEW_MID\",\"device_id\":\"sensor-001\",\"device_time\":\"$ROTATE_NEW_NOW\",\"temperature\":22,\"humidity\":55}"
fi
test "$OLD_REJECTED" = 1
```

Cập nhật password file simulator bằng file `.new` mode `0640`, atomic move và restart đúng instance; backup client file trước để rollback hai phía cùng nhau, không log password:

```bash
sudo cp -a /etc/iot-dashboard/devices/sensor-001.password /etc/iot-dashboard/devices/sensor-001.password.before-rotation
printf '%s\n' "$NEW_DEVICE_PASSWORD" | sudo install -o root -g iotapp -m 0640 /dev/stdin /etc/iot-dashboard/devices/sensor-001.password.new
sudo mv -T /etc/iot-dashboard/devices/sensor-001.password.new /etc/iot-dashboard/devices/sensor-001.password
unset NEW_DEVICE_PASSWORD
sudo systemctl restart iot-simulator@sensor-001
sudo journalctl -u iot-simulator@sensor-001 -n 20 --no-pager
```

Nếu password mới sai, restore **cả** `/etc/mosquitto/passwd.before-rotation` và `sensor-001.password.before-rotation`, đặt lại owner/mode, reload broker, restart simulator rồi test lại. Chỉ xóa hai backup sau khi old-fails/new-passes và simulator đã reconnect; không đóng đường quản trị trong lúc rotation.

**Bằng chứng đạt:** kết nối mới bằng credential cũ bị broker từ chối, credential mới pass; file mode đúng; Git/journal không chứa value; downtime/rollback được ghi. Rotation không tự chứng minh session đã kết nối trước bị cắt; nếu yêu cầu revoke tức thì, phải kick/restart có kiểm soát và test session cũ riêng. Lab này dùng `mosquitto_pub -P`, nên **không** được claim argv không chứa secret: giá trị có thể xuất hiện ngắn hạn trong process list. Production phải dùng client/API hỗ trợ secret file, protected credential store hoặc cơ chế không truyền password qua argv.

**Break/fix:** password mới sai: giữ phiên quản trị/backup password file, đặt lại một giá trị khác qua `mosquitto_passwd`, cập nhật client atomically và test.

**Rollback/reset:** revoke device bằng `sudo mosquitto_passwd -D /etc/mosquitto/passwd sensor-001` khi kết thúc lab, reload/restart và chứng minh credential cuối cũng fail; xóa client config.

**Checkpoint:** rotate một credential device lab, test cũ fail/mới pass, không ghi value vào evidence/Git/journal; ghi rõ giới hạn argv của `-P` và viết rollback nếu giá trị mới lỗi.

**Feynman:** secret storage khác secret rotation? Revoke khác delete file? Vì sao process command line có thể lộ secret?

<a id="ops-07"></a>

### OPS-07 — Staging và production

**Cần trước:** WEB-06, OPS-06. **Mục tiêu:** tách code/config/secret/domain/data và deploy gate; lỗi staging không làm hỏng production. **Rủi ro:** 🟠.

**Tự hỏi:** Đổi mỗi domain có đủ tách staging khỏi production không?

**Chốt lại:** không; còn database, credential, network, alert destination, backup và dữ liệu.

**Lab:** lập inventory hai môi trường:

```text
staging: staging.YOUR_DOMAIN, DB iot_staging, device keys lab, alert sandbox
production: YOUR_DOMAIN, DB iot_production, device keys production, alert on-call
```

Tạo release staging từ commit/tag, chạy migration/health/smoke/authorization matrix. Chỉ promote artifact đã qua gate; không `git pull` trực tiếp production.

```bash
git rev-parse HEAD
git diff --exit-code
curl -fsS https://staging.YOUR_DOMAIN/health
```

**Bằng chứng đạt:** staging có secret/domain/data riêng; release lỗi bị chặn; production version không đổi; alert không gửi nhầm.

**Break/fix:** đưa migration/health fail vào staging; capture evidence và giữ production release cũ.

**Rollback/reset:** xóa staging resource/DB chỉ sau backup/cleanup check; production rollback theo WEB-06.

**Checkpoint:** một người khác chứng minh bằng inventory rằng staging không thể dùng credential production và một bản lỗi không promote.

**Feynman:** staging giống production ở gì và khác gì? Artifact promotion khác rebuild production? Vì sao test DB migration cần restore?

<a id="ops-08"></a>

### OPS-08 — Runbook, incident drill và postmortem

**Cần trước:** OPS-03, OPS-04, OPS-07. **Mục tiêu:** người khác dùng runbook xử lý broker/worker/DB incident, ghi timeline UTC, mitigation, root cause và action owner. **Rủi ro:** 🟡/🟠 lab.

**Tự hỏi:** Restart service ngay có phải lúc nào cũng là xử lý sự cố đúng không?

**Chốt lại:** restart có thể giảm triệu chứng nhưng làm mất evidence hoặc che root cause; thu bằng chứng và bảo toàn dữ liệu trước.

**Mẫu runbook:**

```text
Impact:
Scope:
Expected vs actual:
First safe checks:
Logs/metrics/evidence:
Mitigation:
Root-cause candidates:
Rollback/restore:
Verification:
Escalation/owner:
Communication:
Follow-up action/deadline:
```

**Incident drill — VM/staging:** dừng worker hoặc Mosquitto; ghi thời điểm UTC; dashboard phải báo stale; lấy `systemctl`, journal, DB freshness; khởi động/rollback theo runbook; verify history/realtime/alert clear.

Postmortem không đổ lỗi:

- impact/duration/detection;
- contributing conditions;
- root cause dựa trên evidence;
- điều gì hoạt động/không;
- action có owner/deadline;
- test phòng tái diễn.

**Bằng chứng đạt:** người không viết runbook dùng được; timeline khớp log/metric; restore/mitigation không xóa evidence; action có owner.

**Break/fix:** runbook thiếu một bước và người thực hiện bị kẹt; ghi “missing prerequisite” rồi sửa runbook, không đổ lỗi người chạy.

**Rollback/reset:** khôi phục service/data; lưu postmortem lab không có secret; đóng alert test và cleanup.

**Checkpoint:** điều phối tabletop “worker stopped + DB healthy”, rồi “DB unavailable”; phân biệt mitigation, recovery và root-cause fix.

**Feynman:** runbook khác tutorial? Incident timeline vì sao dùng UTC? Postmortem tốt thay đổi hệ thống thế nào?

---

## Cổng M6 — Operable

Pass khi có:

- image/Compose hoặc native service được kiểm soát;
- backup DB/config và restore sạch;
- metrics/logs/freshness/alert có hành động;
- update/threat model/SSH controls;
- secret rotate/revoke;
- staging gate;
- người khác dùng runbook xử lý incident và viết postmortem.

Không gọi hệ thống “tự phục hồi” chỉ vì có Docker/monitoring/backup; phải có restart/failover/restore behavior được kiểm thử.

---

<!-- Nội dung OPS-01 đến OPS-08 đã được tích hợp ngay sau phần DATA/IOT. -->

---

## Cấp 7 — Nhánh tự động hóa và IoT quy mô (Optional)

> Chỉ chọn khi Core đã operable và có nhu cầu/bottleneck được đo. Mỗi bài phải có decision record: vấn đề, lựa chọn, trade-off, số đo trước/sau và rollback.
>
> **Hợp đồng phạm vi Optional:** nhãn **hands-on** chỉ dùng khi chính bài cung cấp đủ fixture/lệnh để tạo evidence trên VM/sandbox. Nhãn **decision/tabletop** dùng khi mục tiêu là chọn kiến trúc hoặc diễn tập quyết định; pass bằng artifact và rubric được nêu, không được claim đã vận hành công nghệ. Nhãn **environment-bound** yêu cầu tài nguyên ngoài file này như hai VM, provider hoặc cluster; phần chưa chạy phải ghi `NOT RUN` thay vì suy diễn từ thiết kế.

### Nhánh Web/Tự động hóa

<a id="auto-01"></a>

### AUTO-01 — Shell script idempotent và xử lý lỗi

**Cần trước:** CLI-05, OPS-08. **Mục tiêu:** viết script chạy lặp không phá trạng thái, kiểm input/exit, log vừa đủ và cleanup. **Rủi ro:** 🟠.

**Tự hỏi:** Chạy thành công một lần có chứng minh script automation an toàn không?

**Chốt lại:** không; cần test lần hai, partial failure, input sai, concurrent run và rollback.

**Lab — VM:**

```bash
cat > "$HOME/iot-health-check.sh" <<'BASH'
#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

log() { printf '%s %s\n' "$(date -u +%FT%TZ)" "$*" >&2; }
cleanup() { rm -f -- "${tmp:-}"; }
trap cleanup EXIT

tmp=$(mktemp)
if ! curl -fsS --max-time 5 http://127.0.0.1:8080/health >"$tmp"; then
  log 'health request failed'
  exit 1
fi
python3 -c 'import json,sys; x=json.load(open(sys.argv[1])); assert x["status"]=="ok"' "$tmp"
log 'health check passed'
BASH
chmod 0750 "$HOME/iot-health-check.sh"
bash -n "$HOME/iot-health-check.sh"
"$HOME/iot-health-check.sh"
"$HOME/iot-health-check.sh"
```

Không dùng `set -e` như phép màu; hiểu command trong condition/pipeline/trap. Lock hoặc systemd timer policy khi không muốn chạy chồng.

**Bằng chứng đạt:** syntax pass; hai lần cùng kết quả; app down trả nonzero; temp file cleanup; không log secret.

**Break/fix:** đổi URL sai hoặc JSON thiếu status; xác định exit/log, không thêm `|| true` để che lỗi.

**Rollback/reset:** xóa script/timer lab; restore app. Không chạy script có `rm`, wildcard hoặc root trước dry-run/sandbox.

**Checkpoint:** viết script backup/health khác có `--dry-run`, validate, cleanup, nonzero on failure và chạy lặp an toàn.

**Feynman:** idempotent nghĩa gì? Strict mode không bắt loại lỗi nào? Log thế nào để không lộ secret?

<a id="auto-02"></a>

### AUTO-02 — Ansible trước, Terraform khi cần provider resources

**Cần trước:** AUTO-01, OPS-07. **Mục tiêu:** tái tạo staging idempotent, xem check/diff và quản lý state/drift. **Rủi ro:** 🟠/💰.

**Tự hỏi:** Vì sao Terraform không thay Ansible hoàn toàn và ngược lại?

**Chốt lại:** Terraform mạnh về lifecycle tài nguyên/provider/state; Ansible mạnh về cấu hình host. Ranh giới phụ thuộc hệ thống và ownership.

**Phạm vi:** decision/tabletop. File này không cung cấp `inventory.ini`, `site.yml`, role/template, SSH target hay Terraform provider/state fixture; vì vậy các lệnh dưới chỉ là **contract của một lab tự xây**, không phải block có thể dán từ máy sạch:

```bash
ansible-playbook -i inventory.ini site.yml --check --diff
ansible-playbook -i inventory.ini site.yml
ansible-playbook -i inventory.ini site.yml
```

**Input fixture cho decision record:** một staging VM Ubuntu 24.04 cần user service, directory release, systemd unit và Nginx site; hiện tại operator tạo thủ công trong 45 phút, mỗi tháng có hai lần drift, chưa có nhu cầu tạo cloud resource bằng API. So sánh ít nhất: shell idempotent, Ansible và Terraform+Ansible theo `time-to-rebuild`, drift detection, secret/state exposure, rollback và chi phí vận hành.

**Artifact bắt buộc:** bảng quyết định ghi `chosen/not chosen`, giả định, owner, state/secret boundary, file tối thiểu phải viết nếu triển khai và phép đo sẽ chứng minh lần chạy hai không đổi. Kết luận “Ansible phù hợp, Terraform chưa cần” có thể pass; không được ghi `changed=0`, “staging đã dựng” hoặc “Terraform plan pass” nếu chưa có output thật.

**Bằng chứng đạt:** decision record truy được từ input tới kết luận, nêu rõ evidence `NOT RUN`; hoặc, nếu người học tự cung cấp fixture ngoài tài liệu, đính kèm inventory/playbook đã redact, check/diff, apply lần một, lần hai `changed=0` và một drift được phát hiện.

**Break/fix tabletop:** một operator sửa Nginx tay. Quyết định đưa thay đổi hợp lệ vào source hay revert drift; không để hai nguồn sự thật.

**Rollback/reset:** không có hạ tầng được tạo trong capsule mặc định. Nếu tự chạy sandbox, revert code; `destroy` chỉ sau plan/inventory/backup và không bao giờ dùng production để luyện.

**Checkpoint:** chấm đạt khi bảng quyết định đủ sáu tiêu chí trên và không overclaim runtime. Hands-on chỉ đạt khi learner-supplied fixture chứng minh idempotency/drift; file này không tự claim phần đó.

**Feynman:** desired state là gì? State file có rủi ro nào? Check mode có bảo đảm apply 100% không?

<a id="auto-03"></a>

### AUTO-03 — CI/CD qua test, artifact, staging và rollback

**Cần trước:** OPS-07, AUTO-02. **Mục tiêu:** pipeline tạo artifact cố định, test, deploy staging, smoke/approval và rollback production. **Rủi ro:** 🔴 outward-facing.

**Tự hỏi:** “Push là tự deploy production” thiếu những cổng nào?

**Chốt lại:** thiếu test, artifact integrity, secret boundary, staging, approval policy, smoke test, migration/rollback và audit.

**Phạm vi:** decision/tabletop. File này không chọn GitHub/GitLab/runner, không có repository remote, credential sandbox hay pipeline YAML; vì vậy không được gọi đây là pipeline đã chạy.

**Input fixture:** release B đang ở production; commit C pass unit test nhưng `/health.version` sai; migration C không backward-compatible; staging và production có credential riêng; approval cần một người ngoài tác giả. Điền bảng:

```text
Gate | input | pass evidence | fail action | owner
lint/unit | ... | ... | ... | ...
artifact/checksum | ... | ... | ... | ...
staging deploy/smoke/auth | ... | ... | ... | ...
migration compatibility | ... | ... | ... | ...
approval | ... | ... | ... | ...
production smoke | ... | ... | ... | ...
rollback/restore | ... | ... | ... | ...
```

Pipeline thiết kế phải đi theo: lint/unit/security scan → artifact theo commit + checksum → staging → migration/smoke/auth → approval → production → post-deploy smoke → rollback policy. Runner không SSH bằng root hoặc giữ credential rộng; environment secret tách và branch/tag protection được ghi như control, không tự coi đã bật.

**Bằng chứng đạt:** bảng chỉ ra C bị chặn trước production vì version/migration; production B không đổi theo mô hình state transition; artifact, approval, secret boundary và rollback owner rõ. Mọi bước runtime chưa chạy ghi `NOT RUN`.

**Break/fix tabletop:** giả định production smoke fail sau activate C; chọn rollback code hay restore DB dựa trên compatibility, không sửa tay release.

**Rollback/reset:** capsule mặc định không deploy outward-facing. Nếu learner tự cung cấp CI sandbox, xóa/revoke credential và runner disposable sau inventory; giữ log đã redact.

**Checkpoint:** decision/tabletop pass khi mọi gate có input/evidence/fail action/owner và C không thể promote. Chỉ claim pipeline chạy khi có YAML, run URL/log, artifact checksum, staging evidence và rollback test từ sandbox được phép.

**Feynman:** CI khác CD? Artifact promotion khác rebuild? Rollback code không đủ khi nào?

<a id="auto-04"></a>

### AUTO-04 — Đo tải trước khi multi-node

**Cần trước:** OPS-04, OPS-07. **Mục tiêu:** đo bottleneck, cân nhắc scale dọc/ngang, health/state/shared data trước load balancer. **Rủi ro:** 🟠; chỉ staging có giới hạn.

**Tự hỏi:** CPU app thấp nhưng DB latency cao thì thêm nhiều app node có chắc nhanh hơn không?

**Chốt lại:** không; có thể tăng connection/query load vào bottleneck DB.

**Phạm vi:** decision/tabletop; không tạo load trên hệ thống chưa được cấp quyền. File này không cung cấp hai app node, load-balancer config hay load fixture hoàn chỉnh.

**Input fixture:** baseline staging gồm request rate, p50/p95/p99, 5xx, CPU/RAM app, DB latency/connections/disk và SLO; thêm một scenario DB p95 tăng nhưng CPU app còn thấp. Ghi stopping condition, giới hạn request/độ dài/thời gian, owner và cleanup trước khi chọn tool.

**Artifact bắt buộc:** report trước/sau hoặc bảng “chưa đo/NOT RUN”, decision scale dọc/ngang/chưa scale, bottleneck hypothesis, state/session/cache/upload assumptions, expected rollback. Nếu tự chạy sandbox, chỉ dùng tài nguyên thuộc quyền và đính kèm command/config đã redact.

**Bằng chứng đạt:** decision giải thích vì sao không thêm node chỉ vì CPU thấp, gắn mỗi kết luận với số đo hoặc `NOT RUN`; không claim load-balancer health/failover khi chưa có hai node và test thật.

**Break/fix tabletop:** một node bị lỗi trong giới hạn SLO; xác định evidence cần có trước khi gọi “traffic còn phục vụ”.

**Rollback/reset:** hạ hoặc dừng load; xóa node/rule chỉ trong sandbox sau inventory; không tác động production.

**Checkpoint:** pass khi report có SLO, baseline, stop condition, trade-off/chi phí và rollback; runtime load test/failover là environment-bound và chỉ được tick khi có log thật.

**Feynman:** throughput khác latency? Stateless nghĩa gì? Healthcheck sai có thể gây outage thế nào?

### Nhánh IoT quy mô

<a id="iotscale-01"></a>

### IOTSCALE-01 — Edge/gateway khi mất mạng

**Cần trước:** IOT-06, OPS-04. **Mục tiêu:** buffer có giới hạn, gửi lại idempotent và áp dụng drop/backpressure policy. **Rủi ro:** 🟠.

**Tự hỏi:** Buffer mọi telemetry vô hạn có phải “không mất dữ liệu” không?

**Chốt lại:** không; disk đầy sẽ làm gateway/hệ khác hỏng. Cần capacity, TTL, priority, compression/drop và alert.

**Phạm vi:** decision/tabletop mặc định. File này không cung cấp gateway daemon, queue schema, sender, broker outage harness hay fixture để tự claim đã drain queue.

**Input fixture:** producer `P` phát 2 msg/s; uplink outage 10 phút; queue giới hạn 1.000 rows/10 MiB/15 phút; mỗi message có `message_id`; policy ưu tiên telemetry mới, TTL 15 phút, khi đầy thì drop/quarantine theo thứ tự được ghi. Điền state transition cho `connected → offline → full → reconnect → drain`.

**Artifact bắt buộc:** bảng capacity (rows/bytes/age), accepted/duplicate/dropped/quarantined, retry/backoff, disk alert, RPO trade-off và rollback. Nếu tự xây fixture SQLite/file queue trong VM, phải đính kèm schema/command, log trước–sau và cleanup; nếu không, ghi các số đo là `NOT RUN`.

**Bằng chứng đạt:** policy chứng minh queue hữu hạn và có hành động khi đầy; không claim reconnect drain hoặc duplicate dedupe khi chưa có sender/receiver runtime evidence.

**Break/fix tabletop:** DB/broker chậm hơn producer; chọn backpressure/drop/quarantine và ngưỡng stop trước khi queue chạm disk limit.

**Rollback/reset:** stop producer; preserve/quarantine queue trước cleanup; chỉ xóa sau checksum/evidence hoặc khi đã chấp nhận mất dữ liệu lab.

**Checkpoint:** pass bằng decision record 10-minute outage với timeline, giới hạn, số đo `NOT RUN` minh bạch và rollback. Runtime queue/reconnect chỉ được đánh dấu pass khi chạy trên gateway VM thuộc quyền.

**Feynman:** edge khác cloud? Backpressure khác retry? Idempotency bảo vệ điều gì?

<a id="iotscale-02"></a>

### IOTSCALE-02 — Device registry và OTA ký số

**Cần trước:** IOT-03, OPS-06, IOTSCALE-01. **Mục tiêu:** quản lý trạng thái device/credential, verify artifact ký số, canary rollout, rollback/chống downgrade. **Rủi ro:** 🔴; simulator/emulator, không flash thiết bị thật khi chưa có recovery.

**Tự hỏi:** HTTPS tải firmware có đủ chứng minh firmware đúng nhà phát hành không?

**Chốt lại:** không; TLS bảo vệ phiên tải, nhưng artifact cần signature/hash/version policy và key lifecycle.

**Phạm vi:** decision/tabletop mặc định; không flash thiết bị thật. File này không cung cấp registry service, signing script/key fixture, OTA transport hay simulator rollout harness đủ để claim đã verify artifact.

**Input fixture:** ba simulator `sensor-001..003`; model `lab-v1`; current `1.0.0`; desired `1.1.0`; signing key chỉ là key lab tạm; artifact manifest gồm device/model/version/hash/signature; canary 001 trước, stop nếu một canary fail. Các case bắt buộc: signature đúng; hash sai; model sai; downgrade `0.9.0`; device revoked.

**Artifact bắt buộc:** state table trước/sau, verification policy, canary/stop condition, last-known-good, key/credential lifecycle và rollback. Nếu tự cung cấp fixture ngoài tài liệu, đính kèm public key, manifest đã redact, verifier output và audit; private key không đưa vào Git.

**Bằng chứng đạt:** decision record có ma trận accept/reject cho năm case và chỉ ra bước nào là `NOT RUN`; không claim install, canary hay revoke runtime nếu chưa có simulator evidence.

**Break/fix tabletop:** artifact valid signature nhưng sai target model; policy phải fail trước apply và không đổi desired/current state.

**Rollback/reset:** trong capsule mặc định chỉ reset bảng quyết định. Nếu tự chạy sandbox, simulator quay last-known-good, revoke key/credential test, giữ audit đã redact và xóa private key sau inventory.

**Checkpoint:** pass khi staged rollout 3 device được mô hình hóa với stop condition, rollback và blast radius; runtime OTA chỉ được tick khi có fixture/emulator thuộc quyền chứng minh canary fail không ảnh hưởng hai device còn lại.

**Feynman:** signing khác encryption? Registry khác MQTT ACL? Chống downgrade ngăn rủi ro nào?

<a id="iotscale-03"></a>

### IOTSCALE-03 — Kafka/stream processing chỉ khi có nhu cầu

**Cần trước:** IOT-04, OPS-04. **Mục tiêu:** hiểu partition, consumer group, retention/replay và đánh giá chi phí trước khi thêm Kafka. **Rủi ro:** 🟠.

**Tự hỏi:** Kafka có phải “MQTT lớn hơn” không?

**Chốt lại:** không. MQTT tối ưu pub/sub thiết bị/broker; Kafka là distributed log cho throughput, replay và consumer decoupling, với vận hành khác.

**Phạm vi:** decision/tabletop mặc định. File này không cung cấp Kafka broker/Compose, producer, consumer, topic setup hay DB sink fixture; không được claim replay/throughput runtime từ sơ đồ.

**Input fixture:** telemetry 100 msg/s trong 10 phút; key `device_id`; cần hai consumer group (analytics và persistence), replay cửa sổ 5 phút, ordering chỉ trong partition, Mosquitto+PostgreSQL baseline có lag/throughput/storage/ops cost. Ghi ngưỡng mà replay hoặc consumer decoupling vượt khả năng baseline.

**Artifact bắt buộc:** bảng so sánh MQTT+DB/Kafka+DB, partition/key/consumer-group policy, duplicate/idempotency policy, retention/storage estimate, operational owner và rollback/migration boundary. Mọi số đo chưa có runtime phải ghi `NOT RUN` hoặc `ESTIMATE`.

**Bằng chứng đạt:** decision record giải thích bottleneck hoặc requirement thật trước khi chọn Kafka; không claim replay không duplicate, ordering hay throughput nếu chưa có producer/consumer/DB fixture chạy được.

**Break/fix tabletop:** partition key sai gây skew hoặc device mất ordering; chỉ chấp nhận claim sau khi nêu phép đo distribution/lag cần chạy.

**Rollback/reset:** không có cluster mặc định để xóa. Nếu tự chạy sandbox, dừng cluster, giữ report đã redact và không migrate production vì lab.

**Checkpoint:** pass nếu conclusion “chưa dùng” hoặc “cần thử Kafka” được suy ra từ baseline/estimate có nguồn và trade-off; runtime replay chỉ pass khi có log offset, lag, duplicate count và cleanup evidence.

**Feynman:** log retention/replay khác MQTT delivery? Partition đổi ordering ra sao? Chi phí vận hành gồm gì?

---

## Cấp 8 — Nhánh Linux/SRE chiều sâu (Optional)

> Mỗi bài cần giả thuyết và giới hạn quan sát. Công cụ sâu có thể làm chậm hệ thống hoặc thu dữ liệu nhạy cảm; chỉ dùng trên hệ thống thuộc quyền và ưu tiên VM/staging.

<a id="deep-01"></a>

### DEEP-01 — Process, memory, namespace và cgroup

**Cần trước:** OPS-01, OPS-04. **Mục tiêu:** map service/container tới PID, memory/cgroup limits và giải thích OOM bằng evidence. **Rủi ro:** 🟠.

**Tự hỏi:** Container báo hết memory có nghĩa host hết RAM không?

**Chốt lại:** không; có thể chạm cgroup limit dù host còn RAM. Ngược lại host pressure có thể ảnh hưởng nhiều cgroup.

**Lab — VM:**

```bash
PID=$(systemctl show -p MainPID --value iot-api)
ps -o pid,ppid,user,stat,%cpu,%mem,rss,vsz,cmd -p "$PID"
cat "/proc/$PID/status" | grep -E '^(Name|Pid|PPid|VmRSS|VmSize|Threads):'
cat "/proc/$PID/cgroup"
systemctl status iot-api --no-pager
systemd-cgls
systemd-cgtop -n 1
```

Với container:

```bash
docker inspect --format '{{.State.Pid}} {{.HostConfig.Memory}}' iot-api-lab
docker stats --no-stream iot-api-lab
```

Chỉ tạo memory limit/pressure trong VM có stop condition; xem kernel journal:

```bash
sudo journalctl -k --since '10 minutes ago' | grep -i -E 'oom|killed process' || true
```

**Bằng chứng đạt:** map đúng PID/process/cgroup/limit; phân biệt app allocation, RSS, cgroup limit và host memory.

**Break/fix:** container/process bị OOM trong VM; dùng event/journal/cgroup metric, không kết luận từ “process biến mất”.

**Rollback/reset:** dừng workload pressure; bỏ resource override lab, daemon-reload/restart; xác minh memory trở baseline.

**Checkpoint:** đặt limit an toàn cho process lab, tạo pressure có kiểm soát, xác định nguyên nhân và rollback.

**Feynman:** virtual memory khác RSS? Namespace khác cgroup? OOM kill khác app exit code?

<a id="deep-02"></a>

### DEEP-02 — `strace`, `perf`, `tcpdump`, `iostat`

**Cần trước:** DEEP-01, OPS-04. **Mục tiêu:** chọn đúng tool cho syscall/CPU/network/disk, capture ngắn và bảo vệ dữ liệu. **Rủi ro:** 🟠/🔴 trên production.

**Tự hỏi:** Vì sao chạy tất cả tool cùng lúc có thể làm evidence tệ hơn?

**Chốt lại:** overhead và dữ liệu khổng lồ làm đổi behavior/khó tương quan; bắt đầu từ giả thuyết và thời gian ngắn.

**Lab — VM:**

```bash
sudo apt install -y strace sysstat tcpdump linux-tools-common
strace -f -tt -T -o /tmp/iot.strace curl -fsS http://127.0.0.1:8080/health
sudo iostat -xz 1 3
sudo tcpdump -i lo -nn -c 20 'tcp port 8080'
```

`tcpdump` có thể chứa payload/token/cookie; dùng filter, capture count/time, quyền file chặt và xóa sau phân tích. `perf stat -p PID -- sleep 5` chỉ khi kernel/tools cho phép; không hạ security control tùy tiện để chạy perf.

**Bằng chứng đạt:** strace chỉ ra syscall/connect/time; tcpdump cho TCP flow loopback; iostat cho latency/utilization; chọn tool đúng hypothesis.

**Break/fix:** app timeout do connect DB; strace/network capture giúp xác định syscall/socket, không dùng perf CPU làm phép đo đầu tiên.

**Rollback/reset:** dừng capture, xóa/sanitize `/tmp/iot.strace`/pcap; gỡ tool nếu policy yêu cầu.

**Checkpoint:** nhận bốn scenario CPU/syscall/network/disk và chọn một tool + filter + stop condition + privacy plan.

**Feynman:** tracing khác profiling? Capture packet có rủi ro gì? `iostat %util` có luôn là bottleneck không?

<a id="deep-03"></a>

### DEEP-03 — nftables, TCP và WireGuard lab

**Cần trước:** NET-04, DEEP-02. **Mục tiêu:** đọc effective nftables, không xung đột UFW, dựng VPN lab tối thiểu và chỉ tuning TCP sau đo. **Rủi ro:** 🔴; VM snapshot/console.

**Tự hỏi:** UFW và nftables có phải hai firewall độc lập không liên quan?

**Chốt lại:** UFW là frontend quản lý rules backend (trên Ubuntu hiện đại thường nftables); sửa tay có thể xung đột nguồn sự thật.

**Phạm vi:** environment-bound hands-on nếu có **hai VM lab thuộc quyền**; nếu chỉ có một VM thì decision/tabletop. Các lệnh quan sát dưới đây chạy được độc lập, nhưng không chứng minh VPN handshake.

```bash
sudo nft list ruleset
sudo ufw status verbose
sysctl net.ipv4.tcp_congestion_control
ss -s
```

**Input/fixture tối thiểu cho hai VM:** VM-A và VM-B có private IP đã inventory; một subnet tunnel riêng; public keys trao đổi ngoài Git; `AllowedIPs` chỉ chứa subnet lab; firewall chỉ cho UDP WireGuard giữa hai VM và ICMP/port test cần thiết. Không đăng private key. File này không cung cấp địa chỉ/IP/key thực tế nên không có một config có thể dán nguyên trạng.

```bash
sudo apt install -y wireguard
umask 077
wg genkey > /tmp/wg-private.key
wg pubkey < /tmp/wg-private.key > /tmp/wg-public.key
test -s /tmp/wg-private.key
test -s /tmp/wg-public.key
```

**Artifact bắt buộc:** inventory hai VM, config đã redact, `wg show`, `ip route`, handshake/reachability, rule ownership và baseline TCP trước/sau. Một VM không có peer/config là `NOT RUN`, không phải fail của WireGuard.

**Bằng chứng đạt:** chỉ gọi VPN runtime pass khi handshake và ping/private-port test thành công trên hai VM; decision/tabletop pass khi route/subnet/AllowedIPs/firewall/rollback được thiết kế và blast radius rõ.

**Break/fix:** AllowedIPs/route sai; đo bằng `wg show`, `ip route`, `ss`, capture ngắn, không mở `0.0.0.0/0` để chữa.

**Rollback/reset:** `wg-quick down wg0`; restore UFW/nft config/snapshot; xóa key lab sau inventory và xác nhận không còn peer dùng chúng.

**Checkpoint:** environment-bound pass chỉ khi dựng VPN hai VM, cố ý làm hỏng một route/firewall rule rồi sửa không lockout; decision pass khi ghi rõ `NOT RUN` và không overclaim.

**Feynman:** frontend/backend firewall? AllowedIPs vừa route vừa policy ra sao? Vì sao TCP tuning mù nguy hiểm?

<a id="deep-04"></a>

### DEEP-04 — LVM, RAID, filesystem và I/O trên disk ảo

**Cần trước:** SYS-08, OPS-03. **Mục tiêu:** mở rộng storage lab, mô phỏng disk failure và phân biệt redundancy/snapshot/backup. **Rủi ro:** 🔴; chỉ disk ảo VM snapshot.

**Tự hỏi:** RAID1 có bảo vệ khỏi `DELETE` nhầm không?

**Chốt lại:** không; thao tác xóa được mirror. Backup độc lập/restore drill mới bảo vệ nhiều failure mode logic.

**Phạm vi:** environment-bound hands-on chỉ trên VM snapshot có disk ảo riêng; file không thể biết device path/serial của hypervisor nên không cung cấp lệnh `pvcreate`/`mdadm --create` có target cố định. Nếu chưa gắn disk, đây là planning capsule và mọi runtime evidence ghi `NOT RUN`.

**Input/fixture tối thiểu:** inventory trước/sau bằng `lsblk -o NAME,SIZE,TYPE,FSTYPE,MOUNTPOINTS,MODEL,SERIAL`, ít nhất hai disk/loop image disposable có marker ownership, backup độc lập đã restore thử, mountpoint lab rỗng và console VM. Người học tự điền exact device path sau khi đối chiếu size/serial; không copy `/dev/sdX` từ tài liệu.

**Artifact bắt buộc:** target inventory, sequence PV→VG→LV→filesystem→mount hoặc RAID create→degraded→rebuild, marker checksum trước/sau, exact rollback và bằng chứng backup độc lập. Không trộn LVM expansion và RAID failure thành một claim nếu chỉ chạy một phần.

**Bằng chứng đạt:** chỉ gọi expansion/degraded/rebuild pass khi output `pvs/vgs/lvs`, `findmnt/df` hoặc `mdadm --detail` và marker checksum tồn tại; planning pass khi chọn target/guard/rollback đúng và ghi `NOT RUN`.

**Break/fix:** mount sai UUID hoặc một member mất; dùng `lsblk -f`, `blkid`, `mdadm --detail`, journal trước hành động.

**Rollback/reset:** unmount, remove LV/VG/PV/md chỉ sau khi inventory lại và xác nhận đúng disk ảo; nếu target không chắc, dừng và restore snapshot thay vì đoán.

**Checkpoint:** environment-bound pass khi mở rộng filesystem hoặc mô phỏng RAID failure trên disk ảo và restore marker từ backup riêng; planning pass không được claim runtime RPO/RTO.

**Feynman:** LVM khác filesystem? RAID khác backup? Snapshot crash-consistent khác app-consistent?

<a id="deep-05"></a>

### DEEP-05 — AppArmor, audit, PKI và CVE workflow

**Cần trước:** OPS-05, OPS-06. **Mục tiêu:** dùng AppArmor phù hợp Ubuntu, audit event, quản lý certificate/CVE theo asset/impact/test/rollback. **Rủi ro:** 🟠/🔴 policy enforcement.

**Tự hỏi:** App không chạy sau policy thì `aa-complain` vĩnh viễn hoặc cấp quyền rộng có phải sửa đúng?

**Chốt lại:** không; complain dùng học/audit tạm. Phải xác định hành vi hợp lệ tối thiểu, test enforce và theo dõi deny.

**Phạm vi:** environment-bound hands-on trên VM Ubuntu có AppArmor và app/profile lab riêng; nếu không có profile/certificate fixture thì đây là decision/tabletop. Không disable global AppArmor và không lấy certificate production làm fixture.

**Input/fixture tối thiểu:** một script lab đọc file được phép và thử đọc một file ngoài policy; profile path riêng; certificate lab từ IOT-03 hoặc fixture public; một advisory/CVE mẫu với version/asset/impact. Các lệnh quan sát an toàn:

```bash
sudo aa-status
sudo journalctl -k | grep -i apparmor | tail -n 20
sudo auditctl -s 2>/dev/null || true
openssl x509 -in /etc/mosquitto/certs/lab-server.crt -noout -subject -issuer -dates 2>/dev/null || true
```

**Artifact bắt buộc:** profile diff/owner, allow/deny matrix, audit evidence, certificate inventory (CA/key owner, SAN, expiry, rotation/revoke), CVE record (applicability → exposure → fix/mitigation → staging test → backup/rollback → verify). Nếu chưa có profile/cert thật, ghi `NOT RUN` thay vì lấy output rỗng làm pass.

**Bằng chứng đạt:** runtime pass chỉ khi behavior cần thiết pass và access ngoài policy bị deny với event; decision pass khi policy tối thiểu, rollback và CVE decision có owner/deadline.

**Break/fix:** deny file cần thiết; dùng audit path/operation/profile để mở đúng quyền, không tắt toàn AppArmor.

**Rollback/reset:** đưa riêng profile lab về complain/disable theo công cụ, restore backup; không disable global security control; xóa certificate/key fixture theo inventory.

**Checkpoint:** environment-bound pass khi chặn đúng một access không cần mà health lab vẫn pass; planning pass khi có matrix/decision đầy đủ và mọi phần runtime chưa chạy được đánh dấu `NOT RUN`.

**Feynman:** MAC/AppArmor bổ sung Unix permission thế nào? Audit khác alert? CVE score có tự quyết patch priority không?

<a id="deep-06"></a>

### DEEP-06 — SLO, RTO/RPO, HA/DR và chaos có giới hạn

**Cần trước:** OPS-03, OPS-04, OPS-08. **Mục tiêu:** định nghĩa mục tiêu trước kiến trúc, chọn backup/replication/failover và chạy failure drill có stop condition. **Rủi ro:** 🔴; staging/VM.

**Tự hỏi:** Hai node có tự động đạt HA không?

**Chốt lại:** không; còn failure domain, health/failover, state consistency, split brain, capacity và vận hành.

**Phạm vi:** decision/tabletop mặc định; runtime failure drill chỉ environment-bound trên VM/staging thuộc quyền. File này không cung cấp HA/failover controller, second node, disk-image fixture hay automatic measurement harness; không claim actual RTO/RPO từ bảng thiết kế.

**Input fixture:** điền:

```text
SLO availability/latency/freshness:
RTO:
RPO:
Failure hypothesis:
Blast radius:
Preconditions/backup:
Stop condition:
Expected signal:
Recovery/rollback:
```

Chọn đúng một failure hypothesis cho tabletop: worker stopped, broker stopped, DB unavailable hoặc disk image lab chạm threshold. Nếu tự chạy, inventory target, writer/backup state, stop condition, maximum duration/bytes và console trước; tuyệt đối không fill root disk, làm chaos trên production hoặc gọi replication/RAID là backup.

**Artifact bắt buộc:** goal/baseline/assumption table, timeline, expected signal, mitigation/recovery/rollback, actual hoặc `NOT RUN` detection/RTO/RPO, data-loss boundary và owner. Runtime chỉ được claim khi có command/log/version/target/cleanup thật.

**Bằng chứng đạt:** decision pass khi mục tiêu và trade-off đủ để chọn/không chọn HA/DR; runtime pass chỉ khi drill có stop, actual RTO/RPO, restore/consistency và alert clear evidence.

**Break/fix tabletop:** failover nhanh nhưng data stale vượt RPO; ghi fail và điều chỉnh design, không chỉ ghi uptime.

**Rollback/reset:** stop chaos; restore network/service/data; verify alert clear and consistency; preserve evidence trước cleanup.

**Checkpoint:** decision/tabletop pass với fixture và `NOT RUN` boundary; environment-bound pass chỉ sau một drill được phép có timeline, measurement, rollback và cleanup.

**Feynman:** SLI/SLO khác alert? RTO/RPO khác nhau? HA khác DR?

<a id="deep-07"></a>

### DEEP-07 — Kubernetes khi lợi ích vượt chi phí

**Cần trước:** OPS-02, AUTO-03, DEEP-06. **Mục tiêu:** hiểu pod/deployment/service/probe/resource/rollout và viết decision record dùng/không dùng. **Rủi ro:** 🟠/💰.

**Tự hỏi:** Kubernetes có làm app tự động an toàn và HA không?

**Chốt lại:** không; manifest, probe, resource, state, cluster/control plane, network/secret/backup và vận hành vẫn phải đúng.

**Phạm vi:** decision/tabletop mặc định; environment-bound hands-on chỉ khi learner có local cluster và tự cung cấp manifest. File này không chứa thư mục `k8s/`, Deployment/Service/ConfigMap/Secret YAML hay cluster bootstrap, nên block `kubectl apply -f k8s/` không được coi là runnable từ tài liệu.

**Input fixture:** API image lab đã build; hai replicas; readiness/liveness khác mục đích; requests/limits; Service nội bộ; ConfigMap non-secret; Secret giả; release B tốt và image C lỗi health. Không đưa PostgreSQL production vào cluster chỉ để học.

```bash
# Chỉ chạy sau khi learner đã tạo và review manifest k8s/ trong sandbox.
kubectl apply -f k8s/
kubectl get pods,deploy,svc
kubectl describe deployment iot-api
kubectl rollout status deployment/iot-api
kubectl rollout history deployment/iot-api
kubectl rollout undo deployment/iot-api
```

**Artifact bắt buộc:** decision record so systemd/Compose/Kubernetes theo scale, failure domains, operator burden, secret/network/backup; manifest inventory và expected probe/resource/rollout states. Nếu không có cluster/manifest, mọi command/evidence runtime ghi `NOT RUN`.

**Bằng chứng đạt:** decision pass khi kết luận dùng/không dùng có tiêu chí và không overclaim. Runtime pass chỉ khi manifest tồn tại, readiness loại pod chưa sẵn, bad image rollout fail/rollback và resource/probe/log evidence được lưu.

**Break/fix tabletop hoặc sandbox:** liveness quá gắt gây restart loop; phân biệt startup/readiness/liveness và sửa theo behavior app.

**Rollback/reset:** không có cluster mặc định để xóa. Nếu tự chạy, inventory namespace/context trước rồi chỉ delete namespace/cluster lab thuộc quyền; không xóa cluster dùng chung.

**Checkpoint:** decision/tabletop pass khi giải thích Kubernetes có đáng cho dashboard hiện tại và ghi `NOT RUN`; hands-on chỉ pass sau bad-release rollout/rollback thật trên local cluster được phép.

**Feynman:** pod khác container? Readiness khác liveness? Kubernetes không thay backup/secrets/app auth thế nào?

<a id="deep-08"></a>

### DEEP-08 — Incident response và postmortem đa tầng

**Cần trước:** OPS-08, DEEP-06. **Mục tiêu:** điều phối incident, bảo toàn evidence, phân biệt mitigation/root-cause fix và tạo action có owner/deadline. **Rủi ro:** 🟠 tabletop/staging.

**Tự hỏi:** Khi incident đang cháy, có nên ưu tiên tìm root cause hoàn hảo trước khi giảm impact không?

**Chốt lại:** tùy mức độ; thường mitigation an toàn/khả nghịch để giảm impact song song bảo toàn evidence, rồi root-cause analysis sau ổn định.

**Tabletop scenario:** dashboard stale; API 200; broker reconnect tăng; DB disk latency cao; deploy mới vừa diễn ra. Vai trò: incident commander, operations, communications, scribe. Timeline UTC; decision log; evidence chain.

Phân loại:

```text
Symptom: dashboard stale
Impact: device/user nào, duration
Mitigation: rollback worker hoặc giảm ingest
Root cause: chỉ kết luận sau evidence
Contributing factors: alert/runbook/capacity/change
Corrective actions: test, capacity, guardrail, owner/deadline
```

**Bằng chứng đạt:** communication cadence; owner rõ; evidence không bị xóa; mitigation/rollback verify; postmortem không quy lỗi cá nhân.

**Break/fix:** team giả định “do deploy” nhưng evidence chỉ DB saturation; facilitator yêu cầu hypothesis/measure, không confirmation bias.

**Rollback/reset:** đóng tabletop, khôi phục staging, revoke credential test, archive evidence không secret.

**Checkpoint:** chạy incident đa tầng 30–60 phút; người review có thể tái tạo timeline và phân biệt symptom, mitigation, root cause, prevention.

**Feynman:** incident command giảm chaos thế nào? Root cause khác contributing factor? Postmortem không đổ lỗi vẫn giữ accountability ra sao?

## Cổng M7 — Hoàn thành một nhánh chiều sâu

Không cần học cả Cấp 7–8. Chọn đúng một trong hai đường và không trộn claim:

**Decision/tabletop pass:**

- vấn đề/hypothesis và input fixture rõ;
- rubric, state transition, blast radius và rollback được điền;
- số đo là source-backed `ESTIMATE` hoặc ghi `NOT RUN`;
- decision record nêu khi nào **không** dùng giải pháp và artifact cần thêm để chuyển sang runtime.

**Runtime pass:**

- có mọi điều kiện trên, cộng artifact/config/code thật;
- target/version/ownership được inventory;
- số đo trước/sau, negative path và rollback/restore thực sự pass;
- cleanup và secret/evidence boundary được xác minh;
- runbook/postmortem cập nhật nếu thay đổi vận hành.

`NOT RUN` là giới hạn claim trung thực, không phải runtime pass. Một capsule environment-bound không bị coi là fail chỉ vì thiếu provider/two-VM/cluster, nhưng cũng không được ghi “đã vận hành”.

> Chuyên môn sâu là khả năng đo, thiết kế, khôi phục và nêu giới hạn trong một miền — không phải tích đủ tên mọi công nghệ.

---

## Phụ lục A — Checklist hoàn thành Core

- [ ] VM 24.04 restore snapshot được.
- [ ] User thường, SSH key, quyền file và repo không chứa secret.
- [ ] App/worker chạy non-root qua systemd và sống qua reboot.
- [ ] Nginx public; app/database bind nội bộ.
- [ ] Domain A/AAAA đúng trước Certbot; HTTPS và renewal dry-run pass.
- [ ] Device identity/ACL tách biệt; payload được validate.
- [ ] PostgreSQL lưu device time và server receive time theo UTC.
- [ ] API lịch sử và realtime reconnect hoạt động.
- [ ] Backup được restore sang môi trường sạch.
- [ ] Alert có runbook; incident drill và postmortem đã làm.
- [ ] Release pin commit/tag, smoke test và rollback được.
- [ ] Cloud resource không dùng đã xóa và billing đã kiểm tra.

## Phụ lục B — Câu tự kiểm chung

1. Thành phần này thực chất là gì?
2. Nó giải quyết vấn đề nào trong dashboard?
3. Bằng chứng pass/fail nào đáng tin?
4. Khi nào không nên dùng nó?
5. Nếu hỏng, phép đo đầu tiên là gì?
6. Rollback và kiểm tra sau rollback ra sao?
7. Sau logout/reboot hoặc mất mạng chuyện gì xảy ra?
8. Port, identity, quyền hoặc secret nào làm tăng bề mặt tấn công?
