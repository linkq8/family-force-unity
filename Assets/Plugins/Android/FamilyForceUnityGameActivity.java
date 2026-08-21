package com.familyforceunity.input;

import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.provider.Settings;
import android.view.InputDevice;
import android.view.KeyEvent;
import android.view.MotionEvent;

import java.util.Locale;

import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerGameActivity;
import java.io.File;

public class FamilyForceUnityGameActivity extends UnityPlayerGameActivity {
    private static final String RECEIVER = "Android Input Bridge";

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
            int keyCode = normalizeKeyCode(event);
            String payload = event.getDeviceId() + "|" + keyCode + "|" +
                    (event.getAction() == KeyEvent.ACTION_DOWN ? "1" : "0");
            UnityPlayer.UnitySendMessage(RECEIVER, "OnNativeKey", payload);
            return true;
        }
        return super.dispatchKeyEvent(event);
    }
}
