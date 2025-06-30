using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MyAssets.Scripts.Utility;

namespace MyAssets.Scripts.Manager.Network
{
    //Linuxから受信するデータの構造体
    public struct MovementData
    {
        public float TimeStep; //時刻
        public float HandlePosX; //ハンドルのx座標位置
        public float HandlePosY; //ハンドルのy座標位置
        public float HandleVelX; //ハンドルのx座標速度
        public float HandleVelY; //ハンドルのy座標速度
        public float HandleAccX; //ハンドルのx座標加速度
        public float HandleAccY; //ハンドルのy座標加速度
        public float TargetStartPosX; //ターゲットの開始地点x座標
        public float TargetStartPosY; //ターゲットの開始地点y座標
        public float TargetEndPosX; //ターゲットの終了地点x座標
        public float TargetEndPosY; //ターゲットの終了地点y座標
        public int IsTrialFinished; //試行が終了したかどうかのフラグ(0=継続中、1=終了)
    }


    public class NetworkManager : Singleton<NetworkManager>
    {
        //--------Public Events--------//
        public event Action<MovementData> OnDataReceived;


        //--------Publicプロパティ--------//
        [Header("ネットワーク設定")] [SerializeField] private string liuxIpAddress = "127.0.0.1";
        [SerializeField] private int udpReceivePort = 20001;
        [SerializeField] private int tcpCommandPort = 20002;


        //--------Privateプロパティ--------//
        private UdpClient _udpClient;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _receiveTask;


        //--------Unity Lifecycle Methods--------//
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this.gameObject);
        }


        //--------Public Methods--------//
        /// <summary>
        /// UDPデータの受信を開始
        /// 開始処理の正否を返す
        /// </summary>
        public bool StartReceiving()
        {
            if (_receiveTask != null && _receiveTask.IsCompleted)
            {
                Debug.Log("受信はすでに開始されています");
                return true;
            }

            try
            {
                Debug.Log($"UDP受信を開始します。ポート:{udpReceivePort}");
                _udpClient = new UdpClient(udpReceivePort);
                _cancellationTokenSource = new CancellationTokenSource();

                //非同期で受信ループを開始
                _receiveTask = Task.Run(() => ReceiveLoop(_cancellationTokenSource.Token),
                    _cancellationTokenSource.Token);

                Debug.Log("受信タスクの開始に成功しました");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"UDP受信の開始に失敗しました:{ex.Message}");

                //リソースをクリーンアップ
                _udpClient?.Close();
                _cancellationTokenSource?.Dispose();

                return false;
            }
        }

        /// <summary>
        /// UDPデータの受信を停止する
        /// </summary>
        public void StopReceiving()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                Debug.Log("UDP受信の停止を要求します");
                _cancellationTokenSource.Cancel();

                try
                {
                    _receiveTask?.Wait(1000);
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("UDP受信タスクが終了しました");
                }
                finally
                {
                    _udpClient?.Close();
                    _udpClient = null;
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                    Debug.Log("UDP受信を終了しました");
                }
            }
        }


        /// <summary>
        /// Linux PCにTCPコマンドを非同期で送信
        /// </summary>
        /// <param name="command">送信するコマンド文字列</param>
        public async Task SendCommandAsync(string command)
        {
            if (string.IsNullOrEmpty(command)) return;

            try
            {
                using (var tcpClient = new TcpClient())
                {
                    //接続を試みる(タイムアウト付き)
                    var connectTask = tcpClient.ConnectAsync(liuxIpAddress, tcpCommandPort);
                    if (await Task.WhenAny(connectTask, Task.Delay(1000)) != connectTask)
                    {
                        Debug.LogError("TCP接続がタイムアウトしました");
                        return;
                    }

                    //接続成功後、データを送信
                    byte[] data = Encoding.UTF8.GetBytes(command);
                    NetworkStream stream = tcpClient.GetStream();
                    await stream.WriteAsync(data, 0, data.Length);
                    Debug.Log($"TCPコマンド送信成功:{command}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"TCPコマンド送信エラー:{ex.Message}");
            }
        }

        //--------Private Methods--------//

        /// <summary>
        /// UDPデータを受信し続けるバックグランドタスク
        /// </summary>
        private async Task ReceiveLoop(CancellationToken token)
        {
            Debug.LogWarning("[NetworkManager] ReceiveLoop Task has STARTED. Now trying to open port...");

            //MOVEMENT_DATA構造体のサイズを計算
            const int MOVEMENT_DATA_SIZE = sizeof(float) * 11 + sizeof(int);


            Debug.Log("[NetworkManager] Port opened. Waiting for data... (awaiting ReceiveAsync)");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    //データ受信を非同期処理で待機
                    UdpReceiveResult result = await _udpClient.ReceiveAsync();

                    Debug.Log($"[NetworkManager] Received {result.Buffer.Length} bytes.");

                    //パース処理
                    if (result.Buffer.Length < MOVEMENT_DATA_SIZE)
                    {
                        Debug.LogWarning($"受信データサイズが不足しています:{result.Buffer.Length} bytes");
                        continue;
                    }

                    MovementData data = new MovementData();
                    int offset = 0;

                    data.TimeStep = BitConverter.ToSingle(result.Buffer, offset);
                    offset += sizeof(float);
                    data.HandlePosX = BitConverter.ToSingle(result.Buffer, offset);
                    offset += sizeof(float);
                    data.HandlePosY = BitConverter.ToSingle(result.Buffer, offset);
                    offset += sizeof(float);
                    data.HandleVelX = BitConverter.ToSingle(result.Buffer, offset);
                    offset += sizeof(float);
                    data.HandleVelY = BitConverter.ToSingle(result.Buffer, offset);
                    offset += sizeof(float);
                    data.HandleAccX = BitConverter.ToSingle(result.Buffer, offset);
                    offset += sizeof(float);
                    data.HandleAccY = BitConverter.ToSingle(result.Buffer, offset);
                    offset += sizeof(float);
                    data.TargetStartPosX = BitConverter.ToSingle(result.Buffer, offset);
                    offset += sizeof(float);
                    data.TargetStartPosY = BitConverter.ToSingle(result.Buffer, offset);
                    offset += sizeof(float);
                    data.TargetEndPosX = BitConverter.ToSingle(result.Buffer, offset);
                    offset += sizeof(float);
                    data.TargetEndPosY = BitConverter.ToSingle(result.Buffer, offset);
                    offset += sizeof(float);
                    data.IsTrialFinished = BitConverter.ToInt32(result.Buffer, offset);


                    Debug.Log($"[NetworkManager] Parsed IsTrialFinished flag: {data.IsTrialFinished}");

                    OnDataReceived?.Invoke(data);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"UDP受信ループエラー: {ex.Message}");
                }
            }
        }
    }
}