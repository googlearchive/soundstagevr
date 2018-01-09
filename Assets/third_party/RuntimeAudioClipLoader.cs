// This is a placeholder script so that SoundStage runs without Runtime AudioClip Loader from the Unity Asset Store
// Without this asset, you can not load MP3 audio files. Audio file loading will also be less performant.
// You can purchase the asset here: http://u3d.as/hEP

using UnityEngine;

public static class RuntimeAudioClipLoader {
	public static class Manager {

    static bool messageShown = false;

    public static AudioClip Load(string filePath, bool doStream = false, bool loadInBackground = true, bool useCache = true) {
      if (!messageShown) {
        messageShown = true;
        MissingMessage();
      }
      WWW www = new WWW("file:///" + filePath);
      AudioClip c = www.GetAudioClip(false, false);
      return c;
    }

    public static AudioDataLoadState GetAudioClipLoadState(AudioClip c) {
      return c.loadState;
    }

    private static void MissingMessage() {
      Debug.Log("Runtime audio loader asset missing (not included in source control)\n" +
        "You can purchase the Runtime AudioClip Loader asset in the Unity Asset Store: http://u3d.as/hEP");
    }
  }
}
