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

public class maracaUI : manipObject {
  public Transform masterObj;
  Transform glowTrans;
  Material mat;

  public float shakeVal = 0;
  GameObject highlight;
  Material highlightMat;

  Color glowColor = Color.HSVToRGB(0.1f, 0.7f, 0.1f);
  Vector3 origPos = Vector3.zero;

  public override void Awake() {
    base.Awake();
    if (masterObj == null) masterObj = transform.parent;
    glowTrans = transform.GetChild(0);
    stickyGrip = true;
    mat = glowTrans.GetComponent<Renderer>().material;
    mat.SetColor("_TintColor", glowColor);
    glowTrans.gameObject.SetActive(false);
    origPos = transform.localPosition;
    createHandleFeedback();
  }

  void createHandleFeedback() {
    highlight = new GameObject("highlight");

    MeshFilter m = highlight.AddComponent<MeshFilter>();

    m.mesh = GetComponent<MeshFilter>().mesh;
    MeshRenderer r = highlight.AddComponent<MeshRenderer>();
    r.material = Resources.Load("Materials/Highlight") as Material;
    highlightMat = r.material;

    highlight.transform.SetParent(transform, false);

    highlight.transform.localScale = new Vector3(1.15f, 1.05f, 1.1f);
    highlight.transform.localPosition = new Vector3(0, -.0025f, 0);
    highlightMat.SetColor("_TintColor", glowColor);
    highlightMat.SetFloat("_EmissionGain", .75f);

    highlight.SetActive(false);
  }

  public Vector3 instantVelocity = Vector3.zero;
  public Vector3 lastInstantVelocity = Vector3.zero;
  public float instantAcceleration = 0;
  Vector3 lastpos = Vector3.zero;
  void Update() {
    if (curState == manipState.grabbed) {
      instantVelocity = transform.position - lastpos;
      instantAcceleration = Mathf.Clamp01(Vector3.Distance(instantVelocity, lastInstantVelocity) * 100);
      lastpos = transform.position;
      lastInstantVelocity = instantVelocity;
    } else {
      instantAcceleration = 0;
    }
    shakeVal = Mathf.Lerp(instantAcceleration, shakeVal, 0.85f);
    if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse((ushort)(1500f * shakeVal));
    mat.SetFloat("_EmissionGain", .5f + (.5f * shakeVal));
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
      transform.parent = masterObj;
      glowTrans.gameObject.SetActive(false);
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
      highlight.SetActive(false);
      glowTrans.gameObject.SetActive(true);
      transform.parent = manipulatorObj.parent;

      transform.localPosition = Vector3.zero;
      transform.localRotation = Quaternion.identity;

      if (manipulatorObjScript != null) manipulatorObjScript.setVerticalPosition(transform);
      transform.Rotate(90, 0, 0, Space.Self);
    }
  }
}
