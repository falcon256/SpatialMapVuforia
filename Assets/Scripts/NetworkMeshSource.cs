using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.WebCam;
#endif

public class NetworkMeshSource : MonoBehaviour
{

    private static NetworkMeshSource networkMeshSourceSingleton = null;
    public static NetworkMeshSource getSingleton() { return networkMeshSourceSingleton; }
    public int serverPort = 32123;
    public int targetPort = 32123;
    public string targetIP = "";
    public bool targetIPReady = false;
    public bool socketStarted = false;
    public volatile bool connected = false;
    private ConcurrentQueue<messagePackage> outgoingQueue = null;
    private byte[] incomingBuffer = null;
    private Stack<LineRenderer> lineRenderers = null;
    private ConcurrentQueue<lrStruct> incomingLineRenderers = null;
    private bool undoLineRenderer = false;
    public Material LineRendererDefaultMaterial = null;
    private bool startConnect = false;
    private int lengthOfNextPacket = -1;
#if !UNITY_EDITOR
 public StreamSocket tcpClient = null;
    public Windows.Storage.Streams.IOutputStream outputStream = null;
    public Windows.Storage.Streams.IInputStream inputStream = null;
    DataWriter writer = null;
    DataReader reader = null;
    //udp broadcast listening
    DatagramSocket listenerSocket = null;
    const string udpPort = "32124";

    StreamSocketListener _streamSocket = null;



#endif

    private UnityEngine.XR.WSA.WebCam.PhotoCapture photoCaptureObject = null;
    private Texture2D targetTexture = null;
    private Vector3 textureLocation = Vector3.zero;
    private Quaternion textureRotation = Quaternion.identity;

    private Vector3 cameraStartPosition = Vector3.zero;
    private Vector3 cameraEndPosition = Vector3.zero;
    private Quaternion cameraStartRotation = Quaternion.identity;
    private Quaternion cameraEndRotation = Quaternion.identity;



    private struct lrStruct
    {
        public float r, g, b, a, pc, sw, ew;
        public Vector3[] verts;
    }

    private class messagePackage
    {
        public byte[] bytes = null;
        public messagePackage(byte[] b) { bytes = b; }
    }


    //public Mesh testMesh = null;
    // Start is called before the first frame update
    void Start()
    {
        if (networkMeshSourceSingleton != null)
        {
            Destroy(this);
            return;
        }

        lineRenderers = new Stack<LineRenderer>();
        incomingLineRenderers = new ConcurrentQueue<lrStruct>();
        outgoingQueue = new ConcurrentQueue<messagePackage>();
        networkMeshSourceSingleton = this;
#if !UNITY_EDITOR
        Listen();
#endif
    }
    /*
#if !UNITY_EDITOR
    public async Task HandleSocket()
        {

        if(!socketStarted&&targetIPReady&&!connected&&!startConnect&&tcpClient==null)
        {
            tcpClient = new Windows.Networking.Sockets.StreamSocket();
            tcpClient.Control.NoDelay = false;
            tcpClient.Control.KeepAlive = false;
            tcpClient.Control.OutboundBufferSizeInBytes = 1500;
            startConnect=true;
            await tcpClient.ConnectAsync(new HostName(targetIP), "" + targetPort);          
            outputStream = tcpClient.OutputStream;
            inputStream = tcpClient.InputStream;
            writer = new DataWriter(outputStream);
            reader = new DataReader(inputStream);
            reader.InputStreamOptions = InputStreamOptions.Partial;
            reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            reader.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;
            connected = true;
        }

        if(connected&&reader.UnconsumedBufferLength>4)
        {
            lengthOfNextPacket=reader.ReadInt32();
        }
        if(lengthOfNextPacket>0&&reader.UnconsumedBufferLength>lengthOfNextPacket)
        {
            int packetType = reader.ReadInt32();
            float r = reader.ReadSingle();
            float g = reader.ReadSingle();
            float b = reader.ReadSingle();
            float a = reader.ReadSingle();
            int count = reader.ReadInt32();// this is actually just for padding...
            float sw = reader.ReadSingle();
            float ew = reader.ReadSingle();
            byte[] packet = new byte[lengthOfNextPacket-32];
            reader.ReadBytes(packet);
            if(packetType==4&&packet.Length>0)
            {
                lrStruct l = new lrStruct();
                l.r = r;
                l.g = g;
                l.b = b;
                l.a = a;
                l.pc = count;
                l.sw = sw;
                l.ew = ew;
                l.verts = new Vector3[count];
                for(int i = 0; i < count; i++)//Dan actually wrote this one from scratch, so might be bugged.
                {                 
                    l.verts[i]=new Vector3(BitConverter.ToSingle(packet, i*12+0), BitConverter.ToSingle(packet, i * 12 + 4),
                        BitConverter.ToSingle(packet, i * 12 + 8));
                }
                incomingLineRenderers.Enqueue(l);
            }
            if (packetType == 5)
                undoLineRenderer = true;
            lengthOfNextPacket=-1;
        }





}
#endif
*/





#if !UNITY_EDITOR


