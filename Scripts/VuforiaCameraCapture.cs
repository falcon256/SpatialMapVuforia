using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.WSA.WebCam;
using UnityEngine.XR.WSA.Input;
using UnityEngine.UI;
using Vuforia;

public class VuforiaCameraCapture : MonoBehaviour
{
    // Singleton 
    public static VuforiaCameraCapture S = null;

    //Vuforia Variables
    private Vuforia.Image.PIXEL_FORMAT mPixelFormat = Vuforia.Image.PIXEL_FORMAT.UNKNOWN_FORMAT;

    //private bool mAccessCameraImage = true;
    private bool mFormatRegistered = false;
    private bool mAccessCameraImage = true;

    //test variable
    public float lastCaptureTime = 0f;

    public UnityEngine.UI.Text outTextGO = null;

    //old version variables

    // GameObjects where images and text are displayed 
    [Header("Image Display Objects")]
    public RawImage m_RawImageSmall;
    public RawImage m_RawImageBig;
    public Text m_sendTextSmall;
    public Text m_sendTextBig;

    // Photo Capture objects 
    GameObject m_Canvas = null;
    Renderer m_CanvasRenderer = null;
    PhotoCapture m_PhotoCaptureObj;
    CameraParameters m_CameraParameters;
    bool m_CapturingPhoto = false;
    Texture2D m_Texture = null;



    // Start is called before the first frame update
    void Start()
    {
        if(S!=null)
        {
            Debug.LogError("Vuforia Camera Capture Singleton attempted to make duplicate (Static reference not null)");
        }
        else
        {
            S = this;
            mPixelFormat = Vuforia.Image.PIXEL_FORMAT.RGB888;
            lastCaptureTime = Time.realtimeSinceStartup;
            Vuforia.VuforiaARController.Instance.RegisterVuforiaStartedCallback(OnVuforiaStarted);
            Vuforia.VuforiaARController.Instance.RegisterOnPauseCallback(OnPause);
        }
    }

    public void TakePhoto()
    {
        /*
        RegisterFormat();
        Vuforia.Image image = CameraDevice.Instance.GetCameraImage(mPixelFormat);

        if (image != null)
        {
            Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.LoadRawTextureData(image.Pixels);
            tex.Apply();
            //tex.LoadImage(image.Pixels);
            m_Texture = tex;
        }
        */

        if (mFormatRegistered)
        {
            if (mAccessCameraImage)
            {
                Vuforia.Image image = CameraDevice.Instance.GetCameraImage(mPixelFormat);
                if (image != null && image.IsValid())
                {
                    string imageInfo = mPixelFormat + " image: \n";
                    imageInfo += " size: " + image.Width + " x " + image.Height + "\n";
                    imageInfo += " bufferSize: " + image.BufferWidth + " x " + image.BufferHeight + "\n";
                    imageInfo += " stride: " + image.Stride;
                    Debug.Log(imageInfo);
                    byte[] pixels = image.Pixels;

                    if (pixels != null && pixels.Length > 0)
                    {
                        Debug.Log("Image pixels: " + pixels[0] + "," + pixels[1] + "," + pixels[2] + ",...");
                        Texture2D tex = new Texture2D(image.BufferWidth, image.BufferHeight, TextureFormat.RGB24, false); // RGB24
                        tex.LoadRawTextureData(pixels);
                        tex.Apply();
                        m_Texture = tex;
                        m_RawImageBig.texture = tex;
                        m_RawImageBig.material.mainTexture = tex;
                        QRCodeChecker qr = QRCodeChecker.getSingleton();
                        Debug.Log(qr.findQRCodeInImage(m_Texture));
                    }
                }
            }
        }
    }

    
    void FixedUpdate()
    {
        if(lastCaptureTime+10.0f<Time.realtimeSinceStartup)
        {
            TrackerManager.Instance.GetTracker<ObjectTracker>().Stop();
            CameraDevice.Instance.Stop();
            lastCaptureTime = Time.realtimeSinceStartup;
            RegisterFormat();
            this.TakePhoto();
            TrackerManager.Instance.GetTracker<ObjectTracker>().Start();
            CameraDevice.Instance.Start();
#if !UNITY_EDITOR
            NetworkMeshSource.getSingleton().sendImage(m_Texture,Camera.main.transform.position, Camera.main.transform.rotation);
#endif

            QRCodeChecker qr = QRCodeChecker.getSingleton();
            string o = qr.findQRCodeInImage(m_Texture);
            Debug.Log(o);
            if (outTextGO != null)
            {
                outTextGO.text = o;
            }
            //m_RawImageBig.texture = m_Texture;
            //m_RawImageBig.mainTexture = m_Texture;
            //m_RawImageBig.SetNativeSize(); holy giant plane batman.
            //m_RawImageBig.material.SetTexture(m_Texture);
            //m_RawImageBig.material.mainTexture = m_Texture;
        }

    }

    public static VuforiaCameraCapture getSingleton()
    {
        return S;
    }

    private void UnregisterFormat()
    {
        Debug.Log("Unregistering camera pixel format " + mPixelFormat.ToString());
        CameraDevice.Instance.SetFrameFormat(mPixelFormat, false);
        mFormatRegistered = false;
    }
    /// <summary>
    /// Register the camera pixel format
    /// </summary>
    private void RegisterFormat()
    {
        if (CameraDevice.Instance.SetFrameFormat(mPixelFormat, true))
        {
            Debug.Log("Successfully registered camera pixel format " + mPixelFormat.ToString());
            mFormatRegistered = true;
        }
        else
        {
            Debug.LogError("Failed to register camera pixel format " + mPixelFormat.ToString());
            mFormatRegistered = false;
        }
    }

    private void OnVuforiaStarted()
    {
        // Try register camera image format
        if (CameraDevice.Instance.SetFrameFormat(mPixelFormat, true))
        {
            Debug.Log("Successfully registered pixel format " + mPixelFormat.ToString());
            mFormatRegistered = true;
        }
        else
        {
            Debug.LogError("Failed to register pixel format " + mPixelFormat.ToString() +
                "\n the format may be unsupported by your device;" +
                "\n consider using a different pixel format.");
            mFormatRegistered = false;
        }
    }

    private void OnPause(bool paused)
    {
        if (paused)
        {
            Debug.Log("App was paused");
            UnregisterFormat();
        }
        else
        {
            Debug.Log("App was resumed");
            RegisterFormat();
        }
    }
}
