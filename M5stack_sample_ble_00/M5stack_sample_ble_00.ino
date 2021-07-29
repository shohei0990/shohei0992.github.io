#include <M5Stack.h>
#include "utility/MPU9250.h"

MPU9250 IMU;

//ボールの初期位置，初速度
float posx = 160;
float posy = 120;
float velx = 0;
float vely = 0;
int radius = 15; //描画するボールの半径
int16_t   ax_i,   ay_i,   az_i;

//ループ一回分の時間とそれを計算するためのタイマーです。
unsigned int dt = 0;
unsigned int timer = millis();

// ① BLE通信
#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>

#define SERVICE_UUID        "00002201-0000-1000-8000-00805F9B34FB"
#define CHARACTERISTIC_UUID "00002256-0000-1000-8000-00805F9B34FB"

// ① BLE通信
BLEServer* pServer                 = NULL;
BLECharacteristic* pCharacteristic = NULL;
bool deviceConnected    = false;
bool oldDeviceConnected = false;
int a;
uint8_t buff[18];
uint32_t value = 0;

class MyServerCallbacks: public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) {
      deviceConnected = true;
    };

    void onDisconnect(BLEServer* pServer) {
      deviceConnected = false;
    }
};


void setup() {
  M5.begin();
  //  serial for debugging
  Serial.begin(115200);
  //  i2c as a master
  Wire.begin();
  IMU.initMPU9250();
  M5.Lcd.fillScreen(WHITE);

  // ① BLE通信
  // Create the BLE Device
  BLEDevice::init("ESP32");

  // Create the BLE Server
  pServer = BLEDevice::createServer()           ;
  pServer->setCallbacks(new MyServerCallbacks());

  // Create the BLE Service
  BLEService *pService = pServer->createService(SERVICE_UUID);

  // Create a BLE Characteristic
  pCharacteristic = pService->createCharacteristic(
                      CHARACTERISTIC_UUID,
                      BLECharacteristic::PROPERTY_READ   |
                      BLECharacteristic::PROPERTY_WRITE  |
                      BLECharacteristic::PROPERTY_NOTIFY |
                      BLECharacteristic::PROPERTY_INDICATE
                    );

  // https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.descriptor.gatt.client_characteristic_configuration.xml
  // Create a BLE Descriptor
     pCharacteristic -> addDescriptor(new BLE2902());
  // Start the service
     pService->start();

  // Start advertising
     BLEAdvertising *pAdvertising = BLEDevice::getAdvertising();
     pAdvertising -> addServiceUUID(SERVICE_UUID);
     pAdvertising -> setScanResponse(false);
     pAdvertising -> setMinPreferred(0x0);  // set value to 0x00 to not advertise this parameter
     BLEDevice::startAdvertising();
     Serial.println("Waiting a client connection to notify...");

  delay(1000);

}

void loop() {

  if (IMU.readByte(MPU9250_ADDRESS, INT_STATUS) & 0x01)
  {  
    IMU.readAccelData(IMU.accelCount);  // Read the x/y/z adc values
    IMU.getAres();

    // Now we'll calculate the accleration value into actual g's
    // This depends on scale being set
    IMU.ax = (float)IMU.accelCount[0]*IMU.aRes; // - accelBias[0];
    IMU.ay = (float)IMU.accelCount[1]*IMU.aRes; // - accelBias[1];
    IMU.az = (float)IMU.accelCount[2]*IMU.aRes; // - accelBias[2];

    ax_i = IMU.ax*10; 
    ay_i = IMU.ay*10;
    az_i = IMU.az*10;

    //これはPC側のシリアルモニタで見ます。デバッグ用です。
    Serial.print(timer);
    Serial.print(' ');
    Serial.print(dt);
    Serial.print(' ');
    Serial.print(IMU.ax);
    Serial.print(' ');
    Serial.println(IMU.ay);

    // ble接続が完了
    if (deviceConnected) {

    buff[0]  = ax_i & 0xff;   buff[1] = ax_i >>   8;
    buff[2]  = ay_i & 0xff;   buff[3] = ay_i >>   8; 
    buff[4]  = az_i & 0xff;   buff[5] = az_i >>   8; 

    pCharacteristic->setValue( buff, sizeof(buff) );
    pCharacteristic->notify();

    //M5.Lcd.fillScreen(BLACK);
  
    dt = millis() - timer;
    float dtf = (float)dt/1000; //運動の計算用に使うものは単位[s]にしときます。
    //速度更新
    velx = velx + -500 * IMU.ax * dtf; //500は適当に調整しました。慣性を決めます。
    vely = vely + 500 * IMU.ay * dtf;

    //位置更新
    posx = posx + velx * dtf;
    posy = posy + vely * dtf;
    timer = millis();

    //はみ出さない，速くしすぎない
    posx = constrain(posx, 0, 319);
    posy = constrain(posy, 0, 239);
    velx = constrain(velx, -319, 319);
    vely = constrain(vely, -239, 239);
    
    //端っこ来たら跳ね返る
    if(posx == 0 && velx < 0)
      velx = velx * -0.8;
    if(posx == 319 && velx > 0)
      velx = velx * -0.8;
    if(posy == 0 && vely < 0)
      vely = vely * -0.8;
    if(posy == 239 && vely > 0)
      vely = vely * -0.8;
    
    //ボールの描画
    //M5.Lcd.fillCircle((int)posx, (int)posy, radius, RED);
    //M5.Lcd.fillCircle(-(int)posx, -(int)posy, radius*0.5, BLUE);
    //M5.Lcd.fillCircle((int)posy, -(int)posx, radius*1.5, GREEN);
    //M5.Lcd.fillScreen(WHITE);
  }
  
  }
    // disconnecting
    if (!deviceConnected && oldDeviceConnected) {
        delay(500); // give the bluetooth stack the chance to get things ready
        pServer->startAdvertising(); // restart advertising
        Serial.println("start advertising");
        oldDeviceConnected = deviceConnected;
    }
    // connecting
    if (deviceConnected && !oldDeviceConnected) {
        // do stuff here on connecting
        oldDeviceConnected = deviceConnected;
    }
  delay(50);
}
