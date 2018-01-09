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

public class oscillatorDeviceInterface : deviceInterface {

  public int ID = -1;
  bool active = false;
  bool lfo = false;

  // interfaces
  public basicSwitch lfoSwitch;
  public dial freqDial, ampDial;
  public waveViz viz;
  public omniJack signalOutput, freqInput, ampInput;
  public slider waveSlider;

  // current values
  float freqPercent, ampPercent, wavePercent;

  oscillatorSignalGenerator signal;

  Color lfoWaveColor = new Color(133 / 255f, 240 / 255f, 125 / 255f);
  Color oscWaveColor = new Color(125 / 255f, 154 / 255f, 240 / 255f);

  public override void Awake() {
    base.Awake();
    signal = GetComponent<oscillatorSignalGenerator>();
    active = true;
    viz.period = lfo ? 512 : 1;
    viz.waveLine = lfo ? lfoWaveColor : oscWaveColor;

    UpdateLFO();
    UpdateAmp();
    UpdateWave();
  }

  void Update() {
    if (!active) return;

    // update changed inputs
    if (lfo != !lfoSwitch.switchVal) UpdateLFO();
    if (freqPercent != freqDial.percent) UpdateFreq();
    if (ampPercent != ampDial.percent) UpdateAmp();
    if (wavePercent != waveSlider.percent) UpdateWave();

    // update inputs
    if (signal.freqGen != freqInput.signal) signal.freqGen = freqInput.signal;
    if (signal.ampGen != ampInput.signal) signal.ampGen = ampInput.signal;
  }

  void UpdateLFO() {
    lfo = !lfoSwitch.switchVal;
    viz.period = lfo ? 512 : 1;
    viz.waveLine = lfo ? lfoWaveColor : oscWaveColor;
    signal.lfo = lfo;
    UpdateFreq();
  }

  void UpdateFreq() {
    freqPercent = freqDial.percent;
    if (lfo) signal.frequency = freqPercent * 8;
    else signal.frequency = Mathf.Clamp(3520.00f * Mathf.Pow(freqDial.percent, 2), 27.50f, Mathf.Infinity);// Mathf.Pow(freqDial.percent, 2); //signal.frequency = 40 + 10000 * Mathf.Pow(freqDial.percent, 2);
  }

  void UpdateAmp() {
    ampPercent = ampDial.percent;
    signal.amplitude = ampPercent;
  }

  void UpdateWave() {
    wavePercent = waveSlider.percent;
    signal.analogWave = waveSlider.percent;
  }

  public override InstrumentData GetData() {
    OscillatorData data = new OscillatorData();
    data.deviceType = menuItem.deviceType.Oscillator;
    GetTransformData(data);

    data.lfo = lfo;
    data.amp = ampPercent;
    data.freq = freqPercent;
    data.wave = wavePercent;

    data.jackOutID = signalOutput.transform.GetInstanceID();
    data.jackInAmpID = ampInput.transform.GetInstanceID();
    data.jackInFreqID = freqInput.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    OscillatorData data = d as OscillatorData;
    base.Load(data);

    freqDial.setPercent(data.freq);
    ampDial.setPercent(data.amp);
    waveSlider.setPercent(data.wave);
    lfoSwitch.setSwitch(!data.lfo);

    ID = data.ID;
    signalOutput.ID = data.jackOutID;
    ampInput.ID = data.jackInAmpID;
    freqInput.ID = data.jackInFreqID;
  }
}

public class OscillatorData : InstrumentData {
  public float amp;
  public float freq;
  public bool lfo;
  public float wave;
  public int jackOutID;
  public int jackInAmpID;
  public int jackInFreqID;
}

