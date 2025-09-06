using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class TCPIPServerAsync : MonoBehaviour
{
    // Use this for initialization

    System.Threading.Thread SocketThread;
    volatile bool keepReading = false;

    [SerializeField] GameObject cube;
    private readonly object _lockObj = new object();
    private Vector3 nextPos;

    private bool posChanged = false;

    [SerializeField] float posLerpSpeed = 1f;

    const float EPS_POS = 0.001f;

    private Renderer _cubeRenderer;

    void Start()
    {
        Application.runInBackground = true;
        _cubeRenderer = cube.GetComponent<Renderer>();
        startServer();
    }

    void startServer()
    {
        SocketThread = new System.Threading.Thread(networkCode);
        SocketThread.IsBackground = true;
        SocketThread.Start();
    }


    Socket listener;
    Socket handler;

    private void ParsePayload(string payload)
    {
        var parts = payload.Split(',');
        float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[1], CultureInfo.InvariantCulture);

        nextPos = new Vector3(x, 0, y);
        posChanged = true;
    }

    void networkCode()
    {
        string data;

        // Data buffer for incoming data.
        byte[] bytes = new Byte[1024];

        // host running the application.
        //Create EndPoint
        IPAddress IPAdr = IPAddress.Parse("127.0.0.1"); // Dirección IP
        IPEndPoint localEndPoint = new IPEndPoint(IPAdr, 1101);

        // Create a TCP/IP socket.
        listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and 
        // listen for incoming connections.

        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(10);

            // Start listening for connections.
            while (true)
            {
                keepReading = true;

                // Program is suspended while waiting for an incoming connection.
                Debug.Log("Waiting for Connection");     //It works

                handler = listener.Accept();
                Debug.Log("Client Connected");     //It doesn't work
                data = null;

                byte[] SendBytes = System.Text.Encoding.Default.GetBytes("I will send key");
                handler.Send(SendBytes); // dar al cliente



                // An incoming connection needs to be processed.
                while (keepReading)
                {
                    bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);

                    if (bytesRec <= 0)
                    {
                        keepReading = false;
                        handler.Disconnect(true);
                        break;
                    }

                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    Debug.Log("Received from Server: " + data);

                    ParsePayload(data);


                    if (data.IndexOf("<EOF>") > -1)
                    {
                        break;
                    }

                    System.Threading.Thread.Sleep(1);
                }

                System.Threading.Thread.Sleep(1);
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    void stopServer()
    {
        keepReading = false;

        //stop thread
        if (SocketThread != null)
        {
            SocketThread.Abort();
        }

        if (handler != null && handler.Connected)
        {
            handler.Disconnect(false);
            Debug.Log("Disconnected!");
        }
    }

    void OnDisable()
    {
        stopServer();
    }

    void Update()
    {
        Vector3 targetPos;
        bool hasPos;

        lock (_lockObj)
        {
            targetPos = nextPos;

            hasPos = posChanged;
        }

        float dt = Time.deltaTime;
        float tPos = 1f - Mathf.Exp(-posLerpSpeed * dt);

        if (hasPos)
        {
            var cur = cube.transform.position;
            var nxt = Vector3.Lerp(cur, targetPos, tPos);
            cube.transform.position = nxt;

            if ((nxt - targetPos).sqrMagnitude <= EPS_POS * EPS_POS)
            {
                cube.transform.position = targetPos;
                lock (_lockObj) { posChanged = false; }
            }
        }
    }



}
