using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class UDP : MonoBehaviour {
    /***********************************************/
    #region UDP関連パラメータ
    public string hostIP = "133.10.79.170";
    public int hostPort = 61000;
    static private UdpClient client;
    private string controllerIPv4;

    /*
    private string receivableIP = "";
    private int detaMode;
    */
    static private int[] sendParameter;
    static private string sendMessage;

    #endregion
    /**********************************************/

    Thread thread;

    private GameObject[] Output;
    private InputField[] inputField;
    private Slider[] output;
    [SerializeField]
    private Slider[] Switch;
    private Toggle Test;
    private Canvas Width;
    private Canvas Height;
    private Canvas Info;
    private Text ControllerName;
    private Text ControllerIPv4;
    public float widthOfPulseMax = 1000;//(us)
    public float frequencyMax;//(us)
    public float voltMax=4095;
    public bool test=false;

    // Use this for initialization
    void Start () {
        /////////////UDP接続//////////////////
        client = new UdpClient();
        client.Connect(hostIP, hostPort);
        string controllerName = Dns.GetHostName();//コントローラの名前を取得
        //IPAddress[] controllerIP = Dns.GetHostAddresses(controllerName);//コントローラのアドレス群を取得
        IPHostEntry entry = Dns.GetHostEntry(controllerName);

        foreach(IPAddress address in entry.AddressList)//IPv4のみを取得しcontrollerIPに格納
        {
            //Debug.Log(address);
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                controllerIPv4 = address.ToString();
                Debug.Log(controllerName);
                Debug.Log(controllerIPv4);
            }
            
        }


        /******************************************/
        #region マルチスレッド

        thread = new Thread(new ThreadStart(ThreadMethod));
        //thread.Start();

        #endregion
        /******************************************/

        Output = GameObject.FindGameObjectsWithTag("Slider");//とりあえずSliderタグを格納
        output = new Slider[Output.Length];
        Switch = new Slider[Output.Length];
        sendParameter = new int[Output.Length];
        inputField = new InputField[Output.Length];
        sendMessage = "Controller" + ",";//送信メッセージの先頭はArduinoのIP
        for(int i=0; i< Output.Length; i++)
        {
            if (i < Output.Length - 2)//width/FrequencyはSwitchを持たないため
            {
                Switch[i] = GameObject.Find("Switch" + i).GetComponent<Slider>();
            }
            Output[i] = GameObject.Find("Output" + i);
            output[i]=Output[i].GetComponent<Slider>();
            output[i].maxValue = voltMax;
            inputField[i] = GameObject.Find("InputField" + i).GetComponent<InputField>();
        }
        output[8].maxValue = widthOfPulseMax;
        frequencyMax = 1000000 / (2 * output[8].value);//最大周波数を計算
        output[9].maxValue = frequencyMax;



        //<Canvas>"width","Height"の取得
        Width = GameObject.Find("Width").GetComponent<Canvas>();
        Height = GameObject.Find("Height").GetComponent<Canvas>();
        Info = GameObject.Find("Info").GetComponent<Canvas>();
        ControllerName = GameObject.Find("ControllerName").GetComponent<Text>();
        ControllerIPv4 = GameObject.Find("ControllerIPv4").GetComponent<Text>();
        Test = GameObject.Find("Test").GetComponent<Toggle>();

        ControllerName.text = controllerName;
        ControllerIPv4.text = controllerIPv4;
        //初期画面はWidthから
        Width.enabled = true;
        Height.enabled = false;
        Info.enabled = false;

        /*****************プログラム開始直後に１回UDP送信************************/
        SliderUpdate();
        /*********************************************************************/
    }

    // Update is called once per frame
    void Update () {

    }

    public void InputFieldUpdate()////InputField情報の更新（InputFieldが変更されたときに呼ばれる）
    {
        //detaMode = 1;
        for (int i = 0; i < output.Length; i++)
        {
            output[i].value =float.Parse(inputField[i].text);//InputFiekdの値をSliderに代入
            if (i == output.Length - 2)//widthの値の操作のとき周波数の値の最大値を変更
            {
                sendParameter[i] = Mathf.RoundToInt(output[i].value);//送信する値をsendParameterに格納
                frequencyMax = 1000000 / (2 * sendParameter[i]);//最大周波数を計算
                output[i + 1].maxValue = frequencyMax;//最大周波数の更新
            }
            else if(i == output.Length - 1)//Frequencyの値の操作のとき周波数をパルス幅に変換
            {
                if (output[i].value > frequencyMax)//送信する周波数が最大値を超えていたら
                {
                    output[i].value = frequencyMax;//最大周波数を送信する
                    inputField[i].text = frequencyMax.ToString();//InputField内を書き換える
                }
                sendParameter[i] = Mathf.RoundToInt((1 / output[i].value) * 1000000 - output[i - 1].value);//パルス間隔＝(1/周波数)×1000000-パルス幅
                
            }
            else sendParameter[i] = Mathf.RoundToInt(output[i].value);

        }
        UDPsend();
    }

    public void SliderUpdate()////スライダー情報の更新（スライダを動かすときにも呼ばれる）
    {
        //detaMode = 1;
        for (int i = 0; i < output.Length; i++)
        {
            if (i == output.Length - 2)//widthの値の操作のとき周波数の値の最大値を変更
            {
                sendParameter[i] = Mathf.RoundToInt(output[i].value);//送信する値をsendParameterに格納
                frequencyMax = 1000000 / (2 * sendParameter[i]);//最大周波数を計算
                output[i + 1].maxValue = frequencyMax;//最大周波数の更新
            }
            else if (i == output.Length - 1)//Frequencyの値の操作のとき周波数をパルス幅に変換
            {
                if (output[i].value > frequencyMax)//送信する周波数が最大値を超えていたら
                {
                    output[i].value = frequencyMax;//最大周波数を送信する
                }
                sendParameter[i] = Mathf.RoundToInt((1 / output[i].value) * 1000000 - output[i - 1].value);//パルス間隔＝(1/周波数)×1000000-パルス幅
            }
            else
            {
                sendParameter[i] = Mathf.RoundToInt(output[i].value);
            }
            inputField[i].text = output[i].value.ToString() ; //Sliderの値をInputFieldに代入
        }
        UDPsend();
    }

    public void TestMode()
    {
        if (Test.isOn)//TestがOnのときArduinoに"2"を送信
        {
            byte[] dgram = Encoding.UTF8.GetBytes("mode,1\0");
            client.Send(dgram, dgram.Length);
            Debug.Log("On");
        }
        else//TestがOffのときArduinoに"0"を送信
        {
            byte[] dgram = Encoding.UTF8.GetBytes("mode,0\0");
            client.Send(dgram, dgram.Length);
            Debug.Log("Off");
        }
        

    }

    public void ModeChange()//HeightとWidthの変更（Settingボタンが押されたときに呼ばれる）
    {
        if (Width.enabled)
        {
            Width.enabled = false;
            Height.enabled = true;
            Info.enabled = false;
        }
        else if(Height.enabled)
        {
            Width.enabled = true;
            Height.enabled = false;
            Info.enabled = false;
        }
        else
        {
            Width.enabled = true;
            Height.enabled = false;
            Info.enabled = false;
        }
    }

    public void InfoMode()
    {
        Width.enabled = false;
        Height.enabled = false;
        Info.enabled = true;
    }

    public void Initialize()//UDP接続の初期化・パラメータの初期化（Initializeボタンが押されたときに呼ばれる）
    {
        //パラメータをすべて初期値に戻す
        for (int i = 0; i < output.Length; i++)
        {
            output[i].value = 0;
        }
        SliderUpdate();
    }

    public void Quit()
    {
        Initialize();
        client.Close();//UDP接続終了
        //thread.Abort();//スレッド終了
        Application.Quit();
        
    }

    public void UDPsend()
    {
        for (int i = 0; i < Output.Length - 1; i++)
        {
            if (i < Output.Length - 2)
            {
                sendMessage += (sendParameter[i] * Switch[i].value).ToString() + ",";//SwitchがOnのときパラメータはそのまま送信/offのときパラメータに0をかけて送信
            }else sendMessage += sendParameter[i].ToString() + ",";

        }
        sendMessage += sendParameter[Output.Length - 1].ToString() + "\0";
        Debug.Log(sendMessage);
        byte[] dgram = Encoding.UTF8.GetBytes(sendMessage);
        client.Send(dgram, dgram.Length);
        sendMessage = "Controller" + ",";//送信メッセージの先頭はArduinoのIP
    }

    void OnApplicationQuit()
    {
        Quit();
    }
    
    private static void ThreadMethod()
    {
        while (true)
        {
            
        }
    }
}
