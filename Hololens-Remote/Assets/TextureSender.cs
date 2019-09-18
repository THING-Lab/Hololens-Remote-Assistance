﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.XR.WSA.WebCam;

public class TextureSender : MonoBehaviour
{
    WebCamTexture webcam;
    Texture2D tex;
    public int width = 640;
    public int height = 480;
    public int fps = 15;
    float frameTime = 0f;
    float FRAME_MAX = 0.2f;
    bool stop = false;
    public string ip = "localhost";
    public int port = 4444;
    public int messageByteLength = 24;
    Thread clientReceiveThread;
    Thread texSendThread;
    UdpClient client;
    NetworkStream stream = null;
    bool isConnected = false;

    void Start() {
        // Start is called before the first frame update
        WebCamDevice[] devices = WebCamTexture.devices;
        // for debugging purposes, prints available devices to the console
        for (int i = 0; i < devices.Length; i++)
        {
            print("Webcam available: " + devices[i].name);
        }

        // assuming the first available WebCam is desired
        webcam = new WebCamTexture(devices[0].name, width, height, fps);
        webcam.Play();
        tex = new Texture2D(width, height);
        // Start sending coroutine
        // Also initializes server connection
        StartCoroutine(SenderCOR());
    }

    void ConnectToServer() {
        Debug.Log("Connect");
        // Connect to the server
        try {
            clientReceiveThread = new Thread(new ThreadStart(() => {
                client = new UdpClient();
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ip), port);
                client.Connect(remoteEP);
                // Listen for server messages here
                //stream = client.GetStream();
                Debug.Log("Connected");
            }));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
        }
        catch (Exception e) {
            Debug.Log("On client connect exception " + e);
        }
    }

    void byteLengthToFrameByteArray(int byteLength, byte[] fullBytes) {
        //Clear old data
        Array.Clear(fullBytes, 0, fullBytes.Length);
        //Convert int to bytes
        byte[] bytesToSendCount = BitConverter.GetBytes(byteLength);
        //Copy result to fullBytes
        bytesToSendCount.CopyTo(fullBytes, 0);
    }

    bool readyToGetFrame = false;
    IEnumerator SenderCOR() {
        byte[] frameBytesLength = new byte[messageByteLength];
        WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
        ConnectToServer();

        while (!stop) {
            //Wait for End of frame
            yield return endOfFrame;
            byte[] imageBytes = tex.EncodeToJPG();

            //Fill total byte length to send. Result is stored in frameBytesLength
            byteLengthToFrameByteArray(imageBytes.Length, frameBytesLength);
            if (stream != null) {
                texSendThread = new Thread(new ThreadStart(() => {
                    //Send total byte count first
                    stream.Write(frameBytesLength, 0, frameBytesLength.Length);
                    //Send the image bytes
                    stream.Write(imageBytes, 0, imageBytes.Length);
                    //Sent. Set readyToGetFrame true
                    readyToGetFrame = false;
                }));
                texSendThread.IsBackground = true;
                texSendThread.Start();
            } else {
                readyToGetFrame = false;
            }
            // Wait until we are ready to get new frame(Until we are done sending data)
            while (!readyToGetFrame) {
                yield return null;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        frameTime += Time.deltaTime;
        if (frameTime > FRAME_MAX) {
            // if (!takingPhoto) TakePhoto();
            tex.SetPixels(webcam.GetPixels());
            tex.Apply();
            frameTime = 0;
            readyToGetFrame = true;
        }
    }
}
