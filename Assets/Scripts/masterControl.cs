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
using System.IO;
using System.Linq;

public class masterControl : MonoBehaviour {

  public static masterControl instance;
  public UnityEngine.Audio.AudioMixer masterMixer;
  public static float versionNumber = .76f;

  public enum platform {
    Vive,
    NoVR,
    Oculus
  };

  public platform currentPlatform = platform.Vive;

  public Color tipColor = new Color(88 / 255f, 114 / 255f, 174 / 255f);

  public AudioSource backgroundAudio, metronomeClick;
  public GameObject exampleSetups;

  public UnityEngine.UI.Toggle muteEnvToggle;

  public float bpm = 120;
  public float curCycle = 0;

  public double measurePeriod = 4;

  public int curMic = 0;
  public string currentScene = "";

  public delegate void BeatUpdateEvent(float t);
  public BeatUpdateEvent beatUpdateEvent;

  public delegate void BeatResetEvent();
  public BeatResetEvent beatResetEvent;

  public bool showEnvironment = true;
  double _sampleDuration;
  double _measurePhase;

  public SENaturalBloomAndDirtyLens mainGlowShader;
  public float glowVal = 1;

  public string SaveDir;

  public bool handlesEnabled = true;
  public bool jacksEnabled = true;

  void Awake() {
    instance = this;
    _measurePhase = 0;
    _sampleDuration = 1.0 / AudioSettings.outputSampleRate;

    if (!PlayerPrefs.HasKey("glowVal")) PlayerPrefs.SetFloat("glowVal", 1);
    if (!PlayerPrefs.HasKey("envSound")) PlayerPrefs.SetInt("envSound", 1);

    glowVal = PlayerPrefs.GetFloat("glowVal");
    setGlowLevel(glowVal);

    if (PlayerPrefs.GetInt("envSound") == 0) {
      MuteBackgroundSFX(true);
      muteEnvToggle.isOn = true;
    }

    SaveDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + "SoundStage";
    ReadFileLocConfig();
    Directory.CreateDirectory(SaveDir + Path.DirectorySeparatorChar + "Saves");
    Directory.CreateDirectory(SaveDir + Path.DirectorySeparatorChar + "Samples");

    beatUpdateEvent += beatUpdateEventLocal;
    beatResetEvent += beatResetEventLocal;

    setBPM(120);

    GetComponent<sampleManager>().Init();
  }

  public void toggleInstrumentVolume(bool on) {
    masterMixer.SetFloat("instrumentVolume", on ? 0 : -18);
  }


  void ReadFileLocConfig() {
    if(File.Exists(Application.dataPath + Path.DirectorySeparatorChar + "fileloc.cfg")) {
      string _txt = File.ReadAllText(Application.dataPath + Path.DirectorySeparatorChar + "fileloc.cfg");
      if (_txt != @"x:/put/custom/dir/here" && _txt != "") {
        _txt = Path.GetFullPath(_txt);
        if (Directory.Exists(_txt)) SaveDir = _txt;
      }
    } 
  }

  public void toggleHandles(bool on) {
    handlesEnabled = on;
    handle[] handles = FindObjectsOfType<handle>();
    for (int i = 0; i < handles.Length; i++) {
      handles[i].toggleHandles(on);
    }
  }

  public void toggleJacks(bool on) {
    jacksEnabled = on;
    omniJack[] jacks = FindObjectsOfType<omniJack>();
    for (int i = 0; i < jacks.Length; i++) {
      jacks[i].GetComponent<Collider>().enabled = on;
    }

    omniPlug[] plugs = FindObjectsOfType<omniPlug>();
    for (int i = 0; i < plugs.Length; i++) {
      plugs[i].GetComponent<Collider>().enabled = on;
    }
  }

  drumDeviceInterface mostRecentDrum;
  public void newDrum(drumDeviceInterface d) {
    if (mostRecentDrum != null) mostRecentDrum.displayDrumsticks(false);
    mostRecentDrum = d;
    d.displayDrumsticks(true);

  }

  public void setGlowLevel(float t) {
    glowVal = t;
    PlayerPrefs.SetFloat("glowVal", glowVal);
    mainGlowShader.bloomIntensity = Mathf.Lerp(0, .05f, t);
  }

