using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using MyAssets.Scripts.Manager.Network;
using MyAssets.Scripts.Utility;


namespace MyAssets.Scripts.Manager
{
    public enum ExperimentState
    {
        Idle, // 初期状態・待機中
        ReadyToStart, // 実験開始準備完了
        TrialRunning, // 試行実行中
        TrialFinished, // 試行終了後のフィードバック表示中
        Resting, // ブロック間の休憩中
        Finished, // 全ての実験が終了
        Stopped // 緊急停止中
    }

    public class ExperimentManager : Singleton<ExperimentManager>
    {
        [Header("Experiment Parameters")] [SerializeField]
        private int totalBlocks = 4;

        [SerializeField] private int trialsPerBlock = 30;
        [SerializeField] private float restTimeSeconds = 30f;
        [SerializeField] private float feedbackTimeSeconds = 1.5f;


        //--------プロパティ--------//
        public ExperimentState CurrentState { get; private set; } //現在の実験ステート
        public int CurrentTrial { get; private set; } = 0; //現在の試行
        public int CurrentBlock { get; private set; } = 0; //現在の実験ブロック
        public string SubjectID { get; private set; } //被験者ID


        //--------他コンポーネントの内部参照--------//
        private UIManager _uiManager;
        private NetworkManager _networkManager;
        private DataLogger _dataLogger;
        private TaskCompletionSource<bool> _trialCompletionSource;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this.gameObject);
        }

        private async void Start()
        {
            _networkManager = NetworkManager.Instance;
            _networkManager.OnDataReceived += OnMovementDataReceived;
            await RunExperimentFlow();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                EmergencyStop();
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            _networkManager?.StartReceiving();
        }

        
        // ==== Main Experiment Flow ====
        
        /// <summary>
        /// 実験フローを開始する非同期関数
        /// </summary>
        private async Task RunExperimentFlow()
        {
            //1. 初期化と被験者のID入力待ち
            ChangeState(ExperimentState.Idle);
            SubjectID = await _uiManager.ShowInitialAndGetIdAsync();
            Debug.Log($"被験者ID: {SubjectID} で実験を開始します。");
            
            //2. ネットワーク接続の確認
            if (!_networkManager.StartReceiving())
            {
                // TODO UIにエラー表示を指示
                // uiManager.ShowError("ネットワークの初期化に失敗しました。");
                ChangeState(ExperimentState.Stopped);
                return;
            }
            
            //3. ブロックと試行のループ
            for (int i = 0; i < totalBlocks; i++)
            {
                CurrentBlock = i + 1;
                await RunBlock();
            }
            
            //4. 終了処理
            ChangeState(ExperimentState.Finished);
        }

        
        /// <summary>
        /// ブロックを開始する非同期関数
        /// </summary>
        private async Task RunBlock()
        {
            //ブロック開始準備
            ChangeState(ExperimentState.ReadyToStart);
            // TODO:データロガーを初期化
            // _dataLogger.Initialize($"Logs/{SubjectID}_Block{CurrentBlock}.csv");
            
            //試行開始待ち画面を表示し、ユーザーの入力を待つ
            await _uiManager.ShowWaitForTrialAsync(CurrentBlock, totalBlocks);

            for (int i = 0; i < trialsPerBlock; i++)
            {
                CurrentTrial = i + 1;
                await RunTrial();
            }
            
            // ブロック終了処理
            // TODO: データロガーのファイルを閉じる
            // _dataLogger.Close();
            
            // 最終ブロックでなければ休憩に入る
            if (CurrentBlock < totalBlocks)
            {
                ChangeState(ExperimentState.Resting);
                await _uiManager.ShowRestScreenAsync(restTimeSeconds);
            }
        }
        
        /// <summary>
        /// 試行を開始する非同期関数
        /// </summary>
        private async Task RunTrial()
        {
            ChangeState(ExperimentState.TrialRunning);
            _uiManager.UpdateProgress(CurrentTrial, totalBlocks);
            
            // TODO: ここでターゲット位置などを決定する
            Vector2 startPos = Vector2.zero;
            Vector2 endPos = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
            
            // UIにターゲットを表示させる
            _uiManager.ShowTrialScreen(startPos, endPos);
            
            // Linux側に試行開始を通知
            await _networkManager.SendCommandAsync($"START_TRIAL;{endPos.x};{endPos.y}");
            
            // Linux側からの試行終了通知を待つ
            // NetworkManagerに終了通知を受信するイベント追加し、待機
            _trialCompletionSource = new TaskCompletionSource<bool>();
            await _trialCompletionSource.Task;  // 試行終了まで待機
            
            //試行終了後の処理
            ChangeState(ExperimentState.TrialFinished);
            await _uiManager.ShowFeedbackAsync("Great!",feedbackTimeSeconds);
        }
        
        
        // === Event Handlers & State Management ===
        
        private void OnMovementDataReceived(MovementData data)
        {
            // このメソッドはネットワークスレッドから呼び出されるので、
            // Unityのオブジェクト（UIなど）を直接操作してはいけない。

            // TODO: データロガーにデータを渡す
            // if(CurrentState == ExperimentState.TrialRunning) {
            //     _dataLogger.LogData(data, CurrentState, CurrentTrial);
            // }

            // TODO: UIManagerにデータを渡してカーソルを更新させる (スレッドセーフなキュー経由で)
            // uiManager.QueueCursorPosition(new Vector2(data.HandlePosX, data.HandlePosY));

            // TODO: Linux側から試行終了の合図が送られてきたら、待機中のTaskを完了させる
            // 例: if(data.IsTrialFinished) { _trialCompletionSource?.TrySetResult(true); }
        }
        
        /// <summary>
        /// 実験状態を変更する
        /// </summary>
        private void ChangeState(ExperimentState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            Debug.Log($"State changed to: {newState}");

            // TODO: UIManagerに状態遷移を通知し、適切な画面表示をさせる
            // uiManager.OnStateChanged(newState);
        }
        
        
        /// <summary>
        /// 実験を緊急停止する
        /// </summary>
        private void EmergencyStop()
        {
            if (CurrentState == ExperimentState.Finished || CurrentState == ExperimentState.Stopped) return;
            _networkManager.StartReceiving();
            ChangeState(ExperimentState.Stopped);
        }
    }
}