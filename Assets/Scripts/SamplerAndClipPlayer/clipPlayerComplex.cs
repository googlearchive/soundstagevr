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

public class clipPlayerComplex : clipPlayer {

  public float playbackSpeed = 1;
  public float amplitude = 1;

  public samplerDeviceInterface _sampleInterface;

  public Transform scrubTransform;
  public GameObject[] scrubIndicators;
  Vector2 scrubRange = new Vector2(.2f, -.2f);

  public signalGenerator speedGen, ampGen, seqGen;
  public float speedRange = 1;

  public bool active = true;

  float _lastBuffer = 0;
  float[] lastSeqGen;

  Texture2D tex;
  public Renderer waverend;
  public Color32 waveBG = Color.black;
  public Color32 waveLine = Color.white;
  int wavewidth = 512;
  int waveheight = 64;
  Color32[] wavepixels;

  float[] speedBuffer;
  float[] ampBuffer;
  float[] seqBuffer;

  [DllImport("SoundStageNative")]
  public static extern float ClipSignalGenerator(float[] buffer, float[] speedBuffer, float[] ampBuffer, float[] seqBuffer, int length, float[] lastSeqGen, int channels, bool speedGen, bool ampGen, bool seqGen, float floatingBufferCount
, int[] sampleBounds, float playbackSpeed, System.IntPtr clip, int clipChannels, float amplitude, bool playdirection, bool looping, double _sampleDuration, int bufferCount, ref bool active);

  public override void Awake() {
    base.Awake();
    speedBuffer = new float[MAX_BUFFER_LENGTH];
    ampBuffer = new float[MAX_BUFFER_LENGTH];
    seqBuffer = new float[MAX_BUFFER_LENGTH];
  }

  void Start() {
    lastSeqGen = new float[] { 0, 0 };
    if (!loaded) toggleWaveDisplay(false);
  }

  public override menuItem.deviceType queryDeviceType() {
    return menuItem.deviceType.Sampler;
  }

  public override void toggleWaveDisplay(bool on) {
    if (_waveDisplayAnimation != null) StopCoroutine(_waveDisplayAnimation);
    _waveDisplayAnimation = StartCoroutine(waveDisplayAnimation(on));
  }

