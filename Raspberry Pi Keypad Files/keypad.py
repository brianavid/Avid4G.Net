from evdev import InputDevice,ecodes,KeyEvent
import urllib

k=InputDevice("/dev/input/by-id/usb-Telink_Wireless_Receiver-if01-event-kbd")

for e in k.read_loop():
        if (e.type == ecodes.EV_KEY) and (KeyEvent(e).keystate == 1) :
                url = ""
                if KeyEvent(e).keycode == "KEY_KP0" :
                        url = "http://192.168.1.125:83/Security/LoadProfile/0"
                if KeyEvent(e).keycode == "KEY_KP1" :
                        url = "http://192.168.1.125:83/Security/LoadProfile/1"
                if KeyEvent(e).keycode == "KEY_KP2" :
                        url = "http://192.168.1.125:83/Security/LoadProfile/2"
                if KeyEvent(e).keycode == "KEY_KP3" :
                        url = "http://192.168.1.125:83/Security/LoadProfile/3"
                if KeyEvent(e).keycode == "KEY_KP4" :
                        url = "http://192.168.1.125:83/Security/LoadProfile/4"
                if KeyEvent(e).keycode == "KEY_KP5" :
                        url = "http://192.168.1.125:83/Security/LoadProfile/5"
                if KeyEvent(e).keycode == "KEY_KP6" :
                        url = "http://192.168.1.125:83/Security/LoadProfile/6"
                if KeyEvent(e).keycode == "KEY_KP7" :
                        url = "http://192.168.1.125:83/Security/LoadProfile/7"
                if KeyEvent(e).keycode == "KEY_KP8" :
                        url = "http://192.168.1.125:83/Security/LoadProfile/8"
                if KeyEvent(e).keycode == "KEY_KP9" :
                        url = "http://192.168.1.125:83/Security/LoadProfile/9"
                if url != "" :
                        response = urllib.urlopen(url).read()

