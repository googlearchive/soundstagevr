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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class splitterNodeSignalGenerator : signalGenerator {
  public Texture flowTex;
  public Renderer symbolquad, symbolquadB;

  bool flow = true;
  public signalGenerator mainSig;
  public omniJack jack;
  signalGenerator sig;

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);

  public override void Awake() {
    sig = mainSig;
    jack = GetComponentInChildren<omniJack>();

    symbolquad.material.SetTexture("_MainTex", flowTex);
    symbolquad.material.SetColor("_TintColor", Color.HSVToRGB(0.1f, .62f, .7f));
    symbolquadB.material.SetTexture("_MainTex", flowTex);
    symbolquadB.material.SetColor("_TintColor", Color.HSVToRGB(0.1f, .62f, .7f));
  }

  public void setup(signalGenerator s, bool f) {
    sig = mainSig = s;
    setFlow(f);
  }

  public void setFlow(bool on) {
    flow = on;
    if (flow) {
      symbolquad.transform.localPosition = new Vector3(.00075f, -.0016f, .0217f);
      symbolquad.transform.localRotation = Quaternion.Euler(0, 180, 0);
      symbolquadB.transform.localPosition = new Vector3(.00075f, -.0016f, -.0217f);
      symbolquadB.transform.localRotation = Quaternion.Euler(0, 180, 0);
    } else {
      symbolquad.transform.localPosition = new Vector3(.0025f, .0012f, .0217f);
      symbolquadB.transform.localPosition = new Vector3(.0025f, .0012f, -.0217f);
      symbolquad.transform.localRotation = Quaternion.Euler(0, 0, 90);
      symbolquadB.transform.localRotation = Quaternion.Euler(0, 0, 90);
    }

    if (jack.near != null) {
      jack.near.Destruct();
      jack.signal = null;
    }
    jack.outgoing = flow;

    if (flow) sig = mainSig;
    else sig = jack.signal;
  }

  void Update() {
    if (flow) return;

    if (sig != jack.signal) {
      sig = jack.signal;
    }
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (sig == null) {
      SetArrayToSingleValue(buffer, buffer.Length, 0.0f);
      return;
    }

    sig.processBuffer(buffer, dspTime, channels);
  }
}