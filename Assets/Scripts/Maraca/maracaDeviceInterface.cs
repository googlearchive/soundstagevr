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

public class maracaDeviceInterface : deviceInterface {
  maracaSignalGenerator signal;
  maracaUI _maracaUI;
  omniJack jackOut;
  double _sampleDuration;

  [DllImport("SoundStageNative")]
  public static extern void MaracaProcessAudioBuffer(float[] buffer, float[] controlBuffer, int length, int channels, ref double _phase, double _sampleDuration);

  public override void Awake() {
    base.Awake();
    _sampleDuration = 1.0 / AudioSettings.outputSampleRate;
    signal = GetComponent<maracaSignalGenerator>();
    _maracaUI = GetComponentInChildren<maracaUI>();
    jackOut = GetComponentInChildren<omniJack>();
  }

  void Update() {
    signal.curShake = _maracaUI.shakeVal;
  }

  void OnDestroy() {
    if (_maracaUI.transform.parent != transform) Destroy(_maracaUI.gameObject);
  }

  double _phaseB = 0;
  private void OnAudioFilterRead(float[] buffer, int channels) {
    if (jackOut.near != null || signal.curShake < .01f) return;

    double dspTime = AudioSettings.dspTime;
    float[] b = new float[buffer.Length];
    signal.processBuffer(b, dspTime, channels);

    MaracaProcessAudioBuffer(buffer, b, buffer.Length, channels, ref _phaseB, _sampleDuration);
  }

  public override InstrumentData GetData() {
    MaracaData data = new MaracaData();
    data.deviceType = menuItem.deviceType.Maracas;
    GetTransformData(data);
    data.jackOutID = jackOut.transform.GetInstanceID();
    return data;
  }

  public override void Load(InstrumentData d) {
    MaracaData data = d as MaracaData;
    base.Load(data);
    jackOut.ID = data.jackOutID;
  }
}

public class MaracaData : InstrumentData {
  public int jackOutID;
}