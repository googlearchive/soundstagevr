// Copyright 2017 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class cameraDeviceInterface : deviceInterface {

  public Camera rtCam;
  public GameObject realCam, previewCam;
  public Transform rtQuad, screenTrans;
  public GameObject recLight, recMessage;
  public button broadcastButton, previewButton;

  omniJack input;
  signalGenerator externalPulse;
  float[] lastPlaySig;

  bool activated = false;

  [DllImport("SoundStageNative")]
  public static extern int CountPulses(float[] buffer, int length, int channels, float[] lastSig);

  public override void Awake() {
    base.Awake();
    lastPlaySig = new float[] { 0, 0 };
    input = GetComponentInChildren<omniJack>();
    recLight.GetComponent<Renderer>().material.SetColor("_TintColor", Color.HSVToRGB(0, 152 / 255f, 69 / 255f));
    recLight.SetActive(false);
    recMessage.SetActive(false);
    camSetup();

  }

  void Start() {

    bool sky = masterControl.instance.showEnvironment;
    Camera[] cams = GetComponentsInChildren<Camera>();
    for (int i = 0; i < cams.Length; i++) {
      if (cams[i].GetComponent<Skybox>() != null) cams[i].GetComponent<Skybox>().enabled = sky;
    }
    realCam.GetComponent<Skybox>().enabled = sky;
    activated = true;
  }

  void OnDestroy() {
    if (screenTrans != null && activated) Destroy(screenTrans.gameObject);
  }

  void Update() {
    if (input.signal != externalPulse) externalPulse = input.signal;
    if (hits > 0) {
      if (hits % 2 != 0) toggleRealCam(!broadcasting);
      hits = 0;
    }
  }

  void OnDisable() {
    if (broadcasting) toggleRealCam(false);
  }

  bool preview = true;
  void togglePreview(bool on) {
    preview = on;
    previewCam.SetActive(on);
    screenTrans.gameObject.SetActive(on);
  }

  public override void hit(bool on, int ID = -1) {
    if (ID == 0) toggleRealCam(on);
    if (ID == 1) togglePreview(on);
  }

  bool broadcasting = false;
  public void toggleRealCam(bool on) {
    if (broadcasting == on) return;
    broadcasting = on;
    if (on) {
      realCam.GetComponent<Skybox>().enabled = masterControl.instance.showEnvironment;
      cameraDeviceInterface[] cams = FindObjectsOfType<cameraDeviceInterface>();
      for (int i = 0; i < cams.Length; i++) {
        if (cams[i] != this) {
          cams[i].toggleRealCam(false);
          cams[i].broadcastButton.phantomHit(false);
        }
      }
    }

    broadcastButton.phantomHit(on);
    recLight.SetActive(on);
    recMessage.SetActive(on);
    realCam.SetActive(on);
  }

  bool curHiRes = false;
  public void updateResolution(bool hires) {
    if (curHiRes == hires) return;
    camSetup(hires);
  }

  void camSetup(bool hires = false) {
    curHiRes = hires;

    if (rtCam.targetTexture != null) {
      rtCam.targetTexture.Release();
    }
    int mult = hires ? 32 : 16;
    rtCam.targetTexture = new RenderTexture(16 * mult, 9 * mult, 16);
    rtQuad.GetComponent<Renderer>().material.mainTexture = rtCam.targetTexture;
  }



  int hits = 0;
  private void OnAudioFilterRead(float[] buffer, int channels) {
    if (externalPulse == null) return;
    double dspTime = AudioSettings.dspTime;

    float[] playBuffer = new float[buffer.Length];
    externalPulse.processBuffer(playBuffer, dspTime, channels);

    hits += CountPulses(playBuffer, buffer.Length, channels, lastPlaySig);
  }


  public override InstrumentData GetData() {
    CameraData data = new CameraData();
    data.deviceType = menuItem.deviceType.Camera;
    GetTransformData(data);

    data.inputID = input.transform.GetInstanceID();

    data.screenPosition = screenTrans.localPosition;
    data.screenRotation = screenTrans.localRotation;
    data.screenScale = screenTrans.localScale;
    data.screenDisabled = !preview;
    return data;
  }

  public override void Load(InstrumentData d) {
    CameraData data = d as CameraData;
    base.Load(data);

    previewButton.startToggled = !data.screenDisabled;

    screenTrans.localPosition = data.screenPosition;
    screenTrans.localRotation = data.screenRotation;
    screenTrans.localScale = data.screenScale;
    input.ID = data.inputID;
  }
}

public class CameraData : InstrumentData {
  public Vector3 screenPosition;
  public Vector3 screenScale;
  public Quaternion screenRotation;
  public int inputID;
  public bool screenDisabled;
}
