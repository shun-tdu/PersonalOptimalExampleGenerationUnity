using System.Threading.Tasks;
using UnityEngine;
using MyAssets.Scripts.Utility;
using UnityEngine.UI;


namespace MyAssets.Scripts.Manager
{
    public class UIManager : Singleton<UIManager>
    {
        [Header("InitialScreen")] 
        [SerializeField] private GameObject initialPanel;           //初期化画面のパネル
        [SerializeField] private InputField subjectIdInputField;    //ID入力フィールド
        [SerializeField] private Button submitButton;               //送信ボタン


        private TaskCompletionSource<string> _subjectIdTcs;
        
        protected override void Awake()
        {
            base.Awake();
            submitButton.onClick.AddListener(SubmitID);
            initialPanel.SetActive(false);
        }
        
        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
        }
        
        /// <summary>
        /// 初期化画面を表示
        /// SubjectIDの入力フォームを持つ
        /// </summary>
        public void ShowInitial()
        {
            
        }
        
        /// <summary>
        /// 試行開始待ち画面を表示
        /// 現在の試行、ブロック数を表示
        /// ボタンを押して試行画面へ遷移
        /// </summary>
        public void ShowWaitForTrial()
        {
            
        }
        
        /// <summary>
        /// 試行画面を表示
        /// 現在の試行、ブロック数を表示
        /// NetWorkManagerからエンドエフェクタ座標、始点終点を受け取ったExperimentManagerからこれらを受け取る
        /// エンドエフェクタ座標、始点、終点を描写
        /// </summary>
        public void ShowTrial()
        {
            
        }
        
        /// <summary>
        /// 中断画面の表示
        /// 再開ボタンを設置
        /// </summary>
        public void ShowPause()
        {
            
        }

        /// <summary>
        /// 実験ブロックの終了画面を表示
        /// タイマーを表示
        /// タイマー経過後に初めて次のブロックを始められるようにする
        /// タイマー経過前にブロックを始めようとすると確認画面を表示
        /// </summary>
        public void ShowBlockFinished()
        {
            
        }
        /// <summary>
        /// 実験終了画面を表示
        /// </summary>
        public void ShowFinished()
        {
            
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
        /// 被験者IDを登録する
        /// </summary>
        private void SubmitID()
        {
            string id = subjectIdInputField.text;
            if(string.IsNullOrEmpty(id)) return;
            
            //Task完了、結果としてIDを渡す
            _subjectIdTcs.SetResult(id);
            
            initialPanel.SetActive(false);
        }
    }
}