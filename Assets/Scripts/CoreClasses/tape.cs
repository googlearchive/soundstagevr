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
using System.IO;

public class tape : manipObject {
  public Material mat;
  public Transform masterObj;
  public GameObject loadingPrefab;

  Transform deck;
  Transform tapeTrans;
  public TextMesh labelMesh;
  public string label;
  public string filename;
  public bool fullpath = false;
  Vector3 origPos = Vector3.zero;
  Quaternion origRot = Quaternion.identity;

  public bool previewWhenGrabbed = false;

  GameObject highlight;
  Material highlightMat;
  Color tapeColor;
  float tapeHue;

  public override void Awake() {
    base.Awake();
    gameObject.layer = 13;
    tapeTrans = transform.GetChild(0);
    mat = tapeTrans.GetComponent<Renderer>().material;
    labelMesh = GetComponentInChildren<TextMesh>();
    labelMesh.GetComponent<Renderer>().material.SetColor("_Color", Color.black);
    tapeHue = Random.value;
    tapeColor = Color.HSVToRGB(tapeHue, .5f, 1);
    SetColor(tapeColor);
    createHandleFeedback();
  }

  public void setOrigTrans(Vector3 pos, Quaternion rot) {
    origPos = pos;
    origRot = rot;
  }

  public void getOrigTrans(out Vector3 pos, out Quaternion rot) {
    pos = origPos;
    rot = origRot;
  }

  public void Setup(string s, string f) {
    if (masterObj == null) masterObj = transform.parent;
    label = s;
    filename = f;
    labelMesh.text = s;
    origPos = transform.localPosition;
    origRot = transform.localRotation;
  }

  public bool inDeck() {
    return (deck != null);
  }

  public Color setHue(float h) {
    tapeHue = h;
    tapeColor = Color.HSVToRGB(tapeHue, .5f, 1);
    SetColor(tapeColor);
    highlightMat.SetColor("_TintColor", Color.HSVToRGB(tapeHue, .9f, .3f));
    return tapeColor;
  }

  void createHandleFeedback() {
    highlight = new GameObject("highlight");

    MeshFilter m = highlight.AddComponent<MeshFilter>();

    m.mesh = transform.GetChild(0).GetComponent<MeshFilter>().mesh;
    MeshRenderer r = highlight.AddComponent<MeshRenderer>();
    r.material = Resources.Load("Materials/Highlight") as Material;
    highlightMat = r.material;

    highlight.transform.SetParent(transform, false);
    highlight.transform.localScale = Vector3.one * 2.3f;
    highlightMat.SetColor("_TintColor", Color.HSVToRGB(tapeHue, .9f, .3f));
    highlightMat.SetFloat("_EmissionGain", .3f);

    highlight.SetActive(false);
  }

  void SetColor(Color c) {
    mat.SetColor("_Color", c);
    mat.SetColor("_SpecColor", c);
  }

  manipulator tempM;
  void OnCollisionEnter(Collision coll) {
    if (curState != manipState.grabbed) return;
    if (coll.transform.name != "tapeInsert") return;
    if (manipulatorObjScript != null) {
      manipulatorObjScript.hapticPulse(1000);
      tempM = manipulatorObjScript;
    }

    if (deck != null && deck != coll.transform) //if still in another deck
    {
      deck.parent.GetComponent<samplerLoad>().UnloadTape(this);
    }

    deck = coll.transform;

    tapeTrans.position = deck.transform.position;
    tapeTrans.rotation = deck.transform.rotation;
    tapeTrans.parent = deck;
    tapeTrans.Rotate(-90, 0, 0);

  }

  void OnCollisionExit(Collision coll) {
    if (curState != manipState.grabbed) return;
    if (deck == coll.transform) {
      deck.parent.GetComponent<samplerLoad>().UnloadTape(this);
      if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(500);
      deck = null;
      tapeTrans.parent = transform;
      tapeTrans.localPosition = Vector3.zero;
      tapeTrans.localRotation = Quaternion.identity;
    }
  }

  Coroutine insertCoroutine;
  IEnumerator insertRoutine() {
    float timer = 0;
    Vector3 dest = new Vector3(0, 0, -.0225f);
    while (timer < 1) {
      if (tempM != null && timer < 0.5f) tempM.hapticPulse(500);
      timer = Mathf.Clamp01(timer + Time.deltaTime * 2);
      transform.localPosition = Vector3.Lerp(Vector3.zero, dest, timer);
      yield return null;
    }
  }

  void flashEmptySamplers(bool on) {
    if (!on) {
      tape[] tempTapes = FindObjectsOfType<tape>();
      for (int i = 0; i < tempTapes.Length; i++) {
        if (tempTapes[i] != this && tempTapes[i].curState == manipState.grabbed) return;
      }
    }

    samplerLoad[] samps = FindObjectsOfType<samplerLoad>();
    for (int i = 0; i < samps.Length; i++) {
      samps[i].flashDecklight(on);
    }
  }