    private async void _streamSocket_ConnectionReceived(StreamSocketListener sender,
        StreamSocketListenerConnectionReceivedEventArgs args)
{
    using (IInputStream inStream = args.Socket.InputStream)
    {
        reader = new DataReader(inStream);
        reader.InputStreamOptions = InputStreamOptions.Partial;
        outputStream = args.Socket.OutputStream;
        inputStream = inStream;
        writer = new DataWriter(outputStream);
        connected = true;

        while (connected)
                {
                    await reader.LoadAsync(8192);
                    if(reader.UnconsumedBufferLength>4)
                    {
        
                        int incomingSize = reader.ReadInt32();
                        if(incomingSize>0&&incomingSize < 100000)
                        {
                            await reader.LoadAsync((uint)incomingSize);//preloads the buffer with the data which makes the following not needed.
                            
                            
                            
                            int packetType = reader.ReadInt32();
                            float r = reader.ReadSingle();
                            float g = reader.ReadSingle();
                            float b = reader.ReadSingle();
                            float a = reader.ReadSingle();
                            int count = reader.ReadInt32();// this is actually just for padding...
                            float sw = reader.ReadSingle();
                            float ew = reader.ReadSingle();
                            byte[] packet = new byte[incomingSize-32];
                            reader.ReadBytes(packet);
                            if(packetType==4&&packet.Length>0)
                            {
                                lrStruct l = new lrStruct();
                                l.r = r;
                                l.g = g;
                                l.b = b;
                                l.a = a;
                                l.pc = count;
                                l.sw = sw;
                                l.ew = ew;
                                l.verts = new Vector3[count];
                                for(int i = 0; i < count; i++)//Dan actually wrote this one from scratch, so might be bugged.
                                {                 
                                    l.verts[i]=new Vector3(BitConverter.ToSingle(packet, i*12+0), BitConverter.ToSingle(packet, i * 12 + 4),
                                        BitConverter.ToSingle(packet, i * 12 + 8));
                                }
                                incomingLineRenderers.Enqueue(l);
                            }
                            if (packetType == 5)
                                undoLineRenderer = true;


                        }
                        else
                        {
                            //TODO Handle it.
                        }
        
                    }
        
                }
    }

    System.Diagnostics.Debug.WriteLine("Finished reading");
}



    public async Task setupSocket()
    {


        //udpClient = new DatagramSocket();
        //udpClient.Control.DontFragment = true;
        tcpClient = new Windows.Networking.Sockets.StreamSocket();
        //tcpClient.Control.OutboundBufferSizeInBytes = 1500;
        tcpClient.Control.NoDelay = false;
        tcpClient.Control.KeepAlive = false;
        tcpClient.Control.OutboundBufferSizeInBytes = 1500;
        while(!connected)
        {
            try
            {
                //await udpClient.BindServiceNameAsync("" + targetPort);
                await tcpClient.ConnectAsync(new HostName(targetIP), "" + targetPort);
            
                outputStream = tcpClient.OutputStream;
                inputStream = tcpClient.InputStream;
                writer = new DataWriter(outputStream);
                reader = new DataReader(inputStream);
                reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                reader.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;
                reader.InputStreamOptions = InputStreamOptions.Partial;
                connected = true;
                //incomingBuffer = new byte[0];
                /*
                while (connected)
                {
                    
                    if(reader.UnconsumedBufferLength>4)
                    {
        
                        int incomingSize = reader.ReadInt32();
                        if(incomingSize>0&&incomingSize < 100000)
                        {
                            await reader.LoadAsync((uint)incomingSize);//preloads the buffer with the data which makes the following not needed.
                            
                            
                            
                            int packetType = reader.ReadInt32();
                            float r = reader.ReadSingle();
                            float g = reader.ReadSingle();
                            float b = reader.ReadSingle();
                            float a = reader.ReadSingle();
                            int count = reader.ReadInt32();// this is actually just for padding...
                            float sw = reader.ReadSingle();
                            float ew = reader.ReadSingle();
                            byte[] packet = new byte[incomingSize-32];
                            reader.ReadBytes(packet);
                            if(packetType==4&&packet.Length>0)
                            {
                                lrStruct l = new lrStruct();
                                l.r = r;
                                l.g = g;
                                l.b = b;
                                l.a = a;
                                l.pc = count;
                                l.sw = sw;
                                l.ew = ew;
                                l.verts = new Vector3[count];
                                for(int i = 0; i < count; i++)//Dan actually wrote this one from scratch, so might be bugged.
                                {                 
                                    l.verts[i]=new Vector3(BitConverter.ToSingle(packet, i*12+0), BitConverter.ToSingle(packet, i * 12 + 4),
                                        BitConverter.ToSingle(packet, i * 12 + 8));
                                }
                                incomingLineRenderers.Enqueue(l);
                            }
                            if (packetType == 5)
                                undoLineRenderer = true;


                        }
                        else
                        {
                            //TODO Handle it.
                        }
        
                    }
        
                }
                */    
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                return;
            }
        
        }
        
}
#endif
#if !UNITY_EDITOR
    private async void Listen()
    {
        listenerSocket = new DatagramSocket();
        listenerSocket.MessageReceived += udpMessageReceived;
        await listenerSocket.BindServiceNameAsync(udpPort);
    }
    
