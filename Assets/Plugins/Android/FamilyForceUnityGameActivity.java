package com.familyforceunity.input;

import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.provider.Settings;
import android.view.InputDevice;
import android.view.KeyEvent;
import android.view.MotionEvent;

import java.util.Locale;
import java.util.HashSet;
import java.util.Set;

import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerGameActivity;
import java.io.File;
import java.io.BufferedInputStream;
import java.io.BufferedOutputStream;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.net.HttpURLConnection;
import java.net.URL;

public class FamilyForceUnityGameActivity extends UnityPlayerGameActivity {
    private static final String RECEIVER = "Android Input Bridge";
    private static final Set<Integer> ANNOUNCED_DEVICES = new HashSet<>();
    private long activeDownloadId = -1L;
    private volatile String downloadState = "IDLE";
    private volatile long downloadBytes = 0L;
    private volatile long downloadTotal = 0L;
    private volatile int downloadReason = 0;

    public long beginApkDownload(String url) {
        if ("RUNNING".equals(downloadState)) return activeDownloadId;
        activeDownloadId = System.currentTimeMillis();
        downloadState = "RUNNING";
        downloadBytes = 0L;
        downloadTotal = 0L;
        downloadReason = 0;
        final long requestId = activeDownloadId;
        new Thread(() -> downloadDirect(url, requestId), "FFU-Fast-Downloader").start();
        return requestId;
    }

    public String getApkDownloadStatus(long downloadId) {
        if (downloadId != activeDownloadId) return "FAILED|0|0|-2";
        return downloadState + "|" + downloadBytes + "|" + downloadTotal + "|" + downloadReason;
    }

    private void downloadDirect(String sourceUrl, long requestId) {
        File destination = new File(getExternalFilesDir(android.os.Environment.DIRECTORY_DOWNLOADS),
                "FamilyForceUnity-update.apk");
        File partial = new File(destination.getParentFile(), destination.getName() + ".part");
        try {
            if (partial.exists()) partial.delete();
            URL currentUrl = new URL(sourceUrl);
            HttpURLConnection connection = null;
            for (int redirect = 0; redirect < 6; redirect++) {
                connection = (HttpURLConnection)currentUrl.openConnection();
                connection.setInstanceFollowRedirects(false);
                connection.setConnectTimeout(15000);
                connection.setReadTimeout(30000);
                connection.setRequestProperty("User-Agent", "Family-Force-Unity-Updater/0.10.1");
                connection.setRequestProperty("Accept", "application/octet-stream");
                connection.setRequestProperty("Accept-Encoding", "identity");
                int response = connection.getResponseCode();
                if (response >= 300 && response < 400) {
                    String location = connection.getHeaderField("Location");
                    connection.disconnect();
                    if (location == null) throw new java.io.IOException("Redirect without location");
                    currentUrl = new URL(currentUrl, location);
                    continue;
                }
                if (response < 200 || response >= 300)
                    throw new java.io.IOException("HTTP " + response);
                break;
            }
            if (connection == null) throw new java.io.IOException("No connection");
            downloadTotal = connection.getContentLengthLong();
            byte[] buffer = new byte[256 * 1024];
            try (BufferedInputStream input = new BufferedInputStream(connection.getInputStream(), buffer.length);
                 BufferedOutputStream output = new BufferedOutputStream(new FileOutputStream(partial), buffer.length)) {
                int count;
                while ((count = input.read(buffer)) >= 0) {
                    if (requestId != activeDownloadId) throw new java.io.IOException("Download replaced");
                    output.write(buffer, 0, count);
                    downloadBytes += count;
                }
                output.flush();
            } finally {
                connection.disconnect();
            }
            if (destination.exists()) destination.delete();
            if (!partial.renameTo(destination)) {
                try (FileInputStream input = new FileInputStream(partial);
                     FileOutputStream output = new FileOutputStream(destination)) {
                    byte[] copyBuffer = new byte[256 * 1024];
                    int count;
                    while ((count = input.read(copyBuffer)) >= 0) output.write(copyBuffer, 0, count);
                }
                partial.delete();
            }
            downloadState = "SUCCESS";
        } catch (Exception exception) {
            exception.printStackTrace();
            downloadReason = exception.getClass().getSimpleName().hashCode();
            downloadState = "FAILED";
        }
    }

    private static String clean(String value) {
        return value == null ? "" : value.replace('|', '/');
    }

    private static String hostFamily() {
        String identity = (Build.MANUFACTURER + " " + Build.BRAND + " " + Build.MODEL)
                .toLowerCase(Locale.ROOT);
        if (identity.contains("xiaomi") || identity.contains("mitv")) return "XIAOMI_TV";
        if (identity.contains("nvidia") || identity.contains("shield")) return "NVIDIA_SHIELD";
        return "ANDROID";
    }

    private static void announceDevice(int deviceId) {
        if (ANNOUNCED_DEVICES.contains(deviceId)) return;
        InputDevice device = InputDevice.getDevice(deviceId);
        if (device == null) return;
        ANNOUNCED_DEVICES.add(deviceId);
        String payload = deviceId + "|" + hostFamily() + "|" +
                clean(Build.MANUFACTURER) + "|" + clean(Build.MODEL) + "|" +
                clean(device.getName()) + "|" + device.getVendorId() + "|" +
                device.getProductId() + "|" + clean(device.getDescriptor());
        UnityPlayer.UnitySendMessage(RECEIVER, "OnNativeDevice", payload);
    }

