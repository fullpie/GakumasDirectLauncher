using System;
using System.Globalization;

namespace GakumasSmartLauncher
{
    internal sealed class UiStrings
    {
        private UiStrings(bool isChinese)
        {
            IsChinese = isChinese;
        }

        public bool IsChinese { get; private set; }

        public static UiStrings Detect()
        {
            var ui = CultureInfo.CurrentUICulture == null
                ? string.Empty
                : CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var culture = CultureInfo.CurrentCulture == null
                ? string.Empty
                : CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            return new UiStrings(
                string.Equals(ui, "zh", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(culture, "zh", StringComparison.OrdinalIgnoreCase));
        }

        private string Pick(string traditionalChinese, string english)
        {
            return IsChinese ? traditionalChinese : english;
        }

        public string WindowTitle { get { return "Gakumas Direct"; } }
        public string HeaderTitle { get { return Pick("學園偶像大師 Direct", "Gakumas Direct"); } }
        public string HeaderSubtitle { get { return Pick("THE IDOLM@STER Gakuen · DMM PC 版直啟工具", "THE IDOLM@STER Gakuen · DMM PC launcher"); } }
        public string StatusEyebrow { get { return Pick("LIVE STATUS  /  啟動狀態", "LIVE STATUS"); } }
        public string Checking { get { return Pick("正在檢查…", "Checking…"); } }
        public string ReadingLocalState { get { return Pick("正在讀取本機狀態。", "Checking local launch data."); } }
        public string StartButton { get { return Pick("開始遊戲  ★", "START GAME  ★"); } }
        public string UpdateButton { get { return Pick("更新啟動資料", "UPDATE LAUNCH DATA"); } }
        public string GameVersionLabel { get { return Pick("遊戲版本", "GAME VERSION"); } }
        public string LastSyncLabel { get { return Pick("上次同步", "LAST SYNC"); } }
        public string DmmStatusLabel { get { return Pick("DMM 狀態", "DMM STATUS"); } }
        public string GameLocationLabel { get { return Pick("遊戲位置", "GAME LOCATION"); } }
        public string GameRunning { get { return Pick("遊戲正在執行", "Game is running"); } }
        public string Ready { get { return Pick("可以直接啟動", "Ready to launch"); } }
        public string NeedsUpdate { get { return Pick("Token 過期／遊戲需要更新", "Token expired / Game update required"); } }
        public string GameRunningMessage { get { return Pick("已確認遊戲程序；DMM 不需要保持開啟。", "The game is running; DMM does not need to stay open."); } }
        public string ReadyMessage { get { return Pick("啟動資料已準備完成。", "Launch data is ready."); } }
        public string UpdateInstruction { get { return Pick("請開啟 DMM 並啟動一次遊戲，完成後按「更新啟動資料」。", "Open DMM and launch the game once, then click UPDATE LAUNCH DATA."); } }
        public string Unknown { get { return Pick("未知", "Unknown"); } }
        public string NeedsResync { get { return Pick("需要重新同步", "Resync required"); } }
        public string NeverSynced { get { return Pick("尚未同步", "Never synced"); } }
        public string DmmRunning { get { return Pick("執行中 · 更新模式", "Running · Update mode"); } }
        public string DmmClosed { get { return Pick("未執行 · 正常", "Closed · Normal"); } }
        public string NotFound { get { return Pick("尚未找到", "Not found"); } }
        public string StartingGame { get { return Pick("正在啟動遊戲", "Starting game"); } }
        public string StartingFromCache { get { return Pick("使用 Windows 加密資料啟動，不會開啟 DMM。", "Starting with Windows-protected data without opening DMM."); } }
        public string AlreadyRunning { get { return Pick("遊戲已經在執行，不會重複啟動。", "The game is already running."); } }
        public string ConfirmingLaunch { get { return Pick("正在確認啟動狀態", "Confirming launch"); } }
        public string ProcessAppeared { get { return Pick("遊戲程序已出現，正在確認它是否穩定執行。", "The game process appeared; checking that it stays running."); } }
        public string LaunchSuccess { get { return Pick("啟動成功", "Launch successful"); } }
        public string LaunchSuccessMessage { get { return Pick("已確認遊戲穩定執行；DMM 全程沒有啟動。", "The game is running normally and DMM stayed closed."); } }
        public string UpdatingData { get { return Pick("正在更新啟動資料", "Updating launch data"); } }
        public string ReadingDmmLog { get { return Pick("正在讀取 DMM 最新紀錄…", "Reading the latest DMM launch record…"); } }
        public string NoNewData { get { return Pick("尚未偵測到新資料", "No new launch data found"); } }
        public string StartFromDmmFirst { get { return Pick("請先從 DMM 啟動遊戲一次，再按「更新啟動資料」。", "Launch the game from DMM once, then click UPDATE LAUNCH DATA."); } }
        public string UpdateComplete { get { return Pick("更新完成", "Update complete"); } }
        public string UpdatedAndClosed { get { return Pick("已更新啟動資料並關閉 DMM，下次可以直接啟動。", "Launch data was updated and DMM was closed. Direct launch is ready."); } }
        public string UpdatedButDmmOpen { get { return Pick("啟動資料已更新，但 DMM 未完全關閉，請手動退出。", "Launch data was updated, but DMM could not be fully closed. Please exit it manually."); } }
        public string ErrorTitle { get { return Pick("發生錯誤", "Something went wrong"); } }
        public string UnexpectedError { get { return Pick("發生未預期的錯誤，請從 DMM 啟動遊戲一次後再試。", "An unexpected error occurred. Launch the game once from DMM and try again."); } }
        public string AnotherInstance { get { return Pick("智慧啟動器已經開啟。", "Gakumas Direct is already open."); } }

        public string ConfirmingSeconds(int seconds)
        {
            return Pick(
                "遊戲持續執行中 · 還要確認 " + seconds.ToString(CultureInfo.InvariantCulture) + " 秒",
                "Game is running · confirming for " + seconds.ToString(CultureInfo.InvariantCulture) + " more seconds");
        }
    }
}