  Coroutine returnRoutineID;
  IEnumerator returnRoutine() {
    Vector3 curPos = transform.localPosition;
    Quaternion curRot = transform.localRotation;

    float t = 0;
    float modT = 0;
    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime * 2);
      modT = Mathf.Sin(t * Mathf.PI * 0.5f);
      transform.localPosition = Vector3.Lerp(curPos, origPos, modT);
      transform.localRotation = Quaternion.Lerp(curRot, origRot, modT);
      yield return null;
    }
  }

  public void Eject() {
    deck = null;
    if (masterObj) {
      transform.parent = masterObj;
      if (gameObject.activeInHierarchy) {
        if (returnRoutineID != null) StopCoroutine(returnRoutineID);
        returnRoutineID = StartCoroutine(returnRoutine());
      } else {
        transform.localPosition = origPos;
        transform.localRotation = origRot;
      }
    } else Destroy(gameObject);
  }

  void OnEnable() {
    if (deck == null) {
      transform.localPosition = origPos;
      transform.localRotation = origRot;
    }
  }

  public void Release() {
    if (deck == null) {
      if (masterObj == null) Destroy(gameObject);

      transform.parent = masterObj;

      if (gameObject.activeInHierarchy) {

        if (returnRoutineID != null) StopCoroutine(returnRoutineID);
        returnRoutineID = StartCoroutine(returnRoutine());
      } else {
        transform.localPosition = origPos;
        transform.localRotation = origRot;
      }
    } else {

      tapeTrans.position = deck.transform.position;
      tapeTrans.rotation = deck.transform.rotation;
      tapeTrans.parent = deck;
      tapeTrans.Rotate(-90, 0, 0);

      transform.parent = tapeTrans.parent;
      transform.position = tapeTrans.position;
      transform.rotation = tapeTrans.rotation;
      tapeTrans.parent = transform;
      deck.parent.GetComponent<samplerLoad>().LoadTape(this);
      if (insertCoroutine != null) StopCoroutine(insertCoroutine);
      insertCoroutine = StartCoroutine(insertRoutine());
    }
  }

  public void ForceLoad(Transform targetDeck) {
    deck = targetDeck;
    tapeTrans.position = deck.transform.position;
    tapeTrans.rotation = deck.transform.rotation;
    tapeTrans.parent = deck;
    tapeTrans.Rotate(-90, 0, 0);

    transform.parent = tapeTrans.parent;
    transform.position = tapeTrans.position;
    transform.rotation = tapeTrans.rotation;
    tapeTrans.parent = transform;
    transform.localPosition = new Vector3(0, 0, -.0225f);
    deck.parent.GetComponent<samplerLoad>().LoadTape(this);
  }

  IEnumerator streamRoutine(string f) {
    AudioClip c = RuntimeAudioClipLoader.Manager.Load(f, false, true, false);

    loaderObject = Instantiate(loadingPrefab, transform, false) as GameObject;
    loaderObject.transform.localPosition = new Vector3(-.03f, -.037f, .01f);
    loaderObject.transform.localRotation = Quaternion.Euler(-90, 180, 0);
    loaderObject.transform.localScale = Vector3.one * .1f;

    while (RuntimeAudioClipLoader.Manager.GetAudioClipLoadState(c) != AudioDataLoadState.Loaded) {
      yield return null;
    }
    if (loaderObject != null) Destroy(loaderObject);
    masterObj.parent.GetComponent<AudioSource>().PlayOneShot(c, .25f);
  }

  Coroutine _StreamRoutine;
  bool previewing = false;
  void preview(bool on) {
    previewing = on;
    if (on) {
      if (masterObj == null) return;
      if (masterObj.parent == null) return;

      string f = sampleManager.instance.parseFilename(filename);
      if (!File.Exists(f)) return;
      if (_StreamRoutine != null) {
        if (loaderObject != null) Destroy(loaderObject);
        StopCoroutine(_StreamRoutine);
      }
      _StreamRoutine = StartCoroutine(streamRoutine(f));
    } else {
      if (loaderObject != null) Destroy(loaderObject);
      if (_StreamRoutine != null) StopCoroutine(_StreamRoutine);
      if (masterObj != null) {
        if (masterObj.parent != null) masterObj.parent.GetComponent<AudioSource>().Stop();
      }
    }
  }

  GameObject loaderObject;
  public override void setState(manipState state) {
    if (curState == state) return;

    if (previewing) {
      if (previewWhenGrabbed) {
        if (state == manipState.none) preview(false);
      } else if (curState == manipState.selected) preview(false);
    }

    if (curState == manipState.grabbed && state != manipState.grabbed) {
      flashEmptySamplers(false);
      Release();
    }

    curState = state;

    if (curState == manipState.none) {
      highlight.SetActive(false);
    }
    if (curState == manipState.selected) {
      highlight.SetActive(true);
      highlightMat.SetFloat("_EmissionGain", .3f);
      if (deck == null) preview(true);
    }

    if (curState == manipState.grabbed) {
      if (returnRoutineID != null) StopCoroutine(returnRoutineID);
      highlight.SetActive(true);
      flashEmptySamplers(true);
      highlightMat.SetFloat("_EmissionGain", .5f);
      if (insertCoroutine != null) StopCoroutine(insertCoroutine);
      transform.parent = manipulatorObj.parent;
    }
  }
}