    public int installDownloadedApk(String apkPath) {
        try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O &&
                    !getPackageManager().canRequestPackageInstalls()) {
                Intent settingsIntent = new Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES,
                        Uri.parse("package:" + getPackageName()));
                startActivity(settingsIntent);
                return 0;
            }

            Uri apkUri = Uri.parse("content://" + getPackageName() + ".updates/update.apk");
            Intent installIntent = new Intent(Intent.ACTION_VIEW);
            installIntent.setDataAndType(apkUri, "application/vnd.android.package-archive");
            installIntent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_ACTIVITY_NEW_TASK);
            startActivity(installIntent);
            return 1;
        } catch (Exception exception) {
            exception.printStackTrace();
            return -1;
        }
    }

    private static boolean isControllerSource(int source) {
        return (source & InputDevice.SOURCE_GAMEPAD) == InputDevice.SOURCE_GAMEPAD ||
               (source & InputDevice.SOURCE_JOYSTICK) == InputDevice.SOURCE_JOYSTICK;
    }

    private static boolean isXiaomiHost() {
        String identity = (Build.MANUFACTURER + " " + Build.BRAND + " " + Build.MODEL)
                .toLowerCase(Locale.ROOT);
        return identity.contains("xiaomi") || identity.contains("mitv");
    }

    private static boolean isLinuxGamepadScanCode(int scanCode) {
        return scanCode >= 304 && scanCode <= 318;
    }

    private static int mapAospGamepadScanCode(int scanCode) {
        switch (scanCode) {
            case 304: return KeyEvent.KEYCODE_BUTTON_A;
            case 305: return KeyEvent.KEYCODE_BUTTON_B;
            case 306: return KeyEvent.KEYCODE_BUTTON_C;
            case 307: return KeyEvent.KEYCODE_BUTTON_X;
            case 308: return KeyEvent.KEYCODE_BUTTON_Y;
            case 309: return KeyEvent.KEYCODE_BUTTON_Z;
            case 310: return KeyEvent.KEYCODE_BUTTON_L1;
            case 311: return KeyEvent.KEYCODE_BUTTON_R1;
            case 312: return KeyEvent.KEYCODE_BUTTON_L2;
            case 313: return KeyEvent.KEYCODE_BUTTON_R2;
            case 314: return KeyEvent.KEYCODE_BUTTON_SELECT;
            case 315: return KeyEvent.KEYCODE_BUTTON_START;
            case 316: return KeyEvent.KEYCODE_BUTTON_MODE;
            case 317: return KeyEvent.KEYCODE_BUTTON_THUMBL;
            case 318: return KeyEvent.KEYCODE_BUTTON_THUMBR;
            default: return KeyEvent.KEYCODE_UNKNOWN;
        }
    }

    private static boolean isXiaomiRawGamepadEvent(KeyEvent event) {
        return isXiaomiHost() && isLinuxGamepadScanCode(event.getScanCode());
    }

    private static int normalizeKeyCode(KeyEvent event) {
        if (!isXiaomiRawGamepadEvent(event)) {
            return event.getKeyCode();
        }

        int mappedKeyCode = mapAospGamepadScanCode(event.getScanCode());
        if (mappedKeyCode == KeyEvent.KEYCODE_UNKNOWN) {
            return event.getKeyCode();
        }
        return mappedKeyCode;
    }

    private static boolean isControllerKey(KeyEvent event) {
        int keyCode = event.getKeyCode();
        if (isXiaomiRawGamepadEvent(event)) {
            return true;
        }
        if (!isControllerSource(event.getSource())) {
            return false;
        }
        return KeyEvent.isGamepadButton(keyCode) ||
               keyCode == KeyEvent.KEYCODE_DPAD_UP || keyCode == KeyEvent.KEYCODE_DPAD_DOWN ||
               keyCode == KeyEvent.KEYCODE_DPAD_LEFT || keyCode == KeyEvent.KEYCODE_DPAD_RIGHT;
    }

    @Override
    public boolean dispatchGenericMotionEvent(MotionEvent event) {
        if (isControllerSource(event.getSource())) {
            announceDevice(event.getDeviceId());
            String payload = event.getDeviceId() + "|" +
                    event.getAxisValue(MotionEvent.AXIS_X) + "|" +
                    event.getAxisValue(MotionEvent.AXIS_Y) + "|" +
                    event.getAxisValue(MotionEvent.AXIS_HAT_X) + "|" +
                    event.getAxisValue(MotionEvent.AXIS_HAT_Y) + "|" +
                    event.getAxisValue(MotionEvent.AXIS_Z) + "|" +
                    event.getAxisValue(MotionEvent.AXIS_RZ) + "|" +
                    event.getAxisValue(MotionEvent.AXIS_LTRIGGER) + "|" +
                    event.getAxisValue(MotionEvent.AXIS_RTRIGGER);
            UnityPlayer.UnitySendMessage(RECEIVER, "OnNativeMotion", payload);
            return true;
        }
        return super.dispatchGenericMotionEvent(event);
    }

    @Override
    public boolean dispatchKeyEvent(KeyEvent event) {
        if (isControllerKey(event)) {
            announceDevice(event.getDeviceId());
            int keyCode = normalizeKeyCode(event);
            String payload = event.getDeviceId() + "|" + keyCode + "|" +
                    (event.getAction() == KeyEvent.ACTION_DOWN ? "1" : "0");
            UnityPlayer.UnitySendMessage(RECEIVER, "OnNativeKey", payload);
            return true;
        }
        return super.dispatchKeyEvent(event);
    }
}
