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
using System.Runtime.InteropServices;

public class MicrophoneSignalGenerator : signalGenerator {

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);
  [DllImport("SoundStageNative")]
  public static extern void CopyArray(float[] a, float[] b, int length);
  [DllImport("SoundStageNative")]
  public static extern void MicFunction(float[] a, float[] b, int length, float val);

  AudioClip micClip;
  AudioSource source;
  float[] sharedBuffer;
  bool activated = false;

  public Dictionary<float, float[]> freqBuffers = new Dictionary<float, float[]>();

  public float amp = 1;
  int micChannels = 1;
  public bool active = true;
  int curMicID = 0;

  public override void Awake() {
    base.Awake();
    sharedBuffer = new float[MAX_BUFFER_LENGTH];
  }

  void Start() {
    source = GetComponent<AudioSource>();
    SelectMic(0);
  }

  Coroutine _MicActivateRoutine;
  void SelectMic(int num) {
    if (num >= Microphone.devices.Length) {
      return;
    }

    if (_MicActivateRoutine != null) StopCoroutine(_MicActivateRoutine);
    _MicActivateRoutine = StartCoroutine(MicActivateRoutine(num));
  }

  IEnumerator MicActivateRoutine(int num) {
    source.Stop();
    Microphone.End(Microphone.devices[curMicID]);
    curMicID = num;
    micClip = new AudioClip();

    micClip = Microphone.Start(Microphone.devices[num], true, 1, 44100);

    yield return null;
    if (micClip != null) {
      source.clip = micClip;
      source.loop = true;
      while (!(Microphone.GetPosition(null) > 0)) { }
      source.Play();
    }

    yield return null;

  }

  private void OnAudioFilterRead(float[] buffer, int channels) {
    activated = true;
    if (sharedBuffer.Length != buffer.Length)
      System.Array.Resize(ref sharedBuffer, buffer.Length);

    CopyArray(buffer, sharedBuffer, buffer.Length);
    SetArrayToSingleValue(buffer, buffer.Length, 0.0f);
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (!active || !activated) {
      return;
    }

    MicFunction(buffer, sharedBuffer, buffer.Length, amp);
  }
}
