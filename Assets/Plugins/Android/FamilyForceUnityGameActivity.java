package com.familyforceunity.input;

import android.os.Build;
import android.view.InputDevice;
import android.view.KeyEvent;
import android.view.MotionEvent;

import java.util.Locale;

import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerGameActivity;

public class FamilyForceUnityGameActivity extends UnityPlayerGameActivity {
    private static final String RECEIVER = "Android Input Bridge";

    private static boolean isControllerSource(int source) {
        return (source & InputDevice.SOURCE_GAMEPAD) == InputDevice.SOURCE_GAMEPAD ||
               (source & InputDevice.SOURCE_JOYSTICK) == InputDevice.SOURCE_JOYSTICK;
    }

    private static boolean isXiaomiHost() {
        String identity = (Build.MANUFACTURER + " " + Build.BRAND + " " + Build.MODEL)
                .toLowerCase(Locale.ROOT);
        return identity.contains("xiaomi") || identity.contains("mitv");
    }

    private static boolean isPlayStationController(InputDevice device) {
        if (device == null) {
            return false;
        }

        if (device.getVendorId() == 0x054c) {
            return true;
        }

        String name = device.getName().toLowerCase(Locale.ROOT);
        return name.contains("dualsense") || name.contains("dualshock") ||
               name.contains("playstation") || name.contains("sony") ||
               name.contains("wireless controller");
    }

    private static int normalizeKeyCode(InputDevice device, int keyCode) {
        if (!isXiaomiHost() || !isPlayStationController(device)) {
            return keyCode;
        }

        if (keyCode == KeyEvent.KEYCODE_BUTTON_A) {
            return KeyEvent.KEYCODE_BUTTON_X;
        }
        if (keyCode == KeyEvent.KEYCODE_BUTTON_X) {
            return KeyEvent.KEYCODE_BUTTON_A;
        }
        return keyCode;
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
        int rawKeyCode = event.getKeyCode();
        boolean controllerKey = isControllerSource(event.getSource()) &&
                (KeyEvent.isGamepadButton(rawKeyCode) ||
                 rawKeyCode == KeyEvent.KEYCODE_DPAD_UP || rawKeyCode == KeyEvent.KEYCODE_DPAD_DOWN ||
                 rawKeyCode == KeyEvent.KEYCODE_DPAD_LEFT || rawKeyCode == KeyEvent.KEYCODE_DPAD_RIGHT);
        if (controllerKey) {
            int keyCode = normalizeKeyCode(event.getDevice(), rawKeyCode);
            String payload = event.getDeviceId() + "|" + keyCode + "|" +
                    (event.getAction() == KeyEvent.ACTION_DOWN ? "1" : "0");
            UnityPlayer.UnitySendMessage(RECEIVER, "OnNativeKey", payload);
            return true;
        }
        return super.dispatchKeyEvent(event);
    }
}