    async void udpMessageReceived(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs args)
    {
        if (!targetIPReady)
        {
            DataReader reader = args.GetDataReader();
            uint len = reader.UnconsumedBufferLength;
            string msg = reader.ReadString(len);
            string remoteHost = args.RemoteAddress.DisplayName;
            targetIP = msg;
            targetIPReady = true;
        }
    }
#endif

    /*
#if !UNITY_EDITOR
    public void captureImageData()
    {

        Resolution cameraResolution = UnityEngine.XR.WSA.WebCam.PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

        // Create a PhotoCapture object
        UnityEngine.XR.WSA.WebCam.PhotoCapture.CreateAsync(false, delegate (UnityEngine.XR.WSA.WebCam.PhotoCapture captureObject) {
            photoCaptureObject = captureObject;
            UnityEngine.XR.WSA.WebCam.CameraParameters cameraParameters = new UnityEngine.XR.WSA.WebCam.CameraParameters();
            cameraParameters.hologramOpacity = 0.0f;
            cameraParameters.cameraResolutionWidth = cameraResolution.width;
            cameraParameters.cameraResolutionHeight = cameraResolution.height;
            cameraParameters.pixelFormat = UnityEngine.XR.WSA.WebCam.CapturePixelFormat.BGRA32;


            cameraStartPosition = Camera.main.transform.position;
            cameraStartRotation = Camera.main.transform.rotation;

            // Activate the camera
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (UnityEngine.XR.WSA.WebCam.PhotoCapture.PhotoCaptureResult result) {
            // Take a picture
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);

            });
        });
    }

    void OnCapturedPhotoToMemory(UnityEngine.XR.WSA.WebCam.PhotoCapture.PhotoCaptureResult result, UnityEngine.XR.WSA.WebCam.PhotoCaptureFrame photoCaptureFrame)
    {
        // Copy the raw image data into the target texture
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        cameraEndPosition = Camera.main.transform.position;
        cameraEndRotation = Camera.main.transform.rotation;
    }

    void OnStoppedPhotoMode(UnityEngine.XR.WSA.WebCam.PhotoCapture.PhotoCaptureResult result)
    {
        // Shutdown the photo capture resource
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }
#endif*/
#if !UNITY_EDITOR
    public async void sendImage(Texture2D tex, Vector3 location, Quaternion rotation)
    {

        if (!connected)
        return;

        try
        {
        byte[] image = ImageConversion.EncodeToJPG(tex, 50);

        byte[] bytes = new byte[36]; // 4 bytes per float
        System.Buffer.BlockCopy(BitConverter.GetBytes(36 + image.Length), 0, bytes, 0, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes(3), 0, bytes, 4, 4);//type of packet
        System.Buffer.BlockCopy(BitConverter.GetBytes(location.x), 0, bytes, 8, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes(location.y), 0, bytes, 12, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes(location.z), 0, bytes, 16, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.x), 0, bytes, 20, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.y), 0, bytes, 24, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.z), 0, bytes, 28, 4);
        System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.w), 0, bytes, 32, 4);
        byte[] sendData = Combine(bytes, image);
        if(sendData.Length>0)
            enqueueOutgoing(sendData);

        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            return;
        }
    }


    public async void sendMesh(Mesh m, Vector3 location, Quaternion rotation)
    {
        if (!connected)
            return;

        
        try
        {
            //SendHeadsetLocation();
            List<Mesh> meshes = new List<Mesh>();
            meshes.Add(m);
            byte[] meshData =  SimpleMeshSerializer.Serialize(meshes);
            byte[] bytes = new byte[36]; // 4 bytes per float
            System.Buffer.BlockCopy(BitConverter.GetBytes(36 + meshData.Length), 0, bytes, 0, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(1), 0, bytes, 4, 4);//type of packet
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.x), 0, bytes, 8, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.y), 0, bytes, 12, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.z), 0, bytes, 16, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.x), 0, bytes, 20, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.y), 0, bytes, 24, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.z), 0, bytes, 28, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.w), 0, bytes, 32, 4);
            byte[] sendData = Combine(bytes, meshData);
            if(sendData.Length>0)
                enqueueOutgoing(sendData);
            SendHeadsetLocation();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            return;
        }
        //Debug.Log("Sent: " + sendData.Length + " bytes");

}
#endif
#if !UNITY_EDITOR
    public static byte[] Compress(byte[] raw)
    {
        using (MemoryStream memory = new MemoryStream())
        {
            using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
            {
                gzip.Write(raw, 0, raw.Length);
            }
            return memory.ToArray();
        }
    }

    static byte[] Decompress(byte[] gzip)
    {
        using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
        {
            const int size = 4096;
            byte[] buffer = new byte[size];
            using (MemoryStream memory = new MemoryStream())
            {
                int count = 0;
                do
                {
                    count = stream.Read(buffer, 0, size);
                    if (count > 0)
                    {
                        memory.Write(buffer, 0, count);
                    }
                }
                while (count > 0);
                return memory.ToArray();
            }
        }
    }
