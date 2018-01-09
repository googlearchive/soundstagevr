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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

public class xylorollSignalGenerator : signalGenerator {
  public GameObject monophonicPrefab;

  public bool oscInput = true;

  List<monophone> voices;

  [DllImport("SoundStageNative")]
  public static extern void XylorollMergeSignalsWithoutOsc(float[] buf, int length, float[] buf1, float[] buf2);

  [DllImport("SoundStageNative")]
  public static extern void XylorollMergeSignalsWithOsc(float[] buf, int length, float[] buf1, float[] buf2);

  public void spawnVoices(int n, float[] adsrVol, float[] adsrDur) {
    voices = new List<monophone>();

    samplerLoad _samplerLoad = GetComponentInChildren<samplerLoad>();
    _samplerLoad.players = new clipPlayer[n];

    for (int i = 0; i < n; i++) {
      voices.Add(new monophone(Instantiate(monophonicPrefab, transform, false) as GameObject));
      voices[i].adsr.durations = adsrDur;
      voices[i].adsr.volumes = adsrVol;
      _samplerLoad.players[i] = voices[i].sampler;
    }
  }

  public void updateOscAmp(float[] amps, float[] freqs, float[] waves) {
    for (int i = 0; i < voices.Count; i++) {
      for (int i2 = 0; i2 < 2; i2++) {
        voices[i].osc[i2].amplitude = amps[i2];
        voices[i].osc[i2].frequency = Mathf.Lerp(414f, 466f, freqs[i2]);
        voices[i].osc[i2].analogWave = waves[i2];
      }
    }
  }

  public void updateVoices(int ID, bool add) {
    if (add) {
      for (int i = 0; i < voices.Count; i++) {
        if (voices[i].curKey == ID) {
          setMonophone(i, ID);
          return;
        }
      }

      // look for an empty
      for (int i = 0; i < voices.Count; i++) {
        if (voices[i].curKey == -1) {
          setMonophone(i, ID);
          return;
        }
      }

      // look for a releasing
      for (int i = 0; i < voices.Count; i++) {
        if (voices[i].releasing) {
          setMonophone(i, ID);
          return;
        }
      }

      // give up and grab the first thing
      setMonophone(0, ID);
    } else {
      int targVoice = -1;

      for (int i = 0; i < voices.Count; i++) {
        if (voices[i].curKey == ID) targVoice = i;

      }

      if (targVoice != -1) setMonophone(targVoice, -1);
    }
  }

  public void setMonophone(int v, int ID) {

    if (ID == -1) {
      if (voices[v].adsr.sustaining) {
        voices[v].adsr.hit(false);
        voices[v].releasing = true;
        monophone m = voices[v];
        voices.RemoveAt(v);
        voices.Add(m);
      }

    } else {
      voices[v].curKey = ID;
      if (oscInput) voices[v].key.UpdateKey(ID);
      else {
        float mult = voices[v].key.getMult(ID);
        voices[v].sampler.Play(mult);
      }
      voices[v].adsr.hit(true);
      voices[v].releasing = false;
      monophone m = voices[v];
      voices.RemoveAt(v);
      voices.Add(m);
    }

  }

  public void updateOctave(int val) {
    for (int i = 0; i < voices.Count; i++) voices[i].key.updateOctave(val);
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    for (int i = 0; i < buffer.Length; i++) buffer[i] = 0;

    float[] b1 = new float[buffer.Length];
    float[] b2 = new float[buffer.Length];

    if (oscInput) {
      for (int i = 0; i < voices.Count; i++) {
        if (voices[i].adsr.active) {
          voices[i].osc[0].processBuffer(b1, dspTime, channels);
          voices[i].osc[1].processBuffer(b2, dspTime, channels);


          XylorollMergeSignalsWithOsc(buffer, buffer.Length, b1, b2);

        } else {
          voices[i].curKey = -1;
          voices[i].releasing = false;
        }
      }
    } else {
      for (int i = 0; i < voices.Count; i++) {

        if (voices[i].adsr.active) {
          voices[i].sampler.processBuffer(b1, dspTime, channels);
          voices[i].adsr.processBuffer(b2, dspTime, channels);

          XylorollMergeSignalsWithoutOsc(buffer, buffer.Length, b1, b2);
        } else voices[i].curKey = -1;

      }
    }
  }
}

public class monophone {
  public int curKey = -1;

  public bool releasing = false;

  public keyFrequencySignalGenerator key;
  public adsrSignalGenerator adsr;
  public oscillatorSignalGenerator[] osc;
  public clipPlayerSimple sampler;
  GameObject gameobject;

  public monophone(GameObject g) {
    gameobject = g;
    adsr = g.GetComponent<adsrSignalGenerator>();
    sampler = g.GetComponent<clipPlayerSimple>();
    key = g.GetComponent<keyFrequencySignalGenerator>();
    osc = g.GetComponents<oscillatorSignalGenerator>();
  }
}