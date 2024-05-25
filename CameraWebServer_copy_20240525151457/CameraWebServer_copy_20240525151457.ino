#include "esp_camera.h"
#include <WiFi.h>
#include <WebSocketsClient.h>
#include <HTTPClient.h>

// Replace with your network credentials
const char* ssid = "Marcelo Hani";
const char* password = "12312312";

// WebSocket server details
const char* websocket_server_host = "172.20.10.2"; // Replace with your server's IP address
const int websocket_server_port = 8181;
const char* websocket_server_path = "/";

// Azure Blob Storage details
const char* storageAccountName = "iotproject1";
const char* sasToken = "sp=racwdli&st=2024-05-25T14:50:22Z&se=2024-05-25T22:50:22Z&sv=2022-11-02&sr=c&sig=je%2F%2FQhHQEIXUKv8WhzA%2Fb6r0pOCh6imdRlxgC17yZ14%3D";
const char* blobContainerName = "iot-10sec-video";

// WebSocket client
WebSocketsClient webSocket;

// Camera settings
#define CAMERA_MODEL_AI_THINKER // Replace with your camera model
#include "camera_pins.h"

// IoT Button
const int buttonPin = 4; // Pin where the button is connected
volatile bool buttonPressed = false;

void IRAM_ATTR onButtonPress() {
  buttonPressed = true;
}

void setup() {
  Serial.begin(115200);

  // Setup button pin
  pinMode(buttonPin, INPUT_PULLUP);
  attachInterrupt(buttonPin, onButtonPress, FALLING);

  // Connect to Wi-Fi
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    Serial.println("Connecting to WiFi...");
  }
  Serial.println("Connected to WiFi");

  // Initialize the camera
  camera_config_t config;
  config.ledc_channel = LEDC_CHANNEL_0;
  config.ledc_timer = LEDC_TIMER_0;
  config.pin_d0 = Y2_GPIO_NUM;
  config.pin_d1 = Y3_GPIO_NUM;
  config.pin_d2 = Y4_GPIO_NUM;
  config.pin_d3 = Y5_GPIO_NUM;
  config.pin_d4 = Y6_GPIO_NUM;
  config.pin_d5 = Y7_GPIO_NUM;
  config.pin_d6 = Y8_GPIO_NUM;
  config.pin_d7 = Y9_GPIO_NUM;
  config.pin_xclk = XCLK_GPIO_NUM;
  config.pin_pclk = PCLK_GPIO_NUM;
  config.pin_vsync = VSYNC_GPIO_NUM;
  config.pin_href = HREF_GPIO_NUM;
  config.pin_sccb_sda = SIOD_GPIO_NUM;
  config.pin_sccb_scl = SIOC_GPIO_NUM;
  config.pin_pwdn = PWDN_GPIO_NUM;
  config.pin_reset = RESET_GPIO_NUM;
  config.xclk_freq_hz = 20000000;
  config.pixel_format = PIXFORMAT_JPEG;

  if (psramFound()) {
    config.frame_size = FRAMESIZE_VGA;
    config.jpeg_quality = 10;
    config.fb_count = 2;
  } else {
    config.frame_size = FRAMESIZE_QVGA;
    config.jpeg_quality = 12;
    config.fb_count = 1;
  }

  // Camera init
  esp_err_t err = esp_camera_init(&config);
  if (err != ESP_OK) {
    Serial.printf("Camera init failed with error 0x%x", err);
    return;
  }

  // Initialize WebSocket
  webSocket.begin(websocket_server_host, websocket_server_port, websocket_server_path);
  webSocket.onEvent(webSocketEvent);
}

void loop() {
  webSocket.loop();
  
  // Handle button press for picture capture and upload
  if (buttonPressed) {
    buttonPressed = false;
    captureAndUploadImage();
  }
  
  // Regularly send frame to WebSocket
  static unsigned long lastStreamTime = 0;
  if (millis() - lastStreamTime > 100) { // Adjust the interval as necessary
    lastStreamTime = millis();
    captureAndSendFrame();
  }
}

void webSocketEvent(WStype_t type, uint8_t *payload, size_t length) {
  switch (type) {
    case WStype_DISCONNECTED:
      Serial.println("WebSocket Disconnected!");
      break;
    case WStype_CONNECTED:
      Serial.println("WebSocket Connected!");
      break;
    case WStype_TEXT:
      Serial.printf("WebSocket Text: %s\n", payload);
      break;
    case WStype_BIN:
      Serial.printf("WebSocket Binary data length: %u\n", length);
      break;
    case WStype_PING:
      Serial.println("WebSocket Ping!");
      break;
    case WStype_PONG:
      Serial.println("WebSocket Pong!");
      break;
  }
}

void captureAndSendFrame() {
  if (WiFi.status() == WL_CONNECTED) {
    camera_fb_t * fb = NULL;
    fb = esp_camera_fb_get();
    if (!fb) {
      Serial.println("Camera capture failed");
      return;
    }

    if (webSocket.isConnected()) {
      webSocket.sendBIN(fb->buf, fb->len);
    }

    esp_camera_fb_return(fb);
  } else {
    Serial.println("WiFi not connected, cannot stream.");
  }
}

void captureAndUploadImage() {
  camera_fb_t * fb = NULL;
  fb = esp_camera_fb_get();
  if (!fb) {
    Serial.println("Camera capture failed");
    return;
  }

  // Upload to Azure Blob Storage
  if (uploadImageToAzure(fb->buf, fb->len)) {
    Serial.println("Image uploaded to Azure Blob Storage successfully");
  } else {
    Serial.println("Failed to upload image to Azure Blob Storage");
  }

  esp_camera_fb_return(fb);
}

bool uploadImageToAzure(uint8_t *imageData, size_t imageLength) {
  // Create the URL for the Azure Blob Storage with the SAS token
  String url = String("https://") + storageAccountName + ".blob.core.windows.net/" + blobContainerName + "/" + String(millis()) + ".jpg" + "?" + sasToken;

  // Create the HTTP client
  HTTPClient http;
  http.begin(url);

  // Set the headers for the request
  http.addHeader("x-ms-blob-type", "BlockBlob");

  // Send the request
  int httpResponseCode = http.PUT(imageData, imageLength);

  // Check the response code
  if (httpResponseCode == 201) {
    http.end();
    return true;
  } else {
    Serial.printf("Error uploading image, HTTP response code: %d\n", httpResponseCode);
    http.end();
    return false;
  }
}
