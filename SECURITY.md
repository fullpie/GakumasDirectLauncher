# 安全說明

智慧啟動器處理的遊戲啟動資料包括：

- `viewer_id`
- `onetime_token`
- `open_id`
- `pf_access_token`

這些資料只會保存在目前 Windows 使用者可解密的 DPAPI 檔案中。工具自己的 `launcher.log` 僅記錄固定事件名稱與非敏感狀態。

請勿提交或分享以下檔案：

```text
credentials.dat
dll.log
authAccessTokenData.enc
Local State
dmmgame.cnf
Gakumas-API-Diagnostic-*.txt
```

若要公開程式碼，請先掃描工作目錄與 Git 歷史，確認沒有任何啟動資料或 DMM 帳號資料。

本工具不會繞過 DMM 的登入或區域限制；需要更新或刷新啟動資料時，仍由使用者透過官方 DMM 完成。

---

# Security

The launcher handles four game launch values:

- `viewer_id`
- `onetime_token`
- `open_id`
- `pf_access_token`

They are stored only in a Windows DPAPI CurrentUser-protected file. The launcher's own log contains fixed event names and non-sensitive status information only.

Never commit or share these files:

```text
credentials.dat
dll.log
authAccessTokenData.enc
Local State
dmmgame.cnf
Gakumas-API-Diagnostic-*.txt
```

Before publishing a fork, scan both the working tree and Git history for DMM account data and game launch values.

This tool does not bypass DMM login or regional restrictions. The official DMM client is still used whenever game updates, login renewal, or fresh launch data are required.
