using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using MyAssets.Scripts.Manager.Network;
using UnityEngine;

using MyAssets.Scripts.Utility;

namespace MyAssets.Scripts.Manager
{
    public class DataLogger : Singleton<DataLogger>
    {
        
        //--------Privateプロパティ--------//
        private ConcurrentQueue<string> _logQueue;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _loggingTask;
        private StreamWriter _writer;
        private string _currentFilePath;
        
        //--------Unity Lifecycle Methods--------//
        
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this.gameObject);
        }

        private void OnDestroy()
        {
            Close();
        }
        
        //--------Public Methods--------//
        
        /// <summary>
        /// ロギングの初期化処理
        /// </summary>
        /// <param name="filePath">ログファイルのパス</param>
        /// <returns>初期化の正否フラグ</returns>
        public bool Initialize(string filePath)
        {
            if (_loggingTask != null && _loggingTask.IsCompleted)
            {
                Close();
            }

            try
            {
                _currentFilePath = filePath;
                var directory = Path.GetDirectoryName(_currentFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                //新しいキューとCancellationTokenを作成
                _logQueue = new ConcurrentQueue<string>();
                _cancellationTokenSource = new CancellationTokenSource();

                //ファイルを書き込み設定で開く
                _writer = new StreamWriter(_currentFilePath, false, System.Text.Encoding.UTF8);

                //CSVヘッダーの書き込み
                string header = "Timestamp,HandlePosX,HandlePosY,HandleVelX,HandleVelY,HandleAccX,HandleAccY," +
                                "TargetStartPosX,TargetStartPosY,TargetEndPosX,TargetEndPosY," +
                                "ExperimentState,CurrentTrial,IsTrialFinished";
                _writer.WriteLine(header);

                //非同期でファイル書き込みループを開始
                _loggingTask = Task.Run(() => LoggingLoop(_cancellationTokenSource.Token));

                Debug.Log($"ロギングを開始しました。ファイル: {_currentFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ロギングの初期化に失敗しました: {ex.Message}");
                _writer?.Dispose();
                _writer = null;
                return false;
            }
        }
    
        /// <summary>
        /// ログに記録したいデータをキューに追加する。
        /// どのスレッドからでも呼び出し可能
        /// </summary>
        /// <param name="data">記録するMovementData構造体</param>
        /// <param name="state">記録時の実験状態</param>
        /// <param name="trialNum">記録時の試行番号</param>
        public void LogData(MovementData data, ExperimentState state, int trialNum)
        {
            if(_loggingTask == null || _loggingTask.IsCompleted) return;

            string line = string.Join(",",
                data.TimeStep.ToString(CultureInfo.InvariantCulture),
                data.HandlePosX.ToString(CultureInfo.InvariantCulture),
                data.HandlePosY.ToString(CultureInfo.InvariantCulture),
                data.HandleVelX.ToString(CultureInfo.InvariantCulture),
                data.HandleVelY.ToString(CultureInfo.InvariantCulture),
                data.HandleAccX.ToString(CultureInfo.InvariantCulture),
                data.HandleAccY.ToString(CultureInfo.InvariantCulture),
                data.TargetStartPosX.ToString(CultureInfo.InvariantCulture),
                data.TargetStartPosY.ToString(CultureInfo.InvariantCulture),
                data.TargetEndPosX.ToString(CultureInfo.InvariantCulture),
                data.TargetEndPosY.ToString(CultureInfo.InvariantCulture),
                state.ToString(),
                trialNum.ToString(),
                data.IsTrialFinished.ToString()
            );
            
            //スレッドセーフなキューに追加
            _logQueue.Enqueue(line);
        }

        /// <summary>
        /// 現在のロギングを終了し、ファイルを安全に閉じる
        /// </summary>
        public void Close()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                Debug.Log($"ロギングの停止を要求します。ファイル: {_currentFilePath}");
                _cancellationTokenSource.Cancel();
                
                //書き込みタスクの完了待ち
                try
                {
                    _loggingTask?.Wait(2000);
                }
                catch (OperationCanceledException)
                {
                    /*正常終了*/
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ロギングタスクの大気中にエラー: {ex.Message}");
                }
                finally
                {
                    _writer?.Dispose();
                    _writer = null;
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                    Debug.Log("ロギングを正常終了しました");
                }
            }
        }
        
        //--------Private Methods--------//

        private async Task LoggingLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested || !_logQueue.IsEmpty)
            {
                if (_logQueue.TryDequeue(out string line))
                {
                    if (_writer != null)
                    {
                        // ファイルに非同期で書き込む
                        await _writer.WriteLineAsync(line);
                    }
                }
                else
                {
                    try
                    {
                        await Task.Delay(1, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
            
            //ループを抜けた後、キューの残りをすべて書き出す
            while (_logQueue.TryDequeue(out string remainingLine))
            {
                await _writer.WriteLineAsync(remainingLine);
            }
            _writer?.Flush();
        }
    }    
}

