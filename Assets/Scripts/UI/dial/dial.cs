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

public class dial : manipObject {

  public float percent = 0f;

  glowDisk dialFeedback;
  Material[] mats;

  public Color customColor;

  public bool externalHue = false;
  public float hue = .5f;

  GameObject littleDisk;

  public override void Awake() {
    base.Awake();
    littleDisk = transform.FindChild("littleDisk").gameObject;

    mats = new Material[3];
    mats[0] = littleDisk.GetComponent<Renderer>().material;
    mats[1] = transform.parent.FindChild("glowDisk").GetComponent<Renderer>().material;
    mats[2] = transform.parent.FindChild("Label").GetComponent<Renderer>().material;

    customColor = Color.HSVToRGB(hue, 227 / 255f, 206 / 255f);

    setGlowState(manipState.none);

    dialFeedback = transform.parent.FindChild("glowDisk").GetComponent<glowDisk>();
  }

  void setGlowState(manipState s) {
    Color c = customColor;

    switch (s) {
      case manipState.none:
        littleDisk.SetActive(false);

        for (int i = 0; i < mats.Length; i++) {
          mats[i].SetFloat("_EmissionGain", .1f);
          mats[i].SetColor("_TintColor", c);
        }
        break;
      case manipState.selected:
        littleDisk.SetActive(true);
        for (int i = 0; i < mats.Length; i++) {
          mats[i].SetFloat("_EmissionGain", .3f);
          mats[i].SetColor("_TintColor", c);
        }
        break;
      case manipState.grabbed:
        littleDisk.SetActive(true);

        for (int i = 0; i < mats.Length; i++) {
          mats[i].SetFloat("_EmissionGain", .25f);
          mats[i].SetColor("_TintColor", masterControl.instance.tipColor);
        }
        break;
      default:
        break;
    }
  }


  void updateMatsColor(Color c) {
    foreach (Material m in mats) m.SetColor("_TintColor", c);
  }

  void Start() {
    setPercent(percent);
  }

  void Update() {
    updatePercent();
  }

  public void setPercent(float p) {
    percent = Mathf.Clamp01(p);
    if (p >= 0.5f) realRot = (percent - .5f) * 300;
    else realRot = percent * 300 + 210;

    curRot = realRot / 2f;
    transform.localRotation = Quaternion.Euler(0, realRot, 0);
  }

  void updatePercent() {
    if (realRot < 180) percent = .5f + realRot / 300;
    else percent = (realRot - 210) / 300;

    dialFeedback.percent = percent * 0.85f;
    dialFeedback.PercentUpdate();
  }

  Vector2 dialCoordinates(Vector3 vec) {
    Vector3 flat = transform.parent.InverseTransformDirection(Vector3.ProjectOnPlane(vec, transform.parent.up));
    return new Vector2(flat.x, flat.z);
  }

  float deltaRot = 0;
  float curRot = 0;
  float realRot = 0f;
  float prevShakeRot = 0f;
  public override void grabUpdate(Transform t) {
    Vector2 temp = dialCoordinates(t.right);
    curRot = Vector2.Angle(temp, Vector2.up) * Mathf.Sign(temp.x) - deltaRot;

    curRot = Mathf.Repeat(curRot, 360f);
    realRot = Mathf.Repeat(curRot * 2, 360f);

    if (realRot > 150 && realRot < 210) {
      if (realRot < 180) realRot = 150;
      else realRot = 210;
    }
    transform.localRotation = Quaternion.Euler(0, realRot, 0);

    if (Mathf.Abs(realRot - prevShakeRot) > 10f) {
      if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(500);
      prevShakeRot = realRot;
      turnCount++;
    }
  }

  int turnCount = 0;

  Coroutine _dialCheckRoutine;
  IEnumerator dialCheckRoutine() {
    Vector3 lastPos = Vector3.zero;
    float cumulative = 0;
    if (manipulatorObj != null) {
      lastPos = manipulatorObj.position;
    }

    while (curState == manipState.grabbed && manipulatorObj != null && !masterControl.instance.dialUsed) {
      cumulative += Vector3.Magnitude(manipulatorObj.position - lastPos);
      lastPos = manipulatorObj.position;

      if (turnCount > 3) masterControl.instance.dialUsed = true;
      else if (cumulative > .2f) {
        masterControl.instance.dialUsed = true;
        Instantiate(Resources.Load("Hints/TurnVignette", typeof(GameObject)), transform.parent, false);
      }

      yield return null;
    }
    yield return null;
  }

  public override void setState(manipState state) {
    curState = state;
    setGlowState(state);

    if (curState == manipState.grabbed) {
      turnCount = 0;
      if (!masterControl.instance.dialUsed) {
        if (_dialCheckRoutine != null) StopCoroutine(_dialCheckRoutine);
        _dialCheckRoutine = StartCoroutine(dialCheckRoutine());
      }
      Vector2 temp = dialCoordinates(manipulatorObj.right);
      deltaRot = Vector2.Angle(temp, Vector2.up) * Mathf.Sign(temp.x) - curRot;
    }
  }
}
