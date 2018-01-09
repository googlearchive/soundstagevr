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
using System.Runtime.InteropServices;

public class samplerLoad : MonoBehaviour {
  public GameObject tapePrefab;
  public GameObject loadingPrefab;
  public Transform deckOutline;
  public Transform deck;
  public clipPlayer[] players;
  public string CurTapeLabel;
  public string CurFile;

  public embeddedSpeaker miniSpeaker;

  Material deckMat;
  Color deckLight;

  public float[] clipSamples;

  GCHandle m_ClipHandle;

  void Awake() {
    if (players.Length == 0) players = GetComponents<clipPlayer>();
    deckMat = deckOutline.GetComponent<Renderer>().material;

    deckMat.SetFloat("_EmissionGain", .4f);
    deckLight = Color.white;
    deckMat.SetColor("_TintColor", Color.black);

    deckOutline.gameObject.SetActive(false);

    if (miniSpeaker != null) miniSpeaker.updateSecondary(false);
  }

  void Start() {
    deck.gameObject.layer = 14;
  }

  public bool hasTape() {
    return (currentTape != null);
  }

  tape currentTape;
  public void LoadTape(tape t) {
    if (currentTape != null && currentTape != t) {
      ForceEject();
    }
    currentTape = t;
    CurTapeLabel = t.label;
    CurFile = t.filename;
    if (miniSpeaker != null) miniSpeaker.updateSecondary(true);
    LoadClip(t.filename);
  }

  public void getTapeInfo(out string label, out string file) {
    if (currentTape != null) {
      label = CurTapeLabel;
      file = CurFile;
    } else {
      label = file = "";
    }
  }

  public void ForceEject() {
    if (currentTape != null) {
      tape temp = currentTape;
      UnloadTape(currentTape);
      temp.Eject();
    }
  }

  public void UnloadTape(tape t) {

    if (currentTape == t) {
      currentTape = null;
      if (_streamRoutine != null) {
        if (loaderObject != null) Destroy(loaderObject);
        StopCoroutine(_streamRoutine);
      }

      if (miniSpeaker != null) miniSpeaker.updateSecondary(false);

      // unallocate memory
      m_ClipHandle.Free();
      for (int i = 0; i < players.Length; i++) players[i].UnloadClip();
    }
  }

  public void LoadClip(string path) {

    string fullpath = sampleManager.instance.parseFilename(path);

    if (!File.Exists(fullpath)) {
      return;
    }

    if (_streamRoutine != null) {
      if (loaderObject != null) Destroy(loaderObject);
      StopCoroutine(_streamRoutine);
    }

    _streamRoutine = StartCoroutine(streamRoutine(fullpath));
  }

  GameObject loaderObject;
  Coroutine _streamRoutine;
  IEnumerator streamRoutine(string fullpath) {
    AudioClip c = RuntimeAudioClipLoader.Manager.Load(fullpath, false, true, false);

    loaderObject = Instantiate(loadingPrefab, transform, false) as GameObject;
    loaderObject.transform.localPosition = new Vector3(-.05f, .013f, 0.061f);
    loaderObject.transform.localRotation = Quaternion.Euler(0, 180, 0);
    loaderObject.transform.localScale = Vector3.one * .1f;

    while (RuntimeAudioClipLoader.Manager.GetAudioClipLoadState(c) != AudioDataLoadState.Loaded) {
      yield return null;
    }
    if (loaderObject != null) Destroy(loaderObject);


    for (int i = 0; i < players.Length; i++) players[i].UnloadClip();

    while (c.loadState != AudioDataLoadState.Loaded) yield return null;

    clipSamples = new float[c.samples * c.channels];
    c.GetData(clipSamples, 0);

    //alocate the memory
    m_ClipHandle = GCHandle.Alloc(clipSamples, GCHandleType.Pinned);
    for (int i = 0; i < players.Length; i++) players[i].LoadSamples(clipSamples, m_ClipHandle, c.channels);
  }

  void OnDestroy() {
    m_ClipHandle.Free();
  }

  public string[] queuedSample = new string[] { "", "" };
  void OnEnable() {
    if (queuedSample[0] != "") {
      SetSample(queuedSample[0], queuedSample[1]);
      queuedSample[0] = "";
      queuedSample[1] = "";
    }

  }

  public void QueueSample(string s, string f) {
    if (gameObject.activeInHierarchy) SetSample(s, f);
    else {
      queuedSample[0] = s;
      queuedSample[1] = f;
    }
  }

  public void SetSample(string s, string f) {
    if (s == "") return;

    if (!File.Exists(sampleManager.instance.parseFilename(f))) {
      return;
    }

    GameObject g = Instantiate(tapePrefab, Vector3.zero, Quaternion.identity) as GameObject;
    g.transform.localRotation = Quaternion.Euler(-90, 0, 0);
    tape t = g.GetComponent<tape>();
    t.Setup(s, f);
    t.ForceLoad(deck);
  }

  bool flashing = false;
  public void flashDecklight(bool on) {
    if (flashing == on) return;

    flashing = on;
    if (flashing) {
      if (_flashRoutine != null) StopCoroutine(_flashRoutine);
      _flashRoutine = StartCoroutine(flashRoutine());
    }
  }

  Coroutine _flashRoutine;

  IEnumerator flashRoutine() {
    deckOutline.gameObject.SetActive(true);
    while (flashing) {
      float cyc = Mathf.Repeat(masterControl.instance.curCycle * 4, 1);

      if (cyc < .1f) {
        deckMat.SetColor("_TintColor", Color.Lerp(Color.black, deckLight, cyc * 10));
        yield return null;
      } else if (cyc < .5f) {
        deckMat.SetColor("_TintColor", Color.Lerp(deckLight, Color.black, (cyc - .1f) / .4f));
        yield return null;
      } else if (cyc < .6f) {
        deckMat.SetColor("_TintColor", Color.Lerp(Color.black, deckLight, (cyc - .5f) * 10));
        yield return null;
      } else {
        deckMat.SetColor("_TintColor", Color.Lerp(deckLight, Color.black, (cyc - .6f) / .4f));
        yield return null;
      }
    }
    deckOutline.gameObject.SetActive(false);
  }

}
