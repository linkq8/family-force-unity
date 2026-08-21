package com.familyforceunity.input;

import android.view.InputDevice;
import android.view.KeyEvent;
import android.view.MotionEvent;

import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerGameActivity;

public class FamilyForceUnityGameActivity extends UnityPlayerGameActivity {
    private static final String RECEIVER = "Android Input Bridge";

    private static boolean isControllerSource(int source) {
        return (source & InputDevice.SOURCE_GAMEPAD) == InputDevice.SOURCE_GAMEPAD ||
               (source & InputDevice.SOURCE_JOYSTICK) == InputDevice.SOURCE_JOYSTICK;
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
        int keyCode = event.getKeyCode();
        boolean controllerKey = isControllerSource(event.getSource()) &&
                (KeyEvent.isGamepadButton(keyCode) ||
                 keyCode == KeyEvent.KEYCODE_DPAD_UP || keyCode == KeyEvent.KEYCODE_DPAD_DOWN ||
                 keyCode == KeyEvent.KEYCODE_DPAD_LEFT || keyCode == KeyEvent.KEYCODE_DPAD_RIGHT);
        if (controllerKey) {
            String payload = event.getDeviceId() + "|" + keyCode + "|" +
                    (event.getAction() == KeyEvent.ACTION_DOWN ? "1" : "0");
            UnityPlayer.UnitySendMessage(RECEIVER, "OnNativeKey", payload);
            return true;
        }
        return super.dispatchKeyEvent(event);
    }
}
