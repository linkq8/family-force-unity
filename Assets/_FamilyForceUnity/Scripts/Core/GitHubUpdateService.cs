using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace FamilyForceUnity.Core
{
    public sealed class GitHubUpdateService : MonoBehaviour
    {
        private const string LatestReleaseApi = "https://api.github.com/repos/linkq8/family-force-unity/releases/latest";

        [Serializable] private sealed class ReleaseAsset { public string name; public string browser_download_url; }
        [Serializable] private sealed class ReleaseInfo { public string tag_name; public ReleaseAsset[] assets; }

        public string Status { get; private set; } = "CHECK FOR UPDATE";
        public bool IsBusy { get; private set; }
        public bool HasUpdate { get; private set; }

        private string downloadUrl;
        private string downloadedApkPath;

        public void Activate()
        {
            if (IsBusy) return;
            if (!string.IsNullOrEmpty(downloadedApkPath) && File.Exists(downloadedApkPath)) LaunchInstaller(downloadedApkPath);
            else if (HasUpdate && !string.IsNullOrEmpty(downloadUrl)) StartCoroutine(DownloadAndInstall());
            else StartCoroutine(CheckLatest());
        }

        private IEnumerator CheckLatest()
        {
            IsBusy = true;
            Status = "CHECKING GITHUB...";
            using var request = UnityWebRequest.Get(LatestReleaseApi);
            request.SetRequestHeader("Accept", "application/vnd.github+json");
            request.SetRequestHeader("User-Agent", "Family-Force-Unity-Updater");
            request.timeout = 15;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Status = "UPDATE CHECK FAILED — CHECK INTERNET";
                IsBusy = false;
                yield break;
            }

            ReleaseInfo release = JsonUtility.FromJson<ReleaseInfo>(request.downloadHandler.text);
            if (release == null || string.IsNullOrEmpty(release.tag_name))
            {
                Status = "UPDATE CHECK FAILED";
                IsBusy = false;
                yield break;
            }

            HasUpdate = IsNewer(release.tag_name, Application.version);
            downloadUrl = null;
            if (HasUpdate && release.assets != null)
            {
                foreach (ReleaseAsset asset in release.assets)
                    if (asset != null && asset.name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.browser_download_url;
                        break;
                    }
            }

            Status = HasUpdate && !string.IsNullOrEmpty(downloadUrl)
                ? $"UPDATE {release.tag_name} AVAILABLE — PRESS CONFIRM"
                : HasUpdate ? "UPDATE FOUND, BUT APK IS MISSING" : "UP TO DATE";
            IsBusy = false;
        }

        private IEnumerator DownloadAndInstall()
        {
            IsBusy = true;
            Status = "DOWNLOADING UPDATE...";
            string apkPath = Path.Combine(Application.temporaryCachePath, "FamilyForceUnity-update.apk");
            using var request = new UnityWebRequest(downloadUrl, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerFile(apkPath)
            };
            request.SetRequestHeader("User-Agent", "Family-Force-Unity-Updater");
            request.timeout = 180;
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                Status = $"DOWNLOADING UPDATE... {Mathf.RoundToInt(request.downloadProgress * 100f)}%";
                yield return null;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Status = "DOWNLOAD FAILED — TRY AGAIN";
                IsBusy = false;
                yield break;
            }

            downloadedApkPath = apkPath;
            LaunchInstaller(apkPath);
            IsBusy = false;
        }

        private void LaunchInstaller(string apkPath)
        {

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                int result = activity.Call<int>("installDownloadedApk", apkPath);
                Status = result == 1 ? "CONFIRM INSTALLATION" :
                    result == 0 ? "ALLOW INSTALLS, THEN PRESS UPDATE AGAIN" : "COULD NOT OPEN INSTALLER";
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Status = "COULD NOT OPEN INSTALLER";
            }
#else
            Status = "APK DOWNLOADED — INSTALLATION IS ANDROID ONLY";
#endif
        }

        private static bool IsNewer(string remoteTag, string localVersion)
        {
            string remote = remoteTag.Trim().TrimStart('v', 'V');
            return Version.TryParse(remote, out Version remoteVersion) &&
                   Version.TryParse(localVersion, out Version installedVersion) &&
                   remoteVersion > installedVersion;
        }
    }
}
