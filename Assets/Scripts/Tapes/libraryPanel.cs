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

public class libraryPanel : UIpanel {
  panelRingComponentInterface _panelRing;

  Renderer rend;
  Transform masterObj;
  float panelRadius = 1;
  Color panelColor = new Color(18 / 255f, 67 / 255f, 96 / 255f, 87 / 255f);

  public GameObject loadingPrefab, ghostTapePrefab, ghostGroupPrefab;
  GameObject loaderObject, ghostTape, ghostGroup;

  bool secondary = false;

  public override void AwakeB() {
    rend = GetComponent<Renderer>();
    masterObj = transform.parent;
    _panelRing = GetComponentInParent<panelRingComponentInterface>();
  }

  string IDtext = "";
  string primaryText = "";
  string secondaryText = "";
  public void Setup(Transform t, float r, int id, string s, bool s2, bool newSetup = true) {
    masterObj = t;
    panelRadius = r;
    buttonID = id;
    label.text = IDtext = s;
    secondary = s2;

    if (secondary && newSetup) {
      Vector3 temp = transform.localScale;
      temp.y *= .6f;
      temp.x *= 1.3f;
      transform.localScale = temp;

      temp = label.transform.localScale;
      temp.x *= .6f;
      temp.y *= 1.3f;
      label.transform.localScale = temp;
    }
  }

  public override void setTextState(bool on) {
    textMat.SetColor("_TintColor", on ? onColor : offColor);
  }

  public void setNewID(int id, string s, bool selected = false) {
    buttonID = id;
    label.text = IDtext = s;
    toggled = selected;
    setToggleAppearance(selected);
    lastZ = -1;
  }

  Vector3 startPos;
  Quaternion startRot;
  public override void grabEvent(bool on) {
    if (on) {
      startRot = transform.parent.localRotation;
      startPos = masterObj.InverseTransformPoint(manipulatorObj.position);
      startPos.x = 0;

      if (secondary) {
        primaryText = _panelRing._deviceInterface.curPrimary;
        secondaryText = IDtext;
        if (ghostTape != null) Destroy(ghostTape);
        ghostTape = Instantiate(ghostTapePrefab, manipulatorObj.transform, false) as GameObject;
        ghostTape.transform.localPosition = Vector3.zero;
        ghostTape.transform.localRotation = Quaternion.Euler(-90, -90, -90);//.zero;
      } else {
        secondaryText = IDtext;
        if (ghostGroup != null) Destroy(ghostGroup);
        ghostGroup = Instantiate(ghostGroupPrefab, manipulatorObj.transform, false) as GameObject;
        ghostGroup.transform.localPosition = Vector3.zero;
        ghostGroup.transform.localRotation = Quaternion.identity;
      }
    } else {
      if (secondary) {
        if (ghostTape != null) Destroy(ghostTape);
      } else if (ghostGroup != null) Destroy(ghostGroup);
    }
  }

  void Update() {
    UpdateRoutine();
  }

  float lastZ = -1;
  public void UpdateRoutine() {

    float z = Mathf.Clamp01(masterObj.InverseTransformPoint(transform.position).z / panelRadius);
    if (lastZ == z) return;
    lastZ = z;

    if (z == 0) {
      Vector3 pos;
      Quaternion rot;
      bool temp = _panelRing.requestNewID(this, buttonID, -(int)Mathf.Sign(masterObj.InverseTransformPoint(transform.position).y), out pos, out rot);

      transform.localPosition = pos;
      transform.localRotation = rot;
      z = Mathf.Clamp01(masterObj.InverseTransformPoint(transform.position).z / panelRadius);

      SetActive(temp);
    }

    if (active && curState == manipState.none) {
      rend.material.color = Color.Lerp(Color.clear, panelColor, z);
      if (toggled) {
        textMat.SetColor("_TintColor", Color.Lerp(Color.clear, onColor, z));
        outlineRender.material.SetColor("_TintColor", Color.Lerp(Color.clear, onColor, z));
      } else textMat.SetColor("_TintColor", Color.Lerp(Color.clear, offColor, z));
    }

    lastZ = z;
  }


  bool active = true;
  public void SetActive(bool on) {
    active = on;
    GetComponent<Collider>().enabled = on;
    GetComponent<Renderer>().enabled = on;
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
    _panelRing.GetComponent<AudioSource>().PlayOneShot(c, .25f);
  }

  public override void selectEvent(bool on) {
    if (!secondary) return;

    if (on) {
      preview(true);
    } else if (previewing) preview(false);
  }

  Coroutine _StreamRoutine;
  bool previewing = false;
  void preview(bool on) {
    previewing = on;
    if (on) {
      string f = _panelRing._deviceInterface.getFilename(IDtext);
      f = sampleManager.instance.parseFilename(_panelRing._deviceInterface.getFilename(IDtext));

      if (!File.Exists(f)) return;
      if (_StreamRoutine != null) {
        if (loaderObject != null) Destroy(loaderObject);
        StopCoroutine(_StreamRoutine);
      }
      _StreamRoutine = StartCoroutine(streamRoutine(f));

    } else {
      if (loaderObject != null) Destroy(loaderObject);
      if (_StreamRoutine != null) StopCoroutine(_StreamRoutine);
      _panelRing.GetComponent<AudioSource>().Stop();

    }
  }


  float lastYdif = 0;
  public override void grabUpdate(Transform t) {
    Vector3 pos = masterObj.InverseTransformPoint(t.position);
    float yDif = (pos.y - startPos.y) * -180 / (Mathf.PI * panelRadius);


    if (!_panelRing._deviceInterface.spinLocks[0] && !_panelRing._deviceInterface.spinLocks[1]) transform.parent.localRotation = Quaternion.Euler(yDif, 0, 0) * startRot;
    else if (_panelRing._deviceInterface.spinLocks[0]) {

      if (yDif < lastYdif) transform.parent.localRotation = Quaternion.Euler(yDif, 0, 0) * startRot;
      else yDif = lastYdif;
    } else if (_panelRing._deviceInterface.spinLocks[1]) {

      if (yDif > lastYdif) transform.parent.localRotation = Quaternion.Euler(yDif, 0, 0) * startRot;
      else yDif = lastYdif;
    }

    lastYdif = yDif;

    if (transform.InverseTransformPoint(t.position).magnitude > 10) {
      if (secondary) _panelRing._deviceInterface.forceTape(t, primaryText, secondaryText);
      else _panelRing._deviceInterface.forceGroup(t, secondaryText);
    }
  }
}