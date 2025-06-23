using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyAssets.Scripts.Manager.Network;
using UnityEngine;
using MyAssets.Scripts.Utility;


namespace MyAssets.Scripts.Manager
{
    public class ExperimentManager : Singleton<ExperimentManager> 
    {
        public enum ExperimentState
        {
            Initializing,       //初期化中
            WaitingForTrial,    //試行開始街
            InTrial,            //試行中
            Paused,             //実験中断中
            Finished,           //実験終了
        }

        //--------プロパティ--------//
        public ExperimentState CurrentState { get; private set; }   //現在の実験ステート
        public int CurrentTrial { get; private set; }               //現在の試行
        public int CurrentBlock { get; private set; }               //現在の実験ブロック
        public string SubjectID { get; private set; }               //被験者ID


        //--------他コンポーネントの内部参照--------//
        

        protected override void Awake()
        {
            base.Awake();
        }

        private async void Start()
        {
            await InitializeExperiment();
        }

        private void Update()
        {
            
        }
        
        /// <summary>
        /// 実験を開始する
        /// </summary>
        public void StartExperiment()
        {
        }

        /// <summary>
        /// 新しい試行を開始する
        /// </summary>
        public void StartNewTrial()
        {
        }

        /// <summary>
        /// 現在の試行を終了し、次の試行へ進む
        /// </summary>
        public void EndTrial()
        {
            
        }

        /// <summary>
        /// 実験を中断する
        /// </summary>
        public void PauseExperiment()
        {
        }

        /// <summary>
        /// 実験を再開する
        /// </summary>
        public void ResumeExperiment()
        {
        }


        /// <summary>
        /// 実験を緊急停止する
        /// </summary>
        public void EmergencyStop()
        {
        }


        /// <summary>
        /// 実験初期化する
        /// </summary>
        private async Task InitializeExperiment()
        {
            CurrentState = ExperimentState.Initializing;
            CurrentTrial = 0;
            CurrentBlock = 0;
            
            //NetworkManagerから初期化終了通知を受け取る
            if (!NetworkManager.Instance.StartReceiving()) return;
            
            //SubjectIDの入力処理
            this.SubjectID = await UIManager.Instance.ShowInitialAndGetIdAsync();
            Debug.Log($"ExperimentManagerがID '{this.SubjectID}' を受け取りました。");
            
            //stateをWaiting For Trial
            CurrentState = ExperimentState.WaitingForTrial;
            UIManager.Instance.ShowWaitForTrial();
        }
    }
}