#endif
    // Update is called once per frame
    void Update()
    {

    }




    public async void SendHeadsetLocation()
    {
#if !UNITY_EDITOR
        if (!connected)
            return;
        try
        {
            Vector3 location = Camera.main.transform.position;
            Quaternion rotation = Camera.main.transform.rotation;           
            byte[] bytes = new byte[36]; // 4 bytes per float
            System.Buffer.BlockCopy(BitConverter.GetBytes(36), 0, bytes, 0, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(2), 0, bytes, 4, 4);//type of packet
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.x), 0, bytes, 8, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.y), 0, bytes, 12, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.z), 0, bytes, 16, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.x), 0, bytes, 20, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.y), 0, bytes, 24, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.z), 0, bytes, 28, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.w), 0, bytes, 32, 4);
            if(bytes.Length>0)
                enqueueOutgoing(bytes);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            return;
        }
#endif
    }




#if !UNITY_EDITOR
    void FixedUpdate()
    {
    //    Task t = HandleSocket();
   // while(!t.IsCompleted)
    //{
    //    
   // }
    
        if(!socketStarted&&targetIPReady)
        {
            socketStarted = true;
            setupSocket();
        }
        if(!outgoingQueue.IsEmpty)
        {
            messagePackage mp = null;
            outgoingQueue.TryDequeue(out mp);
            if(mp!=null)
            {
                sendOutgoingPacket(mp);
            }
        }
    
        if(!incomingLineRenderers.IsEmpty)
        {           
            lrStruct l = new lrStruct();
            if(incomingLineRenderers.TryDequeue(out l))
            {
                LineRenderer lr = this.gameObject.AddComponent<LineRenderer>();
                lr.material = new Material(LineRendererDefaultMaterial);//copy
                lr.material.color = new Color(l.r, l.g, l.b, l.a);
                lr.startWidth = l.sw;
                lr.endWidth = l.ew;
                lr.endColor = lr.startColor = new Color(l.r, l.g, l.b, l.a);
            }
        }
        //SendHeadsetLocation();
    }

    private async void flush()
    {         
        await writer.StoreAsync();
        //await writer.FlushAsync();
    }

    private async void sendOutgoingPacket(messagePackage sendData)
    {
        try{
        if (sendData.bytes.Length>1000000)
            {
                Debug.Log("Packet of length " + sendData.bytes.Length + " waiting to go out... But can't.. Because it is probably too huge...");
                return;
            }
            lock(outputStream)
            {
                
                writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                writer.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;
                writer.WriteBytes(sendData.bytes); 
                flush();
                Debug.Log("Sent " + sendData.bytes.Length + " bytes.");
            }
            

        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            return;
        }
    }


    private void enqueueOutgoing(byte[] bytes)
    {
        outgoingQueue.Enqueue(new messagePackage(bytes));
    }

#endif


    void OnDestroy()
    {
#if !UNITY_EDITOR
        //if (tcpClient != null)
        //{
            //tcpClient.Close();
       //     tcpClient = null;
        //}

        //if (udpClient != null)
        //{
            //udpClient.Close();
        //    udpClient = null;
        //}
#endif
    }

    //stolen useful code.
    public static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] ret = new byte[first.Length + second.Length];
        System.Buffer.BlockCopy(first, 0, ret, 0, first.Length);
        System.Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
        return ret;
    }
    public static byte[] Combine(byte[] first, byte[] second, byte[] third)
    {
        byte[] ret = new byte[first.Length + second.Length + third.Length];
        System.Buffer.BlockCopy(first, 0, ret, 0, first.Length);
        System.Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
        System.Buffer.BlockCopy(third, 0, ret, first.Length + second.Length,
                         third.Length);
        return ret;
    }


}
