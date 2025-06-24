using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MyAssets.Scripts.Utility;
using UnityEngine.UI;
using TMPro;


namespace MyAssets.Scripts.Manager
{
    public class UIManager : Singleton<UIManager>
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject initialPanel;       //被験者ID入力画面
        [SerializeField] private GameObject waitForTrialPanel;  //試行開始待ち画面
        [SerializeField] private GameObject trialPanel;         //試行中画面
        [SerializeField] private GameObject restPanel;          //休憩画面
        [SerializeField] private GameObject finishedPanel;      //実験終了画面
        [SerializeField] private GameObject stoppedPanel;       //緊急停止画面
        [SerializeField] private GameObject errorPanel;         //エラー表示画面


        [Header("Initial Panel Components")]
        [SerializeField] private TMP_InputField subjectIdInputField;
        [SerializeField] private Button submitIdButton;

        [Header("Wait For Trial Panel Components")]
        [SerializeField] private TextMeshProUGUI blockInfoText;
        [SerializeField] private Button startTrialButton;
        
        [Header("Trial Panel Components")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private GameObject targetObject;
        [SerializeField] private GameObject startObject;
        [SerializeField] private GameObject cursorObject; 
    
        [Header("Rest Panel Components")]
        [SerializeField] private TextMeshProUGUI restTimerText;
        [SerializeField] private Button startNextBlockButton;
    
        [Header("Feedback Components")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private GameObject feedbackPanel;
        
        [Header("Error Panel Components")]
        [SerializeField] private TextMeshProUGUI errorText;
        
        // TaskCompletionSourceを使って、UI操作（ボタン押下など）を待機可能にする
        private TaskCompletionSource<string> _subjectIdTcs;
        private TaskCompletionSource<bool> _waitForInputTcs;

        // === Unity Lifecycle Methods ===
        
        protected override void Awake()
        {
            base.Awake();
            
            //ボタンにリスナーを登録
            submitIdButton.onClick.AddListener(OnSubmitIDClicked);
            startTrialButton.onClick.AddListener(OnStartTrialClicked);
            startNextBlockButton.onClick.AddListener(OnStartNextBlockClicked);

            DeactiveAllPanels();
        }
        
        /// <summary>
        /// InitialPanelを表示して、被験者IDの入力待ちを行う
        /// </summary>
        public Task<string> ShowInitialAndGetIdAsync()
        {
            initialPanel.SetActive(true);
            subjectIdInputField.text = "";

            _subjectIdTcs = new TaskCompletionSource<string>();
            return _subjectIdTcs.Task;
        }
        
        /// <summary>
        /// 試行開始待ち画面を表示し、ボタンが押されるまで待機する
        /// </summary>
        public async Task ShowWaitForTrialAsync(int currentBlock, int totalBlocks)
        {
            DeactiveAllPanels();
            waitForTrialPanel.SetActive(true);
            blockInfoText.text = $"ブロック {currentBlock} / {totalBlocks}";

            _waitForInputTcs = new TaskCompletionSource<bool>();
            await _waitForInputTcs.Task;
        }
        
        /// <summary>
        /// 試行画面を表示
        /// 現在の試行、ブロック数を表示
        /// NetWorkManagerからエンドエフェクタ座標、始点終点を受け取ったExperimentManagerからこれらを受け取る
        /// エンドエフェクタ座標、始点、終点を描写
        /// </summary>
        public void ShowTrialScreen(Vector2 startPos, Vector2 endPos)
        {
            DeactiveAllPanels();
            trialPanel.SetActive(true);
            startObject.transform.position = startPos;
            targetObject.transform.position = endPos;
        }

        /// <summary>
        /// 試行の進捗を更新
        /// </summary>
        public void UpdateProgress(int currentTrial, int trialsPerBlock)
        {
            if (progressText != null)
            {
                progressText.text = $"試行{currentTrial}/{trialsPerBlock}";
            }
        }

        /// <summary>
        /// フィードバックを一定時間表示する
        /// </summary>
        public async Task ShowFeedbackAsync(string message, float durationSeconds)
        {
            feedbackText.text = message;
            feedbackPanel.SetActive(true);
            await Task.Delay((int)(durationSeconds * 1000));
            feedbackPanel.SetActive(false);
        }

        public async Task ShowRestScreenAsync(float durationSeconds)
        {
            DeactiveAllPanels();
            restPanel.SetActive(true);
            startNextBlockButton.interactable = false;
            
            //残り時間をカウントダウン表示
            float timer = durationSeconds;
            while (timer > 0)
            {
                restTimerText.text = $"休憩時間: 残り{Mathf.CeilToInt(timer)} 秒";
                await Task.Yield();
                timer -= Time.deltaTime;
            }

            restTimerText.text = "準備ができたら、次のブロックを開始してください";
            startNextBlockButton.interactable = true;

            _waitForInputTcs = new TaskCompletionSource<bool>();
            await _waitForInputTcs.Task;
        }
        

        /// <summary>
        /// 実験ブロックの終了画面を表示
        /// </summary>
        public void ShowFinishedScreen()
        {
            DeactiveAllPanels();
            finishedPanel.SetActive(true);
        }

        /// <summary>
        /// 緊急停止画面を表示
        /// </summary>
        public void ShowEmergencyStopScreen()
        {
            DeactiveAllPanels();
            stoppedPanel.SetActive(true);
        }

        public void ShowError(string message)
        {
            DeactiveAllPanels();
            errorPanel.SetActive(true);
            errorText.text = $"Error Message: {message}";
        }
        
        // ==== Private Methods ====

        /// <summary>
        /// 被験者IDを登録する
        /// </summary>
        private void OnSubmitIDClicked()
        {
            string id = subjectIdInputField.text;
            if(string.IsNullOrEmpty(id)) return;
            
            //Task完了、結果としてIDを渡す
            _subjectIdTcs?.SetResult(id);
            initialPanel.SetActive(false);
        }

        /// <summary>
        /// 次試行開始ボタンのコールバック
        /// </summary>
        private void OnStartTrialClicked()
        {
            _waitForInputTcs?.TrySetResult(true);
            waitForTrialPanel.SetActive(false);
        }
        
        /// <summary>
        /// 次ブロック開始ボタンのコールバック
        /// </summary>
        private void OnStartNextBlockClicked()
        {
            _waitForInputTcs?.TrySetResult(true);
            restPanel.SetActive(false);
        }
        
        /// <summary>
        /// 全パネルを非アクティブ化
        /// </summary>
        private void DeactiveAllPanels()
        {
            initialPanel.SetActive(false);
            waitForTrialPanel.SetActive(false);
            trialPanel.SetActive(false);
            restPanel.SetActive(false);
            finishedPanel.SetActive(false);
            stoppedPanel.SetActive(false);
            errorPanel.SetActive(false);
            feedbackPanel.SetActive(false);
        }
    }
}