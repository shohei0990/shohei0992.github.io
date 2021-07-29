using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityCoreBluetoothFramework;
#if UNITY_EDITOR_OSX || UNITY_IOS

public class SampleUser : MonoBehaviour
{

    public Text text;
    public Text text_acc_x0;
    public Text text_acc_y0;
    public Text text_acc_z0;
    public Text text_gyro_x0;
    public Text text_gyro_y0;
    public Text text_gyro_z0;

    public delegate void SerialDataReceivedEventSampleUser(byte[] message); //デリゲートの宣言
    private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
    private bool isNewMessageReceived_ = false; //シリアルポートから値が送られてきているかどうか

    float limit, second;
    float fz_angle;
    float lend1_x;
    float lend1_y;
    float dta_x, dta_y, dta_d, dta_a, dta_audio = 0, dta_audio2 = 0;
    private int i, a, b, c, d, fx_i, fy_i, fz_i;
    private int sh = 1, bb = 1, type = 1;
    private int cal_tri, cal_tri_y = 0, cal_p = 0;
    private int ax_i, ay_i, az_i;
    private int ax_s, ay_s, az_s;
    private int acc_x, acc_y, acc_z, acc_s_x, acc_s_y, acc_s_z;
    private int g_x, g_y, g_z, g_s_x, g_s_y, g_s_z;
    private int n;
    private float ax, ay, az;
    private float fx, fy, fz, g_sf_x, g_sf_y, g_sf_z;

    string text_a, text_acc_x, text_acc_y, text_acc_z, text_gyro_x, text_gyro_y, text_gyro_z;

    public GameObject score_object = null; // Textオブジェクト
    public GameObject ble_color;
    public GameObject sphere_color;
    public Transform sphere_trans;
    public Transform cube_trans;



    private bool flag = false;
    private byte[] value = new byte[20];


    private StreamWriter sw;

    // 他のScriptからの受け取り

    private string _savePath;


    // Use this for initialization
    void Start()
    {
        ble_color.GetComponent<Renderer>().material.color = Color.grey;
        Application.targetFrameRate = 60;
        UnityCoreBluetooth.CreateSharedInstance();

        UnityCoreBluetooth.Shared.OnUpdateState((string state) =>
        {
            Debug.Log("state: " + state);
            if (state != "poweredOn") return;
            UnityCoreBluetooth.Shared.StartScan();
        });

        UnityCoreBluetooth.Shared.OnDiscoverPeripheral((UnityCBPeripheral peripheral) =>
        {
            if (peripheral.name != "")
                Debug.Log("discover peripheral name: " + peripheral.name);
            if (peripheral.name != "ESP32") return;// 

            UnityCoreBluetooth.Shared.StopScan();
            UnityCoreBluetooth.Shared.Connect(peripheral);
        });

        UnityCoreBluetooth.Shared.OnConnectPeripheral((UnityCBPeripheral peripheral) =>
        {
            Debug.Log("connected peripheral name: " + peripheral.name);
            ble_color.GetComponent<Renderer>().material.color = Color.yellow;
            peripheral.discoverServices();
        });

        UnityCoreBluetooth.Shared.OnDiscoverService((UnityCBService service) =>
        {
            Debug.Log("discover service uuid: " + service.uuid);
            if (service.uuid != "00002201-0000-1000-8000-00805F9B34FB") return; 
            service.discoverCharacteristics();
        });


        UnityCoreBluetooth.Shared.OnDiscoverCharacteristic((UnityCBCharacteristic characteristic) =>
        {
            string uuid = characteristic.uuid;
            string usage = characteristic.propertis[0];

            Debug.Log("discover characteristic uuid: " + uuid + ", usage: " + usage);
            ble_color.GetComponent<Renderer>().material.color = Color.green;
            if (usage != "read,write,notify,indicate") return;
            characteristic.setNotifyValue(true);
        });

        UnityCoreBluetooth.Shared.OnUpdateValue((UnityCBCharacteristic characteristic, byte[] data) =>
        {
            ble_color.GetComponent<Renderer>().material.color = Color.red;
            this.value = data;
            this.flag = true;
            isNewMessageReceived_ = true;
        });

        UnityCoreBluetooth.Shared.StartCoreBluetooth();
        //m_AudioSource = GetComponent<AudioSource>();

        // csvへの保存
        // iCloudバックアップ不要設定
        // iOS   : /var/mobile/Containers/Data/Application/<guid>/Documents/Product名/hoge/
        // MacOS : /Users/user名/Library/Application Support/DefaultCompany/Product名/hoge/
        UnityEngine.iOS.Device.SetNoBackupFlag(Application.persistentDataPath);

        _savePath = Application.persistentDataPath;
        Debug.Log(_savePath);
        Directory.CreateDirectory(_savePath);

        sw = new StreamWriter(_savePath + "/SaveData.csv", true);
        //sw = new StreamWriter(_savePath + "/SaveData.csv", true, Encoding.GetEncoding("Shift_JIS"));
        //string[] s1 = { "時間[s]", "anglex[deg]", "angley[deg]", "anglez[deg]", "acc_x[G]", "acc_y[G]", "acc_z[G]", "g_x[deg/s]", "g_y[deg/s]", "g_z[deg/s]" };
        //string s2 = string.Join(",", s1);
        //sw.WriteLine(s2);
    }


