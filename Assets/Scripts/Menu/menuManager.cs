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

public class menuManager : MonoBehaviour {
  public GameObject item;
  public GameObject rootNode;
  public GameObject trashNode;
  public GameObject settingsNode;
  public GameObject metronomeNode;
  public GameObject[] menuItems;

  public Dictionary<menuItem.deviceType, GameObject> refObjects;

  public AudioSource _audioSource;
  public AudioClip openClip;
  public AudioClip closeClip;
  public AudioClip selectClip;
  public AudioClip grabClip;
  public AudioClip simpleOpenClip;

  menuItem[] menuItemScripts;
  public static menuManager instance;

  bool active = false;
  int lastController = -1;

  public bool loaded = false;

  void Awake() {
    instance = this;
    refObjects = new Dictionary<menuItem.deviceType, GameObject>();
    _audioSource = GetComponent<AudioSource>();
    loadMenu();
    loadNonMenuItems();
    loaded = true;
    Activate(false, transform);

    if (!PlayerPrefs.HasKey("midiOut")) PlayerPrefs.SetInt("midiOut", 0);
    if (PlayerPrefs.GetInt("midiOut") == 1) {
      toggleMidiOut(true);
    }
  }

  void loadNonMenuItems() {
    GameObject temp = Instantiate(item, Vector3.zero, Quaternion.identity) as GameObject;
    temp.transform.parent = rootNode.transform;
    menuItem m = temp.GetComponent<menuItem>();
    refObjects[menuItem.deviceType.TapeGroup] = m.Setup(menuItem.deviceType.TapeGroup);
    temp.SetActive(false);

    temp = Instantiate(item, Vector3.zero, Quaternion.identity) as GameObject;
    temp.transform.parent = rootNode.transform;
    m = temp.GetComponent<menuItem>();
    refObjects[menuItem.deviceType.Pano] = m.Setup(menuItem.deviceType.Pano);
    temp.SetActive(false);
  }

  public void SetMenuActive(bool on) {
    active = on;
  }

  int rowLength = 5;

  void loadMenu() {
    menuItems = new GameObject[(int)menuItem.deviceType.Max];
    menuItemScripts = new menuItem[menuItems.Length];
    for (int i = 0; i < menuItems.Length; i++) {
      menuItems[i] = Instantiate(item, Vector3.zero, Quaternion.identity) as GameObject;
      menuItems[i].transform.parent = rootNode.transform;
      menuItem m = menuItems[i].GetComponent<menuItem>();
      refObjects[(menuItem.deviceType)i] = m.Setup((menuItem.deviceType)i);
      menuItemScripts[i] = m;
    }

    int tempCount = 0;
    float h = 0;
    float arc = 37.5f;
    while (tempCount < menuItems.Length) {
      for (int i = 0; i < rowLength; i++) {
        if (tempCount < menuItems.Length) {
          menuItems[tempCount].transform.localPosition = Quaternion.Euler(0, (arc / rowLength) * (i - rowLength / 2f) + (arc / rowLength) / 2f, 0) * (Vector3.forward * -.5f) - (Vector3.forward * -.5f) + Vector3.up * h;
          menuItems[tempCount].transform.rotation = Quaternion.Euler(0, (arc / rowLength) * (i - rowLength / 2f) + (arc / rowLength) / 2f, 0);
        }
        tempCount++;
      }
      h += 0.07f;
    }

    metronomeNode.transform.localPosition = Quaternion.Euler(0, -arc / 2 - 10, 0) * (Vector3.forward * -.5f) - (Vector3.forward * -.5f) + Vector3.up * .014f;
    metronomeNode.transform.rotation = Quaternion.Euler(0, -arc / 2 - 10, 0);
    settingsNode.transform.localPosition = Quaternion.Euler(0, arc / 2 + 10, 0) * (Vector3.forward * -.5f) - (Vector3.forward * -.5f);
    settingsNode.transform.rotation = Quaternion.Euler(0, arc / 2 + 10, 0);
  }

  public void SelectAudio() {
    _audioSource.PlayOneShot(selectClip, .05f);
  }

  public void GrabAudio() {
    _audioSource.PlayOneShot(grabClip, .75f);
  }

  public bool midiOutEnabled = false;
  float openSpeed = 3;
  public void toggleMidiOut(bool on) {
    PlayerPrefs.SetInt("midiOut", on ? 1 : 0);
    midiOutEnabled = on;
    openSpeed = on ? 2 : 3;
    menuItemScripts[menuItemScripts.Length - 1].Appear(on);
  }

  Coroutine activationCoroutine;
  IEnumerator activationRoutine(bool on, Transform pad) {
    float timer = 0;
    if (on) {
      _audioSource.PlayOneShot(openClip);

      rootNode.SetActive(true);
      trashNode.SetActive(false);
      settingsNode.SetActive(false);
      metronomeNode.SetActive(false);

      List<int> remaining = new List<int>();
      for (int i = 0; i < menuItemScripts.Length; i++) {
        remaining.Add(i);
        menuItemScripts[i].transform.localScale = Vector3.zero;
      }
      Vector3 startPos = transform.position = pad.position;

      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 1.5f);
        for (int i = 0; i < menuItemScripts.Length; i++) {
          if (remaining.Contains(i)) {
            if (timer / openSpeed > Vector3.Distance(startPos, menuItemScripts[i].transform.position)) {
              menuItemScripts[i].Appear(on);
              remaining.Remove(i);
            }
          }
        }
        transform.position = startPos + Vector3.Lerp(Vector3.zero, Vector3.up * .025f, timer);
        Vector3 camPos = Camera.main.transform.position;
        camPos.y -= .2f;
        transform.LookAt(camPos, Vector3.up);
        yield return null;
      }
      trashNode.SetActive(true);
      settingsNode.SetActive(true);
      metronomeNode.SetActive(true);
    } else {
      _audioSource.PlayOneShot(closeClip);
      Vector3 startPos = transform.position;
      trashNode.SetActive(false);
      settingsNode.SetActive(false);
      metronomeNode.SetActive(false);
      for (int i = 0; i < menuItems.Length; i++) {
        menuItemScripts[i].Appear(on);
      }

      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 4);
        transform.position = Vector3.Lerp(startPos, pad.position, timer);
        yield return null;
      }
      rootNode.SetActive(false);
    }
  }

  void Activate(bool on, Transform pad) {
    active = on;
    if (activationCoroutine != null) StopCoroutine(activationCoroutine);
    activationCoroutine = StartCoroutine(activationRoutine(on, pad));
  }

  void SimpleActivate(bool on, Transform pad) {
    active = on;
    simpleMenu.toggleMenu();

    if (on) _audioSource.PlayOneShot(simpleOpenClip);
    else _audioSource.PlayOneShot(closeClip);
    if (!active) return;

    transform.position = pad.position;
    Vector3 camPos = Camera.main.transform.position;
    camPos.y = transform.position.y;
    transform.LookAt(camPos);
  }

  public bool simple = false;
  public pauseMenu simpleMenu;

  public bool buttonEvent(int controller, Transform pad) {
    bool on = true;

    if (controller != lastController) {
      if (!simple) Activate(true, pad);
      else SimpleActivate(true, pad);
    } else {
      if (!simple) Activate(!active, pad);
      else SimpleActivate(!active, pad);
      on = active;
    }

    lastController = controller;
    return on;
  }
}
