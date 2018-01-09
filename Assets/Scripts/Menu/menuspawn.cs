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

public class menuspawn : MonoBehaviour {
  int controllerIndex = -1;
  bool active = false;

  public GameObject glowNode;
  Material glowRender;

  public menuManager menu;

  public void SetDeviceIndex(int index) {
    controllerIndex = index;
  }

  void Start() {
    glowRender = glowNode.GetComponent<Renderer>().material;
    glowNode.SetActive(false);
    menu = menuManager.instance;
  }

  Coroutine toggleCoroutine;
  public void togglePad() {
    bool on = menu.buttonEvent(controllerIndex, transform);
    if (toggleCoroutine != null) StopCoroutine(toggleCoroutine);
    toggleCoroutine = StartCoroutine(toggleRoutine(on));
  }

  IEnumerator toggleRoutine(bool on) {
    glowNode.SetActive(true);
    Vector3 big = Vector3.one * 2.16f;
    Vector3 small = new Vector3(.01f, 2.16f, .01f);
    float timer = 0;

    if (on) {
      glowNode.transform.localScale = small;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 3);
        glowNode.transform.localScale = Vector3.Lerp(small, big, timer);
        glowRender.SetFloat("_EmissionGain", Mathf.Lerp(.3f, .7f, timer));
        yield return null;
      }
      glowNode.SetActive(false);
    } else {
      glowNode.transform.localScale = small;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 3);
        glowNode.transform.localScale = Vector3.Lerp(small, big, 1 - timer);
        glowRender.SetFloat("_EmissionGain", Mathf.Lerp(.3f, .7f, 1 - timer));
        yield return null;
      }
      glowNode.SetActive(false);
    }
  }
}
