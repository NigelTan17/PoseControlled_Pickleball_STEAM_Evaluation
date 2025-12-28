using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace PB.Scripts
{
    public class UDPReceive : MonoBehaviour
    {
        Thread _thread;
        UdpClient _client;

        [Header("UDP")]
        public int port = 5052;                // Use 5052 for Right, 5051 for Left
        public bool startReceiving = true;
        public bool printToConsole = false;

        // latest packet as raw CSV string (thread-safe enough for our usage)
        public volatile string data = string.Empty;

        void Start()
        {
            _thread = new Thread(ReceiveLoop) { IsBackground = true };
            _thread.Start();
        }

        void OnDestroy()
        {
            try
            {
                startReceiving = false;
                _client?.Close();
                _thread?.Join(100);
            }
            catch { }
        }

        void ReceiveLoop()
        {
            try
            {
                _client = new UdpClient(port);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UDPReceive] Failed to bind port {port}: {e.Message}");
                return;
            }

            while (startReceiving)
            {
                try
                {
                    IPEndPoint any = new IPEndPoint(IPAddress.Any, 0);
                    byte[] bytes = _client.Receive(ref any);
                    string raw = Encoding.UTF8.GetString(bytes);
                    data = raw;                          // <-- keep RAW, no flipping here
                    if (printToConsole) Debug.Log($"[UDP {port}] {raw}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[UDPReceive] {e.Message}");
                }
            }
        }
    }
}
