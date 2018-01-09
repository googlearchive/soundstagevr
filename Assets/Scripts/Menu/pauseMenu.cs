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
using UnityEngine.SceneManagement;
using System.IO;
using System;

public class pauseMenu : MonoBehaviour {
  public GameObject savePanel, wireSettingsPanel, settingsLabel;

  public enum itemType {
    main,
    newItem,
    exitItem,
    saveItem,
    loadItem,
    confirmItem,
    cancelItem,
    wireSettingsItem,
    tooltipItem,
    exampleItem
  };

  public GameObject[] items;
  bool active = true;

  public GameObject menuObject;

  List<pauseMenuItem> menuItems;

  const int optionCount = 7;
  void Awake() {
    menuItems = new List<pauseMenuItem>();
    for (int i = 0; i < items.Length; i++) {
      pauseMenuItem temp = items[i].GetComponent<pauseMenuItem>();
      if (temp != null) menuItems.Add(temp);
    }

    menuObject.SetActive(false);
    savePanel.SetActive(false);
  }

  Coroutine flashCoroutine;
  void Start() {
    toggleMenu(false);
  }

  public void endFlash() {
    if (flashCoroutine != null) StopCoroutine(flashCoroutine);
  }

  void mainMenuActive() {
    for (int i = 0; i < optionCount; i++) items[i].SetActive(true);
    items[optionCount].SetActive(false);
  }

  void justItemActive(int n) {
    for (int i = 0; i < optionCount; i++) {
      items[i].SetActive(i == n);
    }
    items[optionCount].SetActive(true);
  }

  public bool GetActive() {
    return active;
  }

  void noneActive() {
    for (int i = 0; i < optionCount; i++) items[i].SetActive(false);
  }

  public void saveFile(string s) {
    if (s == "[new file]") {
      s = masterControl.instance.SaveDir + Path.DirectorySeparatorChar + "Saves" + Path.DirectorySeparatorChar +
          string.Format("{0:MM-dd_hh-mm-ss-tt}.xml",
          DateTime.Now);
    }

    SaveLoadInterface.instance.Save(s);
    mainMenuActive();
    toggleMenu();
    curItem = itemType.main;
  }

  public void loadFile(string s) {
    clearInstruments();
    SaveLoadInterface.instance.Load(s);
    mainMenuActive();
    toggleMenu();
    curItem = itemType.main;
  }

  public void cancelFileMenu() {
    itemSelect(itemType.cancelItem);
  }

  itemType curItem = itemType.main;
  public void itemSelect(itemType t, int ID = -1) {
    if (t == itemType.exitItem) {
      justItemActive(3);
      curItem = itemType.exitItem;
    }
    if (t == itemType.newItem) {
      justItemActive(0);
      curItem = itemType.newItem;
    }
    if (t == itemType.tooltipItem) {
      masterControl.instance.toggleTooltips();
    }
    if (t == itemType.exampleItem) {
      masterControl.instance.toggleExamples();
    }
    if (t == itemType.saveItem) {
      noneActive();
      savePanel.SetActive(true);
      savePanel.GetComponent<uiPanelComponentInterface>().refreshFiles(true);

      curItem = itemType.saveItem;
    }
    if (t == itemType.wireSettingsItem) {
      noneActive();
      wireSettingsPanel.SetActive(true);
      curItem = itemType.wireSettingsItem;
    }
    if (t == itemType.loadItem) {
      noneActive();
      savePanel.SetActive(true);
      savePanel.GetComponent<uiPanelComponentInterface>().refreshFiles(false);

      curItem = itemType.loadItem;
    }
    if (t == itemType.cancelItem) {
      mainMenuActive();
      curItem = itemType.main;
    }
    if (t == itemType.confirmItem) {
      if (curItem == itemType.exitItem) {
        Application.Quit();
        toggleMenu();
      } else if (curItem == itemType.newItem) {
        metronome m = FindObjectOfType<metronome>();
        if (m != null) m.Reset();

        toggleMenu();

        masterControl.instance.currentScene = "";
        clearInstruments();
      }
    }
    return;
  }

  public void clearInstruments() {
    GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("instrument");
    for (int i = 0; i < gameObjects.Length; i++) {
      Destroy(gameObjects[i]);
    }

    if (masterControl.instance.examplesOn) masterControl.instance.toggleExamples();
  }

  public void toggleMenu(bool animated = true) {
    active = !active;
    settingsLabel.SetActive(!active);
    if (!active) {

      savePanel.GetComponent<uiPanelComponentInterface>().cancel();
      wireSettingsPanel.SetActive(false);
      mainMenuActive();
      curItem = itemType.main;
    }

    if (_menuAnimation != null) StopCoroutine(_menuAnimation);

    if (animated) _menuAnimation = StartCoroutine(menuAnimation(active));
    else menuObject.SetActive(active);
  }

  void OnDisable() {
    if (_menuAnimation != null) StopCoroutine(_menuAnimation);
    menuObject.SetActive(active);
  }

  Coroutine _menuAnimation;
  IEnumerator menuAnimation(bool on) {
    if (on) menuObject.SetActive(on);

    if (on) {
      List<int> remaining = new List<int>();
      for (int i = 0; i < menuItems.Count; i++) {
        remaining.Add(i);
        menuItems[i].transform.localScale = Vector3.zero;
      }

      float timer = 0;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 8);
        for (int i = 0; i < menuItems.Count; i++) {
          if (remaining.Contains(i)) {
            if (timer * .3 > Vector3.Distance(menuObject.transform.position, menuItems[i].transform.position)) {
              menuItems[i].Appear(on);
              remaining.Remove(i);
            }
          }
        }
        yield return null;
      }

    } else {
      for (int i = 0; i < menuItems.Count; i++) {
        menuItems[i].Appear(on);
      }
      yield return new WaitForSeconds(1);
    }

    yield return null;
    if (!on) menuObject.SetActive(on);
  }
}