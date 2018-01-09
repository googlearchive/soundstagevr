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

﻿using UnityEngine;
using System.Collections;

public class airhornUI : manipObject {
  public Transform masterObj;
  public GameObject[] buttonOverlays;
  public GameObject[] buttonOutlines;

  public GameObject canObject;
  GameObject highlight;
  Material highlightMat;

  Color glowColor = Color.HSVToRGB(0f, 0.7f, 0.1f);
  Vector3 origPos = Vector3.zero;

  public airhornDeviceInterface _deviceInterface;
  public Transform touchFeedbackTransform;

  public override void Awake() {
    base.Awake();
    if (masterObj == null) masterObj = transform.parent;
    stickyGrip = true;
    origPos = transform.localPosition;
    createHandleFeedback();

    touchFeedbackTransform.GetComponent<Renderer>().material.SetColor("_TintColor", Color.HSVToRGB(0f, 174f / 255f, 156f / 255f));
    touchFeedbackTransform.GetComponent<Renderer>().material.SetFloat("_EmissionGain", .6f);
    for (int i = 0; i < 4; i++) {
      buttonOutlines[i].GetComponent<Renderer>().material.SetColor("_TintColor", Color.HSVToRGB(0f, 174f / 255f, 156f / 255f));
      buttonOverlays[i].GetComponent<Renderer>().material.SetColor("_TintColor", Color.HSVToRGB(0f, 174f / 255f, 156f / 255f));
      buttonOutlines[i].GetComponent<Renderer>().material.SetFloat("_EmissionGain", .6f);
      buttonOverlays[i].GetComponent<Renderer>().material.SetFloat("_EmissionGain", .6f);
    }
  }

  public override void setPress(bool on) {
    _deviceInterface.PlaySample(on, curSelection);
    for (int i = 0; i < 4; i++) buttonOverlays[i].SetActive(i == curSelection && on);
  }

  public override void setTouch(bool on) {
    touchFeedbackTransform.gameObject.SetActive(on);

    for (int i = 0; i < 4; i++) {
      buttonOutlines[i].SetActive(i == curSelection && on);
      if (!on) buttonOverlays[i].SetActive(false);
    }
  }

  int curSelection = 0;
  public override void updateTouchPos(Vector2 p) {
    touchFeedbackTransform.localPosition = new Vector3(p.x * .013f, p.y * .013f, -0.0005f);
    if (Vector2.SqrMagnitude(p) < .1f) return;

    float angle = 0;
    if (p.x < 0) angle = 360 - (Mathf.Atan2(p.x, p.y) * Mathf.Rad2Deg * -1);
    else angle = Mathf.Atan2(p.x, p.y) * Mathf.Rad2Deg;

    if (angle >= 315 || angle < 45) curSelection = 0;
    else if (angle >= 45 && angle < 135) curSelection = 1;
    else if (angle >= 135 && angle < 225) curSelection = 2;
    else curSelection = 3;

    for (int i = 0; i < 4; i++) buttonOutlines[i].SetActive(i == curSelection);
  }

  void createHandleFeedback() {
    highlight = new GameObject("highlight");
    MeshFilter m = highlight.AddComponent<MeshFilter>();
    m.mesh = canObject.GetComponent<MeshFilter>().mesh;
    MeshRenderer r = highlight.AddComponent<MeshRenderer>();
    r.material = Resources.Load("Materials/Highlight") as Material;
    highlightMat = r.material;

    highlight.transform.SetParent(transform, false);
    highlight.transform.localScale = new Vector3(1.15f, 1.05f, 1.1f);
    highlight.transform.position = canObject.transform.position;// new Vector3(0,-.0025f,0);
    highlightMat.SetColor("_TintColor", glowColor);
    highlightMat.SetFloat("_EmissionGain", .75f);
    highlight.SetActive(false);
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
      transform.localRotation = Quaternion.Lerp(curRot, Quaternion.identity, modT);
      yield return null;
    }
  }

  public override void setState(manipState state) {
    if (curState == state) return;

    if (curState == manipState.grabbed && state != manipState.grabbed) {
      setTouch(false);
      transform.parent = masterObj;
      if (returnRoutineID != null) StopCoroutine(returnRoutineID);
      returnRoutineID = StartCoroutine(returnRoutine());
    }

    curState = state;

    if (curState == manipState.none) {
      highlight.SetActive(false);
    }
    if (curState == manipState.selected) {
      highlight.SetActive(true);
    }
    if (curState == manipState.grabbed) {
      if (returnRoutineID != null) StopCoroutine(returnRoutineID);
      manipulatorObjScript.toggleController(false);
      highlight.SetActive(false);
      transform.parent = manipulatorObj.parent;

      transform.localPosition = Vector3.zero;
      transform.localRotation = Quaternion.identity;

      if (manipulatorObjScript != null) manipulatorObjScript.setVerticalPosition(transform);
      transform.Rotate(45, 0, 0, Space.Self);
    }
  }
}
