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

public class drumstick : manipObject {

  public Transform masterObj;
  Transform glowTrans;
  Material mat;
  public Transform sticktip;
  List<drumpad> pads;
  List<Vector3> lastStickPos;
  public int ID = 1;

  Color glowColor = new Color(.25f, .5f, .25f);
  Vector3 origPos = Vector3.zero;
  Quaternion origRot = Quaternion.identity;
  GameObject highlight;
  Material highlightMat;
  public componentInterface _interface;

  public bool skinnable = true;

  public override void Awake() {
    base.Awake();
    gameObject.layer = 10; //manipulator
    pads = new List<drumpad>();
    lastStickPos = new List<Vector3>();
    stickyGrip = true;

    if (masterObj == null) masterObj = transform.parent;
    glowTrans = transform.GetChild(0);
    mat = glowTrans.GetComponent<Renderer>().material;
    mat.SetColor("_TintColor", glowColor * .5f);
    mat.SetFloat("_EmissionGain", .5f);
    glowTrans.gameObject.SetActive(false);
    origPos = transform.localPosition;
    origRot = transform.localRotation;
    createHandleFeedback();
  }

  GameObject skin;
  public void addSkin(GameObject g) {
    if (!skinnable) return;
    skin = Instantiate(g, transform) as GameObject;
    skin.transform.localPosition = Vector3.zero;
    skin.transform.localRotation = Quaternion.identity;
    skin.transform.localScale = Vector3.one;
  }

  public void removeSkin() {
    if (skin != null) Destroy(skin);
  }

  void OnCollisionEnter(Collision coll) {
    manipObject o = coll.transform.GetComponent<manipObject>();
    if (o != null) o.onTouch(true, manipulatorObjScript);
  }

  void OnCollisionExit(Collision coll) {
    manipObject o = coll.transform.GetComponent<manipObject>();
    if (o != null) o.onTouch(false, manipulatorObjScript);
  }


  void createHandleFeedback() {
    highlight = new GameObject("highlight");

    MeshFilter m = highlight.AddComponent<MeshFilter>();

    m.mesh = GetComponent<MeshFilter>().mesh;
    MeshRenderer r = highlight.AddComponent<MeshRenderer>();
    r.material = Resources.Load("Materials/Highlight") as Material;
    highlightMat = r.material;

    highlight.transform.SetParent(transform, false);

    highlight.transform.localScale = new Vector3(1.6f, 1.6f, 1.02f);
    highlight.transform.localPosition = new Vector3(0, 0, -.003f);
    highlightMat.SetColor("_TintColor", glowColor);
    highlightMat.SetFloat("_EmissionGain", .3f);

    highlight.SetActive(false);
  }

  public override void grabUpdate(Transform t) {
    for (int i = 0; i < pads.Count; i++) {
      Vector3 pos = pads[i].transform.parent.InverseTransformPoint(sticktip.position);
      Vector2 posFlat = new Vector2(pos.x, pos.z);

      if (posFlat.magnitude < .175f) {
        if (lastStickPos[i].y > -.004f && pos.y <= -.004f) {
          pads[i].keyHit(true);
          if (manipulatorObjScript != null) manipulatorObjScript.bigHaptic(3999, .1f);
        }
      }
      lastStickPos[i] = pos;
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

  bool revealing = false;
  Coroutine _revealSelfRoutine;
  public void revealSelf(bool on) {
    if (!gameObject.activeSelf) return;

    if (revealing == on) return;
    revealing = on;

    if (_revealSelfRoutine != null) StopCoroutine(_revealSelfRoutine);

    _revealSelfRoutine = StartCoroutine(revealSelfRoutine(on));
  }

  float curRoutineTime = 0;
  IEnumerator revealSelfRoutine(bool on) {
    float a = -.05f;
    float b = .02f;

    Quaternion quatA = Quaternion.Euler(0, 145 * ID, 180);
    Quaternion quatB = Quaternion.Euler(0, 145 * ID, 0);

    if (on) {
      while (curRoutineTime < 1) {
        curRoutineTime = Mathf.Clamp01(curRoutineTime + Time.deltaTime * 2);
        Vector3 pos = origPos;
        pos.y = Mathf.SmoothStep(a, b, curRoutineTime);
        Quaternion rot = Quaternion.Lerp(quatA, quatB, curRoutineTime);
        updateRestingPosition(pos, rot);
        yield return null;
      }
    } else {
      while (curRoutineTime > 0) {
        curRoutineTime = Mathf.Clamp01(curRoutineTime - Time.deltaTime * 2);
        Vector3 pos = origPos;
        pos.y = Mathf.SmoothStep(a, b, curRoutineTime);
        Quaternion rot = Quaternion.Lerp(quatA, quatB, curRoutineTime);
        updateRestingPosition(pos, rot);
        yield return null;
      }
    }
  }

  public void updateRestingPosition(Vector3 v, Quaternion r) {
    origPos = v;
    origRot = r;
    if (curState != manipState.grabbed) {
      transform.localPosition = v;
      transform.localRotation = r;
    }
  }

  public override void setState(manipState state) {
    if (curState == state) return;

    if (curState == manipState.grabbed) {
      transform.parent = masterObj;
      glowTrans.gameObject.SetActive(false);
      returnRoutineID = StartCoroutine(returnRoutine());
      if (_interface != null) _interface.onGrab(false, -1);
    }

    curState = state;

    if (curState == manipState.none) {
      highlight.SetActive(false);
    } else if (curState == manipState.selected) {
      highlight.SetActive(true);
    } else if (curState == manipState.grabbed) {
      if (_interface != null) _interface.onGrab(true, -1);
      if (returnRoutineID != null) StopCoroutine(returnRoutineID);
      highlight.SetActive(false);
      glowTrans.gameObject.SetActive(true);
      transform.parent = manipulatorObj.parent;

      transform.localPosition = Vector3.zero;
      transform.localRotation = Quaternion.identity;

      if (manipulatorObjScript != null) manipulatorObjScript.setVerticalPosition(transform);

      pads.Clear();
      lastStickPos.Clear();
      pads = FindObjectsOfType<drumpad>().ToList();
      for (int i = 0; i < pads.Count; i++) lastStickPos.Add(pads[i].transform.parent.InverseTransformPoint(sticktip.position));
    }
  }
}