  public bool tooltipsOn = true;
  public bool toggleTooltips() {

    tooltipsOn = !tooltipsOn;

    touchpad[] pads = FindObjectsOfType<touchpad>();
    for (int i = 0; i < pads.Length; i++) {
      pads[i].buttonContainers[0].SetActive(tooltipsOn);
    }


    tooltips[] tips = FindObjectsOfType<tooltips>();
    for (int i = 0; i < tips.Length; i++) {
      tips[i].ShowTooltips(tooltipsOn);
    }
    return tooltipsOn;
  }

  public bool examplesOn = true;
  public bool toggleExamples() {
    examplesOn = !examplesOn;
    exampleSetups.SetActive(examplesOn);

    if (!examplesOn) {
      GameObject prevParent = GameObject.Find("exampleParent");
      if (prevParent != null) Destroy(prevParent);
    }
    return examplesOn;
  }

  public bool dialUsed = false;

  public void MicChange(int val) {
    curMic = val;
  }

  public void MuteBackgroundSFX(bool mute) {
    PlayerPrefs.SetInt("envSound", mute ? 0 : 1);
    if (mute) {
      backgroundAudio.volume = 0;
    } else backgroundAudio.volume = .02f;
  }

  void beatUpdateEventLocal(float t) { }
  void beatResetEventLocal() { }

  public void setBPM(float b) {
    bpm = Mathf.RoundToInt(b);
    measurePeriod = 480f / bpm;
    _measurePhase = curCycle * measurePeriod;
  }

  public void resetClock() {
    _measurePhase = 0;
    curCycle = 0;
    beatResetEvent();
  }

  bool beatUpdateRunning = true;
  public void toggleBeatUpdate(bool on) {
    beatUpdateRunning = on;
  }

  int lastBeat = -1;
  void Update() {
    if (lastBeat != Mathf.FloorToInt(curCycle * 8f)) {
      metronomeClick.Play();
      lastBeat = Mathf.FloorToInt(curCycle * 8f);
    }
  }

  private void OnAudioFilterRead(float[] buffer, int channels) {
    if (!beatUpdateRunning) return;
    double dspTime = AudioSettings.dspTime;

    for (int i = 0; i < buffer.Length; i += channels) {
      beatUpdateEvent(curCycle);
      _measurePhase += _sampleDuration;
      if (_measurePhase > measurePeriod) _measurePhase -= measurePeriod;
      curCycle = (float)(_measurePhase / measurePeriod);
    }
  }

  public void openRecordings() {
    System.Diagnostics.Process.Start("explorer.exe", "/root," + SaveDir + Path.DirectorySeparatorChar + "Samples" + Path.DirectorySeparatorChar + "Recordings" + Path.DirectorySeparatorChar);
  }

  public void openSavedScenes() {
    System.Diagnostics.Process.Start("explorer.exe", "/root," + SaveDir + Path.DirectorySeparatorChar + "Saves" + Path.DirectorySeparatorChar);
  }

  public void openVideoTutorials() {
    Application.OpenURL("https://www.youtube.com/playlist?list=PL9oPBUaRjJEwjy7glYUvOMqw66QrtTxZD");
  }

  public string GetFileURL(string path) {
    return (new System.Uri(path)).AbsoluteUri;
  }

  public enum BinauralMode {
    None,
    Speaker,
    All
  };
  public BinauralMode BinauralSetting = BinauralMode.None;

  public void updateBinaural(int num) {
    if (BinauralSetting == (BinauralMode)num) {
      return;
    }
    BinauralSetting = (BinauralMode)num;

    speakerDeviceInterface[] standaloneSpeakers = FindObjectsOfType<speakerDeviceInterface>();
    for (int i = 0; i < standaloneSpeakers.Length; i++) {
      if (BinauralSetting == BinauralMode.None) standaloneSpeakers[i].audio.spatialize = false;
      else standaloneSpeakers[i].audio.spatialize = true;
    }
    embeddedSpeaker[] embeddedSpeakers = FindObjectsOfType<embeddedSpeaker>();
    for (int i = 0; i < embeddedSpeakers.Length; i++) {
      if (BinauralSetting == BinauralMode.All) embeddedSpeakers[i].audio.spatialize = true;
      else embeddedSpeakers[i].audio.spatialize = false;
    }
  }

  public enum WireMode {
    Curved,
    Straight,
    Invisible
  };

  public WireMode WireSetting = WireMode.Curved;
  public void updateWireSetting(int num) {
    if (WireSetting == (WireMode)num) {
      return;
    }
    WireSetting = (WireMode)num;

    omniPlug[] plugs = FindObjectsOfType<omniPlug>();
    for (int i = 0; i < plugs.Length; i++) {
      plugs[i].updateLineType(WireSetting);
    }
  }
}