  Coroutine _waveDisplayAnimation;
  IEnumerator waveDisplayAnimation(bool on) {
    if (on) {

      waverend.gameObject.SetActive(on);
    } else {
      scrubTransform.gameObject.SetActive(on);
      for (int i = 0; i < scrubIndicators.Length; i++) scrubIndicators[i].SetActive(on);
    }

    if (on) {
      float timer = 0;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 12);
        waverend.material.SetFloat("_EmissionGain", Mathf.Lerp(0, 1f, timer));
        yield return null;
      }
      timer = 0;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 3);
        waverend.material.SetFloat("_EmissionGain", Mathf.Lerp(1, .25f, timer));
        yield return null;
      }
    } else {
      float timer = 0;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 5);
        waverend.material.SetFloat("_EmissionGain", Mathf.Lerp(.25f, 0, timer));
        yield return null;
      }
    }

    if (!on) {
      waverend.gameObject.SetActive(on);
    } else {
      scrubTransform.gameObject.SetActive(on);
      for (int i = 0; i < scrubIndicators.Length; i++) scrubIndicators[i].SetActive(on);
    }
  }

  public void updateTrackBounds() {
    if (!loaded) return;
    sampleBounds[0] = (int)((clipSamples.Length / clipChannels - 1) * (trackBounds.x));
    sampleBounds[1] = (int)((clipSamples.Length / clipChannels - 1) * (trackBounds.y));
  }

  public override void DrawClipTex() {
    tex = new Texture2D(wavewidth, waveheight, TextureFormat.RGBA32, false);
    wavepixels = new Color32[wavewidth * waveheight];

    for (int i = 0; i < wavewidth; i++) {
      for (int i2 = 0; i2 < waveheight; i2++) {
        wavepixels[i2 * wavewidth + i] = waveBG;
      }
    }

    int centerH = waveheight / 2;
    int columnMult = Mathf.CeilToInt((float)clipSamples.Length / (wavewidth - 1));


    for (int i = 0; i < wavewidth; i++) {

      if (columnMult * i < clipSamples.Length) {
        int curH = Mathf.FloorToInt((waveheight - 1) * .5f * Mathf.Clamp01(Mathf.Abs(clipSamples[columnMult * i])));

        for (int i2 = 0; i2 < centerH; i2++) {
          if (i2 < curH) wavepixels[(centerH - i2) * wavewidth + i] = wavepixels[(centerH + i2) * wavewidth + i] = waveLine;
          else wavepixels[(centerH - i2) * wavewidth + i] = wavepixels[(centerH + i2) * wavewidth + i] = waveBG;
        }
      }
    }

    tex.SetPixels32(wavepixels);
    tex.Apply(false);
    waverend.material.mainTexture = tex;
  }

  public bool looping = true;
  public bool playdirection = true;
  public void Play() {
    _lastBuffer = sampleBounds[0];
    active = true;
  }

  public void Back() {
    _lastBuffer = playdirection ? sampleBounds[0] : sampleBounds[1];
  }

  public void togglePause(bool on) {
    active = on;
  }

  public void Loop() {
    active = true;
    looping = true;
  }

  float lastScrubTarg = 0;
  public void grabScrub(bool on) {
    scrubGrabbed = on;
    float targ = Mathf.InverseLerp(scrubRange.x, scrubRange.y, scrubTransform.localPosition.x);
    lastScrubTarg = scrubTarg = (int)((clipSamples.Length / clipChannels - 1) * targ);
  }

  public void updateTurntableDelta(float d) {
    if (!scrubGrabbed) turntableDelta += d;
  }

  public bool turntableGrabbed = false;
  public float turntableDelta = 0; //seconds scrubbed

  public bool scrubGrabbed = false;
  float samplePos = 0;
  float scrubTarg = 0;

  float cumScrubAmount = 0;
  void Update() {
    if (!scrubGrabbed) {
      Vector3 pos = scrubTransform.localPosition;
      pos.x = Mathf.Lerp(scrubRange.x, scrubRange.y, samplePos);
      scrubTransform.localPosition = pos;


    } else if (scrubGrabbed) {
      float targ = Mathf.InverseLerp(scrubRange.x, scrubRange.y, scrubTransform.localPosition.x);
      scrubTarg = (int)((clipSamples.Length / clipChannels - 1) * targ);
      cumScrubAmount += (scrubTarg - lastScrubTarg);
      lastScrubTarg = scrubTarg;

    }
  }

  public float getScrubAmount() {
    float s = cumScrubAmount * (float)_sampleDuration;
    cumScrubAmount = 0;
    return s;
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (!loaded) return;
    floatingBufferCount = _lastBuffer;

    if (speedBuffer.Length != buffer.Length)
      System.Array.Resize(ref speedBuffer, buffer.Length);
    if (ampBuffer.Length != buffer.Length)
      System.Array.Resize(ref ampBuffer, buffer.Length);
    if (seqBuffer.Length != buffer.Length)
      System.Array.Resize(ref seqBuffer, buffer.Length);


    if (speedGen != null) speedGen.processBuffer(speedBuffer, dspTime, channels);
    if (ampGen != null) ampGen.processBuffer(ampBuffer, dspTime, channels);
    if (seqGen != null) seqGen.processBuffer(seqBuffer, dspTime, channels);

    if (!scrubGrabbed && !turntableGrabbed) {
      bool curActive = active;
      floatingBufferCount = ClipSignalGenerator(buffer, speedBuffer, ampBuffer, seqBuffer, buffer.Length, lastSeqGen, channels, speedGen != null, ampGen != null, seqGen != null, floatingBufferCount, sampleBounds,
          playbackSpeed, m_ClipHandle.AddrOfPinnedObject(), clipChannels, amplitude, playdirection, looping, _sampleDuration, bufferCount, ref active);
      if (curActive != active) _sampleInterface.playEvent(active);

      lp_filter[0] = buffer[buffer.Length - 2];
      lp_filter[1] = buffer[buffer.Length - 1];
    } else if (scrubGrabbed) // keeping scrub non-native because such an edge-case and maxes out at 2 instances
      {
      float amount = (scrubTarg - _lastBuffer) / (buffer.Length / channels);

      for (int i = 0; i < buffer.Length; i += channels) {
        bufferCount = Mathf.RoundToInt(floatingBufferCount);
        floatingBufferCount += amount;

        float endAmplitude = amplitude;
        if (ampGen != null) endAmplitude = endAmplitude * ((ampBuffer[i] + 1) / 2f);
        buffer[i] = lp_filter[0] = lp_filter[0] * .9f + .1f * clipSamples[bufferCount * clipChannels] * endAmplitude;
        if (clipChannels == 2) buffer[i + 1] = lp_filter[1] = lp_filter[1] * .9f + .1f * clipSamples[bufferCount * clipChannels + 1] * endAmplitude;
        else buffer[i + 1] = buffer[i];

        dspTime += _sampleDuration;
      }
    } else {
      float amount = turntableDelta * (float)_sampleRate * channels / buffer.Length;
      for (int i = 0; i < buffer.Length; i += channels) {
        bufferCount = Mathf.RoundToInt(floatingBufferCount);
        floatingBufferCount += amount;

        if (bufferCount > sampleBounds[1]) floatingBufferCount = bufferCount = sampleBounds[0];

        else if (bufferCount < sampleBounds[0]) floatingBufferCount = bufferCount = sampleBounds[1];

        float endAmplitude = amplitude;
        if (ampGen != null) endAmplitude = endAmplitude * ((ampBuffer[i] + 1) / 2f);

        buffer[i] = lp_filter[0] = lp_filter[0] * .9f + .1f * clipSamples[bufferCount * clipChannels] * endAmplitude;
        if (clipChannels == 2) buffer[i + 1] = lp_filter[1] = lp_filter[1] * .9f + .1f * clipSamples[bufferCount * clipChannels + 1] * endAmplitude;
        else buffer[i + 1] = buffer[i];

        dspTime += _sampleDuration;
      }
      turntableDelta = 0;

    }

    _lastBuffer = floatingBufferCount;
    samplePos = (float)floatingBufferCount * clipChannels / clipSamples.Length;
  }

  float[] lp_filter = new float[] { 0, 0 };
}