    // Update is called once per frame
    void Update()
    {
        stopwatch.Start();

        // 角度
        ax_i = (value[0] & 0xff) | ((value[1] << 8) & 0xff00);
        ay_i = (value[2] & 0xff) | ((value[3] << 8) & 0xff00);
        az_i = (value[4] & 0xff) | ((value[5] << 8) & 0xff00);

        if (ax_i > 32767) { ax_s = ax_i - 65535; } else { ax_s = ax_i; }
        if (ay_i > 32767) { ay_s = ay_i - 65535; } else { ay_s = ay_i; }
        if (az_i > 32767) { az_s = az_i - 65535; } else { az_s = az_i; }

        ax = ax_s; fx = ax_i / 10; // Debug.Log(fx);
        ay = ay_s; fy = ay_i / 10; // Debug.Log(fy);
        az = az_s; fz = az_i / 10; // Debug.Log(fz);

        Debug.Log(fx);
        Debug.Log(fy);
        Debug.Log(fz);


        // 加速度
        acc_x = (value[6] & 0xff) | ((value[7] << 8) & 0xff00);
        acc_y = (value[8] & 0xff) | ((value[9] << 8) & 0xff00);
        acc_z = (value[10] & 0xff) | ((value[11] << 8) & 0xff00);

        if (acc_x > 32767) { acc_s_x = acc_x - 65535; } else { acc_s_x = acc_x; }
        if (acc_y > 32767) { acc_s_y = acc_y - 65535; } else { acc_s_y = acc_y; }
        if (acc_z > 32767) { acc_s_z = acc_z - 65535; } else { acc_s_z = acc_z; }

        text_acc_x0.text = fx.ToString();
        text_acc_y0.text = fy.ToString();
        text_acc_z0.text = fz.ToString();

        // 角速度
        g_x = (value[12] & 0xff) | ((value[13] << 8) & 0xff00);
        g_y = (value[14] & 0xff) | ((value[15] << 8) & 0xff00);
        g_z = (value[16] & 0xff) | ((value[17] << 8) & 0xff00);

        if (g_x > 32767) { g_s_x = g_x - 65535; } else { g_s_x = g_x; }
        if (g_y > 32767) { g_s_y = g_y - 65535; } else { g_s_y = g_y; }
        if (g_z > 32767) { g_s_z = g_z - 65535; } else { g_s_z = g_z; }

        text_gyro_x0.text = fx_i.ToString();
        text_gyro_y0.text = fy_i.ToString();
        text_gyro_z0.text = fz_i.ToString();


        // csv書き込み
        string[] s1 = { Time.time.ToString(), fx_i.ToString(), fy_i.ToString(), fz_i.ToString(), acc_s_x.ToString(), acc_s_y.ToString(), acc_s_z.ToString(), g_s_x.ToString(), g_s_y.ToString(), g_s_z.ToString() };
        string s2 = string.Join(",", s1);
        sw.WriteLine(s2);
        //sw.Flush();
        //sw.Close();

        // Textに数字を表示
        Text score_text = score_object.GetComponent<Text>();
        text_a = dta_audio.ToString("0.0");
        score_text.text = text_a;
        //stopwatchクラス取得

        //処理メソッド

        stopwatch.Stop();
    }

    void OnDestroy()
    {
        UnityCoreBluetooth.ReleaseSharedInstance();
    }

}
#endif